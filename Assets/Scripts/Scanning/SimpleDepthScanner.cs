using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Simple one-button depth scanner for Quest 3
/// Based on official Unity-DepthAPI samples
/// </summary>
public class SimpleDepthScanner : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private int pointStride = 4;
    [SerializeField] private float minDepth = 0.1f;
    [SerializeField] private float maxDepth = 4.0f;
    [SerializeField] private int maxPoints = 5000;
    [SerializeField] private bool debugDepthValues = false; // Enable to see depth value diagnostics
    
    [Header("Visualization")]
    [SerializeField] private Color pointColor = Color.cyan;
    [SerializeField] private float pointSize = 0.01f;
    
    [Header("Controller")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private OVRInput.Button scanButton = OVRInput.Button.One;
    
    // Depth system
#if UNITY_EDITOR || UNITY_ANDROID
    private EnvironmentDepthManager depthManager;
#endif
    private bool isInitialized = false;
    private float lastWarningTime = 0f;
    private const float WARNING_COOLDOWN = 2f; // Only show warning every 2 seconds max
    
    // Visualization
    private GameObject pointCloudContainer;
    private List<GameObject> pointObjects = new List<GameObject>();
    private Material pointMaterial;
    
    // Shader globals (set by EnvironmentDepthManager)
    private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
    private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
    
    void Start()
    {
        StartCoroutine(InitializeDepth());
    }
    
    IEnumerator InitializeDepth()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Check support
        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogError("[SimpleDepthScanner] Depth API not supported");
            yield break;
        }
        
        // Find or create EnvironmentDepthManager
        depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        
        if (depthManager == null)
        {
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig == null)
            {
                Debug.LogError("[SimpleDepthScanner] No OVRCameraRig found");
                yield break;
            }
            
            depthManager = cameraRig.gameObject.AddComponent<EnvironmentDepthManager>();
            depthManager.RemoveHands = true;
            depthManager.enabled = true;
        }
        else if (!depthManager.enabled)
        {
            depthManager.enabled = true;
        }
        
        // Wait for depth to become available
        int maxWait = 300;
        int frames = 0;
        
        while (!depthManager.IsDepthAvailable && frames < maxWait)
        {
            frames++;
            yield return null;
        }
        
        if (!depthManager.IsDepthAvailable)
        {
            Debug.LogError("[SimpleDepthScanner] Depth failed to initialize. Complete Space Setup on Quest.");
            yield break;
        }
        
        isInitialized = true;
        Debug.Log("[SimpleDepthScanner] Ready! Press A to scan");
#endif
    }
    
    void Update()
    {
        if (OVRInput.GetDown(scanButton, controller))
        {
            Scan();
        }
    }
    
    void Scan()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (!isInitialized || depthManager == null || !depthManager.IsDepthAvailable)
        {
            if (Time.time - lastWarningTime > WARNING_COOLDOWN)
            {
                Debug.LogWarning("[SimpleDepthScanner] Depth not ready - waiting for depth data");
                lastWarningTime = Time.time;
            }
            return;
        }
        
        ClearPointCloud();
        
        Vector3[] points = GeneratePointCloud();
        
        if (points != null && points.Length > 0)
        {
            Debug.Log($"[SimpleDepthScanner] Scan complete: {points.Length} points");
            VisualizePoints(points);
            lastWarningTime = 0f; // Reset warning cooldown on success
        }
        else
        {
            // Only show warning with cooldown to avoid spam
            if (Time.time - lastWarningTime > WARNING_COOLDOWN)
            {
                string reason = GetNoPointsReason();
                Debug.LogWarning($"[SimpleDepthScanner] No points generated. {reason}");
                lastWarningTime = Time.time;
            }
        }
#endif
    }
    
    string GetNoPointsReason()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Check depth texture
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        if (depthTexGlobal == null)
        {
            return "Reason: Depth texture not available";
        }
        
        // Check reprojection matrices
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        if (reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            return "Reason: Reprojection matrices not available";
        }
        
        // Check RenderTexture
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        if (depthRT == null || !depthRT.IsCreated())
        {
            return "Reason: Depth RenderTexture not created";
        }
        
        // If we get here, depth data exists but no valid points were found
        // This could be due to depth range filtering or invalid depth values
        return "Reason: No valid points found (check depth range: " + minDepth + "-" + maxDepth + "m)";
#else
        return "Reason: Not supported on this platform";
#endif
    }
    
    Vector3[] GeneratePointCloud()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Get depth texture and reprojection matrices from shader globals
        // These are set by EnvironmentDepthManager every frame
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        
        if (depthTexGlobal == null || reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            return null;
        }
        
        // Use first eye's inverse reprojection matrix
        // The inverse transforms from Clip Space → World Space
        Matrix4x4 reprojectionMatrix = reprojectionMatrices[0].inverse;
        
        // Convert RenderTexture to Texture2D for reading pixels
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        if (depthRT == null || !depthRT.IsCreated())
        {
            return null;
        }
        
        Texture2D depthTexture = ReadRenderTexture(depthRT);
        if (depthTexture == null)
        {
            return null;
        }
        
        int width = depthTexture.width;
        int height = depthTexture.height;
        Color[] pixels = depthTexture.GetPixels();
        
        if (pixels == null || pixels.Length == 0)
        {
            Destroy(depthTexture);
            return null;
        }
        
        List<Vector3> points = new List<Vector3>();
        
        // Get camera position - prefer OVRCameraRig center eye for accurate world space
        // Note: Depth API computes depth from the two main cameras (not depth sensor directly)
        // Reference: https://communityforums.atmeta.com/discussions/dev-general/how-can-i-get-a-depth-mapor-point-cloud-just-from-my-quest-3s-depth-sensor/1111934
        Vector3 cameraPos = Vector3.zero;
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null && cameraRig.centerEyeAnchor != null)
        {
            cameraPos = cameraRig.centerEyeAnchor.position;
        }
        else
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraPos = mainCamera.transform.position;
            }
        }
        
        // Debug counters
        int totalPixelsSampled = 0;
        int invalidDepthCount = 0;
        int outOfRangeCount = 0;
        int nanInfCount = 0;
        int filteredByRangeCount = 0;
        float minDepthFound = float.MaxValue;
        float maxDepthFound = float.MinValue;
        float minDistanceFound = float.MaxValue;
        float maxDistanceFound = float.MinValue;
        
        // Sample depth texture
        for (int y = 0; y < height; y += pointStride)
        {
            for (int x = 0; x < width; x += pointStride)
            {
                if (points.Count >= maxPoints)
                    break;
                
                int pixelIndex = y * width + x;
                if (pixelIndex >= pixels.Length)
                    continue;
                
                totalPixelsSampled++;
                float depth = pixels[pixelIndex].r;
                
                // Track depth value range
                if (depth > 0 && depth <= 1)
                {
                    if (depth < minDepthFound) minDepthFound = depth;
                    if (depth > maxDepthFound) maxDepthFound = depth;
                }
                
                if (depth <= 0.0f || depth > 1.0f)
                {
                    invalidDepthCount++;
                    continue;
                }
                
                float u = (float)x / width;
                float v = (float)y / height;
                
                Vector3 worldPos = DepthToWorldSpace(u, v, depth, reprojectionMatrix);
                
                if (float.IsNaN(worldPos.x) || float.IsInfinity(worldPos.x) || worldPos == Vector3.zero)
                {
                    nanInfCount++;
                    continue;
                }
                
                float distance = Vector3.Distance(worldPos, cameraPos);
                
                // Track distance range
                if (distance < minDistanceFound) minDistanceFound = distance;
                if (distance > maxDistanceFound) maxDistanceFound = distance;
                
                if (distance < minDepth || distance > maxDepth)
                {
                    filteredByRangeCount++;
                    continue;
                }
                
                points.Add(worldPos);
            }
            
            if (points.Count >= maxPoints)
                break;
        }
        
        // Debug output if enabled
        if (debugDepthValues || points.Count == 0)
        {
            Debug.Log($"[SimpleDepthScanner] Depth Analysis:\n" +
                     $"  Pixels sampled: {totalPixelsSampled}\n" +
                     $"  Invalid depth values (<=0 or >1): {invalidDepthCount}\n" +
                     $"  Depth range found: {minDepthFound:F3} - {maxDepthFound:F3} (normalized 0-1)\n" +
                     $"  NaN/Infinity world positions: {nanInfCount}\n" +
                     $"  Filtered by distance range ({minDepth}-{maxDepth}m): {filteredByRangeCount}\n" +
                     $"  Distance range found: {minDistanceFound:F2} - {maxDistanceFound:F2}m\n" +
                     $"  Valid points generated: {points.Count}");
        }
        
        Destroy(depthTexture);
        return points.ToArray();
#else
        return null;
#endif
    }
    
    /// <summary>
    /// Converts depth texture sample to world space position.
    /// Pipeline: Screen Space [0,1] → Clip Space [-1,1] → Homogeneous Clip → Homogeneous World → World Space
    /// 
    /// Based on forum discussion:
    /// https://communityforums.atmeta.com/discussions/dev-general/how-can-i-get-a-depth-mapor-point-cloud-just-from-my-quest-3s-depth-sensor/1111934
    /// 
    /// Note: Depth API computes depth from the two main cameras (stereo reconstruction), not the depth sensor directly.
    /// The depth value is in [0,1] range in the R channel of the texture.
    /// </summary>
    /// <param name="u">Texture U coordinate [0,1]</param>
    /// <param name="v">Texture V coordinate [0,1]</param>
    /// <param name="depth">Depth value from texture [0,1]</param>
    /// <param name="reprojectionMatrix">Inverse reprojection matrix (reprojectionMatrices[0].inverse)</param>
    /// <returns>World space position</returns>
    Vector3 DepthToWorldSpace(float u, float v, float depth, Matrix4x4 reprojectionMatrix)
    {
        // Step 1: Create point in Screen Space [0,1]
        // Using U, V, and Depth from texture
        Vector3 screenSpacePoint = new Vector3(u, v, depth);
        
        // Step 2: Transform to Clip Space [-1,1]
        // Formula: clipSpace = screenSpace * 2.0 - 1.0
        Vector3 clipSpacePoint = screenSpacePoint * 2.0f - Vector3.one;
        
        // Step 3: Make Homogeneous (add w component = 1)
        // Required for matrix transformation
        Vector4 homogeneousClipSpace = new Vector4(
            clipSpacePoint.x, 
            clipSpacePoint.y, 
            clipSpacePoint.z, 
            1.0f
        );
        
        // Step 4: Transform to Homogeneous World Space
        // Using inverse reprojection matrix (inverse of ProjectionView matrix)
        // This transforms from Clip Space to World Space
        // The reprojectionMatrix is the inverse of the projectionView matrix
        Vector4 homogeneousWorldSpace = reprojectionMatrix * homogeneousClipSpace;
        
        // Step 5: Perspective Divide (convert from homogeneous to cartesian)
        // Formula: worldPos = homogeneousWorld.xyz / homogeneousWorld.w
        if (Mathf.Abs(homogeneousWorldSpace.w) < 0.0001f)
        {
            return Vector3.zero; // Avoid division by zero
        }
        
        Vector3 worldSpacePoint = new Vector3(
            homogeneousWorldSpace.x / homogeneousWorldSpace.w,
            homogeneousWorldSpace.y / homogeneousWorldSpace.w,
            homogeneousWorldSpace.z / homogeneousWorldSpace.w
        );
        
        return worldSpacePoint;
    }
    
    Texture2D ReadRenderTexture(RenderTexture rt)
    {
        if (rt == null || !rt.IsCreated())
            return null;
        
        RenderTexture.active = rt;
        
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        
        RenderTexture.active = null;
        
        return tex;
    }
    
    void VisualizePoints(Vector3[] points)
    {
        if (points == null || points.Length == 0)
            return;
        
        if (pointCloudContainer == null)
        {
            pointCloudContainer = new GameObject("PointCloud");
        }
        
        if (pointMaterial == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            
            pointMaterial = new Material(shader);
            pointMaterial.color = pointColor;
        }
        
        // Create spheres for each point
        foreach (Vector3 point in points)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = Vector3.one * pointSize;
            sphere.transform.parent = pointCloudContainer.transform;
            
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = pointMaterial;
            }
            
            Collider collider = sphere.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            pointObjects.Add(sphere);
        }
    }
    
    void ClearPointCloud()
    {
        foreach (GameObject obj in pointObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        pointObjects.Clear();
    }
    
    void OnDestroy()
    {
        ClearPointCloud();
        
        if (pointCloudContainer != null)
        {
            Destroy(pointCloudContainer);
        }
        
        if (pointMaterial != null)
        {
            Destroy(pointMaterial);
        }
    }
}
