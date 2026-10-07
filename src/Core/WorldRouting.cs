using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class WorldRouting
{
	private sealed class Contact
	{
		internal readonly Dictionary<Collider2D, PlayerSlot> Players = new Dictionary<Collider2D, PlayerSlot>();

		internal PlayerSlot Last;
	}

	internal struct Call
	{
		internal Fsm Previous;

		internal PlayerSlot Player;

		internal Vector3 Position;
	}

	private static readonly Dictionary<Fsm, Contact> contacts = new Dictionary<Fsm, Contact>();

	private static Collider2D[] bathColliders = new Collider2D[0];

	private static float nextBathScan;

	internal static Fsm Current;

	internal static PlayerSlot warp;

	internal static Vector3 warpAt;

	private static float warpTime;

	private static readonly Vector3[] previous = new Vector3[8];

	private static readonly bool[] sampled = new bool[8];

	internal static PlayerSlot Touch(Fsm f, Collider2D collider, bool exit)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !collider || !collider.GetComponentInParent<HeroController>())
		{
			return ShadeCloakRitual.Contact(null, f, exit);
		}
		PlayerSlot playerSlot = coopSession.Resolve(collider);
		if (playerSlot == null)
		{
			return ShadeCloakRitual.Contact(null, f, exit);
		}
		if (coopSession.Resolve(f) != null || !f.GameObject || (bool)f.GameObject.GetComponentInParent<HealthManager>() || (bool)f.GameObject.GetComponentInParent<DamageHero>())
		{
			return ShadeCloakRitual.Contact(playerSlot, f, exit);
		}
		if (!contacts.TryGetValue(f, out var value))
		{
			if (exit)
			{
				return ShadeCloakRitual.Contact(playerSlot, f, exit);
			}
			value = new Contact();
			contacts[f] = value;
		}
		if (exit)
		{
			value.Players.Remove(collider);
		}
		else
		{
			value.Players[collider] = playerSlot;
			value.Last = playerSlot;
		}
		return ShadeCloakRitual.Contact(playerSlot, f, exit);
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (!contacts.TryGetValue(f, out var value))
		{
			return null;
		}
		if (value.Last != null && value.Last.Alive && value.Last.Hero.controlReqlinquished)
		{
			return value.Last;
		}
		PlayerSlot playerSlot = null;
		foreach (KeyValuePair<Collider2D, PlayerSlot> player in value.Players)
		{
			PlayerSlot value2 = player.Value;
			if ((bool)player.Key && player.Key.enabled && value2.Alive && value2.Ready)
			{
				if (value2.Actions != null && value2.Actions.up.IsPressed)
				{
					value.Last = value2;
					return value2;
				}
				if (playerSlot == null || value2 == value.Last)
				{
					playerSlot = value2;
				}
			}
		}
		return playerSlot;
	}

	internal static List<PlayerSlot> BathPlayers()
	{
		CoopSession session = Plugin.Self.Session;
		if (Current == null || session.Resolve(Current) != null || !contacts.TryGetValue(Current, out var value))
		{
			List<PlayerSlot> list = new List<PlayerSlot>();
			foreach (KeyValuePair<Fsm, Contact> contact in contacts)
			{
				Fsm key = contact.Key;
				if (key == null || !key.GameObject)
				{
					continue;
				}
				string text = (key.Name + " " + key.GameObject.name).ToLowerInvariant();
				if (!text.Contains("spa") && !text.Contains("spring") && !text.Contains("bath"))
				{
					continue;
				}
				foreach (KeyValuePair<Collider2D, PlayerSlot> player in contact.Value.Players)
				{
					if ((bool)player.Key && player.Key.enabled && player.Value.Alive && player.Value.Ready && !list.Contains(player.Value))
					{
						list.Add(player.Value);
					}
				}
			}
			if (list.Count > 0)
			{
				return list;
			}
			if (Time.unscaledTime >= nextBathScan)
			{
				nextBathScan = Time.unscaledTime + 1f;
				List<Collider2D> list2 = new List<Collider2D>();
				Collider2D[] array = Object.FindObjectsOfType<Collider2D>();
				foreach (Collider2D collider2D in array)
				{
					if ((bool)collider2D && collider2D.enabled && collider2D.gameObject.activeInHierarchy)
					{
						string text2 = (collider2D.name + " " + (collider2D.transform.parent ? collider2D.transform.parent.name : "")).ToLowerInvariant();
						if (text2.Contains("spa") || text2.Contains("spring") || text2.Contains("bath"))
						{
							list2.Add(collider2D);
						}
					}
				}
				bathColliders = list2.ToArray();
			}
			foreach (PlayerSlot player2 in session.Players)
			{
				if (!player2.Alive || !player2.Ready)
				{
					continue;
				}
				Collider2D[] array = bathColliders;
				foreach (Collider2D collider2D2 in array)
				{
					if ((bool)collider2D2 && collider2D2.bounds.Contains(player2.Hero.transform.position))
					{
						if (!list.Contains(player2))
						{
							list.Add(player2);
						}
						break;
					}
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return list;
		}
		List<PlayerSlot> list3 = new List<PlayerSlot>();
		foreach (KeyValuePair<Collider2D, PlayerSlot> player3 in value.Players)
		{
			if ((bool)player3.Key && player3.Key.enabled && player3.Value.Alive && player3.Value.Ready && !list3.Contains(player3.Value))
			{
				list3.Add(player3.Value);
			}
		}
		if (list3.Count <= 0)
		{
			return null;
		}
		return list3;
	}

	internal static Call Begin(Fsm f)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		PlayerSlot playerSlot = (PlayerContext.TargetingEnemy ? null : (PlayerContext.Current ?? coopSession?.Primary));
		Call call = default(Call);
		call.Previous = Current;
		call.Player = playerSlot;
		call.Position = ((playerSlot != null && (bool)playerSlot.Hero) ? playerSlot.Hero.transform.position : Vector3.zero);
		Call result = call;
		Current = f;
		return result;
	}

	internal static void End(Fsm f, Call before)
	{
		Current = before.Previous;
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		PlayerSlot player = before.Player;
		GameManager instance = GameManager.instance;
		if (coopSession == null || !coopSession.Active || player == null || !player.Ready || !player.Alive || !instance || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition || !instance.HasFinishedEnteringScene || player.ArenaTransfer || ArenaGather.TransferActive || EmergencyWarp.Active(player) || CoopEnding.Active || ScriptedParty.Active || BenchSeats.Seated(player))
		{
			return;
		}
		Vector3 position = player.Hero.transform.position;
		if (!(position.y < -100f))
		{
			float num = Vector2.Distance(before.Position, position);
			bool flag = player.Hero.controlReqlinquished && num > 8f;
			if (!(num < 6f) && (!coopSession.InRoom(position) || flag) && (!InteractionRouter.IsInteraction(f) || flag))
			{
				NativeDreamFx.TeleportTrail(player, before.Position, position);
				warp = player;
				warpAt = position;
				warpTime = Time.unscaledTime;
			}
		}
	}

	internal static void ObserveFrame(CoopSession s)
	{
		GameManager instance = GameManager.instance;
		foreach (PlayerSlot player in s.Players)
		{
			int index = player.Index;
			if (index < 0 || index >= sampled.Length)
			{
				continue;
			}
			if (!player.Ready || !player.Alive || !player.Hero)
			{
				sampled[index] = false;
				continue;
			}
			Vector3 position = player.Hero.transform.position;
			if (sampled[index] && (bool)instance && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene && !ScriptedParty.Active && !ArenaGather.TransferActive && !EmergencyWarp.Active(player) && !BenchSeats.Seated(player) && !player.ArenaTransfer && !player.Hero.cState.transitioning && player.Hero.controlReqlinquished && position.y >= -100f && Vector2.Distance(previous[index], position) > 12f)
			{
				NativeDreamFx.TeleportTrail(player, previous[index], position);
				warp = player;
				warpAt = position;
				warpTime = Time.unscaledTime;
				string[] obj = new string[6]
				{
					"SCRIPT TELEPORT detected P",
					(index + 1).ToString(),
					" ",
					null,
					null,
					null
				};
				Vector3 vector = previous[index];
				obj[3] = vector.ToString();
				obj[4] = " -> ";
				vector = position;
				obj[5] = vector.ToString();
				Diagnostics.Write(string.Concat(obj));
				ScriptedParty.NativeMove(s, player, previous[index], position);
			}
			previous[index] = position;
			sampled[index] = true;
		}
	}

	internal static void Tick(CoopSession s)
	{
		if (warp == null)
		{
			return;
		}
		if (Time.unscaledTime - warpTime > 30f || !warp.Alive)
		{
			warp = null;
		}
		else
		{
			if (!s.Gameplay || ArenaGather.TransferActive || ScriptedParty.Active || (warp.Hero.controlReqlinquished && Time.unscaledTime - warpTime < 3f) || !s.FindSafePosition(warpAt, warp, out var result))
			{
				return;
			}
			PlayerSlot playerSlot = warp;
			warp = null;
			foreach (PlayerSlot player in s.Players)
			{
				if (player != playerSlot && player.Ready && player.Alive)
				{
					Vector3 position = player.Hero.transform.position;
					NativeDreamFx.TeleportTrail(player, position, result);
					using (PlayerContext.Enter(player))
					{
						ActorRecovery.Reset(player);
						player.Hero.transform.position = result;
						CoopSession.RestoreLivingVisuals(player);
					}
					player.SafePoint = result;
					player.HasSafePoint = true;
					player.Vitals.HazardPoint = result;
					player.ProtectionUntil = Time.time + 1f;
				}
			}
			string text = (playerSlot.Index + 1).ToString();
			Vector3 vector = result;
			Diagnostics.Write("SCRIPT TELEPORT group to P" + text + " at=" + vector.ToString());
		}
	}

	internal static void Reset()
	{
		contacts.Clear();
		Current = null;
		warp = null;
		for (int i = 0; i < sampled.Length; i++)
		{
			sampled[i] = false;
		}
		bathColliders = new Collider2D[0];
		nextBathScan = 0f;
	}
}
