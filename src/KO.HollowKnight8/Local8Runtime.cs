using System;
using System.Runtime.CompilerServices;
using Modding;
using Modding.Delegates;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

public sealed class Local8Runtime : MonoBehaviour
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static LanguageGetProxy _003C0_003E__SanitizeLocalized;
	}

	internal static Local8Runtime Self;

	internal Local8Mod Mod;

	internal CoopSession Session;

	internal Setting<bool> Enabled;

	internal Setting<bool> TintPlayers;

	internal Setting<bool> HideOriginalHud;

	internal Setting<bool> ShowLabels;

	internal Setting<bool> ZoomByPlayerCount;

	internal Setting<bool> TimedRespawn;

	internal Setting<bool> ReduceHitEffects;

	internal Setting<bool> BasicHud;

	internal Setting<bool> GroupDarkness;

	internal Setting<bool> SpawnShades;

	internal Setting<bool> WaitForParty;

	internal Setting<float> SideMargin;

	internal Setting<float> ZoomLimit;

	internal Setting<float> ZoomPerPlayer;

	internal Setting<float> CameraSmooth;

	internal Setting<float> GatherDistance;

	internal Setting<float> RespawnSeconds;

	internal Setting<float> ReviveSeconds;

	internal Setting<float> HudScale;

	internal Setting<int> Difficulty;

	internal Setting<string> PrimaryDevice;

	internal bool Panel;

	private bool faulted;

	private bool cursorCaptured;

	private bool previousCursorVisible;

	private CursorLockMode previousCursorLock;

	internal string Message = "Entra en una partida. START en otro mando para unirse; F8 abre el panel.";

	internal float MessageUntil;

	internal void Boot(Local8Mod mod)
	{
		Self = this;
		Mod = mod;
		ImportSettings();
		try
		{
			Diagnostics.Start();
			Application.logMessageReceived += new LogCallback(OnUnityLog);
			object obj = _003C_003EO._003C0_003E__SanitizeLocalized;
			if (obj == null)
			{
				LanguageGetProxy val = Charms.SanitizeLocalized;
				_003C_003EO._003C0_003E__SanitizeLocalized = val;
				obj = (object)val;
			}
			ModHooks.LanguageGetHook += (LanguageGetProxy)obj;
			Hooks.Install();
			Session = new CoopSession(this);
			SceneManager.activeSceneChanged += OnScene;
			((MonoBehaviour)this).StartCoroutine(PvpCombat.Loop());
		}
		catch (Exception ex)
		{
			Fail(ex);
		}
	}

	private void ImportSettings()
	{
		Local8Settings settings = Local8Mod.Settings;
		settings.Ensure();
		if (string.IsNullOrEmpty(settings.PrimaryDevice) || settings.PrimaryDevice == "FirstGamepad")
		{
			settings.PrimaryDevice = "Keyboard";
		}
		Enabled = new Setting<bool>(settings.Enabled);
		TintPlayers = new Setting<bool>(settings.TintPlayers);
		HideOriginalHud = new Setting<bool>(settings.HideOriginalHUD);
		BasicHud = new Setting<bool>(settings.BasicHUD);
		GroupDarkness = new Setting<bool>(settings.GroupDarkness);
		SpawnShades = new Setting<bool>(settings.SpawnShades);
		WaitForParty = new Setting<bool>(settings.WaitForParty);
		SideMargin = new Setting<float>(settings.SideMargin);
		ShowLabels = new Setting<bool>(settings.ShowLabels);
		ZoomByPlayerCount = new Setting<bool>(settings.ZoomByPlayerCount);
		TimedRespawn = new Setting<bool>(settings.TimedRespawn);
		ReduceHitEffects = new Setting<bool>(settings.ReduceHitEffects);
		ZoomLimit = new Setting<float>(settings.MaxZoomOut);
		ZoomPerPlayer = new Setting<float>(settings.ZoomPerExtraPlayer);
		CameraSmooth = new Setting<float>(settings.CameraSmooth);
		GatherDistance = new Setting<float>(settings.GatherDistance);
		RespawnSeconds = new Setting<float>(settings.RespawnSeconds);
		ReviveSeconds = new Setting<float>(settings.ReviveSeconds);
		HudScale = new Setting<float>(settings.HUDScale);
		Difficulty = new Setting<int>(settings.Difficulty);
		PrimaryDevice = new Setting<string>(settings.PrimaryDevice);
	}

	internal void ExportSettings()
	{
		Local8Settings settings = Local8Mod.Settings;
		settings.Enabled = Enabled.Value;
		settings.TintPlayers = TintPlayers.Value;
		settings.HideOriginalHUD = HideOriginalHud.Value;
		settings.ShowLabels = ShowLabels.Value;
		settings.BasicHUD = BasicHud.Value;
		settings.GroupDarkness = GroupDarkness.Value;
		settings.SpawnShades = SpawnShades.Value;
		settings.WaitForParty = WaitForParty.Value;
		settings.SideMargin = SideMargin.Value;
		settings.ZoomByPlayerCount = ZoomByPlayerCount.Value;
		settings.TimedRespawn = TimedRespawn.Value;
		settings.ReduceHitEffects = ReduceHitEffects.Value;
		settings.MaxZoomOut = ZoomLimit.Value;
		settings.ZoomPerExtraPlayer = ZoomPerPlayer.Value;
		settings.CameraSmooth = CameraSmooth.Value;
		settings.GatherDistance = GatherDistance.Value;
		settings.RespawnSeconds = RespawnSeconds.Value;
		settings.ReviveSeconds = ReviveSeconds.Value;
		settings.HUDScale = HudScale.Value;
		settings.Difficulty = Difficulty.Value;
		settings.PrimaryDevice = PrimaryDevice.Value;
	}

	private void OnScene(Scene a, Scene b)
	{
		Diagnostics.Write("SCENE " + ((Scene)(ref a)).name + " -> " + ((Scene)(ref b)).name);
		if (Session != null)
		{
			try
			{
				Session.SceneChanged();
			}
			catch (Exception ex)
			{
				Fail(ex);
			}
		}
	}

	private void Update()
	{
		//IL_004c: Invalid comparison between Unknown and I4
		CoopShades.Tick();
		if (MenuInput.Poll())
		{
			SetPanel(!Panel);
		}
		ActionKeys.Tick(Session);
		if (Panel)
		{
			ForceCursor();
			if (!MenuInput.Waiting && ActionKeys.Capturing == 0 && (int)MenuInput.Key != 290 && Input.GetKeyDown((KeyCode)290) && Session != null)
			{
				Session.JoinFirstAvailable();
			}
		}
		if (Session != null && Enabled.Value && !faulted)
		{
			try
			{
				Session.Tick();
			}
			catch (Exception ex)
			{
				Fail(ex);
			}
		}
	}

	private void LateUpdate()
	{
		if (Panel)
		{
			ForceCursor();
		}
		if (Session != null && Enabled.Value && !faulted)
		{
			try
			{
				Session.VisualTick();
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("VISUAL", ex);
			}
		}
	}

	private void OnGUI()
	{
		int depth = GUI.depth;
		GUI.depth = -10000;
		try
		{
			if (Panel)
			{
				ForceCursor();
			}
			if (Session != null)
			{
				Hud.Draw(this);
			}
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("HUD", ex);
		}
		finally
		{
			GUI.depth = depth;
		}
	}

	private void OnDestroy()
	{
		RestoreCursor();
		SceneManager.activeSceneChanged -= OnScene;
		Application.logMessageReceived -= new LogCallback(OnUnityLog);
		object obj = _003C_003EO._003C0_003E__SanitizeLocalized;
		if (obj == null)
		{
			LanguageGetProxy val = Charms.SanitizeLocalized;
			_003C_003EO._003C0_003E__SanitizeLocalized = val;
			obj = (object)val;
		}
		ModHooks.LanguageGetHook -= (LanguageGetProxy)obj;
		if (Session != null)
		{
			Session.Dispose();
		}
		Hud.ReleaseAssets();
		SkinBridge.ClearCache();
		Hooks.Uninstall();
		Self = null;
		ExportSettings();
		Diagnostics.Stop();
	}

	internal void Notice(string s)
	{
		s = s.Replace("F8", MenuInput.Label);
		Message = s;
		MessageUntil = Time.unscaledTime + 8f;
		if (Local8Mod.Instance != null)
		{
			((Loggable)Local8Mod.Instance).Log(s);
		}
		Diagnostics.Write("NOTICE " + s);
	}

	internal void SetPanel(bool open)
	{
		if (open != Panel)
		{
			if (open)
			{
				previousCursorVisible = Cursor.visible;
				previousCursorLock = Cursor.lockState;
				cursorCaptured = true;
				Panel = true;
				ModHooks.CursorHook += ForceCursor;
				ForceCursor();
			}
			else
			{
				Panel = false;
				MenuInput.Waiting = false;
				ActionKeys.Capturing = 0;
				Charms.Close();
				RestoreCursor();
				ExportSettings();
			}
		}
	}

	private void ForceCursor()
	{
		Cursor.lockState = (CursorLockMode)0;
		Cursor.visible = true;
	}

	private void RestoreCursor()
	{
		ModHooks.CursorHook -= ForceCursor;
		if (cursorCaptured)
		{
			Cursor.lockState = previousCursorLock;
			Cursor.visible = previousCursorVisible;
			cursorCaptured = false;
		}
	}

	private void Fail(Exception ex)
	{
		if (Local8Mod.Instance != null)
		{
			((Loggable)Local8Mod.Instance).LogError((object)ex);
		}
		Diagnostics.Write("FATAL " + ex);
		Notice("Local8 detenido por un error. Envia la carpeta Local8-Logs.");
		if (Session != null)
		{
			Session.Dispose();
		}
		faulted = true;
	}

	private unsafe void OnUnityLog(string condition, string stack, LogType type)
	{
		//IL_0002: Invalid comparison between Unknown and I4
		if ((int)type == 4 || (int)type == 0)
		{
			Diagnostics.Write("UNITY " + ((object)(*(LogType*)(&type))/*cast due to .constrained prefix*/).ToString() + " " + condition + "\n" + stack);
		}
	}
}
