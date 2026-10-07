using System;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class CameraFadeRecovery
{
	private static string scene;

	private static float playableSince;

	private static float nextCheck;

	private static float requestedAt = -1f;

	private static bool requested;

	internal static void Reset()
	{
		scene = null;
		nextCheck = (playableSince = 0f);
		requestedAt = -1f;
		requested = false;
	}

	internal static void Tick(CoopSession s)
	{
		if (s == null || !s.Active)
		{
			return;
		}
		GameManager instance = GameManager.instance;
		GameCameras instance2 = GameCameras.instance;
		string name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		if (scene != name)
		{
			scene = name;
			nextCheck = (playableSince = 0f);
			requestedAt = -1f;
			requested = false;
		}
		UIManager instance3 = UIManager.instance;
		if (!s.Gameplay || !instance || instance.isPaused || !instance3 || instance3.uiState != UIState.PLAYING || TransitionVote.Pending || ScriptedParty.Active || CoopEnding.Active)
		{
			playableSince = 0f;
			return;
		}
		if (playableSince <= 0f)
		{
			playableSince = Time.unscaledTime;
		}
		PlayMakerFSM cameraFadeFSM = instance2.cameraFadeFSM;
		if (!cameraFadeFSM || Time.unscaledTime < nextCheck)
		{
			return;
		}
		nextCheck = Time.unscaledTime + 0.25f;
		string activeStateName = cameraFadeFSM.ActiveStateName;
		if ((!(name == "Dream_Nailcollection") || !(activeStateName == "Normal")) && (activeStateName == "Normal" || activeStateName == "FadingOut" || Time.unscaledTime - playableSince < 8f))
		{
			return;
		}
		if (!requested)
		{
			requested = true;
			requestedAt = Time.unscaledTime;
			Diagnostics.Write("FADE recovery scene=" + name + " state=" + activeStateName + " primary=" + s.Primary.Hero.transform.position.ToString());
			instance.FadeSceneIn();
			FadeWhiteBlanker(instance2);
		}
		else
		{
			FadeWhiteBlanker(instance2);
			if (!(Time.unscaledTime - requestedAt < 2f) && !(activeStateName == "Normal") && cameraFadeFSM.Fsm.GetState("Normal") != null)
			{
				cameraFadeFSM.SetState("Normal");
				Diagnostics.Write("FADE forced normal scene=" + name + " state=" + activeStateName);
			}
		}
	}

	private static void FadeWhiteBlanker(GameCameras cameras)
	{
		try
		{
			if (!cameras || !cameras.hudCamera)
			{
				return;
			}
			Transform transform = cameras.hudCamera.transform.Find("Blanker White");
			if (!transform)
			{
				return;
			}
			PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(transform.gameObject, "Blanker Control");
			if ((bool)playMakerFSM)
			{
				FsmFloat fsmFloat = playMakerFSM.FsmVariables.FindFsmFloat("Fade Time");
				if (fsmFloat != null)
				{
					fsmFloat.Value = 0.35f;
				}
				playMakerFSM.SendEvent("FADE OUT");
			}
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("FADE white blanker", ex);
		}
	}
}
