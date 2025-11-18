# Gaussian Mapping Multi-Angle Scanning System

This system implements multi-angle table scanning with Gaussian splatting (or mesh reconstruction) for accurate 3D object representation in Meta Quest VR.

## Overview

The scanning system guides users to capture a table from multiple viewpoints (8-12 angles), then processes the depth data to create 3D representations of objects on the table. The system replaces the EffectMesh visualization from MRUK with an interactive scanning workflow.

## Components

### Core Scripts

1. **TableScanHelper.cs** - Works with MRUK to detect tables and guide users to multiple scanning positions
2. **MultiViewCapture.cs** - Captures RGB + Depth from multiple viewpoints around the table
3. **DepthCapture.cs** - Integrates with EnvironmentDepthManager to capture and project depth data
4. **ObjectSegmentation.cs** - Uses DBSCAN clustering to separate objects from point cloud
5. **GaussianGenerator.cs** - Generates 3D representations (mesh or Gaussian splats) from segmented objects
6. **ScanningWorkflow.cs** - Main orchestrator managing the scanning state machine

### UI Components

1. **ScanningUI.cs** - Main UI with visual guides and instructions
2. **ScanningProgressIndicator.cs** - Progress bar and view quality feedback

### Object Components

1. **ScannedObjectPrefab.cs** - Component for scanned objects with OVRGrabbable interaction

## Setup Instructions

### 1. Scene Setup

1. Ensure MRUK is set up in your scene (MRUK prefab should be present)
2. Add the scanning components to a GameObject in your scene:
   - `ScanningWorkflow` (main orchestrator)
   - `TableScanHelper`
   - `MultiViewCapture`
   - `DepthCapture`
   - `ObjectSegmentation`
   - `GaussianGenerator`

### 2. Component Configuration

**ScanningWorkflow:**
- Assign all component references
- Set `autoStartScanning` if you want automatic table selection
- Configure `startScanKey` for keyboard input (for testing)

**TableScanHelper:**
- Set `targetViewCount` (default: 8 views)
- Configure `minViewDistance` and `maxViewDistance`
- Set `minCoverageThreshold` (default: 0.7 = 70%)

**MultiViewCapture:**
- Assign `PassthroughCameraAccess` reference
- Assign `DepthCapture` reference
- Assign `TableScanHelper` reference
- Set `captureInterval` (time between captures)

**DepthCapture:**
- Ensure EnvironmentDepthManager is in scene (will auto-create if missing)
- Configure depth filtering parameters

### 3. UI Setup

1. Create a Canvas for UI elements
2. Add `ScanningUI` component to a UI GameObject
3. Create UI elements:
   - TextMeshProUGUI for instructions
   - Slider for progress bar
   - TextMeshProUGUI for view count and coverage
4. Assign references in `ScanningUI` component
5. Create a prefab for position indicators (simple sphere or cube)

### 4. Materials

Create materials for:
- `recommendedPositionMaterial` - For positions not yet captured
- `capturedPositionMaterial` - For positions already captured
- `currentPositionMaterial` - For the current recommended position

## Usage

### Starting a Scan

1. **Automatic**: Set `autoStartScanning = true` in `ScanningWorkflow`
2. **Manual**: Call `StartTableSelection()` on `ScanningWorkflow`
3. **Keyboard**: Press the configured key (default: Space)

### Scanning Process

1. System detects available tables via MRUK
2. User selects a table (or first table is auto-selected)
3. System calculates 8 recommended positions around the table
4. Visual indicators show recommended positions
5. User moves to each position
6. System automatically captures views when user is in position
7. Progress indicator shows coverage percentage
8. When complete (8 views + 70% coverage), processing begins
9. Objects are segmented and 3D representations are created
10. User can interact with scanned objects (grab, move)

### Interacting with Scanned Objects

- Scanned objects have `OVRGrabbable` component for hand tracking
- Objects can be grabbed and moved around
- Objects maintain their original position for reset functionality

## Integration with Existing Systems

The scanning system integrates with:
- **MRUK**: Uses existing table detection (replaces EffectMesh visualization)
- **ObjectDetector/ObjectRenderer**: Can be enhanced to work with scanned objects
- **PassthroughCameraAccess**: Uses existing camera access
- **EnvironmentDepthManager**: Uses Meta SDK depth API

## Performance Considerations

- **Mesh Fallback**: By default, uses mesh reconstruction instead of full Gaussian splatting for Quest performance
- **Voxel Downsampling**: Reduces point cloud density
- **Async Processing**: Object generation happens in coroutines to avoid frame drops
- **LOD System**: Can be added for distance-based quality reduction

## Customization

### Changing View Count
Edit `targetViewCount` in `TableScanHelper` (4-16 views supported)

### Using Full Gaussian Splatting
Set `useMeshFallback = false` in `GaussianGenerator` (requires custom splat renderer)

### Adjusting Clustering
Modify `epsilon` and `minPoints` in `ObjectSegmentation` for different object separation

## Troubleshooting

### No Tables Detected
- Ensure MRUK scene is loaded
- Check that tables have TABLE label in MRUK
- Wait for MRUK.SceneLoadedEvent

### Depth Not Available
- Ensure EnvironmentDepthManager is in scene
- Check Quest 3/Pro depth API support
- Verify permissions are granted

### Poor Object Segmentation
- Adjust `epsilon` in ObjectSegmentation (larger = more points per cluster)
- Increase `minPoints` threshold
- Ensure sufficient coverage (70%+)

### Low Frame Rate
- Reduce `targetSplatCount` in GaussianGenerator
- Use mesh fallback (default)
- Reduce view count
- Increase `captureInterval` in MultiViewCapture

## Future Enhancements

- Full Gaussian Splat rendering with compute shaders
- Server-side processing for higher quality
- Save/load scan data
- Multiple table support
- Real-time preview during scanning
- Integration with ObjectRenderer for hybrid detection

