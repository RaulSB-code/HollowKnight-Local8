using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class Diagnostics
{
	private static readonly object Gate = new object();

	private static StreamWriter writer;

	private static readonly Dictionary<string, float> nextError = new Dictionary<string, float>();

	internal static string Folder { get; private set; }

	internal static string CurrentFile { get; private set; }

	internal static void Start()
	{
		try
		{
			Folder = Path.Combine(Application.persistentDataPath, "Local8-Logs");
			Directory.CreateDirectory(Folder);
			CurrentFile = Path.Combine(Folder, "Local8-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".log");
			writer = new StreamWriter(new FileStream(CurrentFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				AutoFlush = true
			};
			Write("START mod=" + ReleaseInfo.Label + " game=1.5.78.11833 scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
			if (Local8Mod.Instance != null)
			{
				Local8Mod.Instance.Log("Logs: " + CurrentFile);
			}
		}
		catch (Exception message)
		{
			if (Local8Mod.Instance != null)
			{
				Local8Mod.Instance.LogError(message);
			}
		}
	}

	internal static void Write(string message)
	{
		try
		{
			lock (Gate)
			{
				if (writer != null)
				{
					writer.WriteLine(DateTime.UtcNow.ToString("O") + " " + message);
				}
			}
		}
		catch
		{
		}
	}

	internal static void Throttled(string context, Exception ex)
	{
		if (!nextError.TryGetValue(context, out var value) || !(Time.unscaledTime < value))
		{
			nextError[context] = Time.unscaledTime + 5f;
			Write(context + " " + ex);
		}
	}

	internal static void Snapshot(CoopSession session, string reason)
	{
		GameManager instance = GameManager.instance;
		Write("STATE " + reason + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " state=" + (instance ? instance.gameState.ToString() : "no-GM") + " loading=" + ((bool)instance && instance.IsLoadingSceneTransition) + " paused=" + ((bool)instance && instance.isPaused) + " players=" + session.Players.Count);
		foreach (PlayerSlot player in session.Players)
		{
			Write("  P" + (player.Index + 1) + " device=" + player.DeviceName + " connected=" + player.Connected + " hero=" + (player.Hero ? player.Hero.GetInstanceID().ToString() : "missing") + " ready=" + player.Ready + " down=" + player.Down + " hazard=" + player.Hazard + " hp=" + player.Vitals.Health + " soul=" + player.Vitals.Soul + " pos=" + (player.Hero ? player.Hero.transform.position.ToString() : "-"));
		}
	}

	internal static void OpenFolder()
	{
		try
		{
			if (string.IsNullOrEmpty(Folder))
			{
				Start();
			}
			if (!Directory.Exists(Folder))
			{
				Directory.CreateDirectory(Folder);
			}
			Process.Start(new ProcessStartInfo
			{
				FileName = Folder,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Write("OPEN LOGS " + ex);
			CopyFolder();
		}
	}

	internal static void CopyFolder()
	{
		GUIUtility.systemCopyBuffer = Folder ?? Path.Combine(Application.persistentDataPath, "Local8-Logs");
		if ((bool)Plugin.Self)
		{
			Plugin.Self.Notice("Ruta de logs copiada. Pegala en el Explorador de archivos.");
		}
	}

	internal static void Stop()
	{
		lock (Gate)
		{
			if (writer != null)
			{
				Write("STOP");
				writer.Dispose();
				writer = null;
			}
		}
	}
}
