using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR;
using System;

public struct DetectionData
{
    public Vector2 center;
    public Vector2 size;
    public int classId;
    public float confidence;
    public string label;
}

public class ObjectDetector : MonoBehaviour
{
    [Header("Environment Sampling")]
    [SerializeField] private Unity.InferenceEngine.ModelAsset sentisModel;
    [SerializeField] private Unity.InferenceEngine.BackendType backend = Unity.InferenceEngine.BackendType.CPU;
    [SerializeField] private float inferenceInterval = 0.1f;
    [SerializeField] private int kLayersPerFrame = 20;
    [SerializeField, Range(0f, 1f)] private float confidenceThreshold = 0.25f;
    
    private PassthroughCameraAccess _cameraAccess;
    private Unity.InferenceEngine.Model _model;
    private Unity.InferenceEngine.Worker _engine;
    private ObjectRenderer _objectRenderer;
    private Coroutine _inferenceCoroutine;
    private Texture _cameraTexture;
    private const int InputSize = 640;
    private const int NumClasses = 80;
    private bool _hasLoggedOutputShape = false;

    private void Start()
    {
        _cameraAccess = GetComponent<PassthroughCameraAccess>();
        _objectRenderer = GetComponent<ObjectRenderer>();
        
        if (!_cameraAccess || !_objectRenderer)
        {
            Debug.LogError("[ObjectDetector] PassthroughCameraAccess or Object Renderer not found in the scene.");
            return;
        }
        
        LoadModel();
        _inferenceCoroutine = StartCoroutine(InferenceLoop());
    }

    private void OnDestroy()
    {
        if (_inferenceCoroutine != null)
        {
            StopCoroutine(_inferenceCoroutine);
            _inferenceCoroutine = null;
        }
        
        _engine?.Dispose();
    }

    private void LoadModel()
    {
        try
        {
            _model = Unity.InferenceEngine.ModelLoader.Load(sentisModel);
            _engine = new Unity.InferenceEngine.Worker(_model, backend);
            Debug.Log("[ObjectDetector] Model loaded successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("[ObjectDetector] Failed to load model: " + e.Message);
        }
    }

    private IEnumerator InferenceLoop()
    {
        while (isActiveAndEnabled)
        {
            if (!TryEnsureCameraTexture())
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(inferenceInterval);

            yield return StartCoroutine(PerformInference(_cameraTexture));
        }
    }

    private IEnumerator PerformInference(Texture texture)
    {
        var tensorShape = new Unity.InferenceEngine.TensorShape(1, 3, InputSize, InputSize);
        var inputTensor = new Unity.InferenceEngine.Tensor<float>(tensorShape);
        Unity.InferenceEngine.TextureConverter.ToTensor(texture, inputTensor);

        var schedule = _engine.ScheduleIterable(inputTensor);
        if (schedule == null)
        {
            Debug.LogWarning("[ObjectDetector] ScheduleIterable returned null; falling back to synchronous scheduling.");
            _engine.Schedule(inputTensor);
        }
        else
        {
            var it = 0;
            while (schedule.MoveNext())
            {
                if (++it % kLayersPerFrame == 0)
                    yield return null;
            }
        }

        Unity.InferenceEngine.Tensor<float> outputTensor = null;
        var pullOutput = _engine.PeekOutput(0) as Unity.InferenceEngine.Tensor<float>;
        
        if (pullOutput == null)
        {
            Debug.LogError("[ObjectDetector] Output tensor is null.");
            inputTensor.Dispose();
            yield break;
        }

        var isWaiting = false;
        
        while (true)
        {
            if (pullOutput?.dataOnBackend == null)
            {
                Debug.LogError("[ObjectDetector] Output tensor is null or missing backend data.");
                inputTensor.Dispose();
                yield break;
            }
            
            if (!isWaiting)
            {
                pullOutput.ReadbackRequest();
                isWaiting = true;
            }
            else if (pullOutput.IsReadbackRequestDone())
            {
                outputTensor = pullOutput.ReadbackAndClone();
                break;
            }
            
            yield return null;
        }

        if (outputTensor != null)
        {
            // Log output shape on first inference for debugging
            if (!_hasLoggedOutputShape)
            {
                var shape = outputTensor.shape;
                Debug.Log($"[ObjectDetector] Output tensor shape: rank={shape.rank}, dimensions=[{string.Join(", ", GetShapeDimensions(shape))}]");
                _hasLoggedOutputShape = true;
            }

            var detections = ParseYOLOv11Output(outputTensor);
            inputTensor.Dispose();
            outputTensor.Dispose();

            if (_objectRenderer && detections != null)
            {
                _objectRenderer.RenderDetections(detections);
            }
        }
        else
        {
            inputTensor.Dispose();
        }
    }

    private List<DetectionData> ParseYOLOv11Output(Unity.InferenceEngine.Tensor<float> tensor)
    {
        var detections = new List<DetectionData>();
        
        if (tensor == null || tensor.shape.rank < 2)
        {
            Debug.LogWarning("[ObjectDetector] Invalid output tensor shape.");
            return detections;
        }

        var shape = tensor.shape;
        int numDetections = 0;
        int channels = 0;
        bool isChannelFirst = false;
        bool is3D = false;

        // Handle different output formats: [1, 84, N], [1, N, 84], [N, 84], [84, N]
        if (shape.rank == 3)
        {
            is3D = true;
            // Format: [batch, channels, detections] or [batch, detections, channels]
            if (shape[1] == 4 + NumClasses)
            {
                // [1, 84, N] - channel first
                numDetections = shape[2];
                channels = shape[1];
                isChannelFirst = true;
            }
            else if (shape[2] == 4 + NumClasses)
            {
                // [1, N, 84] - detection first
                numDetections = shape[1];
                channels = shape[2];
                isChannelFirst = false;
            }
            else
            {
                Debug.LogWarning($"[ObjectDetector] Unexpected 3D tensor shape: [{shape[0]}, {shape[1]}, {shape[2]}]");
                return detections;
            }
        }
        else if (shape.rank == 2)
        {
            is3D = false;
            // Format: [N, 84] or [84, N]
            if (shape[0] == 4 + NumClasses)
            {
                // [84, N] - channel first
                numDetections = shape[1];
                channels = shape[0];
                isChannelFirst = true;
            }
            else if (shape[1] == 4 + NumClasses)
            {
                // [N, 84] - detection first
                numDetections = shape[0];
                channels = shape[1];
                isChannelFirst = false;
            }
            else
            {
                Debug.LogWarning($"[ObjectDetector] Unexpected 2D tensor shape: [{shape[0]}, {shape[1]}]");
                return detections;
            }
        }
        else
        {
            Debug.LogWarning($"[ObjectDetector] Unsupported tensor rank: {shape.rank}");
            return detections;
        }

        for (int i = 0; i < numDetections; i++)
        {
            float x, y, w, h;
            
            if (is3D)
            {
                if (isChannelFirst)
                {
                    // [1, 84, N] format
                    x = tensor[0, 0, i];
                    y = tensor[0, 1, i];
                    w = tensor[0, 2, i];
                    h = tensor[0, 3, i];
                }
                else
                {
                    // [1, N, 84] format
                    x = tensor[0, i, 0];
                    y = tensor[0, i, 1];
                    w = tensor[0, i, 2];
                    h = tensor[0, i, 3];
                }
            }
            else
            {
                if (isChannelFirst)
                {
                    // [84, N] format
                    x = tensor[0, i];
                    y = tensor[1, i];
                    w = tensor[2, i];
                    h = tensor[3, i];
                }
                else
                {
                    // [N, 84] format
                    x = tensor[i, 0];
                    y = tensor[i, 1];
                    w = tensor[i, 2];
                    h = tensor[i, 3];
                }
            }

            // Find best class confidence
            float bestScore = 0f;
            int bestClass = 0;
            
            for (int c = 0; c < NumClasses; c++)
            {
                float score;
                if (is3D)
                {
                    if (isChannelFirst)
                    {
                        score = tensor[0, 4 + c, i];
                    }
                    else
                    {
                        score = tensor[0, i, 4 + c];
                    }
                }
                else
                {
                    if (isChannelFirst)
                    {
                        score = tensor[4 + c, i];
                    }
                    else
                    {
                        score = tensor[i, 4 + c];
                    }
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestClass = c;
                }
            }

            // Apply confidence threshold
            if (bestScore < confidenceThreshold)
            {
                continue;
            }

            // Convert from model space to normalized image space
            float x1 = (x - w / 2f) / InputSize;
            float y1 = (y - h / 2f) / InputSize;
            float x2 = (x + w / 2f) / InputSize;
            float y2 = (y + h / 2f) / InputSize;

            var center = new Vector2((x1 + x2) * 0.5f, (y1 + y2) * 0.5f);
            var size = new Vector2(x2 - x1, y2 - y1);

            // Get label name from enum
            string labelName = "unknown";
            if (bestClass >= 0 && bestClass < Enum.GetValues(typeof(YOLOv11Labels)).Length)
            {
                labelName = ((YOLOv11Labels)bestClass).ToString();
            }

            detections.Add(new DetectionData
            {
                center = center,
                size = size,
                classId = bestClass,
                confidence = bestScore,
                label = labelName
            });
        }

        return detections;
    }

    private string[] GetShapeDimensions(Unity.InferenceEngine.TensorShape shape)
    {
        var dims = new string[shape.rank];
        for (int i = 0; i < shape.rank; i++)
        {
            dims[i] = shape[i].ToString();
        }
        return dims;
    }

    private bool TryEnsureCameraTexture()
    {
        if (!_cameraAccess || !_cameraAccess.IsPlaying)
        {
            return false;
        }

        if (_cameraTexture)
        {
            return true;
        }

        _cameraTexture = _cameraAccess.GetTexture();
        if (_cameraTexture)
        {
            var resolution = _cameraAccess.CurrentResolution;
            print($"[ObjectDetector] Passthrough texture ready: {resolution.x}x{resolution.y}");
        }
        else
        {
            Debug.LogWarning("[ObjectDetector] Passthrough texture not available yet.");
        }

        return _cameraTexture;
    }
}
