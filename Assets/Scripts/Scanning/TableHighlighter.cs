using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Custom table highlighter that replaces Oculus EffectMesh.
/// Generates world-locked highlighting for MRUK table surfaces.
/// </summary>
public class TableHighlighter : MonoBehaviour
{
    [Header("Appearance Settings")]
    [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.3f); // Green transparent
    [SerializeField, Min(0f)] private float heightOffsetMeters = 0.005f; // Lift above surface to avoid z-fighting
    
    [Header("Material")]
    [SerializeField] private Material tableHighlightMaterial;
    
    [Header("Debug/Testing")]
    [SerializeField] private bool createOnStart = true; // Auto-create when MRUK loads
    [SerializeField] private float retryInterval = 1f; // Check every second for MRUK initialization
    
    private Dictionary<MRUKAnchor, GameObject> _highlightedTables = new Dictionary<MRUKAnchor, GameObject>();
    private float _nextRetryTime = 0f;
    private bool _isSubscribed = false;
    
    public Dictionary<MRUKAnchor, GameObject> HighlightedTables => _highlightedTables;
    
    private void OnEnable()
    {
        TrySubscribeToMRUK();
    }
    
    private void Start()
    {
        // Try to subscribe and create highlights on start
        TrySubscribeToMRUK();
        if (createOnStart)
        {
            TryCreateHighlightsIfReady();
        }
    }
    
    private void Update()
    {
        // Periodically retry if MRUK isn't ready yet
        if (!_isSubscribed && Time.time >= _nextRetryTime)
        {
            _nextRetryTime = Time.time + retryInterval;
            TrySubscribeToMRUK();
            
            // Also try to create highlights if MRUK is now ready
            if (createOnStart && _isSubscribed)
            {
                TryCreateHighlightsIfReady();
            }
        }
    }
    
    private void TrySubscribeToMRUK()
    {
        if (MRUK.Instance != null && !_isSubscribed)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnMrukSceneLoaded);
            _isSubscribed = true;
            
            // Check if scene is already loaded
            if (MRUK.Instance.GetCurrentRoom() != null)
            {
                OnMrukSceneLoaded();
            }
        }
    }
    
    private void TryCreateHighlightsIfReady()
    {
        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            CreateTableHighlights();
        }
    }
    
    private void OnDisable()
    {
        if (MRUK.Instance != null && _isSubscribed)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnMrukSceneLoaded);
            _isSubscribed = false;
        }
        
        ClearAllHighlights();
    }
    
    private void OnMrukSceneLoaded()
    {
        CreateTableHighlights();
    }
    
    /// <summary>
    /// Manually trigger table highlight creation (useful for testing/debugging)
    /// </summary>
    [ContextMenu("Create Table Highlights")]
    public void ManualCreateHighlights()
    {
        CreateTableHighlights();
    }
    
    /// <summary>
    /// Create highlights for all detected tables
    /// </summary>
    public void CreateTableHighlights()
    {
        if (MRUK.Instance == null)
        {
            return;
        }
        
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            return;
        }
        
        // Clear existing highlights
        ClearAllHighlights();
        
        // Find all table anchors
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                CreateTableHighlight(anchor);
            }
        }
    }
    
    /// <summary>
    /// Create highlight for a specific table anchor
    /// </summary>
    public void CreateTableHighlight(MRUKAnchor tableAnchor)
    {
        if (tableAnchor == null)
        {
            return;
        }
        
        if (!tableAnchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
        {
            return;
        }
        
        if (_highlightedTables.ContainsKey(tableAnchor))
        {
            return;
        }
        
        if (!tableAnchor.VolumeBounds.HasValue)
        {
            return;
        }
        
        // Get table bounds early (needed for collider setup)
        Bounds tableBounds = tableAnchor.VolumeBounds.Value;
        
        // Create highlight GameObject
        GameObject highlightGO = new GameObject($"TableHighlight_{tableAnchor.name}");
        
        // Parent to anchor transform for world-locked positioning
        // Using false means localPosition/rotation are preserved (we want local space)
        highlightGO.transform.SetParent(tableAnchor.transform, false);
        
        // Ensure local transform is identity (mesh vertices are already in local space)
        highlightGO.transform.localPosition = Vector3.zero;
        highlightGO.transform.localRotation = Quaternion.identity;
        highlightGO.transform.localScale = Vector3.one;
        
        // Generate table top mesh - pass anchor to determine correct top surface
        Mesh tableTopMesh = GenerateTableTopMesh(tableBounds, tableAnchor.transform);
        
        // Add MeshFilter
        MeshFilter meshFilter = highlightGO.AddComponent<MeshFilter>();
        meshFilter.mesh = tableTopMesh;
        
        // Add MeshRenderer
        MeshRenderer meshRenderer = highlightGO.AddComponent<MeshRenderer>();
        
        // Create or use material
        Material material = GetOrCreateMaterial();
        if (material == null)
        {
            Destroy(highlightGO);
            return;
        }
        
        meshRenderer.material = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true; // Ensure renderer is enabled
        
        // Make sure mesh is visible and not culled
        // Set layer to default or a visible layer
        highlightGO.layer = 0; // Default layer
        
        // Add BoxCollider for raycasting (used by ControllerHighlighter)
        // The collider should match the table bounds in local space
        // Note: Collider is for raycasting only, doesn't affect mesh visibility
        BoxCollider boxCollider = highlightGO.AddComponent<BoxCollider>();
        boxCollider.size = tableBounds.size;
        boxCollider.center = tableBounds.center;
        boxCollider.isTrigger = false; // Not a trigger, so raycasts can hit it
        
        // Store reference
        _highlightedTables[tableAnchor] = highlightGO;
    }
    
    /// <summary>
    /// Generate a quad mesh for the table top surface
    /// Uses the anchor's transform to determine which face is the "top" surface
    /// </summary>
    private Mesh GenerateTableTopMesh(Bounds tableBounds, Transform anchorTransform)
    {
        Mesh mesh = new Mesh();
        mesh.name = "TableTopSurface";
        
        // Bounds are in local space relative to anchor transform
        // The anchor may be rotated, so we need to find which face of the bounds
        // corresponds to the world-space "up" direction
        
        // Get the anchor's "up" direction in world space, then transform to local space
        // This tells us which local axis points "up" in world space
        Vector3 worldUp = Vector3.up;
        Vector3 localUpDirection = anchorTransform.InverseTransformDirection(worldUp);
        
        // Find which axis (X, Y, or Z) is most aligned with the "up" direction
        // The component with the largest absolute value indicates the dominant axis
        float absX = Mathf.Abs(localUpDirection.x);
        float absY = Mathf.Abs(localUpDirection.y);
        float absZ = Mathf.Abs(localUpDirection.z);
        
        // Determine which axis to use and whether to use min or max
        Vector3[] vertices;
        Vector3 normal;
        float topValue;
        float minAxis1, maxAxis1, minAxis2, maxAxis2;
        
        if (absY >= absX && absY >= absZ)
        {
            // Y axis is the "up" direction - use Y for top surface
            topValue = localUpDirection.y > 0 ? tableBounds.max.y : tableBounds.min.y;
            topValue += (localUpDirection.y > 0 ? 1 : -1) * heightOffsetMeters;
            minAxis1 = tableBounds.min.x;
            maxAxis1 = tableBounds.max.x;
            minAxis2 = tableBounds.min.z;
            maxAxis2 = tableBounds.max.z;
            normal = localUpDirection.y > 0 ? Vector3.up : Vector3.down;
            
            vertices = new Vector3[4]
            {
                new Vector3(minAxis1, topValue, minAxis2),
                new Vector3(maxAxis1, topValue, minAxis2),
                new Vector3(minAxis1, topValue, maxAxis2),
                new Vector3(maxAxis1, topValue, maxAxis2)
            };
        }
        else if (absX >= absZ)
        {
            // X axis is the "up" direction
            topValue = localUpDirection.x > 0 ? tableBounds.max.x : tableBounds.min.x;
            topValue += (localUpDirection.x > 0 ? 1 : -1) * heightOffsetMeters;
            minAxis1 = tableBounds.min.y;
            maxAxis1 = tableBounds.max.y;
            minAxis2 = tableBounds.min.z;
            maxAxis2 = tableBounds.max.z;
            normal = localUpDirection.x > 0 ? Vector3.right : Vector3.left;
            
            vertices = new Vector3[4]
            {
                new Vector3(topValue, minAxis1, minAxis2),
                new Vector3(topValue, maxAxis1, minAxis2),
                new Vector3(topValue, minAxis1, maxAxis2),
                new Vector3(topValue, maxAxis1, maxAxis2)
            };
        }
        else
        {
            // Z axis is the "up" direction
            topValue = localUpDirection.z > 0 ? tableBounds.max.z : tableBounds.min.z;
            topValue += (localUpDirection.z > 0 ? 1 : -1) * heightOffsetMeters;
            minAxis1 = tableBounds.min.x;
            maxAxis1 = tableBounds.max.x;
            minAxis2 = tableBounds.min.y;
            maxAxis2 = tableBounds.max.y;
            normal = localUpDirection.z > 0 ? Vector3.forward : Vector3.back;
            
            vertices = new Vector3[4]
            {
                new Vector3(minAxis1, minAxis2, topValue),
                new Vector3(maxAxis1, minAxis2, topValue),
                new Vector3(minAxis1, maxAxis2, topValue),
                new Vector3(maxAxis1, maxAxis2, topValue)
            };
        }
        
        // Create triangles (2 triangles for quad)
        // Winding order: counter-clockwise when viewed from the normal direction
        int[] triangles = new int[6];
        if (Vector3.Dot(normal, Vector3.up) > 0 || Vector3.Dot(normal, Vector3.right) > 0 || Vector3.Dot(normal, Vector3.forward) > 0)
        {
            // Normal points in positive direction - counter-clockwise winding
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 2; triangles[4] = 3; triangles[5] = 1;
        }
        else
        {
            // Normal points in negative direction - clockwise winding
            triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
            triangles[3] = 2; triangles[4] = 1; triangles[5] = 3;
        }
        
        // Create UVs
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // Create normals (all pointing in the normal direction)
        Vector3[] normals = new Vector3[4] { normal, normal, normal, normal };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        
        return mesh;
    }
    
    /// <summary>
    /// Get or create the highlight material
    /// </summary>
    private Material GetOrCreateMaterial()
    {
        Material material;
        
        if (tableHighlightMaterial != null)
        {
            // Create instance to avoid modifying original
            material = new Material(tableHighlightMaterial);
            material.color = highlightColor;
            return material;
        }
        
        // Use Standard shader with transparency
        material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = highlightColor;
        
        return material;
    }
    
    /// <summary>
    /// Remove highlight for a specific table
    /// </summary>
    public void RemoveTableHighlight(MRUKAnchor tableAnchor)
    {
        if (_highlightedTables.ContainsKey(tableAnchor))
        {
            GameObject highlightGO = _highlightedTables[tableAnchor];
            if (highlightGO != null)
            {
                Destroy(highlightGO);
            }
            _highlightedTables.Remove(tableAnchor);
        }
    }
    
    /// <summary>
    /// Clear all highlights
    /// </summary>
    public void ClearAllHighlights()
    {
        foreach (var kvp in _highlightedTables)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        _highlightedTables.Clear();
    }
    
    /// <summary>
    /// Get highlight GameObject for a table
    /// </summary>
    public GameObject GetTableHighlight(MRUKAnchor tableAnchor)
    {
        _highlightedTables.TryGetValue(tableAnchor, out GameObject highlight);
        return highlight;
    }
}

