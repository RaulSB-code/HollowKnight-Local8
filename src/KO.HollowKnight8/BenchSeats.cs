using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using InControl;
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
		if (!Object.op_Implicit((Object)(object)go))
		{
			return null;
		}
		RestBench val = go.GetComponentInParent<RestBench>() ?? go.GetComponentInChildren<RestBench>(true);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return go;
		}
		return ((Component)val).gameObject;
	}

	internal static bool Full(GameObject go)
	{
		go = Root(go);
		if (!Object.op_Implicit((Object)(object)go) || !benches.TryGetValue(((Object)go).GetInstanceID(), out var value))
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
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || !p.Alive || !((PlayerContext.Current == p) ? Plugin.Self.Session.Data.atBench : p.Vitals.AtBench) || !InteractionRouter.IsBench(f) || Custom(p))
		{
			return;
		}
		GameObject val = Root(f.GameObject);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		if (!benches.TryGetValue(((Object)val).GetInstanceID(), out var value))
		{
			value = new Bench
			{
				Root = val,
				Fsm = f,
				Center = ((Component)p.Hero).transform.position,
				SettleUntil = Time.unscaledTime + 0.8f
			};
			benches[((Object)val).GetInstanceID()] = value;
		}
		if (seated.ContainsKey(p))
		{
			return;
		}
		int num = Free(value);
		if (num >= 0)
		{
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
			Diagnostics.Write("BENCH seat native P" + (p.Index + 1) + " bench=" + ((Object)val).name);
		}
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
		tk2dSpriteAnimator component = ((Component)p.Hero).GetComponent<tk2dSpriteAnimator>();
		if (!Object.op_Implicit((Object)(object)component))
		{
			return null;
		}
		if (component.CurrentClip != null && component.CurrentClip.name.IndexOf("sit", StringComparison.OrdinalIgnoreCase) >= 0 && (int)component.CurrentClip.wrapMode == 0)
		{
			return component.CurrentClip.name;
		}
		if (Object.op_Implicit((Object)(object)component.Library))
		{
			tk2dSpriteAnimationClip[] clips = component.Library.clips;
			foreach (tk2dSpriteAnimationClip val in clips)
			{
				if (val != null && val.name.IndexOf("sit", StringComparison.OrdinalIgnoreCase) >= 0 && (int)val.wrapMode == 0)
				{
					return val.name;
				}
			}
		}
		return null;
	}

	internal static void Tick(CoopSession s)
	{
		foreach (Bench value in benches.Values)
		{
			if (!Object.op_Implicit((Object)(object)value.Root))
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
				if (!Object.op_Implicit((Object)(object)player.Hero) || !player.Ready || !player.Alive)
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
						value.Center = ((Component)player.Hero).transform.position;
					}
					string text = Clip(player);
					if (text != null)
					{
						value.Clip = text;
					}
				}
				else if (player.Actions != null)
				{
					bool flag = ((OneAxisInputControl)player.Actions.up).IsPressed || ((OneAxisInputControl)player.Actions.down).IsPressed;
					if (!flag)
					{
						seat.Released = true;
					}
					if (!Charms.NativeMenuOpen && (((OneAxisInputControl)player.Actions.jump).WasPressed || Mathf.Abs(((TwoAxisInputControl)player.Actions.moveVector).X) > 0.4f || (seat.Released && flag)))
					{
						Leave(player);
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
				if (!player2.Alive || !player2.Ready || !player2.Connected || player2.InputBlocked || Seated(player2) || player2.Actions == null || EmergencyWarp.Active(player2) || player2.Hero.controlReqlinquished || Charms.NativeMenuOpen || !InteractionRouter.NearbyBench(value.Fsm, player2) || (!((OneAxisInputControl)player2.Actions.up).WasPressed && !((OneAxisInputControl)player2.Actions.down).WasPressed) || Mathf.Abs(((Component)player2.Hero).transform.position.y - value.Center.y) > 2f || !player2.Hero.cState.onGround)
				{
					continue;
				}
				using (PlayerContext.Enter(player2))
				{
					Revival.End(player2);
					ActorRecovery.Reset(player2);
					player2.Hero.RelinquishControl();
					player2.Hero.StopAnimationControl();
					((Component)player2.Hero).transform.position = value.Center + Vector3.right * (float)PartyRules.SeatOffset(num);
					tk2dSpriteAnimator component = ((Component)player2.Hero).GetComponent<tk2dSpriteAnimator>();
					if (Object.op_Implicit((Object)(object)component) && component.GetClipByName(value.Clip) != null)
					{
						component.Play(value.Clip);
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
				Diagnostics.Write("BENCH seat P" + (player2.Index + 1) + " slot=" + num + " bench=" + ((Object)value.Root).name);
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
				if (seat != null && !seat.Native && Object.op_Implicit((Object)(object)seat.Player.Hero))
				{
					PlayerSlot player = seat.Player;
					int seat2 = Array.IndexOf(value.Seats, seat);
					Vector3 position = value.Center + Vector3.right * (float)PartyRules.SeatOffset(seat2);
					position.z = (float)CoopRules.PlayerDepth(player.Index);
					((Component)player.Hero).transform.position = position;
					ActorRecovery.Freeze(player);
				}
			}
		}
	}

	internal static void Leave(PlayerSlot p)
	{
		if (p == null || !seated.TryGetValue(p, out var value))
		{
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
		if (flag && Object.op_Implicit((Object)(object)p.Hero))
		{
			using (PlayerContext.Enter(p))
			{
				Plugin.Self.Session.Data.atBench = false;
				ActorRecovery.Reset(p);
				CoopSession.RestoreLivingVisuals(p);
			}
		}
		p.Vitals.AtBench = false;
	}

	internal static void Reset()
	{
		foreach (PlayerSlot item in new List<PlayerSlot>(seated.Keys))
		{
			Leave(item);
		}
		benches.Clear();
	}
}
