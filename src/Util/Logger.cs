using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace VertexSnapper.Util;

/// <summary>
///     The same static facade over BepInEx logging that Author Time Hunting uses, with one
///     addition: every line is also appended to a file of our own.
///     BepInEx truncates LogOutput.log on every game start, so a bug found in the third session
///     of an evening has already erased the first two. VertexSnapper.trace.log keeps them, with a
///     separator between sessions and a timestamp on every line, which is what makes a note typed
///     in game line up with the state change that provoked it.
/// </summary>
public static class Logger
{
	private static ManualLogSource _logSource;
	private static StreamWriter _traceFile;

	public static void Initialize(ManualLogSource logSource)
	{
		_logSource = logSource;
		OpenTraceFile();
	}

	public static void Dispose()
	{
		if (_traceFile == null)
		{
			return;
		}

		_traceFile.Dispose();
		_traceFile = null;
	}

	public static void LogDebug(string message)
	{
		_logSource?.LogDebug(message);
		WriteToTraceFile("DEBUG", message);
	}

	public static void LogInfo(string message)
	{
		_logSource?.LogInfo(message);
		WriteToTraceFile("INFO", message);
	}

	public static void LogWarning(string message)
	{
		_logSource?.LogWarning(message);
		WriteToTraceFile("WARN", message);
	}

	public static void LogError(string message)
	{
		_logSource?.LogError(message);
		WriteToTraceFile("ERROR", message);
	}

	public static void LogError(Exception ex, string context = "")
	{
		if (string.IsNullOrEmpty(context))
		{
			LogError($"Exception: {ex.Message}\n{ex.StackTrace}");

			return;
		}

		LogError($"Exception in {context}: {ex.Message}\n{ex.StackTrace}");
	}

	/// <summary>
	///     The file sits next to the config rather than in the BepInEx log folder so that a user
	///     asked for "the VertexSnapper log" finds it in the folder they already know.
	/// </summary>
	public static string TraceFilePath()
	{
		return Path.Combine(Paths.ConfigPath, "VertexSnapper.trace.log");
	}

	private static void OpenTraceFile()
	{
		try
		{
			_traceFile = new StreamWriter(TraceFilePath(), true) { AutoFlush = true };
			_traceFile.WriteLine();
			_traceFile.WriteLine($"===== session started {Timestamp()} =====");
		}
		catch (Exception e)
		{
			_logSource?.LogWarning($"Could not open the trace file: {e.Message}");
			_traceFile = null;
		}
	}

	/// <summary>
	///     A failed write must never take the game down with it, and it must never spam the
	///     BepInEx log either - a locked file would produce one warning per frame. Dropping the
	///     writer on the first failure leaves the in-memory log intact and stays quiet after that.
	/// </summary>
	private static void WriteToTraceFile(string level, string message)
	{
		if (_traceFile == null)
		{
			return;
		}

		try
		{
			_traceFile.WriteLine($"{Timestamp()} [{level,-5}] {message}");
		}
		catch (Exception e)
		{
			_logSource?.LogWarning($"Trace file write failed, tracing to file stops here: {e.Message}");
			_traceFile = null;
		}
	}

	private static string Timestamp()
	{
		return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + " UTC";
	}
}
