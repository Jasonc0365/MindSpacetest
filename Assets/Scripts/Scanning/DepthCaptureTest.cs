using UnityEngine;
using Meta.XR;

/// <summary>
/// Test script for DepthCapture functionality on Quest.
/// Use Oculus controller to test depth capture and visualization.
/// </summary>
public class DepthCaptureTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthCapture depthCapture;
    [SerializeField] private Camera mainCamera;
    
    [Header("Visualization")]
    [SerializeField] private bool showPointCloud = true;
    [SerializeField] private GameObject pointPrefab; // Optional: prefab to visualize points
    [SerializeField] private Material pointMaterial; // Optional: material for point visualization
    [SerializeField, Range(0.01f, 0.1f)] private float pointSize = 0.05f; // Increased default size for better visibility
    [SerializeField, Range(0, 10000)] private int maxPointsToShow = 2000; // Increased default for better visualization
    
    [Header("Test Settings")]
    [SerializeField] private bool autoCaptureOnStart = false;
    [SerializeField] private bool enableDebugLogs = true; // Enable detailed logging for debugging
    [SerializeField] private bool useHapticFeedback = true; // Vibrate controller on capture
    
    [Header("UI Display (Optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI statusText; // Optional: show status on screen
    
    private GameObject _pointCloudParent;
    private Vector3[] _lastPointCloud;
    private float[] _lastDepthData;
    private RenderTexture _lastDepthTexture;
    private bool _isCapturing = false;
    private float _statusUpdateTimer = 0f;
    private float _diagnosticTimer = 0f;
    private int _diagnosticCheckCount = 0;
    
    // Controller input
    private const OVRInput.Button CAPTURE_BUTTON = OVRInput.Button.One; // A button
    private const OVRInput.Controller CONTROLLER = OVRInput.Controller.RTouch;
    
    private void Start()
    {
        // Auto-find references if not assigned
        if (depthCapture == null)
        {
            depthCapture = FindFirstObjectByType<DepthCapture>();
        }
        
        if (mainCamera == null)
        {
            // Try to find VR camera from OVRCameraRig first (most reliable for VR)
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
            {
                mainCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();
            }
            
            // Fallback to Camera.main if OVRCameraRig not found
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
        
        // Initialize depth capture (following official Oculus sample pattern)
        if (depthCapture != null)
        {
            depthCapture.InitializeDepthCapture();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DepthCaptureTest] DepthCapture found. Initializing depth (may take a few frames)...");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogError("[DepthCaptureTest] DepthCapture component not found!");
            }
        }
        
        // Check EnvironmentDepthManager directly
        if (enableDebugLogs)
        {
#if UNITY_EDITOR || UNITY_ANDROID
            var depthManager = FindFirstObjectByType<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
            if (depthManager != null)
            {
                Debug.Log($"[DepthCaptureTest] EnvironmentDepthManager found. IsDepthAvailable: {depthManager.IsDepthAvailable}");
                Debug.Log($"[DepthCaptureTest] EnvironmentDepthManager enabled: {depthManager.enabled}, gameObject active: {depthManager.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("[DepthCaptureTest] EnvironmentDepthManager not found in scene!");
            }
            
            // Get debug info from DepthCapture
            if (depthCapture != null)
            {
                string debugInfo = depthCapture.GetDebugInfo();
                Debug.Log($"[DepthCaptureTest] DepthCapture Debug Info:\n{debugInfo}");
            }
#endif
        }
        
        // Create parent for point cloud visualization
        if (showPointCloud)
        {
            _pointCloudParent = new GameObject("DepthPointCloud");
        }
        
        if (autoCaptureOnStart)
        {
            CaptureDepthFrame();
        }
    }
    
    private void Update()
    {
        // Right controller A button to capture depth
        if (OVRInput.GetDown(CAPTURE_BUTTON, CONTROLLER))
        {
            CaptureDepthFrame();
        }
        
        // Right controller B button to clear visualization
        if (OVRInput.GetDown(OVRInput.Button.Two, CONTROLLER))
        {
            ClearVisualization();
        }
        
        // Right controller X button to run diagnostics manually
        if (OVRInput.GetDown(OVRInput.Button.Three, CONTROLLER))
        {
            RunDiagnostics();
            // Haptic feedback for diagnostics
            if (useHapticFeedback)
            {
                OVRInput.SetControllerVibration(0.2f, 0.2f, CONTROLLER);
                Invoke(nameof(StopHaptics), 0.1f);
            }
        }
        
        // Update status text every 0.5 seconds
        _statusUpdateTimer += Time.deltaTime;
        if (_statusUpdateTimer >= 0.5f)
        {
            _statusUpdateTimer = 0f;
            UpdateStatusText();
        }
        
        // Periodic diagnostic check every 2 seconds
        _diagnosticTimer += Time.deltaTime;
        if (_diagnosticTimer >= 2f && enableDebugLogs)
        {
            _diagnosticTimer = 0f;
            _diagnosticCheckCount++;
            RunDiagnostics();
        }
    }
    
    /// <summary>
    /// Update on-screen status text
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        if (depthCapture == null)
        {
            statusText.text = "DepthCapture: NOT FOUND\nCheck component setup";
            statusText.color = Color.red;
            return;
        }
        
        bool isAvailable = depthCapture.IsDepthAvailable;
        int pointCount = _lastPointCloud != null ? _lastPointCloud.Length : 0;
        
        // Check depth texture status
        RenderTexture depthTex = depthCapture.GetCurrentDepthTexture();
        string textureStatus = depthTex == null ? "No Texture" : (depthTex.IsCreated() ? "Texture Ready" : "Texture Not Ready");
        
        // Get more detailed status
#if UNITY_EDITOR || UNITY_ANDROID
        var depthManager = FindFirstObjectByType<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
        bool managerEnabled = depthManager != null && depthManager.enabled;
        bool managerSupported = Meta.XR.EnvironmentDepth.EnvironmentDepthManager.IsSupported;
        
        if (isAvailable)
        {
            if (pointCount > 0)
            {
                statusText.text = $"Depth: READY ✅\nPoints: {pointCount:N0}\n{textureStatus}\nA=Capture | B=Clear";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = $"Depth: READY ✅\n{textureStatus}\nPress A to Capture";
                statusText.color = Color.yellow;
            }
        }
        else
        {
            string statusDetail = "";
            if (!managerSupported)
            {
                statusDetail = "Not Supported\nCheck OpenXR Settings";
            }
            else if (depthManager == null)
            {
                statusDetail = "Manager Missing\nInitializing...";
            }
            else if (!managerEnabled)
            {
                statusDetail = "Manager Disabled\nEnabling...";
            }
            else
            {
                statusDetail = "Waiting for Depth\nPoint at objects";
            }
            
            statusText.text = $"Depth: INITIALIZING...\n{statusDetail}\n{textureStatus}";
            statusText.color = Color.yellow;
        }
#else
        statusText.text = $"Depth: NOT SUPPORTED\nPlatform: {Application.platform}";
        statusText.color = Color.red;
#endif
    }
    
    /// <summary>
    /// Capture a depth frame and visualize it
    /// </summary>
    public void CaptureDepthFrame()
    {
        if (_isCapturing)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DepthCaptureTest] Already capturing, skipping...");
            return;
        }
        
        if (depthCapture == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[DepthCaptureTest] DepthCapture is null!");
            return;
        }
        
        _isCapturing = true;
        
        if (enableDebugLogs)
            Debug.Log("[DepthCaptureTest] Starting depth capture...");
        
        // Following official Oculus sample: wait for IsDepthAvailable to be true
        // Depth texture is only set after IsDepthAvailable becomes true
        if (!depthCapture.IsDepthAvailable)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[DepthCaptureTest] Depth not available yet. Wait for EnvironmentDepthManager to initialize depth texture.");
#if UNITY_EDITOR || UNITY_ANDROID
                var depthManager = FindFirstObjectByType<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
                if (depthManager != null)
                {
                    Debug.LogWarning($"[DepthCaptureTest] EnvironmentDepthManager: enabled={depthManager.enabled}, IsDepthAvailable={depthManager.IsDepthAvailable}");
                }
#endif
            }
            
            // Haptic feedback on failure
            if (useHapticFeedback)
            {
                OVRInput.SetControllerVibration(0.1f, 0.1f, CONTROLLER);
                Invoke(nameof(StopHaptics), 0.05f);
            }
            
            _isCapturing = false;
            return;
        }
        
        // Get depth texture (following official Oculus sample pattern)
        RenderTexture depthTexture = depthCapture.GetCurrentDepthTexture();
        
        if (enableDebugLogs)
        {
            Debug.Log("[DepthCaptureTest] Depth is available, getting depth texture...");
        }
        if (depthTexture == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DepthCaptureTest] GetCurrentDepthTexture() returned null. Depth texture not accessible yet.");
            _isCapturing = false;
            return;
        }
        
        if (!depthTexture.IsCreated())
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DepthCaptureTest] Depth texture exists but IsCreated() = false. Texture not ready yet.");
            _isCapturing = false;
            return;
        }
        
        _lastDepthTexture = depthTexture;
        
        // Get texture dimensions
        int width = depthTexture.width;
        int height = depthTexture.height;
        
        if (enableDebugLogs)
            Debug.Log($"[DepthCaptureTest] Depth texture found: {width}x{height}");
        
        // Capture depth data
        _lastDepthData = depthCapture.CaptureDepthFrame(depthTexture, width, height);
        
        if (_lastDepthData == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DepthCaptureTest] CaptureDepthFrame() returned null. Failed to read depth data.");
            _isCapturing = false;
            return;
        }
        
        if (enableDebugLogs)
        {
            // Count valid depth values
            int validDepths = 0;
            for (int i = 0; i < _lastDepthData.Length; i++)
            {
                float d = _lastDepthData[i];
                if (d > 0 && !float.IsNaN(d) && !float.IsInfinity(d))
                    validDepths++;
            }
            Debug.Log($"[DepthCaptureTest] Captured depth data: {_lastDepthData.Length} pixels, {validDepths} valid depth values");
        }
        
        // Project to world space
        if (mainCamera == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[DepthCaptureTest] Main camera is null! Cannot project to world space.");
            _isCapturing = false;
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DepthCaptureTest] Projecting depth to world space using camera: {mainCamera.name}");
        
        Matrix4x4 cameraTransform = mainCamera.transform.localToWorldMatrix;
        _lastPointCloud = depthCapture.ProjectDepthToWorld(_lastDepthData, width, height, cameraTransform, mainCamera);
        
        if (_lastPointCloud == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DepthCaptureTest] ProjectDepthToWorld() returned null.");
            _isCapturing = false;
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DepthCaptureTest] ✅ Successfully captured {_lastPointCloud.Length} 3D points from depth frame ({width}x{height})");
        
        // Haptic feedback on success
        if (useHapticFeedback)
        {
            OVRInput.SetControllerVibration(0.3f, 0.3f, CONTROLLER);
            Invoke(nameof(StopHaptics), 0.1f);
        }
        
        // Visualize point cloud
        if (showPointCloud)
        {
            VisualizePointCloud(_lastPointCloud);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[DepthCaptureTest] Point cloud visualization is disabled.");
        }
        
        _isCapturing = false;
    }
    
    /// <summary>
    /// Visualize point cloud using simple GameObjects
    /// </summary>
    private void VisualizePointCloud(Vector3[] points)
    {
        // Clear previous visualization
        ClearVisualization();
        
        if (points == null || points.Length == 0)
        {
            return;
        }
        
        // Limit points for performance
        int pointsToShow = Mathf.Min(points.Length, maxPointsToShow);
        int step = Mathf.Max(1, points.Length / pointsToShow); // Ensure step is at least 1
        
        // Create visualization objects
        int createdCount = 0;
        for (int i = 0; i < points.Length && createdCount < pointsToShow; i += step)
        {
            Vector3 point = points[i];
            
            // Skip invalid points
            if (float.IsNaN(point.x) || float.IsInfinity(point.x))
                continue;
            
            // Stop if we've created enough points
            if (createdCount >= pointsToShow)
                break;
            
            // Create point visualization
            GameObject pointGO;
            if (pointPrefab != null)
            {
                pointGO = Instantiate(pointPrefab, _pointCloudParent.transform);
            }
            else
            {
                // Create simple sphere
                pointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointGO.transform.SetParent(_pointCloudParent.transform);
                pointGO.transform.localScale = Vector3.one * pointSize;
                
                // Apply material if provided
                if (pointMaterial != null)
                {
                    pointGO.GetComponent<Renderer>().material = pointMaterial;
                }
                else
                {
                        // Default bright green color for visibility
                    Material mat = pointGO.GetComponent<Renderer>().material;
                    mat.color = Color.green;
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Glossiness", 0.5f);
                    // Make it unlit for better visibility
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.green * 2f);
                }
            }
            
            pointGO.transform.position = point;
            pointGO.name = $"DepthPoint_{createdCount}";
            createdCount++;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DepthCaptureTest] ✅ Visualized {createdCount} points (out of {points.Length} total). Look for green spheres in the scene!");
    }
    
    /// <summary>
    /// Clear point cloud visualization
    /// </summary>
    public void ClearVisualization()
    {
        if (_pointCloudParent != null)
        {
            foreach (Transform child in _pointCloudParent.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        _lastPointCloud = null;
        _lastDepthData = null;
    }
    
    /// <summary>
    /// Get statistics about last capture
    /// </summary>
    public void GetCaptureStats(out int pointCount, out int depthPixelCount, out bool hasData)
    {
        pointCount = _lastPointCloud != null ? _lastPointCloud.Length : 0;
        depthPixelCount = _lastDepthData != null ? _lastDepthData.Length : 0;
        hasData = _lastPointCloud != null && _lastPointCloud.Length > 0;
    }
    
    private void OnDestroy()
    {
        ClearVisualization();
        if (_pointCloudParent != null)
        {
            Destroy(_pointCloudParent);
        }
    }
    
    /// <summary>
    /// Run diagnostic checks to see why depth isn't working
    /// </summary>
    private void RunDiagnostics()
    {
        if (!enableDebugLogs) return;
        
        Debug.Log($"[DepthCaptureTest] === Diagnostic Check #{_diagnosticCheckCount} ===");
        
        // Check DepthCapture
        if (depthCapture == null)
        {
            Debug.LogError("[DepthCaptureTest] ❌ DepthCapture component is NULL!");
            return;
        }
        else
        {
            Debug.Log("[DepthCaptureTest] ✅ DepthCapture component found");
        }
        
        // Check EnvironmentDepthManager
#if UNITY_EDITOR || UNITY_ANDROID
        var depthManager = FindFirstObjectByType<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
        if (depthManager == null)
        {
            Debug.LogError("[DepthCaptureTest] ❌ EnvironmentDepthManager NOT FOUND in scene!");
        }
        else
        {
            Debug.Log($"[DepthCaptureTest] ✅ EnvironmentDepthManager found");
            Debug.Log($"[DepthCaptureTest]   - GameObject: {depthManager.gameObject.name}");
            Debug.Log($"[DepthCaptureTest]   - Enabled: {depthManager.enabled}");
            Debug.Log($"[DepthCaptureTest]   - Active: {depthManager.gameObject.activeInHierarchy}");
            Debug.Log($"[DepthCaptureTest]   - IsDepthAvailable: {depthManager.IsDepthAvailable}");
            
            // Check depth texture
            RenderTexture depthTex = Shader.GetGlobalTexture("_EnvironmentDepthTexture") as RenderTexture;
            if (depthTex == null)
            {
                Debug.LogWarning("[DepthCaptureTest] ⚠️ Depth texture (_EnvironmentDepthTexture) is NULL");
            }
            else
            {
                Debug.Log($"[DepthCaptureTest] ✅ Depth texture found: {depthTex.width}x{depthTex.height}, IsCreated: {depthTex.IsCreated()}");
            }
        }
        
        // Check camera
        if (mainCamera == null)
        {
            Debug.LogError("[DepthCaptureTest] ❌ Main camera is NULL!");
        }
        else
        {
            Debug.Log($"[DepthCaptureTest] ✅ Main camera found: {mainCamera.name}");
        }
        
        // Check DepthCapture status
        Debug.Log($"[DepthCaptureTest] DepthCapture.IsDepthAvailable: {depthCapture.IsDepthAvailable}");
        string debugInfo = depthCapture.GetDebugInfo();
        Debug.Log($"[DepthCaptureTest] DepthCapture Debug:\n{debugInfo}");
        
        // Quest 3 - depth API supported
        Debug.Log($"[DepthCaptureTest] Device: {OVRPlugin.GetSystemHeadsetType()} (Quest 3 - depth API supported)");
#endif
        
        Debug.Log("[DepthCaptureTest] === End Diagnostic ===");
    }
    
    /// <summary>
    /// Stop haptic feedback
    /// </summary>
    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, CONTROLLER);
    }
    
    // Gizmos for editor visualization
    private void OnDrawGizmos()
    {
        if (_lastPointCloud != null && _lastPointCloud.Length > 0)
        {
            Gizmos.color = Color.green;
            int pointsToDraw = Mathf.Min(_lastPointCloud.Length, 100); // Limit for performance
            int step = _lastPointCloud.Length / pointsToDraw;
            
            for (int i = 0; i < _lastPointCloud.Length; i += step)
            {
                Vector3 point = _lastPointCloud[i];
                if (!float.IsNaN(point.x) && !float.IsInfinity(point.x))
                {
                    Gizmos.DrawWireSphere(point, pointSize);
                }
            }
        }
    }
}

