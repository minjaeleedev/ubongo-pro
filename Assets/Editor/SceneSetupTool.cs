using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

namespace Ubongo.Editor
{
    /// <summary>
    /// Unity 에디터에서 Ubongo 3D 씬을 자동 설정하는 도구
    /// Window > Ubongo 3D > Setup Scene 메뉴에서 실행
    /// </summary>
    public class SceneSetupTool : EditorWindow
    {
        private bool createGameSystems = true;
        private bool createMaterials = true;
        private bool createUI = true;
        private bool connectReferences = true;

        [MenuItem("Window/Ubongo 3D/Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupTool>("Ubongo 3D Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Ubongo 3D Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label("Select components to create:", EditorStyles.label);
            EditorGUILayout.Space();

            createGameSystems = EditorGUILayout.Toggle("Game Systems", createGameSystems);
            createMaterials = EditorGUILayout.Toggle("Materials", createMaterials);
            createUI = EditorGUILayout.Toggle("UI Panels", createUI);
            connectReferences = EditorGUILayout.Toggle("Connect References", connectReferences);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
            {
                SetupScene();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Materials Only", GUILayout.Height(30)))
            {
                CreateMaterials();
            }

            if (GUILayout.Button("Create Game Systems Only", GUILayout.Height(30)))
            {
                CreateGameSystems();
            }

            if (GUILayout.Button("Create UI Only", GUILayout.Height(30)))
            {
                CreateUI();
            }
        }

        private void SetupScene()
        {
            if (createMaterials)
            {
                CreateMaterials();
            }

            if (createGameSystems)
            {
                CreateGameSystems();
            }

            if (createUI)
            {
                CreateUI();
            }

            if (connectReferences)
            {
                ConnectReferences();
            }

            Debug.Log("[SceneSetupTool] Scene setup completed!");
            EditorUtility.DisplayDialog("Scene Setup", "Ubongo 3D scene setup completed successfully!", "OK");
        }

        private static void CreateMaterials()
        {
            string materialsPath = "Assets/Materials";

            if (!AssetDatabase.IsValidFolder(materialsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // CellDefault - Gray
            CreateMaterialIfNotExists($"{materialsPath}/CellDefault.mat", new Color(0.5f, 0.5f, 0.5f), false);

            // CellValid - Light Green, Semi-transparent
            CreateMaterialIfNotExists($"{materialsPath}/CellValid.mat", new Color(0.56f, 0.93f, 0.56f, 0.5f), true);

            // CellInvalid - Light Red, Semi-transparent
            CreateMaterialIfNotExists($"{materialsPath}/CellInvalid.mat", new Color(1f, 0.42f, 0.42f, 0.5f), true);

            // CellTarget - Yellow
            CreateMaterialIfNotExists($"{materialsPath}/CellTarget.mat", new Color(1f, 0.84f, 0f), false);

            // PieceOutline - White
            CreateMaterialIfNotExists($"{materialsPath}/PieceOutline.mat", Color.white, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneSetupTool] Materials created at Assets/Materials/");
        }

        private static void CreateMaterialIfNotExists(string path, Color color, bool isTransparent)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                Debug.Log($"[SceneSetupTool] Material already exists: {path}");
                return;
            }

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;

            if (isTransparent)
            {
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(mat, path);
            Debug.Log($"[SceneSetupTool] Created material: {path}");
        }

        private static void CreateGameSystems()
        {
            // Find or create parent object
            GameObject gameSystems = GameObject.Find("GameSystems");
            if (gameSystems == null)
            {
                gameSystems = new GameObject("GameSystems");
                Debug.Log("[SceneSetupTool] Created GameSystems parent object");
            }

            // Create system objects with their scripts
            CreateSystemObject<Ubongo.LevelGenerator>(gameSystems.transform, "LevelGenerator");
            CreateSystemObject<Ubongo.Systems.GemSystem>(gameSystems.transform, "GemSystem");
            CreateSystemObject<Ubongo.Systems.RoundManager>(gameSystems.transform, "RoundManager");
            CreateSystemObject<Ubongo.Systems.DifficultySystem>(gameSystems.transform, "DifficultySystem");
            CreateSystemObject<Ubongo.Systems.TiebreakerManager>(gameSystems.transform, "TiebreakerManager");

            Debug.Log("[SceneSetupTool] Game systems created under GameSystems object");
        }

        private static void CreateSystemObject<T>(Transform parent, string name) where T : Component
        {
            // Check if already exists
            Transform existing = parent.Find(name);
            if (existing != null && existing.GetComponent<T>() != null)
            {
                Debug.Log($"[SceneSetupTool] {name} already exists");
                return;
            }

            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            obj.transform.SetParent(parent);

            if (obj.GetComponent<T>() == null)
            {
                obj.AddComponent<T>();
            }

            Debug.Log($"[SceneSetupTool] Created {name} with {typeof(T).Name} component");
        }

        private static void CreateUI()
        {
            // Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[SceneSetupTool] Created Canvas");
            }

            // Add UIManager if not present
            if (canvas.GetComponent<Ubongo.UIManager>() == null)
            {
                canvas.gameObject.AddComponent<Ubongo.UIManager>();
                Debug.Log("[SceneSetupTool] Added UIManager to Canvas");
            }

            // Create UI Panels
            CreateUIPanel(canvas.transform, "MenuPanel", true);
            CreateUIPanel(canvas.transform, "GamePanel", false);
            CreateUIPanel(canvas.transform, "PausePanel", false);
            CreateUIPanel(canvas.transform, "GameOverPanel", false);
            CreateUIPanel(canvas.transform, "LevelCompletePanel", false);

            // Create menu panel contents
            CreateMenuPanelContents(canvas.transform.Find("MenuPanel"));

            // Create game panel contents
            CreateGamePanelContents(canvas.transform.Find("GamePanel"));

            // Create pause panel contents
            CreatePausePanelContents(canvas.transform.Find("PausePanel"));

            // Create game over panel contents
            CreateGameOverPanelContents(canvas.transform.Find("GameOverPanel"));

            // Create level complete panel contents
            CreateLevelCompletePanelContents(canvas.transform.Find("LevelCompletePanel"));

            Debug.Log("[SceneSetupTool] UI panels created");
        }

        private static void CreateUIPanel(Transform parent, string name, bool active)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Debug.Log($"[SceneSetupTool] {name} already exists");
                return;
            }

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);

            panel.SetActive(active);

            Debug.Log($"[SceneSetupTool] Created UI Panel: {name}");
        }

        private static void CreateMenuPanelContents(Transform panel)
        {
            if (panel == null) return;

            // Title
            CreateText(panel, "TitleText", "Ubongo 3D", new Vector2(0, 100), 48);

            // Start Button
            CreateButton(panel, "StartButton", "Start Game", new Vector2(0, 0));

            // Quit Button
            CreateButton(panel, "QuitButton", "Quit", new Vector2(0, -80));

            // Total Gems Text
            CreateText(panel, "TotalGemsText", "Gems: 0", new Vector2(0, -160), 24);
        }

        private static void CreateGamePanelContents(Transform panel)
        {
            if (panel == null) return;

            // Timer
            CreateText(panel, "TimerText", "01:30", new Vector2(0, 200), 36, TextAnchor.UpperCenter);

            // Score
            CreateText(panel, "ScoreText", "Score: 0", new Vector2(200, 200), 24, TextAnchor.UpperRight);

            // Level
            CreateText(panel, "LevelText", "Level 1", new Vector2(-200, 200), 24, TextAnchor.UpperLeft);

            // Round
            CreateText(panel, "RoundText", "Round 1/9", new Vector2(0, 170), 20, TextAnchor.UpperCenter);

            // Gem Counter
            CreateText(panel, "GemCounterText", "0", new Vector2(200, 170), 20, TextAnchor.UpperRight);

            // Difficulty
            CreateText(panel, "DifficultyText", "Easy", new Vector2(-200, 170), 20, TextAnchor.UpperLeft);

            // Pause Button
            CreateButton(panel, "PauseButton", "||", new Vector2(250, 200), new Vector2(50, 50));
        }

        private static void CreatePausePanelContents(Transform panel)
        {
            if (panel == null) return;

            // Resume Button
            CreateButton(panel, "ResumeButton", "Resume", new Vector2(0, 40));

            // Restart Button
            CreateButton(panel, "RestartButton", "Restart", new Vector2(0, -40));

            // Menu Button
            CreateButton(panel, "MenuButton", "Main Menu", new Vector2(0, -120));
        }

        private static void CreateGameOverPanelContents(Transform panel)
        {
            if (panel == null) return;

            // Title
            CreateText(panel, "GameOverTitle", "Game Over", new Vector2(0, 100), 48);

            // Final Score
            CreateText(panel, "FinalScoreText", "Final Score: 0", new Vector2(0, 20), 28);

            // Final Gems
            CreateText(panel, "FinalGemsText", "Gems Collected: 0", new Vector2(0, -20), 24);

            // Reached Round
            CreateText(panel, "ReachedRoundText", "Reached Round: 1/9", new Vector2(0, -60), 24);

            // Retry Button
            CreateButton(panel, "RetryButton", "Retry", new Vector2(-80, -140));

            // Main Menu Button
            CreateButton(panel, "MainMenuButton", "Main Menu", new Vector2(80, -140));
        }

        private static void CreateLevelCompletePanelContents(Transform panel)
        {
            if (panel == null) return;

            // Title
            CreateText(panel, "LevelCompleteTitle", "Level Complete!", new Vector2(0, 100), 48);

            // Score
            CreateText(panel, "LevelCompleteScoreText", "Score: 0", new Vector2(0, 20), 28);

            // Gems Earned
            CreateText(panel, "RoundGemsText", "Gems Earned: 0", new Vector2(0, -20), 24);

            // Bonus Score
            CreateText(panel, "BonusScoreText", "Time Bonus: +0", new Vector2(0, -60), 24);

            // Next Level Button
            CreateButton(panel, "NextLevelButton", "Next Level", new Vector2(0, -140));
        }

        private static void CreateText(Transform parent, string name, string text, Vector2 position, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            if (parent.Find(name) != null) return;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 60);

            Text textComp = obj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = Color.white;
        }

        private static void CreateButton(Transform parent, string name, string text, Vector2 position, Vector2? size = null)
        {
            if (parent.Find(name) != null) return;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size ?? new Vector2(200, 60);

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.9f);

            Button button = obj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.8f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 24;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
        }

        private static void ConnectReferences()
        {
            // Find UIManager and connect panel references
            var uiManager = Object.FindObjectOfType<Ubongo.UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[SceneSetupTool] UIManager not found - cannot connect references");
                return;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Use SerializedObject to set private serialized fields
            SerializedObject serializedUI = new SerializedObject(uiManager);

            SetSerializedReference(serializedUI, "menuPanel", canvas.transform.Find("MenuPanel")?.gameObject);
            SetSerializedReference(serializedUI, "gamePanel", canvas.transform.Find("GamePanel")?.gameObject);
            SetSerializedReference(serializedUI, "pausePanel", canvas.transform.Find("PausePanel")?.gameObject);
            SetSerializedReference(serializedUI, "gameOverPanel", canvas.transform.Find("GameOverPanel")?.gameObject);
            SetSerializedReference(serializedUI, "levelCompletePanel", canvas.transform.Find("LevelCompletePanel")?.gameObject);

            // Connect UI elements
            Transform gamePanel = canvas.transform.Find("GamePanel");
            if (gamePanel != null)
            {
                SetSerializedReference(serializedUI, "timerText", gamePanel.Find("TimerText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "scoreText", gamePanel.Find("ScoreText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "levelText", gamePanel.Find("LevelText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "roundText", gamePanel.Find("RoundText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "gemCounterText", gamePanel.Find("GemCounterText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "difficultyText", gamePanel.Find("DifficultyText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "pauseButton", gamePanel.Find("PauseButton")?.GetComponent<Button>());
            }

            Transform menuPanel = canvas.transform.Find("MenuPanel");
            if (menuPanel != null)
            {
                SetSerializedReference(serializedUI, "startButton", menuPanel.Find("StartButton")?.GetComponent<Button>());
                SetSerializedReference(serializedUI, "quitButton", menuPanel.Find("QuitButton")?.GetComponent<Button>());
                SetSerializedReference(serializedUI, "totalGemsText", menuPanel.Find("TotalGemsText")?.GetComponent<Text>());
            }

            Transform pausePanel = canvas.transform.Find("PausePanel");
            if (pausePanel != null)
            {
                SetSerializedReference(serializedUI, "resumeButton", pausePanel.Find("ResumeButton")?.GetComponent<Button>());
                SetSerializedReference(serializedUI, "restartButton", pausePanel.Find("RestartButton")?.GetComponent<Button>());
                SetSerializedReference(serializedUI, "menuButton", pausePanel.Find("MenuButton")?.GetComponent<Button>());
            }

            Transform gameOverPanel = canvas.transform.Find("GameOverPanel");
            if (gameOverPanel != null)
            {
                SetSerializedReference(serializedUI, "finalScoreText", gameOverPanel.Find("FinalScoreText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "finalGemsText", gameOverPanel.Find("FinalGemsText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "reachedRoundText", gameOverPanel.Find("ReachedRoundText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "retryButton", gameOverPanel.Find("RetryButton")?.GetComponent<Button>());
                SetSerializedReference(serializedUI, "mainMenuButton", gameOverPanel.Find("MainMenuButton")?.GetComponent<Button>());
            }

            Transform levelCompletePanel = canvas.transform.Find("LevelCompletePanel");
            if (levelCompletePanel != null)
            {
                SetSerializedReference(serializedUI, "levelCompleteScoreText", levelCompletePanel.Find("LevelCompleteScoreText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "roundGemsText", levelCompletePanel.Find("RoundGemsText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "bonusScoreText", levelCompletePanel.Find("BonusScoreText")?.GetComponent<Text>());
                SetSerializedReference(serializedUI, "nextLevelButton", levelCompletePanel.Find("NextLevelButton")?.GetComponent<Button>());
            }

            serializedUI.ApplyModifiedProperties();

            // Connect GameManager references
            var gameManager = Object.FindObjectOfType<Ubongo.GameManager>();
            if (gameManager != null)
            {
                SerializedObject serializedGM = new SerializedObject(gameManager);

                var gemSystem = Object.FindObjectOfType<Ubongo.Systems.GemSystem>();
                var roundManager = Object.FindObjectOfType<Ubongo.Systems.RoundManager>();
                var difficultySystem = Object.FindObjectOfType<Ubongo.Systems.DifficultySystem>();
                var tiebreakerManager = Object.FindObjectOfType<Ubongo.Systems.TiebreakerManager>();

                SetSerializedReference(serializedGM, "gemSystem", gemSystem);
                SetSerializedReference(serializedGM, "roundManager", roundManager);
                SetSerializedReference(serializedGM, "difficultySystem", difficultySystem);
                SetSerializedReference(serializedGM, "tiebreakerManager", tiebreakerManager);

                serializedGM.ApplyModifiedProperties();
            }

            Debug.Log("[SceneSetupTool] References connected");
        }

        private static void SetSerializedReference(SerializedObject obj, string propertyName, Object value)
        {
            SerializedProperty prop = obj.FindProperty(propertyName);
            if (prop != null && value != null)
            {
                prop.objectReferenceValue = value;
            }
        }
    }
}
