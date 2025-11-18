using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays scanning progress and view quality feedback
/// </summary>
public class ScanningProgressIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI viewCountText;
    [SerializeField] private TextMeshProUGUI coverageText;
    [SerializeField] private GameObject qualityIndicator;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color goodQualityColor = Color.green;
    [SerializeField] private Color poorQualityColor = Color.red;
    [SerializeField] private Color neutralColor = Color.yellow;
    
    private Image _qualityIndicatorImage;
    
    private void Start()
    {
        if (qualityIndicator != null)
        {
            _qualityIndicatorImage = qualityIndicator.GetComponent<Image>();
        }
    }
    
    /// <summary>
    /// Update progress display
    /// </summary>
    public void UpdateProgress(float progress, int currentViews, int targetViews, float coverage)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{(progress * 100f):0}%";
        }
        
        if (viewCountText != null)
        {
            viewCountText.text = $"{currentViews}/{targetViews} views";
        }
        
        if (coverageText != null)
        {
            coverageText.text = $"Coverage: {(coverage * 100f):0}%";
        }
    }
    
    /// <summary>
    /// Update view quality indicator
    /// </summary>
    public void UpdateQualityIndicator(bool isGoodQuality)
    {
        if (_qualityIndicatorImage != null)
        {
            _qualityIndicatorImage.color = isGoodQuality ? goodQualityColor : poorQualityColor;
        }
    }
    
    /// <summary>
    /// Show/hide the indicator
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}

