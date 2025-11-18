using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Point Cloud Visualizer - Visualizes depth data as 3D point cloud
/// Based on step2-depth-capture-implementation.md
/// </summary>
public class PointCloudVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthCaptureManager depthCapture;
    
    [Header("Visualization Settings")]
    [SerializeField] private Material pointMaterial;
    [SerializeField] private float pointSize = 0.01f;
    [SerializeField] private Color nearColor = Color.blue;
    [SerializeField] private Color farColor = Color.red;
    [SerializeField] private float colorDistanceRange = 5f;
    
    [Header("Performance")]
    [SerializeField] private int maxPoints = 10000;
    [SerializeField] private bool useMeshRendering = true; // Faster than individual spheres
    
    // Point cloud data
    private GameObject pointCloudContainer;
    private List<Vector3> currentPoints = new List<Vector3>();
    private Mesh pointCloudMesh;
    
    void Start()
    {
        // Find depth capture if not assigned
        if (depthCapture == null)
        {
            depthCapture = FindFirstObjectByType<DepthCaptureManager>();
        }
        
        // Create material if not assigned
        if (pointMaterial == null)
        {
            Shader pointShader = Shader.Find("Particles/Standard Unlit");
            if (pointShader == null)
                pointShader = Shader.Find("Standard");
            
            if (pointShader != null)
            {
                pointMaterial = new Material(pointShader);
                pointMaterial.SetFloat("_Mode", 3); // Transparent
                pointMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                pointMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                pointMaterial.SetInt("_ZWrite", 0);
                pointMaterial.EnableKeyword("_ALPHABLEND_ON");
                pointMaterial.color = Color.white;
            }
        }
        
        // Setup point cloud container
        SetupPointCloudContainer();
    }
    
    void SetupPointCloudContainer()
    {
        if (pointCloudContainer != null)
            Destroy(pointCloudContainer);
            
        pointCloudContainer = new GameObject("PointCloudVisualization");
        
        if (useMeshRendering)
        {
            // Use mesh for better performance
            MeshFilter mf = pointCloudContainer.AddComponent<MeshFilter>();
            MeshRenderer mr = pointCloudContainer.AddComponent<MeshRenderer>();
            mr.material = pointMaterial;
            
            pointCloudMesh = new Mesh();
            pointCloudMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support more points
            mf.mesh = pointCloudMesh;
        }
    }
    
    /// <summary>
    /// Visualize latest capture from DepthCaptureManager
    /// </summary>
    public void VisualizeLatestCapture()
    {
        if (depthCapture == null || !depthCapture.IsReady())
        {
            Debug.LogWarning("[PointCloudVisualizer] Depth capture not ready");
            return;
        }
        
        // Convert depth to points
        Vector3[] points = depthCapture.ConvertDepthToPointCloud();
        
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[PointCloudVisualizer] No points generated");
            return;
        }
        
        // Limit points for performance
        if (points.Length > maxPoints)
        {
            Vector3[] limitedPoints = new Vector3[maxPoints];
            int stride = points.Length / maxPoints;
            for (int i = 0; i < maxPoints; i++)
            {
                limitedPoints[i] = points[i * stride];
            }
            points = limitedPoints;
        }
        
        currentPoints.Clear();
        currentPoints.AddRange(points);
        
        // Visualize
        if (useMeshRendering)
        {
            UpdateMeshVisualization();
        }
        else
        {
            UpdateSphereVisualization();
        }
        
        Debug.Log($"[PointCloudVisualizer] ✅ Visualized {currentPoints.Count} points");
    }
    
    void UpdateMeshVisualization()
    {
        if (pointCloudMesh == null) return;
        
        pointCloudMesh.Clear();
        
        // Set vertices
        pointCloudMesh.vertices = currentPoints.ToArray();
        
        // Calculate colors based on distance
        Color[] colors = new Color[currentPoints.Count];
        Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        for (int i = 0; i < currentPoints.Count; i++)
        {
            float distance = Vector3.Distance(currentPoints[i], cameraPos);
            float t = Mathf.Clamp01(distance / colorDistanceRange);
            colors[i] = Color.Lerp(nearColor, farColor, t);
        }
        
        pointCloudMesh.colors = colors;
        
        // Set indices for point topology
        int[] indices = new int[currentPoints.Count];
        for (int i = 0; i < currentPoints.Count; i++)
        {
            indices[i] = i;
        }
        
        pointCloudMesh.SetIndices(indices, MeshTopology.Points, 0);
        pointCloudMesh.RecalculateBounds();
        
        // Set point size (requires shader support)
        if (pointMaterial != null)
        {
            pointMaterial.SetFloat("_PointSize", pointSize * 100f);
        }
    }
    
    void UpdateSphereVisualization()
    {
        // Clear previous
        if (pointCloudContainer != null)
        {
            foreach (Transform child in pointCloudContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        // Create spheres for each point
        foreach (Vector3 point in currentPoints)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = Vector3.one * pointSize;
            sphere.transform.parent = pointCloudContainer.transform;
            
            // Color by distance
            float distance = Vector3.Distance(point, cameraPos);
            float t = Mathf.Clamp01(distance / colorDistanceRange);
            Color color = Color.Lerp(nearColor, farColor, t);
            
            Renderer r = sphere.GetComponent<Renderer>();
            r.material = new Material(pointMaterial);
            r.material.color = color;
            
            // Remove collider for performance
            Destroy(sphere.GetComponent<Collider>());
        }
    }
    
    public void ClearVisualization()
    {
        if (pointCloudContainer != null)
        {
            foreach (Transform child in pointCloudContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (pointCloudMesh != null)
        {
            pointCloudMesh.Clear();
        }
        
        currentPoints.Clear();
        
        Debug.Log("[PointCloudVisualizer] Cleared visualization");
    }
    
    public int GetPointCount() => currentPoints.Count;
}
