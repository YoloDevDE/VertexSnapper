using System.Collections.Generic;
using UnityEngine;
using VertexSnapper.Managers;
using ZeepSDK.LevelEditor;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.Components;

/// <summary>
///     A one-line text box that writes what you type into the trace log, so a session can be
///     narrated while it happens: "gizmo is stuck now" lands between the state changes that caused
///     it instead of being reconstructed from memory afterwards.
///     Deliberately IMGUI. It needs no asset bundle and no canvas, works in every scene, and is
///     debug scaffolding - the cost of making it pretty would be paid by a window that only ever
///     opens when something is broken.
/// </summary>
public class NoteWindow : MonoBehaviour
{
	private const int WindowId = 0x5EEF;
	private const string ControlName = "VertexSnapperNoteField";
	private const int MaxRecentNotes = 6;

	private static readonly List<string> RecentNotes = [];

	private bool _focusRequested;
	private string _note = "";
	private Rect _windowRect = new(20f, 20f, 460f, 220f);

	/// <summary>
	///     Read by <see cref="UiTypingDetector" />: while this is true the snapper's own keybinds
	///     have to hold still, or typing the word "target" would fire the snap key twice.
	/// </summary>
	public static bool IsOpen { get; private set; }

	private void Update()
	{
		if (!Input.GetKeyDown(VertexSnapperConfigManager.NoteWindowKeyBind.Value))
		{
			return;
		}

		Toggle();
	}

	private void OnDestroy()
	{
		if (!IsOpen)
		{
			return;
		}

		Close();
	}

	private void OnGUI()
	{
		if (!IsOpen)
		{
			return;
		}

		_windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "VertexSnapper - note to the log");
	}

	private void Toggle()
	{
		if (IsOpen)
		{
			Close();

			return;
		}

		Open();
	}

	private void Open()
	{
		IsOpen = true;
		_focusRequested = true;
		LevelEditorApi.BlockKeyboardInput(this);
		LevelEditorApi.BlockMouseInput(this);
	}

	private void Close()
	{
		IsOpen = false;
		_note = "";
		LevelEditorApi.UnblockKeyboardInput(this);
		LevelEditorApi.UnblockMouseInput(this);
	}

	private void DrawWindow(int windowId)
	{
		GUILayout.Label($"Type what just happened, Enter sends it. {VertexSnapperConfigManager.NoteWindowKeyBind.Value} closes.");

		HandleReturnKey();

		GUI.SetNextControlName(ControlName);
		_note = GUILayout.TextField(_note, GUILayout.Height(24f));

		TakeFocus();

		DrawRecentNotes();

		GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 20f));
	}

	/// <summary>
	///     Read before the text field draws. Afterwards the field has already consumed the event and
	///     the Return key never arrives here.
	/// </summary>
	private void HandleReturnKey()
	{
		if (Event.current.type != EventType.KeyDown)
		{
			return;
		}

		if (Event.current.keyCode != KeyCode.Return && Event.current.keyCode != KeyCode.KeypadEnter)
		{
			return;
		}

		Submit();
		Event.current.Use();
	}

	private void Submit()
	{
		if (string.IsNullOrEmpty(_note.Trim()))
		{
			return;
		}

		Logger.LogInfo($"NOTE: {_note.Trim()}");
		RecentNotes.Insert(0, _note.Trim());
		_note = "";

		TrimRecentNotes();
	}

	private static void TrimRecentNotes()
	{
		while (RecentNotes.Count > MaxRecentNotes)
		{
			RecentNotes.RemoveAt(RecentNotes.Count - 1);
		}
	}

	private void TakeFocus()
	{
		if (!_focusRequested)
		{
			return;
		}

		_focusRequested = false;
		GUI.FocusControl(ControlName);
	}

	private static void DrawRecentNotes()
	{
		GUILayout.Space(8f);
		GUILayout.Label(RecentNotes.Count == 0 ? "No notes yet." : "Sent:");

		foreach (string note in RecentNotes)
		{
			GUILayout.Label($"  {note}");
		}
	}
}
