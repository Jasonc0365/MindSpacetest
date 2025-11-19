using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_ANDROID
using Meta.XR.EnvironmentDepth;
#endif

/// <summary>
/// Simple one-button depth scanner for Quest 3
/// Based on official Unity-DepthAPI samples
/// </summary>
public class SimpleDepthScanner : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private int pointStride = 2; // Reduced from 4 for higher density
    [SerializeField] private float minDepth = 0.0f; // Set to 0 to see everything
    [SerializeField] private float maxDepth = 10.0f; // Increased to 10m to catch far walls
    [SerializeField] private int maxPoints = 2000; // Reduced to 2000 for performance
    [SerializeField] private bool realtimeScanning = false; // Toggle for continuous scanning
    [SerializeField] private bool accumulatePoints = false; // Toggle to keep adding points (for meshing)
    
    [Header("Manual Alignment")]
    [SerializeField] private bool useManualTransform = false; // Disabled: Trusting the API
    [SerializeField] private bool applyCameraPosition = true; 
    [SerializeField] private bool applyCameraRotation = false;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Header("Depth Debug")]
    [SerializeField] private bool reverseZ = false; // Try Standard Z first
    [SerializeField] private bool useNDC_Neg1to1 = true; // Try -1..1 (OpenGL) range to fix "Too Close" issue
    [SerializeField] private bool invertReprojectionMatrix = true; // Matrix is World->Clip, so Inverse is Clip->World
    [SerializeField] private float depthScale = 1.0f; // Manual scale multiplier if points are consistently off
    [SerializeField] private float depthBias = 0.0f; // "Environment Depth Bias" from docs (e.g. 0.06). Pulls points towards camera.
    [SerializeField] private Color pointColor = Color.cyan;
    [SerializeField] private float pointSize = 0.005f; // Reduced from 0.01f for "small dots"
    [SerializeField] private Material particleMaterial; // Assign "Default-Particle" or similar here
    
    [Header("Controller")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private OVRInput.Button scanButton = OVRInput.Button.One;
    [SerializeField] private OVRInput.Button clearButton = OVRInput.Button.Two; // Button to clear points
    
    // Depth system
#if UNITY_EDITOR || UNITY_ANDROID
    private EnvironmentDepthManager depthManager;
#endif
    private bool isInitialized = false;
    private float lastWarningTime = 0f;
    private const float WARNING_COOLDOWN = 2f; // Only show warning every 2 seconds max
    
    // Visualization
    // private GameObject pointCloudContainer; // Replaced by ParticleSystem
    // private List<GameObject> pointObjects = new List<GameObject>(); // Replaced by ParticleSystem
    // private Material pointMaterial; // Replaced by ParticleSystem renderer
    
    // Shader globals (set by EnvironmentDepthManager)
    private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
    private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
    
    // Matrix Fallback
    private Matrix4x4 lastValidMatrix;
    private bool hasValidMatrix = false;
    
    void Start()
    {
        StartCoroutine(InitializeDepth());
    }
    
    IEnumerator InitializeDepth()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Check support
        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogError("[SimpleDepthScanner] Depth API not supported on this device/platform");
            yield break;
        }
        
        // Find or create EnvironmentDepthManager
        depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
        
        if (depthManager == null)
        {
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig == null)
            {
                Debug.LogError("[SimpleDepthScanner] No OVRCameraRig found. Cannot auto-create EnvironmentDepthManager.");
                yield break;
            }
            
            Debug.Log("[SimpleDepthScanner] Creating EnvironmentDepthManager...");
            depthManager = cameraRig.gameObject.AddComponent<EnvironmentDepthManager>();
            depthManager.RemoveHands = true;
            depthManager.enabled = true;
        }
        else if (!depthManager.enabled)
        {
            Debug.Log("[SimpleDepthScanner] Enabling existing EnvironmentDepthManager");
            depthManager.enabled = true;
        }
        
        // Wait for depth to become available - NO TIMEOUT
        Debug.Log("[SimpleDepthScanner] Waiting for depth data...");
        
        while (!depthManager.IsDepthAvailable)
        {
            // Optional: Add a timeout warning but keep trying
            if (Time.frameCount % 300 == 0) 
            {
                Debug.LogWarning($"[SimpleDepthScanner] Still waiting for depth... (IsDepthAvailable: {depthManager.IsDepthAvailable})");
            }
            yield return null;
        }
        
        isInitialized = true;
        Debug.Log("[SimpleDepthScanner] Ready! Press A to scan, B to clear");
#endif
        yield return null;
    }

    [ContextMenu("Force Re-Initialize")]
    public void ForceReInitialize()
    {
        isInitialized = false;
        StopAllCoroutines();
        StartCoroutine(InitializeDepth());
    }

    [ContextMenu("Reset to High Quality Defaults")]
    public void ResetToDefaults()
    {
        pointStride = 2;
        minDepth = 0.0f;
        maxDepth = 10.0f;
        maxPoints = 50000;
        pointSize = 0.005f;
        pointColor = Color.cyan;
        Debug.Log("[SimpleDepthScanner] Reset to high quality settings (50k points, 2 stride, 0.005 size)");
    }

    [ContextMenu("Reset to Official Settings (Recommended)")]
    public void ResetToOfficialSettings()
    {
        // 1. Depth Interpretation
        reverseZ = false;           // Standard 0..1 depth
        useNDC_Neg1to1 = false;     // Vulkan uses 0..1 Z range
        invertReprojectionMatrix = true; // Matrix is World->Clip, so Inverse is Clip->World
        
        // 2. Alignment
        useManualTransform = false; // Trust the API's matrix
        applyCameraPosition = true;
        applyCameraRotation = false;
        rotationOffset = Vector3.zero;
        positionOffset = Vector3.zero;
        
        // 3. Performance
        maxPoints = 4000;
        pointStride = 4;
        
        Debug.Log("[SimpleDepthScanner] Reset to Official Settings: ReverseZ=False, NDC=False, Invert=True, Manual=False");
    }
    
    void Update()
    {
        // Clear button
        if (OVRInput.GetDown(clearButton, controller))
        {
            ClearPointCloud();
            Debug.Log("[SimpleDepthScanner] Point cloud cleared");
        }

        if (realtimeScanning)
        {
            Scan(suppressLogs: true);
        }
        else if (OVRInput.GetDown(scanButton, controller))
        {
            Scan(suppressLogs: false);
        }
    }
    
    void Scan(bool suppressLogs = false)
    {
#if UNITY_EDITOR || UNITY_ANDROID
        if (!isInitialized || depthManager == null || !depthManager.IsDepthAvailable)
        {
            if (Time.time - lastWarningTime > WARNING_COOLDOWN)
            {
                Debug.LogWarning("[SimpleDepthScanner] Depth not ready - waiting for depth data");
                lastWarningTime = Time.time;
            }
            return;
        }
        
        // Only clear if NOT accumulating
        if (!accumulatePoints)
        {
            ClearPointCloud();
        }
        
        Vector3[] points = GeneratePointCloud();
        
        if (points != null && points.Length > 0)
        {
            if (!suppressLogs)
            {
                Debug.Log($"[SimpleDepthScanner] Scan complete: {points.Length} points");
            }
            VisualizePoints(points);
            lastWarningTime = 0f; // Reset warning cooldown on success
        }
        else
        {
            // Only show warning with cooldown to avoid spam
            if (Time.time - lastWarningTime > WARNING_COOLDOWN)
            {
                string reason = GetNoPointsReason();
                Debug.LogWarning($"[SimpleDepthScanner] No points generated. {reason}");
                lastWarningTime = Time.time;
            }
        }
#endif
    }
    
    string GetNoPointsReason()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Diagnostics:");

        // Check Manager
        if (depthManager == null) sb.AppendLine("- EnvironmentDepthManager is NULL");
        else sb.AppendLine($"- Manager.IsDepthAvailable: {depthManager.IsDepthAvailable}");

        // Check depth texture
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        if (depthTexGlobal == null)
        {
            sb.AppendLine("- Global Texture '_EnvironmentDepthTexture' is NULL");
        }
        else
        {
            sb.AppendLine($"- Global Texture found: {depthTexGlobal.width}x{depthTexGlobal.height} ({depthTexGlobal.GetType().Name})");
        }
        
        // Check reprojection matrices
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        if (reprojectionMatrices == null || reprojectionMatrices.Length == 0)
        {
            sb.AppendLine("- Global Matrix Array '_EnvironmentDepthReprojectionMatrices' is MISSING or EMPTY");
        }
        else
        {
            sb.AppendLine($"- Reprojection Matrices found: {reprojectionMatrices.Length}");
            if (reprojectionMatrices[0].isIdentity)
            {
                sb.AppendLine("  - [WARNING] Reprojection Matrix is IDENTITY (System not ready)");
            }
            else if (reprojectionMatrices[0] == Matrix4x4.zero)
            {
                sb.AppendLine("  ! WARNING: Matrix[0] is ZERO (Depth API data invalid)");
            }
        }
        
        // Check Camera/Tracking
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
        {
            sb.AppendLine("- OVRCameraRig: NOT FOUND (using Camera.main)");
            if (Camera.main == null) sb.AppendLine("  ! Camera.main is also NULL");
            else sb.AppendLine($"  ! Camera.main pos: {Camera.main.transform.position}");
        }
        else
        {
            sb.AppendLine($"- OVRCameraRig: Found. CenterEyeAnchor pos: {rig.centerEyeAnchor.position}");
            if (rig.centerEyeAnchor.position == Vector3.zero)
            {
                sb.AppendLine("  ! WARNING: Camera is at (0,0,0). Is tracking working?");
            }
        }
        
        // Check RenderTexture
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        if (depthRT == null)
        {
            sb.AppendLine("- Depth texture is NOT a RenderTexture");
        }
        else if (!depthRT.IsCreated())
        {
            sb.AppendLine("- Depth RenderTexture is not created (IsCreated() = false)");
        }
        
        // If we get here, depth data exists but no valid points were found
        sb.AppendLine($"- Scan Settings: MinDepth={minDepth}, MaxDepth={maxDepth}");
        
        return sb.ToString();
#else
        return "Reason: Not supported on this platform";
#endif
    }
    
    Vector3[] GeneratePointCloud()
    {
#if UNITY_EDITOR || UNITY_ANDROID
        // Get depth texture and reprojection matrices
        Texture depthTexGlobal = Shader.GetGlobalTexture(DepthTextureID);
        Matrix4x4[] reprojectionMatrices = Shader.GetGlobalMatrixArray(ReprojectionMatricesID);
        
        if (depthTexGlobal == null || reprojectionMatrices == null || reprojectionMatrices.Length == 0)
            return null;
            
        Matrix4x4 reprojectionMatrix;
        
        // Check for uninitialized matrix (Identity)
        if (reprojectionMatrices[0].isIdentity)
        {
            if (hasValidMatrix)
            {
                // Use fallback
                reprojectionMatrix = lastValidMatrix;
            }
            else
            {
                // No valid data yet
                return null;
            }
        }
        else
        {
            // Valid matrix found
            if (invertReprojectionMatrix)
            {
                reprojectionMatrix = reprojectionMatrices[0].inverse;
            }
            else
            {
                reprojectionMatrix = reprojectionMatrices[0];
            }
            
            lastValidMatrix = reprojectionMatrix;
            hasValidMatrix = true;
        }
        
        // Read Texture
        RenderTexture depthRT = depthTexGlobal as RenderTexture;
        Texture2D depthTexture = ReadRenderTexture(depthRT);
        if (depthTexture == null) return null;
        
        int width = depthTexture.width;
        int height = depthTexture.height;
        Color[] pixels = depthTexture.GetPixels();
        
        if (pixels == null || pixels.Length == 0)
        {
            Destroy(depthTexture);
            return null;
        }
        
        // Dynamic Stride Calculation: Ensure we cover the WHOLE image even with few points
        // Target: Sample 'maxPoints' distributed evenly across 'width * height'
        float totalPixels = width * height;
        float coverageRatio = Mathf.Clamp01((float)maxPoints / totalPixels);
        
        // Calculate stride to hit target count roughly
        // stride = sqrt(1 / ratio). e.g. ratio 0.01 (1%) -> stride 10
        int dynamicStride = Mathf.CeilToInt(1.0f / Mathf.Sqrt(coverageRatio));
        dynamicStride = Mathf.Max(dynamicStride, pointStride); // Respect user minimum stride
        
        // Debug log to explain coverage (only once or if debug enabled)
        // Debug.Log($"[SimpleDepthScanner] Coverage: {maxPoints} points / {totalPixels} pixels. Dynamic Stride: {dynamicStride}");
        
        List<Vector3> points = new List<Vector3>();
        
        // Camera pos for distance filtering
        Vector3 cameraPos = Vector3.zero;
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (Camera.main != null) cameraPos = Camera.main.transform.position;
        else if (cameraRig != null) cameraPos = cameraRig.centerEyeAnchor.position;
        
        // Sample with dynamic stride
        int centerPixelIndex = (height / 2) * width + (width / 2);
        if (centerPixelIndex < pixels.Length)
        {
             float centerDepth = pixels[centerPixelIndex].r;
             // Only log this occasionally to avoid spam
             if (Time.frameCount % 60 == 0)
             {
                 Debug.Log($"[SimpleDepthScanner] Center Pixel Depth: {centerDepth} (Raw). Valid Range: 0-1");
             }
        }

        float minCalcDist = float.MaxValue;
        float maxCalcDist = float.MinValue;

        for (int y = 0; y < height; y += dynamicStride)
        {
            for (int x = 0; x < width; x += dynamicStride)
            {
                if (points.Count >= maxPoints) break;
                
                int pixelIndex = y * width + x;
                if (pixelIndex >= pixels.Length) continue;
                
                float rawDepth = pixels[pixelIndex].r;
                
                // Filter invalid 0/1 values
                if (rawDepth <= 0.0001f || rawDepth >= 0.9999f) continue;
                
                float u = (float)x / width;
                float v = 1.0f - (float)y / height; // Flip V for Unity texture coords
                
                Vector3 worldPos = DepthToWorldSpace(u, v, rawDepth, reprojectionMatrix);
                
                // Validate
                if (float.IsNaN(worldPos.x) || float.IsInfinity(worldPos.x) || worldPos == Vector3.zero) continue;
                
                // Manual Alignment (Fix for Swimming/Roof/Rotation issues)
                if (useManualTransform)
                {
                    // 1. Apply Local Offsets
                    if (rotationOffset != Vector3.zero)
                        worldPos = Quaternion.Euler(rotationOffset) * worldPos;
                    
                    worldPos += positionOffset;
                    
                    // 2. Apply Camera Rotation (Optional)
                    if (applyCameraRotation)
                    {
                        worldPos = Camera.main.transform.rotation * worldPos;
                    }
                    
                    // 3. Apply Camera Position (Fixes Swimming)
                    if (applyCameraPosition)
                    {
                        worldPos += Camera.main.transform.position;
                    }
                }
                
                float dist = Vector3.Distance(worldPos, cameraPos);
                
                // Track stats
                if (dist < minCalcDist) minCalcDist = dist;
                if (dist > maxCalcDist) maxCalcDist = dist;
                
                // Debug filtering (occasional)
                if (Time.frameCount % 120 == 0 && pixelIndex == centerPixelIndex)
                {
                    Debug.Log($"[SimpleDepthScanner] Center Point Dist: {dist:F2}m (Raw: {rawDepth:F4}). Range: {minDepth}-{maxDepth}");
                }

                // TEMPORARILY DISABLE FILTER to see where points are
                if (dist < minDepth || dist > maxDepth) continue;
                
                points.Add(worldPos);
            }
        }
        
        Destroy(depthTexture);

        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[SimpleDepthScanner] Generated {points.Count} points. Dist Range: {minCalcDist:F2}m to {maxCalcDist:F2}m");
        }
        
        // VisualizePoints(points.ToArray()); // Removed to prevent double visualization
        return points.ToArray();
#else
        return null;
#endif
    }
    
    /// <summary>
    /// Converts depth texture sample to world space position.
    /// Pipeline: Screen Space [0,1] → Clip Space [-1,1] → Homogeneous Clip → Homogeneous World → World Space
    /// 
    /// Based on forum discussion:
    /// https://communityforums.atmeta.com/discussions/dev-general/how-can-i-get-a-depth-mapor-point-cloud-just-from-my-quest-3s-depth-sensor/1111934
    /// 
    /// Note: Depth API computes depth from the two main cameras (stereo reconstruction), not the depth sensor directly.
    /// The depth value is in [0,1] range in the R channel of the texture.
    /// </summary>
    /// <param name="u">Texture U coordinate [0,1]</param>
    /// <param name="v">Texture V coordinate [0,1]</param>
    /// <param name="depth">Depth value from texture [0,1]</param>
    /// <param name="reprojectionMatrix">Inverse reprojection matrix (reprojectionMatrices[0].inverse)</param>
    /// <returns>World space position</returns>
    /// <summary>
    /// Converts depth texture sample to world space position.
    /// </summary>
    Vector3 DepthToWorldSpace(float u, float v, float rawDepth, Matrix4x4 reprojectionMatrix)
    {
        // Direct Unprojection using the Inverse View-Projection Matrix.
        // The matrix expects NDC coordinates.
        // Our Unproject helper converts u,v,rawDepth (0..1) to NDC (-1..1).
        
        float z = rawDepth;
        if (reverseZ)
        {
            z = 1.0f - rawDepth;
        }
        
        Vector3 worldPos = Unproject(u, v, z, reprojectionMatrix);
        
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        Vector3 dir = worldPos - camPos;
        float rawDist = dir.magnitude;
        
        // Apply manual depth scale (Fixes "20cm vs 1m" gross error)
        if (depthScale != 1.0f)
        {
            worldPos = camPos + dir * depthScale;
            // Recalculate for bias
            dir = worldPos - camPos;
            rawDist = dir.magnitude;
        }
        
        // Apply Environment Depth Bias (Fixes Z-Fighting/Fine Error)
        // Docs: "offset is calculated towards the camera... scales linearly with metric distance"
        if (depthBias != 0.0f)
        {
            // Offset = distance * bias
            float offset = rawDist * depthBias;
            // Move towards camera
            worldPos -= dir.normalized * offset;
        }
        
        return worldPos;
    }
    
    // ... (existing code) ...

    Vector3 Unproject(float u, float v, float z, Matrix4x4 invMatrix)
    {
        Vector3 screenPos = new Vector3(u, v, z);
        
        // Convert X,Y to NDC [-1, 1]
        Vector3 clipPos;
        clipPos.x = screenPos.x * 2.0f - 1.0f;
        clipPos.y = screenPos.y * 2.0f - 1.0f;
        
        // Handle Z convention
        if (useNDC_Neg1to1)
        {
            clipPos.z = screenPos.z * 2.0f - 1.0f; // OpenGL style
        }
        else
        {
            clipPos.z = screenPos.z; // Vulkan/DirectX style (0..1)
        }
        
        Vector4 hClip = new Vector4(clipPos.x, clipPos.y, clipPos.z, 1.0f);
        Vector4 hWorld = invMatrix * hClip;
        if (Mathf.Abs(hWorld.w) < 0.00001f) return Vector3.zero;
        return new Vector3(hWorld.x, hWorld.y, hWorld.z) / hWorld.w;
    }
    
    Texture2D ReadRenderTexture(RenderTexture sourceRT)
    {
        if (sourceRT == null || !sourceRT.IsCreated())
            return null;
            
        // Create a temporary destination RenderTexture that is definitely readable
        // RFloat is best for depth, but some platforms might prefer RHalf or ARGBFloat
        RenderTexture tempRT = RenderTexture.GetTemporary(
            sourceRT.width, 
            sourceRT.height, 
            0, 
            RenderTextureFormat.RFloat, 
            RenderTextureReadWrite.Linear
        );
        
        // Blit source to temp. This handles format conversion (e.g. Depth -> Color)
        Graphics.Blit(sourceRT, tempRT);
        
        // Read from the temp RT
        RenderTexture.active = tempRT;
        
        Texture2D tex = new Texture2D(sourceRT.width, sourceRT.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, sourceRT.width, sourceRT.height), 0, 0);
        tex.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);
        
        return tex;
    }
    
    // Visualization
    private ParticleSystem pointCloudParticles;
    private List<Vector3> accumulatedPointsList = new List<Vector3>();
    
    void VisualizePoints(Vector3[] newPoints)
    {
        if (newPoints == null || newPoints.Length == 0)
            return;
        
        if (pointCloudParticles == null)
        {
            CreateParticleSystem();
        }
        
        Vector3[] pointsToRender;

        if (accumulatePoints)
        {
            accumulatedPointsList.AddRange(newPoints);
            
            // Safety cap: prevent infinite memory growth (cap at 500k points)
            if (accumulatedPointsList.Count > 500000) 
            {
                accumulatedPointsList.RemoveRange(0, accumulatedPointsList.Count - 500000);
            }
            
            pointsToRender = accumulatedPointsList.ToArray();
        }
        else
        {
            // If we just switched off accumulation, clear the buffer
            if (accumulatedPointsList.Count > 0) accumulatedPointsList.Clear();
            pointsToRender = newPoints;
        }
        
        // Update particle system capacity if needed
        var main = pointCloudParticles.main;
        if (main.maxParticles < pointsToRender.Length)
        {
            main.maxParticles = pointsToRender.Length;
        }
        
        main.startSize = pointSize;
        main.startColor = pointColor;
        
        // Force bounds to prevent culling
        var renderer = pointCloudParticles.GetComponent<ParticleSystemRenderer>();
        renderer.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        
        // Create particles array
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[pointsToRender.Length];
        for (int i = 0; i < pointsToRender.Length; i++)
        {
            particles[i].position = pointsToRender[i];
            particles[i].startColor = pointColor;
            particles[i].startSize = pointSize;
            particles[i].remainingLifetime = float.MaxValue;
        }
        
        pointCloudParticles.SetParticles(particles, pointsToRender.Length);
    }
    
    void ClearPointCloud()
    {
        accumulatedPointsList.Clear();
        if (pointCloudParticles != null)
        {
            pointCloudParticles.Clear();
        }
    }
    
    void OnDestroy()
    {
        ClearPointCloud();
    }

    private void CreateParticleSystem()
    {
        GameObject obj = new GameObject("PointCloudParticles");
        pointCloudParticles = obj.AddComponent<ParticleSystem>();
        
        // Configure Particle System
        var main = pointCloudParticles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxPoints; 
        main.startSize = pointSize;
        main.startColor = pointColor;
        
        var emission = pointCloudParticles.emission;
        emission.enabled = false;
        
        var shape = pointCloudParticles.shape;
        shape.enabled = false;
        
        // Setup Renderer
        var renderer = pointCloudParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Use assigned material or fallback
        if (particleMaterial != null)
        {
            renderer.material = particleMaterial;
        }
        else
        {
            // Fallback to code-based shader finding (might fail on Quest if stripped)
            // Try Mobile/Particles/Additive which is usually always included
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            
            if (shader != null)
            {
                renderer.material = new Material(shader);
            }
            else
            {
                // Last resort: just use a default material if possible, or warn
                Debug.LogWarning("[SimpleDepthScanner] Could not find particle shader. Please assign 'ParticleMaterial' in Inspector.");
            }
        }
    }
}
