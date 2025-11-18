using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_SENTIS || true
using Unity.InferenceEngine;
#endif

public struct YoloDetection
{
	public string label;
	public int classId;
	public float confidence;
	public Rect bbox; // pixel coords in original image
}

public class YoloV11SentisDetector : MonoBehaviour
{
	[SerializeField] private YoloConfig config;
	#if UNITY_SENTIS || true
	[SerializeField] private BackendType backend = BackendType.GPUCompute;
	private Model _model;
	private Worker _worker;
	#endif
	[SerializeField, Min(1)] private int maxDetections = 200;

	private string[] _labels = Array.Empty<string>();
	private HashSet<string> _whitelist = new HashSet<string>();

	void Start()
	{
		if (config != null)
		{
			_labels = config.GetLabels();
			_whitelist = new HashSet<string>(config.tabletopWhitelist ?? Array.Empty<string>());
		}
		#if UNITY_SENTIS || true
		if (config != null && config.modelAsset != null)
		{
			_model = ModelLoader.Load(config.modelAsset);
			_worker = new Worker(_model, backend);
		}
		#endif
	}

	void OnDestroy()
	{
		#if UNITY_SENTIS || true
		_worker?.Dispose();
		#endif
	}

	public bool IsReady()
	{
		#if UNITY_SENTIS || true
		return _worker != null;
		#else
		return false;
		#endif
	}

	/// <summary>
	/// Run inference on a Texture2D (legacy support)
	/// </summary>
	public List<YoloDetection> Run(Texture2D source)
	{
		if (source == null) return new List<YoloDetection>();
		// Convert Texture2D to RenderTexture for processing
		RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
		Graphics.Blit(source, rt);
		var results = Run(rt, source.width, source.height);
		RenderTexture.ReleaseTemporary(rt);
		return results;
	}

	/// <summary>
	/// Run inference on a RenderTexture (GPU-native, preferred for PassthroughCameraAccess)
	/// </summary>
	public List<YoloDetection> Run(RenderTexture source, int originalWidth, int originalHeight)
	{
		var results = new List<YoloDetection>();
		#if UNITY_SENTIS || true
		if (_worker == null || config == null || source == null) return results;

		int inp = Mathf.Max(1, config.inputSize);
		RenderTexture resized = ResizeRenderTexture(source, inp, inp);

		var inputShape = new TensorShape(1, 3, inp, inp);
		using var input = new Tensor<float>(inputShape);
		RenderTextureToTensor(resized, input);
		RenderTexture.ReleaseTemporary(resized);

		_worker.Schedule(input);
		using Tensor<float> output = _worker.PeekOutput() as Tensor<float>;
		if (output == null) return results;
		using Tensor<float> cpu = output.ReadbackAndClone();

		results = ParseOutput(cpu, originalWidth, originalHeight);
		results = ClassAwareNMS(results, config.iouThreshold, maxDetections);
		#endif
		return results;
	}

	/// <summary>
	/// Convert RenderTexture to Tensor (GPU-native, preferred method)
	/// </summary>
	private void RenderTextureToTensor(RenderTexture src, Tensor<float> dest)
	{
		int w = src.width;
		int h = src.height;
		
		// Read pixels from RenderTexture
		Texture2D temp = new Texture2D(w, h, TextureFormat.RGB24, false);
		RenderTexture.active = src;
		temp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
		temp.Apply();
		RenderTexture.active = null;
		
		Color[] pixels = temp.GetPixels();
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int idx = (h - 1 - y) * w + x; // flip Y to match screen space
				Color c = pixels[idx];
				dest[0, 0, y, x] = c.r;
				dest[0, 1, y, x] = c.g;
				dest[0, 2, y, x] = c.b;
			}
		}
		
		Destroy(temp);
	}

	/// <summary>
	/// Convert Texture2D to Tensor (legacy support)
	/// </summary>
	private void ImageToTensor(Texture2D src, Tensor<float> dest)
	{
		Color[] pixels = src.GetPixels();
		int w = src.width;
		int h = src.height;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int idx = (h - 1 - y) * w + x; // flip Y to match screen space
				Color c = pixels[idx];
				dest[0, 0, y, x] = c.r;
				dest[0, 1, y, x] = c.g;
				dest[0, 2, y, x] = c.b;
			}
		}
	}

	private List<YoloDetection> ParseOutput(Tensor<float> tensor, int imgW, int imgH)
	{
		var res = new List<YoloDetection>();
		// Assume channel-first layout [1, C, N] where C = 4 + 80 and N = number of detections
		if (tensor.shape.rank < 3) return res;
		int numDet = tensor.shape[2];
		for (int i = 0; i < numDet; i++)
		{
			float x = tensor[0, 0, i];
			float y = tensor[0, 1, i];
			float w = tensor[0, 2, i];
			float h = tensor[0, 3, i];

			float bestScore = 0f;
			int bestClass = 0;
			for (int c = 0; c < 80; c++)
			{
				float s = tensor[0, 4 + c, i];
				if (s > bestScore)
				{
					bestScore = s;
					bestClass = c;
				}
			}
			if (bestScore < config.confidenceThreshold) continue;

			float x1 = (x - w / 2f) / config.inputSize;
			float y1 = (y - h / 2f) / config.inputSize;
			float x2 = (x + w / 2f) / config.inputSize;
			float y2 = (y + h / 2f) / config.inputSize;

			Rect bbox = new Rect(x1 * imgW, y1 * imgH, (x2 - x1) * imgW, (y2 - y2 + (y2 - y1)) * imgH);
			// fix height calculation (y2 - y1)
			bbox.height = (y2 - y1) * imgH;

			string label = (bestClass >= 0 && bestClass < _labels.Length) ? _labels[bestClass] : ($"cls_{bestClass}");
			if (_whitelist.Count > 0 && !_whitelist.Contains(label)) continue;

			res.Add(new YoloDetection
			{
				label = label,
				classId = bestClass,
				confidence = bestScore,
				bbox = bbox
			});
		}
		return res.OrderByDescending(d => d.confidence).ToList();
	}

	private static List<YoloDetection> ClassAwareNMS(List<YoloDetection> dets, float iouTh, int maxKeep)
	{
		var result = new List<YoloDetection>();
		var remaining = new List<YoloDetection>(dets);
		while (remaining.Count > 0)
		{
			var best = remaining[0];
			result.Add(best);
			remaining.RemoveAt(0);
			remaining.RemoveAll(d => d.classId == best.classId && IoU(best.bbox, d.bbox) > iouTh);
			if (result.Count >= maxKeep) break;
		}
		return result;
	}

	private static float IoU(Rect a, Rect b)
	{
		float x1 = Mathf.Max(a.xMin, b.xMin);
		float y1 = Mathf.Max(a.yMin, b.yMin);
		float x2 = Mathf.Min(a.xMax, b.xMax);
		float y2 = Mathf.Min(a.yMax, b.yMax);
		float inter = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
		float union = a.width * a.height + b.width * b.height - inter;
		return union > 0 ? inter / union : 0f;
	}

	/// <summary>
	/// Resize RenderTexture (GPU-native, preferred method)
	/// </summary>
	private static RenderTexture ResizeRenderTexture(RenderTexture src, int w, int h)
	{
		RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
		Graphics.Blit(src, rt);
		return rt;
	}

	/// <summary>
	/// Resize Texture2D (legacy support)
	/// </summary>
	private static Texture2D ResizeTexture(Texture2D src, int w, int h)
	{
		RenderTexture rt = RenderTexture.GetTemporary(w, h);
		Graphics.Blit(src, rt);
		RenderTexture.active = rt;
		Texture2D result = new Texture2D(w, h, TextureFormat.RGB24, false);
		result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
		result.Apply();
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary(rt);
		return result;
	}
}
