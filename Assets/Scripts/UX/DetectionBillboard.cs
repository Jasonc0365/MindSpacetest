using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class DetectionBillboard : MonoBehaviour
{
	[SerializeField] private float faceLerp = 20f;
	[SerializeField] private Color textColor = Color.white;
	[SerializeField] private float textSize = 0.05f;

	private TextMesh _tm;

	void Awake()
	{
		_tm = GetComponent<TextMesh>();
		_tm.color = textColor;
		_tm.characterSize = textSize;
		_tm.anchor = TextAnchor.LowerCenter;
	}

	public void SetText(string s)
	{
		if (_tm != null) _tm.text = s;
	}

	void LateUpdate()
	{
		var cam = Camera.main;
		if (cam == null) return;
		Vector3 dir = (cam.transform.position - transform.position).normalized;
		Quaternion look = Quaternion.LookRotation(dir);
		transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * faceLerp);
	}
}
