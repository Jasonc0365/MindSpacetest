using UnityEngine;

/// <summary>
/// Component for scanned objects that can be manipulated
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ScannedObjectPrefab : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool isGrabbable = true;
    [SerializeField] private bool useGravity = false;
    [SerializeField] private bool isKinematic = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material ghostMaterial;
    
    private Rigidbody _rigidbody;
    private Renderer _renderer;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private bool _isGrabbed = false;
    
    public Vector3 OriginalPosition => _originalPosition;
    public Quaternion OriginalRotation => _originalRotation;
    public bool IsGrabbed => _isGrabbed;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        
        if (_rigidbody != null)
        {
            _rigidbody.useGravity = useGravity;
            _rigidbody.isKinematic = isKinematic;
        }
        
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        
        // Add OVRGrabbable component for hand tracking interaction
        if (isGrabbable && GetComponent<OVRGrabbable>() == null)
        {
            OVRGrabbable grabbable = gameObject.AddComponent<OVRGrabbable>();
            // OVRGrabbable will auto-configure grab points from colliders
        }
    }
    
    private void Start()
    {
        // Store original material
        if (_renderer != null && originalMaterial == null)
        {
            originalMaterial = _renderer.material;
        }
    }
    
    /// <summary>
    /// Called when object is grabbed
    /// </summary>
    public void OnGrabbed()
    {
        _isGrabbed = true;
        
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
        }
        
        if (_renderer != null && ghostMaterial != null)
        {
            _renderer.material = ghostMaterial;
        }
    }
    
    /// <summary>
    /// Called when object is released
    /// </summary>
    public void OnReleased()
    {
        _isGrabbed = false;
        
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = isKinematic;
        }
        
        if (_renderer != null && originalMaterial != null)
        {
            _renderer.material = originalMaterial;
        }
    }
    
    /// <summary>
    /// Reset object to original position
    /// </summary>
    public void ResetToOriginalPosition()
    {
        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        
        if (_rigidbody != null)
        {
#if UNITY_6000_0_OR_NEWER
            _rigidbody.linearVelocity = Vector3.zero;
#else
            _rigidbody.velocity = Vector3.zero;
#endif
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Highlight object
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (_renderer == null) return;
        
        if (highlighted && highlightMaterial != null)
        {
            _renderer.material = highlightMaterial;
        }
        else if (originalMaterial != null)
        {
            _renderer.material = originalMaterial;
        }
    }
}

