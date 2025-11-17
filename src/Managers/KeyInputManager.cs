using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Managers;

public class KeyInputManager : MonoBehaviour
{
    private static KeyInputManager _instance;

    private static readonly Dictionary<KeyCode, Action> OnDown = new Dictionary<KeyCode, Action>();
    private static readonly Dictionary<KeyCode, Action> OnUp = new Dictionary<KeyCode, Action>();
    private static readonly Dictionary<KeyCode, Action> OnHeld = new Dictionary<KeyCode, Action>();

    private static readonly Dictionary<int, Action> MouseDown = new Dictionary<int, Action>();
    private static readonly Dictionary<int, Action> MouseUp = new Dictionary<int, Action>();
    private static readonly Dictionary<int, Action> MouseHeld = new Dictionary<int, Action>();
    private static readonly HashSet<int> RegisteredMouseButtons = new HashSet<int>();

    private static readonly HashSet<KeyCode> RegisteredKeys = new HashSet<KeyCode>();

    public static KeyEventAccessor OnKeyDown { get; } = new KeyEventAccessor(OnDown);
    public static KeyEventAccessor OnKeyUp { get; } = new KeyEventAccessor(OnUp);
    public static KeyEventAccessor OnKeyHeld { get; } = new KeyEventAccessor(OnHeld);

    public static MouseEventAccessor OnMouseDown { get; } = new MouseEventAccessor(MouseDown);
    public static MouseEventAccessor OnMouseUp { get; } = new MouseEventAccessor(MouseUp);
    public static MouseEventAccessor OnMouseHeld { get; } = new MouseEventAccessor(MouseHeld);

    private void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (RegisteredKeys.Count != 0)
        {
            // Snapshot, damit Änderungen während Invoke nicht die Enumeration sprengen
            List<KeyCode> keysSnapshot = new List<KeyCode>(RegisteredKeys);
            foreach (KeyCode key in keysSnapshot)
            {
                if (Input.GetKeyDown(key))
                {
                    if (OnDown.TryGetValue(key, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyKeyDown?.Invoke(key);
                }

                if (Input.GetKey(key))
                {
                    if (OnHeld.TryGetValue(key, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyKeyHeld?.Invoke(key);
                }

                if (Input.GetKeyUp(key))
                {
                    if (OnUp.TryGetValue(key, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyKeyUp?.Invoke(key);
                }
            }
        }

        if (RegisteredMouseButtons.Count == 0)
        {
            return;
        }

        {
            List<int> mouseSnapshot = new List<int>(RegisteredMouseButtons);
            foreach (int btn in mouseSnapshot)
            {
                if (Input.GetMouseButtonDown(btn))
                {
                    if (MouseDown.TryGetValue(btn, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyMouseDown?.Invoke(btn);
                }

                if (Input.GetMouseButton(btn))
                {
                    if (MouseHeld.TryGetValue(btn, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyMouseHeld?.Invoke(btn);
                }

                if (Input.GetMouseButtonUp(btn))
                {
                    if (MouseUp.TryGetValue(btn, out Action cb))
                    {
                        cb?.Invoke();
                    }

                    AnyMouseUp?.Invoke(btn);
                }
            }
        }
    }

    // Optional: global mouse notifications
    public static event Action<int> AnyMouseDown;
    public static event Action<int> AnyMouseUp;
    public static event Action<int> AnyMouseHeld;

    public static event Action<KeyCode> AnyKeyDown;
    public static event Action<KeyCode> AnyKeyUp;
    public static event Action<KeyCode> AnyKeyHeld;

    public static void EnsureExists()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(KeyInputManager));
        _instance = go.AddComponent<KeyInputManager>();
        DontDestroyOnLoad(go);
    }

    private static void AddHandler(Dictionary<KeyCode, Action> map, KeyCode key, Action handler)
    {
        EnsureExists();
        if (!map.TryGetValue(key, out Action existing))
        {
            map[key] = handler;
        }
        else
        {
            existing += handler;
            map[key] = existing;
        }

        RegisteredKeys.Add(key);
    }

    private static void RemoveHandler(Dictionary<KeyCode, Action> map, KeyCode key, Action handler)
    {
        if (!map.TryGetValue(key, out Action existing))
        {
            return;
        }

        existing -= handler;
        if (existing == null)
        {
            map.Remove(key);
            if (!OnDown.ContainsKey(key) && !OnUp.ContainsKey(key) && !OnHeld.ContainsKey(key))
            {
                RegisteredKeys.Remove(key);
            }
        }
        else
        {
            map[key] = existing;
        }
    }

    // Helpers for mouse maps
    private static void AddMouseHandler(Dictionary<int, Action> map, int button, Action handler)
    {
        EnsureExists();
        if (!map.TryGetValue(button, out Action existing))
        {
            map[button] = handler;
        }
        else
        {
            map[button] = existing + handler;
        }

        RegisteredMouseButtons.Add(button);
    }

    private static void RemoveMouseHandler(Dictionary<int, Action> map, int button, Action handler)
    {
        if (!map.TryGetValue(button, out Action existing))
        {
            return;
        }

        existing -= handler;
        if (existing == null)
        {
            map.Remove(button);
            if (!MouseDown.ContainsKey(button) && !MouseUp.ContainsKey(button) && !MouseHeld.ContainsKey(button))
            {
                RegisteredMouseButtons.Remove(button);
            }
        }
        else
        {
            map[button] = existing;
        }
    }

    // Accessor types for mouse (mirrors KeyEventAccessor)
    public sealed class MouseEventAccessor
    {
        private readonly Dictionary<int, Action> _map;

        internal MouseEventAccessor(Dictionary<int, Action> map)
        {
            _map = map;
        }

        public MouseEvent this[int button]
        {
            get => new MouseEvent(_map, button);
            set
            {
                /* no-op for +=/-= desugaring */
            }
        }
    }

    public readonly struct MouseEvent
    {
        private readonly Dictionary<int, Action> _map;
        private readonly int _button;

        internal MouseEvent(Dictionary<int, Action> map, int button)
        {
            _map = map;
            _button = button;
        }

        public static MouseEvent operator +(MouseEvent e, Action handler)
        {
            AddMouseHandler(e._map, e._button, handler);
            return e;
        }

        public static MouseEvent operator -(MouseEvent e, Action handler)
        {
            RemoveMouseHandler(e._map, e._button, handler);
            return e;
        }
    }

    // Enables += / -= with an indexer per KeyCode
    public sealed class KeyEventAccessor
    {
        private readonly Dictionary<KeyCode, Action> _map;

        internal KeyEventAccessor(Dictionary<KeyCode, Action> map)
        {
            _map = map;
        }

        // Get returns a wrapper value
        public KeyEvent this[KeyCode key]
        {
            get => new KeyEvent(_map, key);
            // Setter exists only to satisfy C#'s x[i] += ... desugaring into x[i] = x[i] + ...
            // It discards the assigned value since the actual subscription already happened in operator +/-
            set
            {
                /* no-op: assignment result is ignored on purpose */
            }
        }
    }

    public readonly struct KeyEvent
    {
        private readonly Dictionary<KeyCode, Action> _map;
        private readonly KeyCode _key;

        internal KeyEvent(Dictionary<KeyCode, Action> map, KeyCode key)
        {
            _map = map;
            _key = key;
        }

        public static KeyEvent operator +(KeyEvent e, Action handler)
        {
            AddHandler(e._map, e._key, handler);
            return e;
        }

        public static KeyEvent operator -(KeyEvent e, Action handler)
        {
            RemoveHandler(e._map, e._key, handler);
            return e;
        }
    }
}