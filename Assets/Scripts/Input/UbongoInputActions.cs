using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Ubongo
{
    public partial class UbongoInputActions : IInputActionCollection2, IDisposable
    {
        public InputActionAsset asset { get; }

        public UbongoInputActions()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""UbongoInputActions"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""b0e3c2d1-f4a5-4b6c-8d7e-9f0a1b2c3d4e"",
            ""actions"": [
                {
                    ""name"": ""Point"",
                    ""type"": ""Value"",
                    ""id"": ""a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Click"",
                    ""type"": ""Button"",
                    ""id"": ""b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RotateYNeg"",
                    ""type"": ""Button"",
                    ""id"": ""c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RotateYPos"",
                    ""type"": ""Button"",
                    ""id"": ""d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RotateXPos"",
                    ""type"": ""Button"",
                    ""id"": ""e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RotateZPos"",
                    ""type"": ""Button"",
                    ""id"": ""f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""10a1b2c3-d4e5-4f6a-7b8c-9d0e1f2a3b4c"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Point"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""20b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""30c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""RotateYNeg"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""40d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""RotateYPos"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""50e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""RotateXPos"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""60f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b"",
                    ""path"": ""<Keyboard>/f"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""RotateZPos"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Debug"",
            ""id"": ""c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f"",
            ""actions"": [
                {
                    ""name"": ""Help"",
                    ""type"": ""Button"",
                    ""id"": ""a1a2a3a4-b5b6-4c7d-8e9f-0a1b2c3d4e5f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleDebug"",
                    ""type"": ""Button"",
                    ""id"": ""b2b3b4b5-c6c7-4d8e-9f0a-1b2c3d4e5f6a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleGenerator"",
                    ""type"": ""Button"",
                    ""id"": ""c3c4c5c6-d7d8-4e9f-0a1b-2c3d4e5f6a7b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleRotation"",
                    ""type"": ""Button"",
                    ""id"": ""d4d5d6d7-e8e9-4f0a-1b2c-3d4e5f6a7b8c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""QuickGenerate"",
                    ""type"": ""Button"",
                    ""id"": ""e5e6e7e8-f9f0-4a1b-2c3d-4e5f6a7b8c9d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""AutoSolve"",
                    ""type"": ""Button"",
                    ""id"": ""f6f7f8f9-0a1b-4c2d-3e4f-5a6b7c8d9e0f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""StepSolution"",
                    ""type"": ""Button"",
                    ""id"": ""a7a8a9a0-b1b2-4c3d-4e5f-6a7b8c9d0e1f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Export"",
                    ""type"": ""Button"",
                    ""id"": ""b8b9b0b1-c2c3-4d4e-5f6a-7b8c9d0e1f2a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleGrid"",
                    ""type"": ""Button"",
                    ""id"": ""c9c0c1c2-d3d4-4e5f-6a7b-8c9d0e1f2a3b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleWireframe"",
                    ""type"": ""Button"",
                    ""id"": ""d0d1d2d3-e4e5-4f6a-7b8c-9d0e1f2a3b4c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleStats"",
                    ""type"": ""Button"",
                    ""id"": ""e1e2e3e4-f5f6-4a7b-8c9d-0e1f2a3b4c5d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ResetPuzzle"",
                    ""type"": ""Button"",
                    ""id"": ""f2f3f4f5-a6a7-4b8c-9d0e-1f2a3b4c5d6e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""11111111-1111-4111-1111-111111111111"",
                    ""path"": ""<Keyboard>/f1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Help"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""22222222-2222-4222-2222-222222222222"",
                    ""path"": ""<Keyboard>/f2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleDebug"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""33333333-3333-4333-3333-333333333333"",
                    ""path"": ""<Keyboard>/f3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleGenerator"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""44444444-4444-4444-4444-444444444444"",
                    ""path"": ""<Keyboard>/f4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleRotation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""55555555-5555-4555-5555-555555555555"",
                    ""path"": ""<Keyboard>/f5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""QuickGenerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""CtrlN"",
                    ""id"": ""55555556-5555-4555-5555-555555555556"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""QuickGenerate"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""55555557-5555-4555-5555-555555555557"",
                    ""path"": ""<Keyboard>/leftCtrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""QuickGenerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""55555558-5555-4555-5555-555555555558"",
                    ""path"": ""<Keyboard>/n"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""QuickGenerate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""66666666-6666-4666-6666-666666666666"",
                    ""path"": ""<Keyboard>/f6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""AutoSolve"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""77777777-7777-4777-7777-777777777777"",
                    ""path"": ""<Keyboard>/f7"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""StepSolution"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""88888888-8888-4888-8888-888888888888"",
                    ""path"": ""<Keyboard>/f8"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Export"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""99999999-9999-4999-9999-999999999999"",
                    ""path"": ""<Keyboard>/f10"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleGrid"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""aaaaaaaa-aaaa-4aaa-aaaa-aaaaaaaaaaaa"",
                    ""path"": ""<Keyboard>/f11"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleWireframe"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb"",
                    ""path"": ""<Keyboard>/f12"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ToggleStats"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""CtrlR"",
                    ""id"": ""cccccccc-cccc-4ccc-cccc-cccccccccccc"",
                    ""path"": ""ButtonWithOneModifier"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ResetPuzzle"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""modifier"",
                    ""id"": ""cccccccd-cccc-4ccc-cccc-cccccccccccc"",
                    ""path"": ""<Keyboard>/leftCtrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ResetPuzzle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""button"",
                    ""id"": ""ccccccce-cccc-4ccc-cccc-cccccccccccc"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""ResetPuzzle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""bindingGroup"": ""Keyboard&Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
            m_Gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
            m_Gameplay_Point = m_Gameplay.FindAction("Point", throwIfNotFound: true);
            m_Gameplay_Click = m_Gameplay.FindAction("Click", throwIfNotFound: true);
            m_Gameplay_RotateYNeg = m_Gameplay.FindAction("RotateYNeg", throwIfNotFound: true);
            m_Gameplay_RotateYPos = m_Gameplay.FindAction("RotateYPos", throwIfNotFound: true);
            m_Gameplay_RotateXPos = m_Gameplay.FindAction("RotateXPos", throwIfNotFound: true);
            m_Gameplay_RotateZPos = m_Gameplay.FindAction("RotateZPos", throwIfNotFound: true);

            m_Debug = asset.FindActionMap("Debug", throwIfNotFound: true);
            m_Debug_Help = m_Debug.FindAction("Help", throwIfNotFound: true);
            m_Debug_ToggleDebug = m_Debug.FindAction("ToggleDebug", throwIfNotFound: true);
            m_Debug_ToggleGenerator = m_Debug.FindAction("ToggleGenerator", throwIfNotFound: true);
            m_Debug_ToggleRotation = m_Debug.FindAction("ToggleRotation", throwIfNotFound: true);
            m_Debug_QuickGenerate = m_Debug.FindAction("QuickGenerate", throwIfNotFound: true);
            m_Debug_AutoSolve = m_Debug.FindAction("AutoSolve", throwIfNotFound: true);
            m_Debug_StepSolution = m_Debug.FindAction("StepSolution", throwIfNotFound: true);
            m_Debug_Export = m_Debug.FindAction("Export", throwIfNotFound: true);
            m_Debug_ToggleGrid = m_Debug.FindAction("ToggleGrid", throwIfNotFound: true);
            m_Debug_ToggleWireframe = m_Debug.FindAction("ToggleWireframe", throwIfNotFound: true);
            m_Debug_ToggleStats = m_Debug.FindAction("ToggleStats", throwIfNotFound: true);
            m_Debug_ResetPuzzle = m_Debug.FindAction("ResetPuzzle", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        public IEnumerable<InputBinding> bindings => asset.bindings;

        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        // Gameplay
        private readonly InputActionMap m_Gameplay;
        private readonly InputAction m_Gameplay_Point;
        private readonly InputAction m_Gameplay_Click;
        private readonly InputAction m_Gameplay_RotateYNeg;
        private readonly InputAction m_Gameplay_RotateYPos;
        private readonly InputAction m_Gameplay_RotateXPos;
        private readonly InputAction m_Gameplay_RotateZPos;

        public struct GameplayActions
        {
            private UbongoInputActions m_Wrapper;

            public GameplayActions(UbongoInputActions wrapper) { m_Wrapper = wrapper; }

            public InputAction Point => m_Wrapper.m_Gameplay_Point;
            public InputAction Click => m_Wrapper.m_Gameplay_Click;
            public InputAction RotateYNeg => m_Wrapper.m_Gameplay_RotateYNeg;
            public InputAction RotateYPos => m_Wrapper.m_Gameplay_RotateYPos;
            public InputAction RotateXPos => m_Wrapper.m_Gameplay_RotateXPos;
            public InputAction RotateZPos => m_Wrapper.m_Gameplay_RotateZPos;

            public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;

            public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        }

        public GameplayActions Gameplay => new GameplayActions(this);

        // Debug
        private readonly InputActionMap m_Debug;
        private readonly InputAction m_Debug_Help;
        private readonly InputAction m_Debug_ToggleDebug;
        private readonly InputAction m_Debug_ToggleGenerator;
        private readonly InputAction m_Debug_ToggleRotation;
        private readonly InputAction m_Debug_QuickGenerate;
        private readonly InputAction m_Debug_AutoSolve;
        private readonly InputAction m_Debug_StepSolution;
        private readonly InputAction m_Debug_Export;
        private readonly InputAction m_Debug_ToggleGrid;
        private readonly InputAction m_Debug_ToggleWireframe;
        private readonly InputAction m_Debug_ToggleStats;
        private readonly InputAction m_Debug_ResetPuzzle;

        public struct DebugActions
        {
            private UbongoInputActions m_Wrapper;

            public DebugActions(UbongoInputActions wrapper) { m_Wrapper = wrapper; }

            public InputAction Help => m_Wrapper.m_Debug_Help;
            public InputAction ToggleDebug => m_Wrapper.m_Debug_ToggleDebug;
            public InputAction ToggleGenerator => m_Wrapper.m_Debug_ToggleGenerator;
            public InputAction ToggleRotation => m_Wrapper.m_Debug_ToggleRotation;
            public InputAction QuickGenerate => m_Wrapper.m_Debug_QuickGenerate;
            public InputAction AutoSolve => m_Wrapper.m_Debug_AutoSolve;
            public InputAction StepSolution => m_Wrapper.m_Debug_StepSolution;
            public InputAction Export => m_Wrapper.m_Debug_Export;
            public InputAction ToggleGrid => m_Wrapper.m_Debug_ToggleGrid;
            public InputAction ToggleWireframe => m_Wrapper.m_Debug_ToggleWireframe;
            public InputAction ToggleStats => m_Wrapper.m_Debug_ToggleStats;
            public InputAction ResetPuzzle => m_Wrapper.m_Debug_ResetPuzzle;

            public InputActionMap Get() { return m_Wrapper.m_Debug; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;

            public static implicit operator InputActionMap(DebugActions set) { return set.Get(); }
        }

        public DebugActions Debug => new DebugActions(this);

        private int m_KeyboardMouseSchemeIndex = -1;

        public InputControlScheme KeyboardMouseScheme
        {
            get
            {
                if (m_KeyboardMouseSchemeIndex == -1)
                {
                    m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard&Mouse");
                }
                return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
            }
        }
    }
}
