using System;
using UnityEngine;
using Logger = VertexSnapper.Util.Logger;

namespace VertexSnapper.Service;

/// <summary>
///     Writes down what the editor and the player did, so a bug report is a timeline rather than a
///     memory. Same idea as the TraceService in Author Time Hunting, pointed at the level editor:
///     which state the snapper is in, where the gizmo thinks its origin is, what was clicked, and
///     what the undo history looks like.
///     The gizmo half is the reason this exists. StateGizmoLocked writes the gizmo origin every
///     frame while the editor writes it too, and from inside the game there is no way to see who
///     won - the log is the only place that difference becomes visible.
/// </summary>
public class TraceService
{
	private static readonly KeyCode[] Keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	private readonly VertexSnapper _vertexSnapper;

	private int _lastCacheCount = int.MinValue;
	private Vector3 _lastFirstCursor = NoPosition;
	private Vector3 _lastGizmoOrigin = NoPosition;
	private int _lastHistoryCount = int.MinValue;
	private int _lastHistoryPosition = int.MinValue;
	private bool _lastIsEditing;
	private Vector3 _lastSecondCursor = NoPosition;
	private int _lastSelectionCount = int.MinValue;
	private int _lastTool = int.MinValue;

	public TraceService(VertexSnapper vertexSnapper)
	{
		_vertexSnapper = vertexSnapper;
		Logger.LogInfo($"Trace: Tracing started. File is {Logger.TraceFilePath()}.");
	}

	/// <summary>
	///     A position no gizmo and no cursor can hold, so the first frame always counts as a change
	///     and gets written out. Vector3.zero would not do - the editor origin really can be there.
	/// </summary>
	private static Vector3 NoPosition => new(float.MinValue, float.MinValue, float.MinValue);

	public void Tick()
	{
		TraceEditorContext();
		TraceGizmo();
		TraceCursors();
		TraceUndoHistory();
		TraceKeys();
		TraceMouse();
	}

	/// <summary>
	///     Called straight from <see cref="VertexSnapper.ChangeState" /> rather than polled, because a
	///     state that enters and leaves within the same frame would otherwise never show up - and that
	///     is exactly the failure worth catching.
	/// </summary>
	public static void TraceStateChange(object from, object to)
	{
		Logger.LogInfo($"Trace: State {StateName(from)} -> {StateName(to)}.");
	}

	private static string StateName(object state)
	{
		return state == null ? "none" : state.GetType().Name;
	}

	private void TraceEditorContext()
	{
		int tool = _vertexSnapper.LevelEditorCentral.tool.currentTool;
		int selection = _vertexSnapper.LevelEditorCentral.selection.list.Count;
		int cache = _vertexSnapper.BlockSelectionCache.Count;
		bool editing = _vertexSnapper.IsInEditingMode;

		if (tool == _lastTool && selection == _lastSelectionCount && cache == _lastCacheCount &&
		    editing == _lastIsEditing)
		{
			return;
		}

		_lastTool = tool;
		_lastSelectionCount = selection;
		_lastCacheCount = cache;
		_lastIsEditing = editing;

		Logger.LogInfo(
			$"Trace: Editor is now tool {tool}, editing {editing}, {selection} blocks selected, {cache} in the snapper cache.");
	}

	private void TraceGizmo()
	{
		Vector3 origin = _vertexSnapper.LevelEditorCentral.gizmos.motherOrigin;
		if (origin == _lastGizmoOrigin)
		{
			return;
		}

		_lastGizmoOrigin = origin;

		Logger.LogInfo($"Trace: Gizmo origin is now {Format(origin)}.");
	}

	private void TraceCursors()
	{
		_lastFirstCursor = TraceCursor("First", _vertexSnapper.FirstCursor, _lastFirstCursor);
		_lastSecondCursor = TraceCursor("Second", _vertexSnapper.SecondCursor, _lastSecondCursor);
	}

	private static Vector3 TraceCursor(string name, GameObject cursor, Vector3 last)
	{
		Vector3 current = cursor ? cursor.transform.position : NoPosition;
		if (current == last)
		{
			return last;
		}

		Logger.LogInfo($"Trace: {name} cursor is now {(cursor ? Format(current) : "gone")}.");

		return current;
	}

	private void TraceUndoHistory()
	{
		int position = _vertexSnapper.LevelEditorCentral.undoRedo.currentHistoryPosition;
		int count = _vertexSnapper.LevelEditorCentral.undoRedo.historyList.Count;
		if (position == _lastHistoryPosition && count == _lastHistoryCount)
		{
			return;
		}

		_lastHistoryPosition = position;
		_lastHistoryCount = count;

		Logger.LogInfo($"Trace: Undo history is now at {position} of {count} entries.");
	}

	private void TraceKeys()
	{
		if (!Input.anyKeyDown)
		{
			return;
		}

		try
		{
			LogPressedKeys();
		}
		catch (Exception e)
		{
			Logger.LogWarning($"Trace: Could not read the keyboard: {e.Message}");
		}
	}

	private void LogPressedKeys()
	{
		foreach (KeyCode key in Keys)
		{
			if (!Input.GetKeyDown(key))
			{
				continue;
			}

			Logger.LogInfo($"Trace: Key {key} pressed in state {StateName(_vertexSnapper.CurrentState)}.");
		}
	}

	private void TraceMouse()
	{
		LogMouseButton(0, "left");
		LogMouseButton(1, "right");
		LogMouseButton(2, "middle");
	}

	private void LogMouseButton(int button, string name)
	{
		if (!Input.GetMouseButtonDown(button))
		{
			return;
		}

		Logger.LogInfo($"Trace: Mouse {name} pressed in state {StateName(_vertexSnapper.CurrentState)}.");
	}

	private static string Format(Vector3 position)
	{
		return $"({position.x:F3}, {position.y:F3}, {position.z:F3})";
	}
}
