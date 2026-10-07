using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class AcidSwimming
{
	internal struct Surface
	{
		internal Collider2D Collider;

		internal bool Acid;
	}

	internal struct PrimaryState
	{
		internal Vector3 Position;

		internal Vector2 Velocity;

		internal bool Valid;
	}

	private static readonly List<Surface> pools = new List<Surface>();

	private static readonly float[] floatHeight = new float[8];

	private static readonly float[] jumpUntil = new float[8];

	private static readonly Dictionary<Fsm, bool> surfaceFsms = new Dictionary<Fsm, bool>();

	private static readonly Dictionary<Fsm, int> blockedPlayers = new Dictionary<Fsm, int>();

	private static readonly HashSet<int> blockedEvents = new HashSet<int>();

	private static readonly Dictionary<int, bool> swimClips = new Dictionary<int, bool>();

	private static readonly HashSet<long> isolatedCollisions = new HashSet<long>();

	private static readonly float[] nextSwimRetry = new float[8];

	private static int sceneHandle = -1;

	private static float nextScan;

	private static float nextPrimaryDiagnostic;

	private static readonly string[] visualClips = new string[8];

	private static Vector3 lastPrimaryPosition;

	private static Vector2 lastPrimaryVelocity;

	private static bool primaryPositionKnown;

	internal static PrimaryState CapturePrimary()
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !coopSession.Primary.Alive || !coopSession.Primary.Ready)
		{
			return default(PrimaryState);
		}
		Rigidbody2D component = coopSession.Primary.Hero.GetComponent<Rigidbody2D>();
		PrimaryState result = default(PrimaryState);
		result.Position = coopSession.Primary.Hero.transform.position;
		result.Velocity = (component ? component.velocity : Vector2.zero);
		result.Valid = true;
		return result;
	}

	internal static void RestorePrimary(PrimaryState before, string source)
	{
		if (!before.Valid)
		{
			return;
		}
		CoopSession session = Plugin.Self.Session;
		if (session == null || !session.Active || session.Primary == null || !session.Primary.Hero)
		{
			return;
		}
		HeroController hero = session.Primary.Hero;
		Vector3 position = hero.transform.position;
		if (!((position - before.Position).sqrMagnitude < 0.0025f))
		{
			hero.transform.position = before.Position;
			Rigidbody2D component = hero.GetComponent<Rigidbody2D>();
			if ((bool)component)
			{
				component.position = before.Position;
				component.velocity = before.Velocity;
			}
			string[] obj = new string[6] { "WATER prevented P1 displacement during P2 ", source, " ", null, null, null };
			Vector3 vector = position;
			obj[3] = vector.ToString();
			obj[4] = " -> ";
			vector = before.Position;
			obj[5] = vector.ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	private static void Scan()
	{
		Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
		if (sceneHandle == activeScene.handle && Time.unscaledTime < nextScan)
		{
			AcidSurfaceImmersion.Classify(pools, nextScan);
			return;
		}
		sceneHandle = activeScene.handle;
		nextScan = Time.unscaledTime + 5f;
		pools.Clear();
		Collider2D[] array = UnityEngine.Object.FindObjectsOfType<Collider2D>();
		foreach (Collider2D collider2D in array)
		{
			if (!collider2D || !collider2D.enabled || collider2D.bounds.size.x < 1.5f || (bool)collider2D.GetComponentInParent<HeroController>())
			{
				continue;
			}
			DamageHero componentInParent = collider2D.GetComponentInParent<DamageHero>();
			bool flag = collider2D.CompareTag("Acid") || ((bool)componentInParent && componentInParent.hazardType == 3);
			bool flag2 = collider2D.CompareTag("Water Surface");
			string text = collider2D.name.ToLowerInvariant();
			if (!flag2 && !flag)
			{
				Transform parent = collider2D.transform.parent;
				while ((bool)parent && text.Length < 110)
				{
					text = text + " " + parent.name.ToLowerInvariant();
					parent = parent.parent;
				}
			}
			if (!flag)
			{
				flag = text == "acid" || text.Contains("acid surface") || text.Contains("surface acid") || text.Contains("acid pool");
			}
			if (!flag2)
			{
				flag2 = text == "water" || text.Contains("water surface") || text.Contains("surface water") || text.Contains("water pool") || text.Contains("pool water");
			}
			if (flag2 || flag)
			{
				pools.Add(new Surface
				{
					Collider = collider2D,
					Acid = flag
				});
			}
		}
		AcidSurfaceImmersion.Classify(pools, nextScan);
	}

	internal static bool WorldSurface(GameObject go)
	{
		if (!go || (bool)go.GetComponentInParent<HeroController>())
		{
			return false;
		}
		Transform transform = go.transform;
		while ((bool)transform)
		{
			if (transform.CompareTag("Water Surface") || transform.CompareTag("Acid"))
			{
				return true;
			}
			string text = transform.name.ToLowerInvariant();
			if (text == "water" || text == "acid" || text.Contains("water surface") || text.Contains("surface water") || text.Contains("acid surface") || text.Contains("surface acid") || text.Contains("water pool") || text.Contains("pool water") || text.Contains("acid pool"))
			{
				return true;
			}
			if ((bool)transform.GetComponent<HealthManager>() || (bool)transform.GetComponent<HeroController>())
			{
				break;
			}
			transform = transform.parent;
		}
		return false;
	}

	internal static bool WorldSurface(Fsm f)
	{
		if (f == null || !f.GameObject)
		{
			return false;
		}
		if (surfaceFsms.TryGetValue(f, out var value))
		{
			return value;
		}
		if ((bool)f.GameObject.GetComponentInParent<HeroController>())
		{
			surfaceFsms[f] = false;
			return false;
		}
		string text = (f.Name ?? "").ToLowerInvariant();
		if (WorldSurface(f.GameObject))
		{
			goto IL_00a6;
		}
		switch (text)
		{
		case "water":
		case "surface water":
		case "water surface":
		case "acid swim":
			goto IL_00a6;
		}
		int num = ((text == "swim") ? 1 : 0);
		goto IL_00a7;
		IL_00a6:
		num = 1;
		goto IL_00a7;
		IL_00a7:
		value = (byte)num != 0;
		surfaceFsms[f] = value;
		return value;
	}

	private static bool AtPool(PlayerSlot p)
	{
		if (p == null || !p.Hero)
		{
			return false;
		}
		Scan();
		Vector3 position = p.Hero.transform.position;
		foreach (Surface pool in pools)
		{
			if ((bool)pool.Collider && pool.Collider.enabled)
			{
				Bounds bounds = pool.Collider.bounds;
				if (position.x > bounds.min.x && position.x < bounds.max.x && position.y > bounds.min.y - 0.5f && DuelGround.Body(p).min.y < bounds.max.y + 0.08f)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void PrepareCollisions(PlayerSlot p, bool hasIsma)
	{
		if (p == null || p.Index == 0 || !p.Hero || !p.Hero.gameObject.activeInHierarchy)
		{
			return;
		}
		Scan();
		Vector3 position = p.Hero.transform.position;
		Collider2D[] array = p.Colliders ?? p.Hero.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Surface pool in pools)
		{
			Collider2D collider = pool.Collider;
			if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy || (pool.Acid && !hasIsma))
			{
				continue;
			}
			Bounds bounds = collider.bounds;
			if (position.x < bounds.min.x - 5f || position.x > bounds.max.x + 5f || position.y < bounds.min.y - 4f || position.y > bounds.max.y + 5f)
			{
				continue;
			}
			Collider2D[] array2 = array;
			foreach (Collider2D collider2D in array2)
			{
				if ((bool)collider2D && collider2D.enabled && collider2D.gameObject.activeInHierarchy && !(collider2D == collider))
				{
					if (!Physics2D.GetIgnoreCollision(collider2D, collider))
					{
						Physics2D.IgnoreCollision(collider2D, collider, ignore: true);
					}
					long item = ((long)collider2D.GetInstanceID() << 32) | (uint)collider.GetInstanceID();
					if (isolatedCollisions.Add(item))
					{
						Diagnostics.Write("WATER collider isolated P" + (p.Index + 1) + " knight=" + collider2D.name + " water=" + collider.name);
					}
				}
			}
		}
	}

	internal static bool SuppressWorldFsm(Fsm f)
	{
		if (!WorldSurface(f))
		{
			return false;
		}
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || AtPool(coopSession.Primary))
		{
			return false;
		}
		foreach (PlayerSlot player in coopSession.Players)
		{
			if (player.Index > 0 && player.Ready && player.Alive && (player.AcidAssistActive || AtPool(player)))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool WrongPrimarySwim(HeroController hero)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession != null && coopSession.Active && coopSession.Primary != null && hero == coopSession.Primary.Hero)
		{
			return !AtPool(coopSession.Primary);
		}
		return false;
	}

	internal static bool WaterCollision(Collision2D collision)
	{
		if (collision == null)
		{
			return false;
		}
		if (WorldSurface(collision.gameObject))
		{
			return true;
		}
		Scan();
		foreach (Surface pool in pools)
		{
			if ((bool)pool.Collider && (pool.Collider == collision.collider || pool.Collider == collision.otherCollider || pool.Collider.gameObject == collision.gameObject))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool WorldWaterContact(GameObject go, PlayerSlot player)
	{
		if (!go || player == null || player.Index == 0)
		{
			return false;
		}
		if (WorldSurface(go))
		{
			return true;
		}
		if (!AtPool(player) && !blockedEvents.Contains(go.GetInstanceID() * 9 + player.Index))
		{
			return false;
		}
		Scan();
		Bounds bounds = DuelGround.Body(player);
		Collider2D[] componentsInChildren = go.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Collider2D collider2D in componentsInChildren)
		{
			if (!collider2D || !collider2D.enabled || !collider2D.isTrigger || !collider2D.bounds.Intersects(bounds))
			{
				continue;
			}
			foreach (Surface pool in pools)
			{
				if ((bool)pool.Collider && pool.Collider != collider2D && collider2D.bounds.Intersects(pool.Collider.bounds))
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void NoteBlocked(GameObject go, PlayerSlot p)
	{
		if ((bool)go && p != null)
		{
			int item = go.GetInstanceID() * 9 + p.Index;
			if (blockedEvents.Add(item))
			{
				Diagnostics.Write("WATER isolated P" + (p.Index + 1) + " event=" + go.name + " at=" + p.Hero.transform.position.ToString());
			}
		}
	}

	internal static void GuardPrimary()
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !coopSession.Primary.Hero || AtPool(coopSession.Primary))
		{
			return;
		}
		HeroController hero = coopSession.Primary.Hero;
		if (hero.inAcid || hero.cState.inAcid || hero.cState.swimming)
		{
			hero.inAcid = false;
			hero.cState.inAcid = false;
			hero.cState.swimming = false;
			RepairPrimaryAnimation(hero);
			if (Time.unscaledTime >= nextPrimaryDiagnostic)
			{
				nextPrimaryDiagnostic = Time.unscaledTime + 2f;
				Diagnostics.Write("WATER cleared remote P1 swimming state");
			}
		}
	}

	private static void RepairPrimaryAnimation(HeroController hero)
	{
		HeroAnimationController component = hero.GetComponent<HeroAnimationController>();
		if ((bool)component && component.enabled)
		{
			component.StartControl();
		}
	}

	internal static void NoteBlocked(Fsm f, PlayerSlot p)
	{
		blockedPlayers.TryGetValue(f, out var value);
		int num = 1 << p.Index;
		if ((value & num) == 0)
		{
			blockedPlayers[f] = value | num;
			Diagnostics.Write("WATER isolated P" + (p.Index + 1) + " object=" + f.GameObject.name + " fsm=" + f.Name);
		}
	}

	internal static bool HasSwimClip(PlayerSlot p)
	{
		if (p == null || !p.Hero)
		{
			return false;
		}
		tk2dSpriteAnimator component = p.Hero.GetComponent<tk2dSpriteAnimator>();
		if (!component)
		{
			return false;
		}
		int instanceID = component.GetInstanceID();
		if (swimClips.TryGetValue(instanceID, out var value) && value && component.GetClipByName("Swim") != null)
		{
			return true;
		}
		if (swimClips.ContainsKey(instanceID) && !value && Time.unscaledTime < nextSwimRetry[p.Index])
		{
			return false;
		}
		value = component.GetClipByName("Swim") != null;
		if (!value && p.Index > 0)
		{
			CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
			tk2dSpriteAnimator tk2dSpriteAnimator = ((coopSession == null || coopSession.Primary == null || !coopSession.Primary.Hero) ? null : coopSession.Primary.Hero.GetComponent<tk2dSpriteAnimator>());
			tk2dSpriteAnimationClip tk2dSpriteAnimationClip = (tk2dSpriteAnimator ? tk2dSpriteAnimator.GetClipByName("Swim") : null);
			if (tk2dSpriteAnimationClip != null && (bool)component.Library)
			{
				tk2dSpriteAnimation tk2dSpriteAnimation = component.gameObject.AddComponent<tk2dSpriteAnimation>();
				tk2dSpriteAnimationClip[] array = component.Library.clips ?? new tk2dSpriteAnimationClip[0];
				tk2dSpriteAnimation.clips = new tk2dSpriteAnimationClip[array.Length + 1];
				Array.Copy(array, tk2dSpriteAnimation.clips, array.Length);
				tk2dSpriteAnimation.clips[array.Length] = new tk2dSpriteAnimationClip(tk2dSpriteAnimationClip);
				component.Library = tk2dSpriteAnimation;
				value = true;
			}
		}
		bool num = !swimClips.ContainsKey(instanceID) || swimClips[instanceID] != value;
		swimClips[instanceID] = value;
		nextSwimRetry[p.Index] = Time.unscaledTime + 2f;
		if (num)
		{
			Diagnostics.Write("WATER P" + (p.Index + 1) + " native swim clip=" + value + " P1=" + (Plugin.Self != null && Plugin.Self.Session != null && Plugin.Self.Session.Primary != null && (bool)Plugin.Self.Session.Primary.Hero && (bool)Plugin.Self.Session.Primary.Hero.GetComponent<tk2dSpriteAnimator>() && Plugin.Self.Session.Primary.Hero.GetComponent<tk2dSpriteAnimator>().GetClipByName("Swim") != null));
		}
		return value;
	}

	internal static bool SurfacePose(PlayerSlot p)
	{
		return GuestSurfaceAnimation.Pose(p);
	}

	internal static void GuardPrimaryPosition()
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !coopSession.Primary.Alive || !coopSession.Primary.Ready || coopSession.EntryProxy || coopSession.Primary.ArenaTransfer || coopSession.Primary.Hero.cState.transitioning || GameManager.instance == null || GameManager.instance.IsLoadingSceneTransition)
		{
			primaryPositionKnown = false;
			return;
		}
		HeroController hero = coopSession.Primary.Hero;
		Rigidbody2D component = hero.GetComponent<Rigidbody2D>();
		Vector3 position = hero.transform.position;
		if (primaryPositionKnown && coopSession.Players.Count > 1 && !AtPoolPosition(lastPrimaryPosition, coopSession.Primary) && AtPool(coopSession.Primary))
		{
			bool flag = false;
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (player.Index > 0 && player.Alive && player.Ready && (player.AcidAssistActive || AtPool(player)))
				{
					flag = true;
					break;
				}
			}
			if (flag && (position - lastPrimaryPosition).sqrMagnitude > 1.44f)
			{
				tk2dSpriteAnimator component2 = hero.GetComponent<tk2dSpriteAnimator>();
				string text = (((bool)component2 && component2.CurrentClip != null) ? component2.CurrentClip.name : "none");
				bool swimming = hero.cState.swimming;
				hero.transform.position = lastPrimaryPosition;
				if ((bool)component)
				{
					component.position = lastPrimaryPosition;
					component.velocity = lastPrimaryVelocity;
				}
				hero.inAcid = false;
				hero.cState.inAcid = false;
				hero.cState.swimming = false;
				RepairPrimaryAnimation(hero);
				string[] obj = new string[10] { "WATER restored P1 after remote water warp ", null, null, null, null, null, null, null, null, null };
				Vector3 vector = position;
				obj[1] = vector.ToString();
				obj[2] = " -> ";
				vector = lastPrimaryPosition;
				obj[3] = vector.ToString();
				obj[4] = " state=";
				obj[5] = hero.hero_state.ToString();
				obj[6] = " clip=";
				obj[7] = text;
				obj[8] = " swimming=";
				obj[9] = swimming.ToString();
				Diagnostics.Write(string.Concat(obj));
				position = lastPrimaryPosition;
			}
		}
		lastPrimaryPosition = position;
		lastPrimaryVelocity = (component ? component.velocity : Vector2.zero);
		primaryPositionKnown = true;
	}

	private static bool AtPoolPosition(Vector3 pos, PlayerSlot p)
	{
		Scan();
		if (p == null || !p.Hero)
		{
			return false;
		}
		float num = DuelGround.Body(p).min.y + (pos.y - p.Hero.transform.position.y);
		foreach (Surface pool in pools)
		{
			if ((bool)pool.Collider && pool.Collider.enabled)
			{
				Bounds bounds = pool.Collider.bounds;
				if (pos.x > bounds.min.x && pos.x < bounds.max.x && pos.y > bounds.min.y - 0.5f && num < bounds.max.y + 0.08f)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void Reset()
	{
		surfaceFsms.Clear();
		blockedPlayers.Clear();
		blockedEvents.Clear();
		swimClips.Clear();
		isolatedCollisions.Clear();
		pools.Clear();
		sceneHandle = -1;
		nextScan = (nextPrimaryDiagnostic = 0f);
		primaryPositionKnown = false;
		Array.Clear(jumpUntil, 0, jumpUntil.Length);
		Array.Clear(nextSwimRetry, 0, nextSwimRetry.Length);
		Array.Clear(visualClips, 0, visualClips.Length);
		GuestSurfaceAnimation.Reset();
		AcidSurfaceImmersion.Reset();
	}

	internal static void Tick(PlayerSlot p, bool hasIsma)
	{
		if (!p.Alive || !p.Ready)
		{
			return;
		}
		Scan();
		PrepareCollisions(p, hasIsma);
		HeroController hero = p.Hero;
		Rigidbody2D component = hero.GetComponent<Rigidbody2D>();
		if (!component || hero.controlReqlinquished || hero.cState.transitioning)
		{
			Exit(p);
			return;
		}
		if (p.AcidAssistActive && p.Actions != null && !p.InputBlocked && p.Actions.jump.WasPressed)
		{
			PrimaryState before = CapturePrimary();
			using (PlayerContext.Enter(p))
			{
				Reflect.Call(hero, "HeroJumpNoEffect");
			}
			RestorePrimary(before, "jump");
			Exit(p);
			jumpUntil[p.Index] = Time.time + 0.42f;
			component.velocity = new Vector2(component.velocity.x, Mathf.Max(hero.JUMP_SPEED_UNDERWATER, hero.JUMP_SPEED * 0.8f));
			Diagnostics.Write("WATER jump P" + (p.Index + 1) + " speed=" + component.velocity.y);
			return;
		}
		if (Time.time < jumpUntil[p.Index] || hero.cState.jumping || hero.cState.dashing || hero.cState.superDashing)
		{
			Exit(p);
			return;
		}
		Vector3 position = p.Hero.transform.position;
		Bounds bounds = DuelGround.Body(p);
		float num = bounds.extents.y - (bounds.center.y - position.y);
		float num2 = float.NegativeInfinity;
		bool acid = default(bool);
		Collider2D collider = default(Collider2D);
		foreach (Surface pool in pools)
		{
			if (!pool.Collider || !pool.Collider.enabled || !pool.Collider.gameObject.activeInHierarchy || (pool.Acid && !hasIsma))
			{
				continue;
			}
			Bounds bounds2 = pool.Collider.bounds;
			if (!(position.x < bounds2.min.x + 0.15f) && !(position.x > bounds2.max.x - 0.15f))
			{
				float num3 = bounds2.max.y + num - AcidSurfaceImmersion.Inset(pool.Acid, pool.Collider, num);
				if (SwimmingRules.InRange(position.y, num3, p.AcidAssistActive) && bounds2.max.y > num2)
				{
					num2 = bounds2.max.y;
					acid = pool.Acid;
					collider = pool.Collider;
				}
			}
		}
		if (float.IsNegativeInfinity(num2))
		{
			Exit(p);
			return;
		}
		if (!p.AcidAssistActive)
		{
			p.AcidAssistActive = true;
			Diagnostics.Write("WATER swim P" + (p.Index + 1) + " surface=" + num2 + " feet=" + num);
		}
		floatHeight[p.Index] = num2 + num - AcidSurfaceImmersion.Inset(acid, collider, num);
		Float(p);
	}

	internal static void Float(PlayerSlot p)
	{
		if (p == null || !p.AcidAssistActive || !p.Hero || !p.Alive)
		{
			return;
		}
		HeroController hero = p.Hero;
		Rigidbody2D component = hero.GetComponent<Rigidbody2D>();
		if (!component)
		{
			return;
		}
		if (hero.cState.jumping || hero.cState.dashing || hero.cState.superDashing)
		{
			Exit(p);
			return;
		}
		bool flag = (hero.inAcid = HasSwimClip(p));
		hero.cState.inAcid = flag;
		hero.cState.onGround = false;
		hero.cState.falling = false;
		hero.cState.swimming = flag;
		if (hero.hero_state != ActorStates.airborne)
		{
			using (PlayerContext.Enter(p))
			{
				Reflect.Call(hero, "SetState", ActorStates.airborne);
			}
		}
		float num = floatHeight[p.Index] - component.position.y;
		float y = Mathf.Clamp(num * 7f, -5f, 8f);
		if (Mathf.Abs(num) < 0.025f)
		{
			y = 0f;
		}
		float value = ((p.Actions != null && !p.InputBlocked) ? (p.Actions.right.Value - p.Actions.left.Value) : 0f);
		component.velocity = new Vector2(Mathf.Clamp(value, -1f, 1f) * hero.UNDERWATER_SPEED, y);
		component.gravityScale = 0f;
	}

	private static void Exit(PlayerSlot p)
	{
		if (!p.AcidAssistActive)
		{
			return;
		}
		p.AcidAssistActive = false;
		visualClips[p.Index] = null;
		if ((bool)p.Hero)
		{
			p.Hero.inAcid = false;
			p.Hero.cState.inAcid = false;
			p.Hero.cState.swimming = false;
			Reflect.Set(p.Hero, "airDashed", false);
			Reflect.Set(p.Hero, "doubleJumped", false);
			Rigidbody2D component = p.Hero.GetComponent<Rigidbody2D>();
			if ((bool)component)
			{
				component.gravityScale = p.Hero.DEFAULT_GRAVITY;
			}
			Diagnostics.Write("WATER leave P" + (p.Index + 1));
		}
	}
}
