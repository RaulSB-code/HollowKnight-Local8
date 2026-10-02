using System;
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
		//IL_0087: Invalid comparison between Unknown and I4
		if (s == null || !s.Active)
		{
			return;
		}
		GameManager instance = GameManager.instance;
		GameCameras instance2 = GameCameras.instance;
		Scene activeScene = SceneManager.GetActiveScene();
		string name = ((Scene)(ref activeScene)).name;
		if (scene != name)
		{
			scene = name;
			nextCheck = (playableSince = 0f);
			requestedAt = -1f;
			requested = false;
		}
		UIManager instance3 = UIManager.instance;
		if (!s.Gameplay || !Object.op_Implicit((Object)(object)instance) || instance.isPaused || !Object.op_Implicit((Object)(object)instance3) || (int)instance3.uiState != 4 || TransitionVote.Pending || ScriptedParty.Active || CoopEnding.Active)
		{
			playableSince = 0f;
			return;
		}
		if (playableSince <= 0f)
		{
			playableSince = Time.unscaledTime;
		}
		PlayMakerFSM cameraFadeFSM = instance2.cameraFadeFSM;
		if (!Object.op_Implicit((Object)(object)cameraFadeFSM) || Time.unscaledTime < nextCheck)
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
			Diagnostics.Write("FADE recovery scene=" + name + " state=" + activeStateName + " primary=" + ((object)((Component)s.Primary.Hero).transform.position/*cast due to .constrained prefix*/).ToString());
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
			if (!Object.op_Implicit((Object)(object)cameras) || !Object.op_Implicit((Object)(object)cameras.hudCamera))
			{
				return;
			}
			Transform val = ((Component)cameras.hudCamera).transform.Find("Blanker White");
			if (!Object.op_Implicit((Object)(object)val))
			{
				return;
			}
			PlayMakerFSM val2 = PlayMakerFSM.FindFsmOnGameObject(((Component)val).gameObject, "Blanker Control");
			if (Object.op_Implicit((Object)(object)val2))
			{
				FsmFloat val3 = val2.FsmVariables.FindFsmFloat("Fade Time");
				if (val3 != null)
				{
					val3.Value = 0.35f;
				}
				val2.SendEvent("FADE OUT");
			}
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("FADE white blanker", ex);
		}
	}
}
