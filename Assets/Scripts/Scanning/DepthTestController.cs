using UnityEngine;

/// <summary>
/// Depth Test Controller - Simple controller input handler
/// Right Controller A button = Scan surface
/// Right Controller B button = Clear visualization
/// Based on step2-depth-capture-implementation.md
/// </summary>
public class DepthTestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthCaptureManager depthCapture;
    [SerializeField] private PointCloudVisualizer visualizer;
    
    [Header("Controller Settings")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    void Start()
    {
        // Find components if not assigned
        if (depthCapture == null)
            depthCapture = FindFirstObjectByType<DepthCaptureManager>();
        
        if (visualizer == null)
            visualizer = FindFirstObjectByType<PointCloudVisualizer>();
        
        if (depthCapture == null)
            Debug.LogError("[DepthTestController] DepthCaptureManager not found!");
        
        if (visualizer == null)
            Debug.LogError("[DepthTestController] PointCloudVisualizer not found!");
    }
    
    void Update()
    {
        // Right Controller A button - Scan surface
        if (OVRInput.GetDown(OVRInput.Button.One, controller))
        {
            OnScanButtonPressed();
        }
        
        // Right Controller B button - Clear
        if (OVRInput.GetDown(OVRInput.Button.Two, controller))
        {
            OnClearButtonPressed();
        }
    }
    
    void OnScanButtonPressed()
    {
        Debug.Log("[DepthTestController] 📸 Scan button pressed (Right Controller A)");
        
        if (depthCapture == null)
        {
            Debug.LogWarning("[DepthTestController] DepthCaptureManager not found!");
            return;
        }
        
        if (!depthCapture.IsReady())
        {
            Debug.LogWarning("[DepthTestController] Depth system not ready. Check permissions and Space Setup.");
            return;
        }
        
        // Capture depth frame
        if (depthCapture.CaptureDepthFrame())
        {
            // Visualize the captured frame
            if (visualizer != null)
            {
                visualizer.VisualizeLatestCapture();
            }
            else
            {
                Debug.LogWarning("[DepthTestController] PointCloudVisualizer not found!");
            }
        }
        else
        {
            Debug.LogWarning("[DepthTestController] Failed to capture depth frame!");
        }
    }
    
    void OnClearButtonPressed()
    {
        Debug.Log("[DepthTestController] 🗑️ Clear button pressed (Right Controller B)");
        
        if (visualizer != null)
        {
            visualizer.ClearVisualization();
        }
        else
        {
            Debug.LogWarning("[DepthTestController] PointCloudVisualizer not found!");
        }
    }
}
