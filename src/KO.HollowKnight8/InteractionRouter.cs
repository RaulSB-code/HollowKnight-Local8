using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using TMPro;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class InteractionRouter
{
	private sealed class Entry
	{
		internal Fsm Fsm;

		internal PlayerSlot Owner;

		internal bool Busy;

		internal bool Bench;

		internal bool Up;

		internal bool Down;

		internal bool Scene;

		internal float LastTouch;

		internal float RequestedAt = -100f;

		internal float NextAllowed;

		internal Collider2D[] Triggers;

		internal TriggerEnterEvent[] Subscribed;

		internal readonly float[] Contacts = new float[8];
	}

	private static readonly Dictionary<Fsm, Entry> entries = new Dictionary<Fsm, Entry>();

	private static readonly Dictionary<TriggerEnterEvent, Dictionary<Collider2D, PlayerSlot>> subscribedContacts = new Dictionary<TriggerEnterEvent, Dictionary<Collider2D, PlayerSlot>>();

	private static readonly HashSet<Fsm> ignored = new HashSet<Fsm>();

	private static Entry active;

	private static float nextInputDiagnostic;

	private static int promptScanFrame = -1;

	private static bool promptTextVisible;

	internal static PlayerSlot ActivePlayer
	{
		get
		{
			if (active != null)
			{
				Refresh(active);
				if (active != null && active.Busy)
				{
					return active.Owner;
				}
			}
			return null;
		}
	}

	private static string Kind(Fsm f)
	{
		return (f.Name + " " + (Object.op_Implicit((Object)(object)f.GameObject) ? ((Object)f.GameObject).name : "")).ToLowerInvariant();
	}

	internal static bool IsUi(Fsm f)
	{
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return false;
		}
		GameCameras instance = GameCameras.instance;
		UIManager instance2 = UIManager.instance;
		Transform transform = f.GameObject.transform;
		if (!Object.op_Implicit((Object)(object)instance) || ((!Object.op_Implicit((Object)(object)instance.hudCamera) || !transform.IsChildOf(((Component)instance.hudCamera).transform)) && (!Object.op_Implicit((Object)(object)instance.hudCanvas) || !transform.IsChildOf(instance.hudCanvas.transform))))
		{
			if (Object.op_Implicit((Object)(object)instance2) && Object.op_Implicit((Object)(object)instance2.UICanvas))
			{
				return transform.IsChildOf(((Component)instance2.UICanvas).transform);
			}
			return false;
		}
		return true;
	}

	internal static bool ScreenList(Fsm f)
	{
		if (f != null && Object.op_Implicit((Object)(object)f.GameObject))
		{
			if (!string.Equals(f.Name, "ui_list_getinput", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(f.Name, "ui_list", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static Entry Describe(Fsm f)
	{
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return null;
		}
		if (entries.TryGetValue(f, out var value))
		{
			return value;
		}
		if (ignored.Contains(f))
		{
			return null;
		}
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active)
		{
			return null;
		}
		if (IsUi(f) || ScreenList(f) || coopSession.Resolve(f) != null || Object.op_Implicit((Object)(object)f.GameObject.GetComponentInParent<HealthManager>()))
		{
			ignored.Add(f);
			return null;
		}
		Kind(f);
		bool flag = (Object)(object)f.GameObject.GetComponentInParent<RestBench>() != (Object)null || (Object)(object)f.GameObject.GetComponentInChildren<RestBench>(true) != (Object)null;
		value = new Entry
		{
			Fsm = f,
			Bench = (flag && string.Equals(f.Name, "Bench Control", StringComparison.OrdinalIgnoreCase))
		};
		if (f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState val in states)
			{
				if (val.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = val.Actions;
				foreach (FsmStateAction val2 in actions)
				{
					if (val2 != null)
					{
						string name = ((object)val2).GetType().Name;
						if (name == "ListenForUp")
						{
							value.Up = true;
						}
						if (name == "ListenForDown")
						{
							value.Down = true;
						}
						switch (name)
						{
						case "BeginSceneTransition":
						case "LoadScene":
						case "TransitionToScene":
							value.Scene = true;
							break;
						}
					}
				}
			}
		}
		if (!value.Up && !value.Down && !value.Scene)
		{
			if (f.States != null && f.States.Length != 0)
			{
				ignored.Add(f);
			}
			return null;
		}
		if (!value.Up && !value.Down)
		{
			value.Up = true;
		}
		Transform transform = f.GameObject.transform;
		Collider2D[] array = ((Component)transform).GetComponentsInChildren<Collider2D>(true);
		if (array.Length == 0 && Object.op_Implicit((Object)(object)transform.parent))
		{
			array = ((Component)transform.parent).GetComponents<Collider2D>();
		}
		List<Collider2D> list = new List<Collider2D>();
		Collider2D[] array2 = array;
		Bounds bounds;
		foreach (Collider2D val3 in array2)
		{
			if (!Object.op_Implicit((Object)(object)val3) || !val3.isTrigger || Object.op_Implicit((Object)(object)((Component)val3).GetComponentInParent<HeroController>()))
			{
				continue;
			}
			bounds = val3.bounds;
			if (((Bounds)(ref bounds)).size.x < 30f)
			{
				bounds = val3.bounds;
				if (((Bounds)(ref bounds)).size.y < 20f)
				{
					list.Add(val3);
				}
			}
		}
		List<TriggerEnterEvent> list2 = new List<TriggerEnterEvent>();
		if (f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState val4 in states)
			{
				if (val4.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = val4.Actions;
				foreach (FsmStateAction obj in actions)
				{
					TriggerEnterEventSubscribe val5 = (TriggerEnterEventSubscribe)(object)((obj is TriggerEnterEventSubscribe) ? obj : null);
					TriggerEnterEvent val6 = (TriggerEnterEvent)((val5 == null || val5.trigger == null) ? null : /*isinst with value type is only supported in some contexts*/);
					if (Object.op_Implicit((Object)(object)val6))
					{
						if (!list2.Contains(val6))
						{
							list2.Add(val6);
						}
						Collider2D component = ((Component)val6).GetComponent<Collider2D>();
						if (Object.op_Implicit((Object)(object)component) && component.isTrigger && !list.Contains(component))
						{
							list.Add(component);
						}
					}
				}
			}
		}
		if (list.Count == 0 && !value.Scene && !value.Bench)
		{
			Transform parent = transform.parent;
			int num = 0;
			while (Object.op_Implicit((Object)(object)parent) && num < 2 && parent.childCount <= 16)
			{
				array2 = ((Component)parent).GetComponentsInChildren<Collider2D>(true);
				foreach (Collider2D val7 in array2)
				{
					if (!Object.op_Implicit((Object)(object)val7) || !val7.isTrigger || Object.op_Implicit((Object)(object)((Component)val7).GetComponentInParent<HeroController>()))
					{
						continue;
					}
					bounds = val7.bounds;
					if (((Bounds)(ref bounds)).size.x < 10f)
					{
						bounds = val7.bounds;
						if (((Bounds)(ref bounds)).size.y < 10f && Vector2.Distance(Vector2.op_Implicit(((Component)val7).transform.position), Vector2.op_Implicit(transform.position)) < 8f && !list.Contains(val7))
						{
							list.Add(val7);
						}
					}
				}
				if (list.Count > 0)
				{
					break;
				}
				num++;
				parent = parent.parent;
			}
		}
		value.Triggers = list.ToArray();
		value.Subscribed = list2.ToArray();
		entries[f] = value;
		return value;
	}

	private static bool Valid(PlayerSlot p)
	{
		if (p != null && Object.op_Implicit((Object)(object)p.Hero) && p.Ready && p.Alive && p.Connected && !EmergencyWarp.Active(p))
		{
			return !BenchSeats.Custom(p);
		}
		return false;
	}

	private static bool PromptText(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		value = value.Trim();
		if (value.Length < 3 || value.Length > 40 || value.IndexOf('\n') >= 0)
		{
			return false;
		}
		int num = 0;
		string text = value;
		foreach (char c in text)
		{
			if (char.IsLetter(c))
			{
				num++;
				if (char.IsLower(c))
				{
					return false;
				}
			}
		}
		return num >= 3;
	}

	private static bool PromptTextVisible()
	{
		if (promptScanFrame == Time.frameCount)
		{
			return promptTextVisible;
		}
		promptScanFrame = Time.frameCount;
		promptTextVisible = false;
		TMP_Text[] array = Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).isActiveAndEnabled && PromptText(val.text))
			{
				promptTextVisible = true;
				return true;
			}
		}
		tk2dTextMesh[] array2 = Object.FindObjectsOfType<tk2dTextMesh>();
		foreach (tk2dTextMesh val2 in array2)
		{
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).isActiveAndEnabled && PromptText(val2.text))
			{
				promptTextVisible = true;
				return true;
			}
		}
		return false;
	}

	private static bool NativePromptReady(Entry e)
	{
		FsmState activeState = e.Fsm.ActiveState;
		if (activeState == null || activeState.Actions == null)
		{
			return false;
		}
		bool flag = false;
		FsmStateAction[] actions = activeState.Actions;
		foreach (FsmStateAction val in actions)
		{
			if (val != null && val.Enabled && ((e.Up && val is ListenForUp) || (e.Down && val is ListenForDown)))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		string text = (activeState.Name ?? "").ToLowerInvariant();
		if (text.Contains("range") || text.Contains("prompt") || text.Contains("listen") || text.Contains("near") || text.Contains("interact") || text.Contains("talk"))
		{
			return true;
		}
		CoopSession session = Plugin.Self.Session;
		if (session == null || !session.Gameplay)
		{
			return false;
		}
		bool flag2 = false;
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Actions != null && (((OneAxisInputControl)player.Actions.up).WasPressed || ((OneAxisInputControl)player.Actions.down).WasPressed))
			{
				flag2 = true;
				break;
			}
		}
		if (flag2)
		{
			return PromptTextVisible();
		}
		return false;
	}

	private static Vector2 PromptAnchor(Entry e, Vector2 at)
	{
		Vector2 val = Vector2.op_Implicit(e.Fsm.GameObject.transform.position);
		Vector2 val2 = val - at;
		float num = ((Vector2)(ref val2)).sqrMagnitude;
		Collider2D[] triggers = e.Triggers;
		foreach (Collider2D val3 in triggers)
		{
			if (Object.op_Implicit((Object)(object)val3) && ((Behaviour)val3).enabled && ((Component)val3).gameObject.activeInHierarchy)
			{
				Bounds bounds = val3.bounds;
				val2 = Vector2.op_Implicit(((Bounds)(ref bounds)).ClosestPoint(Vector2.op_Implicit(at))) - at;
				float sqrMagnitude = ((Vector2)(ref val2)).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					val = Vector2.op_Implicit(((Bounds)(ref bounds)).center);
				}
			}
		}
		return val;
	}

	private static bool NativePromptNear(Entry e, PlayerSlot p)
	{
		if (!Valid(p) || e.Bench || !NativePromptReady(e))
		{
			return false;
		}
		Vector2 val = Vector2.op_Implicit(((Component)p.Hero).transform.position);
		Vector2 val2 = PromptAnchor(e, val);
		if (Mathf.Abs(val.x - val2.x) < 8f)
		{
			return Mathf.Abs(val.y - val2.y) < 5f;
		}
		return false;
	}

	private static bool Eligible(Entry e, PlayerSlot p)
	{
		if (!Inside(e, p))
		{
			return NativePromptNear(e, p);
		}
		return true;
	}

	internal static bool BenchSeatNear(Fsm f, PlayerSlot p)
	{
		if (!Valid(p) || f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return false;
		}
		RestBench val = f.GameObject.GetComponentInParent<RestBench>() ?? f.GameObject.GetComponentInChildren<RestBench>(true);
		Vector3 val2 = (Object.op_Implicit((Object)(object)val) ? ((Component)val).transform.position : f.GameObject.transform.position);
		Vector3 position = ((Component)p.Hero).transform.position;
		if (Mathf.Abs(position.x - val2.x) < 1.65f)
		{
			return Mathf.Abs(position.y - val2.y) < 1.8f;
		}
		return false;
	}

	private static bool CompetingPrompt(PlayerSlot p, Entry bench)
	{
		if (p.Actions == null || !((OneAxisInputControl)p.Actions.up).IsPressed)
		{
			return false;
		}
		foreach (Entry value in entries.Values)
		{
			if (value != bench && !value.Bench && !value.Scene && value.Up && NativePromptReady(value) && Eligible(value, p))
			{
				Vector2 val = PromptAnchor(value, Vector2.op_Implicit(((Component)p.Hero).transform.position));
				Vector2 val2 = Vector2.op_Implicit(((Component)p.Hero).transform.position);
				if (Mathf.Abs(val.x - val2.x) < 3.2f && Mathf.Abs(val.y - val2.y) < 2.4f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool Inside(Entry e, PlayerSlot p)
	{
		if (!Valid(p))
		{
			return false;
		}
		if (Time.unscaledTime - e.Contacts[p.Index] < 0.25f && e.Contacts[p.Index] > 0f)
		{
			return true;
		}
		TriggerEnterEvent[] subscribed = e.Subscribed;
		foreach (TriggerEnterEvent val in subscribed)
		{
			if (!Object.op_Implicit((Object)(object)val) || !subscribedContacts.TryGetValue(val, out var value))
			{
				continue;
			}
			foreach (KeyValuePair<Collider2D, PlayerSlot> item in value)
			{
				if (Object.op_Implicit((Object)(object)item.Key) && ((Behaviour)item.Key).enabled && item.Value == p)
				{
					return true;
				}
			}
		}
		Collider2D[] triggers = e.Triggers;
		foreach (Collider2D val2 in triggers)
		{
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).enabled && ((Component)val2).gameObject.activeInHierarchy)
			{
				Bounds bounds = val2.bounds;
				((Bounds)(ref bounds)).Expand(new Vector3(0.65f, 0.35f, 0f));
				Vector3 position = ((Component)p.Hero).transform.position;
				position.z = ((Bounds)(ref bounds)).center.z;
				if (((Bounds)(ref bounds)).Contains(position))
				{
					return true;
				}
			}
		}
		if (e.Scene || Kind(e.Fsm).Contains("door") || Kind(e.Fsm).Contains("exit prompt"))
		{
			List<TransitionPoint> transitionPoints = TransitionPoint.TransitionPoints;
			if (transitionPoints != null)
			{
				foreach (TransitionPoint item2 in transitionPoints)
				{
					if (!Object.op_Implicit((Object)(object)item2) || !item2.isADoor || !((Component)item2).gameObject.activeInHierarchy || !(Mathf.Abs(((Component)item2).transform.position.x - e.Fsm.GameObject.transform.position.x) < 6f) || !(Mathf.Abs(((Component)item2).transform.position.y - e.Fsm.GameObject.transform.position.y) < 5f))
					{
						continue;
					}
					Collider2D component = ((Component)item2).GetComponent<Collider2D>();
					if (Object.op_Implicit((Object)(object)component) && ((Behaviour)component).enabled)
					{
						Bounds bounds2 = component.bounds;
						((Bounds)(ref bounds2)).Expand(new Vector3(0.7f, 0.5f, 0f));
						Vector3 position2 = ((Component)p.Hero).transform.position;
						position2.z = ((Bounds)(ref bounds2)).center.z;
						if (((Bounds)(ref bounds2)).Contains(position2))
						{
							return true;
						}
					}
					else if (Mathf.Abs(((Component)p.Hero).transform.position.x - ((Component)item2).transform.position.x) < 2f && Mathf.Abs(((Component)p.Hero).transform.position.y - ((Component)item2).transform.position.y) < 2.5f)
					{
						return true;
					}
				}
			}
		}
		if (e.Triggers.Length == 0 || (!e.Scene && !e.Bench))
		{
			Vector3 position3 = ((Component)p.Hero).transform.position;
			Vector3 position4 = e.Fsm.GameObject.transform.position;
			bool flag = Kind(e.Fsm).Contains("stag") && !Kind(e.Fsm).Contains("bell");
			if (Mathf.Abs(position3.x - position4.x) < (flag ? 8f : 2.2f) && Mathf.Abs(position3.y - position4.y) < (flag ? 5f : 2.2f))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Request(Entry e, PlayerSlot p)
	{
		if (p.Actions != null)
		{
			if (!e.Up || !((OneAxisInputControl)p.Actions.up).IsPressed)
			{
				if (e.Down)
				{
					return ((OneAxisInputControl)p.Actions.down).IsPressed;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private unsafe static void Choose(Entry e, PlayerSlot touched = null)
	{
		Refresh(e);
		if (e.Busy || Time.unscaledTime < e.NextAllowed || (active != null && active != e && active.Busy && !e.Scene))
		{
			return;
		}
		CoopSession session = Plugin.Self.Session;
		PlayerSlot playerSlot = null;
		float num = float.MaxValue;
		Vector2 val;
		foreach (PlayerSlot player in session.Players)
		{
			if (Valid(player) && (Eligible(e, player) || player == touched) && Request(e, player) && (!e.Bench || (BenchSeatNear(e.Fsm, player) && !CompetingPrompt(player, e))))
			{
				val = PromptAnchor(e, Vector2.op_Implicit(((Component)player.Hero).transform.position)) - Vector2.op_Implicit(((Component)player.Hero).transform.position);
				float sqrMagnitude = ((Vector2)(ref val)).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					playerSlot = player;
					num = sqrMagnitude;
				}
			}
		}
		if (playerSlot != null)
		{
			if (e.Scene && PvpMatch.BlockExit(playerSlot))
			{
				return;
			}
			if (e.Owner != playerSlot)
			{
				Diagnostics.Write("INTERACTION owner=P" + (playerSlot.Index + 1) + " fsm=" + e.Fsm.Name + " object=" + ((Object)e.Fsm.GameObject).name);
			}
			if (!Inside(e, playerSlot) && playerSlot.Actions != null && (((OneAxisInputControl)playerSlot.Actions.up).WasPressed || ((OneAxisInputControl)playerSlot.Actions.down).WasPressed))
			{
				string[] obj = new string[8]
				{
					"INTERACTION native prompt P",
					(playerSlot.Index + 1).ToString(),
					" fsm=",
					e.Fsm.Name,
					" state=",
					(e.Fsm.ActiveState == null) ? "none" : e.Fsm.ActiveState.Name,
					" anchor=",
					null
				};
				val = PromptAnchor(e, Vector2.op_Implicit(((Component)playerSlot.Hero).transform.position));
				obj[7] = ((object)(*(Vector2*)(&val))/*cast due to .constrained prefix*/).ToString();
				Diagnostics.Write(string.Concat(obj));
			}
			e.Owner = playerSlot;
			e.RequestedAt = (e.LastTouch = Time.unscaledTime);
			if (Kind(e.Fsm).Contains("stag") && !Kind(e.Fsm).Contains("bell"))
			{
				StagMenuRouting.NoteInteraction(playerSlot);
			}
		}
		else if (!Valid(e.Owner) || (!Eligible(e, e.Owner) && Time.unscaledTime - e.LastTouch > 0.25f))
		{
			e.Owner = (Valid(touched) ? touched : null);
			if (e.Owner == null)
			{
				foreach (PlayerSlot player2 in session.Players)
				{
					if (Inside(e, player2))
					{
						e.Owner = player2;
						break;
					}
				}
			}
		}
		if (e.Owner != null && Eligible(e, e.Owner))
		{
			e.LastTouch = Time.unscaledTime;
		}
	}

	internal static bool Trigger(Fsm f, Collider2D collider, bool exiting)
	{
		PlayerSlot playerSlot = Plugin.Self.Session.Resolve(collider);
		if (playerSlot == null)
		{
			return true;
		}
		Entry entry = Describe(f);
		if (entry == null)
		{
			return true;
		}
		if (!Valid(playerSlot))
		{
			return false;
		}
		if (entry.Bench && !exiting && !BenchSeats.Seated(playerSlot) && !BenchSeatNear(f, playerSlot))
		{
			return false;
		}
		bool flag = entry.Scene || entry.Bench || Kind(f).Contains("door") || entry.Busy;
		if (exiting)
		{
			entry.Contacts[playerSlot.Index] = 0f;
			if (entry.Owner != playerSlot && flag)
			{
				return false;
			}
			if (entry.Owner == playerSlot && !entry.Busy)
			{
				entry.LastTouch = Time.unscaledTime - 1f;
			}
			return true;
		}
		entry.Contacts[playerSlot.Index] = Time.unscaledTime;
		Choose(entry, playerSlot);
		if (!flag || entry.Owner == playerSlot)
		{
			return !Blocks(f);
		}
		return false;
	}

	internal static void TriggerContact(TriggerEnterEvent trigger, Collider2D collider, bool exiting)
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !Object.op_Implicit((Object)(object)trigger) || !Object.op_Implicit((Object)(object)collider))
		{
			return;
		}
		PlayerSlot playerSlot = coopSession.Resolve(collider);
		if (playerSlot == null)
		{
			return;
		}
		if (!subscribedContacts.TryGetValue(trigger, out var value))
		{
			if (exiting)
			{
				return;
			}
			value = new Dictionary<Collider2D, PlayerSlot>();
			subscribedContacts[trigger] = value;
		}
		if (exiting)
		{
			value.Remove(collider);
			if (value.Count == 0)
			{
				subscribedContacts.Remove(trigger);
			}
		}
		else
		{
			value[collider] = playerSlot;
		}
	}

	private static void Refresh(Entry e)
	{
		if (!Valid(e.Owner))
		{
			if (active == e)
			{
				active = null;
			}
			e.Busy = false;
			e.Owner = null;
			return;
		}
		bool flag = (e.Owner.Hero.controlReqlinquished || e.Owner.Vitals.AtBench) && (e.Busy || Time.unscaledTime - e.RequestedAt < 2f);
		if (flag && !e.Busy)
		{
			e.Busy = true;
			active = e;
		}
		else if (!flag && e.Busy)
		{
			e.Busy = false;
			if (active == e)
			{
				active = null;
			}
			e.NextAllowed = Time.unscaledTime + 0.2f;
			Diagnostics.Write("INTERACTION end P" + (e.Owner.Index + 1));
		}
	}

	internal static void RecoverChallenge(CoopSession s)
	{
		//IL_0193: Invalid comparison between Unknown and I4
		if (ChallengeSequence.BlocksRecovery || active == null || !active.Busy || active.Owner == null || active.Owner.Index == 0 || active.Fsm == null || !s.Gameplay)
		{
			return;
		}
		bool flag = active.Fsm.Name == "Challenge Start";
		bool flag2 = (active.Fsm.Name ?? "").IndexOf("inspect", StringComparison.OrdinalIgnoreCase) >= 0;
		if (!flag && !flag2)
		{
			return;
		}
		string text = ((active.Fsm.ActiveState == null) ? "" : active.Fsm.ActiveState.Name.ToLowerInvariant());
		bool flag3 = text.Contains("range") || text == "idle" || text == "finish";
		float num = Time.unscaledTime - active.RequestedAt;
		bool flag4 = flag2 && text.Contains("get up");
		if (num < (flag3 ? 1.4f : (flag ? 4f : (flag4 ? 3.2f : 6f))))
		{
			return;
		}
		PlayerSlot owner = active.Owner;
		UIManager instance = UIManager.instance;
		if (owner.Alive && owner.Ready && Object.op_Implicit((Object)(object)owner.Hero) && owner.Hero.controlReqlinquished && Object.op_Implicit((Object)(object)instance) && (int)instance.uiState == 4 && PickupCard.Owner == null && !ShopMenuRouting.MenuVisible && !ScriptedParty.Active && !CoopEnding.Active)
		{
			Entry entry = active;
			using (PlayerContext.Enter(owner))
			{
				ActorRecovery.Reset(owner);
				CoopSession.RestoreLivingVisuals(owner);
			}
			owner.ProtectionUntil = Time.time + 1f;
			entry.Busy = false;
			entry.Owner = null;
			entry.NextAllowed = Time.unscaledTime + 0.4f;
			active = null;
			Diagnostics.Write("INTERACTION animation wake P" + (owner.Index + 1) + " fsm=" + entry.Fsm.Name + " state=" + text + " after " + num + "s");
		}
	}

	internal static bool Blocks(Fsm f)
	{
		if (PvpMatch.Running && entries.TryGetValue(f, out var value))
		{
			return value.Scene;
		}
		return false;
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (active != null)
		{
			Refresh(active);
		}
		if (ScreenList(f))
		{
			PlayerSlot playerSlot = ShopMenuRouting.Resolve(f);
			if (playerSlot != null)
			{
				return playerSlot;
			}
			if (active != null && active.Busy && Valid(active.Owner))
			{
				return active.Owner;
			}
		}
		Entry entry = Describe(f);
		if (entry != null)
		{
			Choose(entry);
			if (entry.Owner != null)
			{
				return entry.Owner;
			}
		}
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return null;
		}
		foreach (KeyValuePair<Fsm, Entry> entry2 in entries)
		{
			Entry value = entry2.Value;
			if (value != entry && Object.op_Implicit((Object)(object)value.Fsm.GameObject) && f.GameObject.transform.IsChildOf(value.Fsm.GameObject.transform))
			{
				Refresh(value);
				if (Valid(value.Owner) && (value.Busy || Inside(value, value.Owner)))
				{
					return value.Owner;
				}
			}
		}
		if (active == null)
		{
			return null;
		}
		string text = Kind(f);
		if (text.Contains("dialogue") || text.Contains("conversation") || text.Contains("transition"))
		{
			return active.Owner;
		}
		if (active.Busy && IsUi(f) && (text.Contains("list") || text.Contains("choice")))
		{
			return active.Owner;
		}
		return null;
	}

	internal static bool InputOwner(Fsm f, out PlayerSlot p)
	{
		CoopSession session = Plugin.Self.Session;
		if (ScreenList(f))
		{
			p = ShopMenuRouting.Resolve(f) ?? ActivePlayer;
			if (p != null)
			{
				ShopMenuRouting.Observe(f, p);
			}
			if (p != null)
			{
				if (!p.InputBlocked)
				{
					return Valid(p);
				}
				return false;
			}
			p = session.Resolve(f);
			if (p != null)
			{
				if (!p.InputBlocked)
				{
					return Valid(p);
				}
				return false;
			}
			return true;
		}
		p = session.Resolve(f);
		if (p != null)
		{
			return !BenchSeats.Custom(p);
		}
		Entry entry = Describe(f);
		p = Resolve(f);
		if (entry == null)
		{
			if (IsUi(f) && active != null)
			{
				Refresh(active);
				if (active != null && active.Busy && Valid(active.Owner) && !Charms.NativeMenuOpen)
				{
					p = active.Owner;
					return true;
				}
			}
			p = WorldRouting.Resolve(f);
			return true;
		}
		if (p == null || (!entry.Busy && !Eligible(entry, p)) || p.InputBlocked || !Valid(p) || (entry.Bench && !BenchSeats.Seated(p) && (!BenchSeatNear(f, p) || CompetingPrompt(p, entry))))
		{
			PlayerSlot playerSlot = null;
			if (session != null)
			{
				foreach (PlayerSlot player in session.Players)
				{
					if (player.Actions != null && (((OneAxisInputControl)player.Actions.up).WasPressed || ((OneAxisInputControl)player.Actions.down).WasPressed))
					{
						playerSlot = player;
						break;
					}
				}
			}
			if (playerSlot != null && Time.unscaledTime >= nextInputDiagnostic)
			{
				nextInputDiagnostic = Time.unscaledTime + 1f;
				Diagnostics.Write("INTERACTION denied fsm=" + f.Name + " object=" + ((Object)f.GameObject).name + " state=" + ((f.ActiveState == null) ? "none" : f.ActiveState.Name) + " owner=" + ((p == null) ? "none" : ("P" + (p.Index + 1))) + " input=P" + (playerSlot.Index + 1) + " pos=" + ((object)((Component)playerSlot.Hero).transform.position/*cast due to .constrained prefix*/).ToString() + " anchor=" + ((object)PromptAnchor(entry, Vector2.op_Implicit(((Component)playerSlot.Hero).transform.position))/*cast due to .constrained prefix*/).ToString() + " native=" + NativePromptReady(entry) + " triggers=" + entry.Triggers.Length + " busy=" + entry.Busy + " blocked=" + (p != null && p.InputBlocked));
			}
			return false;
		}
		if (entry.Bench && !entry.Busy && BenchSeats.Full(f.GameObject))
		{
			return false;
		}
		return true;
	}

	internal static bool IsInteraction(Fsm f)
	{
		if (f != null)
		{
			return Describe(f) != null;
		}
		return false;
	}

	internal static bool IsBench(Fsm f)
	{
		return Describe(f)?.Bench ?? false;
	}

	internal static bool NearbyBench(Fsm f, PlayerSlot p)
	{
		Entry entry = Describe(f);
		if (entry != null && entry.Bench && Inside(entry, p) && BenchSeatNear(f, p))
		{
			return !CompetingPrompt(p, entry);
		}
		return false;
	}

	internal static GameObject BenchFor(PlayerSlot p)
	{
		foreach (Entry value in entries.Values)
		{
			if (value.Bench && value.Owner == p && (value.Busy || Inside(value, p)))
			{
				return value.Fsm.GameObject;
			}
		}
		return null;
	}

	internal static bool EventAllowed(Fsm target, FsmEventData data)
	{
		CoopSession session = Plugin.Self.Session;
		if (session == null || !session.Active)
		{
			return true;
		}
		PlayerSlot playerSlot = session.Resolve(target);
		if (playerSlot == null)
		{
			return true;
		}
		Fsm val = data?.SentByFsm;
		if (val == null)
		{
			val = WorldRouting.Current;
		}
		if (val == null || session.Resolve(val) != null)
		{
			return true;
		}
		if (Describe(val) == null)
		{
			return true;
		}
		PlayerSlot playerSlot2 = Resolve(val);
		if (playerSlot2 != null)
		{
			return playerSlot == playerSlot2;
		}
		return true;
	}

	internal static void Ready(Fsm f)
	{
		if (f != null)
		{
			ignored.Remove(f);
		}
	}

	internal static void CloseForTransition()
	{
		PlayerSlot playerSlot = ((active == null) ? null : active.Owner);
		if (active != null)
		{
			active.Busy = false;
		}
		active = null;
		DialogueCleanup.Close();
		if (playerSlot != null)
		{
			Diagnostics.Write("INTERACTION closed for scene transition owner=P" + (playerSlot.Index + 1));
		}
	}

	internal static bool BenchActor(PlayerSlot p)
	{
		foreach (Entry value in entries.Values)
		{
			if (value.Owner == p && value.Bench && (value.Busy || Time.unscaledTime - value.RequestedAt < 1f))
			{
				return true;
			}
		}
		return false;
	}

	internal static void ObserveBenches()
	{
		foreach (Entry value in entries.Values)
		{
			if (value.Bench && value.Owner != null && value.Owner.Vitals.AtBench)
			{
				BenchSeats.Observe(value.Fsm, value.Owner);
			}
		}
	}

	internal static bool DoorFallbackTick(CoopSession s)
	{
		bool result = Doorways.Tick(s);
		if (Object.op_Implicit((Object)(object)GameManager.instance) && GameManager.instance.sceneName == "Room_Final_Boss_Core" && s.Players.Count > 1)
		{
			int num = -1;
			for (int i = 0; i < s.Players.Count; i++)
			{
				PlayerSlot playerSlot = s.Players[i];
				if (playerSlot.Ready && playerSlot.Alive && Object.op_Implicit((Object)(object)playerSlot.Hero) && ((Component)playerSlot.Hero).transform.position.x > 22f)
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				for (int i = 0; i < s.Players.Count; i++)
				{
					if (i != num)
					{
						PlayerSlot playerSlot = s.Players[i];
						if (playerSlot.Ready && playerSlot.Alive && Object.op_Implicit((Object)(object)playerSlot.Hero) && ((Component)playerSlot.Hero).transform.position.x < 19f)
						{
							NativeDreamFx.TeleportTrail(playerSlot, ((Component)playerSlot.Hero).transform.position, ((Component)s.Players[num].Hero).transform.position);
							((Component)playerSlot.Hero).transform.position = ((Component)s.Players[num].Hero).transform.position;
						}
					}
				}
			}
		}
		return result;
	}

	internal static void Reset()
	{
		entries.Clear();
		ignored.Clear();
		subscribedContacts.Clear();
		active = null;
		nextInputDiagnostic = 0f;
		promptScanFrame = -1;
		promptTextVisible = false;
		Doorways.Reset();
	}
}
