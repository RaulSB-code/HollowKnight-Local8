using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using TMPro;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PickupCard
{
	private sealed class Collection
	{
		internal PlayerSlot Player;

		internal float Began;

		internal float Finished = -1f;
	}

	private static PlayerSlot owner;

	private static PlayerSlot pending;

	private static GameObject card;

	private static Fsm source;

	private static Fsm pendingSource;

	private static Component fallbackLabel;

	private static float started;

	private static float pendingAt;

	private static float closedAt = -1f;

	private static float nextDiagnostic;

	private static float nextProbe;

	private static bool reported;

	private static bool pendingMask;

	private static readonly Dictionary<Renderer, bool> hiddenRenderers = new Dictionary<Renderer, bool>();

	private static readonly Dictionary<TMP_Text, bool> hiddenTexts = new Dictionary<TMP_Text, bool>();

	private static readonly Dictionary<Fsm, Collection> collections = new Dictionary<Fsm, Collection>();

	private static readonly List<Fsm> completed = new List<Fsm>();

	internal static PlayerSlot Owner
	{
		get
		{
			if (!Visible())
			{
				return null;
			}
			return owner;
		}
	}

	internal static void ObserveCall(Fsm f, PlayerSlot p, string method)
	{
		if (f != null && Object.op_Implicit((Object)(object)f.GameObject) && p != null && p.Alive && !collections.ContainsKey(f))
		{
			string text = (f.Name + " " + ((Object)f.GameObject).name).ToLowerInvariant();
			if ((text.Contains("shiny") || text.Contains("pickup") || text.Contains("pick up")) && (!(method != "RelinquishControl") || !(method != "StopAnimationControl")))
			{
				collections[f] = new Collection
				{
					Player = p,
					Began = Time.unscaledTime
				};
				Diagnostics.Write("PICKUP transaction P" + (p.Index + 1) + " object=" + ((Object)f.GameObject).name + " fsm=" + f.Name);
			}
		}
	}

	private static bool Locked(PlayerSlot p)
	{
		HeroAnimationController component = ((Component)p.Hero).GetComponent<HeroAnimationController>();
		if (!p.Hero.controlReqlinquished && p.Hero.acceptingInput)
		{
			if (Object.op_Implicit((Object)(object)component))
			{
				return !component.controlEnabled;
			}
			return false;
		}
		return true;
	}

	private static void TickCollections(CoopSession s)
	{
		//IL_00d7: Invalid comparison between Unknown and I4
		completed.Clear();
		foreach (KeyValuePair<Fsm, Collection> collection in collections)
		{
			Fsm key = collection.Key;
			Collection value = collection.Value;
			PlayerSlot player = value.Player;
			if (!Object.op_Implicit((Object)(object)player.Hero) || !player.Ready || !player.Alive || player.Hazard)
			{
				completed.Add(key);
				continue;
			}
			if (owner == player && Visible())
			{
				continue;
			}
			GameManager instance = GameManager.instance;
			UIManager instance2 = UIManager.instance;
			if (!s.Gameplay || !Object.op_Implicit((Object)(object)instance) || instance.IsLoadingSceneTransition || !instance.HasFinishedEnteringScene || !Object.op_Implicit((Object)(object)instance2) || (int)instance2.uiState != 4 || ShopMenuRouting.MenuVisible || ScriptedParty.Active || CoopEnding.Active || BenchSeats.Seated(player))
			{
				continue;
			}
			float num = Time.unscaledTime - value.Began;
			bool num2 = !Object.op_Implicit((Object)(object)key.GameObject) || !key.GameObject.activeInHierarchy;
			string text = ((key.ActiveState == null) ? "" : key.ActiveState.Name.ToLowerInvariant());
			if (!num2)
			{
				switch (text)
				{
				case "finish":
				case "finished":
				case "idle":
					goto IL_0190;
				}
				if (!(text == "collected"))
				{
					value.Finished = -1f;
					goto IL_01b5;
				}
			}
			goto IL_0190;
			IL_0190:
			if (value.Finished < 0f)
			{
				value.Finished = Time.unscaledTime;
			}
			goto IL_01b5;
			IL_01b5:
			if (num < 1f)
			{
				continue;
			}
			if (!Locked(player))
			{
				completed.Add(key);
				continue;
			}
			bool flag = text.Contains("get up") || text.Contains("stand up");
			if (!(num < (flag ? 3.2f : 20f)) || (!(value.Finished < 0f) && !(Time.unscaledTime - value.Finished < 0.35f)))
			{
				using (PlayerContext.Enter(player))
				{
					ActorRecovery.Reset(player);
					CoopSession.RestoreLivingVisuals(player);
				}
				Diagnostics.Write("PICKUP transaction wake P" + (player.Index + 1) + " state=" + text + " timeout=" + (num >= 20f));
				completed.Add(key);
			}
		}
		foreach (Fsm item in completed)
		{
			collections.Remove(item);
		}
	}

	private static bool Visible()
	{
		if (owner == null || !Object.op_Implicit((Object)(object)owner.Hero) || !Object.op_Implicit((Object)(object)card) || !card.activeInHierarchy)
		{
			return false;
		}
		if (Object.op_Implicit((Object)(object)fallbackLabel))
		{
			Component obj = fallbackLabel;
			TMP_Text val = (TMP_Text)(object)((obj is TMP_Text) ? obj : null);
			if (Object.op_Implicit((Object)(object)val))
			{
				if (((Behaviour)val).isActiveAndEnabled)
				{
					return ItemTitle(val.text);
				}
				return false;
			}
			Component obj2 = fallbackLabel;
			tk2dTextMesh val2 = (tk2dTextMesh)(object)((obj2 is tk2dTextMesh) ? obj2 : null);
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).isActiveAndEnabled)
			{
				return ItemTitle(val2.text);
			}
			return false;
		}
		return true;
	}

	private static bool ItemTitle(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		value = value.ToLowerInvariant();
		if ((!value.Contains("amuleto") && !value.Contains("charm")) || (!value.Contains("obtenido") && !value.Contains("obtained") && !value.Contains("acquired")))
		{
			if (pendingMask && (value.Contains("mascara") || value.Contains("máscara") || value.Contains("fragmento") || value.Contains("mask shard") || value.Contains("heart piece")))
			{
				if (!value.Contains("obtenido") && !value.Contains("obtained"))
				{
					return value.Contains("acquired");
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static void ProbePending()
	{
		if (owner != null || pending == null || Time.unscaledTime - pendingAt > (pendingMask ? 20f : 8f) || Time.unscaledTime < nextProbe)
		{
			return;
		}
		nextProbe = Time.unscaledTime + 0.12f;
		Component val = null;
		TMP_Text[] array = Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text val2 in array)
		{
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).isActiveAndEnabled && ItemTitle(val2.text))
			{
				val = (Component)(object)val2;
				break;
			}
		}
		if (!Object.op_Implicit((Object)(object)val))
		{
			tk2dTextMesh[] array2 = Object.FindObjectsOfType<tk2dTextMesh>();
			foreach (tk2dTextMesh val3 in array2)
			{
				if (Object.op_Implicit((Object)(object)val3) && ((Behaviour)val3).isActiveAndEnabled && ItemTitle(val3.text))
				{
					val = (Component)(object)val3;
					break;
				}
			}
		}
		if (Object.op_Implicit((Object)(object)val))
		{
			owner = pending;
			source = pendingSource;
			pending = null;
			pendingSource = null;
			fallbackLabel = val;
			card = val.gameObject;
			started = Time.unscaledTime;
			closedAt = -1f;
			nextDiagnostic = started + 10f;
			reported = false;
			Diagnostics.Write("PICKUP native text card P" + (owner.Index + 1) + " object=" + ((Object)card).name);
		}
	}

	internal static void CharmAcquired(PlayerSlot p, Fsm pickup)
	{
		if (p != null && p.Index != 0 && p.Alive)
		{
			RestoreVisuals();
			pending = p;
			pendingSource = pickup;
			pendingAt = Time.unscaledTime;
			pendingMask = false;
			Diagnostics.Write("PICKUP charm acquired P" + (p.Index + 1) + " fsm=" + ((pickup == null) ? "none" : pickup.Name) + " object=" + ((pickup == null || !Object.op_Implicit((Object)(object)pickup.GameObject)) ? "none" : ((Object)pickup.GameObject).name) + " state=" + ((pickup == null || pickup.ActiveState == null) ? "none" : pickup.ActiveState.Name));
		}
	}

	internal static void MaskAcquired(PlayerSlot p, Fsm pickup, string key)
	{
		if (p != null && p.Index != 0 && p.Alive && (pending != p || !pendingMask || !(Time.unscaledTime - pendingAt < 3f)))
		{
			RestoreVisuals();
			pending = p;
			pendingSource = pickup;
			pendingAt = Time.unscaledTime;
			pendingMask = true;
			Diagnostics.Write("PICKUP mask fragment acquired P" + (p.Index + 1) + " key=" + key + " fsm=" + ((pickup == null) ? "none" : pickup.Name) + " object=" + ((pickup == null || !Object.op_Implicit((Object)(object)pickup.GameObject)) ? "none" : ((Object)pickup.GameObject).name));
		}
	}

	internal static void Created(Fsm f, GameObject objectCreated)
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !Object.op_Implicit((Object)(object)objectCreated))
		{
			return;
		}
		PlayerSlot playerSlot = PlayerContext.Current ?? ShopMenuRouting.Resolve(f) ?? InteractionRouter.Resolve(f) ?? WorldRouting.Resolve(f);
		if ((playerSlot == null || !playerSlot.Alive) && pending != null && Time.unscaledTime - pendingAt < 5f)
		{
			playerSlot = pending;
		}
		if ((playerSlot == null || !playerSlot.Alive) && f != null && Object.op_Implicit((Object)(object)f.GameObject))
		{
			playerSlot = coopSession.Nearest(f.GameObject.transform.position);
		}
		if (playerSlot == null || playerSlot.Index == 0 || !playerSlot.Alive)
		{
			return;
		}
		owner = playerSlot;
		card = objectCreated;
		source = f;
		fallbackLabel = null;
		started = Time.unscaledTime;
		closedAt = -1f;
		nextDiagnostic = started + 10f;
		reported = false;
		pending = null;
		pendingSource = null;
		Diagnostics.Write("PICKUP card P" + (playerSlot.Index + 1) + " source=" + ((f == null) ? "none" : f.Name) + " object=" + ((Object)objectCreated).name + " ui=" + (Object.op_Implicit((Object)(object)UIManager.instance) ? ((object)Unsafe.As<UIState, UIState>(ref UIManager.instance.uiState)/*cast due to .constrained prefix*/).ToString() : "missing"));
		int num = 0;
		PlayMakerFSM[] componentsInChildren = objectCreated.GetComponentsInChildren<PlayMakerFSM>(true);
		foreach (PlayMakerFSM val in componentsInChildren)
		{
			if (num++ < 8)
			{
				Diagnostics.Write("PICKUP controller " + val.FsmName + " object=" + ((Object)((Component)val).gameObject).name + " state=" + ((val.Fsm.ActiveState == null) ? "none" : val.Fsm.ActiveState.Name));
				continue;
			}
			break;
		}
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (f != null && collections.TryGetValue(f, out var value) && value.Player.Alive)
		{
			return value.Player;
		}
		if (f != null && f == pendingSource && pending != null)
		{
			return pending;
		}
		if (f != null && f == source && owner != null)
		{
			return owner;
		}
		if (owner == null && pendingMask && pending != null && f != null && Object.op_Implicit((Object)(object)f.GameObject) && Time.unscaledTime - pendingAt < 20f)
		{
			string text = (f.Name + " " + ((Object)f.GameObject).name).ToLowerInvariant();
			if ((text.Contains("item") || text.Contains("mask") || text.Contains("fragment") || text.Contains("heart piece")) && (InteractionRouter.IsUi(f) || text.Contains("get item") || text.Contains("mask fragment")))
			{
				return pending;
			}
		}
		if (!Visible() || f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return null;
		}
		if (f == source)
		{
			return owner;
		}
		Transform transform = f.GameObject.transform;
		if ((Object)(object)transform == (Object)(object)card.transform || transform.IsChildOf(card.transform))
		{
			return owner;
		}
		if (InteractionRouter.IsUi(f))
		{
			string text2 = (f.Name + " " + ((Object)f.GameObject).name).ToLowerInvariant();
			if ((text2.Contains("item") || text2.Contains("charm")) && (text2.Contains("get") || text2.Contains("msg") || text2.Contains("pickup") || text2.Contains("collect")))
			{
				return owner;
			}
		}
		return null;
	}

	internal static PlayerSlot InputOwner(Fsm f)
	{
		PlayerSlot playerSlot = Resolve(f);
		if (playerSlot == null)
		{
			if (Owner == null || !InteractionRouter.IsUi(f))
			{
				return null;
			}
			playerSlot = owner;
		}
		return playerSlot;
	}

	internal static void Tick(CoopSession s)
	{
		//IL_012e: Invalid comparison between Unknown and I4
		//IL_05b4: Invalid comparison between Unknown and I4
		foreach (KeyValuePair<Renderer, bool> hiddenRenderer in hiddenRenderers)
		{
			if (Object.op_Implicit((Object)(object)hiddenRenderer.Key))
			{
				hiddenRenderer.Key.forceRenderingOff = true;
			}
		}
		foreach (KeyValuePair<TMP_Text, bool> hiddenText in hiddenTexts)
		{
			if (Object.op_Implicit((Object)(object)hiddenText.Key))
			{
				((Behaviour)hiddenText.Key).enabled = false;
			}
		}
		ProbePending();
		TickCollections(s);
		if (owner == null && pending != null && !pendingMask && Time.unscaledTime - pendingAt > 7f)
		{
			PlayerSlot playerSlot = pending;
			UIManager instance = UIManager.instance;
			if (Object.op_Implicit((Object)(object)playerSlot.Hero) && playerSlot.Ready && playerSlot.Alive && Locked(playerSlot) && s.Gameplay && Object.op_Implicit((Object)(object)instance) && (int)instance.uiState == 4 && !ShopMenuRouting.MenuVisible)
			{
				using (PlayerContext.Enter(playerSlot))
				{
					ActorRecovery.Reset(playerSlot);
					CoopSession.RestoreLivingVisuals(playerSlot);
				}
				playerSlot.ProtectionUntil = Time.time + 1f;
				Diagnostics.Write("PICKUP world animation wake P" + (playerSlot.Index + 1) + " after missing card");
			}
			if (!Object.op_Implicit((Object)(object)playerSlot.Hero) || !playerSlot.Alive || !Locked(playerSlot))
			{
				pending = null;
				pendingSource = null;
			}
		}
		if (owner == null && pendingMask && pending != null && Time.unscaledTime - pendingAt >= 20f)
		{
			PlayerSlot playerSlot2 = pending;
			Diagnostics.Write("PICKUP mask fragment timeout P" + (playerSlot2.Index + 1) + " control=" + (Object.op_Implicit((Object)(object)playerSlot2.Hero) && playerSlot2.Hero.controlReqlinquished) + " shop=" + ShopMenuRouting.MenuVisible);
			if (Object.op_Implicit((Object)(object)playerSlot2.Hero) && playerSlot2.Ready && playerSlot2.Alive)
			{
				using (PlayerContext.Enter(playerSlot2))
				{
					ActorRecovery.Reset(playerSlot2);
					CoopSession.RestoreLivingVisuals(playerSlot2);
				}
				playerSlot2.ProtectionUntil = Time.time + 1f;
			}
			pending = null;
			pendingSource = null;
			pendingMask = false;
		}
		if (owner == null)
		{
			return;
		}
		PlayerSlot playerSlot3 = owner;
		if ((Object)(object)playerSlot3.Hero == (Object)null || !playerSlot3.Ready || playerSlot3.Down || playerSlot3.Hazard)
		{
			Reset(all: false);
			return;
		}
		if (Visible())
		{
			if (Time.unscaledTime - started >= 20f)
			{
				Emergency(playerSlot3);
				return;
			}
			if (!reported)
			{
				reported = true;
				Diagnostics.Write("PICKUP card active P" + (playerSlot3.Index + 1) + " control=" + playerSlot3.Hero.controlReqlinquished + " input=" + playerSlot3.Hero.acceptingInput);
			}
			if (Time.unscaledTime >= nextDiagnostic)
			{
				nextDiagnostic = Time.unscaledTime + 10f;
				UIManager instance2 = UIManager.instance;
				Diagnostics.Write("PICKUP waiting P" + (playerSlot3.Index + 1) + " ui=" + (Object.op_Implicit((Object)(object)instance2) ? ((object)Unsafe.As<UIState, UIState>(ref instance2.uiState)/*cast due to .constrained prefix*/).ToString() : "missing") + " game=" + ((object)Unsafe.As<GameState, GameState>(ref GameManager.instance.gameState)/*cast due to .constrained prefix*/).ToString() + " control=" + playerSlot3.Hero.controlReqlinquished + " source=" + ((source == null || source.ActiveState == null) ? "none" : source.ActiveState.Name) + " blocked=" + playerSlot3.InputBlocked + " submit=" + (playerSlot3.Actions != null && ((OneAxisInputControl)playerSlot3.Actions.menuSubmit).WasPressed) + " jump=" + (playerSlot3.Actions != null && ((OneAxisInputControl)playerSlot3.Actions.jump).WasPressed) + " cancel=" + (playerSlot3.Actions != null && ((OneAxisInputControl)playerSlot3.Actions.menuCancel).WasPressed));
			}
			closedAt = -1f;
			return;
		}
		if (closedAt < 0f)
		{
			closedAt = Time.unscaledTime;
			Diagnostics.Write("PICKUP card closed P" + (playerSlot3.Index + 1));
		}
		UIManager instance3 = UIManager.instance;
		if (Time.unscaledTime - closedAt < 0.5f || !s.Gameplay || !Object.op_Implicit((Object)(object)instance3) || (int)instance3.uiState != 4)
		{
			return;
		}
		if (Locked(playerSlot3))
		{
			using (PlayerContext.Enter(playerSlot3))
			{
				ActorRecovery.Reset(playerSlot3);
				CoopSession.RestoreLivingVisuals(playerSlot3);
			}
			Diagnostics.Write("PICKUP wake P" + (playerSlot3.Index + 1));
		}
		Reset(all: false);
	}

	private static Transform VisualRoot()
	{
		if (!Object.op_Implicit((Object)(object)fallbackLabel))
		{
			if (!Object.op_Implicit((Object)(object)card))
			{
				return null;
			}
			return card.transform;
		}
		Transform result;
		Transform obj = (result = fallbackLabel.transform);
		GameCameras instance = GameCameras.instance;
		UIManager instance2 = UIManager.instance;
		Transform parent = obj.parent;
		while (Object.op_Implicit((Object)(object)parent))
		{
			Transform obj2 = parent;
			object obj3;
			if (instance == null)
			{
				obj3 = null;
			}
			else
			{
				GameObject hudCanvas = instance.hudCanvas;
				obj3 = ((hudCanvas != null) ? hudCanvas.transform : null);
			}
			if (!((Object)(object)obj2 != (Object)obj3))
			{
				break;
			}
			Transform obj4 = parent;
			object obj5;
			if (instance2 == null)
			{
				obj5 = null;
			}
			else
			{
				Canvas uICanvas = instance2.UICanvas;
				obj5 = ((uICanvas != null) ? ((Component)uICanvas).transform : null);
			}
			if (!((Object)(object)obj4 != (Object)obj5))
			{
				break;
			}
			string text = ((Object)parent).name.ToLowerInvariant();
			if (text.Contains("item") || text.Contains("charm") || text.Contains("amuleto") || text.Contains("popup") || text.Contains("message") || text.Contains("msg"))
			{
				result = parent;
				break;
			}
			if (((Component)parent).GetComponentsInChildren<Renderer>(true).Length > 70 || ((Component)parent).GetComponentsInChildren<TMP_Text>(true).Length > 30)
			{
				break;
			}
			result = parent;
			parent = parent.parent;
		}
		return result;
	}

	private static void HideVisuals(Transform root)
	{
		if (!Object.op_Implicit((Object)(object)root))
		{
			return;
		}
		Renderer[] componentsInChildren = ((Component)root).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				if (!hiddenRenderers.ContainsKey(val))
				{
					hiddenRenderers[val] = val.forceRenderingOff;
				}
				val.forceRenderingOff = true;
			}
		}
		TMP_Text[] componentsInChildren2 = ((Component)root).GetComponentsInChildren<TMP_Text>(true);
		foreach (TMP_Text val2 in componentsInChildren2)
		{
			if (Object.op_Implicit((Object)(object)val2))
			{
				if (!hiddenTexts.ContainsKey(val2))
				{
					hiddenTexts[val2] = ((Behaviour)val2).enabled;
				}
				((Behaviour)val2).enabled = false;
			}
		}
	}

	private static void RestoreVisuals()
	{
		foreach (KeyValuePair<Renderer, bool> hiddenRenderer in hiddenRenderers)
		{
			if (Object.op_Implicit((Object)(object)hiddenRenderer.Key))
			{
				hiddenRenderer.Key.forceRenderingOff = hiddenRenderer.Value;
			}
		}
		foreach (KeyValuePair<TMP_Text, bool> hiddenText in hiddenTexts)
		{
			if (Object.op_Implicit((Object)(object)hiddenText.Key))
			{
				((Behaviour)hiddenText.Key).enabled = hiddenText.Value;
			}
		}
		hiddenRenderers.Clear();
		hiddenTexts.Clear();
	}

	private static bool NativeDismiss(Fsm f, PlayerSlot p)
	{
		if (f == null || f.ActiveState == null || f.ActiveState.Actions == null)
		{
			return false;
		}
		FsmStateAction[] actions = f.ActiveState.Actions;
		foreach (FsmStateAction val in actions)
		{
			if (val == null || !val.Enabled)
			{
				continue;
			}
			ListenForMenuActions val2 = (ListenForMenuActions)(object)((val is ListenForMenuActions) ? val : null);
			if (val2 != null && val2.submitPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(val2.eventTarget, val2.submitPressed);
				}
				return true;
			}
			ListenForMenuSubmit val3 = (ListenForMenuSubmit)(object)((val is ListenForMenuSubmit) ? val : null);
			if (val3 != null && val3.wasPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(val3.wasPressed);
				}
				return true;
			}
			ListenForJump val4 = (ListenForJump)(object)((val is ListenForJump) ? val : null);
			if (val4 != null && val4.wasPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(val4.wasPressed);
				}
				return true;
			}
		}
		return false;
	}

	private static void Emergency(PlayerSlot p)
	{
		try
		{
			Transform val = VisualRoot();
			Diagnostics.Write("PICKUP emergency P" + (p.Index + 1) + " after 20s root=" + (Object.op_Implicit((Object)(object)val) ? ((Object)val).name : "none") + " source=" + ((source == null || source.ActiveState == null) ? "none" : source.ActiveState.Name));
			try
			{
				bool flag = NativeDismiss(source, p);
				if (!flag && Object.op_Implicit((Object)(object)val))
				{
					PlayMakerFSM[] componentsInChildren = ((Component)val).GetComponentsInChildren<PlayMakerFSM>(true);
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						if (NativeDismiss(componentsInChildren[i].Fsm, p))
						{
							flag = true;
							break;
						}
					}
				}
				Diagnostics.Write("PICKUP emergency native-dismiss=" + flag);
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("PICKUP native dismissal", ex);
			}
			HideVisuals(val);
			if (Object.op_Implicit((Object)(object)p.Hero) && p.Alive)
			{
				using (PlayerContext.Enter(p))
				{
					ActorRecovery.Reset(p);
					CoopSession.RestoreLivingVisuals(p);
				}
				p.ProtectionUntil = Time.time + 1f;
			}
		}
		catch (Exception ex2)
		{
			Diagnostics.Throttled("PICKUP emergency", ex2);
		}
		finally
		{
			owner = (pending = null);
			card = null;
			fallbackLabel = null;
			source = (pendingSource = null);
			started = (pendingAt = (nextDiagnostic = (nextProbe = 0f)));
			closedAt = -1f;
			reported = (pendingMask = false);
		}
	}

	internal static void Reset(bool all = true)
	{
		if (all)
		{
			collections.Clear();
			completed.Clear();
		}
		RestoreVisuals();
		owner = (pending = null);
		card = null;
		fallbackLabel = null;
		source = (pendingSource = null);
		started = (pendingAt = (nextDiagnostic = (nextProbe = 0f)));
		closedAt = -1f;
		reported = (pendingMask = false);
	}
}
