using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class ChallengeSequence
{
	private sealed class Guest
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal tk2dSpriteAnimator Animator;

		internal float EndsAt;

		internal bool Played;
	}

	private sealed class Watch
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal float CheckAt;

		internal float ExpireAt;
	}

	private static readonly List<Guest> guests = new List<Guest>();

	private static readonly List<Watch> watches = new List<Watch>();

	private static PlayMakerFSM native;

	private static Fsm raw;

	private static PlayerSlot owner;

	private static string scene;

	private static float started;

	private static float ownerDeadline;

	private static float nextAt;

	private static float recoveryGraceUntil;

	private static int cursor;

	private static bool active;

	private static bool ownerFinished;

	private static bool bypass;

	internal static bool Active => active;

	internal static bool BlocksRecovery
	{
		get
		{
			if (!active)
			{
				return Time.unscaledTime < recoveryGraceUntil;
			}
			return true;
		}
	}

	internal static bool Holds(PlayerSlot p)
	{
		if (!active || p == null)
		{
			return false;
		}
		foreach (Guest guest in guests)
		{
			if (guest.Player == p && Object.op_Implicit((Object)(object)guest.Hero) && (Object)(object)guest.Hero == (Object)(object)p.Hero)
			{
				return true;
			}
		}
		return false;
	}

	internal static void ObserveState(FsmState state, PlayerSlot resolved)
	{
		if (active || state == null || state.Fsm == null || state.Fsm.Name != "Challenge Start" || state.Name != "Challenge")
		{
			return;
		}
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || !coopSession.Gameplay || coopSession.Players.Count < 2)
		{
			return;
		}
		PlayerSlot p = resolved ?? InteractionRouter.ActivePlayer ?? coopSession.Primary;
		if (p == null || !p.Alive || !p.Ready || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return;
		}
		List<PlayerSlot> list = (from x in coopSession.Players
			where x != p && x.Alive && x.Ready && x.Connected && Object.op_Implicit((Object)(object)x.Hero)
			orderby x.Index
			select x).ToList();
		if (list.Count == 0)
		{
			return;
		}
		PlayMakerFSM val = null;
		if (Object.op_Implicit((Object)(object)state.Fsm.GameObject))
		{
			PlayMakerFSM[] components = state.Fsm.GameObject.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM val2 in components)
			{
				if (Object.op_Implicit((Object)(object)val2) && val2.Fsm == state.Fsm)
				{
					val = val2;
					break;
				}
			}
		}
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		active = true;
		native = val;
		raw = state.Fsm;
		owner = p;
		Scene activeScene = SceneManager.GetActiveScene();
		scene = ((Scene)(ref activeScene)).name;
		recoveryGraceUntil = Time.unscaledTime + 20f;
		started = Time.unscaledTime;
		cursor = -1;
		ownerFinished = false;
		bypass = false;
		tk2dSpriteAnimator component = ((Component)p.Hero).GetComponent<tk2dSpriteAnimator>();
		ownerDeadline = started + Duration(component, "Challenge Start") + 0.65f;
		guests.Clear();
		foreach (PlayerSlot item in list)
		{
			Guest guest = new Guest
			{
				Player = item,
				Hero = item.Hero,
				Animator = ((Component)item.Hero).GetComponent<tk2dSpriteAnimator>()
			};
			guests.Add(guest);
			Freeze(guest);
		}
		Diagnostics.Write("CHALLENGE party start owner=P" + (p.Index + 1) + " guests=" + guests.Count + " scene=" + scene);
	}

	internal static bool Intercept(Fsm source, FsmEvent evt)
	{
		if (!active || bypass || source == null || source != raw || evt == null || evt.Name != "FINISHED")
		{
			return false;
		}
		FsmState activeState = source.ActiveState;
		if (activeState == null || activeState.Name != "Challenge")
		{
			return false;
		}
		if (!ownerFinished)
		{
			ownerFinished = true;
			nextAt = Time.unscaledTime + 0.05f;
			Diagnostics.Write("CHALLENGE native owner animation complete P" + ((owner == null) ? "?" : (owner.Index + 1).ToString()));
		}
		return true;
	}

	internal static void Tick(CoopSession s)
	{
		TickWatchdogs(s);
		if (!active)
		{
			return;
		}
		if (s != null && s.Active && raw != null && Object.op_Implicit((Object)(object)native))
		{
			string text = scene;
			Scene activeScene = SceneManager.GetActiveScene();
			if (!(text != ((Scene)(ref activeScene)).name))
			{
				if (owner == null || !Object.op_Implicit((Object)(object)owner.Hero) || owner.Down || owner.Hazard)
				{
					Abort(s, "owner unavailable", restore: true);
					return;
				}
				float unscaledTime = Time.unscaledTime;
				if (!ownerFinished && unscaledTime < ownerDeadline)
				{
					return;
				}
				if (!ownerFinished)
				{
					ownerFinished = true;
					nextAt = unscaledTime + 0.05f;
					Diagnostics.Write("CHALLENGE owner watchdog advanced P" + (owner.Index + 1));
				}
				if (unscaledTime < nextAt)
				{
					return;
				}
				if (cursor < 0)
				{
					cursor = 0;
				}
				if (cursor < guests.Count)
				{
					Guest guest = guests[cursor];
					if (!guest.Played)
					{
						Play(guest);
						return;
					}
					if (unscaledTime < guest.EndsAt)
					{
						return;
					}
					cursor++;
					nextAt = unscaledTime + 0.12f;
					if (cursor < guests.Count)
					{
						return;
					}
				}
				Finish(s);
				return;
			}
		}
		Abort(s, "scene/runtime changed", restore: false);
	}

	private static void Play(Guest g)
	{
		g.Played = true;
		if (Object.op_Implicit((Object)(object)g.Hero) && (Object)(object)g.Hero == (Object)(object)g.Player.Hero)
		{
			try
			{
				float num = ((raw != null && Object.op_Implicit((Object)(object)raw.GameObject)) ? raw.GameObject.transform.position.x : ((Component)g.Hero).transform.position.x);
				if (((Component)g.Hero).transform.position.x <= num)
				{
					g.Hero.FaceRight();
				}
				else
				{
					g.Hero.FaceLeft();
				}
			}
			catch
			{
			}
			if (Object.op_Implicit((Object)(object)g.Animator) && g.Animator.GetClipByName("Challenge Start") != null)
			{
				g.Animator.Play("Challenge Start");
			}
			Object.op_Implicit((Object)(object)((Component)g.Hero).GetComponent<AudioSource>());
		}
		float num2 = Duration(g.Animator, "Challenge Start");
		g.EndsAt = Time.unscaledTime + num2 + 0.1f;
		nextAt = g.EndsAt;
		Diagnostics.Write("CHALLENGE animation P" + (g.Player.Index + 1) + " duration=" + num2.ToString("0.00"));
	}

	private static float Duration(tk2dSpriteAnimator animator, string clipName)
	{
		try
		{
			if (Object.op_Implicit((Object)(object)animator))
			{
				tk2dSpriteAnimationClip clipByName = animator.GetClipByName(clipName);
				if (clipByName != null && clipByName.frames != null && clipByName.frames.Length != 0 && clipByName.fps > 0f)
				{
					return Mathf.Clamp((float)clipByName.frames.Length / clipByName.fps, 0.35f, 2.5f);
				}
			}
		}
		catch
		{
		}
		return 0.95f;
	}

	private static void Freeze(Guest g)
	{
		if (g == null || g.Player == null || !Object.op_Implicit((Object)(object)g.Hero))
		{
			return;
		}
		try
		{
			using (PlayerContext.Enter(g.Player))
			{
				Revival.End(g.Player);
				g.Hero.RelinquishControl();
				g.Hero.StopAnimationControl();
				ActorRecovery.Freeze(g.Player);
			}
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("CHALLENGE freeze P" + (g.Player.Index + 1), ex);
		}
	}

	private static void RestoreGuest(Guest g, bool watchdog)
	{
		if (g == null || g.Player == null || !Object.op_Implicit((Object)(object)g.Hero) || (Object)(object)g.Hero != (Object)(object)g.Player.Hero || g.Player.Down || g.Player.Hazard)
		{
			return;
		}
		try
		{
			using (PlayerContext.Enter(g.Player))
			{
				ActorRecovery.Reset(g.Player);
				CoopSession.RestoreLivingVisuals(g.Player);
			}
			g.Player.ProtectionUntil = Time.time + 1f;
			if (watchdog)
			{
				WatchPlayer(g.Player, 1.25f, 8f);
			}
			Diagnostics.Write("CHALLENGE control restored P" + (g.Player.Index + 1));
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("CHALLENGE restore P" + (g.Player.Index + 1), ex);
		}
	}

	private static void Finish(CoopSession s)
	{
		PlayMakerFSM val = native;
		PlayerSlot p = owner;
		foreach (Guest guest in guests)
		{
			RestoreGuest(guest, watchdog: true);
		}
		Diagnostics.Write("CHALLENGE party complete; resuming native challenge");
		Clear(restore: false);
		if (Object.op_Implicit((Object)(object)val))
		{
			try
			{
				bypass = true;
				val.SendEvent("FINISHED");
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("CHALLENGE resume", ex);
			}
			finally
			{
				bypass = false;
			}
		}
		WatchPlayer(p, 1.5f, 12f);
		recoveryGraceUntil = Time.unscaledTime + 12f;
	}

	private static void Abort(CoopSession s, string why, bool restore)
	{
		Diagnostics.Write("CHALLENGE abort " + why);
		if (restore)
		{
			foreach (Guest guest in guests)
			{
				RestoreGuest(guest, watchdog: true);
			}
		}
		Clear(restore: false);
	}

	private static void Clear(bool restore)
	{
		if (restore)
		{
			foreach (Guest guest in guests)
			{
				RestoreGuest(guest, watchdog: false);
			}
		}
		guests.Clear();
		active = false;
		native = null;
		raw = null;
		owner = null;
		scene = null;
		started = (ownerDeadline = (nextAt = 0f));
		cursor = -1;
		ownerFinished = false;
		bypass = false;
	}

	private static void WatchPlayer(PlayerSlot p, float delay, float lifetime)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || p.Down || p.Hazard)
		{
			return;
		}
		for (int i = 0; i < watches.Count; i++)
		{
			if (watches[i].Player == p && (Object)(object)watches[i].Hero == (Object)(object)p.Hero)
			{
				watches[i].CheckAt = Mathf.Min(watches[i].CheckAt, Time.unscaledTime + delay);
				watches[i].ExpireAt = Mathf.Max(watches[i].ExpireAt, Time.unscaledTime + lifetime);
				return;
			}
		}
		watches.Add(new Watch
		{
			Player = p,
			Hero = p.Hero,
			CheckAt = Time.unscaledTime + delay,
			ExpireAt = Time.unscaledTime + lifetime
		});
	}

	private static bool Stuck(Watch w)
	{
		HeroController hero = w.Hero;
		if (!Object.op_Implicit((Object)(object)hero) || w.Player == null || (Object)(object)w.Player.Hero != (Object)(object)hero)
		{
			return false;
		}
		Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
		HeroAnimationController component2 = ((Component)hero).GetComponent<HeroAnimationController>();
		if (!hero.controlReqlinquished && hero.acceptingInput && (!Object.op_Implicit((Object)(object)component2) || component2.controlEnabled))
		{
			if (Object.op_Implicit((Object)(object)component))
			{
				if (component.simulated)
				{
					return component.isKinematic;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static void TickWatchdogs(CoopSession s)
	{
		if (watches.Count == 0)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		for (int num = watches.Count - 1; num >= 0; num--)
		{
			Watch watch = watches[num];
			if (watch.Player == null || !Object.op_Implicit((Object)(object)watch.Hero) || (Object)(object)watch.Player.Hero != (Object)(object)watch.Hero || unscaledTime > watch.ExpireAt || watch.Player.Down || watch.Player.Hazard)
			{
				watches.RemoveAt(num);
			}
			else if (!(unscaledTime < watch.CheckAt))
			{
				if (s == null || !s.Active || !s.Gameplay || GameManager.instance.IsLoadingSceneTransition)
				{
					watch.CheckAt = unscaledTime + 0.5f;
				}
				else
				{
					if (Stuck(watch))
					{
						try
						{
							using (PlayerContext.Enter(watch.Player))
							{
								ActorRecovery.Reset(watch.Player);
								CoopSession.RestoreLivingVisuals(watch.Player);
							}
							watch.Player.ProtectionUntil = Time.time + 1f;
							Diagnostics.Write("CHALLENGE watchdog recovered P" + (watch.Player.Index + 1));
						}
						catch (Exception ex)
						{
							Diagnostics.Throttled("CHALLENGE watchdog", ex);
						}
					}
					watches.RemoveAt(num);
				}
			}
		}
	}

	internal static void Reset()
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (active && coopSession != null && coopSession.Active && !GameManager.instance.IsLoadingSceneTransition)
		{
			foreach (Guest guest in guests)
			{
				RestoreGuest(guest, watchdog: false);
			}
		}
		Clear(restore: false);
		watches.Clear();
		recoveryGraceUntil = 0f;
	}
}
