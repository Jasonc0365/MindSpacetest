using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Main UI for scanning workflow with visual guides and instructions
/// </summary>
public class ScanningUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private ScanningProgressIndicator progressIndicator;
    [SerializeField] private GameObject positionIndicatorPrefab;
    [SerializeField] private Transform positionIndicatorParent;
    
    [Header("Visual Guides")]
    [SerializeField] private Material recommendedPositionMaterial;
    [SerializeField] private Material capturedPositionMaterial;
    [SerializeField] private Material currentPositionMaterial;
    [SerializeField] private float indicatorScale = 0.1f;
    
    [Header("Settings")]
    [SerializeField] private float instructionUpdateInterval = 0.5f;
    
    private TableScanHelper _scanHelper;
    private Camera _mainCamera;
    private List<GameObject> _positionIndicators = new List<GameObject>();
    private float _lastInstructionUpdate = 0f;
    
    private void Start()
    {
        _mainCamera = Camera.main;
        _scanHelper = FindFirstObjectByType<TableScanHelper>();
        
        if (progressIndicator != null)
        {
            progressIndicator.SetVisible(false);
        }
    }
    
    private void Update()
    {
        if (_scanHelper == null || !_scanHelper.IsScanning)
        {
            if (progressIndicator != null)
            {
                progressIndicator.SetVisible(false);
            }
            return;
        }
        
        UpdateUI();
        UpdatePositionIndicators();
        
        if (Time.time - _lastInstructionUpdate >= instructionUpdateInterval)
        {
            UpdateInstructions();
            _lastInstructionUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// Initialize UI for scanning session
    /// </summary>
    public void InitializeScanningUI(TableScanHelper scanHelper)
    {
        _scanHelper = scanHelper;
        
        if (progressIndicator != null)
        {
            progressIndicator.SetVisible(true);
        }
        
        CreatePositionIndicators();
        UpdateInstructions();
    }
    
    /// <summary>
    /// Update UI elements
    /// </summary>
    private void UpdateUI()
    {
        if (_scanHelper == null || progressIndicator == null)
            return;
        
        float progress = (float)_scanHelper.CapturedViewCount / _scanHelper.TargetViewCount;
        float coverage = _scanHelper.CoveragePercentage;
        
        progressIndicator.UpdateProgress(
            progress,
            _scanHelper.CapturedViewCount,
            _scanHelper.TargetViewCount,
            coverage
        );
        
        // Update quality indicator based on distance to recommended position
        if (_mainCamera != null)
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            int nearestIndex = _scanHelper.GetNearestRecommendedPositionIndex(cameraPos);
            
            if (nearestIndex >= 0)
            {
                float distance = Vector3.Distance(cameraPos, _scanHelper.RecommendedPositions[nearestIndex]);
                bool isGoodQuality = distance < 0.2f;
                progressIndicator.UpdateQualityIndicator(isGoodQuality);
            }
        }
    }
    
    /// <summary>
    /// Update instruction text
    /// </summary>
    private void UpdateInstructions()
    {
        if (instructionText == null || _scanHelper == null)
            return;
        
        if (!_scanHelper.IsScanning)
        {
            instructionText.text = "Ready to scan. Select a table to begin.";
            return;
        }
        
        if (_mainCamera == null)
            return;
        
        Vector3 cameraPos = _mainCamera.transform.position;
        int nearestIndex = _scanHelper.GetNearestRecommendedPositionIndex(cameraPos);
        
        if (nearestIndex >= 0)
        {
            float distance = Vector3.Distance(cameraPos, _scanHelper.RecommendedPositions[nearestIndex]);
            
            if (distance < 0.2f)
            {
                instructionText.text = $"Position {nearestIndex + 1} - Good! Hold still to capture...";
            }
            else
            {
                instructionText.text = $"Move to position {nearestIndex + 1} ({distance * 100f:0}cm away)";
            }
        }
        
        if (_scanHelper.IsScanComplete())
        {
            instructionText.text = "Scan complete! Processing data...";
        }
        else
        {
            int remaining = _scanHelper.TargetViewCount - _scanHelper.CapturedViewCount;
            instructionText.text += $"\n{remaining} views remaining";
        }
    }
    
    /// <summary>
    /// Create visual indicators for recommended positions
    /// </summary>
    private void CreatePositionIndicators()
    {
        ClearPositionIndicators();
        
        if (_scanHelper == null || positionIndicatorPrefab == null)
            return;
        
        foreach (var position in _scanHelper.RecommendedPositions)
        {
            GameObject indicator = Instantiate(positionIndicatorPrefab, positionIndicatorParent);
            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * indicatorScale;
            
            // Set initial material
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null && recommendedPositionMaterial != null)
            {
                renderer.material = recommendedPositionMaterial;
            }
            
            _positionIndicators.Add(indicator);
        }
    }
    
    /// <summary>
    /// Update position indicators based on capture status
    /// </summary>
    private void UpdatePositionIndicators()
    {
        if (_scanHelper == null || _positionIndicators.Count == 0)
            return;
        
        for (int i = 0; i < _positionIndicators.Count && i < _scanHelper.RecommendedPositions.Count; i++)
        {
            GameObject indicator = _positionIndicators[i];
            if (indicator == null) continue;
            
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer == null) continue;
            
            // Check if this position has been captured
            // (We'd need to track this in scanHelper or check view indices)
            // For now, use material based on distance
            Vector3 indicatorPos = indicator.transform.position;
            
            if (_mainCamera != null)
            {
                float distance = Vector3.Distance(_mainCamera.transform.position, indicatorPos);
                
                if (distance < 0.2f)
                {
                    if (currentPositionMaterial != null)
                    {
                        renderer.material = currentPositionMaterial;
                    }
                }
                else if (capturedPositionMaterial != null)
                {
                    // Check if captured (would need scanHelper to expose this)
                    renderer.material = recommendedPositionMaterial;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear position indicators
    /// </summary>
    private void ClearPositionIndicators()
    {
        foreach (var indicator in _positionIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        _positionIndicators.Clear();
    }
    
    private void OnDestroy()
    {
        ClearPositionIndicators();
    }
}

