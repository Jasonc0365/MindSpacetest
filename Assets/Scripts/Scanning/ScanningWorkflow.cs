using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Main orchestrator for the scanning workflow
/// Manages state machine and coordinates all scanning components
/// </summary>
public class ScanningWorkflow : MonoBehaviour
{
    public enum ScanningState
    {
        Idle,
        TableSelection,
        Scanning,
        Processing,
        Complete
    }
    
    [Header("Component References")]
    [SerializeField] private TableScanHelper scanHelper;
    [SerializeField] private MultiViewCapture multiViewCapture;
    [SerializeField] private ObjectSegmentation segmentation;
    [SerializeField] private GaussianGenerator gaussianGenerator;
    [SerializeField] private ScanningUI scanningUI;
    
    [Header("Settings")]
    [SerializeField] private bool autoStartScanning = false;
    [SerializeField] private bool useControllerInput = true; // Use Oculus controller instead of keyboard
    [SerializeField] private KeyCode startScanKey = KeyCode.Space; // Fallback for testing
    
    private ScanningState _currentState = ScanningState.Idle;
    private MRUKAnchor _selectedTable;
    private TableScanData _scanData;
    private List<GameObject> _scannedObjects = new List<GameObject>();
    
    public ScanningState CurrentState => _currentState;
    public MRUKAnchor SelectedTable => _selectedTable;
    
    private void Start()
    {
        // Find components if not assigned
        if (scanHelper == null)
            scanHelper = FindFirstObjectByType<TableScanHelper>();
        
        if (multiViewCapture == null)
            multiViewCapture = FindFirstObjectByType<MultiViewCapture>();
        
        if (segmentation == null)
            segmentation = FindFirstObjectByType<ObjectSegmentation>();
        
        if (gaussianGenerator == null)
            gaussianGenerator = FindFirstObjectByType<GaussianGenerator>();
        
        if (scanningUI == null)
            scanningUI = FindFirstObjectByType<ScanningUI>();
        
        SetState(ScanningState.Idle);
    }
    
    private void Update()
    {
        // Handle input - Oculus controller or keyboard
        bool startScanPressed = false;
        
        if (useControllerInput)
        {
            // Right controller A button (Button.One)
            startScanPressed = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
        }
        else
        {
            // Keyboard fallback for testing
            startScanPressed = Input.GetKeyDown(startScanKey);
        }
        
        if (startScanPressed)
        {
            if (_currentState == ScanningState.Idle)
            {
                StartTableSelection();
            }
        }
        
        // State machine updates
        switch (_currentState)
        {
            case ScanningState.Idle:
                UpdateIdle();
                break;
            case ScanningState.TableSelection:
                UpdateTableSelection();
                break;
            case ScanningState.Scanning:
                UpdateScanning();
                break;
            case ScanningState.Processing:
                UpdateProcessing();
                break;
            case ScanningState.Complete:
                UpdateComplete();
                break;
        }
    }
    
    /// <summary>
    /// Start table selection process
    /// </summary>
    public void StartTableSelection()
    {
        SetState(ScanningState.TableSelection);
        
        // Get available tables
        List<MRUKAnchor> tables = TableScanHelper.GetAvailableTables();
        
        if (tables.Count == 0)
        {
            Debug.LogWarning("[ScanningWorkflow] No tables found. Waiting for MRUK to detect tables...");
            return;
        }
        
        // Auto-select first table, or let user choose
        if (autoStartScanning && tables.Count > 0)
        {
            SelectTable(tables[0]);
        }
        else
        {
            Debug.Log($"[ScanningWorkflow] Found {tables.Count} table(s). Select one to scan.");
            // In a full implementation, show UI for table selection
        }
    }
    
    /// <summary>
    /// Select a table to scan
    /// </summary>
    public void SelectTable(MRUKAnchor table)
    {
        if (table == null || !table.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
        {
            Debug.LogError("[ScanningWorkflow] Invalid table selected.");
            return;
        }
        
        _selectedTable = table;
        
        // Initialize scan helper
        if (scanHelper != null)
        {
            if (scanHelper.InitializeScan(table))
            {
                SetState(ScanningState.Scanning);
            }
        }
    }
    
    /// <summary>
    /// Start scanning
    /// </summary>
    public void StartScanning()
    {
        if (_selectedTable == null)
        {
            Debug.LogError("[ScanningWorkflow] No table selected.");
            return;
        }
        
        if (scanHelper != null)
        {
            scanHelper.StartScanning();
        }
        
        if (multiViewCapture != null)
        {
            multiViewCapture.StartCapture(_selectedTable);
        }
        
        if (scanningUI != null)
        {
            scanningUI.InitializeScanningUI(scanHelper);
        }
        
        SetState(ScanningState.Scanning);
    }
    
    /// <summary>
    /// Stop scanning and process data
    /// </summary>
    public void StopScanning()
    {
        if (scanHelper != null)
        {
            scanHelper.StopScanning();
        }
        
        if (multiViewCapture != null)
        {
            multiViewCapture.StopCapture();
            _scanData = multiViewCapture.CurrentScanData;
        }
        
        SetState(ScanningState.Processing);
        StartCoroutine(ProcessScanData());
    }
    
    /// <summary>
    /// Process scan data and generate objects
    /// </summary>
    private IEnumerator ProcessScanData()
    {
        if (_scanData == null || _scanData.views.Count == 0)
        {
            Debug.LogError("[ScanningWorkflow] No scan data to process.");
            SetState(ScanningState.Idle);
            yield break;
        }
        
        Debug.Log($"[ScanningWorkflow] Processing {_scanData.views.Count} views...");
        
        // Combine all point clouds
        List<Vector3> allPoints = new List<Vector3>();
        foreach (var view in _scanData.views)
        {
            if (view.pointCloud != null)
            {
                allPoints.AddRange(view.pointCloud);
            }
        }
        
        if (allPoints.Count == 0)
        {
            Debug.LogError("[ScanningWorkflow] No points in point cloud.");
            SetState(ScanningState.Idle);
            yield break;
        }
        
        Debug.Log($"[ScanningWorkflow] Total points: {allPoints.Count}");
        yield return null;
        
        // Segment objects
        if (segmentation != null)
        {
            List<ObjectCluster> clusters = segmentation.SegmentObjects(allPoints.ToArray());
            Debug.Log($"[ScanningWorkflow] Found {clusters.Count} object clusters.");
            yield return null;
            
            // Generate representations for each cluster
            _scannedObjects.Clear();
            for (int i = 0; i < clusters.Count; i++)
            {
                ObjectCluster cluster = clusters[i];
                GameObject obj = gaussianGenerator.GenerateObjectRepresentation(
                    cluster, 
                    $"ScannedObject_{i}"
                );
                
                if (obj != null)
                {
                    // Add scanned object component
                    ScannedObjectPrefab scannedObj = obj.GetComponent<ScannedObjectPrefab>();
                    if (scannedObj == null)
                    {
                        scannedObj = obj.AddComponent<ScannedObjectPrefab>();
                    }
                    
                    _scannedObjects.Add(obj);
                }
                
                yield return null; // Yield every object to avoid frame drops
            }
        }
        
        Debug.Log($"[ScanningWorkflow] Processing complete. Created {_scannedObjects.Count} objects.");
        SetState(ScanningState.Complete);
    }
    
    /// <summary>
    /// Reset workflow to idle
    /// </summary>
    public void ResetWorkflow()
    {
        // Clean up scanned objects
        foreach (var obj in _scannedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _scannedObjects.Clear();
        
        _selectedTable = null;
        _scanData = null;
        
        SetState(ScanningState.Idle);
    }
    
    private void SetState(ScanningState newState)
    {
        if (_currentState == newState) return;
        
        Debug.Log($"[ScanningWorkflow] State: {_currentState} -> {newState}");
        _currentState = newState;
    }
    
    private void UpdateIdle()
    {
        // Wait for user input or auto-start
    }
    
    private void UpdateTableSelection()
    {
        // Wait for table selection
        if (_selectedTable != null && _currentState == ScanningState.TableSelection)
        {
            StartScanning();
        }
    }
    
    private void UpdateScanning()
    {
        if (scanHelper == null) return;
        
        // Check if scan is complete
        if (scanHelper.IsScanComplete())
        {
            StopScanning();
        }
    }
    
    private void UpdateProcessing()
    {
        // Processing happens in coroutine
    }
    
    private void UpdateComplete()
    {
        // Scan complete, objects are created
        // User can interact with objects now
    }
}

