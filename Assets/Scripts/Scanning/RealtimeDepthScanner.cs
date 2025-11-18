using UnityEngine;
using System.Collections.Generic;
#if UNITY_ANDROID || UNITY_EDITOR
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Real-time depth point cloud scanner for Quest 3
/// Based on Meta XR SDK EnvironmentDepthManager
/// Reference: https://developers.meta.com/horizon/documentation/native/android/mobile-depth/
/// </summary>
public class RealtimeDepthScanner : MonoBehaviour
{
    [Header("Point Cloud Settings")]
    [SerializeField] private int stride = 8; // Sample every 8th pixel for performance
    [SerializeField] private float minDepth = 0.2f;
    [SerializeField] private float maxDepth = 3.5f;
    [SerializeField] private int maxPoints = 3000;
    
    [Header("Visualization")]
    [SerializeField] private bool visualize = true;
    [SerializeField] private GameObject pointPrefab; // Assign a small sphere prefab
    [SerializeField] private float pointSize = 0.01f;
    [SerializeField] private Color pointColor = Color.cyan;
    [SerializeField] private float updateRate = 10f; // Updates per second
    
#if UNITY_ANDROID || UNITY_EDITOR
    private EnvironmentDepthManager depthManager;
    private GameObject pointCloudParent;
    private List<GameObject> pointPool = new List<GameObject>();
    private float lastUpdateTime;
    
    // Shader globals set by EnvironmentDepthManager
    private readonly int _DepthTexture = Shader.PropertyToID("_EnvironmentDepthTexture");
    private readonly int _ReprojectionMatrices = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
    
    void Start()
    {
        // Find or create EnvironmentDepthManager
        depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        
        if (depthManager == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                depthManager = rig.gameObject.AddComponent<EnvironmentDepthManager>();
            }
            else
            {
                GameObject obj = new GameObject("EnvironmentDepthManager");
                depthManager = obj.AddComponent<EnvironmentDepthManager>();
            }
        }
        
        // Enable for better quality
        depthManager.RemoveHands = true;
        
        // Create parent for point cloud
        pointCloudParent = new GameObject("PointCloud");
        
        // Create point pool if no prefab
        if (pointPrefab == null)
        {
            CreateDefaultPoints();
        }
    }
    
    void Update()
    {
        if (!depthManager || !depthManager.IsDepthAvailable)
            return;
        
        // Throttle updates
        if (Time.time - lastUpdateTime < 1f / updateRate)
            return;
        
        lastUpdateTime = Time.time;
        
        GeneratePointCloud();
    }
    
    void GeneratePointCloud()
    {
        // Get depth texture from shader global
        Texture depthTex = Shader.GetGlobalTexture(_DepthTexture);
        if (depthTex == null)
            return;
        
        // Get reprojection matrices
        Matrix4x4[] matrices = Shader.GetGlobalMatrixArray(_ReprojectionMatrices);
        if (matrices == null || matrices.Length == 0)
            return;
        
        // Use left eye matrix (index 0)
        Matrix4x4 reprojMatrix = matrices[0].inverse;
        
        // Convert to readable texture
        RenderTexture rt = depthTex as RenderTexture;
        if (rt == null || !rt.IsCreated())
            return;
        
        Texture2D tex = ReadDepthTexture(rt);
        if (tex == null)
            return;
        
        Color[] pixels = tex.GetPixels();
        int width = tex.width;
        int height = tex.height;
        
        // Hide all points first
        foreach (var pt in pointPool)
            pt.SetActive(false);
        
        int pointIndex = 0;
        Camera cam = Camera.main;
        if (cam == null)
        {
            Destroy(tex);
            return;
        }
        
        Vector3 camPos = cam.transform.position;
        
        // Sample depth texture
        for (int y = 0; y < height && pointIndex < maxPoints; y += stride)
        {
            for (int x = 0; x < width && pointIndex < maxPoints; x += stride)
            {
                int idx = y * width + x;
                if (idx >= pixels.Length)
                    continue;
                
                float depth = pixels[idx].r;
                
                // Skip invalid depth
                if (depth <= 0f || depth > 1f)
                    continue;
                
                // UV coordinates [0,1]
                float u = (float)x / width;
                float v = (float)y / height;
                
                // Transform to world space
                Vector3 worldPos = DepthToWorld(u, v, depth, reprojMatrix);
                
                // Validate
                if (!IsValidPosition(worldPos))
                    continue;
                
                // Distance filter
                float dist = Vector3.Distance(worldPos, camPos);
                if (dist < minDepth || dist > maxDepth)
                    continue;
                
                // Show point
                if (visualize && pointIndex < pointPool.Count)
                {
                    GameObject pt = pointPool[pointIndex];
                    pt.transform.position = worldPos;
                    pt.SetActive(true);
                    pointIndex++;
                }
            }
        }
        
        Destroy(tex);
    }
    
    Vector3 DepthToWorld(float u, float v, float depth, Matrix4x4 reproj)
    {
        // Screen [0,1] to Clip [-1,1]
        Vector3 screen = new Vector3(u, v, depth);
        Vector3 clip = screen * 2f - Vector3.one;
        
        // Homogeneous coordinates
        Vector4 homo = new Vector4(clip.x, clip.y, clip.z, 1f);
        
        // Transform to world
        Vector4 worldHomo = reproj * homo;
        
        // Perspective divide
        if (Mathf.Abs(worldHomo.w) < 0.0001f)
            return Vector3.zero;
        
        return new Vector3(
            worldHomo.x / worldHomo.w,
            worldHomo.y / worldHomo.w,
            worldHomo.z / worldHomo.w
        );
    }
    
    bool IsValidPosition(Vector3 pos)
    {
        return !float.IsNaN(pos.x) && !float.IsInfinity(pos.x) && pos != Vector3.zero;
    }
    
    Texture2D ReadDepthTexture(RenderTexture rt)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }
    
    void CreateDefaultPoints()
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = pointColor;
        
        for (int i = 0; i < maxPoints; i++)
        {
            GameObject pt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pt.transform.localScale = Vector3.one * pointSize;
            pt.transform.parent = pointCloudParent.transform;
            
            // Remove collider for performance
            Destroy(pt.GetComponent<Collider>());
            
            // Set material
            pt.GetComponent<Renderer>().material = mat;
            pt.SetActive(false);
            
            pointPool.Add(pt);
        }
    }
    
    void OnDestroy()
    {
        if (pointCloudParent != null)
            Destroy(pointCloudParent);
    }
#endif
}

