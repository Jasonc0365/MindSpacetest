using UnityEngine;
using Meta.XR.MRUtilityKit;

public class TableTopModifier : MonoBehaviour
{
    [SerializeField] private EffectMesh _effectMesh;
    [SerializeField, Min(0f)] private float heightOffsetMeters = 0.005f; // lift slightly above surface to avoid z-fighting

    private void OnEnable()
    {
        Debug.Log("TableTopModifier: OnEnable called.");
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnMrukSceneLoaded);
            Debug.Log("TableTopModifier: Subscribed to MRUK.SceneLoadedEvent.");
        }
        else
        {
            Debug.LogWarning("TableTopModifier: MRUK.Instance is null in OnEnable.");
        }
    }

    private void OnDisable()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnMrukSceneLoaded);
            Debug.Log("TableTopModifier: Unsubscribed from MRUK.SceneLoadedEvent.");
        }
    }

    private void OnMrukSceneLoaded()
    {
        Debug.Log("TableTopModifier: OnMrukSceneLoaded called. Attempting to modify table top meshes.");
        ModifyTableTopMeshes();
    }

    private void ModifyTableTopMeshes()
    {
        Debug.Log("TableTopModifier: ModifyTableTopMeshes called.");
        if (_effectMesh == null)
        {
            Debug.LogWarning("TableTopModifier: EffectMesh reference is not set on TableTopModifier. Please assign it in the Inspector.");
            return;
        }
        Debug.Log($"TableTopModifier: EffectMesh reference is set. EffectMeshObjects count: {_effectMesh.EffectMeshObjects.Count}");

        foreach (var kvp in _effectMesh.EffectMeshObjects)
        {
            var anchor = kvp.Key;
            var effectMeshObject = kvp.Value;

            Debug.Log($"TableTopModifier: Processing anchor: {anchor.name} (Labels: {anchor.Label})");

            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                Debug.Log($"TableTopModifier: Found a TABLE anchor: {anchor.name}");
                var meshFilter = effectMeshObject.effectMeshGO.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.mesh != null)
                {
                    Mesh mesh = meshFilter.mesh;
                    Vector3[] vertices = mesh.vertices;
                    Bounds anchorBounds = anchor.VolumeBounds.HasValue ? anchor.VolumeBounds.Value : new Bounds();

                    float topYLocal = anchorBounds.center.y + anchorBounds.extents.y;
                    Debug.Log($"TableTopModifier: {anchor.name} - Original Mesh has {vertices.Length} vertices. Top Y Local: {topYLocal}");

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i].y = topYLocal + heightOffsetMeters;
                    }

                    mesh.vertices = vertices;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    Debug.Log($"TableTopModifier: {anchor.name} - Mesh vertices modified and mesh updated.");
                }
                else
                {
                    Debug.LogWarning($"TableTopModifier: {anchor.name} - MeshFilter or Mesh is null.");
                }
            }
        }
    }
}
