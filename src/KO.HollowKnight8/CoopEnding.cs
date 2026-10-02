using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CoopEnding
{
	private static readonly float[] focusing = new float[8];

	private static readonly bool[] focusAnimation = new bool[8];

	private static readonly bool[] completed = new bool[8];

	private static readonly Dictionary<int, GameObject> beams = new Dictionary<int, GameObject>();

	internal static readonly HashSet<string> observed = new HashSet<string>();

	private static SceneLoadInfo pending;

	private static string pendingDirect;

	private static Renderer source;

	private static PlayerSlot initiator;

	private static bool active;

	private static bool accepting;

	private static bool released;

	private static float nextBeamSearch;

	private static float visualArmedUntil;

	internal static bool Active => active;

	internal static bool HoldsActor(PlayerSlot p)
	{
		if (released && p != null)
		{
			return p != initiator;
		}
		return false;
	}

	internal static bool BlocksFsm(PlayerSlot p, Fsm f)
	{
		return HoldsActor(p);
	}

	internal static bool NeedsFocus(PlayerSlot p)
	{
		if (active && !released && p != null && p != initiator && p.Ready && p.Connected)
		{
			return !completed[p.Index];
		}
		return false;
	}

	private static bool InFinalRoom()
	{
		GameManager instance = GameManager.instance;
		if (Object.op_Implicit((Object)(object)instance))
		{
			return instance.sceneName == "Room_Final_Boss_Core";
		}
		return false;
	}

	internal static bool EndingScene(string name)
	{
		if (!(name == "Cinematic_Ending_A"))
		{
			return name == "Cinematic_Ending_B";
		}
		return true;
	}

	private static void Arm(CoopSession s)
	{
		if (active || s == null || !s.Active || !InFinalRoom())
		{
			return;
		}
		int num = 0;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Ready && player.Connected)
			{
				num++;
			}
		}
		if (num < 2)
		{
			return;
		}
		active = true;
		released = false;
		initiator = s.Primary;
		for (int i = 0; i < focusing.Length; i++)
		{
			focusing[i] = 0f;
			completed[i] = false;
		}
		EmergencyWarp.CancelAll();
		PvpMatch.Stop(restorePosition: false);
		foreach (PlayerSlot player2 in s.Players)
		{
			if (player2 != initiator)
			{
			}
			if (!player2.Ready || !Object.op_Implicit((Object)(object)player2.Hero))
			{
				continue;
			}
			if (player2.Down || player2.Hazard)
			{
				PlayerSlot playerSlot = ArenaGather.Anchor ?? initiator;
				if (playerSlot != null && s.FindSafePosition(((Component)playerSlot.Hero).transform.position, player2, out var result))
				{
					s.Recover(player2, respawn: true, result);
				}
			}
			if (player2.Alive && player2.Vitals.Soul < player2.Vitals.FocusCost)
			{
				player2.Vitals.Soul = Math.Min(player2.Vitals.MaxSoul, Math.Max(33, player2.Vitals.FocusCost));
				s.Commit(player2);
			}
		}
		Diagnostics.Write("ENDING cooperative Focus armed players=" + num);
	}

	internal static bool Defer(SceneLoadInfo info, CoopSession s)
	{
		return false;
	}

	internal static bool DeferDirect(string scene, CoopSession s)
	{
		return false;
	}

	private static void NativeFocus(PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || focusAnimation[p.Index])
		{
			return;
		}
		PlayMakerFSM val = PlayMakerFSM.FindFsmOnGameObject(((Component)p.Hero).gameObject, "Spell Control");
		if (!Object.op_Implicit((Object)(object)val) || val.Fsm == null)
		{
			Diagnostics.Write("ENDING P" + (p.Index + 1) + " missing Spell Control");
			return;
		}
		bool flag = false;
		FsmState[] states = val.Fsm.States;
		for (int i = 0; i < states.Length; i++)
		{
			if (states[i].Name == "Focus Start D")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Diagnostics.Write("ENDING P" + (p.Index + 1) + " missing native Focus Start D");
			return;
		}
		focusAnimation[p.Index] = true;
		using (PlayerContext.Enter(p))
		{
			val.Fsm.SetState("Focus Start D");
		}
		Diagnostics.Write("ENDING native special Focus started P" + (p.Index + 1));
	}

	internal static void Tick(CoopSession s)
	{
		//Discarded unreachable code: IL_0257
		if (!active)
		{
			return;
		}
		if (!InFinalRoom() || s.TeamWipe)
		{
			Reset();
			return;
		}
		if (released)
		{
			UpdateBeams(s);
			return;
		}
		bool flag = true;
		int num = 0;
		foreach (PlayerSlot player in s.Players)
		{
			if (player == initiator || !player.Ready || !player.Connected)
			{
				continue;
			}
			num++;
			bool flag2 = player.Actions != null && (((OneAxisInputControl)player.Actions.focus).IsPressed || ((OneAxisInputControl)player.Actions.cast).IsPressed);
			if (!player.Alive)
			{
				focusing[player.Index] = 0f;
				flag = false;
				continue;
			}
			if (flag2)
			{
				NativeFocus(player);
			}
			else if (!completed[player.Index])
			{
				focusAnimation[player.Index] = false;
				focusing[player.Index] = 0f;
			}
			if (!completed[player.Index])
			{
				PlayMakerFSM val = PlayMakerFSM.FindFsmOnGameObject(((Component)player.Hero).gameObject, "Spell Control");
				string text = ((Object.op_Implicit((Object)(object)val) && val.Fsm != null && val.Fsm.ActiveState != null) ? val.Fsm.ActiveState.Name : "");
				bool flag3 = text == "Focus D" || text == "Keep Focus";
				focusing[player.Index] = ((flag2 && focusAnimation[player.Index] && flag3) ? Mathf.Min(1f, focusing[player.Index] + Time.deltaTime) : 0f);
				if (focusing[player.Index] >= 0.65f)
				{
					completed[player.Index] = true;
					CoopEndingFx.Celebrate(player, climax: false);
					Diagnostics.Write("ENDING native Focus P" + (player.Index + 1) + " state=" + text);
				}
			}
			if (!completed[player.Index])
			{
				flag = false;
			}
		}
		UpdateBeams(s);
		if (!flag || (pending == null && pendingDirect == null))
		{
			return;
		}
		if (!(visualArmedUntil < 0f))
		{
			visualArmedUntil = 0f - (Time.unscaledTime + 7.2f);
		}
		else if (Time.unscaledTime + visualArmedUntil >= 0f)
		{
			accepting = true;
			Diagnostics.Write("ENDING cooperative Focus complete players=" + (num + 1) + " scene=" + (pendingDirect ?? pending.SceneName));
			CoopEndingFx.Celebrate(initiator, climax: true);
			if (!Object.op_Implicit((Object)(object)source))
			{
				Diagnostics.Write("ENDING native beam was not visible for replication; please include the Local8 log with the in-game result");
			}
			if (pendingDirect != null)
			{
				GameManager.instance.LoadScene(pendingDirect);
			}
			else
			{
				GameManager.instance.BeginSceneTransition(pending);
			}
			pending = null;
			pendingDirect = null;
		}
	}

	internal static void BeforeState(FsmState state)
	{
		if (!InFinalRoom() || state == null || state.Fsm == null || !Object.op_Implicit((Object)(object)state.Fsm.GameObject))
		{
			return;
		}
		string text = (state.Fsm.Name + "/" + ((Object)state.Fsm.GameObject).name + "/" + state.Name).ToLowerInvariant();
		if ((text.Contains("focus") || text.Contains("absorb") || text.Contains("ending")) && observed.Add(text))
		{
			Diagnostics.Write("ENDING state " + text);
		}
		if (text.Contains("corpse/boss corpse/set knight focus"))
		{
			Arm(Plugin.Self.Session);
			if (active && !(visualArmedUntil < 0f))
			{
				visualArmedUntil = 0f - (Time.unscaledTime + 7.2f);
			}
		}
		if (active && text.Contains("spell control/knight/focus start") && observed.Add("local8 native p1 focus fx"))
		{
			PlayerSlot player = initiator;
			if (!(visualArmedUntil < 0f))
			{
				visualArmedUntil = 0f - (Time.unscaledTime + 7.2f);
			}
			CoopEndingFx.Celebrate(player, climax: false);
		}
		if ((text.Contains("absorb") || (text.Contains("ending") && text.Contains("focus"))) && !Object.op_Implicit((Object)(object)state.Fsm.GameObject.GetComponentInParent<HeroController>()))
		{
			_ = Time.unscaledTime + 8f;
		}
	}

	private static Renderer FindNativeBeam(CoopSession s)
	{
		if (Time.unscaledTime < nextBeamSearch)
		{
			return null;
		}
		nextBeamSearch = Time.unscaledTime + 0.25f;
		Renderer val = null;
		float num = float.MaxValue;
		Renderer[] array = Object.FindObjectsOfType<Renderer>();
		foreach (Renderer val2 in array)
		{
			if (!Object.op_Implicit((Object)(object)val2) || !val2.enabled || !((Component)val2).gameObject.activeInHierarchy || ((Object)val2).name.StartsWith("Local8") || Object.op_Implicit((Object)(object)((Component)val2).GetComponentInParent<HeroController>()) || Object.op_Implicit((Object)(object)((Component)val2).GetComponentInParent<DamageHero>()))
			{
				continue;
			}
			string text = "";
			Transform val3 = ((Component)val2).transform;
			while (Object.op_Implicit((Object)(object)val3) && text.Length < 140)
			{
				text = text + " " + ((Object)val3).name;
				val3 = val3.parent;
			}
			text = text.ToLowerInvariant();
			if (text.Contains("local8") || (!text.Contains("beam") && !text.Contains("absorb")))
			{
				continue;
			}
			float num2 = float.MaxValue;
			foreach (PlayerSlot player in s.Players)
			{
				if (player.Ready && Object.op_Implicit((Object)(object)player.Hero))
				{
					float num3 = num2;
					Vector2 val4 = Vector2.op_Implicit(((Component)val2).transform.position - ((Component)player.Hero).transform.position);
					num2 = Mathf.Min(num3, ((Vector2)(ref val4)).sqrMagnitude);
				}
			}
			if (!(num2 > 1600f) && num2 < num)
			{
				num = num2;
				val = val2;
			}
		}
		if (Object.op_Implicit((Object)(object)val))
		{
			Diagnostics.Write("ENDING native beam=" + ((Object)val).name);
		}
		return val;
	}

	private static GameObject CloneBeam(Renderer original, int player)
	{
		GameObject val = new GameObject("Local8 Ending staging");
		val.SetActive(false);
		GameObject val2 = null;
		try
		{
			val2 = Object.Instantiate<GameObject>(((Component)original).gameObject, val.transform, false);
			val2.SetActive(false);
			((Object)val2).name = "Local8 Ending Beam P" + (player + 1);
			Collider2D[] componentsInChildren = val2.GetComponentsInChildren<Collider2D>(true);
			foreach (Collider2D val3 in componentsInChildren)
			{
				if (Object.op_Implicit((Object)(object)val3))
				{
					Object.DestroyImmediate((Object)(object)val3);
				}
			}
			MonoBehaviour[] componentsInChildren2 = val2.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (MonoBehaviour val4 in componentsInChildren2)
			{
				if (Object.op_Implicit((Object)(object)val4) && !(val4 is tk2dSprite))
				{
					Object.DestroyImmediate((Object)(object)val4);
				}
			}
			AudioSource[] componentsInChildren3 = val2.GetComponentsInChildren<AudioSource>(true);
			foreach (AudioSource val5 in componentsInChildren3)
			{
				if (Object.op_Implicit((Object)(object)val5))
				{
					Object.DestroyImmediate((Object)(object)val5);
				}
			}
			val2.transform.SetParent((Transform)null, false);
			val2.transform.position = ((Component)original).transform.position;
			val2.transform.rotation = ((Component)original).transform.rotation;
			val2.SetActive(true);
			return val2;
		}
		catch (Exception ex)
		{
			if (Object.op_Implicit((Object)(object)val2))
			{
				Object.Destroy((Object)(object)val2);
			}
			Diagnostics.Throttled("ENDING native beam clone", ex);
			return null;
		}
		finally
		{
			Object.Destroy((Object)(object)val);
		}
	}

	private static void UpdateBeams(CoopSession s)
	{
		if (!Object.op_Implicit((Object)(object)source))
		{
			source = FindNativeBeam(s);
		}
		if (!Object.op_Implicit((Object)(object)source))
		{
			foreach (GameObject value2 in beams.Values)
			{
				if (Object.op_Implicit((Object)(object)value2))
				{
					value2.SetActive(false);
				}
			}
			return;
		}
		foreach (KeyValuePair<int, GameObject> beam in beams)
		{
			if (Object.op_Implicit((Object)(object)beam.Value))
			{
				PlayerSlot playerSlot = ((beam.Key < s.Players.Count) ? s.Players[beam.Key] : null);
				beam.Value.SetActive(source.enabled && ((Component)source).gameObject.activeInHierarchy && playerSlot != null && playerSlot.Alive);
			}
		}
		Vector3 position = ((Component)s.Primary.Hero).transform.position;
		Vector3 position2 = ((Component)source).transform.position;
		foreach (PlayerSlot player in s.Players)
		{
			if (player == s.Primary || !player.Ready || !Object.op_Implicit((Object)(object)player.Hero) || !player.Alive)
			{
				continue;
			}
			if (!beams.TryGetValue(player.Index, out var value) || !Object.op_Implicit((Object)(object)value))
			{
				value = CloneBeam(source, player.Index);
				if (!Object.op_Implicit((Object)(object)value))
				{
					continue;
				}
				beams[player.Index] = value;
			}
			Renderer obj = source;
			LineRenderer val = (LineRenderer)(object)((obj is LineRenderer) ? obj : null);
			LineRenderer component = value.GetComponent<LineRenderer>();
			if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)component) && val.positionCount >= 2)
			{
				component.positionCount = val.positionCount;
				for (int i = 0; i < val.positionCount; i++)
				{
					Vector3 val2 = val.GetPosition(i);
					if (!val.useWorldSpace)
					{
						val2 = ((Component)val).transform.TransformPoint(val2);
					}
					float num = (float)i / (float)(val.positionCount - 1);
					val2 += num * (((Component)player.Hero).transform.position - position);
					component.SetPosition(i, component.useWorldSpace ? val2 : ((Component)component).transform.InverseTransformPoint(val2));
				}
			}
			else
			{
				Vector3 val3 = position - position2;
				Vector3 val4 = ((Component)player.Hero).transform.position - position2;
				value.transform.position = position2;
				if (((Vector3)(ref val3)).sqrMagnitude > 0.01f)
				{
					value.transform.rotation = Quaternion.FromToRotation(val3, val4) * ((Component)source).transform.rotation;
					value.transform.localScale = new Vector3(((Component)source).transform.localScale.x * ((Vector3)(ref val4)).magnitude / ((Vector3)(ref val3)).magnitude, ((Component)source).transform.localScale.y, ((Component)source).transform.localScale.z);
				}
			}
		}
	}

	internal static void VisualTick(CoopSession s)
	{
	}

	internal static void Reset()
	{
		active = (accepting = (released = false));
		pending = null;
		pendingDirect = null;
		source = null;
		initiator = null;
		nextBeamSearch = (visualArmedUntil = 0f);
		observed.Clear();
		CoopEndingFx.Reset();
		foreach (GameObject value in beams.Values)
		{
			if (Object.op_Implicit((Object)(object)value))
			{
				Object.Destroy((Object)(object)value);
			}
		}
		beams.Clear();
		for (int i = 0; i < focusing.Length; i++)
		{
			focusing[i] = 0f;
			focusAnimation[i] = (completed[i] = false);
		}
	}
}
