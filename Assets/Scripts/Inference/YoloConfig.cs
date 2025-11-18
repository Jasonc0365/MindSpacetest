using UnityEngine;
#if UNITY_SENTIS || true
using Unity.InferenceEngine;
#endif

[CreateAssetMenu(fileName = "YoloConfig", menuName = "Inference/Yolo Config", order = 0)]
public class YoloConfig : ScriptableObject
{
	#if UNITY_SENTIS || true
	[SerializeField] public ModelAsset modelAsset;
	#endif
	[SerializeField] public TextAsset labelsText;
	[SerializeField, Min(1)] public int inputSize = 640;
	[SerializeField, Range(0f, 1f)] public float confidenceThreshold = 0.25f;
	[SerializeField, Range(0f, 1f)] public float iouThreshold = 0.45f;
	[SerializeField] public string[] tabletopWhitelist = new string[]
	{
		"laptop","keyboard","mouse","cell phone","remote","cup","bottle","bowl","book","clock","vase","potted plant","wine glass","fork","knife","spoon","banana","apple","orange","sandwich","pizza","donut","cake","scissors"
	};

	public string[] GetLabels()
	{
		if (labelsText != null)
		{
			return labelsText.text.Replace("\r","\n").Split('\n');
		}
		return new string[0];
	}
}
