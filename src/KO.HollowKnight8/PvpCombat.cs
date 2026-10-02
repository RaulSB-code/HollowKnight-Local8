using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using InControl;
using Modding;
using Modding.Delegates;
using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PvpCombat
{
	private sealed class Attack
	{
		internal Collider2D Collider;

		internal PlayerSlot Owner;

		internal NailSlash Slash;

		internal DamageEnemies Damage;

		internal PlayMakerFSM Fsm;

		internal PvpKind Kind;

		internal bool WasActive;

		internal readonly PvpSwing Swing = new PvpSwing();

		internal bool Active
		{
			get
			{
				if (!Object.op_Implicit((Object)(object)Collider) || !((Behaviour)Collider).enabled || !((Component)Collider).gameObject.activeInHierarchy)
				{
					return false;
				}
				if (Object.op_Implicit((Object)(object)Slash))
				{
					if (((Behaviour)Slash).enabled)
					{
						return Reflect.Get(Slash, "slashing", fallback: false);
					}
					return false;
				}
				if (Object.op_Implicit((Object)(object)Damage))
				{
					if (((Behaviour)Damage).enabled)
					{
						return Damage.damageDealt > 0;
					}
					return false;
				}
				if (Object.op_Implicit((Object)(object)Fsm))
				{
					FsmInt val = Fsm.FsmVariables.FindFsmInt("damageDealt");
					if (((Behaviour)Fsm).enabled && val != null)
					{
						return val.Value > 0;
					}
					return false;
				}
				return false;
			}
		}
	}

	private struct Contact
	{
		internal Attack Attack;

		internal PlayerSlot Target;

		internal int Damage;
	}

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static hook_StartSlash _003C0_003E__SlashStart;

		public static hook_FixedUpdate _003C1_003E__EnemyDamageTick;

		public static hook_TakeHealth _003C2_003E__TakeHealth;

		public static AfterTakeDamageHandler _003C3_003E__AfterTakeDamage;
	}

	private static readonly Dictionary<int, Attack> tracked = new Dictionary<int, Attack>();

	private static readonly List<Attack> active = new List<Attack>(64);

	private static readonly List<int> expired = new List<int>();

	private static readonly Contact[] contacts = new Contact[8];

	private static readonly Collider2D[] hurtboxes = (Collider2D[])(object)new Collider2D[8];

	private static readonly Vector2[] clashAway = (Vector2[])(object)new Vector2[8];

	private static readonly float[] nextHit = new float[8];

	private static readonly float[] parryLabel = new float[8];

	private static readonly float[] lastCast = new float[8];

	internal static readonly int[] Kills = new int[8];

	internal static readonly int[] Deaths = new int[8];

	internal static readonly int[] Parries = new int[8];

	internal static readonly int[] Wins = new int[8];

	internal static PlayerSlot Incoming;

	internal static PlayerSlot Victim;

	private static int incomingMasks;

	private static bool installed;

	private static Material sparkMaterial;

	private static AudioClip parryAudio;

	private static GameObject tinkPrefab;

	internal static CoopSession Session
	{
		get
		{
			if (!((Object)(object)Plugin.Self == (Object)null))
			{
				return Plugin.Self.Session;
			}
			return null;
		}
	}

	internal static bool Enabled
	{
		get
		{
			if (Local8Mod.Settings.PvpMode > 0 && (Object)(object)Plugin.Self != (Object)null)
			{
				return Plugin.Self.Enabled.Value;
			}
			return false;
		}
	}

	internal static bool Applying(PlayerSlot p)
	{
		if (Incoming != null)
		{
			return Victim == p;
		}
		return false;
	}

	internal static int Team(PlayerSlot p)
	{
		return Local8Mod.Settings.PvpTeams[p.Index];
	}

	internal static bool Opponents(PlayerSlot a, PlayerSlot b)
	{
		if (a != null && b != null)
		{
			return PvpRules.Opponents(a.Index, b.Index, Team(a), Team(b));
		}
		return false;
	}

	internal static bool ParryVisible(PlayerSlot p)
	{
		return Time.unscaledTime < parryLabel[p.Index];
	}

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			object obj = _003C_003EO._003C0_003E__SlashStart;
			if (obj == null)
			{
				hook_StartSlash val = SlashStart;
				_003C_003EO._003C0_003E__SlashStart = val;
				obj = (object)val;
			}
			NailSlash.StartSlash += (hook_StartSlash)obj;
			object obj2 = _003C_003EO._003C1_003E__EnemyDamageTick;
			if (obj2 == null)
			{
				hook_FixedUpdate val2 = EnemyDamageTick;
				_003C_003EO._003C1_003E__EnemyDamageTick = val2;
				obj2 = (object)val2;
			}
			DamageEnemies.FixedUpdate += (hook_FixedUpdate)obj2;
			object obj3 = _003C_003EO._003C2_003E__TakeHealth;
			if (obj3 == null)
			{
				hook_TakeHealth val3 = TakeHealth;
				_003C_003EO._003C2_003E__TakeHealth = val3;
				obj3 = (object)val3;
			}
			PlayerData.TakeHealth += (hook_TakeHealth)obj3;
			object obj4 = _003C_003EO._003C3_003E__AfterTakeDamage;
			if (obj4 == null)
			{
				AfterTakeDamageHandler val4 = AfterTakeDamage;
				_003C_003EO._003C3_003E__AfterTakeDamage = val4;
				obj4 = (object)val4;
			}
			ModHooks.AfterTakeDamageHook += (AfterTakeDamageHandler)obj4;
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			object obj = _003C_003EO._003C0_003E__SlashStart;
			if (obj == null)
			{
				hook_StartSlash val = SlashStart;
				_003C_003EO._003C0_003E__SlashStart = val;
				obj = (object)val;
			}
			NailSlash.StartSlash -= (hook_StartSlash)obj;
			object obj2 = _003C_003EO._003C1_003E__EnemyDamageTick;
			if (obj2 == null)
			{
				hook_FixedUpdate val2 = EnemyDamageTick;
				_003C_003EO._003C1_003E__EnemyDamageTick = val2;
				obj2 = (object)val2;
			}
			DamageEnemies.FixedUpdate -= (hook_FixedUpdate)obj2;
			object obj3 = _003C_003EO._003C2_003E__TakeHealth;
			if (obj3 == null)
			{
				hook_TakeHealth val3 = TakeHealth;
				_003C_003EO._003C2_003E__TakeHealth = val3;
				obj3 = (object)val3;
			}
			PlayerData.TakeHealth -= (hook_TakeHealth)obj3;
			object obj4 = _003C_003EO._003C3_003E__AfterTakeDamage;
			if (obj4 == null)
			{
				AfterTakeDamageHandler val4 = AfterTakeDamage;
				_003C_003EO._003C3_003E__AfterTakeDamage = val4;
				obj4 = (object)val4;
			}
			ModHooks.AfterTakeDamageHook -= (AfterTakeDamageHandler)obj4;
			Reset(scores: true);
			if (Object.op_Implicit((Object)(object)sparkMaterial))
			{
				Object.Destroy((Object)(object)sparkMaterial);
			}
			sparkMaterial = null;
			parryAudio = null;
			tinkPrefab = null;
		}
	}

	private static int AfterTakeDamage(int hazard, int amount)
	{
		if (Incoming == null || Victim == null || PlayerContext.Current != Victim || amount <= 0)
		{
			return amount;
		}
		return incomingMasks;
	}

	private static void TakeHealth(orig_TakeHealth orig, PlayerData self, int amount)
	{
		if (Incoming != null && Victim != null && PlayerContext.Current == Victim && Session != null && self == Session.Data && amount > 0)
		{
			amount = incomingMasks;
		}
		orig.Invoke(self, amount);
	}

	private static void SlashStart(orig_StartSlash orig, NailSlash self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(self);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
		if (session != null && session.Active && playerSlot != null)
		{
			Track(((Component)self).gameObject, playerSlot, reset: false);
			Collider2D component = ((Component)self).GetComponent<Collider2D>();
			if (Object.op_Implicit((Object)(object)component) && tracked.TryGetValue(((Object)component).GetInstanceID(), out var value))
			{
				value.Swing.Reset();
				value.WasActive = false;
			}
		}
	}

	private static void EnemyDamageTick(orig_FixedUpdate orig, DamageEnemies self)
	{
		orig.Invoke(self);
		if (!Enabled || !Object.op_Implicit((Object)(object)self))
		{
			return;
		}
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			return;
		}
		Collider2D component = ((Component)self).GetComponent<Collider2D>();
		if (Object.op_Implicit((Object)(object)component) && !tracked.ContainsKey(((Object)component).GetInstanceID()))
		{
			PlayerSlot playerSlot = session.Resolve(self);
			if (playerSlot != null)
			{
				Track(((Component)self).gameObject, playerSlot, reset: false);
			}
		}
	}

	internal static void TrackFsm(Fsm fsm)
	{
		if (fsm == null || !Object.op_Implicit((Object)(object)fsm.GameObject) || fsm.Name != "damages_enemy" || PlayerContext.TargetingEnemy)
		{
			return;
		}
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			return;
		}
		PlayerSlot playerSlot = (PlayerContext.TargetingEnemy ? null : (PlayerContext.Current ?? session.Resolve(fsm)));
		if (playerSlot != null)
		{
			if (session.Resolve(fsm) != playerSlot)
			{
				(fsm.GameObject.GetComponent<OwnerTag>() ?? fsm.GameObject.AddComponent<OwnerTag>()).Player = playerSlot;
				session.RefreshOwnership(fsm.GameObject, playerSlot);
			}
			Track(fsm.GameObject, playerSlot, reset: false);
		}
	}

	internal static void Track(GameObject root, PlayerSlot owner, bool reset)
	{
		if (!Object.op_Implicit((Object)(object)root) || owner == null)
		{
			return;
		}
		Collider2D[] componentsInChildren = root.GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D val in componentsInChildren)
		{
			NailSlash component = ((Component)val).GetComponent<NailSlash>();
			DamageEnemies component2 = ((Component)val).GetComponent<DamageEnemies>();
			PlayMakerFSM val2 = null;
			PlayMakerFSM[] components = ((Component)val).GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM val3 in components)
			{
				if (val3.FsmName == "damages_enemy")
				{
					val2 = val3;
					break;
				}
			}
			if (!Object.op_Implicit((Object)(object)component) && !Object.op_Implicit((Object)(object)component2) && !Object.op_Implicit((Object)(object)val2))
			{
				continue;
			}
			int instanceID = ((Object)val).GetInstanceID();
			if (!tracked.TryGetValue(instanceID, out var value))
			{
				value = new Attack();
				tracked[instanceID] = value;
				reset = true;
			}
			if (value.Owner != owner)
			{
				reset = true;
			}
			value.Collider = val;
			value.Owner = owner;
			value.Slash = component;
			value.Damage = component2;
			value.Fsm = val2;
			int num = ((!Object.op_Implicit((Object)(object)component2)) ? ((Object.op_Implicit((Object)(object)val2) && val2.FsmVariables.FindFsmInt("attackType") != null) ? val2.FsmVariables.FindFsmInt("attackType").Value : 0) : ((int)component2.attackType));
			Attack attack = value;
			int kind;
			if (!Object.op_Implicit((Object)(object)component))
			{
				switch (num)
				{
				default:
					kind = 3;
					break;
				case 2:
				case 7:
					kind = 2;
					break;
				case 0:
					kind = 1;
					break;
				}
			}
			else
			{
				kind = 0;
			}
			attack.Kind = (PvpKind)kind;
			if (reset)
			{
				value.Swing.Reset();
				value.WasActive = false;
			}
		}
	}

	internal static IEnumerator Loop()
	{
		WaitForFixedUpdate step = new WaitForFixedUpdate();
		while (true)
		{
			yield return step;
			try
			{
				Tick();
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("PVP", ex);
			}
		}
	}

	private static bool CanAttack(PlayerSlot p)
	{
		if (p != null && p.Ready && p.Connected && p.Alive && !p.InputBlocked && !p.ArenaTransfer && !p.Vitals.AtBench && Time.time >= p.ProtectionUntil)
		{
			return !p.Hero.cState.transitioning;
		}
		return false;
	}

	private static bool CanReceive(PlayerSlot p)
	{
		if (!CanAttack(p) || Time.time < nextHit[p.Index] || p.Vitals.Invincible)
		{
			return false;
		}
		HeroController hero = p.Hero;
		if (!hero.takeNoDamage && !hero.cState.shadowDashing && hero.parryInvulnTimer <= 0f && (int)hero.damageMode == 0 && !hero.cState.invulnerable && !hero.cState.recoiling)
		{
			return !hero.cState.dead;
		}
		return false;
	}

	private static bool Overlap(Collider2D a, Collider2D b)
	{
		if (!Object.op_Implicit((Object)(object)a) || !Object.op_Implicit((Object)(object)b) || !((Behaviour)a).enabled || !((Behaviour)b).enabled)
		{
			return false;
		}
		Bounds bounds = a.bounds;
		Bounds bounds2 = b.bounds;
		if (((Bounds)(ref bounds)).min.x > ((Bounds)(ref bounds2)).max.x || ((Bounds)(ref bounds)).max.x < ((Bounds)(ref bounds2)).min.x || ((Bounds)(ref bounds)).min.y > ((Bounds)(ref bounds2)).max.y || ((Bounds)(ref bounds)).max.y < ((Bounds)(ref bounds2)).min.y)
		{
			return false;
		}
		ColliderDistance2D val = a.Distance(b);
		if (((ColliderDistance2D)(ref val)).isValid)
		{
			return ((ColliderDistance2D)(ref val)).isOverlapped;
		}
		return false;
	}

	private static void Tick()
	{
		CoopSession session = Session;
		GameManager instance = GameManager.instance;
		if (!Enabled || session == null || !session.Active || !session.Gameplay || session.TeamWipe || Plugin.Self.Panel || !Object.op_Implicit((Object)(object)instance) || instance.isPaused || !PvpMatch.CanFight)
		{
			return;
		}
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Alive && player.Ready && player.Actions != null && (player.Hero.cState.casting || ((OneAxisInputControl)player.Actions.cast).WasPressed || ((OneAxisInputControl)player.Actions.quickCast).WasPressed))
			{
				lastCast[player.Index] = Time.unscaledTime;
			}
		}
		active.Clear();
		expired.Clear();
		Array.Clear(contacts, 0, contacts.Length);
		Array.Clear(clashAway, 0, clashAway.Length);
		foreach (KeyValuePair<int, Attack> item in tracked)
		{
			Attack value = item.Value;
			if (!Object.op_Implicit((Object)(object)value.Collider) || value.Owner == null || !Object.op_Implicit((Object)(object)value.Owner.Hero) || !session.Players.Contains(value.Owner))
			{
				expired.Add(item.Key);
				continue;
			}
			bool flag = value.Active;
			if (flag)
			{
				if (!value.WasActive)
				{
					value.Swing.Reset();
				}
				CorrectSpellOwner(session, value);
			}
			value.WasActive = flag;
			if (flag && !value.Swing.Parried && CanAttack(value.Owner) && session.Resolve(value.Collider) == value.Owner && (value.Kind != PvpKind.Charm || Local8Mod.Settings.PvpCharmAttacks))
			{
				active.Add(value);
			}
		}
		foreach (int item2 in expired)
		{
			tracked.Remove(item2);
		}
		int num = 0;
		if (Local8Mod.Settings.PvpParry)
		{
			for (int i = 0; i < active.Count; i++)
			{
				Attack attack = active[i];
				if (attack.Kind != PvpKind.Nail && attack.Kind != PvpKind.Art)
				{
					continue;
				}
				for (int j = i + 1; j < active.Count; j++)
				{
					Attack attack2 = active[j];
					if ((attack2.Kind != PvpKind.Nail && attack2.Kind != PvpKind.Art) || !Opponents(attack.Owner, attack2.Owner) || !Overlap(attack.Collider, attack2.Collider))
					{
						continue;
					}
					attack.Swing.Parried = (attack2.Swing.Parried = true);
					num |= (1 << attack.Owner.Index) | (1 << attack2.Owner.Index);
					if (attack.Owner.Hero.cState.facingRight != attack2.Owner.Hero.cState.facingRight)
					{
						float num2 = Mathf.Sign(((Component)attack.Owner.Hero).transform.position.x - ((Component)attack2.Owner.Hero).transform.position.x);
						if (Mathf.Abs(((Component)attack.Owner.Hero).transform.position.x - ((Component)attack2.Owner.Hero).transform.position.x) < 0.05f)
						{
							num2 = ((attack.Owner.Index < attack2.Owner.Index) ? (-1f) : 1f);
						}
						ref Vector2 reference = ref clashAway[attack.Owner.Index];
						reference += new Vector2(num2, 0f);
						ref Vector2 reference2 = ref clashAway[attack2.Owner.Index];
						reference2 -= new Vector2(num2, 0f);
					}
				}
			}
		}
		foreach (PlayerSlot player2 in session.Players)
		{
			if ((num & (1 << player2.Index)) == 0)
			{
				continue;
			}
			foreach (Attack item3 in active)
			{
				if (item3.Owner == player2 && (item3.Kind == PvpKind.Nail || item3.Kind == PvpKind.Art))
				{
					item3.Swing.Parried = true;
				}
			}
			using (PlayerContext.Enter(player2))
			{
				player2.Hero.NailParry();
				player2.Hero.NailParryRecover();
				Vector2 val = clashAway[player2.Index];
				if (val.x < -0.1f)
				{
					player2.Hero.RecoilLeft();
				}
				else if (val.x > 0.1f)
				{
					player2.Hero.RecoilRight();
				}
			}
			Parries[player2.Index]++;
			parryLabel[player2.Index] = Time.unscaledTime + 0.4f;
			Spark(((Component)player2.Hero).transform.position + Vector3.up * 0.2f, player2.Color);
		}
		if (num != 0)
		{
			Vector3 val2 = Vector3.zero;
			int num3 = 0;
			foreach (PlayerSlot player3 in session.Players)
			{
				if ((num & (1 << player3.Index)) != 0 && Object.op_Implicit((Object)(object)player3.Hero))
				{
					val2 += ((Component)player3.Hero).transform.position;
					num3++;
				}
			}
			if (num3 > 0)
			{
				PlayParrySound(val2 / (float)num3 + Vector3.up * 0.2f);
			}
			CombatEffects.MarkCombat();
			Diagnostics.Write("PVP PARRY players=" + num);
		}
		foreach (PlayerSlot player4 in session.Players)
		{
			if (!CanReceive(player4))
			{
				continue;
			}
			Collider2D val3 = hurtboxes[player4.Index];
			if (!Object.op_Implicit((Object)(object)val3) || !((Component)val3).transform.IsChildOf(((Component)player4.Hero).transform))
			{
				HeroBox componentInChildren = ((Component)player4.Hero).GetComponentInChildren<HeroBox>(true);
				val3 = (Object.op_Implicit((Object)(object)componentInChildren) ? ((Component)componentInChildren).GetComponent<Collider2D>() : null);
				if (!Object.op_Implicit((Object)(object)val3))
				{
					val3 = ((Component)player4.Hero).GetComponent<Collider2D>();
				}
				hurtboxes[player4.Index] = val3;
			}
			if (!Object.op_Implicit((Object)(object)val3) || !((Behaviour)val3).enabled)
			{
				continue;
			}
			foreach (Attack item4 in active)
			{
				if (!item4.Swing.Parried && Opponents(item4.Owner, player4) && Overlap(item4.Collider, val3) && item4.Swing.Touch(player4.Index))
				{
					int num4 = Damage(item4.Kind);
					if (contacts[player4.Index].Attack == null || num4 > contacts[player4.Index].Damage)
					{
						contacts[player4.Index] = new Contact
						{
							Attack = item4,
							Target = player4,
							Damage = num4
						};
					}
				}
			}
		}
		for (int k = 0; k < contacts.Length; k++)
		{
			if (contacts[k].Attack != null)
			{
				Apply(contacts[k]);
			}
		}
	}

	private static void CorrectSpellOwner(CoopSession s, Attack a)
	{
		PlayerSlot playerSlot = null;
		float num = 6.25f;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Alive && player.Ready && Time.unscaledTime - lastCast[player.Index] < 0.6f)
			{
				Vector3 val = ((Component)player.Hero).transform.position - ((Component)a.Collider).transform.position;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					playerSlot = player;
				}
			}
		}
		if (a.Kind != PvpKind.Spell || !Object.op_Implicit((Object)(object)a.Collider) || a.Owner == null || !Object.op_Implicit((Object)(object)a.Owner.Hero))
		{
			return;
		}
		GameObject gameObject;
		if (playerSlot != null && playerSlot != a.Owner && Time.unscaledTime - lastCast[a.Owner.Index] > 0.6f)
		{
			a.Owner = playerSlot;
			gameObject = ((Component)a.Collider).gameObject;
			(gameObject.GetComponent<OwnerTag>() ?? gameObject.AddComponent<OwnerTag>()).Player = playerSlot;
			s.RefreshOwnership(gameObject, playerSlot);
			a.Swing.touched &= 536870911;
		}
		gameObject = ((Component)a.Collider).gameObject;
		Rigidbody2D componentInParent = gameObject.GetComponentInParent<Rigidbody2D>();
		if (!Object.op_Implicit((Object)(object)componentInParent) || (!((Object)((Component)componentInParent).gameObject).name.ToLowerInvariant().Contains("fireball") && !((Object)((Component)componentInParent).gameObject).name.ToLowerInvariant().Contains("vengeful")))
		{
			return;
		}
		int touched = a.Swing.touched;
		float num2;
		if ((touched & 0x20000000) != 0)
		{
			num2 = (((touched & 0x40000000) == 0) ? (-1f) : 1f);
		}
		else
		{
			if (a.Owner.Hero.cState.facingRight)
			{
				num2 = 1f;
				touched |= 0x60000000;
			}
			else
			{
				num2 = -1f;
				touched |= 0x20000000;
			}
			a.Swing.touched = touched;
		}
		componentInParent.velocity = new Vector2(Mathf.Abs(componentInParent.velocity.x) * num2, componentInParent.velocity.y);
		Vector3 localScale = ((Component)componentInParent).transform.localScale;
		localScale.x = Mathf.Abs(localScale.x) * num2;
		((Component)componentInParent).transform.localScale = localScale;
	}

	private static int Damage(PvpKind kind)
	{
		Local8Settings settings = Local8Mod.Settings;
		return kind switch
		{
			PvpKind.Spell => settings.PvpSpellDamage, 
			PvpKind.Art => settings.PvpArtDamage, 
			PvpKind.Nail => settings.PvpNailDamage, 
			_ => settings.PvpCharmDamage, 
		};
	}

	private static void Apply(Contact contact)
	{
		Attack attack = contact.Attack;
		PlayerSlot target = contact.Target;
		CoopSession session = Session;
		if (!target.Alive || !Object.op_Implicit((Object)(object)attack.Collider) || session == null)
		{
			return;
		}
		int num = target.Vitals.Health + target.Vitals.Blue;
		PlayerSlot incoming = Incoming;
		PlayerSlot victim = Victim;
		int num2 = incomingMasks;
		Incoming = attack.Owner;
		Victim = target;
		incomingMasks = contact.Damage;
		try
		{
			using (PlayerContext.Enter(target))
			{
				HeroController hero = target.Hero;
				GameObject gameObject = ((Component)attack.Collider).gameObject;
				Bounds bounds = attack.Collider.bounds;
				hero.TakeDamage(gameObject, (CollisionSide)((!(((Bounds)(ref bounds)).center.x > ((Component)target.Hero).transform.position.x)) ? 1 : 2), contact.Damage, 1);
			}
		}
		finally
		{
			Incoming = incoming;
			Victim = victim;
			incomingMasks = num2;
		}
		if (target.Vitals.Health + target.Vitals.Blue >= num && !target.Down)
		{
			return;
		}
		nextHit[target.Index] = Time.time + 0.85f;
		Revival.End(target);
		target.DamageFlashUntil = Time.unscaledTime + 0.4f;
		CombatEffects.MarkCombat();
		if (attack.Owner.Alive && (attack.Kind == PvpKind.Nail || attack.Kind == PvpKind.Art))
		{
			using (PlayerContext.Enter(attack.Owner))
			{
				if (Local8Mod.Settings.PvpSoulOnHit)
				{
					attack.Owner.Hero.SoulGain();
				}
				float num3 = (Object.op_Implicit((Object)(object)attack.Slash) ? Reflect.Get(attack.Slash, "slashAngle", 0f) : 0f);
				if (Object.op_Implicit((Object)(object)attack.Slash) && Mathf.Abs(Mathf.DeltaAngle(num3, 270f)) < 10f)
				{
					attack.Owner.Hero.Bounce();
				}
			}
		}
		Diagnostics.Write("PVP HIT P" + (attack.Owner.Index + 1) + " -> P" + (target.Index + 1) + " kind=" + attack.Kind.ToString() + " masks=" + contact.Damage + " hp=" + num + "->" + (target.Vitals.Health + target.Vitals.Blue));
	}

	internal static void Down(PlayerSlot p)
	{
		if (Enabled)
		{
			Deaths[p.Index]++;
			if (Incoming != null && Victim == p && Opponents(Incoming, p))
			{
				Kills[Incoming.Index]++;
			}
		}
	}

	internal static void Forget(PlayerSlot p)
	{
		hurtboxes[p.Index] = null;
		nextHit[p.Index] = (parryLabel[p.Index] = 0f);
		Kills[p.Index] = (Deaths[p.Index] = (Parries[p.Index] = (Wins[p.Index] = 0)));
	}

	internal static void Recovered(PlayerSlot p)
	{
		foreach (Attack value in tracked.Values)
		{
			if (value.Owner == p)
			{
				value.Swing.Parried = true;
				value.WasActive = value.Active;
			}
		}
		hurtboxes[p.Index] = null;
		nextHit[p.Index] = 0f;
	}

	internal static void Invalidate()
	{
		foreach (Attack value in tracked.Values)
		{
			value.Swing.Parried = true;
			value.WasActive = value.Active;
		}
		Array.Clear(nextHit, 0, 8);
	}

	internal static void Reset(bool scores)
	{
		tracked.Clear();
		active.Clear();
		expired.Clear();
		Array.Clear(hurtboxes, 0, 8);
		Array.Clear(nextHit, 0, 8);
		Array.Clear(parryLabel, 0, 8);
		Array.Clear(lastCast, 0, 8);
		Incoming = (Victim = null);
		if (scores)
		{
			ResetScores();
		}
	}

	internal static void ResetScores()
	{
		Array.Clear(Kills, 0, 8);
		Array.Clear(Deaths, 0, 8);
		Array.Clear(Parries, 0, 8);
		Array.Clear(Wins, 0, 8);
	}

	private static void Spark(Vector3 position, Color color)
	{
		if (!Object.op_Implicit((Object)(object)sparkMaterial))
		{
			Shader val = Shader.Find("Sprites/Default");
			if (!Object.op_Implicit((Object)(object)val))
			{
				return;
			}
			sparkMaterial = new Material(val);
		}
		position.z = -3f;
		GameObject val2 = new GameObject("Local8 Parry");
		val2.transform.position = position;
		val2.AddComponent<PvpSpark>().Init(sparkMaterial, Color.Lerp(color, Color.white, 0.7f));
	}

	private static void PlayParrySound(Vector3 position)
	{
		if (!Object.op_Implicit((Object)(object)tinkPrefab))
		{
			TinkEffect[] array = Resources.FindObjectsOfTypeAll<TinkEffect>();
			foreach (TinkEffect val in array)
			{
				if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val.blockEffect))
				{
					tinkPrefab = val.blockEffect;
					break;
				}
			}
		}
		if (Object.op_Implicit((Object)(object)tinkPrefab))
		{
			GameObject val2 = ObjectPoolExtensions.Spawn(tinkPrefab, position, Quaternion.identity);
			if (Object.op_Implicit((Object)(object)val2))
			{
				AudioSource component = val2.GetComponent<AudioSource>();
				if (Object.op_Implicit((Object)(object)component))
				{
					component.pitch = Random.Range(0.9f, 1.1f);
				}
				return;
			}
		}
		if (!Object.op_Implicit((Object)(object)parryAudio))
		{
			AudioClip[] array2 = Resources.FindObjectsOfTypeAll<AudioClip>();
			foreach (AudioClip val3 in array2)
			{
				if (Object.op_Implicit((Object)(object)val3) && ((Object)val3).name.IndexOf("tink", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					parryAudio = val3;
					break;
				}
			}
		}
		if (Object.op_Implicit((Object)(object)parryAudio))
		{
			AudioSource.PlayClipAtPoint(parryAudio, position, 0.65f);
		}
	}
}
