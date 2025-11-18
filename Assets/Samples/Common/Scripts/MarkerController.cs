using UnityEngine;
using TMPro;

public class MarkerController : MonoBehaviour
{
    private TextMeshProUGUI _textMesh;
    public float lastUpdateTime;
    [SerializeField] private bool disableAutoHide = false;
    [SerializeField] private float autoHideDelay = 2f;

    private void Awake()
    {
        _textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (_textMesh == null)
        {
            Debug.LogError("No TextMeshProUGUI found on marker prefab!");
        }
    }

    /// <summary>
    /// Updates the marker's transform and text, and records the update time.
    /// </summary>
    public void UpdateMarker(Vector3 position, Quaternion rotation, Vector3 scale, string text)
    {
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;
        if (_textMesh)
        {
            _textMesh.text = text;
        }
        
        lastUpdateTime = Time.time;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (!disableAutoHide && gameObject.activeSelf && Time.time - lastUpdateTime > autoHideDelay)
        {
            gameObject.SetActive(false);
        }
    }
}
