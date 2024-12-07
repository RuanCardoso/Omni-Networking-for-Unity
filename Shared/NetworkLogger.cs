/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Omni.Shared
{
	public static class NetworkLogger
	{
		private const string LogPath = "omni_player_log.txt";
		public static StreamWriter fileStream = null;

		public enum LogType
		{
			Error = 0,
			Warning = 2,
			Log = 3,
		}

#pragma warning disable IDE1006
#if OMNI_DEBUG
		/// <summary>
		/// Logs a message to both the console and the log file (debug builds only).
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="logType">The log message type (default: <see cref="LogType.Log"/>).</param>
		public static void __Log__(string message, LogType logType = LogType.Log)
		{
			Log(message, true, logType);
		}
#else
		/// <summary>
		/// Logs a message to both the console and the log file (release builds only).
		/// </summary>
		/// <remarks>
		/// In the Unity Editor, the message is logged to the console. In other builds, the message is logged to a file.
		/// </remarks>
		/// <param name="message">The message to log.</param>
		/// <param name="logType">The log message type (default: <see cref="LogType.Log"/>).</param>
		public static void __Log__(string message, LogType logType = LogType.Log)
		{
#if UNITY_EDITOR
			Log(message, true, logType);
#else
            LogToFile(message, logType);
#endif
		}
#endif
#pragma warning restore IDE1006
		/// <summary>
		/// Prints the contents of the player's log file to the log output.
		/// </summary>
		public static void PrintPlayerLog()
		{
			if (File.Exists(LogPath))
			{
				Log($"Log Path: {Path.GetFullPath(LogPath)}");
				Log($"Player Log:\n\r{File.ReadAllText(LogPath)}");
			}
			else
			{
				Log("No player log found. this file is empty.");
			}
		}

		/// <summary>
		/// Logs a message to a persistent log file, keeping the file stream open for better performance.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="logType">
		/// The type of log message, indicating its severity and category. Default is <see cref="LogType.Log"/>.
		/// </param>
		/// <remarks>
		/// This method appends the log message to a file, maintaining an open stream for improved performance when writing multiple log entries.
		/// </remarks>
		public static void LogToFile(object message, LogType logType = LogType.Log)
		{
			try
			{
				fileStream ??= new(LogPath, append: true); // Keep the stream open for better performance.
				string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				int threadId = Thread.CurrentThread.ManagedThreadId;
				fileStream.WriteLine($"{dateTime}: {message} -> Thread Id: ({threadId}) - {logType}");
			}
			catch
			{
				// Ignored -> IOException: Sharing violation
			}
		}

		/// <summary>
		/// Logs a message to the console and optionally writes it to a log file.
		/// </summary>
		/// <param name="message">The message to be logged.</param>
		/// <param name="writeToLogFile">
		/// Indicates whether the message should also be written to a log file. Default is <c>false</c>.
		/// </param>
		/// <param name="logType">
		/// The type of log message, determining its severity and appearance. Default is <see cref="LogType.Log"/>.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method provides a flexible logging mechanism, supporting console output, optional file logging, and enhanced debug information.
		/// </para>
		/// <para>
		/// In server builds:
		/// <list type="bullet">
		/// <item><see cref="LogType.Error"/> messages are displayed in red.</item>
		/// <item><see cref="LogType.Warning"/> messages are displayed in yellow.</item>
		/// <item>Other messages are displayed in white.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static void Log(
			object message,
			bool writeToLogFile = false,
			LogType logType = LogType.Log
		)
		{
			if (writeToLogFile)
			{
				LogToFile(message, logType);
			}

#if OMNI_SERVER && !UNITY_EDITOR
            ConsoleColor logColor = logType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };

            Console.ForegroundColor = logColor;
            Console.WriteLine($"[{logType}] -> {message}");
            Console.WriteLine(new string('-', Console.WindowWidth - 1));
#else
#if OMNI_DEBUG
			Debug.LogFormat((UnityEngine.LogType)logType, UnityEngine.LogOption.None, null, "{0}", message);
#else
            Debug.LogFormat(
                (UnityEngine.LogType)logType,
                UnityEngine.LogOption.NoStacktrace,
                null,
                "{0}",
                message
            );
#endif
#endif
		}

		/// <summary>
		/// Prints a message to the console or debug output without a stack trace. This method does not log to a file.
		/// </summary>
		/// <param name="message">The message to be printed.</param>
		/// <param name="logType">
		/// The type of log message, determining its severity and appearance.
		/// Defaults to <see cref="LogType.Error"/>.
		/// </param>
		/// <remarks>
		/// <para>
		/// In a server build, the message is printed to the console with a color indicating its log type:
		/// <list type="bullet">
		/// <item><see cref="LogType.Error"/> messages are displayed in red.</item>
		/// <item><see cref="LogType.Warning"/> messages are displayed in yellow.</item>
		/// <item>Other messages are displayed in white.</item>
		/// </list>
		/// </para>
		/// <para>
		/// In non-server builds, the message is output using Unity's debug system without a stack trace.
		/// </para>
		/// <para>
		/// This method is designed for quick and lightweight logging and should not be used for persistent logs.
		/// </para>
		/// </remarks>
		public static void Print(string message, LogType logType = LogType.Error)
		{
#if OMNI_SERVER && !UNITY_EDITOR
            ConsoleColor logColor = logType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };

            Console.ForegroundColor = logColor;
            Console.WriteLine($"[{logType}] -> {message}");
            Console.WriteLine(new string('-', Console.WindowWidth - 1));
#else
			Debug.LogFormat(
				(UnityEngine.LogType)logType,
				UnityEngine.LogOption.NoStacktrace,
				null,
				"{0}",
				message
			);
#endif
		}

		/// <summary>
		/// Retrieves detailed stack trace information, including class, method, and line number, for debugging purposes.
		/// </summary>
		/// <param name="exception">
		/// An optional <see cref="Exception"/> object. If provided, the stack trace for the exception is used.
		/// If <c>null</c>, the current call stack is retrieved.
		/// </param>
		/// <returns>
		/// A string containing the stack trace details, including class name, method name, and line number.
		/// </returns>
		/// <remarks>
		/// This method is useful for debugging scenarios to provide insights into the call stack.
		/// It performs a detailed analysis of the stack frames, which can be computationally expensive.
		/// Use this method primarily in debug builds or for diagnostic purposes.
		/// </remarks>
		public static string GetStackTrace(Exception exception = null)
		{
			var frames = CreateStackTrace(exception);
			var _message = "";

			// Very slow operation, but useful for debugging. Debug mode only.
			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				int line = frame.GetFileLineNumber();
				_message +=
					$"StackTrace -> Class: [{method.DeclaringType}] | Method: [{method.Name}] | Line: [{line}]\r\n";
			}

			return _message;
		}

		/// <summary>
		/// Creates a sequence of <see cref="StackFrame"/> objects representing the call stack.
		/// </summary>
		/// <param name="exception">
		/// An optional <see cref="Exception"/> object. If provided, the stack trace for the exception is analyzed.
		/// If <c>null</c>, the current call stack is analyzed.
		/// </param>
		/// <returns>
		/// An enumerable sequence of <see cref="StackFrame"/> objects, representing the frames in the call stack.
		/// </returns>
		/// <remarks>
		/// This method generates a detailed representation of the call stack for diagnostic purposes.
		/// Each <see cref="StackFrame"/> includes file information such as line numbers and method details,
		/// which may require the application to be compiled with debug symbols for complete accuracy.
		/// </remarks>
		public static IEnumerable<StackFrame> CreateStackTrace(Exception exception = null)
		{
			StackTrace stack = exception == null ? new StackTrace(fNeedFileInfo: true) : new StackTrace(exception, fNeedFileInfo: true);
			for (int i = stack.FrameCount - 1; i >= 0; i--)
			{
				StackFrame frame = stack.GetFrame(i);
				if (frame == null)
					continue;

				yield return frame;
			}
		}
	}
}
