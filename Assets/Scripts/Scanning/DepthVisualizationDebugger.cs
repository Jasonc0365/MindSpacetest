using UnityEngine;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Enhanced visualization and debugging tool for depth point clouds
/// Provides multiple visualization modes and debug information
/// </summary>
public class DepthVisualizationDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DepthPointCloudGenerator pointCloudGenerator;
    [SerializeField] private Camera vrCamera;
    
    [Header("Visualization Modes")]
    [SerializeField] private VisualizationMode visualizationMode = VisualizationMode.MeshPoints;
    [SerializeField] private bool showStatistics = true;
    [SerializeField] private bool showGizmos = true;
    
    [Header("Quest Controller Input")]
    [SerializeField] private bool useControllerInput = true;
    [SerializeField] private OVRInput.Button cycleModeButton = OVRInput.Button.Three; // X/Y button
    [SerializeField] private OVRInput.Button toggleGizmosButton = OVRInput.Button.Four; // Menu button
    [SerializeField] private OVRInput.Button toggleStatsButton = OVRInput.Button.PrimaryThumbstick; // Thumbstick click
    [SerializeField] private OVRInput.Button toggleDepthTextureButton = OVRInput.Button.SecondaryThumbstick; // Right thumbstick
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoSize = 0.02f;
    [SerializeField] private int maxGizmoPoints = 1000; // Limit for performance
    
    [Header("Statistics Display")]
    [SerializeField] private bool showOnScreenStats = true;
    [SerializeField] private int fontSize = 20;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Vector2 statsPosition = new Vector2(10, 10);
    
    [Header("Depth Texture Preview")]
    [SerializeField] private bool showDepthTexturePreview = true;
    [SerializeField] private Vector2 depthTexturePosition = new Vector2(10, 200);
    [SerializeField] private float depthTextureScale = 0.3f;
    
    [Header("Color Coding")]
    [SerializeField] private bool useDepthColorCoding = true;
    [SerializeField] private Gradient depthGradient; // Blue (near) to Red (far)
    
    // Shader property IDs
    private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
    
    private Vector3[] debugPoints;
    private Color[] debugColors;
    private Texture2D depthTexturePreview;
    
    public enum VisualizationMode
    {
        MeshPoints,      // Standard mesh point rendering
        Gizmos,          // Unity Gizmos (visible in Scene view)
        Both,            // Both mesh and gizmos
        StatisticsOnly   // Just show stats, no visualization
    }
    
    void Start()
    {
        if (pointCloudGenerator == null)
        {
            pointCloudGenerator = FindFirstObjectByType<DepthPointCloudGenerator>();
        }
        
        if (vrCamera == null)
        {
            var cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
            {
                vrCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();
            }
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
            }
        }
        
        // Setup depth gradient if not assigned
        if (depthGradient == null || depthGradient.colorKeys.Length == 0)
        {
            SetupDefaultGradient();
        }
    }
    
    void SetupDefaultGradient()
    {
        depthGradient = new Gradient();
        depthGradient.colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.blue, 0.0f),    // Near (0m)
            new GradientColorKey(Color.cyan, 0.25f),
            new GradientColorKey(Color.green, 0.5f),
            new GradientColorKey(Color.yellow, 0.75f),
            new GradientColorKey(Color.red, 1.0f)      // Far (4m)
        };
        depthGradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1.0f, 0.0f),
            new GradientAlphaKey(1.0f, 1.0f)
        };
    }
    
    void Update()
    {
        if (pointCloudGenerator == null) return;
        
        // Update debug data
        debugPoints = pointCloudGenerator.GetPointCloud();
        debugColors = pointCloudGenerator.GetPointColors();
        
        // Update depth texture preview
        if (showDepthTexturePreview)
        {
            UpdateDepthTexturePreview();
        }
        
        // Handle Quest controller input (with keyboard fallback for editor)
        if (useControllerInput)
        {
            // Cycle visualization modes (X/Y button)
            if (OVRInput.GetDown(cycleModeButton, controller) || Input.GetKeyDown(KeyCode.Space))
            {
                int currentMode = (int)visualizationMode;
                currentMode = (currentMode + 1) % System.Enum.GetValues(typeof(VisualizationMode)).Length;
                visualizationMode = (VisualizationMode)currentMode;
                
                Debug.Log($"[DepthVisualizationDebugger] Visualization mode: {visualizationMode}");
                
                // Haptic feedback
                OVRInput.SetControllerVibration(0.1f, 0.1f, controller);
            }
            
            // Toggle Gizmos (Menu button)
            if (OVRInput.GetDown(toggleGizmosButton, controller) || Input.GetKeyDown(KeyCode.G))
            {
                showGizmos = !showGizmos;
                Debug.Log($"[DepthVisualizationDebugger] Gizmos: {showGizmos}");
                
                // Haptic feedback
                OVRInput.SetControllerVibration(0.05f, 0.05f, controller);
            }
            
            // Toggle Statistics (Left thumbstick click)
            if (OVRInput.GetDown(toggleStatsButton, OVRInput.Controller.LTouch) || Input.GetKeyDown(KeyCode.S))
            {
                showStatistics = !showStatistics;
                Debug.Log($"[DepthVisualizationDebugger] Statistics: {showStatistics}");
                
                // Haptic feedback
                OVRInput.SetControllerVibration(0.05f, 0.05f, OVRInput.Controller.LTouch);
            }
            
            // Toggle Depth Texture Preview (Right thumbstick click)
            if (OVRInput.GetDown(toggleDepthTextureButton, controller) || Input.GetKeyDown(KeyCode.D))
            {
                showDepthTexturePreview = !showDepthTexturePreview;
                Debug.Log($"[DepthVisualizationDebugger] Depth texture preview: {showDepthTexturePreview}");
                
                // Haptic feedback
                OVRInput.SetControllerVibration(0.05f, 0.05f, controller);
            }
        }
        else
        {
            // Keyboard fallback (for editor testing)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int currentMode = (int)visualizationMode;
                currentMode = (currentMode + 1) % System.Enum.GetValues(typeof(VisualizationMode)).Length;
                visualizationMode = (VisualizationMode)currentMode;
                Debug.Log($"[DepthVisualizationDebugger] Visualization mode: {visualizationMode}");
            }
            
            if (Input.GetKeyDown(KeyCode.G))
            {
                showGizmos = !showGizmos;
                Debug.Log($"[DepthVisualizationDebugger] Gizmos: {showGizmos}");
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                showStatistics = !showStatistics;
                Debug.Log($"[DepthVisualizationDebugger] Statistics: {showStatistics}");
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                showDepthTexturePreview = !showDepthTexturePreview;
                Debug.Log($"[DepthVisualizationDebugger] Depth texture preview: {showDepthTexturePreview}");
            }
        }
    }
    
    void UpdateDepthTexturePreview()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        
        if (depthTexGlobal != null && depthTexturePreview == null)
        {
            RenderTexture depthRT = depthTexGlobal as RenderTexture;
            if (depthRT != null && depthRT.IsCreated())
            {
                // Create preview texture
                depthTexturePreview = new Texture2D(depthRT.width, depthRT.height, TextureFormat.RGB24, false);
            }
        }
        
        if (depthTexGlobal != null && depthTexturePreview != null)
        {
            RenderTexture depthRT = depthTexGlobal as RenderTexture;
            if (depthRT != null && depthRT.IsCreated())
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = depthRT;
                
                depthTexturePreview.ReadPixels(new Rect(0, 0, depthRT.width, depthRT.height), 0, 0);
                depthTexturePreview.Apply();
                
                RenderTexture.active = previous;
            }
        }
#endif
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || debugPoints == null || debugPoints.Length == 0)
            return;
        
        if (visualizationMode == VisualizationMode.Gizmos || visualizationMode == VisualizationMode.Both)
        {
            Gizmos.color = gizmoColor;
            
            int pointCount = Mathf.Min(debugPoints.Length, maxGizmoPoints);
            int step = debugPoints.Length / pointCount;
            
            for (int i = 0; i < debugPoints.Length; i += step)
            {
                if (i >= debugPoints.Length) break;
                
                Vector3 point = debugPoints[i];
                
                // Skip invalid points
                if (float.IsNaN(point.x) || float.IsInfinity(point.x))
                    continue;
                
                // Use color from debug colors if available
                if (debugColors != null && i < debugColors.Length)
                {
                    Gizmos.color = debugColors[i];
                }
                else if (useDepthColorCoding && vrCamera != null)
                {
                    float distance = Vector3.Distance(point, vrCamera.transform.position);
                    float normalizedDistance = Mathf.Clamp01(distance / 4.0f);
                    Gizmos.color = depthGradient.Evaluate(normalizedDistance);
                }
                
                Gizmos.DrawSphere(point, gizmoSize);
            }
        }
    }
    
    void OnGUI()
    {
        if (!showOnScreenStats && !showDepthTexturePreview)
            return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.fontStyle = FontStyle.Bold;
        
        // Statistics Display
        if (showStatistics && showOnScreenStats)
        {
            DrawStatistics(style);
        }
        
        // Depth Texture Preview
        if (showDepthTexturePreview && depthTexturePreview != null)
        {
            DrawDepthTexturePreview(style);
        }
    }
    
    void DrawStatistics(GUIStyle style)
    {
        float y = statsPosition.y;
        float lineHeight = fontSize + 5;
        
        // Title
        GUI.Label(new Rect(statsPosition.x, y, 400, 30), "=== DEPTH API DEBUG ===", style);
        y += lineHeight * 1.5f;
        
        // Point Cloud Stats
        if (debugPoints != null)
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                $"Point Count: {debugPoints.Length:N0}", style);
            y += lineHeight;
            
            if (debugPoints.Length > 0)
            {
                // Calculate statistics
                Vector3 center = CalculateCenter(debugPoints);
                float avgDistance = CalculateAverageDistance(debugPoints);
                Bounds bounds = CalculateBounds(debugPoints);
                
                GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                    $"Center: ({center.x:F2}, {center.y:F2}, {center.z:F2})", style);
                y += lineHeight;
                
                GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                    $"Avg Distance: {avgDistance:F2}m", style);
                y += lineHeight;
                
                GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                    $"Bounds: {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2}", style);
                y += lineHeight;
            }
        }
        else
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "Point Count: 0 (No points generated)", style);
            y += lineHeight;
        }
        
        y += lineHeight;
        
        // Depth API Status
#if UNITY_EDITOR || UNITY_ANDROID
        var depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        if (depthManager != null)
        {
            bool isAvailable = depthManager.IsDepthAvailable;
            string status = isAvailable ? "✅ AVAILABLE" : "❌ NOT AVAILABLE";
            Color statusColor = isAvailable ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(style);
            statusStyle.normal.textColor = statusColor;
            
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                $"Depth API: {status}", statusStyle);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "Depth API: ❌ EnvironmentDepthManager not found", style);
            y += lineHeight;
        }
        
        // Depth Texture Info
        Texture depthTex = Shader.GetGlobalTexture(DepthTextureID);
        if (depthTex != null)
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                $"Depth Texture: {depthTex.width}x{depthTex.height}", style);
            y += lineHeight;
        }
        else
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "Depth Texture: NULL", style);
            y += lineHeight;
        }
#endif
        
        y += lineHeight;
        
        // Visualization Mode
        GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
            $"Mode: {visualizationMode}", style);
        y += lineHeight;
        
        // Instructions
        if (useControllerInput)
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "X/Y: Cycle mode | Menu: Toggle Gizmos", style);
            y += lineHeight;
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "L Thumb: Stats | R Thumb: Depth Texture", style);
        }
        else
        {
            GUI.Label(new Rect(statsPosition.x, y, 400, 30), 
                "SPACE: Cycle mode | G: Gizmos | S: Stats | D: Depth", style);
        }
    }
    
    void DrawDepthTexturePreview(GUIStyle style)
    {
        if (depthTexturePreview == null) return;
        
        float width = depthTexturePreview.width * depthTextureScale;
        float height = depthTexturePreview.height * depthTextureScale;
        
        // Draw label
        GUI.Label(new Rect(depthTexturePosition.x, depthTexturePosition.y - 25, 300, 25), 
            "Depth Texture Preview:", style);
        
        // Draw texture
        GUI.DrawTexture(
            new Rect(depthTexturePosition.x, depthTexturePosition.y, width, height),
            depthTexturePreview,
            ScaleMode.StretchToFill,
            true
        );
        
        // Draw border
        DrawRect(new Rect(depthTexturePosition.x, depthTexturePosition.y, width, height), Color.white, 2);
    }
    
    void DrawRect(Rect rect, Color color, int width)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), texture);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), texture);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), texture);
        // Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), texture);
    }
    
    Vector3 CalculateCenter(Vector3[] points)
    {
        if (points == null || points.Length == 0)
            return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        int validCount = 0;
        
        foreach (Vector3 point in points)
        {
            if (!float.IsNaN(point.x) && !float.IsInfinity(point.x))
            {
                sum += point;
                validCount++;
            }
        }
        
        return validCount > 0 ? sum / validCount : Vector3.zero;
    }
    
    float CalculateAverageDistance(Vector3[] points)
    {
        if (points == null || points.Length == 0 || vrCamera == null)
            return 0f;
        
        float sum = 0f;
        int validCount = 0;
        Vector3 cameraPos = vrCamera.transform.position;
        
        foreach (Vector3 point in points)
        {
            if (!float.IsNaN(point.x) && !float.IsInfinity(point.x))
            {
                sum += Vector3.Distance(point, cameraPos);
                validCount++;
            }
        }
        
        return validCount > 0 ? sum / validCount : 0f;
    }
    
    Bounds CalculateBounds(Vector3[] points)
    {
        if (points == null || points.Length == 0)
            return new Bounds();
        
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        
        foreach (Vector3 point in points)
        {
            if (!float.IsNaN(point.x) && !float.IsInfinity(point.x))
            {
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
        }
        
        if (min.x == float.MaxValue)
            return new Bounds();
        
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }
    
    void OnDestroy()
    {
        if (depthTexturePreview != null)
        {
            Destroy(depthTexturePreview);
        }
    }
}
