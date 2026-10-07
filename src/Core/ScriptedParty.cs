using System;
using System.Collections.Generic;
using GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class ScriptedParty
{
	private sealed class Guest
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal tk2dSpriteAnimator Animator;

		internal string Clip;

		internal Vector3 Offset;

		internal GameObject Effect;
	}

	private static readonly List<Guest> guests = new List<Guest>();

	private static string scene;

	private static float started;

	private static float unlocked = -1f;

	private static float playableAt = -1f;

	private static int observedFireball = -1;

	private static bool attempted;

	private static Vector3 lastPrimary;

	private static bool active;

	internal static bool Active => ShadeCloakRitual.Combined(active);

	internal static bool Holds(PlayerSlot p)
	{
		if (!active || p == null)
		{
			return ShadeCloakRitual.Holds(native: false, p);
		}
		bool native;
		foreach (Guest guest in guests)
		{
			if (guest.Player != p || !(guest.Hero == p.Hero))
			{
				continue;
			}
			native = true;
			goto IL_0082;
		}
		return ShadeCloakRitual.Holds(native: false, p);
		IL_0082:
		return ShadeCloakRitual.Holds(native, p);
	}

	private static bool FirstSpell(CoopSession s)
	{
		if (s != null && s.Active && s.Players.Count > 1 && s.Primary != null && s.Primary.Alive && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Crossroads_ShamanTemple")
		{
			return s.Data != null;
		}
		return false;
	}

	internal static void NativeMove(CoopSession s, PlayerSlot p, Vector3 from, Vector3 to)
	{
		if (p == s.Primary && FirstSpell(s) && s.Data.fireballLevel == 0 && s.InRoom(from) && s.InRoom(to) && !(Vector2.Distance(from, to) < 12f))
		{
			Begin(s, "native chamber arrival");
		}
	}

	internal static void SpellOrb(SpellGetOrb orb)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (!orb || !orb.trackToHero || !FirstSpell(coopSession))
		{
			return;
		}
		if (!active && coopSession.Data.fireballLevel == 0)
		{
			Begin(coopSession, "native spell orb");
		}
		if (!active || !orb.ptZoom)
		{
			return;
		}
		foreach (Guest guest in guests)
		{
			if ((bool)guest.Effect || !guest.Hero)
			{
				continue;
			}
			try
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(orb.ptZoom.gameObject, guest.Hero.transform);
				gameObject.name = "Local8 Spell Get P" + (guest.Player.Index + 1);
				gameObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
				gameObject.SetActive(value: true);
				ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
				if ((bool)component)
				{
					component.Play(withChildren: true);
				}
				guest.Effect = gameObject;
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("SCRIPT spell particles", ex);
			}
		}
	}

	private static void Begin(CoopSession s, string reason)
	{
		if (active || attempted || !FirstSpell(s))
		{
			return;
		}
		attempted = true;
		active = true;
		scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		started = Time.unscaledTime;
		unlocked = ((s.Data.fireballLevel > 0) ? started : (-1f));
		playableAt = -1f;
		lastPrimary = s.Primary.Hero.transform.position;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Index <= 0 || !player.Alive || !player.Ready || !player.Hero)
			{
				continue;
			}
			Guest guest = new Guest
			{
				Player = player,
				Hero = player.Hero,
				Animator = player.Hero.GetComponent<tk2dSpriteAnimator>(),
				Offset = new Vector3((float)((player.Index % 2 == 0) ? 1 : (-1)) * (0.75f + 0.35f * (float)((player.Index - 1) / 2)), 0f, (float)CoopRules.PlayerDepth(player.Index) - (float)CoopRules.PlayerDepth(0))
			};
			guests.Add(guest);
			using (PlayerContext.Enter(player))
			{
				Revival.End(player);
				player.Hero.RelinquishControl();
				player.Hero.StopAnimationControl();
				player.Hero.SetDamageMode(DamageMode.NO_DAMAGE);
				Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					component.velocity = Vector2.zero;
					component.simulated = false;
				}
			}
			Vector3 vector = s.Primary.Hero.transform.position + guest.Offset;
			if (s.InRoom(vector))
			{
				Vector3 position = player.Hero.transform.position;
				NativeDreamFx.TeleportTrail(player, position, vector);
				player.Hero.transform.position = vector;
			}
			SpriteFlash component2 = player.Hero.GetComponent<SpriteFlash>();
			if ((bool)component2)
			{
				component2.flashFocusGet();
			}
		}
		if (guests.Count == 0)
		{
			active = false;
		}
		else
		{
			Diagnostics.Write("SCRIPT first spell started " + reason + " guests=" + guests.Count);
		}
	}

	internal static void Tick(CoopSession s)
	{
		ShadeCloakRitual.Tick(s);
		if (s == null || !s.Active || s.Primary == null || !s.Primary.Hero || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Crossroads_ShamanTemple")
		{
			if (active)
			{
				End(restore: false);
			}
		}
		else
		{
			if (!active && !FirstSpell(s))
			{
				return;
			}
			if (!active && s.Data.fireballLevel == 0)
			{
				Vector3 position = s.Primary.Hero.transform.position;
				if (s.Primary.Hero.controlReqlinquished && position.x > 17f && position.x < 23f && position.y > 9.4f && position.y < 15f)
				{
					Begin(s, "native spell ascent");
				}
			}
			int fireballLevel = s.Data.fireballLevel;
			if (observedFireball < 0)
			{
				observedFireball = fireballLevel;
			}
			if (!active)
			{
				if (fireballLevel > observedFireball && s.Primary.Hero.controlReqlinquished)
				{
					Begin(s, "shared spell unlock");
				}
				observedFireball = fireballLevel;
				return;
			}
			observedFireball = fireballLevel;
			if (s.Primary.Down)
			{
				End(restore: true);
				return;
			}
			HeroController hero = s.Primary.Hero;
			if (unlocked < 0f && s.Data.fireballLevel > 0)
			{
				unlocked = Time.unscaledTime;
				Diagnostics.Write("SCRIPT first spell unlocked; waiting for native wake");
			}
			Vector3 position2 = hero.transform.position;
			if (Vector2.Distance(lastPrimary, position2) > 6f)
			{
				Vector3 vector = lastPrimary;
				string text = vector.ToString();
				vector = position2;
				Diagnostics.Write("SCRIPT first spell native move " + text + " -> " + vector.ToString());
			}
			lastPrimary = position2;
			tk2dSpriteAnimator component = hero.GetComponent<tk2dSpriteAnimator>();
			string text2 = (((bool)component && component.CurrentClip != null) ? component.CurrentClip.name : null);
			bool flag = s.InRoom(position2);
			foreach (Guest guest in guests)
			{
				if ((bool)guest.Hero && !(guest.Hero != guest.Player.Hero))
				{
					if (flag)
					{
						guest.Hero.transform.position = position2 + guest.Offset;
					}
					if (text2 != null && text2 != guest.Clip && (bool)guest.Animator && guest.Animator.GetClipByName(text2) != null)
					{
						guest.Clip = text2;
						guest.Animator.Play(text2);
					}
				}
			}
			UIManager instance = UIManager.instance;
			bool flag2 = s.Gameplay && (bool)instance && instance.uiState == UIState.PLAYING && !Plugin.Self.Panel;
			if (!flag2 && s.Gameplay && unlocked >= 0f && Time.unscaledTime - unlocked > 18f && s.Primary.Actions != null && !Plugin.Self.Panel && (Mathf.Abs(s.Primary.Actions.moveVector.X) > 0.25f || Mathf.Abs(s.Primary.Actions.moveVector.Y) > 0.25f))
			{
				flag2 = true;
				Diagnostics.Write("SCRIPT first spell recovering stuck UI state=" + (instance ? instance.uiState.ToString() : "missing"));
			}
			if (flag2)
			{
				if (playableAt < 0f)
				{
					playableAt = Time.unscaledTime;
				}
			}
			else
			{
				playableAt = -1f;
			}
			if (unlocked < 0f)
			{
				if (Time.unscaledTime - started > 40f && playableAt > 0f)
				{
					End(restore: true);
				}
			}
			else
			{
				if (!flag || playableAt < 0f || Time.unscaledTime - unlocked < 1f || Time.unscaledTime - playableAt < 0.6f)
				{
					return;
				}
				tk2dSprite component2 = hero.GetComponent<tk2dSprite>();
				Renderer component3 = hero.GetComponent<Renderer>();
				bool num = (bool)component3 && component3.enabled && (!component2 || component2.color.a > 0.05f);
				HeroAnimationController component4 = hero.GetComponent<HeroAnimationController>();
				if (!num || hero.controlReqlinquished || !hero.acceptingInput || ((bool)component4 && !component4.controlEnabled))
				{
					if (!(hero == s.Primary.Hero) || s.Primary.Actions == null || (!(Mathf.Abs(s.Primary.Actions.moveVector.X) > 0.25f) && !(Mathf.Abs(s.Primary.Actions.moveVector.Y) > 0.25f)) || Time.unscaledTime - playableAt < 3f || (InteractionRouter.ActivePlayer != null && Time.unscaledTime - playableAt < 12f))
					{
						return;
					}
					using (PlayerContext.Enter(s.Primary))
					{
						ActorRecovery.Reset(s.Primary);
						CoopSession.RestoreLivingVisuals(s.Primary);
					}
					Diagnostics.Write("SCRIPT first spell recovered native P1 after missing wake");
				}
				End(restore: true);
			}
		}
	}

	private static void End(bool restore)
	{
		if (!active)
		{
			return;
		}
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		foreach (Guest guest in guests)
		{
			if ((bool)guest.Effect)
			{
				UnityEngine.Object.Destroy(guest.Effect);
			}
			PlayerSlot player = guest.Player;
			if (restore && (bool)guest.Hero && !(player.Hero != guest.Hero) && !player.Down && !player.Hazard)
			{
				Vector3 vector = ((coopSession != null && coopSession.Primary != null && (bool)coopSession.Primary.Hero) ? coopSession.Primary.Hero.transform.position : guest.Hero.transform.position);
				if (coopSession != null && coopSession.FindSafePosition(vector + guest.Offset, player, out var result))
				{
					guest.Hero.transform.position = result;
				}
				using (PlayerContext.Enter(player))
				{
					ActorRecovery.Reset(player);
					CoopSession.RestoreLivingVisuals(player);
				}
				player.ProtectionUntil = Time.time + 1.5f;
				player.SafePoint = guest.Hero.transform.position;
				player.HasSafePoint = coopSession?.IsSafe(player.SafePoint, player) ?? false;
				string text = (player.Index + 1).ToString();
				Vector3 safePoint = player.SafePoint;
				Diagnostics.Write("SCRIPT first spell wake P" + text + " at=" + safePoint.ToString());
			}
		}
		guests.Clear();
		active = false;
		scene = null;
		started = 0f;
		unlocked = (playableAt = -1f);
	}

	internal static void Reset()
	{
		End(restore: false);
		observedFireball = -1;
		attempted = false;
	}
}
