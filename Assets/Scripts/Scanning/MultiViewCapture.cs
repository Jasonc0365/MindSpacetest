using System.Collections.Generic;
using UnityEngine;
using Meta.XR;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Captures RGB + Depth from multiple viewpoints around the table
/// </summary>
public class MultiViewCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private PassthroughCameraAccess cameraAccess;
    [SerializeField] private DepthCapture depthCapture;
    [SerializeField] private TableScanHelper scanHelper;
    [SerializeField, Range(0.1f, 2f)] private float captureInterval = 0.5f; // Time between captures
    
    [Header("View Validation")]
    [SerializeField, Min(0.1f)] private float minDistanceFromTable = 0.3f;
    [SerializeField, Min(0.5f)] private float maxDistanceFromTable = 0.8f;
    [SerializeField, Range(0f, 90f)] private float maxViewAngle = 75f; // Max angle from table normal
    
    private Camera _mainCamera;
    private TableScanData _currentScanData;
    private float _lastCaptureTime = 0f;
    private bool _isCapturing = false;
    
    public TableScanData CurrentScanData => _currentScanData;
    public bool IsCapturing => _isCapturing;
    
    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (cameraAccess == null)
        {
            cameraAccess = FindFirstObjectByType<PassthroughCameraAccess>();
        }
        
        if (depthCapture == null)
        {
            depthCapture = FindFirstObjectByType<DepthCapture>();
        }
        
        if (scanHelper == null)
        {
            scanHelper = FindFirstObjectByType<TableScanHelper>();
        }
    }
    
    private void Update()
    {
        if (_isCapturing && scanHelper != null && scanHelper.IsScanning)
        {
            // Check if we should capture a view
            if (Time.time - _lastCaptureTime >= captureInterval)
            {
                Vector3 cameraPos = _mainCamera.transform.position;
                Vector3 viewDir = _mainCamera.transform.forward;
                
                if (scanHelper.ShouldCaptureView(cameraPos, out int viewIndex))
                {
                    if (ValidateViewQuality(cameraPos, viewDir))
                    {
                        CaptureView(viewIndex);
                        scanHelper.MarkViewCaptured(viewIndex);
                        _lastCaptureTime = Time.time;
                    }
                }
                
                // Update coverage
                scanHelper.UpdateCoverage(cameraPos, viewDir);
            }
        }
    }
    
    /// <summary>
    /// Initialize capture session for a table
    /// </summary>
    public bool StartCapture(MRUKAnchor tableAnchor)
    {
        if (tableAnchor == null)
        {
            Debug.LogError("[MultiViewCapture] Invalid table anchor.");
            return false;
        }
        
        _currentScanData = new TableScanData(tableAnchor);
        _isCapturing = true;
        _lastCaptureTime = 0f;
        
        Debug.Log($"[MultiViewCapture] Started capture session for table: {tableAnchor.name}");
        return true;
    }
    
    /// <summary>
    /// Stop capture session
    /// </summary>
    public void StopCapture()
    {
        _isCapturing = false;
        Debug.Log($"[MultiViewCapture] Stopped capture session. Total views: {_currentScanData?.views.Count ?? 0}");
    }
    
    /// <summary>
    /// Capture a single view
    /// </summary>
    private void CaptureView(int viewIndex)
    {
        if (cameraAccess == null || !cameraAccess.IsPlaying)
        {
            Debug.LogWarning("[MultiViewCapture] Camera access not available.");
            return;
        }
        
        if (depthCapture == null || !depthCapture.IsDepthAvailable)
        {
            Debug.LogWarning("[MultiViewCapture] Depth capture not available.");
            return;
        }
        
        // Get camera transform
        Matrix4x4 cameraTransform = _mainCamera.transform.localToWorldMatrix;
        
        // Capture RGB image
        Texture sourceTexture = cameraAccess.GetTexture();
        if (sourceTexture == null)
        {
            Debug.LogWarning("[MultiViewCapture] Failed to get camera texture.");
            return;
        }
        
        Vector2Int resolution = cameraAccess.CurrentResolution;
        int width = resolution.x;
        int height = resolution.y;
        
        // Convert to Texture2D
        Texture2D rgbImage = ConvertToTexture2D(sourceTexture, width, height);
        
        // Capture depth
        RenderTexture depthTexture = depthCapture.GetCurrentDepthTexture();
        float[] depthData = null;
        Vector3[] pointCloud = null;
        
        if (depthTexture != null && depthTexture.IsCreated())
        {
            depthData = depthCapture.CaptureDepthFrame(depthTexture, width, height);
            
            if (depthData != null && _currentScanData != null)
            {
                // Project depth to world space
                pointCloud = depthCapture.ProjectDepthToWorld(
                    depthData, width, height, cameraTransform, _mainCamera
                );
                
                // Filter by table height
                if (pointCloud != null && _currentScanData.tableAnchor.VolumeBounds.HasValue)
                {
                    float tableHeight = _currentScanData.tableHeight;
                    pointCloud = depthCapture.FilterPointsByTableHeight(pointCloud, tableHeight, 0.5f);
                }
            }
        }
        
        // Create view data
        ViewData view = new ViewData(
            cameraTransform,
            rgbImage,
            depthData,
            pointCloud,
            width,
            height
        );
        
        // Add to scan data
        if (_currentScanData != null)
        {
            _currentScanData.views.Add(view);
            Debug.Log($"[MultiViewCapture] Captured view {viewIndex + 1}. Points: {pointCloud?.Length ?? 0}");
        }
    }
    
    /// <summary>
    /// Validate view quality before capturing
    /// </summary>
    private bool ValidateViewQuality(Vector3 cameraPosition, Vector3 viewDirection)
    {
        if (_currentScanData == null || !_currentScanData.tableAnchor.VolumeBounds.HasValue)
            return false;
        
        Bounds tableBounds = _currentScanData.tableAnchor.VolumeBounds.Value;
        Vector3 tableCenter = tableBounds.center;
        float tableHeight = tableBounds.center.y + tableBounds.extents.y;
        
        // Check distance from table
        Vector3 toTable = tableCenter - cameraPosition;
        toTable.y = 0; // Horizontal distance
        float horizontalDistance = toTable.magnitude;
        
        if (horizontalDistance < minDistanceFromTable || horizontalDistance > maxDistanceFromTable)
            return false;
        
        // Check viewing angle (should be looking down at table)
        Vector3 tableNormal = Vector3.up;
        float viewAngle = Vector3.Angle(-viewDirection, tableNormal);
        
        if (viewAngle > maxViewAngle)
            return false;
        
        // Check if camera is at appropriate height
        float heightDiff = Mathf.Abs(cameraPosition.y - (tableHeight + 1.6f)); // Eye level
        if (heightDiff > 0.5f) // Too far from expected eye level
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Convert any texture to Texture2D
    /// </summary>
    private Texture2D ConvertToTexture2D(Texture source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        
        RenderTexture.active = rt;
        Texture2D tex2D = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex2D.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return tex2D;
    }
    
    /// <summary>
    /// Get coverage percentage
    /// </summary>
    public float GetCoveragePercentage()
    {
        if (_currentScanData == null)
            return 0f;
        
        return _currentScanData.coveragePercentage;
    }
}

