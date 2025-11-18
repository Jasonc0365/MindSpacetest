using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Data structure for a single view captured during scanning
/// </summary>
[System.Serializable]
public class ViewData
{
    public Matrix4x4 cameraTransform;
    public Texture2D rgbImage;
    public float[] depthData;
    public Vector3[] pointCloud;
    public int imageWidth;
    public int imageHeight;
    public float timestamp;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
    
    public ViewData(Matrix4x4 transform, Texture2D rgb, float[] depth, Vector3[] points, int width, int height)
    {
        cameraTransform = transform;
        rgbImage = rgb;
        depthData = depth;
        pointCloud = points;
        imageWidth = width;
        imageHeight = height;
        timestamp = Time.time;
        
        // Extract position and rotation from matrix
        cameraPosition = transform.GetColumn(3);
        cameraRotation = Quaternion.LookRotation(transform.GetColumn(2), transform.GetColumn(1));
    }
}

/// <summary>
/// Data structure for complete table scan
/// </summary>
[System.Serializable]
public class TableScanData
{
    public MRUKAnchor tableAnchor;
    public List<ViewData> views;
    public Bounds scanBounds;
    public float coveragePercentage;
    public float tableHeight;
    public Vector3 tableCenter;
    public Vector3 tableSize;
    
    public TableScanData(MRUKAnchor anchor)
    {
        tableAnchor = anchor;
        views = new List<ViewData>();
        coveragePercentage = 0f;
        
        if (anchor.VolumeBounds.HasValue)
        {
            scanBounds = anchor.VolumeBounds.Value;
            tableHeight = scanBounds.center.y + scanBounds.extents.y;
            tableCenter = scanBounds.center;
            tableSize = scanBounds.size;
        }
    }
}

