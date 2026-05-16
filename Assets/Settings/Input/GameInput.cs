using System;
using UnityEngine.InputSystem.Utilities;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// GameInput 包装类 — 手动实现的强类型包装，等效于 .inputactions 的 "Generate C# Class" 输出。
/// 内嵌 JSON 构建 InputActionAsset，无需依赖外部资源文件。
/// </summary>
public class @GameInput : IInputActionCollection, IDisposable
{
    private InputActionAsset asset;

    // 动作地图
    private GameplayActions gameplay;

    public GameInput()
    {
        asset = InputActionAsset.FromJson(json);
        gameplay = new GameplayActions(asset.FindActionMap("Gameplay"));
    }

    ~GameInput() => Dispose();

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

    public bool Contains(InputAction action) => asset.Contains(action);

    public IEnumerator<InputAction> GetEnumerator() => asset.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => asset.GetEnumerator();

    public void Enable() => asset.Enable();

    public void Disable() => asset.Disable();

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        => asset.FindAction(actionNameOrId, throwIfNotFound);

    public GameplayActions Gameplay => gameplay;

    // ====================================================================
    //  动作地图：Gameplay
    // ====================================================================

    public class GameplayActions
    {
        private InputActionMap map;
        private InputAction clickAction;
        private InputAction contextMenuAction;
        private InputAction escapeAction;

        public GameplayActions(InputActionMap actionMap)
        {
            map = actionMap;
            clickAction = map.FindAction("Click", true);
            contextMenuAction = map.FindAction("ContextMenu", true);
            escapeAction = map.FindAction("Escape", true);
        }

        public InputAction Click => clickAction;
        public InputAction ContextMenu => contextMenuAction;
        public InputAction Escape => escapeAction;

        public InputActionMap Get() => map;
    }

    // ====================================================================
    //  内嵌 InputActionAsset JSON
    // ====================================================================

    private static readonly string json = @"{
    ""name"": ""GameInput"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
            ""actions"": [
                {
                    ""name"": ""Click"",
                    ""type"": ""Button"",
                    ""id"": ""b2c3d4e5-f6a7-8901-bcde-f12345678901"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""MultiTap(tapTime=0.3,tapDelay=0.5,tapCount=2)"",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ContextMenu"",
                    ""type"": ""Button"",
                    ""id"": ""c3d4e5f6-a7b8-9012-cdef-123456789012"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Escape"",
                    ""type"": ""Button"",
                    ""id"": ""d4e5f6a7-b8c9-0123-defa-234567890123"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""e5f6a7b8-c9d0-1234-efab-345678901234"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f6a7b8c9-d0e1-2345-fabc-456789012345"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ContextMenu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a7b8c9d0-e1f2-3456-abcd-567890123456"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Escape"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}";
}
