---
description: "Unity .unity Scene 파일 YAML 구조 및 변환 공식 레퍼런스. Scene 파일 편집 시 참조."
---

# Unity Scene File Editing Reference

## Finding the Main Camera in .unity Files

1. Search for `m_TagString: MainCamera` to find the camera GameObject
2. Note the GameObject's `m_Component` list — it references Transform and Camera components by fileID
3. The Transform component (type 4) contains position/rotation
4. The Camera component (type 20) contains orthographic size

## Camera Transform Fields

```yaml
Transform:
  m_LocalPosition: {x: 0, y: 10, z: -7}
  m_LocalRotation: {x: 0.30070582, y: 0, z: 0, w: 0.95371693}
  m_LocalEulerAnglesHint: {x: 35, y: 0, z: 0}
```

- `m_LocalPosition` — world position (no parent = local == world)
- `m_LocalRotation` — quaternion `{x, y, z, w}` (this is what Unity actually uses)
- `m_LocalEulerAnglesHint` — euler angles for inspector display only (but should match quaternion)

## Camera Component Fields

```yaml
Camera:
  orthographic: 1
  orthographic size: 5
  near clip plane: 0.1
  far clip plane: 100
```

## Euler to Quaternion Conversion

### X-axis only rotation (most common for isometric camera)

```
half = angle_degrees * PI / 360
qx = sin(half)
qy = 0
qz = 0
qw = cos(half)
```

### Common Angles Reference Table

| Euler X | qx | qw | Notes |
|---|---|---|---|
| 30° | 0.25881905 | 0.96592583 | Gentle angle |
| 35° | 0.30070582 | 0.95371693 | **Current project value** |
| 40° | 0.34202014 | 0.93969262 | |
| 45° | 0.38268343 | 0.92387953 | Classic isometric |
| 50° | 0.42261826 | 0.90630779 | Steeper view |
| 55° | 0.46174861 | 0.88701083 | |
| 60° | 0.50000000 | 0.86602540 | Top-down-ish |

### Combined X + Y rotation

```
half_x = x_degrees * PI / 360
half_y = y_degrees * PI / 360
qx = sin(half_x) * cos(half_y)
qy = cos(half_x) * sin(half_y)
qz = -sin(half_x) * sin(half_y)
qw = cos(half_x) * cos(half_y)
```

### Camera Position Rule of Thumb

For orthographic camera looking down at angle X:
- Higher Y = sees more from above
- More negative Z = further behind the board
- Typical ratio: `Z ≈ -Y * tan(90° - X°)`
- At 35°: `Z ≈ -Y * tan(55°) ≈ -Y * 1.428` → Y=10, Z≈-7 (matches current)

## Directional Light Transform

```yaml
Transform:
  m_LocalRotation: {x: qx, y: qy, z: qz, w: qw}
  m_LocalEulerAnglesHint: {x: ex, y: ey, z: ez}
```

Same quaternion conversion applies. Typical directional light: Euler `(50, -30, 0)`.

## YAML Editing Rules

1. **Preserve exact fileID references** — never change `--- !u!` headers or `m_ObjectHideFlags`
2. **Use correct precision** — Unity uses up to 8 decimal places for quaternions
3. **Both rotation fields must match** — `m_LocalRotation` (quaternion) and `m_LocalEulerAnglesHint` (euler) must represent the same rotation
4. **Indentation matters** — YAML uses 2-space indentation in Unity files
5. **No trailing spaces** — Unity is sensitive to whitespace changes

## Workflow: Updating Camera in Scene File

1. Calculate new quaternion from desired euler angles using the formula above
2. Search for `m_TagString: MainCamera` in the .unity file
3. Find the corresponding Transform component
4. Update `m_LocalPosition`, `m_LocalRotation`, and `m_LocalEulerAnglesHint`
5. If changing ortho size, also update the Camera component's `orthographic size`
6. Update `GameManager.SetupCamera()` and `CameraSetupTool.cs` with matching values
