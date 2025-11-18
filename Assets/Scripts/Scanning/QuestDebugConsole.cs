using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// On-device debug console for Quest 3 - See logs in VR
/// Just add this component to ANY GameObject - it auto-creates everything!
/// No setup needed - completely automatic.
/// </summary>
public class QuestDebugConsole : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxLines = 20;
    [SerializeField] private OVRInput.Button toggleButton = OVRInput.Button.Two; // B button
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private bool showTimestamp = true;
    [SerializeField] private bool showStackTrace = false;
    
    [Header("Position")]
    [SerializeField] private Vector3 consolePosition = new Vector3(0, 1.5f, 0.8f); // Closer: 0.8m instead of 2m
    [SerializeField] private Vector3 consoleScale = new Vector3(0.001f, 0.001f, 0.001f);
    [SerializeField] private bool followCamera = false; // Auto-face camera (disable if you want to grab and move it)
    
    [Header("Size")]
    [SerializeField] private Vector2 panelSize = new Vector2(1000, 600); // Smaller since it's closer
    [SerializeField] private float fontSize = 14; // Smaller font since it's closer
    
    [Header("Grab Settings")]
    [SerializeField] private OVRInput.Button grabButton = OVRInput.Button.PrimaryHandTrigger; // Grip button
    [SerializeField] private OVRInput.Controller grabController = OVRInput.Controller.RTouch;
    
    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;
    
    // Internal references (auto-created, no need to assign)
    private TextMeshProUGUI consoleText;
    private GameObject consolePanel;
    private Canvas canvas;
    
    private Queue<string> logQueue = new Queue<string>();
    private bool isVisible = true;
    
    // Grab functionality
    private bool isGrabbed = false;
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;
    
    void Awake()
    {
        // Always auto-create UI
        CreateDebugUI();
        
        // Subscribe to Unity log messages
        Application.logMessageReceived += HandleLog;
        
        UpdateConsoleDisplay();
    }
    
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string timestamp = showTimestamp ? $"[{System.DateTime.Now:HH:mm:ss}] " : "";
        string colorTag = "";
        string colorEndTag = "";
        
        // Color code by log type
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>";
                colorEndTag = "</color>";
                break;
            case LogType.Warning:
                colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>";
                colorEndTag = "</color>";
                break;
            default:
                colorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(normalColor)}>";
                colorEndTag = "</color>";
                break;
        }
        
        string formattedLog = $"{timestamp}{colorTag}{logString}{colorEndTag}";
        
        if (showStackTrace && !string.IsNullOrEmpty(stackTrace))
        {
            formattedLog += $"\n<size=10>{stackTrace}</size>";
        }
        
        logQueue.Enqueue(formattedLog);
        
        // Keep only recent logs
        while (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }
        
        UpdateConsoleDisplay();
    }
    
    void UpdateConsoleDisplay()
    {
        if (consoleText != null)
        {
            consoleText.text = string.Join("\n", logQueue);
        }
    }
    
    void ToggleConsole()
    {
        isVisible = !isVisible;
        if (consolePanel != null)
        {
            consolePanel.SetActive(isVisible);
        }
    }
    
    void CreateDebugUI()
    {
        // Create Canvas as child of this GameObject
        GameObject canvasObj = new GameObject("DebugConsoleCanvas");
        canvasObj.transform.SetParent(this.transform, false);
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.localScale = consoleScale;
        
        // Position in front of user (or follow camera)
        canvasObj.transform.position = consolePosition;
        canvasObj.transform.rotation = Quaternion.identity;
        
        // Add CanvasScaler for better text quality
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Add GraphicRaycaster (optional, for interactions)
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create Panel (background)
        GameObject panelObj = new GameObject("ConsolePanel");
        panelObj.transform.SetParent(canvas.transform, false);
        consolePanel = panelObj;
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = backgroundColor;
        
        // Create Text
        GameObject textObj = new GameObject("ConsoleText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = panelSize - new Vector2(40, 40); // Padding
        textRect.anchoredPosition = Vector2.zero;
        
        consoleText = textObj.AddComponent<TextMeshProUGUI>();
        consoleText.fontSize = fontSize;
        consoleText.color = normalColor;
        consoleText.alignment = TextAlignmentOptions.TopLeft;
        consoleText.textWrappingMode = TextWrappingModes.Normal;
        consoleText.overflowMode = TextOverflowModes.Ellipsis;
        consoleText.enableAutoSizing = false;
        consoleText.text = "[DEBUG CONSOLE READY]\nPress B button to toggle\nAll Debug.Log() messages appear here";
        
        Debug.Log("[QuestDebugConsole] Auto-created and ready! Press B to toggle.");
    }
    
    void Update()
    {
        if (canvas == null) return;
        
        // Handle grab/release
        HandleGrabbing();
        
        // Optional: Make console face camera (only if not grabbed and followCamera enabled)
        if (!isGrabbed && followCamera)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.transform != null)
            {
                canvas.transform.LookAt(mainCamera.transform);
                canvas.transform.Rotate(0, 180, 0); // Face camera
            }
        }
        
        // Toggle console with button
        if (OVRInput.GetDown(toggleButton, controller))
        {
            ToggleConsole();
        }
    }
    
    void HandleGrabbing()
    {
        // Check for grab button press
        if (OVRInput.GetDown(grabButton, grabController) && !isGrabbed)
        {
            TryGrabConsole();
        }
        
        // Check for grab button release
        if (OVRInput.GetUp(grabButton, grabController) && isGrabbed)
        {
            ReleaseConsole();
        }
        
        // Update console position while grabbed
        if (isGrabbed)
        {
            UpdateGrabbedPosition();
        }
    }
    
    void TryGrabConsole()
    {
        // Get controller position and rotation
        Transform controllerTransform = GetControllerTransform();
        if (controllerTransform == null) return;
        
        // Check if controller is close enough to console
        float distanceToConsole = Vector3.Distance(controllerTransform.position, canvas.transform.position);
        float grabDistance = 0.5f; // Grab range in meters
        
        if (distanceToConsole <= grabDistance)
        {
            isGrabbed = true;
            
            // Calculate offset from controller to console
            grabOffset = canvas.transform.position - controllerTransform.position;
            grabRotationOffset = Quaternion.Inverse(controllerTransform.rotation) * canvas.transform.rotation;
            
            Debug.Log("[QuestDebugConsole] Console grabbed! Move it with your controller, release grip to place.");
        }
    }
    
    void UpdateGrabbedPosition()
    {
        Transform controllerTransform = GetControllerTransform();
        if (controllerTransform == null)
        {
            ReleaseConsole();
            return;
        }
        
        // Update canvas position and rotation to follow controller
        canvas.transform.position = controllerTransform.position + controllerTransform.rotation * Quaternion.Inverse(controllerTransform.rotation) * grabOffset;
        canvas.transform.rotation = controllerTransform.rotation * grabRotationOffset;
    }
    
    void ReleaseConsole()
    {
        isGrabbed = false;
        Debug.Log("[QuestDebugConsole] Console released at new position");
    }
    
    Transform GetControllerTransform()
    {
        // Try to find OVRCameraRig and get the appropriate controller anchor
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig == null) return null;
        
        // Return the appropriate controller anchor based on which controller is grabbing
        if (grabController == OVRInput.Controller.RTouch)
        {
            return cameraRig.rightControllerAnchor;
        }
        else if (grabController == OVRInput.Controller.LTouch)
        {
            return cameraRig.leftControllerAnchor;
        }
        
        return null;
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        
        // Cleanup
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }
    
    // Public methods for manual logging
    public void Log(string message)
    {
        Debug.Log(message);
    }
    
    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
    
    public void LogError(string message)
    {
        Debug.LogError(message);
    }
    
    public void Clear()
    {
        logQueue.Clear();
        UpdateConsoleDisplay();
    }
}

