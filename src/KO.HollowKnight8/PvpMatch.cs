using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PvpMatch
{
	internal sealed class Before
	{
		internal PlayerSlot Player;

		internal Vector3 Position;

		internal int Health;

		internal int Blue;

		internal int Soul;

		internal int Reserve;

		internal Vector3 Offset;

		internal Vector3 Size;
	}

	private static readonly List<Before> roster = new List<Before>();

	private static readonly int[] sides = new int[8];

	private static readonly bool[] alive = new bool[8];

	private static int state;

	private static int round;

	private static int bestOf;

	private static float remaining;

	private static bool requested;

	private static readonly Dictionary<int, int> seriesWins = new Dictionary<int, int>();

	internal static string Result = "";

	internal static string StartIssue = "";

	internal static bool Running => state != 0;

	internal static bool CanFight
	{
		get
		{
			if (Local8Mod.Settings.PvpMode == 2)
			{
				return state == 2;
			}
			return true;
		}
	}

	internal static bool BlocksInput
	{
		get
		{
			if (state != 1 && state != 3)
			{
				return state == 4;
			}
			return true;
		}
	}

	internal static bool Protects => BlocksInput;

	internal static string Announcement
	{
		get
		{
			if (state != 1)
			{
				if (state != 3 && state != 4)
				{
					return "";
				}
				return Result;
			}
			return Mathf.Max(1, Mathf.CeilToInt(remaining)).ToString();
		}
	}

	internal static string Banner
	{
		get
		{
			if (Local8Mod.Settings.PvpMode == 0)
			{
				return "";
			}
			if (Local8Mod.Settings.PvpMode == 1)
			{
				return "FUEGO AMIGO";
			}
			if (state == 0)
			{
				return "DUELO: " + ActionKeys.PvpLabel + " / F8 > PvP";
			}
			if (state == 4)
			{
				return Result;
			}
			if (state == 1)
			{
				return "RONDA " + round + "  -  " + Mathf.Max(1, Mathf.CeilToInt(remaining));
			}
			if (state == 3)
			{
				return Result + "  |  Siguiente ronda en " + Mathf.Max(1, Mathf.CeilToInt(remaining));
			}
			return "RONDA " + round + "  |  " + Mathf.FloorToInt(remaining / 60f) + ":" + Mathf.FloorToInt(remaining % 60f).ToString("00");
		}
	}

	internal static void SetMode(int mode)
	{
		if (mode != Local8Mod.Settings.PvpMode)
		{
			Stop(restorePosition: true);
			Local8Mod.Settings.PvpMode = mode;
			PvpCombat.Invalidate();
			Plugin.Self.Notice(mode switch
			{
				1 => "Fuego amigo activado. F8 > PvP para dano y equipos.", 
				0 => "PvP desactivado.", 
				_ => "Modo duelo. Colocaos en suelo seguro y pulsa Iniciar rondas en F8 > PvP.", 
			});
		}
	}

	internal static void RequestStart()
	{
		if (!requested)
		{
			CoopSession session = PvpCombat.Session;
			if (session == null || session.Players.Count < 2)
			{
				StartIssue = "Se necesitan al menos dos jugadores.";
				Plugin.Self.Notice(StartIssue);
			}
			else
			{
				StartIssue = "";
				requested = true;
				Plugin.Self.SetPanel(open: false);
			}
		}
	}

	internal static void Toggle()
	{
		if (Running || requested)
		{
			Stop(restorePosition: true);
			return;
		}
		SetMode(2);
		RequestStart();
	}

	internal static bool BlockExit(PlayerSlot p)
	{
		if (ShopMenuRouting.Buyer == null)
		{
			if (Doorways.scanAt - Time.unscaledTime > 2.05f)
			{
				return true;
			}
			if (Running)
			{
				if (p != null && !p.Down && p.Ready)
				{
					CoopSession session = PvpCombat.Session;
					if (session != null)
					{
						Revival.End(p);
						session.Down(p, spawnShade: false);
						PvpCombat.Down(p);
						return true;
					}
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static bool Reject(PlayerSlot p, string reason)
	{
		StartIssue = "P" + (p.Index + 1) + ": " + reason + ".";
		Plugin.Self.Notice("Duelo: " + StartIssue);
		Plugin.Self.SetPanel(open: true);
		if (Object.op_Implicit((Object)(object)p.Hero))
		{
			Diagnostics.Write("PVP START REJECT P" + (p.Index + 1) + " reason=" + reason + " grounded=" + p.Hero.cState.onGround + " bench=" + p.Vitals.AtBench + " nearBench=" + p.Hero.cState.nearBench + " control=" + p.Hero.controlReqlinquished + " body=" + ((object)DuelGround.Body(p)/*cast due to .constrained prefix*/).ToString());
		}
		return false;
	}

	private static bool Begin(CoopSession s)
	{
		if (CoopEnding.Active)
		{
			requested = false;
			return false;
		}
		EmergencyWarp.CancelAll();
		requested = false;
		roster.Clear();
		int num = 0;
		foreach (PlayerSlot player in s.Players)
		{
			if (!player.Connected)
			{
				return Reject(player, "mando desconectado");
			}
			if (!player.Ready || !Object.op_Implicit((Object)(object)player.Hero))
			{
				return Reject(player, "jugador todavia cargando");
			}
			if (!player.Alive)
			{
				return Reject(player, "debe estar vivo");
			}
			if (player.Vitals.AtBench && !player.Hero.controlReqlinquished)
			{
				player.Vitals.AtBench = false;
				s.Commit(player);
			}
			if (player.Hero.controlReqlinquished)
			{
				return Reject(player, player.Vitals.AtBench ? "sal del banco" : "termina la animacion o conversacion");
			}
			Bounds val = DuelGround.Body(player);
			if (!DuelGround.Safe(player, ((Component)player.Hero).transform.position, ((Bounds)(ref val)).center - ((Component)player.Hero).transform.position, ((Bounds)(ref val)).size, out var reason))
			{
				return Reject(player, reason);
			}
			sides[num] = PvpRules.Side(player.Index, PvpCombat.Team(player));
			alive[num++] = true;
		}
		if (num < 2 || PvpRules.Outcome(sides, alive, num) != int.MinValue)
		{
			StartIssue = "Se necesitan dos bandos distintos. Libre = todos contra todos.";
			Plugin.Self.Notice(StartIssue);
			Plugin.Self.SetPanel(open: true);
			return false;
		}
		foreach (PlayerSlot player2 in s.Players)
		{
			Bounds val2 = DuelGround.Body(player2);
			roster.Add(new Before
			{
				Player = player2,
				Position = ((Component)player2.Hero).transform.position,
				Offset = ((Bounds)(ref val2)).center - ((Component)player2.Hero).transform.position,
				Size = ((Bounds)(ref val2)).size,
				Health = player2.Vitals.Health,
				Blue = player2.Vitals.Blue,
				Soul = player2.Vitals.Soul,
				Reserve = player2.Vitals.Reserve
			});
		}
		round = 0;
		bestOf = PvpRules.SeriesLength(Local8Mod.Settings.DuelBestOf);
		seriesWins.Clear();
		CoopShades.Reset();
		NextRound(s);
		return true;
	}

	private static void NextRound(CoopSession s)
	{
		foreach (Before item in roster)
		{
			if (!Object.op_Implicit((Object)(object)item.Player.Hero) || !DuelGround.Safe(item.Player, item.Position, item.Offset, item.Size, out var _))
			{
				Stop(restorePosition: false);
				Plugin.Self.Notice("Duelo detenido: el punto inicial ya no es seguro. Colocaos de nuevo e inicia otra vez.");
				return;
			}
		}
		state = 1;
		remaining = 3f;
		round++;
		Result = "";
		PvpCombat.Invalidate();
		foreach (Before item2 in roster)
		{
			PlayerSlot player = item2.Player;
			Revival.End(player);
			s.Recover(player, respawn: true, item2.Position);
			player.Vitals.Health = player.CurrentMaxHealth;
			player.Vitals.Blue = Math.Max(item2.Blue, player.Vitals.Joni);
			player.Vitals.Soul = Math.Min(player.Vitals.MaxSoul, Local8Mod.Settings.DuelSoul);
			player.Vitals.Reserve = 0;
			player.Vitals.Invincible = false;
			player.Vitals.DisablePause = false;
			s.Commit(player);
			player.ProtectionUntil = Time.time + 3f;
		}
		Diagnostics.Write("PVP ROUND " + round + " players=" + roster.Count);
	}

	internal static bool Tick(CoopSession s)
	{
		if (Local8Mod.Settings.PvpMode != 2)
		{
			if (Running)
			{
				Stop(restorePosition: true);
			}
			return false;
		}
		if (requested && !Running)
		{
			Begin(s);
		}
		if (!Running)
		{
			return false;
		}
		bool flag = roster.Count != s.Players.Count;
		bool flag2 = false;
		for (int i = 0; i < roster.Count; i++)
		{
			if (flag)
			{
				break;
			}
			PlayerSlot player = roster[i].Player;
			if (!s.Players.Contains(player) || !player.Connected || !player.Ready || !Object.op_Implicit((Object)(object)player.Hero) || PvpRules.Side(player.Index, PvpCombat.Team(player)) != sides[i])
			{
				flag = true;
			}
		}
		if (flag)
		{
			Stop(restorePosition: true);
			Plugin.Self.Notice("Duelo detenido: cambio de jugadores, equipos o dispositivo. Puedes volver a iniciarlo en F8.");
			return true;
		}
		foreach (Before item in roster)
		{
			if (item.Player.Vitals.AtBench)
			{
				flag2 = true;
			}
		}
		if (flag2)
		{
			Stop(restorePosition: false);
			Plugin.Self.Notice("Duelo detenido al descansar en un banco.");
			return true;
		}
		remaining -= Time.deltaTime;
		if (state == 1)
		{
			foreach (Before item2 in roster)
			{
				item2.Player.ProtectionUntil = Time.time + 0.1f;
			}
			if (remaining <= 0f)
			{
				state = 2;
				remaining = Local8Mod.Settings.DuelSeconds;
				PvpCombat.Invalidate();
				foreach (Before item3 in roster)
				{
					item3.Player.ProtectionUntil = 0f;
					item3.Player.Hero.cState.invulnerable = false;
				}
				Plugin.Self.Notice("Ronda " + round + ": a luchar.");
			}
		}
		else if (state == 2)
		{
			for (int j = 0; j < roster.Count; j++)
			{
				PlayerSlot player2 = roster[j].Player;
				alive[j] = !player2.Down && player2.Vitals.Health + player2.Vitals.Blue > 0;
			}
			int num = PvpRules.Outcome(sides, alive, roster.Count);
			if (num != int.MinValue || remaining <= 0f)
			{
				if (num == int.MinValue)
				{
					num = 0;
				}
				Result = ((num == 0) ? "EMPATE" : ((num > 0) ? ("GANA EQUIPO " + num) : ("GANA P" + -num)));
				if (num != 0)
				{
					for (int k = 0; k < roster.Count; k++)
					{
						if (sides[k] == num)
						{
							PvpCombat.Wins[roster[k].Player.Index]++;
						}
					}
				}
				int value = 0;
				if (num != 0)
				{
					seriesWins.TryGetValue(num, out value);
					value = (seriesWins[num] = value + 1);
				}
				bool flag3 = num != 0 && PvpRules.SeriesComplete(bestOf, value);
				if (flag3)
				{
					Result = ((num > 0) ? ("EQUIPO " + num) : ("P" + -num)) + " GANA EL DUELO";
				}
				else if (bestOf > 0 && num != 0)
				{
					Result = Result + "  (" + value + "/" + PvpRules.WinsNeeded(bestOf) + ")";
				}
				state = (flag3 ? 4 : 3);
				remaining = (flag3 ? 5 : 4);
				PvpCombat.Invalidate();
				Plugin.Self.Notice(Result);
				Diagnostics.Write("PVP RESULT round=" + round + " winner=" + num + " wins=" + value + " bestOf=" + bestOf + " complete=" + flag3);
			}
		}
		else if (state == 3 && remaining <= 0f)
		{
			NextRound(s);
		}
		else if (state == 4 && remaining <= 0f)
		{
			Stop(restorePosition: true);
		}
		return true;
	}

	internal static Before Saved(PlayerSlot p)
	{
		foreach (Before item in roster)
		{
			if (item.Player == p)
			{
				return item;
			}
		}
		return null;
	}

	internal static void Stop(bool restorePosition)
	{
		requested = false;
		state = 0;
		remaining = 0f;
		Result = "";
		CoopSession session = PvpCombat.Session;
		foreach (Before item in roster)
		{
			PlayerSlot player = item.Player;
			if (session != null && session.Players.Contains(player))
			{
				if (Object.op_Implicit((Object)(object)player.Hero) && (player.Down || player.Hazard || restorePosition))
				{
					string reason;
					Vector3 value = ((restorePosition && DuelGround.Safe(player, item.Position, item.Offset, item.Size, out reason)) ? item.Position : ((Component)player.Hero).transform.position);
					session.Recover(player, respawn: true, value);
				}
				player.Down = false;
				player.Hazard = false;
				player.Vitals.Health = Math.Max(1, item.Health);
				player.Vitals.Blue = item.Blue;
				player.Vitals.Soul = item.Soul;
				player.Vitals.Reserve = item.Reserve;
				session.Commit(player);
			}
		}
		roster.Clear();
		seriesWins.Clear();
		PvpCombat.Invalidate();
	}

	internal static void SceneChanged()
	{
		Stop(restorePosition: false);
		PvpCombat.Reset(scores: false);
	}
}
