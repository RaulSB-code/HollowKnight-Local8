using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
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
		return (f.Name + " " + (f.GameObject ? f.GameObject.name : "")).ToLowerInvariant();
	}

	internal static bool IsUi(Fsm f)
	{
		if (f == null || !f.GameObject)
		{
			return false;
		}
		GameCameras instance = GameCameras.instance;
		UIManager instance2 = UIManager.instance;
		Transform transform = f.GameObject.transform;
		if (!instance || ((!instance.hudCamera || !transform.IsChildOf(instance.hudCamera.transform)) && (!instance.hudCanvas || !transform.IsChildOf(instance.hudCanvas.transform))))
		{
			if ((bool)instance2 && (bool)instance2.UICanvas)
			{
				return transform.IsChildOf(instance2.UICanvas.transform);
			}
			return false;
		}
		return true;
	}

	internal static bool ScreenList(Fsm f)
	{
		if (f != null && (bool)f.GameObject)
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
		if (f == null || !f.GameObject)
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
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active)
		{
			return null;
		}
		if (IsUi(f) || ScreenList(f) || coopSession.Resolve(f) != null || (bool)f.GameObject.GetComponentInParent<HealthManager>())
		{
			ignored.Add(f);
			return null;
		}
		Kind(f);
		bool flag = f.GameObject.GetComponentInParent<RestBench>() != null || f.GameObject.GetComponentInChildren<RestBench>(includeInactive: true) != null;
		value = new Entry
		{
			Fsm = f,
			Bench = (flag && string.Equals(f.Name, "Bench Control", StringComparison.OrdinalIgnoreCase))
		};
		if (f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState fsmState in states)
			{
				if (fsmState.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = fsmState.Actions;
				foreach (FsmStateAction fsmStateAction in actions)
				{
					if (fsmStateAction != null)
					{
						string name = fsmStateAction.GetType().Name;
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
		Collider2D[] array = transform.GetComponentsInChildren<Collider2D>(includeInactive: true);
		if (array.Length == 0 && (bool)transform.parent)
		{
			array = transform.parent.GetComponents<Collider2D>();
		}
		List<Collider2D> list = new List<Collider2D>();
		Collider2D[] array2 = array;
		foreach (Collider2D collider2D in array2)
		{
			if ((bool)collider2D && collider2D.isTrigger && !collider2D.GetComponentInParent<HeroController>() && collider2D.bounds.size.x < 30f && collider2D.bounds.size.y < 20f)
			{
				list.Add(collider2D);
			}
		}
		List<TriggerEnterEvent> list2 = new List<TriggerEnterEvent>();
		if (f.States != null)
		{
			FsmState[] states = f.States;
			foreach (FsmState fsmState2 in states)
			{
				if (fsmState2.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = fsmState2.Actions;
				for (int j = 0; j < actions.Length; j++)
				{
					TriggerEnterEvent triggerEnterEvent = ((!(actions[j] is TriggerEnterEventSubscribe triggerEnterEventSubscribe) || triggerEnterEventSubscribe.trigger == null) ? null : (triggerEnterEventSubscribe.trigger.Value as TriggerEnterEvent));
					if ((bool)triggerEnterEvent)
					{
						if (!list2.Contains(triggerEnterEvent))
						{
							list2.Add(triggerEnterEvent);
						}
						Collider2D component = triggerEnterEvent.GetComponent<Collider2D>();
						if ((bool)component && component.isTrigger && !list.Contains(component))
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
			while ((bool)parent && num < 2 && parent.childCount <= 16)
			{
				array2 = parent.GetComponentsInChildren<Collider2D>(includeInactive: true);
				foreach (Collider2D collider2D2 in array2)
				{
					if ((bool)collider2D2 && collider2D2.isTrigger && !collider2D2.GetComponentInParent<HeroController>() && collider2D2.bounds.size.x < 10f && collider2D2.bounds.size.y < 10f && Vector2.Distance(collider2D2.transform.position, transform.position) < 8f && !list.Contains(collider2D2))
					{
						list.Add(collider2D2);
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
		if (p != null && (bool)p.Hero && p.Ready && p.Alive && p.Connected && !EmergencyWarp.Active(p))
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
		TMP_Text[] array = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text tMP_Text in array)
		{
			if ((bool)tMP_Text && tMP_Text.isActiveAndEnabled && PromptText(tMP_Text.text))
			{
				promptTextVisible = true;
				return true;
			}
		}
		tk2dTextMesh[] array2 = UnityEngine.Object.FindObjectsOfType<tk2dTextMesh>();
		foreach (tk2dTextMesh tk2dTextMesh in array2)
		{
			if ((bool)tk2dTextMesh && tk2dTextMesh.isActiveAndEnabled && PromptText(tk2dTextMesh.text))
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
		foreach (FsmStateAction fsmStateAction in actions)
		{
			if (fsmStateAction != null && fsmStateAction.Enabled && ((e.Up && fsmStateAction is ListenForUp) || (e.Down && fsmStateAction is ListenForDown)))
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
			if (player.Actions != null && (player.Actions.up.WasPressed || player.Actions.down.WasPressed))
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
		Vector2 vector = e.Fsm.GameObject.transform.position;
		float num = (vector - at).sqrMagnitude;
		Collider2D[] triggers = e.Triggers;
		foreach (Collider2D collider2D in triggers)
		{
			if ((bool)collider2D && collider2D.enabled && collider2D.gameObject.activeInHierarchy)
			{
				UnityEngine.Bounds bounds = collider2D.bounds;
				float sqrMagnitude = ((Vector2)bounds.ClosestPoint(at) - at).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					vector = bounds.center;
				}
			}
		}
		return vector;
	}

	private static bool NativePromptNear(Entry e, PlayerSlot p)
	{
		if (!Valid(p) || e.Bench || !NativePromptReady(e))
		{
			return false;
		}
		Vector2 at = p.Hero.transform.position;
		Vector2 vector = PromptAnchor(e, at);
		if (Mathf.Abs(at.x - vector.x) < 8f)
		{
			return Mathf.Abs(at.y - vector.y) < 5f;
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
		if (!Valid(p) || f == null || !f.GameObject)
		{
			return false;
		}
		RestBench restBench = f.GameObject.GetComponentInParent<RestBench>() ?? f.GameObject.GetComponentInChildren<RestBench>(includeInactive: true);
		Vector3 vector = (restBench ? restBench.transform.position : f.GameObject.transform.position);
		Vector3 position = p.Hero.transform.position;
		if (Mathf.Abs(position.x - vector.x) < 1.65f)
		{
			return Mathf.Abs(position.y - vector.y) < 1.8f;
		}
		return false;
	}

	private static bool CompetingPrompt(PlayerSlot p, Entry bench)
	{
		if (p.Actions == null || !p.Actions.up.IsPressed)
		{
			return false;
		}
		foreach (Entry value in entries.Values)
		{
			if (value != bench && !value.Bench && !value.Scene && value.Up && NativePromptReady(value) && Eligible(value, p))
			{
				Vector2 vector = PromptAnchor(value, p.Hero.transform.position);
				Vector2 vector2 = p.Hero.transform.position;
				if (Mathf.Abs(vector.x - vector2.x) < 3.2f && Mathf.Abs(vector.y - vector2.y) < 2.4f)
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
		foreach (TriggerEnterEvent triggerEnterEvent in subscribed)
		{
			if (!triggerEnterEvent || !subscribedContacts.TryGetValue(triggerEnterEvent, out var value))
			{
				continue;
			}
			foreach (KeyValuePair<Collider2D, PlayerSlot> item in value)
			{
				if ((bool)item.Key && item.Key.enabled && item.Value == p)
				{
					return true;
				}
			}
		}
		Collider2D[] triggers = e.Triggers;
		foreach (Collider2D collider2D in triggers)
		{
			if ((bool)collider2D && collider2D.enabled && collider2D.gameObject.activeInHierarchy)
			{
				UnityEngine.Bounds bounds = collider2D.bounds;
				bounds.Expand(new Vector3(0.65f, 0.35f, 0f));
				Vector3 position = p.Hero.transform.position;
				position.z = bounds.center.z;
				if (bounds.Contains(position))
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
					if (!item2 || !item2.isADoor || !item2.gameObject.activeInHierarchy || !(Mathf.Abs(item2.transform.position.x - e.Fsm.GameObject.transform.position.x) < 6f) || !(Mathf.Abs(item2.transform.position.y - e.Fsm.GameObject.transform.position.y) < 5f))
					{
						continue;
					}
					Collider2D component = item2.GetComponent<Collider2D>();
					if ((bool)component && component.enabled)
					{
						UnityEngine.Bounds bounds2 = component.bounds;
						bounds2.Expand(new Vector3(0.7f, 0.5f, 0f));
						Vector3 position2 = p.Hero.transform.position;
						position2.z = bounds2.center.z;
						if (bounds2.Contains(position2))
						{
							return true;
						}
					}
					else if (Mathf.Abs(p.Hero.transform.position.x - item2.transform.position.x) < 2f && Mathf.Abs(p.Hero.transform.position.y - item2.transform.position.y) < 2.5f)
					{
						return true;
					}
				}
			}
		}
		if (e.Triggers.Length == 0 || (!e.Scene && !e.Bench))
		{
			Vector3 position3 = p.Hero.transform.position;
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
			if (!e.Up || !p.Actions.up.IsPressed)
			{
				if (e.Down)
				{
					return p.Actions.down.IsPressed;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static void Choose(Entry e, PlayerSlot touched = null)
	{
		Refresh(e);
		if (e.Busy || Time.unscaledTime < e.NextAllowed || (active != null && active != e && active.Busy && !e.Scene))
		{
			return;
		}
		CoopSession session = Plugin.Self.Session;
		PlayerSlot playerSlot = null;
		float num = float.MaxValue;
		foreach (PlayerSlot player in session.Players)
		{
			if (Valid(player) && (Eligible(e, player) || player == touched) && Request(e, player) && (!e.Bench || (BenchSeatNear(e.Fsm, player) && !CompetingPrompt(player, e))))
			{
				float sqrMagnitude = (PromptAnchor(e, player.Hero.transform.position) - (Vector2)player.Hero.transform.position).sqrMagnitude;
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
				Diagnostics.Write("INTERACTION owner=P" + (playerSlot.Index + 1) + " fsm=" + e.Fsm.Name + " object=" + e.Fsm.GameObject.name);
			}
			if (!Inside(e, playerSlot) && playerSlot.Actions != null && (playerSlot.Actions.up.WasPressed || playerSlot.Actions.down.WasPressed))
			{
				Diagnostics.Write("INTERACTION native prompt P" + (playerSlot.Index + 1) + " fsm=" + e.Fsm.Name + " state=" + ((e.Fsm.ActiveState == null) ? "none" : e.Fsm.ActiveState.Name) + " anchor=" + PromptAnchor(e, playerSlot.Hero.transform.position).ToString());
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
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !trigger || !collider)
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
		if (owner.Alive && owner.Ready && (bool)owner.Hero && owner.Hero.controlReqlinquished && (bool)instance && instance.uiState == UIState.PLAYING && PickupCard.Owner == null && !ShopMenuRouting.MenuVisible && !ScriptedParty.Active && !CoopEnding.Active)
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
			return PvpArena.BlocksWorldInteraction(value.Scene, f);
		}
		return PvpArena.BlocksWorldInteraction(blocked: false, f);
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
		if (f == null || !f.GameObject)
		{
			return null;
		}
		foreach (KeyValuePair<Fsm, Entry> entry2 in entries)
		{
			Entry value = entry2.Value;
			if (value != entry && (bool)value.Fsm.GameObject && f.GameObject.transform.IsChildOf(value.Fsm.GameObject.transform))
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
					if (player.Actions != null && (player.Actions.up.WasPressed || player.Actions.down.WasPressed))
					{
						playerSlot = player;
						break;
					}
				}
			}
			if (playerSlot != null && Time.unscaledTime >= nextInputDiagnostic)
			{
				nextInputDiagnostic = Time.unscaledTime + 1f;
				Diagnostics.Write("INTERACTION denied fsm=" + f.Name + " object=" + f.GameObject.name + " state=" + ((f.ActiveState == null) ? "none" : f.ActiveState.Name) + " owner=" + ((p == null) ? "none" : ("P" + (p.Index + 1))) + " input=P" + (playerSlot.Index + 1) + " pos=" + playerSlot.Hero.transform.position.ToString() + " anchor=" + PromptAnchor(entry, playerSlot.Hero.transform.position).ToString() + " native=" + NativePromptReady(entry) + " triggers=" + entry.Triggers.Length + " busy=" + entry.Busy + " blocked=" + (p != null && p.InputBlocked));
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
		Fsm fsm = data?.SentByFsm;
		if (fsm == null)
		{
			fsm = WorldRouting.Current;
		}
		if (fsm == null || session.Resolve(fsm) != null)
		{
			return true;
		}
		if (Describe(fsm) == null)
		{
			return true;
		}
		PlayerSlot playerSlot2 = Resolve(fsm);
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
		if ((bool)GameManager.instance && GameManager.instance.sceneName == "Room_Final_Boss_Core" && s.Players.Count > 1)
		{
			int num = -1;
			for (int i = 0; i < s.Players.Count; i++)
			{
				PlayerSlot playerSlot = s.Players[i];
				if (playerSlot.Ready && playerSlot.Alive && (bool)playerSlot.Hero && playerSlot.Hero.transform.position.x > 22f)
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
						if (playerSlot.Ready && playerSlot.Alive && (bool)playerSlot.Hero && playerSlot.Hero.transform.position.x < 19f)
						{
							NativeDreamFx.TeleportTrail(playerSlot, playerSlot.Hero.transform.position, s.Players[num].Hero.transform.position);
							playerSlot.Hero.transform.position = s.Players[num].Hero.transform.position;
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
