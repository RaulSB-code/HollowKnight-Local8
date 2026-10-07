using System;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

public sealed class Local8Runtime : MonoBehaviour
{
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
			Application.logMessageReceived += OnUnityLog;
			ModHooks.LanguageGetHook += Charms.SanitizeLocalized;
			Hooks.Install();
			Session = new CoopSession(this);
			UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnScene;
			StartCoroutine(PvpCombat.Loop());
		}
		catch (Exception ex)
		{
			Fail(ex);
		}
		ShadeRitualTriggers.Install();
		HitStopIsolation.Install();
		LiquidPolish.Install();
		ChallengeDrawAudio.Install();
		CrystalDashCoop.Install();
		PvpRoundClock.Migrate();
		RoleSystem.Install();
		PvpCharms.Install();
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
		Diagnostics.Write("SCENE " + a.name + " -> " + b.name);
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
		CoopShades.Tick();
		if (MenuInput.Poll())
		{
			SetPanel(!Panel);
		}
		ActionKeys.Tick(Session);
		if (Panel)
		{
			ForceCursor();
			if (!MenuInput.Waiting && ActionKeys.Capturing == 0 && MenuInput.Key != KeyCode.F9 && KeypadInput.Pressed(KeyCode.F9) && Session != null)
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
		CrystalDashTransit.Tick();
		HitStopIsolation.Tick();
		RadianceAscentCheckpoint.Tick();
		BenchPoseRecovery.Tick();
		CrystalDashCoop.Tick();
		PvpArena.Update(this);
		BossMusicGuard.Tick();
		RoleSystem.Tick(this);
		ArenaVisualRecovery.Tick();
		ArenaReturnRecovery.Tick();
		InteractionMotion.Tick();
		MenuInputRecovery.Tick();
		PvpGeoHud.Tick();
		FlowerAura.Tick();
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
		RoleSystem.Shutdown();
		RestoreCursor();
		UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnScene;
		Application.logMessageReceived -= OnUnityLog;
		ModHooks.LanguageGetHook -= Charms.SanitizeLocalized;
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
		CrystalDashTransit.Reset();
		ShadeRitualTriggers.Uninstall();
		ShadeCloakRitual.Reset();
		HitStopIsolation.Uninstall();
		LiquidPolish.Uninstall();
		ChallengeDrawAudio.Uninstall();
		CrystalDashCoop.Uninstall();
		DreamRescueFeedback.Reset();
		PvpCharms.Uninstall();
		FlowerAura.Reset();
	}

	internal void Notice(string s)
	{
		s = s.Replace("F8", MenuInput.Label);
		Message = s;
		MessageUntil = Time.unscaledTime + 8f;
		if (Local8Mod.Instance != null)
		{
			Local8Mod.Instance.Log(s);
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
		Cursor.lockState = CursorLockMode.None;
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
			Local8Mod.Instance.LogError(ex);
		}
		Diagnostics.Write("FATAL " + ex);
		Notice(ReleaseInfo.Label + " detenido por un error. Envia la carpeta Local8-Logs.");
		if (Session != null)
		{
			Session.Dispose();
		}
		faulted = true;
	}

	private void OnUnityLog(string condition, string stack, LogType type)
	{
		if (type == LogType.Exception || type == LogType.Error)
		{
			Diagnostics.Write("UNITY " + type.ToString() + " " + condition + "\n" + stack);
		}
	}
}
