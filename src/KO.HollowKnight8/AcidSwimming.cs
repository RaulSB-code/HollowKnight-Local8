using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class AcidSwimming
{
	private struct Surface
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
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !coopSession.Primary.Alive || !coopSession.Primary.Ready)
		{
			return default(PrimaryState);
		}
		Rigidbody2D component = ((Component)coopSession.Primary.Hero).GetComponent<Rigidbody2D>();
		return new PrimaryState
		{
			Position = ((Component)coopSession.Primary.Hero).transform.position,
			Velocity = (Object.op_Implicit((Object)(object)component) ? component.velocity : Vector2.zero),
			Valid = true
		};
	}

	internal unsafe static void RestorePrimary(PrimaryState before, string source)
	{
		if (!before.Valid)
		{
			return;
		}
		CoopSession session = Plugin.Self.Session;
		if (session == null || !session.Active || session.Primary == null || !Object.op_Implicit((Object)(object)session.Primary.Hero))
		{
			return;
		}
		HeroController hero = session.Primary.Hero;
		Vector3 position = ((Component)hero).transform.position;
		Vector3 val = position - before.Position;
		if (!(((Vector3)(ref val)).sqrMagnitude < 0.0025f))
		{
			((Component)hero).transform.position = before.Position;
			Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.position = Vector2.op_Implicit(before.Position);
				component.velocity = before.Velocity;
			}
			string[] obj = new string[6] { "WATER prevented P1 displacement during P2 ", source, " ", null, null, null };
			val = position;
			obj[3] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
			obj[4] = " -> ";
			val = before.Position;
			obj[5] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	private static void Scan()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if (sceneHandle == ((Scene)(ref activeScene)).handle && Time.unscaledTime < nextScan)
		{
			return;
		}
		sceneHandle = ((Scene)(ref activeScene)).handle;
		nextScan = Time.unscaledTime + 5f;
		pools.Clear();
		Collider2D[] array = Object.FindObjectsOfType<Collider2D>();
		foreach (Collider2D val in array)
		{
			if (!Object.op_Implicit((Object)(object)val) || !((Behaviour)val).enabled)
			{
				continue;
			}
			Bounds bounds = val.bounds;
			if (((Bounds)(ref bounds)).size.x < 1.5f || Object.op_Implicit((Object)(object)((Component)val).GetComponentInParent<HeroController>()))
			{
				continue;
			}
			DamageHero componentInParent = ((Component)val).GetComponentInParent<DamageHero>();
			bool flag = ((Component)val).CompareTag("Acid") || (Object.op_Implicit((Object)(object)componentInParent) && componentInParent.hazardType == 3);
			bool flag2 = ((Component)val).CompareTag("Water Surface");
			string text = ((Object)val).name.ToLowerInvariant();
			if (!flag2 && !flag)
			{
				Transform parent = ((Component)val).transform.parent;
				while (Object.op_Implicit((Object)(object)parent) && text.Length < 110)
				{
					text = text + " " + ((Object)parent).name.ToLowerInvariant();
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
					Collider = val,
					Acid = flag
				});
			}
		}
	}

	internal static bool WorldSurface(GameObject go)
	{
		if (!Object.op_Implicit((Object)(object)go) || Object.op_Implicit((Object)(object)go.GetComponentInParent<HeroController>()))
		{
			return false;
		}
		Transform val = go.transform;
		while (Object.op_Implicit((Object)(object)val))
		{
			if (((Component)val).CompareTag("Water Surface") || ((Component)val).CompareTag("Acid"))
			{
				return true;
			}
			string text = ((Object)val).name.ToLowerInvariant();
			if (text == "water" || text == "acid" || text.Contains("water surface") || text.Contains("surface water") || text.Contains("acid surface") || text.Contains("surface acid") || text.Contains("water pool") || text.Contains("pool water") || text.Contains("acid pool"))
			{
				return true;
			}
			if (Object.op_Implicit((Object)(object)((Component)val).GetComponent<HealthManager>()) || Object.op_Implicit((Object)(object)((Component)val).GetComponent<HeroController>()))
			{
				break;
			}
			val = val.parent;
		}
		return false;
	}

	internal static bool WorldSurface(Fsm f)
	{
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
		{
			return false;
		}
		if (surfaceFsms.TryGetValue(f, out var value))
		{
			return value;
		}
		if (Object.op_Implicit((Object)(object)f.GameObject.GetComponentInParent<HeroController>()))
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
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return false;
		}
		Scan();
		Vector3 position = ((Component)p.Hero).transform.position;
		foreach (Surface pool in pools)
		{
			if (!Object.op_Implicit((Object)(object)pool.Collider) || !((Behaviour)pool.Collider).enabled)
			{
				continue;
			}
			Bounds bounds = pool.Collider.bounds;
			if (position.x > ((Bounds)(ref bounds)).min.x && position.x < ((Bounds)(ref bounds)).max.x && position.y > ((Bounds)(ref bounds)).min.y - 0.5f)
			{
				Bounds val = DuelGround.Body(p);
				if (((Bounds)(ref val)).min.y < ((Bounds)(ref bounds)).max.y + 0.08f)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void PrepareCollisions(PlayerSlot p, bool hasIsma)
	{
		if (p == null || p.Index == 0 || !Object.op_Implicit((Object)(object)p.Hero) || !((Component)p.Hero).gameObject.activeInHierarchy)
		{
			return;
		}
		Scan();
		Vector3 position = ((Component)p.Hero).transform.position;
		Collider2D[] array = p.Colliders ?? ((Component)p.Hero).GetComponentsInChildren<Collider2D>(true);
		foreach (Surface pool in pools)
		{
			Collider2D collider = pool.Collider;
			if (!Object.op_Implicit((Object)(object)collider) || !((Behaviour)collider).enabled || !((Component)collider).gameObject.activeInHierarchy || (pool.Acid && !hasIsma))
			{
				continue;
			}
			Bounds bounds = collider.bounds;
			if (position.x < ((Bounds)(ref bounds)).min.x - 5f || position.x > ((Bounds)(ref bounds)).max.x + 5f || position.y < ((Bounds)(ref bounds)).min.y - 4f || position.y > ((Bounds)(ref bounds)).max.y + 5f)
			{
				continue;
			}
			Collider2D[] array2 = array;
			foreach (Collider2D val in array2)
			{
				if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).enabled && ((Component)val).gameObject.activeInHierarchy && !((Object)(object)val == (Object)(object)collider))
				{
					if (!Physics2D.GetIgnoreCollision(val, collider))
					{
						Physics2D.IgnoreCollision(val, collider, true);
					}
					long item = ((long)((Object)val).GetInstanceID() << 32) | (uint)((Object)collider).GetInstanceID();
					if (isolatedCollisions.Add(item))
					{
						Diagnostics.Write("WATER collider isolated P" + (p.Index + 1) + " knight=" + ((Object)val).name + " water=" + ((Object)collider).name);
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
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
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
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession != null && coopSession.Active && coopSession.Primary != null && (Object)(object)hero == (Object)(object)coopSession.Primary.Hero)
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
			if (Object.op_Implicit((Object)(object)pool.Collider) && ((Object)(object)pool.Collider == (Object)(object)collision.collider || (Object)(object)pool.Collider == (Object)(object)collision.otherCollider || (Object)(object)((Component)pool.Collider).gameObject == (Object)(object)collision.gameObject))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool WorldWaterContact(GameObject go, PlayerSlot player)
	{
		if (!Object.op_Implicit((Object)(object)go) || player == null || player.Index == 0)
		{
			return false;
		}
		if (WorldSurface(go))
		{
			return true;
		}
		if (!AtPool(player) && !blockedEvents.Contains(((Object)go).GetInstanceID() * 9 + player.Index))
		{
			return false;
		}
		Scan();
		Bounds val = DuelGround.Body(player);
		Collider2D[] componentsInChildren = go.GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D val2 in componentsInChildren)
		{
			if (!Object.op_Implicit((Object)(object)val2) || !((Behaviour)val2).enabled || !val2.isTrigger)
			{
				continue;
			}
			Bounds bounds = val2.bounds;
			if (!((Bounds)(ref bounds)).Intersects(val))
			{
				continue;
			}
			foreach (Surface pool in pools)
			{
				if (Object.op_Implicit((Object)(object)pool.Collider) && (Object)(object)pool.Collider != (Object)(object)val2)
				{
					bounds = val2.bounds;
					if (((Bounds)(ref bounds)).Intersects(pool.Collider.bounds))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	internal static void NoteBlocked(GameObject go, PlayerSlot p)
	{
		if (Object.op_Implicit((Object)(object)go) && p != null)
		{
			int item = ((Object)go).GetInstanceID() * 9 + p.Index;
			if (blockedEvents.Add(item))
			{
				Diagnostics.Write("WATER isolated P" + (p.Index + 1) + " event=" + ((Object)go).name + " at=" + ((object)((Component)p.Hero).transform.position/*cast due to .constrained prefix*/).ToString());
			}
		}
	}

	internal static void GuardPrimary()
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !Object.op_Implicit((Object)(object)coopSession.Primary.Hero) || AtPool(coopSession.Primary))
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
		HeroAnimationController component = ((Component)hero).GetComponent<HeroAnimationController>();
		if (Object.op_Implicit((Object)(object)component) && ((Behaviour)component).enabled)
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
			Diagnostics.Write("WATER isolated P" + (p.Index + 1) + " object=" + ((Object)f.GameObject).name + " fsm=" + f.Name);
		}
	}

	internal static bool HasSwimClip(PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return false;
		}
		tk2dSpriteAnimator component = ((Component)p.Hero).GetComponent<tk2dSpriteAnimator>();
		if (!Object.op_Implicit((Object)(object)component))
		{
			return false;
		}
		int instanceID = ((Object)component).GetInstanceID();
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
			CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
			tk2dSpriteAnimator val = ((coopSession == null || coopSession.Primary == null || !Object.op_Implicit((Object)(object)coopSession.Primary.Hero)) ? null : ((Component)coopSession.Primary.Hero).GetComponent<tk2dSpriteAnimator>());
			tk2dSpriteAnimationClip val2 = (Object.op_Implicit((Object)(object)val) ? val.GetClipByName("Swim") : null);
			if (val2 != null && Object.op_Implicit((Object)(object)component.Library))
			{
				tk2dSpriteAnimation val3 = ((Component)component).gameObject.AddComponent<tk2dSpriteAnimation>();
				tk2dSpriteAnimationClip[] array = (tk2dSpriteAnimationClip[])(((object)component.Library.clips) ?? ((object)new tk2dSpriteAnimationClip[0]));
				val3.clips = (tk2dSpriteAnimationClip[])(object)new tk2dSpriteAnimationClip[array.Length + 1];
				Array.Copy(array, val3.clips, array.Length);
				val3.clips[array.Length] = new tk2dSpriteAnimationClip(val2);
				component.Library = val3;
				value = true;
			}
		}
		bool num = !swimClips.ContainsKey(instanceID) || swimClips[instanceID] != value;
		swimClips[instanceID] = value;
		nextSwimRetry[p.Index] = Time.unscaledTime + 2f;
		if (num)
		{
			Diagnostics.Write("WATER P" + (p.Index + 1) + " native swim clip=" + value + " P1=" + ((Object)(object)Plugin.Self != (Object)null && Plugin.Self.Session != null && Plugin.Self.Session.Primary != null && Object.op_Implicit((Object)(object)Plugin.Self.Session.Primary.Hero) && Object.op_Implicit((Object)(object)((Component)Plugin.Self.Session.Primary.Hero).GetComponent<tk2dSpriteAnimator>()) && ((Component)Plugin.Self.Session.Primary.Hero).GetComponent<tk2dSpriteAnimator>().GetClipByName("Swim") != null));
		}
		return value;
	}

	internal static bool SurfacePose(PlayerSlot p)
	{
		if (p == null || p.Index == 0 || !p.AcidAssistActive || !Object.op_Implicit((Object)(object)p.Hero) || HasSwimClip(p))
		{
			return false;
		}
		HeroController hero = p.Hero;
		if (hero.cState.jumping || hero.cState.dashing || hero.cState.superDashing || hero.cState.attacking || hero.cState.casting || hero.cState.recoiling || hero.cState.dead || hero.cState.transitioning)
		{
			return false;
		}
		tk2dSpriteAnimator component = ((Component)hero).GetComponent<tk2dSpriteAnimator>();
		if (!Object.op_Implicit((Object)(object)component))
		{
			return false;
		}
		string text = "Idle";
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		string text2 = "none";
		if (coopSession != null && coopSession.Primary != null && Object.op_Implicit((Object)(object)coopSession.Primary.Hero) && AtPool(coopSession.Primary))
		{
			tk2dSpriteAnimator component2 = ((Component)coopSession.Primary.Hero).GetComponent<tk2dSpriteAnimator>();
			if (Object.op_Implicit((Object)(object)component2) && component2.CurrentClip != null)
			{
				text2 = component2.CurrentClip.name;
				if ((text2.IndexOf("Swim", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || text2 == "Idle") && component.GetClipByName(text2) != null)
				{
					text = text2;
				}
			}
		}
		if (component.GetClipByName(text) == null)
		{
			return false;
		}
		if (visualClips[p.Index] != text)
		{
			visualClips[p.Index] = text;
			Diagnostics.Write("WATER surface pose P" + (p.Index + 1) + "=" + text + " native P1=" + text2);
		}
		if (component.CurrentClip == null || component.CurrentClip.name != text)
		{
			component.Play(text);
		}
		return true;
	}

	internal unsafe static void GuardPrimaryPosition()
	{
		CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (coopSession == null || !coopSession.Active || coopSession.Primary == null || !coopSession.Primary.Alive || !coopSession.Primary.Ready || coopSession.EntryProxy || coopSession.Primary.ArenaTransfer || coopSession.Primary.Hero.cState.transitioning || (Object)(object)GameManager.instance == (Object)null || GameManager.instance.IsLoadingSceneTransition)
		{
			primaryPositionKnown = false;
			return;
		}
		HeroController hero = coopSession.Primary.Hero;
		Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
		Vector3 position = ((Component)hero).transform.position;
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
			if (flag)
			{
				Vector3 val = position - lastPrimaryPosition;
				if (((Vector3)(ref val)).sqrMagnitude > 1.44f)
				{
					tk2dSpriteAnimator component2 = ((Component)hero).GetComponent<tk2dSpriteAnimator>();
					string text = ((Object.op_Implicit((Object)(object)component2) && component2.CurrentClip != null) ? component2.CurrentClip.name : "none");
					bool swimming = hero.cState.swimming;
					((Component)hero).transform.position = lastPrimaryPosition;
					if (Object.op_Implicit((Object)(object)component))
					{
						component.position = Vector2.op_Implicit(lastPrimaryPosition);
						component.velocity = lastPrimaryVelocity;
					}
					hero.inAcid = false;
					hero.cState.inAcid = false;
					hero.cState.swimming = false;
					RepairPrimaryAnimation(hero);
					string[] obj = new string[10] { "WATER restored P1 after remote water warp ", null, null, null, null, null, null, null, null, null };
					val = position;
					obj[1] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
					obj[2] = " -> ";
					val = lastPrimaryPosition;
					obj[3] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
					obj[4] = " state=";
					obj[5] = ((object)Unsafe.As<ActorStates, ActorStates>(ref hero.hero_state)/*cast due to .constrained prefix*/).ToString();
					obj[6] = " clip=";
					obj[7] = text;
					obj[8] = " swimming=";
					obj[9] = swimming.ToString();
					Diagnostics.Write(string.Concat(obj));
					position = lastPrimaryPosition;
				}
			}
		}
		lastPrimaryPosition = position;
		lastPrimaryVelocity = (Object.op_Implicit((Object)(object)component) ? component.velocity : Vector2.zero);
		primaryPositionKnown = true;
	}

	private static bool AtPoolPosition(Vector3 pos, PlayerSlot p)
	{
		Scan();
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return false;
		}
		Bounds val = DuelGround.Body(p);
		float num = ((Bounds)(ref val)).min.y + (pos.y - ((Component)p.Hero).transform.position.y);
		foreach (Surface pool in pools)
		{
			if (Object.op_Implicit((Object)(object)pool.Collider) && ((Behaviour)pool.Collider).enabled)
			{
				Bounds bounds = pool.Collider.bounds;
				if (pos.x > ((Bounds)(ref bounds)).min.x && pos.x < ((Bounds)(ref bounds)).max.x && pos.y > ((Bounds)(ref bounds)).min.y - 0.5f && num < ((Bounds)(ref bounds)).max.y + 0.08f)
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
		Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
		if (!Object.op_Implicit((Object)(object)component) || hero.controlReqlinquished || hero.cState.transitioning)
		{
			Exit(p);
			return;
		}
		if (p.AcidAssistActive && p.Actions != null && !p.InputBlocked && ((OneAxisInputControl)p.Actions.jump).WasPressed)
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
		Vector3 position = ((Component)p.Hero).transform.position;
		Bounds val = DuelGround.Body(p);
		float num = ((Bounds)(ref val)).extents.y - (((Bounds)(ref val)).center.y - position.y);
		float num2 = float.NegativeInfinity;
		foreach (Surface pool in pools)
		{
			if (!Object.op_Implicit((Object)(object)pool.Collider) || !((Behaviour)pool.Collider).enabled || !((Component)pool.Collider).gameObject.activeInHierarchy || (pool.Acid && !hasIsma))
			{
				continue;
			}
			Bounds bounds = pool.Collider.bounds;
			if (!(position.x < ((Bounds)(ref bounds)).min.x + 0.15f) && !(position.x > ((Bounds)(ref bounds)).max.x - 0.15f))
			{
				float num3 = ((Bounds)(ref bounds)).max.y + num - 0.18f;
				if (SwimmingRules.InRange(position.y, num3, p.AcidAssistActive) && ((Bounds)(ref bounds)).max.y > num2)
				{
					num2 = ((Bounds)(ref bounds)).max.y;
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
		floatHeight[p.Index] = num2 + num - 0.18f;
		Float(p);
	}

	internal static void Float(PlayerSlot p)
	{
		//IL_00ab: Invalid comparison between Unknown and I4
		if (p == null || !p.AcidAssistActive || !Object.op_Implicit((Object)(object)p.Hero) || !p.Alive)
		{
			return;
		}
		HeroController hero = p.Hero;
		Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
		if (!Object.op_Implicit((Object)(object)component))
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
		if ((int)hero.hero_state != 3)
		{
			using (PlayerContext.Enter(p))
			{
				Reflect.Call(hero, "SetState", (object)(ActorStates)3);
			}
		}
		float num = floatHeight[p.Index] - component.position.y;
		float num2 = Mathf.Clamp(num * 7f, -5f, 8f);
		if (Mathf.Abs(num) < 0.025f)
		{
			num2 = 0f;
		}
		float num3 = ((p.Actions != null && !p.InputBlocked) ? (((OneAxisInputControl)p.Actions.right).Value - ((OneAxisInputControl)p.Actions.left).Value) : 0f);
		component.velocity = new Vector2(Mathf.Clamp(num3, -1f, 1f) * hero.UNDERWATER_SPEED, num2);
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
		if (Object.op_Implicit((Object)(object)p.Hero))
		{
			p.Hero.inAcid = false;
			p.Hero.cState.inAcid = false;
			p.Hero.cState.swimming = false;
			Reflect.Set(p.Hero, "airDashed", false);
			Reflect.Set(p.Hero, "doubleJumped", false);
			Rigidbody2D component = ((Component)p.Hero).GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.gravityScale = p.Hero.DEFAULT_GRAVITY;
			}
			Diagnostics.Write("WATER leave P" + (p.Index + 1));
		}
	}
}
