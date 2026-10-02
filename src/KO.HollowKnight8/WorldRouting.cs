using System.Collections.Generic;
using HutongGames.PlayMaker;
using InControl;
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

	private static Collider2D[] bathColliders = (Collider2D[])(object)new Collider2D[0];

	private static float nextBathScan;

	internal static Fsm Current;

	internal static PlayerSlot warp;

	internal static Vector3 warpAt;

	private static float warpTime;

	private static readonly Vector3[] previous = (Vector3[])(object)new Vector3[8];

	private static readonly bool[] sampled = new bool[8];

	internal static PlayerSlot Touch(Fsm f, Collider2D collider, bool exit)
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !Object.op_Implicit((Object)(object)collider) || !Object.op_Implicit((Object)(object)((Component)collider).GetComponentInParent<HeroController>()))
		{
			return null;
		}
		PlayerSlot playerSlot = coopSession.Resolve(collider);
		if (playerSlot == null)
		{
			return null;
		}
		if (coopSession.Resolve(f) != null || !Object.op_Implicit((Object)(object)f.GameObject) || Object.op_Implicit((Object)(object)f.GameObject.GetComponentInParent<HealthManager>()) || Object.op_Implicit((Object)(object)f.GameObject.GetComponentInParent<DamageHero>()))
		{
			return playerSlot;
		}
		if (!contacts.TryGetValue(f, out var value))
		{
			if (exit)
			{
				return playerSlot;
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
		return playerSlot;
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
			if (Object.op_Implicit((Object)(object)player.Key) && ((Behaviour)player.Key).enabled && value2.Alive && value2.Ready)
			{
				if (value2.Actions != null && ((OneAxisInputControl)value2.Actions.up).IsPressed)
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
				if (key == null || !Object.op_Implicit((Object)(object)key.GameObject))
				{
					continue;
				}
				string text = (key.Name + " " + ((Object)key.GameObject).name).ToLowerInvariant();
				if (!text.Contains("spa") && !text.Contains("spring") && !text.Contains("bath"))
				{
					continue;
				}
				foreach (KeyValuePair<Collider2D, PlayerSlot> player in contact.Value.Players)
				{
					if (Object.op_Implicit((Object)(object)player.Key) && ((Behaviour)player.Key).enabled && player.Value.Alive && player.Value.Ready && !list.Contains(player.Value))
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
				foreach (Collider2D val in array)
				{
					if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).enabled && ((Component)val).gameObject.activeInHierarchy)
					{
						string text2 = (((Object)val).name + " " + (Object.op_Implicit((Object)(object)((Component)val).transform.parent) ? ((Object)((Component)val).transform.parent).name : "")).ToLowerInvariant();
						if (text2.Contains("spa") || text2.Contains("spring") || text2.Contains("bath"))
						{
							list2.Add(val);
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
				foreach (Collider2D val2 in array)
				{
					if (!Object.op_Implicit((Object)(object)val2))
					{
						continue;
					}
					Bounds bounds = val2.bounds;
					if (((Bounds)(ref bounds)).Contains(((Component)player2.Hero).transform.position))
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
			if (Object.op_Implicit((Object)(object)player3.Key) && ((Behaviour)player3.Key).enabled && player3.Value.Alive && player3.Value.Ready && !list3.Contains(player3.Value))
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
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		PlayerSlot playerSlot = (PlayerContext.TargetingEnemy ? null : (PlayerContext.Current ?? coopSession?.Primary));
		Call result = new Call
		{
			Previous = Current,
			Player = playerSlot,
			Position = ((playerSlot != null && Object.op_Implicit((Object)(object)playerSlot.Hero)) ? ((Component)playerSlot.Hero).transform.position : Vector3.zero)
		};
		Current = f;
		return result;
	}

	internal static void End(Fsm f, Call before)
	{
		Current = before.Previous;
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		PlayerSlot player = before.Player;
		GameManager instance = GameManager.instance;
		if (coopSession == null || !coopSession.Active || player == null || !player.Ready || !player.Alive || !Object.op_Implicit((Object)(object)instance) || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition || !instance.HasFinishedEnteringScene || player.ArenaTransfer || ArenaGather.TransferActive || EmergencyWarp.Active(player) || CoopEnding.Active || ScriptedParty.Active || BenchSeats.Seated(player))
		{
			return;
		}
		Vector3 position = ((Component)player.Hero).transform.position;
		if (!(position.y < -100f))
		{
			float num = Vector2.Distance(Vector2.op_Implicit(before.Position), Vector2.op_Implicit(position));
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

	internal unsafe static void ObserveFrame(CoopSession s)
	{
		GameManager instance = GameManager.instance;
		foreach (PlayerSlot player in s.Players)
		{
			int index = player.Index;
			if (index < 0 || index >= sampled.Length)
			{
				continue;
			}
			if (!player.Ready || !player.Alive || !Object.op_Implicit((Object)(object)player.Hero))
			{
				sampled[index] = false;
				continue;
			}
			Vector3 position = ((Component)player.Hero).transform.position;
			if (sampled[index] && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene && !ScriptedParty.Active && !ArenaGather.TransferActive && !EmergencyWarp.Active(player) && !BenchSeats.Seated(player) && !player.ArenaTransfer && !player.Hero.cState.transitioning && player.Hero.controlReqlinquished && position.y >= -100f && Vector2.Distance(Vector2.op_Implicit(previous[index]), Vector2.op_Implicit(position)) > 12f)
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
				Vector3 val = previous[index];
				obj[3] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
				obj[4] = " -> ";
				val = position;
				obj[5] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
				Diagnostics.Write(string.Concat(obj));
				ScriptedParty.NativeMove(s, player, previous[index], position);
			}
			previous[index] = position;
			sampled[index] = true;
		}
	}

	internal unsafe static void Tick(CoopSession s)
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
					Vector3 position = ((Component)player.Hero).transform.position;
					NativeDreamFx.TeleportTrail(player, position, result);
					using (PlayerContext.Enter(player))
					{
						ActorRecovery.Reset(player);
						((Component)player.Hero).transform.position = result;
						CoopSession.RestoreLivingVisuals(player);
					}
					player.SafePoint = result;
					player.HasSafePoint = true;
					player.Vitals.HazardPoint = result;
					player.ProtectionUntil = Time.time + 1f;
				}
			}
			string text = (playerSlot.Index + 1).ToString();
			Vector3 val = result;
			Diagnostics.Write("SCRIPT TELEPORT group to P" + text + " at=" + ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString());
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
		bathColliders = (Collider2D[])(object)new Collider2D[0];
		nextBathScan = 0f;
	}
}
