using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
using Meta.XR;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#endif

/// <summary>
/// Handles depth data capture and projection to world space point clouds.
/// STEP 1: Depth Capture - Following official Oculus Depth API sample pattern.
/// </summary>
public class DepthCapture : MonoBehaviour
{
    [Header("Depth Settings")]
    [SerializeField, Min(0.01f)] private float minDepth = 0.1f;
    [SerializeField, Min(0.5f)] private float maxDepth = 5.0f;
    
    private EnvironmentDepthManager _depthManager;
    private bool _isInitialized = false;
    
    public bool IsDepthAvailable => _depthManager != null && _depthManager.IsDepthAvailable;
    
    private void Start()
    {
        StartCoroutine(InitializeDepthCaptureCoroutine());
    }
    
    /// <summary>
    /// Initialize EnvironmentDepthManager following official Oculus sample pattern
    /// </summary>
    private IEnumerator InitializeDepthCaptureCoroutine()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Check support first (static check, can be done before creating manager)
        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogWarning("[DepthCapture] Depth API is not supported on this device/platform");
            yield break;
        }
        
        // Find existing EnvironmentDepthManager (only one is allowed per scene)
        _depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        
        if (_depthManager == null)
        {
            // Create new one if none exists
            GameObject depthManagerGO = new GameObject("EnvironmentDepthManager");
            _depthManager = depthManagerGO.AddComponent<EnvironmentDepthManager>();
            
            // Enable it (following official sample pattern)
            _depthManager.enabled = true;
        }
        else
        {
            // Found existing one - make sure it's enabled
            if (!_depthManager.enabled)
            {
                _depthManager.enabled = true;
            }
        }
        
        // Wait until depth becomes available (following official sample pattern)
        // This is critical - depth texture is set in OnBeforeRender() which happens every frame
        // IsDepthAvailable becomes true only after first successful depth texture fetch
        while (!_depthManager.IsDepthAvailable)
        {
            yield return null; // Wait one frame
        }
        
        _isInitialized = true;
        
        Debug.Log("[DepthCapture] Depth initialized and available");
#else
        yield break;
#endif
    }
    
    /// <summary>
    /// Initialize EnvironmentDepthManager (public method for manual initialization)
    /// </summary>
    public void InitializeDepthCapture()
    {
        if (!_isInitialized)
        {
            StartCoroutine(InitializeDepthCaptureCoroutine());
        }
    }
    
    /// <summary>
    /// Capture depth data synchronized with RGB
    /// Handles both regular RenderTexture and Texture2DArray formats
    /// </summary>
    public float[] CaptureDepthFrame(RenderTexture depthTexture, int width, int height)
    {
        if (depthTexture == null || !depthTexture.IsCreated())
        {
            return null;
        }
        
        // Extract slice if it's a Texture2DArray (left eye = slice 0)
        RenderTexture sourceTexture = depthTexture;
        bool needsCleanup = false;
        
        if (depthTexture.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
        {
            sourceTexture = ExtractDepthSlice(depthTexture, 0);
            needsCleanup = true;
            
            if (sourceTexture == null)
            {
                return null;
            }
        }
        
        // Read depth data from texture
        // Depth texture is typically R16_UNorm format, so we need to read it correctly
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat);
        
        // Use a compute shader or material to convert depth format if needed
        // For now, try direct blit (may need adjustment based on actual format)
        Graphics.Blit(sourceTexture, rt);
        
        RenderTexture.active = rt;
        Texture2D tempTex = new Texture2D(width, height, TextureFormat.RFloat, false);
        tempTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tempTex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        // Convert to float array
        Color[] pixels = tempTex.GetPixels();
        float[] depthData = new float[pixels.Length];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            // Depth stored in red channel
            // If the original format was R16_UNorm, we may need to scale it
            depthData[i] = pixels[i].r;
        }
        
        Destroy(tempTex);
        
        // Cleanup extracted slice if we created it
        if (needsCleanup && sourceTexture != depthTexture)
        {
            sourceTexture.Release();
            Destroy(sourceTexture);
        }
        
        return depthData;
    }
    
    /// <summary>
    /// Project depth data to world space point cloud using camera intrinsics
    /// </summary>
    public Vector3[] ProjectDepthToWorld(float[] depthData, int width, int height, 
        Matrix4x4 cameraTransform, Camera camera)
    {
        if (depthData == null || depthData.Length != width * height)
        {
            return null;
        }
        
        if (camera == null)
        {
            return null;
        }
        
        List<Vector3> points = new List<Vector3>();
        
        // Get camera intrinsics (approximate for Quest)
        float fx = width / (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        float fy = fx; // Assuming square pixels
        float cx = width * 0.5f;
        float cy = height * 0.5f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depth = depthData[index];
                
                // Filter invalid depths (too close/far, NaN, zero)
                if (depth < minDepth || depth > maxDepth || depth <= 0 || float.IsNaN(depth) || float.IsInfinity(depth))
                    continue;
                
                // Convert pixel to camera space
                float xCam = (x - cx) * depth / fx;
                float yCam = (y - cy) * depth / fy;
                float zCam = depth;
                
                Vector3 pointCameraSpace = new Vector3(xCam, -yCam, zCam);
                
                // Transform to world space
                Vector3 pointWorldSpace = cameraTransform.MultiplyPoint3x4(pointCameraSpace);
                
                points.Add(pointWorldSpace);
            }
        }
        
        return points.ToArray();
    }
    
    /// <summary>
    /// Filter points by table height
    /// </summary>
    public Vector3[] FilterPointsByTableHeight(Vector3[] points, float tableHeight, float tolerance = 0.5f)
    {
        List<Vector3> filteredPoints = new List<Vector3>();
        
        float minHeight = tableHeight + 0.01f; // 1cm above table
        float maxHeight = tableHeight + tolerance; // Up to 50cm above table
        
        foreach (var point in points)
        {
            if (point.y >= minHeight && point.y <= maxHeight)
            {
                filteredPoints.Add(point);
            }
        }
        
        return filteredPoints.ToArray();
    }
    
    /// <summary>
    /// Get current depth texture from EnvironmentDepthManager
    /// Following official Oculus sample: access via shader global _EnvironmentDepthTexture
    /// The depth texture is set by EnvironmentDepthManager in OnBeforeRender() every frame
    /// </summary>
    public RenderTexture GetCurrentDepthTexture()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (_depthManager == null || !_depthManager.IsDepthAvailable)
            return null;
        
        // Access depth texture via shader global (set by EnvironmentDepthManager)
        // Following official sample pattern
        Texture depthTexGlobal = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
        
        if (depthTexGlobal == null)
            return null;
        
        // Cast to RenderTexture (it should be a RenderTexture)
        RenderTexture depthTex = depthTexGlobal as RenderTexture;
        
        if (depthTex != null && depthTex.IsCreated())
        {
            return depthTex;
        }
        
        return null;
#else
        return null;
#endif
    }
    
    /// <summary>
    /// Extract a single slice from Texture2DArray depth texture
    /// Returns a RenderTexture with the left eye depth data
    /// </summary>
    private RenderTexture ExtractDepthSlice(RenderTexture depthArray, int sliceIndex = 0)
    {
        if (depthArray == null || !depthArray.IsCreated())
            return null;
        
        // Check if it's actually a Texture2DArray
        if (depthArray.dimension != UnityEngine.Rendering.TextureDimension.Tex2DArray)
        {
            // It's already a regular 2D texture, return as-is
            return depthArray;
        }
        
        // Create a temporary RenderTexture for the slice
        RenderTexture sliceRT = new RenderTexture(depthArray.width, depthArray.height, 0, 
            depthArray.format, RenderTextureReadWrite.Linear);
        sliceRT.Create();
        
        // Copy the slice using Graphics.CopyTexture
        Graphics.CopyTexture(depthArray, sliceIndex, sliceRT, 0);
        
        return sliceRT;
    }
    
    /// <summary>
    /// Get debug info about depth capture status
    /// </summary>
    public string GetDebugInfo()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (_depthManager == null)
            return "EnvironmentDepthManager: NULL";
        
        bool isAvailable = _depthManager.IsDepthAvailable;
        RenderTexture depthTex = GetCurrentDepthTexture();
        Texture depthTexGlobal = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
        
        string info = $"EnvironmentDepthManager: Found\n";
        info += $"IsDepthAvailable: {isAvailable}\n";
        info += $"IsSupported: {EnvironmentDepthManager.IsSupported}\n";
        
        // Check permissions (Android only)
#if UNITY_ANDROID
        bool hasPermission = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
        info += $"HasPermission: {hasPermission}\n";
#else
        info += $"HasPermission: N/A (Editor)\n";
#endif
        
        if (depthTexGlobal == null)
        {
            info += "Global Depth Texture: NULL (not set yet)";
        }
        else
        {
            info += $"Global Depth Texture Type: {depthTexGlobal.GetType().Name}\n";
            
            if (depthTexGlobal is RenderTexture rt)
            {
                info += $"  Dimension: {rt.dimension}\n";
                info += $"  Format: {rt.format}\n";
                if (rt.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
                {
                    info += $"  Array Size: {rt.volumeDepth}\n";
                }
            }
        }
        
        if (depthTex == null)
        {
            info += "\nExtracted Depth Texture: NULL";
        }
        else
        {
            info += $"\nExtracted Depth Texture: {depthTex.width}x{depthTex.height}\n";
            info += $"IsCreated: {depthTex.IsCreated()}";
        }
        
        return info;
#else
        return "Depth API not available on this platform";
#endif
    }
}

