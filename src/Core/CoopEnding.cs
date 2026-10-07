using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CoopEnding
{
	private static readonly float[] focusing = new float[8];

	private static readonly bool[] focusAnimation = new bool[8];

	private static readonly bool[] completed = new bool[8];

	private static readonly Dictionary<int, GameObject> beams = new Dictionary<int, GameObject>();

	internal static readonly HashSet<string> observed = new HashSet<string>();

	private static GameManager.SceneLoadInfo pending;

	private static string pendingDirect;

	private static Renderer source;

	private static PlayerSlot initiator;

	private static bool active;

	private static bool accepting;

	private static bool released;

	private static float nextBeamSearch;

	private static float visualArmedUntil;

	internal static bool Active => EndingAbsorption.Active;

	internal static bool HoldsActor(PlayerSlot p)
	{
		return EndingAbsorption.Holds(p);
	}

	internal static bool BlocksFsm(PlayerSlot p, Fsm f)
	{
		return EndingAbsorption.Blocks(p, f);
	}

	internal static bool NeedsFocus(PlayerSlot p)
	{
		return EndingAbsorption.NeedsFocus(p);
	}

	private static bool InFinalRoom()
	{
		GameManager instance = GameManager.instance;
		if ((bool)instance)
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
			if (!player2.Ready || !player2.Hero)
			{
				continue;
			}
			if (player2.Down || player2.Hazard)
			{
				PlayerSlot playerSlot = ArenaGather.Anchor ?? initiator;
				if (playerSlot != null && s.FindSafePosition(playerSlot.Hero.transform.position, player2, out var result))
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

	internal static bool Defer(GameManager.SceneLoadInfo info, CoopSession s)
	{
		return false;
	}

	internal static bool DeferDirect(string scene, CoopSession s)
	{
		return false;
	}

	private static void NativeFocus(PlayerSlot p)
	{
		if (p == null || !p.Hero || focusAnimation[p.Index])
		{
			return;
		}
		PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(p.Hero.gameObject, "Spell Control");
		if (!playMakerFSM || playMakerFSM.Fsm == null)
		{
			Diagnostics.Write("ENDING P" + (p.Index + 1) + " missing Spell Control");
			return;
		}
		bool flag = false;
		FsmState[] states = playMakerFSM.Fsm.States;
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
			playMakerFSM.Fsm.SetState("Focus Start D");
		}
		Diagnostics.Write("ENDING native special Focus started P" + (p.Index + 1));
	}

	internal static void Tick(CoopSession s)
	{
		EndingAbsorption.Tick(s);
	}

	internal static void BeforeState(FsmState state)
	{
		EndingAbsorption.Observe(state);
	}

	private static Renderer FindNativeBeam(CoopSession s)
	{
		if (Time.unscaledTime < nextBeamSearch)
		{
			return null;
		}
		nextBeamSearch = Time.unscaledTime + 0.25f;
		Renderer renderer = null;
		float num = float.MaxValue;
		Renderer[] array = UnityEngine.Object.FindObjectsOfType<Renderer>();
		foreach (Renderer renderer2 in array)
		{
			if (!renderer2 || !renderer2.enabled || !renderer2.gameObject.activeInHierarchy || renderer2.name.StartsWith("Local8") || (bool)renderer2.GetComponentInParent<HeroController>() || (bool)renderer2.GetComponentInParent<DamageHero>())
			{
				continue;
			}
			string text = "";
			Transform transform = renderer2.transform;
			while ((bool)transform && text.Length < 140)
			{
				text = text + " " + transform.name;
				transform = transform.parent;
			}
			text = text.ToLowerInvariant();
			if (text.Contains("local8") || (!text.Contains("beam") && !text.Contains("absorb")))
			{
				continue;
			}
			float num2 = float.MaxValue;
			foreach (PlayerSlot player in s.Players)
			{
				if (player.Ready && (bool)player.Hero)
				{
					num2 = Mathf.Min(num2, ((Vector2)(renderer2.transform.position - player.Hero.transform.position)).sqrMagnitude);
				}
			}
			if (!(num2 > 1600f) && num2 < num)
			{
				num = num2;
				renderer = renderer2;
			}
		}
		if ((bool)renderer)
		{
			Diagnostics.Write("ENDING native beam=" + renderer.name);
		}
		return renderer;
	}

	private static GameObject CloneBeam(Renderer original, int player)
	{
		GameObject gameObject = new GameObject("Local8 Ending staging");
		gameObject.SetActive(value: false);
		GameObject gameObject2 = null;
		try
		{
			gameObject2 = UnityEngine.Object.Instantiate(original.gameObject, gameObject.transform, worldPositionStays: false);
			gameObject2.SetActive(value: false);
			gameObject2.name = "Local8 Ending Beam P" + (player + 1);
			Collider2D[] componentsInChildren = gameObject2.GetComponentsInChildren<Collider2D>(includeInactive: true);
			foreach (Collider2D collider2D in componentsInChildren)
			{
				if ((bool)collider2D)
				{
					UnityEngine.Object.DestroyImmediate(collider2D);
				}
			}
			MonoBehaviour[] componentsInChildren2 = gameObject2.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			foreach (MonoBehaviour monoBehaviour in componentsInChildren2)
			{
				if ((bool)monoBehaviour && !(monoBehaviour is tk2dSprite))
				{
					UnityEngine.Object.DestroyImmediate(monoBehaviour);
				}
			}
			AudioSource[] componentsInChildren3 = gameObject2.GetComponentsInChildren<AudioSource>(includeInactive: true);
			foreach (AudioSource audioSource in componentsInChildren3)
			{
				if ((bool)audioSource)
				{
					UnityEngine.Object.DestroyImmediate(audioSource);
				}
			}
			gameObject2.transform.SetParent(null, worldPositionStays: false);
			gameObject2.transform.position = original.transform.position;
			gameObject2.transform.rotation = original.transform.rotation;
			gameObject2.SetActive(value: true);
			return gameObject2;
		}
		catch (Exception ex)
		{
			if ((bool)gameObject2)
			{
				UnityEngine.Object.Destroy(gameObject2);
			}
			Diagnostics.Throttled("ENDING native beam clone", ex);
			return null;
		}
		finally
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private static void UpdateBeams(CoopSession s)
	{
		if (!source)
		{
			source = FindNativeBeam(s);
		}
		if (!source)
		{
			foreach (GameObject value2 in beams.Values)
			{
				if ((bool)value2)
				{
					value2.SetActive(value: false);
				}
			}
			return;
		}
		foreach (KeyValuePair<int, GameObject> beam in beams)
		{
			if ((bool)beam.Value)
			{
				PlayerSlot playerSlot = ((beam.Key < s.Players.Count) ? s.Players[beam.Key] : null);
				beam.Value.SetActive(source.enabled && source.gameObject.activeInHierarchy && playerSlot != null && playerSlot.Alive);
			}
		}
		Vector3 position = s.Primary.Hero.transform.position;
		Vector3 position2 = source.transform.position;
		foreach (PlayerSlot player in s.Players)
		{
			if (player == s.Primary || !player.Ready || !player.Hero || !player.Alive)
			{
				continue;
			}
			if (!beams.TryGetValue(player.Index, out var value) || !value)
			{
				value = CloneBeam(source, player.Index);
				if (!value)
				{
					continue;
				}
				beams[player.Index] = value;
			}
			LineRenderer lineRenderer = source as LineRenderer;
			LineRenderer component = value.GetComponent<LineRenderer>();
			if ((bool)lineRenderer && (bool)component && lineRenderer.positionCount >= 2)
			{
				component.positionCount = lineRenderer.positionCount;
				for (int i = 0; i < lineRenderer.positionCount; i++)
				{
					Vector3 vector = lineRenderer.GetPosition(i);
					if (!lineRenderer.useWorldSpace)
					{
						vector = lineRenderer.transform.TransformPoint(vector);
					}
					float num = (float)i / (float)(lineRenderer.positionCount - 1);
					vector += num * (player.Hero.transform.position - position);
					component.SetPosition(i, component.useWorldSpace ? vector : component.transform.InverseTransformPoint(vector));
				}
			}
			else
			{
				Vector3 fromDirection = position - position2;
				Vector3 toDirection = player.Hero.transform.position - position2;
				value.transform.position = position2;
				if (fromDirection.sqrMagnitude > 0.01f)
				{
					value.transform.rotation = Quaternion.FromToRotation(fromDirection, toDirection) * source.transform.rotation;
					value.transform.localScale = new Vector3(source.transform.localScale.x * toDirection.magnitude / fromDirection.magnitude, source.transform.localScale.y, source.transform.localScale.z);
				}
			}
		}
	}

	internal static void VisualTick(CoopSession s)
	{
		EndingAbsorption.Visual(s);
	}

	internal static void Reset()
	{
		EndingAbsorption.Reset();
	}
}
