using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Helper class for table scanning using MRUK built-in system.
/// Tracks scanning progress and guides user to multiple angles.
/// </summary>
public class TableScanHelper : MonoBehaviour
{
    [Header("Scanning Settings")]
    [SerializeField, Range(4, 16)] private int targetViewCount = 8;
    [SerializeField, Min(0.1f)] private float minViewDistance = 0.3f; // meters
    [SerializeField, Min(0.5f)] private float maxViewDistance = 0.8f; // meters
    [SerializeField, Range(0f, 1f)] private float minCoverageThreshold = 0.7f; // 70% coverage required
    
    [Header("Visualization")]
    [SerializeField] private Material scanIndicatorMaterial;
    [SerializeField] private GameObject coverageVisualizationPrefab;
    
    private MRUKAnchor _currentTableAnchor;
    private List<Vector3> _recommendedPositions = new List<Vector3>();
    private HashSet<int> _capturedViewIndices = new HashSet<int>();
    private float _currentCoverage = 0f;
    private bool _isScanning = false;
    
    // Coverage tracking
    private Dictionary<Vector2Int, bool> _coverageGrid = new Dictionary<Vector2Int, bool>();
    private int _gridResolution = 20; // 20x20 grid for coverage tracking
    
    public MRUKAnchor CurrentTableAnchor => _currentTableAnchor;
    public bool IsScanning => _isScanning;
    public float CoveragePercentage => _currentCoverage;
    public int CapturedViewCount => _capturedViewIndices.Count;
    public int TargetViewCount => targetViewCount;
    public List<Vector3> RecommendedPositions => _recommendedPositions;
    
    private void OnEnable()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnMrukSceneLoaded);
        }
    }
    
    private void OnDisable()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnMrukSceneLoaded);
        }
    }
    
    private void OnMrukSceneLoaded()
    {
        Debug.Log("[TableScanHelper] MRUK scene loaded. Ready for table scanning.");
    }
    
    /// <summary>
    /// Initialize scanning for a specific table anchor
    /// </summary>
    public bool InitializeScan(MRUKAnchor tableAnchor)
    {
        if (tableAnchor == null || !tableAnchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
        {
            Debug.LogWarning("[TableScanHelper] Invalid table anchor provided.");
            return false;
        }
        
        _currentTableAnchor = tableAnchor;
        _capturedViewIndices.Clear();
        _coverageGrid.Clear();
        _currentCoverage = 0f;
        
        CalculateRecommendedPositions();
        InitializeCoverageGrid();
        
        Debug.Log($"[TableScanHelper] Initialized scan for table: {tableAnchor.name}");
        return true;
    }
    
    /// <summary>
    /// Calculate recommended camera positions around the table
    /// </summary>
    private void CalculateRecommendedPositions()
    {
        _recommendedPositions.Clear();
        
        if (_currentTableAnchor == null || !_currentTableAnchor.VolumeBounds.HasValue)
            return;
        
        Bounds tableBounds = _currentTableAnchor.VolumeBounds.Value;
        Vector3 tableCenter = tableBounds.center;
        Vector3 tableSize = tableBounds.size;
        
        // Calculate radius based on table size (ensure we're at good viewing distance)
        float radius = Mathf.Max(tableSize.x, tableSize.z) * 0.7f + 0.4f;
        radius = Mathf.Clamp(radius, minViewDistance, maxViewDistance);
        
        // Generate positions in a circle around the table
        for (int i = 0; i < targetViewCount; i++)
        {
            float angle = (360f / targetViewCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * radius,
                0f,
                Mathf.Sin(rad) * radius
            );
            
            // Position at table height + eye level offset (approximately 1.6m above table)
            Vector3 position = tableCenter + offset;
            position.y = tableCenter.y + 1.6f; // Eye level above table
            
            _recommendedPositions.Add(position);
        }
    }
    
    /// <summary>
    /// Initialize coverage grid for tracking scanned areas
    /// </summary>
    private void InitializeCoverageGrid()
    {
        _coverageGrid.Clear();
        
        if (_currentTableAnchor == null || !_currentTableAnchor.VolumeBounds.HasValue)
            return;
        
        Bounds tableBounds = _currentTableAnchor.VolumeBounds.Value;
        
        for (int x = 0; x < _gridResolution; x++)
        {
            for (int z = 0; z < _gridResolution; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                _coverageGrid[gridPos] = false;
            }
        }
    }
    
    /// <summary>
    /// Update coverage based on current camera position and view direction
    /// </summary>
    public void UpdateCoverage(Vector3 cameraPosition, Vector3 viewDirection)
    {
        if (_currentTableAnchor == null || !_currentTableAnchor.VolumeBounds.HasValue)
            return;
        
        Bounds tableBounds = _currentTableAnchor.VolumeBounds.Value;
        
        // Project view direction onto table surface
        // Mark grid cells that are visible from this position
        int visibleCells = 0;
        
        for (int x = 0; x < _gridResolution; x++)
        {
            for (int z = 0; z < _gridResolution; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                
                // Calculate world position of this grid cell
                float normalizedX = (float)x / (_gridResolution - 1);
                float normalizedZ = (float)z / (_gridResolution - 1);
                
                Vector3 cellWorldPos = new Vector3(
                    tableBounds.min.x + normalizedX * tableBounds.size.x,
                    tableBounds.center.y + tableBounds.extents.y,
                    tableBounds.min.z + normalizedZ * tableBounds.size.z
                );
                
                // Check if this cell is visible from camera position
                Vector3 toCell = (cellWorldPos - cameraPosition).normalized;
                float dot = Vector3.Dot(viewDirection.normalized, toCell);
                
                if (dot > 0.7f) // Within ~45 degree cone
                {
                    float distance = Vector3.Distance(cameraPosition, cellWorldPos);
                    if (distance >= minViewDistance && distance <= maxViewDistance)
                    {
                        _coverageGrid[gridPos] = true;
                        visibleCells++;
                    }
                }
            }
        }
        
        // Calculate coverage percentage
        int totalScanned = 0;
        foreach (var kvp in _coverageGrid)
        {
            if (kvp.Value) totalScanned++;
        }
        
        _currentCoverage = (float)totalScanned / _coverageGrid.Count;
    }
    
    /// <summary>
    /// Get the index of the nearest recommended position to current camera position
    /// </summary>
    public int GetNearestRecommendedPositionIndex(Vector3 cameraPosition)
    {
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;
        
        for (int i = 0; i < _recommendedPositions.Count; i++)
        {
            float distance = Vector3.Distance(cameraPosition, _recommendedPositions[i]);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
    
    /// <summary>
    /// Check if a view should be captured at current position
    /// </summary>
    public bool ShouldCaptureView(Vector3 cameraPosition, out int recommendedIndex)
    {
        recommendedIndex = GetNearestRecommendedPositionIndex(cameraPosition);
        
        if (recommendedIndex == -1)
            return false;
        
        // Check if we've already captured this position
        if (_capturedViewIndices.Contains(recommendedIndex))
            return false;
        
        // Check if we're close enough to recommended position
        float distance = Vector3.Distance(cameraPosition, _recommendedPositions[recommendedIndex]);
        return distance < 0.2f; // Within 20cm of recommended position
    }
    
    /// <summary>
    /// Mark a view as captured
    /// </summary>
    public void MarkViewCaptured(int viewIndex)
    {
        _capturedViewIndices.Add(viewIndex);
        Debug.Log($"[TableScanHelper] View {viewIndex + 1}/{targetViewCount} captured.");
    }
    
    /// <summary>
    /// Check if scanning is complete
    /// </summary>
    public bool IsScanComplete()
    {
        return _capturedViewIndices.Count >= targetViewCount && _currentCoverage >= minCoverageThreshold;
    }
    
    /// <summary>
    /// Start scanning mode
    /// </summary>
    public void StartScanning()
    {
        _isScanning = true;
        Debug.Log("[TableScanHelper] Scanning started.");
    }
    
    /// <summary>
    /// Stop scanning mode
    /// </summary>
    public void StopScanning()
    {
        _isScanning = false;
        Debug.Log("[TableScanHelper] Scanning stopped.");
    }
    
    /// <summary>
    /// Get all detected table anchors from MRUK
    /// </summary>
    public static List<MRUKAnchor> GetAvailableTables()
    {
        List<MRUKAnchor> tables = new List<MRUKAnchor>();
        
        if (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            return tables;
        
        var room = MRUK.Instance.GetCurrentRoom();
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                tables.Add(anchor);
            }
        }
        
        return tables;
    }
}

