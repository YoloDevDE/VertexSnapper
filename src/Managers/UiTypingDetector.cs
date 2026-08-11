using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VertexSnapper.Components;

namespace VertexSnapper.Managers;

public static class UiTypingDetector
{
	public static bool IsTyping()
	{
		if (NoteWindow.IsOpen)
		{
			return true;
		}

		EventSystem eventSystem = EventSystem.current;
		if (!eventSystem)
		{
			return false;
		}

		GameObject selected = eventSystem.currentSelectedGameObject;
		if (!selected)
		{
			return false;
		}

		return selected.GetComponentInParent<TMP_InputField>() ? true : selected.GetComponentInParent<InputField>();
	}
}
