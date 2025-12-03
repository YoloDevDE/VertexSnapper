using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VertexSnapper.Managers;

public static class UiTypingDetector
{
    public static bool IsTyping()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            return false;
        }

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null)
        {
            return false;
        }
        
        if (selected.GetComponentInParent<TMP_InputField>() != null)
        {
            return true;
        }


        return selected.GetComponentInParent<InputField>() != null;
    }
}