using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates simplified Gaussian splats or mesh from point cloud data
/// </summary>
public class GaussianGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField, Min(0.001f)] private float voxelSize = 0.005f; // 5mm voxels
    [SerializeField, Min(100)] private int targetSplatCount = 1000;
    [SerializeField, Min(0.001f)] private float mergeThreshold = 0.005f; // 5mm merge distance
    [SerializeField] private bool useMeshFallback = true; // Use mesh instead of splats for Quest performance
    
    [Header("Gaussian Settings")]
    [SerializeField, Min(0.001f)] private float defaultScale = 0.01f;
    [SerializeField, Range(0f, 1f)] private float defaultOpacity = 0.8f;
    [SerializeField, Min(0.001f)] private float covarianceRadius = 0.01f;
    
    /// <summary>
    /// Generate Gaussian splats or mesh from object cluster
    /// </summary>
    public GameObject GenerateObjectRepresentation(ObjectCluster cluster, string objectName)
    {
        if (cluster == null || cluster.points.Count == 0)
        {
            Debug.LogWarning("[GaussianGenerator] Empty cluster provided.");
            return null;
        }
        
        if (useMeshFallback)
        {
            return GenerateMeshRepresentation(cluster, objectName);
        }
        else
        {
            return GenerateSplatRepresentation(cluster, objectName);
        }
    }
    
    /// <summary>
    /// Generate mesh representation (simpler, better for Quest)
    /// </summary>
    private GameObject GenerateMeshRepresentation(ObjectCluster cluster, string objectName)
    {
        GameObject obj = new GameObject(objectName);
        
        // Voxel downsample points
        List<Vector3> downsampledPoints = VoxelDownsample(cluster.points, voxelSize);
        
        // Generate mesh using simple approach
        Mesh mesh = GenerateMeshFromPoints(downsampledPoints);
        
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.material = CreateMaterialFromCluster(cluster);
        
        // Add collider
        MeshCollider collider = obj.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        
        obj.transform.position = cluster.centroid;
        
        return obj;
    }
    
    /// <summary>
    /// Generate Gaussian splat representation
    /// </summary>
    private GameObject GenerateSplatRepresentation(ObjectCluster cluster, string objectName)
    {
        GameObject obj = new GameObject(objectName);
        
        // Combine and downsample points
        List<Vector3> downsampledPoints = VoxelDownsample(cluster.points, voxelSize);
        
        // Limit to target count
        if (downsampledPoints.Count > targetSplatCount)
        {
            downsampledPoints = RandomSample(downsampledPoints, targetSplatCount);
        }
        
        // Generate Gaussian splats
        List<GaussianSplat> splats = new List<GaussianSplat>();
        
        foreach (var point in downsampledPoints)
        {
            GaussianSplat splat = new GaussianSplat
            {
                position = point,
                color = cluster.averageColor,
                scale = defaultScale,
                opacity = defaultOpacity,
                covariance = EstimateCovariance(point, cluster.points, covarianceRadius)
            };
            
            splats.Add(splat);
        }
        
        // Merge similar splats
        splats = MergeSimilarSplats(splats, mergeThreshold);
        
        // Add splat renderer component (would need custom implementation)
        // For now, fall back to mesh
        Debug.Log($"[GaussianGenerator] Generated {splats.Count} splats. Using mesh fallback for now.");
        return GenerateMeshRepresentation(cluster, objectName);
    }
    
    /// <summary>
    /// Voxel downsample point cloud
    /// </summary>
    private List<Vector3> VoxelDownsample(List<Vector3> points, float voxelSize)
    {
        Dictionary<Vector3Int, Vector3> voxelMap = new Dictionary<Vector3Int, Vector3>();
        Dictionary<Vector3Int, int> voxelCounts = new Dictionary<Vector3Int, int>();
        
        foreach (var point in points)
        {
            Vector3Int voxel = new Vector3Int(
                Mathf.FloorToInt(point.x / voxelSize),
                Mathf.FloorToInt(point.y / voxelSize),
                Mathf.FloorToInt(point.z / voxelSize)
            );
            
            if (voxelMap.ContainsKey(voxel))
            {
                voxelMap[voxel] += point;
                voxelCounts[voxel]++;
            }
            else
            {
                voxelMap[voxel] = point;
                voxelCounts[voxel] = 1;
            }
        }
        
        List<Vector3> downsampled = new List<Vector3>();
        foreach (var kvp in voxelMap)
        {
            Vector3 average = kvp.Value / voxelCounts[kvp.Key];
            downsampled.Add(average);
        }
        
        return downsampled;
    }
    
    /// <summary>
    /// Random sample points
    /// </summary>
    private List<Vector3> RandomSample(List<Vector3> points, int count)
    {
        if (points.Count <= count)
            return new List<Vector3>(points);
        
        List<Vector3> sampled = new List<Vector3>();
        HashSet<int> usedIndices = new HashSet<int>();
        
        while (sampled.Count < count && usedIndices.Count < points.Count)
        {
            int index = Random.Range(0, points.Count);
            if (!usedIndices.Contains(index))
            {
                sampled.Add(points[index]);
                usedIndices.Add(index);
            }
        }
        
        return sampled;
    }
    
    /// <summary>
    /// Estimate covariance matrix for Gaussian
    /// </summary>
    private Matrix3x3 EstimateCovariance(Vector3 point, List<Vector3> neighbors, float radius)
    {
        List<Vector3> localNeighbors = neighbors.Where(p => 
            Vector3.Distance(p, point) <= radius).ToList();
        
        if (localNeighbors.Count < 3)
        {
            // Default covariance
            return Matrix3x3.identity * (defaultScale * defaultScale);
        }
        
        // Calculate covariance from local neighbors
        Vector3 mean = Vector3.zero;
        foreach (var n in localNeighbors)
        {
            mean += n;
        }
        mean /= localNeighbors.Count;
        
        // Simplified covariance (diagonal)
        float variance = 0f;
        foreach (var n in localNeighbors)
        {
            variance += (n - mean).sqrMagnitude;
        }
        variance /= localNeighbors.Count;
        
        return Matrix3x3.identity * variance;
    }
    
    /// <summary>
    /// Merge similar splats
    /// </summary>
    private List<GaussianSplat> MergeSimilarSplats(List<GaussianSplat> splats, float threshold)
    {
        List<GaussianSplat> merged = new List<GaussianSplat>();
        bool[] mergedFlags = new bool[splats.Count];
        
        for (int i = 0; i < splats.Count; i++)
        {
            if (mergedFlags[i]) continue;
            
            GaussianSplat current = splats[i];
            List<int> toMerge = new List<int> { i };
            
            for (int j = i + 1; j < splats.Count; j++)
            {
                if (mergedFlags[j]) continue;
                
                float distance = Vector3.Distance(current.position, splats[j].position);
                if (distance < threshold)
                {
                    toMerge.Add(j);
                }
            }
            
            // Merge splats
            if (toMerge.Count > 1)
            {
                Vector3 mergedPos = Vector3.zero;
                Color mergedColor = Color.black;
                float totalOpacity = 0f;
                
                foreach (int idx in toMerge)
                {
                    mergedPos += splats[idx].position;
                    mergedColor += splats[idx].color;
                    totalOpacity += splats[idx].opacity;
                    mergedFlags[idx] = true;
                }
                
                mergedPos /= toMerge.Count;
                mergedColor /= toMerge.Count;
                totalOpacity /= toMerge.Count;
                
                merged.Add(new GaussianSplat
                {
                    position = mergedPos,
                    color = mergedColor,
                    scale = current.scale,
                    opacity = totalOpacity,
                    covariance = current.covariance
                });
            }
            else
            {
                merged.Add(current);
            }
        }
        
        return merged;
    }
    
    /// <summary>
    /// Generate mesh from points using simple approach
    /// </summary>
    private Mesh GenerateMeshFromPoints(List<Vector3> points)
    {
        Mesh mesh = new Mesh();
        mesh.name = "GeneratedMesh";
        
        // Simple approach: create vertices and use convex hull approximation
        mesh.vertices = points.ToArray();
        
        // Generate simple indices (this is a simplified version)
        // In production, use proper mesh generation like Poisson reconstruction
        List<int> indices = new List<int>();
        
        // Create a simple triangulation (very basic)
        for (int i = 0; i < points.Count - 2; i++)
        {
            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);
        }
        
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    /// <summary>
    /// Create material from cluster
    /// </summary>
    private Material CreateMaterialFromCluster(ObjectCluster cluster)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = cluster.averageColor;
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.5f);
        return mat;
    }
}

/// <summary>
/// Gaussian splat data structure
/// </summary>
[System.Serializable]
public struct GaussianSplat
{
    public Vector3 position;
    public Color color;
    public Matrix3x3 covariance;
    public float opacity;
    public float scale;
}

/// <summary>
/// Simple 3x3 matrix structure
/// </summary>
[System.Serializable]
public struct Matrix3x3
{
    public float m00, m01, m02;
    public float m10, m11, m12;
    public float m20, m21, m22;
    
    public static Matrix3x3 identity => new Matrix3x3
    {
        m00 = 1, m01 = 0, m02 = 0,
        m10 = 0, m11 = 1, m12 = 0,
        m20 = 0, m21 = 0, m22 = 1
    };
    
    public static Matrix3x3 operator *(Matrix3x3 m, float scalar)
    {
        return new Matrix3x3
        {
            m00 = m.m00 * scalar, m01 = m.m01 * scalar, m02 = m.m02 * scalar,
            m10 = m.m10 * scalar, m11 = m.m11 * scalar, m12 = m.m12 * scalar,
            m20 = m.m20 * scalar, m21 = m.m21 * scalar, m22 = m.m22 * scalar
        };
    }
}

