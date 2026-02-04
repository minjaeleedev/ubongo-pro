using UnityEngine;
using UnityEditor;

namespace Ubongo.Editor
{
    /// <summary>
    /// 카메라 및 조명 설정 도구
    /// </summary>
    public class CameraSetupTool : EditorWindow
    {
        [MenuItem("Window/Ubongo 3D/Setup Camera & Lighting")]
        public static void SetupCameraAndLighting()
        {
            // Setup Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
            }

            // Position camera for isometric-like view of the puzzle board
            mainCamera.transform.position = new Vector3(0, 10, -10);
            mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;

            Debug.Log("[CameraSetupTool] Main Camera configured");

            // Setup Directional Light
            Light directionalLight = null;
            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (directionalLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            directionalLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            directionalLight.color = Color.white;
            directionalLight.intensity = 1.0f;
            directionalLight.shadows = LightShadows.Soft;

            Debug.Log("[CameraSetupTool] Directional Light configured");

            // Setup ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f);

            Debug.Log("[CameraSetupTool] Ambient lighting configured");

            EditorUtility.DisplayDialog("Camera Setup", "Camera and lighting configured successfully!", "OK");
        }

        [MenuItem("Window/Ubongo 3D/Create GameManager")]
        public static void CreateGameManager()
        {
            // Check if GameManager already exists
            var existingGM = Object.FindObjectOfType<GameManager>();
            if (existingGM != null)
            {
                Debug.Log("[CameraSetupTool] GameManager already exists in scene");
                Selection.activeGameObject = existingGM.gameObject;
                return;
            }

            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            Debug.Log("[CameraSetupTool] GameManager created");
            Selection.activeGameObject = gmObj;

            EditorUtility.DisplayDialog("GameManager", "GameManager created successfully!", "OK");
        }

        [MenuItem("Window/Ubongo 3D/Setup Layers")]
        public static void SetupLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            // Layer 8: Board
            SetLayer(layers, 8, "Board");

            // Layer 9: Piece
            SetLayer(layers, 9, "Piece");

            // Layer 10: UI (usually already exists)
            SetLayer(layers, 10, "UI");

            tagManager.ApplyModifiedProperties();

            Debug.Log("[CameraSetupTool] Layers configured: Board (8), Piece (9)");
            EditorUtility.DisplayDialog("Layers Setup", "Layers configured:\n- Layer 8: Board\n- Layer 9: Piece", "OK");
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = name;
            }
        }

        [MenuItem("Window/Ubongo 3D/Setup Everything")]
        public static void SetupEverything()
        {
            SetupLayers();
            SetupCameraAndLighting();
            CreateGameManager();
            SceneSetupTool.ShowWindow();
        }
    }
}
