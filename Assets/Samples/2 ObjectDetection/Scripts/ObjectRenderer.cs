using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Meta.XR;

public class ObjectRenderer : MonoBehaviour
{
    [Header("Marker Settings")]
    [SerializeField] private GameObject markerPrefab;
    
    [Header("Label Filtering")]
    [SerializeField] private YOLOv11Labels[] labelFilters;
    [SerializeField, Range(0f, 1f)] private float minConfidence = 0.15f;
    [SerializeField] private bool useDeskObjectFilter = true;
    
    [Header("Stability Tracking")]
    [SerializeField, Min(1)] private int stabilityFrameCount = 3;
    [SerializeField, Min(1)] private int removalFrameCount = 5;
    [SerializeField] private float positionMatchThreshold = 0.2f;
    
    [Header("Label Locking")]
    [SerializeField, Range(0f, 1f)] private float lockConfidenceThreshold = 0.7f;
    [SerializeField, Min(1)] private int lockFrameCount = 3;
    
    [Header("World Locking")]
    [SerializeField] private float movementUpdateThreshold = 0.1f; // Distance in meters object must move before label position updates

    // Default desk objects that can be found on a desk
    private static readonly YOLOv11Labels[] DefaultDeskObjects = new[]
    {
        YOLOv11Labels.laptop,
        YOLOv11Labels.keyboard,
        YOLOv11Labels.mouse,
        YOLOv11Labels.cell_phone,
        YOLOv11Labels.remote,
        YOLOv11Labels.cup,
        YOLOv11Labels.bottle,
        YOLOv11Labels.bowl,
        YOLOv11Labels.book,
        YOLOv11Labels.clock,
        YOLOv11Labels.vase,
        YOLOv11Labels.pottedplant,
        YOLOv11Labels.wine_glass,
        YOLOv11Labels.fork,
        YOLOv11Labels.knife,
        YOLOv11Labels.spoon,
        YOLOv11Labels.banana,
        YOLOv11Labels.apple,
        YOLOv11Labels.orange,
        YOLOv11Labels.sandwich,
        YOLOv11Labels.pizza,
        YOLOv11Labels.donut,
        YOLOv11Labels.cake,
        YOLOv11Labels.scissors
    };

    private Camera _mainCamera;
    private const float ModelInputSize = 640f;
    private PassthroughCameraAccess _cameraAccess;
    private EnvironmentRaycastManager _envRaycastManager;
    private readonly Dictionary<string, MarkerController> _activeMarkers = new();
    private readonly Dictionary<string, DetectionTracker> _trackers = new();

    private class DetectionTracker
    {
        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public Vector3 lastScale;
        public int classId;
        public string label;
        public string lockedLabelText; // Preserved label text when locked
        public int consecutiveDetections;
        public int consecutiveMisses;
        public int consecutiveHighConfidenceFrames;
        public float avgConfidence;
        public float lastConfidence;
        public string trackerId;
        public bool isLocked;
        
        // Anchor position for world locking
        public Vector3 anchorPosition;
        public Vector3 anchorScale;
        public bool hasAnchor;

        public DetectionTracker(Vector3 position, Quaternion rotation, Vector3 scale, int classId, string label, float confidence, string id)
        {
            lastPosition = position;
            lastRotation = rotation;
            lastScale = scale;
            this.classId = classId;
            this.label = label;
            lockedLabelText = label; // Initialize with label name
            consecutiveDetections = 1;
            consecutiveMisses = 0;
            consecutiveHighConfidenceFrames = confidence >= 0.7f ? 1 : 0;
            avgConfidence = confidence;
            lastConfidence = confidence;
            trackerId = id;
            isLocked = false;
            hasAnchor = false;
            anchorPosition = position;
            anchorScale = scale;
        }

        public void Update(Vector3 position, Quaternion rotation, Vector3 scale, float confidence, float lockThreshold, int lockFrames, int stabilityFrames)
        {
            lastPosition = position;
            lastRotation = rotation;
            lastScale = scale;
            lastConfidence = confidence;
            avgConfidence = (avgConfidence * consecutiveDetections + confidence) / (consecutiveDetections + 1);
            consecutiveDetections++;
            consecutiveMisses = 0;

            // Set anchor position when label becomes stable
            if (!hasAnchor && consecutiveDetections >= stabilityFrames)
            {
                anchorPosition = position;
                anchorScale = scale;
                hasAnchor = true;
            }

            // Check for locking condition
            if (!isLocked)
            {
                if (confidence >= lockThreshold)
                {
                    consecutiveHighConfidenceFrames++;
                    if (consecutiveHighConfidenceFrames >= lockFrames)
                    {
                        isLocked = true;
                        lockedLabelText = label; // Lock the current label text
                    }
                }
                else
                {
                    consecutiveHighConfidenceFrames = 0;
                }
            }
            // If already locked, continue background updates (position/scale) but don't change label
        }

        public void Miss()
        {
            consecutiveMisses++;
            if (!isLocked)
            {
                consecutiveHighConfidenceFrames = 0;
            }
        }

        public bool IsStable => consecutiveDetections >= 1; // Will be checked against stabilityFrameCount externally
        public bool ShouldRemove => consecutiveMisses >= 1; // Will be checked against removalFrameCount externally
    }

    private void Awake()
    {
        _cameraAccess = GetComponent<PassthroughCameraAccess>() ?? FindAnyObjectByType<PassthroughCameraAccess>(FindObjectsInactive.Include);
        _envRaycastManager = GetComponent<EnvironmentRaycastManager>() ?? FindAnyObjectByType<EnvironmentRaycastManager>(FindObjectsInactive.Include);
        if (!_cameraAccess || !_envRaycastManager)
        {
            Debug.LogWarning("[ObjectRenderer] Passthrough camera or Environment Raycast Manager is not ready.");
            return;
        }
        _mainCamera = Camera.main;
        
        // Apply desk object filter by default if enabled and no custom filter set
        if (useDeskObjectFilter && (labelFilters == null || labelFilters.Length == 0))
        {
            labelFilters = DefaultDeskObjects;
        }
    }
    
    public void RenderDetections(List<DetectionData> detections)
    {
        if (detections == null || detections.Count == 0)
        {
            // No detections this frame - increment misses for all trackers
            UpdateTrackerMisses();
            return;
        }

        if (!_cameraAccess || !_envRaycastManager)
        {
            Debug.LogWarning("[ObjectRenderer] Missing dependencies.");
            return;
        }

        // Mark all trackers as potentially missed this frame
        foreach (var tracker in _trackers.Values)
        {
            tracker.Miss();
        }

        // Process new detections and match to existing trackers
        var matchedTrackerIds = new HashSet<string>();
        
        foreach (var detection in detections)
        {
            // Apply label filter (desk objects or custom filter)
            if (labelFilters is { Length: > 0 })
            {
                if (!Enum.TryParse<YOLOv11Labels>(detection.label, out var labelEnum) ||
                    !Array.Exists(labelFilters, label => label == labelEnum))
                {
                    continue;
                }
            }

            // Apply confidence threshold
            if (detection.confidence < minConfidence)
            {
                continue;
            }

            // Convert detection to 3D world position
            var worldPos = DetectionToWorldPosition(detection);
            if (worldPos == null)
            {
                continue;
            }

            var (position, rotation, scale) = worldPos.Value;

            // Try to match to existing tracker
            DetectionTracker matchedTracker = null;
            string matchedTrackerId = null;

            foreach (var kvp in _trackers)
            {
                var tracker = kvp.Value;
                var distance = Vector3.Distance(tracker.lastPosition, position);
                
                // Match by position and class
                if (distance < positionMatchThreshold && tracker.classId == detection.classId)
                {
                    matchedTracker = tracker;
                    matchedTrackerId = kvp.Key;
                    break;
                }
            }

            if (matchedTracker != null)
            {
                // Update existing tracker (background check continues even if locked)
                matchedTracker.Update(position, rotation, scale, detection.confidence, lockConfidenceThreshold, lockFrameCount, stabilityFrameCount);
                matchedTrackerIds.Add(matchedTrackerId);
            }
            else
            {
                // Create new tracker
                var trackerId = $"{detection.label}_{detection.classId}_{_trackers.Count}";
                var newTracker = new DetectionTracker(position, rotation, scale, detection.classId, detection.label, detection.confidence, trackerId);
                _trackers[trackerId] = newTracker;
                matchedTrackerIds.Add(trackerId);
            }
        }

        // Update markers based on tracker states
        UpdateMarkersFromTrackers();
    }

    private void UpdateTrackerMisses()
    {
        foreach (var tracker in _trackers.Values)
        {
            tracker.Miss();
        }
        UpdateMarkersFromTrackers();
    }

    private void UpdateMarkersFromTrackers()
    {
        var trackersToRemove = new List<string>();

        foreach (var kvp in _trackers)
        {
            var trackerId = kvp.Key;
            var tracker = kvp.Value;

            // Remove tracker if it has been missing for too long
            if (tracker.consecutiveMisses >= removalFrameCount)
            {
                trackersToRemove.Add(trackerId);
                continue;
            }

            // Only show marker if tracker is stable (detected for enough consecutive frames)
            if (tracker.consecutiveDetections >= stabilityFrameCount)
            {
                // Get or create marker
                if (!_activeMarkers.TryGetValue(trackerId, out var marker))
                {
                    // Create new marker
                    var markerGo = Instantiate(markerPrefab);
                    marker = markerGo.GetComponent<MarkerController>();
                    if (!marker)
                    {
                        Debug.LogWarning($"[ObjectRenderer] Marker prefab is missing a MarkerController component.");
                        Destroy(markerGo);
                        continue;
                    }
                    _activeMarkers[trackerId] = marker;
                }

                // Determine position to use: only update if object moved significantly from anchor
                Vector3 markerPosition;
                Vector3 markerScale;
                
                if (tracker.hasAnchor)
                {
                    // Check if object has moved significantly from anchor position
                    float distanceFromAnchor = Vector3.Distance(tracker.lastPosition, tracker.anchorPosition);
                    if (distanceFromAnchor > movementUpdateThreshold)
                    {
                        // Object moved significantly - update anchor and use new position
                        tracker.anchorPosition = tracker.lastPosition;
                        tracker.anchorScale = tracker.lastScale;
                        markerPosition = tracker.lastPosition;
                        markerScale = tracker.lastScale;
                    }
                    else
                    {
                        // Object hasn't moved much - keep label at anchor position (world locked)
                        markerPosition = tracker.anchorPosition;
                        markerScale = tracker.anchorScale;
                    }
                }
                else
                {
                    // No anchor set yet - use current position
                    markerPosition = tracker.lastPosition;
                    markerScale = tracker.lastScale;
                }

                // Calculate rotation to face camera (billboard effect)
                Quaternion markerRotation;
                if (_mainCamera != null)
                {
                    Vector3 directionToCamera = _mainCamera.transform.position - markerPosition;
                    if (directionToCamera.sqrMagnitude > 0.0001f)
                    {
                        // Negate direction to flip 180 degrees so label faces camera correctly
                        markerRotation = Quaternion.LookRotation(-directionToCamera.normalized);
                    }
                    else
                    {
                        markerRotation = Quaternion.identity;
                    }
                }
                else
                {
                    markerRotation = tracker.lastRotation;
                }

                // Update marker position and text
                // For locked labels, use the preserved locked label text (stable)
                // For unlocked labels, use current label name (no confidence percentage)
                string labelText = tracker.isLocked ? tracker.lockedLabelText : tracker.label;

                // Update marker with world-locked position and camera-facing rotation
                marker.UpdateMarker(markerPosition, markerRotation, markerScale, labelText);
            }
            else
            {
                // Tracker not stable yet - hide marker if it exists
                if (_activeMarkers.TryGetValue(trackerId, out var marker))
                {
                    if (marker && marker.gameObject)
                    {
                        marker.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Remove old trackers and their markers
        foreach (var trackerId in trackersToRemove)
        {
            if (_activeMarkers.TryGetValue(trackerId, out var marker))
            {
                if (marker && marker.gameObject)
                {
                    Destroy(marker.gameObject);
                }
                _activeMarkers.Remove(trackerId);
            }
            _trackers.Remove(trackerId);
        }
    }

    private (Vector3 position, Quaternion rotation, Vector3 scale)? DetectionToWorldPosition(DetectionData detection)
    {
        var imageWidth = ModelInputSize;
        var imageHeight = ModelInputSize;

        // Convert normalized center to pixel coordinates
        var centerX = detection.center.x * imageWidth;
        var centerY = detection.center.y * imageHeight;
        var width = detection.size.x * imageWidth;
        var height = detection.size.y * imageHeight;

        var perX = detection.center.x;
        var perY = detection.center.y;
        var centerRay = _cameraAccess.ViewportPointToRay(DetectionToViewport(perX, perY));

        if (!_envRaycastManager.Raycast(centerRay, out var centerHit))
        {
            return null;
        }

        var markerWorldPos = centerHit.point;

        // Calculate bbox corners for scale
        var u1 = (centerX - width * 0.5f) / imageWidth;
        var v1 = (centerY - height * 0.5f) / imageHeight;
        var u2 = (centerX + width * 0.5f) / imageWidth;
        var v2 = (centerY + height * 0.5f) / imageHeight;

        var tlRay = _cameraAccess.ViewportPointToRay(DetectionToViewport(u1, v1));
        var trRay = _cameraAccess.ViewportPointToRay(DetectionToViewport(u2, v1));
        var blRay = _cameraAccess.ViewportPointToRay(DetectionToViewport(u1, v2));

        var depth = Vector3.Distance(_mainCamera.transform.position, markerWorldPos);
        var worldTL = tlRay.GetPoint(depth);
        var worldTR = trRay.GetPoint(depth);
        var worldBL = blRay.GetPoint(depth);

        var markerWidth = Vector3.Distance(worldTR, worldTL);
        var markerHeight = Vector3.Distance(worldBL, worldTL);
        var markerScale = new Vector3(markerWidth, markerHeight, 1f);

        var surfaceNormal = SampleSurfaceNormal(markerWorldPos, centerHit.normal);
        
        Quaternion markerRotation;
        if (surfaceNormal.sqrMagnitude > 0.0001f)
        {
            markerRotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
        }
        else
        {
            markerRotation = Quaternion.identity;
        }

        return (markerWorldPos, markerRotation, markerScale);
    }

    private Vector2 DetectionToViewport(float normalizedX, float normalizedY)
    {
        var resolution = (Vector2)_cameraAccess.CurrentResolution;
        if (resolution == Vector2.zero)
        {
            resolution = (Vector2)_cameraAccess.Intrinsics.SensorResolution;
        }
        if (resolution == Vector2.zero)
        {
            return new Vector2(Mathf.Clamp01(normalizedX), Mathf.Clamp01(1f - normalizedY));
        }

        var scaledX = Mathf.Clamp01(normalizedX) * ModelInputSize;
        var scaledY = Mathf.Clamp01(normalizedY) * ModelInputSize;

        var actualPixel = new Vector2(
            scaledX * (resolution.x / ModelInputSize),
            scaledY * (resolution.y / ModelInputSize));

        return new Vector2(
            Mathf.Clamp01(actualPixel.x / resolution.x),
            Mathf.Clamp01(1f - actualPixel.y / resolution.y));
    }

    private Vector3 SampleSurfaceNormal(Vector3 position, Vector3 fallbackNormal)
    {
        if (_envRaycastManager == null)
        {
            return fallbackNormal;
        }

        var origin = _mainCamera ? _mainCamera.transform.position : position - fallbackNormal * 0.1f;
        var direction = position - origin;
        if (direction.sqrMagnitude > 0.0001f)
        {
            if (_envRaycastManager.Raycast(new Ray(origin, direction.normalized), out var hit, direction.magnitude + 0.05f))
            {
                return hit.normal;
            }
        }

        var offsetOrigin = position + fallbackNormal.normalized * 0.05f;
        if (_envRaycastManager.Raycast(new Ray(offsetOrigin, -fallbackNormal.normalized), out var reverseHit, 0.2f))
        {
            return reverseHit.normal;
        }

        return fallbackNormal;
    }
}
