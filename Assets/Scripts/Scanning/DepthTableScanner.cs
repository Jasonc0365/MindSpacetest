using UnityEngine;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Combined Point Cloud + Table Filtering
/// Based on depth-scanning-implementation.md guide - Step 3
/// </summary>
public class DepthTableScanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthPointCloudGenerator pointCloudGen;
    [SerializeField] private EnvironmentDepthManager depthManager;
    
    [Header("Filtering")]
    [SerializeField] private float minHeightAboveTable = 0.01f;
    [SerializeField] private float maxHeightAboveTable = 0.5f;
    [SerializeField] private float tableBoundsMargin = 0.05f;
    
    [Header("Visualization")]
    [SerializeField] private bool visualizeFilteredPoints = true;
    [SerializeField] private Material filteredPointMaterial;
    [SerializeField] private float filteredPointSize = 0.01f;
    
    private MRUKAnchor targetTable;
    private List<Vector3> filteredPoints = new List<Vector3>();
    private GameObject filteredPointsContainer;
    
    void Start()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (pointCloudGen == null)
        {
            pointCloudGen = FindFirstObjectByType<DepthPointCloudGenerator>();
        }
        
        if (depthManager == null)
        {
            depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        }
#else
        Debug.LogWarning("[DepthTableScanner] Depth API not supported on this platform");
#endif
    }
    
    /// <summary>
    /// Scan table objects - main entry point
    /// </summary>
    public void ScanTableObjects()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Step 1: Get closest table
        targetTable = GetClosestTable(Camera.main.transform.position);
        
        if (targetTable == null)
        {
            Debug.LogError("[DepthTableScanner] No table found!");
            return;
        }
        
        Debug.Log($"[DepthTableScanner] Scanning table: {targetTable.name}");
        
        // Step 2: Generate point cloud
        Vector3[] allPoints = pointCloudGen != null ? pointCloudGen.GetPointCloud() : null;
        
        if (allPoints == null || allPoints.Length == 0)
        {
            // Generate point cloud if not already generated
            allPoints = pointCloudGen != null ? pointCloudGen.GeneratePointCloud() : null;
        }
        
        if (allPoints == null || allPoints.Length == 0)
        {
            Debug.LogWarning("[DepthTableScanner] No points generated!");
            return;
        }
        
        // Step 3: Filter to table surface
        FilterPointsOnTable(allPoints);
        
        Debug.Log($"[DepthTableScanner] Filtered {filteredPoints.Count} points on table");
        
        // Step 4: Visualize filtered points
        if (visualizeFilteredPoints)
        {
            VisualizeFilteredPoints();
        }
#else
        Debug.LogWarning("[DepthTableScanner] Depth API not supported on this platform");
#endif
    }
    
    /// <summary>
    /// Get closest table to position
    /// </summary>
    MRUKAnchor GetClosestTable(Vector3 position)
    {
        if (MRUK.Instance == null)
        {
            Debug.LogWarning("[DepthTableScanner] MRUK.Instance is null");
            return null;
        }
        
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogWarning("[DepthTableScanner] No MRUK room found");
            return null;
        }
        
        MRUKAnchor closestTable = null;
        float minDistance = float.MaxValue;
        
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) && anchor.VolumeBounds.HasValue)
            {
                float distance = Vector3.Distance(position, anchor.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTable = anchor;
                }
            }
        }
        
        return closestTable;
    }
    
    void FilterPointsOnTable(Vector3[] allPoints)
    {
        filteredPoints.Clear();
        
        if (targetTable == null || !targetTable.VolumeBounds.HasValue)
        {
            Debug.LogWarning("[DepthTableScanner] Invalid target table");
            return;
        }
        
        // Get table information
        Vector3 tableCenter = targetTable.transform.position;
        Bounds tableBounds = targetTable.VolumeBounds.Value;
        float tableHeight = tableBounds.center.y + tableBounds.extents.y;
        
        // Define table bounds in XZ plane
        Bounds tableXZBounds = new Bounds(
            new Vector3(tableCenter.x, tableHeight, tableCenter.z),
            new Vector3(
                tableBounds.size.x + tableBoundsMargin * 2, 
                0.01f, 
                tableBounds.size.z + tableBoundsMargin * 2
            )
        );
        
        // Filter points
        foreach (Vector3 point in allPoints)
        {
            // Check height above table
            float heightAboveTable = point.y - tableHeight;
            
            if (heightAboveTable < minHeightAboveTable || heightAboveTable > maxHeightAboveTable)
                continue;
            
            // Check if point is within table XZ bounds
            Vector3 pointXZ = new Vector3(point.x, tableHeight, point.z);
            
            if (tableXZBounds.Contains(pointXZ))
            {
                filteredPoints.Add(point);
            }
        }
    }
    
    void VisualizeFilteredPoints()
    {
        // Clear previous visualization
        if (filteredPointsContainer != null)
            Destroy(filteredPointsContainer);
        
        filteredPointsContainer = new GameObject("FilteredTablePoints");
        
        // Create material if not assigned
        Material mat = filteredPointMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader != null)
            {
                mat = new Material(shader);
                mat.color = Color.yellow;
            }
        }
        
        // Create spheres for each filtered point
        foreach (Vector3 point in filteredPoints)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = Vector3.one * filteredPointSize;
            sphere.transform.parent = filteredPointsContainer.transform;
            
            Renderer r = sphere.GetComponent<Renderer>();
            if (mat != null)
            {
                r.material = mat;
            }
            else
            {
                r.material.color = Color.yellow;
            }
            
            Destroy(sphere.GetComponent<Collider>());
        }
    }
    
    /// <summary>
    /// Get filtered points for further processing
    /// </summary>
    public List<Vector3> GetFilteredPoints()
    {
        return filteredPoints;
    }
    
    /// <summary>
    /// Get target table anchor
    /// </summary>
    public MRUKAnchor GetTargetTable()
    {
        return targetTable;
    }
    
    /// <summary>
    /// Set target table manually
    /// </summary>
    public void SetTargetTable(MRUKAnchor table)
    {
        if (table != null && table.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
        {
            targetTable = table;
        }
    }
}

