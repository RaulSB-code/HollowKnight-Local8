using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class BenchSeats
{
	private sealed class Seat
	{
		internal PlayerSlot Player;

		internal bool Native;

		internal bool Released;
	}

	private sealed class Bench
	{
		internal GameObject Root;

		internal Fsm Fsm;

		internal Vector3 Center;

		internal float SettleUntil;

		internal string Clip;

		internal readonly Seat[] Seats = new Seat[3];
	}

	private static readonly Dictionary<int, Bench> benches = new Dictionary<int, Bench>();

	private static readonly Dictionary<PlayerSlot, Bench> seated = new Dictionary<PlayerSlot, Bench>();

	private static GameObject Root(GameObject go)
	{
		if (!go)
		{
			return null;
		}
		RestBench restBench = go.GetComponentInParent<RestBench>() ?? go.GetComponentInChildren<RestBench>(includeInactive: true);
		if (!restBench)
		{
			return go;
		}
		return restBench.gameObject;
	}

	internal static bool Full(GameObject go)
	{
		go = Root(go);
		if (!go || !benches.TryGetValue(go.GetInstanceID(), out var value))
		{
			return false;
		}
		Seat[] seats = value.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			if (seats[i] == null)
			{
				return false;
			}
		}
		return true;
	}

	internal static bool Seated(PlayerSlot p)
	{
		if (p != null)
		{
			return seated.ContainsKey(p);
		}
		return false;
	}

	internal static bool Custom(PlayerSlot p)
	{
		if (p == null || !seated.TryGetValue(p, out var value))
		{
			return false;
		}
		Seat[] seats = value.Seats;
		foreach (Seat seat in seats)
		{
			if (seat != null && seat.Player == p)
			{
				return !seat.Native;
			}
		}
		return false;
	}

	internal static void Observe(Fsm f, PlayerSlot p)
	{
		if (p == null || !p.Hero || !p.Alive || !((PlayerContext.Current == p) ? Plugin.Self.Session.Data.atBench : p.Vitals.AtBench) || !InteractionRouter.IsBench(f) || Custom(p))
		{
			BenchPoseRecovery.Observe(f, p);
			return;
		}
		GameObject gameObject = Root(f.GameObject);
		if (!gameObject)
		{
			BenchPoseRecovery.Observe(f, p);
			return;
		}
		if (!benches.TryGetValue(gameObject.GetInstanceID(), out var value))
		{
			value = new Bench
			{
				Root = gameObject,
				Fsm = f,
				Center = p.Hero.transform.position,
				SettleUntil = Time.unscaledTime + 0.8f
			};
			benches[gameObject.GetInstanceID()] = value;
		}
		if (seated.ContainsKey(p))
		{
			BenchPoseRecovery.Observe(f, p);
			return;
		}
		int num = Free(value);
		if (num < 0)
		{
			BenchPoseRecovery.Observe(f, p);
			return;
		}
		if (value.Seats[0] != null && !value.Seats[0].Native)
		{
			value.Seats[num] = value.Seats[0];
			value.Seats[0] = null;
			num = 0;
		}
		value.Seats[num] = new Seat
		{
			Player = p,
			Native = true
		};
		seated[p] = value;
		Diagnostics.Write("BENCH seat native P" + (p.Index + 1) + " bench=" + gameObject.name);
		BenchPoseRecovery.Observe(f, p);
	}

	private static int Free(Bench b)
	{
		return PartyRules.FreeSeat(new bool[3]
		{
			b.Seats[0] != null,
			b.Seats[1] != null,
			b.Seats[2] != null
		});
	}

	private static string Clip(PlayerSlot p)
	{
		tk2dSpriteAnimator component = p.Hero.GetComponent<tk2dSpriteAnimator>();
		if (!component)
		{
			return null;
		}
		if (component.CurrentClip != null && component.CurrentClip.name.IndexOf("sit", StringComparison.OrdinalIgnoreCase) >= 0 && component.CurrentClip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop)
		{
			return component.CurrentClip.name;
		}
		if ((bool)component.Library)
		{
			tk2dSpriteAnimationClip[] clips = component.Library.clips;
			foreach (tk2dSpriteAnimationClip tk2dSpriteAnimationClip in clips)
			{
				if (tk2dSpriteAnimationClip != null && tk2dSpriteAnimationClip.name.IndexOf("sit", StringComparison.OrdinalIgnoreCase) >= 0 && tk2dSpriteAnimationClip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Loop)
				{
					return tk2dSpriteAnimationClip.name;
				}
			}
		}
		return null;
	}

	internal static void Tick(CoopSession s)
	{
		foreach (Bench value in benches.Values)
		{
			if (!value.Root)
			{
				continue;
			}
			Seat[] seats = value.Seats;
			foreach (Seat seat in seats)
			{
				if (seat == null)
				{
					continue;
				}
				PlayerSlot player = seat.Player;
				if (!player.Hero || !player.Ready || !player.Alive)
				{
					Leave(player);
				}
				else if (seat.Native)
				{
					if (!player.Vitals.AtBench)
					{
						Leave(player);
						continue;
					}
					if (Time.unscaledTime < value.SettleUntil)
					{
						value.Center = player.Hero.transform.position;
					}
					string text = Clip(player);
					if (text != null)
					{
						value.Clip = text;
					}
				}
				else if (player.Actions != null)
				{
					bool flag = player.Actions.up.IsPressed || player.Actions.down.IsPressed;
					if (!flag)
					{
						seat.Released = true;
					}
					if (!Charms.NativeMenuOpen && (player.Actions.jump.WasPressed || Mathf.Abs(player.Actions.moveVector.X) > 0.4f || (seat.Released && flag)))
					{
						InteractionMotion.BenchLeave(player);
					}
				}
			}
			if (Time.unscaledTime < value.SettleUntil || string.IsNullOrEmpty(value.Clip))
			{
				continue;
			}
			foreach (PlayerSlot player2 in s.Players)
			{
				int num = Free(value);
				if (num < 0)
				{
					break;
				}
				if (!player2.Alive || !player2.Ready || !player2.Connected || player2.InputBlocked || Seated(player2) || player2.Actions == null || EmergencyWarp.Active(player2) || player2.Hero.controlReqlinquished || Charms.NativeMenuOpen || !InteractionRouter.NearbyBench(value.Fsm, player2) || (!player2.Actions.up.WasPressed && !player2.Actions.down.WasPressed) || Mathf.Abs(player2.Hero.transform.position.y - value.Center.y) > 2f || !player2.Hero.cState.onGround)
				{
					continue;
				}
				using (PlayerContext.Enter(player2))
				{
					Revival.End(player2);
					ActorRecovery.Reset(player2);
					player2.Hero.RelinquishControl();
					player2.Hero.StopAnimationControl();
					InteractionMotion.BenchPlace(player2.Hero.transform, value.Center + Vector3.right * (float)PartyRules.SeatOffset(num));
					tk2dSpriteAnimator component = player2.Hero.GetComponent<tk2dSpriteAnimator>();
					if ((bool)component && component.GetClipByName(value.Clip) != null)
					{
						InteractionMotion.BenchPlay(component, value.Clip);
					}
					s.Data.atBench = true;
				}
				value.Seats[num] = new Seat
				{
					Player = player2,
					Native = false
				};
				seated[player2] = value;
				ActorRecovery.Freeze(player2);
				s.BenchRest(player2.Hero);
				Diagnostics.Write("BENCH seat P" + (player2.Index + 1) + " slot=" + num + " bench=" + value.Root.name);
			}
		}
	}

	internal static void VisualTick()
	{
		foreach (Bench value in benches.Values)
		{
			Seat[] seats = value.Seats;
			foreach (Seat seat in seats)
			{
				if (seat != null && !seat.Native && (bool)seat.Player.Hero)
				{
					PlayerSlot player = seat.Player;
					int seat2 = Array.IndexOf(value.Seats, seat);
					Vector3 at = value.Center + Vector3.right * (float)PartyRules.SeatOffset(seat2);
					at.z = (float)CoopRules.PlayerDepth(player.Index);
					InteractionMotion.BenchPosition(player.Hero.transform, at);
					ActorRecovery.Freeze(player);
				}
			}
		}
	}

	internal static void Leave(PlayerSlot p)
	{
		if (p == null || !seated.TryGetValue(p, out var value))
		{
			BenchPoseRecovery.Released(p);
			BenchSave.Leave(p);
			InteractionMotion.BenchReleased(p);
			return;
		}
		seated.Remove(p);
		bool flag = false;
		for (int i = 0; i < 3; i++)
		{
			if (value.Seats[i] != null && value.Seats[i].Player == p)
			{
				flag = !value.Seats[i].Native;
				value.Seats[i] = null;
			}
		}
		if (flag && (bool)p.Hero)
		{
			using (PlayerContext.Enter(p))
			{
				Plugin.Self.Session.Data.atBench = false;
				ActorRecovery.Reset(p);
				CoopSession.RestoreLivingVisuals(p);
			}
		}
		p.Vitals.AtBench = false;
		BenchPoseRecovery.Released(p);
		BenchSave.Leave(p);
		InteractionMotion.BenchReleased(p);
	}

	internal static void Reset()
	{
		foreach (PlayerSlot item in new List<PlayerSlot>(seated.Keys))
		{
			Leave(item);
		}
		benches.Clear();
		BenchPoseRecovery.Reset();
	}
}
