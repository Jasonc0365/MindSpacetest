using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Segments objects from point cloud using clustering algorithms
/// </summary>
public class ObjectSegmentation : MonoBehaviour
{
    [Header("Clustering Settings")]
    [SerializeField, Min(0.01f)] private float epsilon = 0.05f; // DBSCAN epsilon (5cm)
    [SerializeField, Min(1)] private int minPoints = 50; // Minimum points per cluster
    [SerializeField] private bool useDBSCAN = true;
    
    [Header("Filtering")]
    [SerializeField, Min(0.01f)] private float minObjectSize = 0.02f; // 2cm minimum
    [SerializeField, Min(0.1f)] private float maxObjectSize = 1.0f; // 1m maximum
    
    /// <summary>
    /// Segment objects from point cloud using DBSCAN clustering
    /// </summary>
    public List<ObjectCluster> SegmentObjects(Vector3[] points)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[ObjectSegmentation] Empty point cloud.");
            return new List<ObjectCluster>();
        }
        
        if (useDBSCAN)
        {
            return DBSCANClustering(points);
        }
        else
        {
            return KMeansClustering(points);
        }
    }
    
    /// <summary>
    /// DBSCAN clustering algorithm
    /// </summary>
    private List<ObjectCluster> DBSCANClustering(Vector3[] points)
    {
        List<ObjectCluster> clusters = new List<ObjectCluster>();
        bool[] visited = new bool[points.Length];
        bool[] clustered = new bool[points.Length];
        
        for (int i = 0; i < points.Length; i++)
        {
            if (visited[i]) continue;
            
            visited[i] = true;
            List<int> neighbors = GetNeighbors(points, i, epsilon);
            
            if (neighbors.Count < minPoints)
            {
                // Noise point
                continue;
            }
            
            // Create new cluster
            ObjectCluster cluster = new ObjectCluster();
            ExpandCluster(points, i, neighbors, visited, clustered, cluster, epsilon, minPoints);
            
            if (cluster.points.Count >= minPoints)
            {
                cluster.CalculateBounds();
                if (IsValidObject(cluster))
                {
                    clusters.Add(cluster);
                }
            }
        }
        
        return clusters;
    }
    
    /// <summary>
    /// Expand cluster from seed point
    /// </summary>
    private void ExpandCluster(Vector3[] points, int seedIndex, List<int> neighbors, 
        bool[] visited, bool[] clustered, ObjectCluster cluster, float eps, int minPts)
    {
        cluster.points.Add(points[seedIndex]);
        clustered[seedIndex] = true;
        
        Queue<int> queue = new Queue<int>(neighbors);
        
        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            
            if (!visited[currentIndex])
            {
                visited[currentIndex] = true;
                List<int> currentNeighbors = GetNeighbors(points, currentIndex, eps);
                
                if (currentNeighbors.Count >= minPts)
                {
                    foreach (int neighborIndex in currentNeighbors)
                    {
                        if (!queue.Contains(neighborIndex))
                        {
                            queue.Enqueue(neighborIndex);
                        }
                    }
                }
            }
            
            if (!clustered[currentIndex])
            {
                cluster.points.Add(points[currentIndex]);
                clustered[currentIndex] = true;
            }
        }
    }
    
    /// <summary>
    /// Get neighbors within epsilon distance
    /// </summary>
    private List<int> GetNeighbors(Vector3[] points, int pointIndex, float epsilon)
    {
        List<int> neighbors = new List<int>();
        Vector3 point = points[pointIndex];
        float epsilonSq = epsilon * epsilon;
        
        for (int i = 0; i < points.Length; i++)
        {
            if (i == pointIndex) continue;
            
            float distSq = (points[i] - point).sqrMagnitude;
            if (distSq <= epsilonSq)
            {
                neighbors.Add(i);
            }
        }
        
        return neighbors;
    }
    
    /// <summary>
    /// Simple K-means clustering (fallback)
    /// </summary>
    private List<ObjectCluster> KMeansClustering(Vector3[] points)
    {
        // Simplified K-means with fixed number of clusters
        int k = Mathf.Min(10, points.Length / minPoints);
        if (k < 1) k = 1;
        
        List<ObjectCluster> clusters = new List<ObjectCluster>();
        
        // Initialize centroids randomly
        Vector3[] centroids = new Vector3[k];
        for (int i = 0; i < k; i++)
        {
            centroids[i] = points[Random.Range(0, points.Length)];
        }
        
        // Iterate
        for (int iteration = 0; iteration < 10; iteration++)
        {
            // Assign points to nearest centroid
            int[] assignments = new int[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                float minDist = float.MaxValue;
                int nearestCluster = 0;
                
                for (int j = 0; j < k; j++)
                {
                    float dist = Vector3.Distance(points[i], centroids[j]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestCluster = j;
                    }
                }
                
                assignments[i] = nearestCluster;
            }
            
            // Update centroids
            Vector3[] newCentroids = new Vector3[k];
            int[] counts = new int[k];
            
            for (int i = 0; i < points.Length; i++)
            {
                int cluster = assignments[i];
                newCentroids[cluster] += points[i];
                counts[cluster]++;
            }
            
            for (int j = 0; j < k; j++)
            {
                if (counts[j] > 0)
                {
                    centroids[j] = newCentroids[j] / counts[j];
                }
            }
        }
        
        // Create clusters from final assignments
        for (int j = 0; j < k; j++)
        {
            ObjectCluster cluster = new ObjectCluster();
            for (int i = 0; i < points.Length; i++)
            {
                // Re-assign for final clusters
                float minDist = float.MaxValue;
                int nearestCluster = 0;
                for (int c = 0; c < k; c++)
                {
                    float dist = Vector3.Distance(points[i], centroids[c]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestCluster = c;
                    }
                }
                
                if (nearestCluster == j)
                {
                    cluster.points.Add(points[i]);
                }
            }
            
            if (cluster.points.Count >= minPoints)
            {
                cluster.CalculateBounds();
                if (IsValidObject(cluster))
                {
                    clusters.Add(cluster);
                }
            }
        }
        
        return clusters;
    }
    
    /// <summary>
    /// Check if cluster represents a valid object
    /// </summary>
    private bool IsValidObject(ObjectCluster cluster)
    {
        if (cluster.bounds.size.magnitude < minObjectSize)
            return false;
        
        if (cluster.bounds.size.magnitude > maxObjectSize)
            return false;
        
        return true;
    }
}

/// <summary>
/// Represents a cluster of points forming an object
/// </summary>
[System.Serializable]
public class ObjectCluster
{
    public List<Vector3> points = new List<Vector3>();
    public Bounds bounds;
    public Vector3 centroid;
    public Color averageColor = Color.white;
    
    public void CalculateBounds()
    {
        if (points.Count == 0)
        {
            bounds = new Bounds();
            centroid = Vector3.zero;
            return;
        }
        
        Vector3 min = points[0];
        Vector3 max = points[0];
        
        Vector3 sum = Vector3.zero;
        
        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
            sum += point;
        }
        
        centroid = sum / points.Count;
        bounds = new Bounds(centroid, max - min);
    }
}

