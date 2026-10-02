using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using GlobalEnums;
using Modding;
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
			Scene activeScene = SceneManager.GetActiveScene();
			Write("START version=0.3.47-bg111xx game=1.5.78.11833 scene=" + ((Scene)(ref activeScene)).name);
			if (Local8Mod.Instance != null)
			{
				((Loggable)Local8Mod.Instance).Log("Logs: " + CurrentFile);
			}
		}
		catch (Exception ex)
		{
			if (Local8Mod.Instance != null)
			{
				((Loggable)Local8Mod.Instance).LogError((object)ex);
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
		string[] obj = new string[12]
		{
			"STATE ", reason, " scene=", null, null, null, null, null, null, null,
			null, null
		};
		Scene activeScene = SceneManager.GetActiveScene();
		obj[3] = ((Scene)(ref activeScene)).name;
		obj[4] = " state=";
		obj[5] = (Object.op_Implicit((Object)(object)instance) ? ((object)Unsafe.As<GameState, GameState>(ref instance.gameState)/*cast due to .constrained prefix*/).ToString() : "no-GM");
		obj[6] = " loading=";
		obj[7] = (Object.op_Implicit((Object)(object)instance) && instance.IsLoadingSceneTransition).ToString();
		obj[8] = " paused=";
		obj[9] = (Object.op_Implicit((Object)(object)instance) && instance.isPaused).ToString();
		obj[10] = " players=";
		obj[11] = session.Players.Count.ToString();
		Write(string.Concat(obj));
		foreach (PlayerSlot player in session.Players)
		{
			Write("  P" + (player.Index + 1) + " device=" + player.DeviceName + " connected=" + player.Connected + " hero=" + (Object.op_Implicit((Object)(object)player.Hero) ? ((Object)player.Hero).GetInstanceID().ToString() : "missing") + " ready=" + player.Ready + " down=" + player.Down + " hazard=" + player.Hazard + " hp=" + player.Vitals.Health + " soul=" + player.Vitals.Soul + " pos=" + (Object.op_Implicit((Object)(object)player.Hero) ? ((object)((Component)player.Hero).transform.position/*cast due to .constrained prefix*/).ToString() : "-"));
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
		if (Object.op_Implicit((Object)(object)Plugin.Self))
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
