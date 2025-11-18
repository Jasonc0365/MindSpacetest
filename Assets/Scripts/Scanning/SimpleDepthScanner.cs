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
            Debug.LogWarning("[SimpleDepthScanner] Depth not ready");
            return;
        }
        
        ClearPointCloud();
        
        Vector3[] points = GeneratePointCloud();
        
        if (points != null && points.Length > 0)
        {
            Debug.Log($"[SimpleDepthScanner] Scan complete: {points.Length} points");
            VisualizePoints(points);
        }
        else
        {
            Debug.LogWarning("[SimpleDepthScanner] No points generated");
        }
#endif
    }
    
    Vector3[] GeneratePointCloud()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Get depth texture and reprojection matrices
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        
        if (depthTexGlobal == null || reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            return null;
        }
        
        // Use first eye's inverse reprojection matrix
        Matrix4x4 reprojectionMatrix = reprojectionMatrices[0].inverse;
        
        // Convert to Texture2D
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
        Camera mainCamera = Camera.main;
        Vector3 cameraPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        
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
                
                float depth = pixels[pixelIndex].r;
                
                if (depth <= 0.0f || depth > 1.0f)
                    continue;
                
                float u = (float)x / width;
                float v = (float)y / height;
                
                Vector3 worldPos = DepthToWorldSpace(u, v, depth, reprojectionMatrix);
                
                if (float.IsNaN(worldPos.x) || float.IsInfinity(worldPos.x) || worldPos == Vector3.zero)
                    continue;
                
                float distance = Vector3.Distance(worldPos, cameraPos);
                
                if (distance < minDepth || distance > maxDepth)
                    continue;
                
                points.Add(worldPos);
            }
            
            if (points.Count >= maxPoints)
                break;
        }
        
        Destroy(depthTexture);
        return points.ToArray();
#else
        return null;
#endif
    }
    
    Vector3 DepthToWorldSpace(float u, float v, float depth, Matrix4x4 reprojectionMatrix)
    {
        // Screen space [0,1] → Clip space [-1,1]
        Vector3 screenPos = new Vector3(u, v, depth);
        Vector3 clipPos = screenPos * 2.0f - Vector3.one;
        
        // Homogeneous clip space
        Vector4 homogeneousClip = new Vector4(clipPos.x, clipPos.y, clipPos.z, 1.0f);
        
        // Transform to homogeneous world space
        Vector4 homogeneousWorld = reprojectionMatrix * homogeneousClip;
        
        // Perspective divide
        if (Mathf.Abs(homogeneousWorld.w) < 0.0001f)
        {
            return Vector3.zero;
        }
        
        return new Vector3(
            homogeneousWorld.x / homogeneousWorld.w,
            homogeneousWorld.y / homogeneousWorld.w,
            homogeneousWorld.z / homogeneousWorld.w
        );
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
