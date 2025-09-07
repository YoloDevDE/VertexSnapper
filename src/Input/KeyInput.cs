using System.Collections.Generic;
using UnityEngine;

namespace VertexSnapper.Input;

public static class KeyInput
{
    private static readonly Dictionary<KeyCode, KeyEvents> KeyEvents = new Dictionary<KeyCode, KeyEvents>();

    public static KeyEvents GetKey(KeyCode keyCode)
    {
        if (!KeyEvents.ContainsKey(keyCode))
        {
            KeyEvents[keyCode] = new KeyEvents();
        }

        return KeyEvents[keyCode];
    }

    public static void Update()
    {
        foreach (KeyValuePair<KeyCode, KeyEvents> kvp in KeyEvents)
        {
            KeyCode keyCode = kvp.Key;
            KeyEvents events = kvp.Value;

            if (UnityEngine.Input.GetKeyDown(keyCode))
            {
                events.TriggerKeyDown();
                continue;
            }

            if (UnityEngine.Input.GetKeyUp(keyCode))
            {
                events.TriggerKeyUp();
                continue;
            }

            if (UnityEngine.Input.GetKey(keyCode))
            {
                events.TriggerKeyHold();
            }
        }
    }
}