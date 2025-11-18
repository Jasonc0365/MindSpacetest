using UnityEngine;
using TMPro;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Phase 1: Simple table detection and UI feedback
/// Tests: Press A button → See "Table found!" message
/// </summary>
public class Phase1_TableDetector : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Settings")]
    [SerializeField] private bool useControllerInput = true;
    [SerializeField] private KeyCode keyboardKey = KeyCode.Space;
    
    private void Update()
    {
        // Check for input
        bool inputPressed = false;
        
        if (useControllerInput)
        {
            // Right controller A button
            inputPressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
        }
        else
        {
            inputPressed = Input.GetKeyDown(keyboardKey);
        }
        
        if (inputPressed)
        {
            DetectTables();
        }
    }
    
    private void DetectTables()
    {
        // Check if MRUK is available
        if (MRUK.Instance == null)
        {
            UpdateStatus("MRUK not available. Waiting...");
            Debug.Log("[Phase1] MRUK.Instance is null");
            return;
        }
        
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            UpdateStatus("Room not loaded. Waiting for MRUK...");
            Debug.Log("[Phase1] Room is null");
            return;
        }
        
        // Find tables
        int tableCount = 0;
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                tableCount++;
                Debug.Log($"[Phase1] Found table: {anchor.name}");
            }
        }
        
        // Update UI
        if (tableCount > 0)
        {
            UpdateStatus($"✅ Found {tableCount} table(s)!");
            Debug.Log($"[Phase1] Success! Found {tableCount} table(s)");
        }
        else
        {
            UpdateStatus("❌ No tables found. Move around to help MRUK scan.");
            Debug.Log("[Phase1] No tables found");
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        else
        {
            Debug.Log($"[Phase1] Status: {message}");
        }
    }
    
    private void Start()
    {
        // Initial status
        UpdateStatus("Press A button (or Space) to detect tables");
        
        // Check MRUK
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
        }
    }
    
    private void OnSceneLoaded()
    {
        UpdateStatus("MRUK scene loaded! Press A button to detect tables");
        Debug.Log("[Phase1] MRUK scene loaded");
    }
}

