using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
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
		ShadeCloakRitual.ObserveCall(f, p, method);
		if (f != null && (bool)f.GameObject && p != null && p.Alive && !collections.ContainsKey(f))
		{
			string text = (f.Name + " " + f.GameObject.name).ToLowerInvariant();
			if ((text.Contains("shiny") || text.Contains("pickup") || text.Contains("pick up")) && (!(method != "RelinquishControl") || !(method != "StopAnimationControl")))
			{
				collections[f] = new Collection
				{
					Player = p,
					Began = Time.unscaledTime
				};
				Diagnostics.Write("PICKUP transaction P" + (p.Index + 1) + " object=" + f.GameObject.name + " fsm=" + f.Name);
			}
		}
	}

	private static bool Locked(PlayerSlot p)
	{
		HeroAnimationController component = p.Hero.GetComponent<HeroAnimationController>();
		if (!p.Hero.controlReqlinquished && p.Hero.acceptingInput)
		{
			if ((bool)component)
			{
				return !component.controlEnabled;
			}
			return false;
		}
		return true;
	}

	private static void TickCollections(CoopSession s)
	{
		completed.Clear();
		foreach (KeyValuePair<Fsm, Collection> collection in collections)
		{
			Fsm key = collection.Key;
			Collection value = collection.Value;
			PlayerSlot player = value.Player;
			if (!player.Hero || !player.Ready || !player.Alive || player.Hazard)
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
			if (!s.Gameplay || !instance || instance.IsLoadingSceneTransition || !instance.HasFinishedEnteringScene || !instance2 || instance2.uiState != UIState.PLAYING || ShopMenuRouting.MenuVisible || ScriptedParty.Active || CoopEnding.Active || BenchSeats.Seated(player))
			{
				continue;
			}
			float num = Time.unscaledTime - value.Began;
			bool num2 = !key.GameObject || !key.GameObject.activeInHierarchy;
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
		if (owner == null || !owner.Hero || !card || !card.activeInHierarchy)
		{
			return false;
		}
		if ((bool)fallbackLabel)
		{
			TMP_Text tMP_Text = fallbackLabel as TMP_Text;
			if ((bool)tMP_Text)
			{
				if (tMP_Text.isActiveAndEnabled)
				{
					return ItemTitle(tMP_Text.text);
				}
				return false;
			}
			tk2dTextMesh tk2dTextMesh = fallbackLabel as tk2dTextMesh;
			if ((bool)tk2dTextMesh && tk2dTextMesh.isActiveAndEnabled)
			{
				return ItemTitle(tk2dTextMesh.text);
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
		Component component = null;
		TMP_Text[] array = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text tMP_Text in array)
		{
			if ((bool)tMP_Text && tMP_Text.isActiveAndEnabled && ItemTitle(tMP_Text.text))
			{
				component = tMP_Text;
				break;
			}
		}
		if (!component)
		{
			tk2dTextMesh[] array2 = UnityEngine.Object.FindObjectsOfType<tk2dTextMesh>();
			foreach (tk2dTextMesh tk2dTextMesh in array2)
			{
				if ((bool)tk2dTextMesh && tk2dTextMesh.isActiveAndEnabled && ItemTitle(tk2dTextMesh.text))
				{
					component = tk2dTextMesh;
					break;
				}
			}
		}
		if ((bool)component)
		{
			owner = pending;
			source = pendingSource;
			pending = null;
			pendingSource = null;
			fallbackLabel = component;
			card = component.gameObject;
			started = Time.unscaledTime;
			closedAt = -1f;
			nextDiagnostic = started + 10f;
			reported = false;
			Diagnostics.Write("PICKUP native text card P" + (owner.Index + 1) + " object=" + card.name);
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
			Diagnostics.Write("PICKUP charm acquired P" + (p.Index + 1) + " fsm=" + ((pickup == null) ? "none" : pickup.Name) + " object=" + ((pickup == null || !pickup.GameObject) ? "none" : pickup.GameObject.name) + " state=" + ((pickup == null || pickup.ActiveState == null) ? "none" : pickup.ActiveState.Name));
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
			Diagnostics.Write("PICKUP mask fragment acquired P" + (p.Index + 1) + " key=" + key + " fsm=" + ((pickup == null) ? "none" : pickup.Name) + " object=" + ((pickup == null || !pickup.GameObject) ? "none" : pickup.GameObject.name));
		}
	}

	internal static void Created(Fsm f, GameObject objectCreated)
	{
		ShadeCloakRitual.Card(f, objectCreated);
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !objectCreated)
		{
			return;
		}
		PlayerSlot playerSlot = PlayerContext.Current ?? ShopMenuRouting.Resolve(f) ?? InteractionRouter.Resolve(f) ?? WorldRouting.Resolve(f);
		if ((playerSlot == null || !playerSlot.Alive) && pending != null && Time.unscaledTime - pendingAt < 5f)
		{
			playerSlot = pending;
		}
		if ((playerSlot == null || !playerSlot.Alive) && f != null && (bool)f.GameObject)
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
		Diagnostics.Write("PICKUP card P" + (playerSlot.Index + 1) + " source=" + ((f == null) ? "none" : f.Name) + " object=" + objectCreated.name + " ui=" + (UIManager.instance ? UIManager.instance.uiState.ToString() : "missing"));
		int num = 0;
		PlayMakerFSM[] componentsInChildren = objectCreated.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
		foreach (PlayMakerFSM playMakerFSM in componentsInChildren)
		{
			if (num++ < 8)
			{
				Diagnostics.Write("PICKUP controller " + playMakerFSM.FsmName + " object=" + playMakerFSM.gameObject.name + " state=" + ((playMakerFSM.Fsm.ActiveState == null) ? "none" : playMakerFSM.Fsm.ActiveState.Name));
				continue;
			}
			break;
		}
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (f != null && collections.TryGetValue(f, out var value) && value.Player.Alive)
		{
			return ShadeCloakRitual.Route(value.Player, f);
		}
		if (f != null && f == pendingSource && pending != null)
		{
			return ShadeCloakRitual.Route(pending, f);
		}
		if (f != null && f == source && owner != null)
		{
			return ShadeCloakRitual.Route(owner, f);
		}
		if (owner == null && pendingMask && pending != null && f != null && (bool)f.GameObject && Time.unscaledTime - pendingAt < 20f)
		{
			string text = (f.Name + " " + f.GameObject.name).ToLowerInvariant();
			if ((text.Contains("item") || text.Contains("mask") || text.Contains("fragment") || text.Contains("heart piece")) && (InteractionRouter.IsUi(f) || text.Contains("get item") || text.Contains("mask fragment")))
			{
				return ShadeCloakRitual.Route(pending, f);
			}
		}
		if (!Visible() || f == null || !f.GameObject)
		{
			return ShadeCloakRitual.Route(null, f);
		}
		if (f == source)
		{
			return ShadeCloakRitual.Route(owner, f);
		}
		Transform transform = f.GameObject.transform;
		if (transform == card.transform || transform.IsChildOf(card.transform))
		{
			return ShadeCloakRitual.Route(owner, f);
		}
		if (InteractionRouter.IsUi(f))
		{
			string text2 = (f.Name + " " + f.GameObject.name).ToLowerInvariant();
			if ((text2.Contains("item") || text2.Contains("charm")) && (text2.Contains("get") || text2.Contains("msg") || text2.Contains("pickup") || text2.Contains("collect")))
			{
				return ShadeCloakRitual.Route(owner, f);
			}
		}
		return ShadeCloakRitual.Route(null, f);
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
		foreach (KeyValuePair<Renderer, bool> hiddenRenderer in hiddenRenderers)
		{
			if ((bool)hiddenRenderer.Key)
			{
				hiddenRenderer.Key.forceRenderingOff = true;
			}
		}
		foreach (KeyValuePair<TMP_Text, bool> hiddenText in hiddenTexts)
		{
			if ((bool)hiddenText.Key)
			{
				hiddenText.Key.enabled = false;
			}
		}
		ProbePending();
		TickCollections(s);
		if (owner == null && pending != null && !pendingMask && Time.unscaledTime - pendingAt > 7f)
		{
			PlayerSlot playerSlot = pending;
			UIManager instance = UIManager.instance;
			if ((bool)playerSlot.Hero && playerSlot.Ready && playerSlot.Alive && Locked(playerSlot) && s.Gameplay && (bool)instance && instance.uiState == UIState.PLAYING && !ShopMenuRouting.MenuVisible)
			{
				using (PlayerContext.Enter(playerSlot))
				{
					ActorRecovery.Reset(playerSlot);
					CoopSession.RestoreLivingVisuals(playerSlot);
				}
				playerSlot.ProtectionUntil = Time.time + 1f;
				Diagnostics.Write("PICKUP world animation wake P" + (playerSlot.Index + 1) + " after missing card");
			}
			if (!playerSlot.Hero || !playerSlot.Alive || !Locked(playerSlot))
			{
				pending = null;
				pendingSource = null;
			}
		}
		if (owner == null && pendingMask && pending != null && Time.unscaledTime - pendingAt >= 20f)
		{
			PlayerSlot playerSlot2 = pending;
			Diagnostics.Write("PICKUP mask fragment timeout P" + (playerSlot2.Index + 1) + " control=" + ((bool)playerSlot2.Hero && playerSlot2.Hero.controlReqlinquished) + " shop=" + ShopMenuRouting.MenuVisible);
			if ((bool)playerSlot2.Hero && playerSlot2.Ready && playerSlot2.Alive)
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
		if (playerSlot3.Hero == null || !playerSlot3.Ready || playerSlot3.Down || playerSlot3.Hazard)
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
				Diagnostics.Write("PICKUP waiting P" + (playerSlot3.Index + 1) + " ui=" + (instance2 ? instance2.uiState.ToString() : "missing") + " game=" + GameManager.instance.gameState.ToString() + " control=" + playerSlot3.Hero.controlReqlinquished + " source=" + ((source == null || source.ActiveState == null) ? "none" : source.ActiveState.Name) + " blocked=" + playerSlot3.InputBlocked + " submit=" + (playerSlot3.Actions != null && playerSlot3.Actions.menuSubmit.WasPressed) + " jump=" + (playerSlot3.Actions != null && playerSlot3.Actions.jump.WasPressed) + " cancel=" + (playerSlot3.Actions != null && playerSlot3.Actions.menuCancel.WasPressed));
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
		if (Time.unscaledTime - closedAt < 0.5f || !s.Gameplay || !instance3 || instance3.uiState != UIState.PLAYING)
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
		if (!fallbackLabel)
		{
			if (!card)
			{
				return null;
			}
			return card.transform;
		}
		Transform result;
		Transform transform = (result = fallbackLabel.transform);
		GameCameras instance = GameCameras.instance;
		UIManager instance2 = UIManager.instance;
		Transform parent = transform.parent;
		while ((bool)parent && parent != instance?.hudCanvas?.transform && parent != instance2?.UICanvas?.transform)
		{
			string text = parent.name.ToLowerInvariant();
			if (text.Contains("item") || text.Contains("charm") || text.Contains("amuleto") || text.Contains("popup") || text.Contains("message") || text.Contains("msg"))
			{
				result = parent;
				break;
			}
			if (parent.GetComponentsInChildren<Renderer>(includeInactive: true).Length > 70 || parent.GetComponentsInChildren<TMP_Text>(includeInactive: true).Length > 30)
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
		if (!root)
		{
			return;
		}
		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((bool)renderer)
			{
				if (!hiddenRenderers.ContainsKey(renderer))
				{
					hiddenRenderers[renderer] = renderer.forceRenderingOff;
				}
				renderer.forceRenderingOff = true;
			}
		}
		TMP_Text[] componentsInChildren2 = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		foreach (TMP_Text tMP_Text in componentsInChildren2)
		{
			if ((bool)tMP_Text)
			{
				if (!hiddenTexts.ContainsKey(tMP_Text))
				{
					hiddenTexts[tMP_Text] = tMP_Text.enabled;
				}
				tMP_Text.enabled = false;
			}
		}
	}

	private static void RestoreVisuals()
	{
		foreach (KeyValuePair<Renderer, bool> hiddenRenderer in hiddenRenderers)
		{
			if ((bool)hiddenRenderer.Key)
			{
				hiddenRenderer.Key.forceRenderingOff = hiddenRenderer.Value;
			}
		}
		foreach (KeyValuePair<TMP_Text, bool> hiddenText in hiddenTexts)
		{
			if ((bool)hiddenText.Key)
			{
				hiddenText.Key.enabled = hiddenText.Value;
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
		foreach (FsmStateAction fsmStateAction in actions)
		{
			if (fsmStateAction == null || !fsmStateAction.Enabled)
			{
				continue;
			}
			if (fsmStateAction is ListenForMenuActions listenForMenuActions && listenForMenuActions.submitPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(listenForMenuActions.eventTarget, listenForMenuActions.submitPressed);
				}
				return true;
			}
			if (fsmStateAction is ListenForMenuSubmit listenForMenuSubmit && listenForMenuSubmit.wasPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(listenForMenuSubmit.wasPressed);
				}
				return true;
			}
			if (fsmStateAction is ListenForJump listenForJump && listenForJump.wasPressed != null)
			{
				using (PlayerContext.Enter(p))
				{
					f.Event(listenForJump.wasPressed);
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
			Transform transform = VisualRoot();
			Diagnostics.Write("PICKUP emergency P" + (p.Index + 1) + " after 20s root=" + (transform ? transform.name : "none") + " source=" + ((source == null || source.ActiveState == null) ? "none" : source.ActiveState.Name));
			try
			{
				bool flag = NativeDismiss(source, p);
				if (!flag && (bool)transform)
				{
					PlayMakerFSM[] componentsInChildren = transform.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
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
			HideVisuals(transform);
			if ((bool)p.Hero && p.Alive)
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
