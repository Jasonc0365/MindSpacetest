using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Simplified Depth Capture Manager - Captures depth data on demand
/// Based on step2-depth-capture-implementation.md and depth-scanning-implementation.md
/// </summary>
public class DepthCaptureManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OVRCameraRig cameraRig;
    
    [Header("Capture Settings")]
    [SerializeField] private int pointStride = 4; // Sample every 4th pixel
    [SerializeField] private float maxDepth = 4.0f; // Quest 3 max reliable depth
    [SerializeField] private float minDepth = 0.1f; // Minimum valid depth
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Depth components
    private EnvironmentDepthManager depthManager;
    
    // Latest capture data
    private DepthFrameData latestFrame;
    
    // State
    private bool isInitialized = false;
    
    // Shader property IDs (matching EnvironmentDepthManager)
    private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
    private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
    
    void Start()
    {
        InitializeDepthSystem();
    }
    
    void InitializeDepthSystem()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Find or create EnvironmentDepthManager
        depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        
        if (depthManager == null)
        {
            if (showDebugInfo)
                Debug.Log("[DepthCaptureManager] Creating EnvironmentDepthManager...");
            GameObject depthObj = new GameObject("EnvironmentDepthManager");
            depthManager = depthObj.AddComponent<EnvironmentDepthManager>();
        }
        
        // Find camera rig if not assigned
        if (cameraRig == null)
        {
            cameraRig = FindFirstObjectByType<OVRCameraRig>();
        }
        
        if (cameraRig == null)
        {
            Debug.LogError("[DepthCaptureManager] OVRCameraRig not found!");
            return;
        }
        
        isInitialized = true;
        if (showDebugInfo)
            Debug.Log("[DepthCaptureManager] ✅ Depth system initialized");
#else
        Debug.LogWarning("[DepthCaptureManager] Depth API not supported on this platform");
#endif
    }
    
    /// <summary>
    /// Capture depth frame on demand
    /// </summary>
    public bool CaptureDepthFrame()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (!isInitialized)
        {
            InitializeDepthSystem();
            if (!isInitialized) return false;
        }
        
        if (depthManager == null || !depthManager.IsDepthAvailable)
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] Depth not available. Check permissions and Space Setup.");
            return false;
        }
        
        // Get depth texture from shader global
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        
        if (depthTexGlobal == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] Depth texture is null");
            return false;
        }
        
        // Get reprojection matrices
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        
        if (reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] No reprojection matrices available!");
            return false;
        }
        
        // Convert RenderTexture to Texture2D for reading
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        if (depthRT == null || !depthRT.IsCreated())
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] Depth texture is not a valid RenderTexture");
            return false;
        }
        
        // Read depth texture
        Texture2D depthTexture = ReadRenderTextureToTexture2D(depthRT);
        
        if (depthTexture == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] Failed to read depth texture!");
            return false;
        }
        
        // Create frame data
        latestFrame = new DepthFrameData
        {
            width = depthTexture.width,
            height = depthTexture.height,
            timestamp = Time.time,
            cameraPosition = cameraRig.centerEyeAnchor.position,
            cameraRotation = cameraRig.centerEyeAnchor.rotation,
            reprojectionMatrix = reprojectionMatrices[0].inverse // Inverse for clip to world
        };
        
        // Read depth values
        Color[] pixels = depthTexture.GetPixels();
        latestFrame.depthValues = new float[pixels.Length];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            // Depth is encoded in red channel (0-1 normalized)
            latestFrame.depthValues[i] = pixels[i].r;
        }
        
        // Clean up
        Destroy(depthTexture);
        
        if (showDebugInfo)
            Debug.Log($"[DepthCaptureManager] 📸 Captured depth frame: {latestFrame.width}x{latestFrame.height}");
        
        return true;
#else
        return false;
#endif
    }
    
    /// <summary>
    /// Read RenderTexture to Texture2D for CPU access
    /// </summary>
    Texture2D ReadRenderTextureToTexture2D(RenderTexture rt)
    {
        if (rt == null || !rt.IsCreated())
            return null;
        
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D texture = null;
        
        try
        {
            // Try RFloat first (most common for depth)
            texture = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
        }
        catch
        {
            // If RFloat fails, try RGB24
            if (texture != null)
            {
                Destroy(texture);
            }
            
            try
            {
                texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture.Apply();
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                    Debug.LogError($"[DepthCaptureManager] Failed to read RenderTexture: {e.Message}");
            }
        }
        
        RenderTexture.active = previous;
        return texture;
    }
    
    /// <summary>
    /// Convert depth to point cloud in world space
    /// </summary>
    public Vector3[] ConvertDepthToPointCloud()
    {
        if (latestFrame.depthValues == null || latestFrame.depthValues.Length == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("[DepthCaptureManager] No depth data to convert");
            return new Vector3[0];
        }
        
        List<Vector3> points = new List<Vector3>();
        
        // Convert depth pixels to 3D points
        for (int y = 0; y < latestFrame.height; y += pointStride)
        {
            for (int x = 0; x < latestFrame.width; x += pointStride)
            {
                int index = y * latestFrame.width + x;
                
                if (index >= latestFrame.depthValues.Length)
                    continue;
                
                float depth = latestFrame.depthValues[index];
                
                // Skip invalid depths
                if (depth <= 0.0f || depth > 1.0f)
                    continue;
                
                // Calculate UV coordinates [0,1]
                float u = (float)x / latestFrame.width;
                float v = (float)y / latestFrame.height;
                
                // Transform to world space using reprojection matrix
                Vector3 worldPos = DepthToWorldSpace(u, v, depth);
                
                // Skip invalid positions
                if (float.IsNaN(worldPos.x) || float.IsInfinity(worldPos.x))
                    continue;
                
                // Filter by depth range
                float distance = Vector3.Distance(worldPos, latestFrame.cameraPosition);
                if (distance < minDepth || distance > maxDepth)
                    continue;
                
                points.Add(worldPos);
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"[DepthCaptureManager] ✅ Generated {points.Count} points from depth");
        
        return points.ToArray();
    }
    
    /// <summary>
    /// Converts depth texture sample to world space position.
    /// Pipeline: Screen Space → Clip Space → Homogeneous Clip → Homogeneous World → World Space
    /// </summary>
    Vector3 DepthToWorldSpace(float u, float v, float depth)
    {
        // Step 1: Screen Space [0,1] → Clip Space [-1,1]
        Vector3 clipSpacePoint = new Vector3(u * 2.0f - 1.0f, v * 2.0f - 1.0f, depth * 2.0f - 1.0f);
        
        // Step 2: Make Homogeneous (add w component = 1)
        Vector4 homogeneousClipSpace = new Vector4(clipSpacePoint.x, clipSpacePoint.y, clipSpacePoint.z, 1.0f);
        
        // Step 3: Transform to Homogeneous World Space using inverse reprojection matrix
        Vector4 homogeneousWorldSpace = latestFrame.reprojectionMatrix * homogeneousClipSpace;
        
        // Step 4: Perspective Divide
        if (Mathf.Abs(homogeneousWorldSpace.w) < 0.0001f)
            return Vector3.zero;
        
        Vector3 worldSpacePoint = new Vector3(
            homogeneousWorldSpace.x / homogeneousWorldSpace.w,
            homogeneousWorldSpace.y / homogeneousWorldSpace.w,
            homogeneousWorldSpace.z / homogeneousWorldSpace.w
        );
        
        return worldSpacePoint;
    }
    
    public DepthFrameData GetLatestFrame()
    {
        return latestFrame;
    }
    
    public bool IsReady()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        return isInitialized && depthManager != null && depthManager.IsDepthAvailable;
#else
        return false;
#endif
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 500, 30), 
            $"Depth Available: {IsReady()}", style);
        
        if (latestFrame.depthValues != null)
        {
            GUI.Label(new Rect(10, 40, 500, 30), 
                $"Frame: {latestFrame.width}x{latestFrame.height}", style);
        }
    }
}

[System.Serializable]
public struct DepthFrameData
{
    public float[] depthValues;
    public int width;
    public int height;
    public float timestamp;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
    public Matrix4x4 reprojectionMatrix;
}

