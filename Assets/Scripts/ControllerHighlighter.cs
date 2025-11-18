using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Simple controller raycast highlighter for table surfaces.
/// Only handles visual highlighting - detection is handled elsewhere.
/// </summary>
public class ControllerHighlighter : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private Material lineMaterial; // Assign a material for the beam
    [SerializeField, Min(0f)] private float lineWidth = 0.005f;
    [SerializeField, Min(0f)] private float maxBeamLength = 10f; // Maximum length of the beam if nothing is hit
    
    [Header("Table Highlighter")]
    [SerializeField] private TableHighlighter tableHighlighter;

    private GameObject _currentHighlightObject = null;
    private Color _originalColor;
    private LineRenderer _lineRenderer;

    void Start()
    {
        // Ensure a LineRenderer exists on this GameObject
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer properties
        _lineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Unlit/Color")) { color = Color.white };
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true; // Use world space coordinates
        
        // Initialize TableHighlighter
        if (tableHighlighter == null)
        {
            tableHighlighter = FindFirstObjectByType<TableHighlighter>();
        }
        
        if (tableHighlighter == null)
        {
            Debug.LogWarning("ControllerHighlighter: TableHighlighter not found. Table highlighting may not work correctly.");
        }
    }

    void Update()
    {
        RaycastHit hit;
        // Ensure the raycast only hits BoxColliders
        if (Physics.Raycast(this.transform.position, this.transform.forward, out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            _lineRenderer.SetPosition(0, this.transform.position);
            _lineRenderer.SetPosition(1, hit.point);
            if (hit.collider is BoxCollider)
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // Check if this is a table highlight from TableHighlighter
                if (IsTableHighlight(hitObject))
                {
                    // This is a table highlight - handle it
                    if (_currentHighlightObject != hitObject)
                    {
                        // Restore original color of previous object if any
                        if (_currentHighlightObject != null)
                        {
                            RestoreOriginalColor();
                        }

                        // Set new highlight object
                        _currentHighlightObject = hitObject;
                        StoreOriginalColor();
                        ApplyHighlightColor();
                    }
                }
                else
                {
                    // Not a table highlight - use old behavior for other BoxColliders
                    if (_currentHighlightObject != hitObject)
                    {
                        // Restore original color of previous object if any
                        if (_currentHighlightObject != null)
                        {
                            RestoreOriginalColor();
                        }

                        // Set new highlight object and store its original color
                        _currentHighlightObject = hitObject;
                        StoreOriginalColor();
                        ApplyHighlightColor();
                    }
                }
            }
            else
            {
                // Hit something, but it's not a BoxCollider, so clear highlight
                ClearHighlight();
            }
        }
        else
        {
            // No hit, so clear highlight and extend beam to max length
            ClearHighlight();
            _lineRenderer.SetPosition(0, this.transform.position);
            _lineRenderer.SetPosition(1, this.transform.position + this.transform.forward * maxBeamLength);
        }
    }


    void ClearHighlight()
    {
        if (_currentHighlightObject != null)
        {
            RestoreOriginalColor();
            _currentHighlightObject = null;
        }
    }
    
    /// <summary>
    /// Check if a GameObject is a table highlight from TableHighlighter
    /// </summary>
    private bool IsTableHighlight(GameObject highlightObject)
    {
        if (tableHighlighter == null) return false;
        
        // Check if this GameObject is in the TableHighlighter's highlighted tables
        foreach (var kvp in tableHighlighter.HighlightedTables)
        {
            if (kvp.Value == highlightObject)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Store the original color of the current highlight object
    /// </summary>
    private void StoreOriginalColor()
    {
        if (_currentHighlightObject != null)
        {
            Renderer renderer = _currentHighlightObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                _originalColor = renderer.material.color;
            }
        }
    }
    
    /// <summary>
    /// Restore the original color of the current highlight object
    /// </summary>
    private void RestoreOriginalColor()
    {
        if (_currentHighlightObject != null)
        {
            Renderer renderer = _currentHighlightObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = _originalColor;
            }
        }
    }
    
    /// <summary>
    /// Apply highlight color to the current object
    /// </summary>
    private void ApplyHighlightColor()
    {
        if (_currentHighlightObject != null)
        {
            Renderer renderer = _currentHighlightObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = highlightColor;
            }
        }
    }
}
