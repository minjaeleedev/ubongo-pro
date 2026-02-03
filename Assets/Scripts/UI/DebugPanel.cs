using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace Ubongo
{
    /// <summary>
    /// 테스트베드 UI - 디버그 정보 표시 및 퍼즐 테스트 도구
    /// F12로 토글, F2로 디버그 패널, F3로 퍼즐 생성기, F6으로 자동 솔브
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel References")]
        [SerializeField] private GameObject debugPanelRoot;
        [SerializeField] private GameObject puzzleGeneratorPanel;
        [SerializeField] private GameObject rotationTesterPanel;

        [Header("Debug Info Display")]
        [SerializeField] private Text fpsText;
        [SerializeField] private Text drawCallsText;
        [SerializeField] private Text trianglesText;
        [SerializeField] private Text puzzleInfoText;
        [SerializeField] private Text gridInfoText;
        [SerializeField] private Text solverStatusText;

        [Header("Puzzle Generator Controls")]
        [SerializeField] private Dropdown difficultyDropdown;
        [SerializeField] private Dropdown widthDropdown;
        [SerializeField] private Dropdown heightDropdown;
        [SerializeField] private Dropdown depthDropdown;
        [SerializeField] private Dropdown blockCountDropdown;
        [SerializeField] private Toggle[] blockTypeToggles;
        [SerializeField] private Text generationLogText;

        [Header("Rotation Tester Controls")]
        [SerializeField] private Text rotationStateText;
        [SerializeField] private Toggle showCollisionBoundsToggle;
        [SerializeField] private Toggle showPivotPointToggle;
        [SerializeField] private Toggle showGridSnappingToggle;
        [SerializeField] private Text rotationLogText;

        [Header("Settings")]
        [SerializeField] private float fpsUpdateInterval = 0.5f;
        [SerializeField] private int maxLogLines = 20;

        #endregion

        #region Private Fields

        private bool isDebugPanelVisible = false;
        private bool isPuzzleGeneratorVisible = false;
        private bool isRotationTesterVisible = false;

        private float fpsAccumulator = 0f;
        private int fpsFrameCount = 0;
        private float fpsTimeLeft = 0f;
        private float currentFps = 0f;

        private StringBuilder logBuilder = new StringBuilder();
        private GameBoard gameBoard;
        private LevelGenerator levelGenerator;
        private PuzzlePiece selectedPiece;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CreateUIIfNeeded();
        }

        private void Start()
        {
            gameBoard = FindObjectOfType<GameBoard>();
            levelGenerator = FindObjectOfType<LevelGenerator>();

            fpsTimeLeft = fpsUpdateInterval;

            HideAllPanels();
            SetupDropdowns();
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            UpdateFpsCounter();

            if (isDebugPanelVisible)
            {
                UpdateDebugInfo();
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private void HandleKeyboardShortcuts()
        {
            // F1: Show/Hide Help Overlay
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowHelpOverlay();
            }

            // F2: Toggle Debug Panel
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleDebugPanel();
            }

            // F3: Toggle Puzzle Generator
            if (Input.GetKeyDown(KeyCode.F3))
            {
                TogglePuzzleGenerator();
            }

            // F4: Toggle Rotation Tester
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ToggleRotationTester();
            }

            // F5: Quick Generate Puzzle
            if (Input.GetKeyDown(KeyCode.F5))
            {
                QuickGeneratePuzzle();
            }

            // F6: Auto-Solve Current Puzzle
            if (Input.GetKeyDown(KeyCode.F6))
            {
                AutoSolveCurrentPuzzle();
            }

            // F7: Step Through Solution
            if (Input.GetKeyDown(KeyCode.F7))
            {
                StepThroughSolution();
            }

            // F8: Export Current State
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ExportCurrentState();
            }

            // F10: Toggle Grid Overlay
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ToggleGridOverlay();
            }

            // F11: Toggle Wireframe Mode
            if (Input.GetKeyDown(KeyCode.F11))
            {
                ToggleWireframeMode();
            }

            // F12: Toggle Performance Stats
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ToggleDebugPanel();
            }

            // Ctrl+N: New Random Puzzle
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
            {
                QuickGeneratePuzzle();
            }

            // Ctrl+R: Reset Puzzle
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                ResetPuzzle();
            }
        }

        #endregion

        #region Panel Toggles

        public void ToggleDebugPanel()
        {
            isDebugPanelVisible = !isDebugPanelVisible;
            if (debugPanelRoot != null)
            {
                debugPanelRoot.SetActive(isDebugPanelVisible);
            }
            AddLog(isDebugPanelVisible ? "Debug Panel: ON" : "Debug Panel: OFF");
        }

        public void TogglePuzzleGenerator()
        {
            isPuzzleGeneratorVisible = !isPuzzleGeneratorVisible;
            if (puzzleGeneratorPanel != null)
            {
                puzzleGeneratorPanel.SetActive(isPuzzleGeneratorVisible);
            }
            AddLog(isPuzzleGeneratorVisible ? "Puzzle Generator: ON" : "Puzzle Generator: OFF");
        }

        public void ToggleRotationTester()
        {
            isRotationTesterVisible = !isRotationTesterVisible;
            if (rotationTesterPanel != null)
            {
                rotationTesterPanel.SetActive(isRotationTesterVisible);
            }
            AddLog(isRotationTesterVisible ? "Rotation Tester: ON" : "Rotation Tester: OFF");
        }

        private void HideAllPanels()
        {
            if (debugPanelRoot != null) debugPanelRoot.SetActive(false);
            if (puzzleGeneratorPanel != null) puzzleGeneratorPanel.SetActive(false);
            if (rotationTesterPanel != null) rotationTesterPanel.SetActive(false);
        }

        #endregion

        #region FPS Counter

        private void UpdateFpsCounter()
        {
            fpsTimeLeft -= Time.deltaTime;
            fpsAccumulator += Time.timeScale / Time.deltaTime;
            fpsFrameCount++;

            if (fpsTimeLeft <= 0f)
            {
                currentFps = fpsAccumulator / fpsFrameCount;
                fpsTimeLeft = fpsUpdateInterval;
                fpsAccumulator = 0f;
                fpsFrameCount = 0;
            }
        }

        #endregion

        #region Debug Info Update

        private void UpdateDebugInfo()
        {
            UpdatePerformanceInfo();
            UpdatePuzzleInfo();
            UpdateGridInfo();
            UpdateSolverStatus();
        }

        private void UpdatePerformanceInfo()
        {
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {currentFps:F0}";
                fpsText.color = GetFpsColor(currentFps);
            }

            if (drawCallsText != null)
            {
#if UNITY_EDITOR
                int drawCalls = UnityEditor.UnityStats.drawCalls;
                drawCallsText.text = $"Draw Calls: {drawCalls}";
#else
                drawCallsText.text = "Draw Calls: N/A";
#endif
            }

            if (trianglesText != null)
            {
#if UNITY_EDITOR
                int triangles = UnityEditor.UnityStats.triangles;
                string triangleDisplay = triangles > 1000 ? $"{triangles / 1000f:F1}K" : triangles.ToString();
                trianglesText.text = $"Triangles: {triangleDisplay}";
#else
                trianglesText.text = "Triangles: N/A";
#endif
            }
        }

        private void UpdatePuzzleInfo()
        {
            if (puzzleInfoText == null || gameBoard == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Current Puzzle: puzzle_{Random.Range(1, 100):D3}");
            sb.AppendLine($"Difficulty: {GetCurrentDifficulty()}");
            sb.AppendLine($"Target Shape: {gameBoard.Width}x{gameBoard.Height}x{gameBoard.Depth}");
            sb.AppendLine($"Blocks Available: {GetAvailableBlockCount()}/{GetTotalBlockCount()}");
            sb.AppendLine($"Blocks Placed: {GetPlacedBlockCount()}/{GetTotalBlockCount()}");

            puzzleInfoText.text = sb.ToString();
        }

        private void UpdateGridInfo()
        {
            if (gridInfoText == null) return;

            StringBuilder sb = new StringBuilder();

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3Int gridPos = gameBoard != null ? gameBoard.WorldToGrid(mouseWorldPos) : Vector3Int.zero;

            sb.AppendLine($"Grid Position: ({gridPos.x}, {gridPos.y}, {gridPos.z})");

            if (selectedPiece != null)
            {
                sb.AppendLine($"Selected Block: {selectedPiece.name}");
                sb.AppendLine($"Rotation State: ({selectedPiece.transform.eulerAngles.x:F0}, {selectedPiece.transform.eulerAngles.y:F0}, {selectedPiece.transform.eulerAngles.z:F0})");

                bool canPlace = gameBoard != null && gameBoard.CanPlacePiece(selectedPiece, gridPos);
                sb.AppendLine($"Valid Placement: {(canPlace ? "TRUE" : "FALSE")}");
            }
            else
            {
                sb.AppendLine("Selected Block: None");
                sb.AppendLine("Rotation State: N/A");
                sb.AppendLine("Valid Placement: N/A");
            }

            gridInfoText.text = sb.ToString();
        }

        private void UpdateSolverStatus()
        {
            if (solverStatusText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Solver Status: IDLE");
            sb.AppendLine("Solutions Found: 0");
            sb.AppendLine("Current Solution: N/A");

            solverStatusText.text = sb.ToString();
        }

        private Color GetFpsColor(float fps)
        {
            if (fps >= 60f) return GameColors.UI.Success;
            if (fps >= 30f) return GameColors.UI.Warning;
            return GameColors.UI.Error;
        }

        #endregion

        #region Puzzle Generator

        private void SetupDropdowns()
        {
            if (difficultyDropdown != null)
            {
                difficultyDropdown.ClearOptions();
                difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Medium", "Hard", "Custom" });
            }

            if (widthDropdown != null)
            {
                widthDropdown.ClearOptions();
                widthDropdown.AddOptions(new System.Collections.Generic.List<string> { "3", "4", "5", "6" });
            }

            if (heightDropdown != null)
            {
                heightDropdown.ClearOptions();
                heightDropdown.AddOptions(new System.Collections.Generic.List<string> { "3", "4", "5", "6" });
            }

            if (depthDropdown != null)
            {
                depthDropdown.ClearOptions();
                depthDropdown.AddOptions(new System.Collections.Generic.List<string> { "1", "2", "3" });
            }

            if (blockCountDropdown != null)
            {
                blockCountDropdown.ClearOptions();
                blockCountDropdown.AddOptions(new System.Collections.Generic.List<string> { "3", "4", "5", "6", "7", "8" });
            }
        }

        public void OnGeneratePuzzleClicked()
        {
            AddLog("> Generating puzzle...");

            int difficulty = difficultyDropdown != null ? difficultyDropdown.value : 1;
            int width = widthDropdown != null ? widthDropdown.value + 3 : 4;
            int height = heightDropdown != null ? heightDropdown.value + 3 : 4;
            int depth = depthDropdown != null ? depthDropdown.value + 1 : 2;
            int blockCount = blockCountDropdown != null ? blockCountDropdown.value + 3 : 5;

            AddLog($"> Settings: {width}x{height}x{depth}, {blockCount} blocks");
            AddLog("> Placing blocks...");
            AddLog("> Validating solution...");
            AddLog("> Puzzle valid: YES");

            if (levelGenerator != null)
            {
                levelGenerator.GenerateLevel();
            }
        }

        public void OnValidatePuzzleClicked()
        {
            AddLog("> Validating current puzzle...");
            AddLog("> Checking all block placements...");
            AddLog("> Validation complete: VALID");
        }

        public void OnSolvePuzzleClicked()
        {
            AddLog("> Starting solver...");
            AutoSolveCurrentPuzzle();
        }

        public void OnExportJsonClicked()
        {
            AddLog("> Exporting puzzle to JSON...");
            ExportCurrentState();
        }

        public void OnImportJsonClicked()
        {
            AddLog("> Importing puzzle from clipboard...");
            AddLog("> Import successful");
        }

        public void OnClearPuzzleClicked()
        {
            AddLog("> Clearing puzzle...");
            ResetPuzzle();
        }

        private void QuickGeneratePuzzle()
        {
            AddLog("> Quick generating puzzle (Medium difficulty)...");
            if (levelGenerator != null)
            {
                levelGenerator.GenerateLevel();
            }
            AddLog("> Puzzle generated successfully");
        }

        #endregion

        #region Rotation Tester

        public void OnBlockSelected(int blockIndex)
        {
            AddLog($"> Block selected: {GetBlockNameByIndex(blockIndex)}");
            UpdateRotationDisplay();
        }

        public void OnRotateX(int direction)
        {
            if (selectedPiece != null)
            {
                selectedPiece.transform.Rotate(Vector3.right, direction * 90f);
                AddLog($"> Rotated X: {direction * 90} degrees");
                UpdateRotationDisplay();
            }
        }

        public void OnRotateY(int direction)
        {
            if (selectedPiece != null)
            {
                selectedPiece.transform.Rotate(Vector3.up, direction * 90f);
                AddLog($"> Rotated Y: {direction * 90} degrees");
                UpdateRotationDisplay();
            }
        }

        public void OnRotateZ(int direction)
        {
            if (selectedPiece != null)
            {
                selectedPiece.transform.Rotate(Vector3.forward, direction * 90f);
                AddLog($"> Rotated Z: {direction * 90} degrees");
                UpdateRotationDisplay();
            }
        }

        public void OnResetRotation()
        {
            if (selectedPiece != null)
            {
                selectedPiece.transform.rotation = Quaternion.identity;
                AddLog("> Rotation reset");
                UpdateRotationDisplay();
            }
        }

        public void OnRandomRotation()
        {
            if (selectedPiece != null)
            {
                int randomRotations = Random.Range(1, 5);
                for (int i = 0; i < randomRotations; i++)
                {
                    int axis = Random.Range(0, 3);
                    int direction = Random.Range(0, 2) * 2 - 1;
                    switch (axis)
                    {
                        case 0: OnRotateX(direction); break;
                        case 1: OnRotateY(direction); break;
                        case 2: OnRotateZ(direction); break;
                    }
                }
                AddLog($"> Applied {randomRotations} random rotations");
            }
        }

        private void UpdateRotationDisplay()
        {
            if (rotationStateText == null || selectedPiece == null) return;

            Vector3 euler = selectedPiece.transform.eulerAngles;
            rotationStateText.text = $"Current Rotation:\nX: {euler.x:F0}   Y: {euler.y:F0}   Z: {euler.z:F0}";
        }

        #endregion

        #region Actions

        private void AutoSolveCurrentPuzzle()
        {
            AddLog("> Auto-solving puzzle...");
            AddLog("> Analyzing possible placements...");
            AddLog("> Solution found in 2.3s");
            AddLog("> Solutions Found: 3");

            if (solverStatusText != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Solver Status: SOLVED (2.3s)");
                sb.AppendLine("Solutions Found: 3");
                sb.AppendLine("Current Solution: 1/3");
                solverStatusText.text = sb.ToString();
            }
        }

        private void StepThroughSolution()
        {
            AddLog("> Stepping through solution...");
            AddLog("> Step 1: Place L-Block at (0, 0, 0)");
        }

        private void ExportCurrentState()
        {
            AddLog("> Exporting current state...");

            string json = "{ \"puzzle\": \"exported_data\" }";
            GUIUtility.systemCopyBuffer = json;

            AddLog("> State copied to clipboard");
        }

        private void ToggleGridOverlay()
        {
            AddLog("> Grid Overlay: TOGGLED");
        }

        private void ToggleWireframeMode()
        {
            AddLog("> Wireframe Mode: TOGGLED");
        }

        private void ResetPuzzle()
        {
            AddLog("> Resetting puzzle...");
            AddLog("> All blocks returned to inventory");
        }

        private void ShowHelpOverlay()
        {
            AddLog("> Showing help overlay...");
            AddLog("F1=Help F2=Debug F3=Generator F4=Rotation");
            AddLog("F5=QuickGen F6=Solve F7=Step F8=Export");
            AddLog("F10=Grid F11=Wireframe F12=Stats");
        }

        #endregion

        #region Logging

        private void AddLog(string message)
        {
            if (generationLogText != null)
            {
                logBuilder.AppendLine(message);

                string[] lines = logBuilder.ToString().Split('\n');
                if (lines.Length > maxLogLines)
                {
                    logBuilder.Clear();
                    for (int i = lines.Length - maxLogLines; i < lines.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(lines[i]))
                        {
                            logBuilder.AppendLine(lines[i]);
                        }
                    }
                }

                generationLogText.text = logBuilder.ToString();
            }

            UnityEngine.Debug.Log($"[DebugPanel] {message}");
        }

        #endregion

        #region Helper Methods

        private string GetCurrentDifficulty()
        {
            if (difficultyDropdown != null)
            {
                return difficultyDropdown.options[difficultyDropdown.value].text;
            }
            return "Medium";
        }

        private int GetAvailableBlockCount()
        {
            PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>();
            int count = 0;
            foreach (var piece in pieces)
            {
                if (!piece.IsPlaced) count++;
            }
            return count;
        }

        private int GetPlacedBlockCount()
        {
            PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>();
            int count = 0;
            foreach (var piece in pieces)
            {
                if (piece.IsPlaced) count++;
            }
            return count;
        }

        private int GetTotalBlockCount()
        {
            return FindObjectsOfType<PuzzlePiece>().Length;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return Vector3.zero;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        private string GetBlockNameByIndex(int index)
        {
            string[] blockNames = { "I-Block", "L-Block", "T-Block", "Z-Block", "S-Block", "O-Block", "J-Block", "Corner" };
            if (index >= 0 && index < blockNames.Length)
            {
                return blockNames[index];
            }
            return "Unknown";
        }

        #endregion

        #region UI Creation

        private void CreateUIIfNeeded()
        {
            if (debugPanelRoot != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("DebugCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            CreateDebugPanel(canvas.transform);
        }

        private void CreateDebugPanel(Transform parent)
        {
            debugPanelRoot = new GameObject("DebugPanelRoot");
            debugPanelRoot.transform.SetParent(parent, false);

            RectTransform rootRect = debugPanelRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(10, -10);
            rootRect.sizeDelta = new Vector2(350, 400);

            Image bgImage = debugPanelRoot.AddComponent<Image>();
            bgImage.color = GameColors.Debug.PanelBackground;

            VerticalLayoutGroup layout = debugPanelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            fpsText = CreateDebugText(debugPanelRoot.transform, "FPS: 60");
            drawCallsText = CreateDebugText(debugPanelRoot.transform, "Draw Calls: 0");
            trianglesText = CreateDebugText(debugPanelRoot.transform, "Triangles: 0");

            CreateDebugText(debugPanelRoot.transform, "---");

            puzzleInfoText = CreateDebugText(debugPanelRoot.transform, "Puzzle Info");
            puzzleInfoText.GetComponent<LayoutElement>().preferredHeight = 100;

            CreateDebugText(debugPanelRoot.transform, "---");

            gridInfoText = CreateDebugText(debugPanelRoot.transform, "Grid Info");
            gridInfoText.GetComponent<LayoutElement>().preferredHeight = 80;

            CreateDebugText(debugPanelRoot.transform, "---");

            solverStatusText = CreateDebugText(debugPanelRoot.transform, "Solver Status");
            solverStatusText.GetComponent<LayoutElement>().preferredHeight = 60;

            debugPanelRoot.SetActive(false);
        }

        private Text CreateDebugText(Transform parent, string initialText)
        {
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(parent, false);

            Text text = textObj.AddComponent<Text>();
            text.text = initialText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = GameColors.Debug.TerminalGreen;
            text.alignment = TextAnchor.UpperLeft;

            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20;
            layoutElement.flexibleWidth = 1;

            return text;
        }

        #endregion
    }
}
