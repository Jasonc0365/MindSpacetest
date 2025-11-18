using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Filters depth points to only those on/above the table surface.
/// STEP 3: Table Surface Filter - Following roadmap implementation.
/// </summary>
public class TablePointFilter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TableHighlighter tableHighlighter; // Optional: use existing table highlighter
    [SerializeField] private bool autoFindTable = true; // Automatically find closest table
    
    [Header("Filter Settings")]
    [SerializeField, Min(0.01f)] private float minHeightAboveTable = 0.01f; // 1cm above table
    [SerializeField, Min(0.1f)] private float maxHeightAboveTable = 0.5f;  // 50cm above table
    [SerializeField, Min(0f)] private float tableBoundsMargin = 0.05f;   // 5cm margin around table
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool visualizeFilterBounds = false;
    
    private MRUKAnchor targetTable;
    private GameObject boundsVisualization;
    
    public MRUKAnchor TargetTable => targetTable;
    public bool HasTargetTable => targetTable != null;
    
    void Start()
    {
        // Auto-find table highlighter if not assigned
        if (tableHighlighter == null)
        {
            tableHighlighter = FindFirstObjectByType<TableHighlighter>();
        }
        
        // Auto-find table if enabled
        if (autoFindTable)
        {
            FindClosestTable();
        }
    }
    
    /// <summary>
    /// Find the closest table to the camera/player
    /// </summary>
    public void FindClosestTable()
    {
        if (MRUK.Instance == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[TablePointFilter] MRUK.Instance is null. Wait for scene to load.");
            return;
        }
        
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[TablePointFilter] No MRUK room found.");
            return;
        }
        
        // Get camera position (or player position)
        Vector3 searchPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        MRUKAnchor closestTable = null;
        float minDistance = float.MaxValue;
        
        // Find all table anchors
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) && anchor.VolumeBounds.HasValue)
            {
                float distance = Vector3.Distance(searchPosition, anchor.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTable = anchor;
                }
            }
        }
        
        if (closestTable != null)
        {
            SetTargetTable(closestTable);
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("[TablePointFilter] No table found in scene.");
        }
    }
    
    /// <summary>
    /// Set the target table for filtering
    /// </summary>
    public void SetTargetTable(MRUKAnchor table)
    {
        if (table == null)
        {
            Debug.LogWarning("[TablePointFilter] Cannot set null table");
            return;
        }
        
        if (!table.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
        {
            Debug.LogWarning($"[TablePointFilter] Anchor {table.name} is not a table");
            return;
        }
        
        if (!table.VolumeBounds.HasValue)
        {
            Debug.LogWarning($"[TablePointFilter] Table {table.name} has no volume bounds");
            return;
        }
        
        targetTable = table;
        
        if (showDebugInfo)
        {
            Bounds bounds = table.VolumeBounds.Value;
            Debug.Log($"[TablePointFilter] ✅ Target table set: {table.name}");
            Debug.Log($"[TablePointFilter]   Position: {table.transform.position}");
            Debug.Log($"[TablePointFilter]   Size: {bounds.size}");
            Debug.Log($"[TablePointFilter]   Center: {bounds.center}");
        }
        
        // Update visualization
        if (visualizeFilterBounds)
        {
            UpdateBoundsVisualization();
        }
    }
    
    /// <summary>
    /// Filter points to only those on/above the table surface
    /// </summary>
    public Vector3[] FilterPointsOnTable(Vector3[] allPoints)
    {
        if (targetTable == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[TablePointFilter] No target table set! Use SetTargetTable() or enable autoFindTable");
            return new Vector3[0];
        }
        
        if (!targetTable.VolumeBounds.HasValue)
        {
            Debug.LogWarning("[TablePointFilter] Target table has no volume bounds");
            return new Vector3[0];
        }
        
        List<Vector3> filteredPoints = new List<Vector3>();
        
        // Get table info in world space
        Bounds tableBounds = targetTable.VolumeBounds.Value;
        Vector3 tableCenter = targetTable.transform.TransformPoint(tableBounds.center);
        Vector3 tableSize = targetTable.transform.TransformVector(tableBounds.size);
        
        // Calculate table top height (highest point of table)
        // The table bounds center is in local space, so we need to transform it
        float tableTopHeight = tableCenter.y + (tableSize.y / 2f);
        
        // Define table bounds in XZ plane (with margin)
        float tableWidth = tableSize.x + (tableBoundsMargin * 2f);
        float tableDepth = tableSize.z + (tableBoundsMargin * 2f);
        
        // Filter points
        int filteredCount = 0;
        foreach (Vector3 point in allPoints)
        {
            // Check if point is within height range (above table surface)
            float heightAboveTable = point.y - tableTopHeight;
            
            if (heightAboveTable < minHeightAboveTable || heightAboveTable > maxHeightAboveTable)
                continue;
            
            // Check if point is within table XZ bounds
            // Calculate distance from table center in XZ plane
            Vector3 pointXZ = new Vector3(point.x, tableTopHeight, point.z);
            Vector3 tableCenterXZ = new Vector3(tableCenter.x, tableTopHeight, tableCenter.z);
            
            // Check if point is within table bounds (accounting for rotation)
            // Transform point to table's local space for easier bounds checking
            Vector3 pointLocal = targetTable.transform.InverseTransformPoint(point);
            Vector3 pointLocalXZ = new Vector3(pointLocal.x, 0, pointLocal.z);
            
            // Check bounds in local space (simpler)
            float halfWidth = (tableBounds.size.x / 2f) + tableBoundsMargin;
            float halfDepth = (tableBounds.size.z / 2f) + tableBoundsMargin;
            
            if (Mathf.Abs(pointLocalXZ.x) <= halfWidth && Mathf.Abs(pointLocalXZ.z) <= halfDepth)
            {
                filteredPoints.Add(point);
                filteredCount++;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[TablePointFilter] ✅ Filtered {filteredPoints.Count} points on table from {allPoints.Length} total");
            Debug.Log($"[TablePointFilter]   Table top height: {tableTopHeight:F3}m");
            Debug.Log($"[TablePointFilter]   Height range: {minHeightAboveTable:F3}m to {maxHeightAboveTable:F3}m above table");
        }
        
        return filteredPoints.ToArray();
    }
    
    /// <summary>
    /// Get table surface position (center of table top)
    /// </summary>
    public Vector3 GetTableSurfacePosition()
    {
        if (targetTable == null || !targetTable.VolumeBounds.HasValue)
            return Vector3.zero;
        
        Bounds tableBounds = targetTable.VolumeBounds.Value;
        Vector3 tableCenter = targetTable.transform.TransformPoint(tableBounds.center);
        Vector3 tableSize = targetTable.transform.TransformVector(tableBounds.size);
        float tableTopHeight = tableCenter.y + (tableSize.y / 2f);
        
        return new Vector3(tableCenter.x, tableTopHeight, tableCenter.z);
    }
    
    /// <summary>
    /// Get table top height in world space
    /// </summary>
    public float GetTableTopHeight()
    {
        if (targetTable == null || !targetTable.VolumeBounds.HasValue)
            return 0f;
        
        Bounds tableBounds = targetTable.VolumeBounds.Value;
        Vector3 tableCenter = targetTable.transform.TransformPoint(tableBounds.center);
        Vector3 tableSize = targetTable.transform.TransformVector(tableBounds.size);
        
        return tableCenter.y + (tableSize.y / 2f);
    }
    
    /// <summary>
    /// Visualize the filter bounds (for debugging)
    /// </summary>
    void UpdateBoundsVisualization()
    {
        if (!visualizeFilterBounds || targetTable == null || !targetTable.VolumeBounds.HasValue)
        {
            if (boundsVisualization != null)
            {
                Destroy(boundsVisualization);
            }
            return;
        }
        
        // Clear previous
        if (boundsVisualization != null)
        {
            Destroy(boundsVisualization);
        }
        
        Bounds tableBounds = targetTable.VolumeBounds.Value;
        Vector3 tableCenter = targetTable.transform.TransformPoint(tableBounds.center);
        Vector3 tableSize = targetTable.transform.TransformVector(tableBounds.size);
        float tableTopHeight = tableCenter.y + (tableSize.y / 2f);
        
        // Create visualization box
        boundsVisualization = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsVisualization.name = "TableFilterBounds";
        
        // Position at table top
        boundsVisualization.transform.position = new Vector3(tableCenter.x, tableTopHeight + (maxHeightAboveTable / 2f), tableCenter.z);
        boundsVisualization.transform.rotation = targetTable.transform.rotation;
        
        // Size includes margin
        boundsVisualization.transform.localScale = new Vector3(
            tableSize.x + (tableBoundsMargin * 2f),
            maxHeightAboveTable,
            tableSize.z + (tableBoundsMargin * 2f)
        );
        
        // Make it transparent wireframe
        Renderer r = boundsVisualization.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 1f, 0f, 0.2f); // Yellow transparent
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        r.material = mat;
        
        // Remove collider
        Destroy(boundsVisualization.GetComponent<Collider>());
    }
    
    void OnDestroy()
    {
        if (boundsVisualization != null)
        {
            Destroy(boundsVisualization);
        }
    }
}

