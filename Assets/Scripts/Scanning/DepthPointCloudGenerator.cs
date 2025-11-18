using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Production-ready depth point cloud generator using proper reprojection matrices.
/// Based on depth-scanning-implementation.md guide.
/// Compatible with Quest 3 and Oculus SDK v81.
/// Pipeline: Screen Space [0,1] → Clip Space [-1,1] → Homogeneous Clip Space 
/// → Homogeneous World Space → World Space (perspective divide)
/// </summary>
public class DepthPointCloudGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnvironmentDepthManager depthManager;
    
    [Header("Sampling Settings")]
    [SerializeField] private int strideX = 4;  // Sample every Nth pixel
    [SerializeField] private int strideY = 4;
    [SerializeField] private float maxDepth = 4.0f;  // Quest 3 max reliable depth
    [SerializeField] private float minDepth = 0.1f; // Minimum valid depth
    
    [Header("Visualization")]
    [SerializeField] private bool visualize = false; // Set to false if using PointCloudVisualizer instead
    [SerializeField] private Material pointMaterial;
    [SerializeField] private float pointSize = 0.008f;
    
    [Header("Performance")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int maxPointsPerFrame = 5000;
    [SerializeField] private float updateInterval = 0.1f; // Update every 0.1 seconds (10 FPS)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Shader property IDs (matching EnvironmentDepthManager)
    private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
    private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
    
    private Texture2D depthTexture;
    private Matrix4x4 reprojectionMatrix;
    private Vector3[] worldSpacePoints;
    private Color[] pointColors;
    private GameObject pointCloudContainer;
    
    // Object pooling
    private Queue<GameObject> pointPool = new Queue<GameObject>();
    private List<GameObject> activePoints = new List<GameObject>();
    
    // Performance
    private float lastUpdateTime;
    private int frameSkipCounter = 0;
    
    void Start()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (depthManager == null)
        {
            depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        }
        
        if (depthManager == null)
        {
            Debug.LogError("[DepthPointCloudGenerator] EnvironmentDepthManager not found! Please add it to the scene.");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[DepthPointCloudGenerator] ✅ Found EnvironmentDepthManager. IsSupported: {EnvironmentDepthManager.IsSupported}");
        }
        
        if (useObjectPooling)
        {
            InitializePointPool(maxPointsPerFrame);
        }
#else
        Debug.LogWarning("[DepthPointCloudGenerator] Depth API not supported on this platform");
#endif
    }
    
    void Update()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Check depth manager
        if (depthManager == null)
        {
            return;
        }
        
        // Check if depth is available
        if (!depthManager.IsDepthAvailable)
        {
            if (showDebugLogs && frameSkipCounter % 60 == 0) // Log every 60 frames
            {
                Debug.LogWarning("[DepthPointCloudGenerator] ⚠️ Depth not available yet. Waiting for EnvironmentDepthManager...");
            }
            frameSkipCounter++;
            return;
        }
        
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        
        lastUpdateTime = Time.time;
        
        // Generate point cloud
        Vector3[] points = GeneratePointCloud();
        
        if (points != null && points.Length > 0 && showDebugLogs)
        {
            Debug.Log($"[DepthPointCloudGenerator] ✅ Generated {points.Length} points");
        }
        
        if (visualize && points != null && points.Length > 0)
        {
            VisualizePointCloud();
        }
#endif
    }
    
    /// <summary>
    /// Main function to generate world space point cloud from depth data
    /// </summary>
    public Vector3[] GeneratePointCloud()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Get depth texture from shader global (set by EnvironmentDepthManager)
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        
        if (depthTexGlobal == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[DepthPointCloudGenerator] Depth texture is null. IsDepthAvailable: " + 
                    (depthManager != null ? depthManager.IsDepthAvailable.ToString() : "null"));
            }
            return null;
        }
        
        // Get reprojection matrices from shader global
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        
        if (reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[DepthPointCloudGenerator] No reprojection matrices available!");
            }
            return null;
        }
        
        // Use first eye (typically left eye)
        // The inverse transforms from Clip Space → World Space
        reprojectionMatrix = reprojectionMatrices[0].inverse;
        
        // Convert RenderTexture to Texture2D for reading
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        if (depthRT == null || !depthRT.IsCreated())
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[DepthPointCloudGenerator] Depth texture is not a valid RenderTexture. Type: {depthTexGlobal.GetType()}");
            }
            return null;
        }
        
        // Read depth texture to Texture2D
        depthTexture = ReadRenderTextureToTexture2D(depthRT);
        
        if (depthTexture == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[DepthPointCloudGenerator] Failed to read depth texture!");
            }
            return null;
        }
        
        int width = depthTexture.width;
        int height = depthTexture.height;
        
        if (showDebugLogs && frameSkipCounter % 300 == 0) // Log every 5 seconds at 60fps
        {
            Debug.Log($"[DepthPointCloudGenerator] Depth texture: {width}x{height}");
        }
        
        // Read pixels from texture
        Color[] pixels = depthTexture.GetPixels();
        
        if (pixels == null || pixels.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[DepthPointCloudGenerator] Failed to read pixels from depth texture!");
            }
            return null;
        }
        
        // Calculate number of points we'll generate
        int maxPoints = (width / strideX) * (height / strideY);
        Vector3[] tempPoints = new Vector3[maxPoints];
        Color[] tempColors = new Color[maxPoints];
        
        int pointIndex = 0;
        Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        for (int y = 0; y < height; y += strideY)
        {
            for (int x = 0; x < width; x += strideX)
            {
                int pixelIndex = y * width + x;
                
                if (pixelIndex >= pixels.Length)
                    continue;
                
                // Depth is stored in R channel [0,1]
                float depth = pixels[pixelIndex].r;
                
                // Skip invalid depth
                if (depth <= 0.0f || depth > 1.0f)
                    continue;
                
                // Calculate UV coordinates [0,1]
                float u = (float)x / width;
                float v = (float)y / height;
                
                // Transform to world space
                Vector3 worldPos = DepthToWorldSpace(u, v, depth);
                
                // Skip invalid world positions
                if (float.IsNaN(worldPos.x) || float.IsInfinity(worldPos.x) || worldPos == Vector3.zero)
                    continue;
                
                // Calculate actual distance from camera
                float distance = Vector3.Distance(worldPos, cameraPos);
                
                // Filter by depth range
                if (distance < minDepth || distance > maxDepth)
                    continue;
                
                // Limit points per frame for performance
                if (pointIndex >= maxPointsPerFrame)
                    break;
                
                tempPoints[pointIndex] = worldPos;
                
                // Color by distance (blue = close, red = far)
                float normalizedDistance = Mathf.Clamp01(distance / maxDepth);
                tempColors[pointIndex] = Color.Lerp(Color.blue, Color.red, normalizedDistance);
                
                pointIndex++;
            }
            
            if (pointIndex >= maxPointsPerFrame)
                break;
        }
        
        // Trim arrays to actual point count
        worldSpacePoints = new Vector3[pointIndex];
        pointColors = new Color[pointIndex];
        System.Array.Copy(tempPoints, worldSpacePoints, pointIndex);
        System.Array.Copy(tempColors, pointColors, pointIndex);
        
        return worldSpacePoints;
#else
        return null;
#endif
    }
    
    /// <summary>
    /// Converts depth texture sample to world space position.
    /// Pipeline: Screen Space → Clip Space → Homogeneous Clip → Homogeneous World → World Space
    /// </summary>
    /// <param name="u">Texture U coordinate [0,1]</param>
    /// <param name="v">Texture V coordinate [0,1]</param>
    /// <param name="depth">Depth value from texture [0,1]</param>
    /// <returns>World space position</returns>
    Vector3 DepthToWorldSpace(float u, float v, float depth)
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
        Vector4 homogeneousWorldSpace = reprojectionMatrix * homogeneousClipSpace;
        
        // Step 5: Perspective Divide (convert from homogeneous to cartesian)
        // Formula: worldPos = homogeneousWorld.xyz / homogeneousWorld.w
        if (Mathf.Abs(homogeneousWorldSpace.w) < 0.0001f)
        {
            // Avoid division by zero
            return Vector3.zero;
        }
        
        Vector3 worldSpacePoint = new Vector3(
            homogeneousWorldSpace.x / homogeneousWorldSpace.w,
            homogeneousWorldSpace.y / homogeneousWorldSpace.w,
            homogeneousWorldSpace.z / homogeneousWorldSpace.w
        );
        
        return worldSpacePoint;
    }
    
    /// <summary>
    /// Read RenderTexture to Texture2D for CPU access
    /// </summary>
    Texture2D ReadRenderTextureToTexture2D(RenderTexture rt)
    {
        if (rt == null || !rt.IsCreated())
        {
            return null;
        }
        
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        // Try different texture formats - depth texture might be RFloat or R16
        Texture2D texture = null;
        
        try
        {
            // First try RFloat (most common for depth)
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
                if (showDebugLogs)
                {
                    Debug.LogError($"[DepthPointCloudGenerator] Failed to read RenderTexture: {e.Message}");
                }
            }
        }
        
        RenderTexture.active = previous;
        return texture;
    }
    
    void InitializePointPool(int poolSize)
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject point = CreatePointObject();
            point.SetActive(false);
            pointPool.Enqueue(point);
        }
    }
    
    GameObject CreatePointObject()
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.transform.localScale = Vector3.one * pointSize;
        
        Renderer renderer = point.GetComponent<Renderer>();
        if (pointMaterial != null)
            renderer.material = pointMaterial;
        
        Destroy(point.GetComponent<Collider>());
        
        return point;
    }
    
    GameObject GetPointFromPool()
    {
        if (pointPool.Count > 0)
        {
            GameObject point = pointPool.Dequeue();
            point.SetActive(true);
            return point;
        }
        else
        {
            return CreatePointObject();
        }
    }
    
    void ReturnPointToPool(GameObject point)
    {
        point.SetActive(false);
        pointPool.Enqueue(point);
    }
    
    void VisualizePointCloud()
    {
        if (worldSpacePoints == null || worldSpacePoints.Length == 0)
            return;
        
        // Return all active points to pool
        if (useObjectPooling)
        {
            foreach (GameObject point in activePoints)
            {
                ReturnPointToPool(point);
            }
            activePoints.Clear();
        }
        else
        {
            // Destroy previous visualization
            if (pointCloudContainer != null)
                Destroy(pointCloudContainer);
            
            pointCloudContainer = new GameObject("PointCloud");
        }
        
        // Create new points
        for (int i = 0; i < worldSpacePoints.Length; i++)
        {
            GameObject point;
            
            if (useObjectPooling)
            {
                point = GetPointFromPool();
                activePoints.Add(point);
            }
            else
            {
                point = CreatePointObject();
                point.transform.parent = pointCloudContainer.transform;
            }
            
            point.transform.position = worldSpacePoints[i];
            
            // Apply color
            if (pointColors != null && i < pointColors.Length)
            {
                point.GetComponent<Renderer>().material.color = pointColors[i];
            }
        }
    }
    
    /// <summary>
    /// Get the generated point cloud for further processing
    /// </summary>
    public Vector3[] GetPointCloud()
    {
        return worldSpacePoints;
    }
    
    /// <summary>
    /// Get point colors for visualization or analysis
    /// </summary>
    public Color[] GetPointColors()
    {
        return pointColors;
    }
    
    /// <summary>
    /// Get number of points in current point cloud
    /// </summary>
    public int GetPointCount()
    {
        return worldSpacePoints != null ? worldSpacePoints.Length : 0;
    }
    
    void OnDestroy()
    {
        // Clean up point pool
        foreach (GameObject point in pointPool)
        {
            if (point != null)
                Destroy(point);
        }
        
        foreach (GameObject point in activePoints)
        {
            if (point != null)
                Destroy(point);
        }
        
        if (depthTexture != null)
        {
            Destroy(depthTexture);
        }
    }
}
