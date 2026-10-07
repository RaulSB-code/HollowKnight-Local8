using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using Modding;
using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PvpCombat
{
	internal sealed class Attack
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
				if (!Collider || !Collider.enabled || !Collider.gameObject.activeInHierarchy)
				{
					return PvpFamiliars.Active(vanilla: false, this);
				}
				if ((bool)Slash)
				{
					if (Slash.enabled)
					{
						return PvpFamiliars.Active(Reflect.Get(Slash, "slashing", fallback: false), this);
					}
					return PvpFamiliars.Active(vanilla: false, this);
				}
				if ((bool)Damage)
				{
					if (Damage.enabled)
					{
						return PvpFamiliars.Active(Damage.damageDealt > 0, this);
					}
					return PvpFamiliars.Active(vanilla: false, this);
				}
				if ((bool)Fsm)
				{
					FsmInt fsmInt = Fsm.FsmVariables.FindFsmInt("damageDealt");
					if (Fsm.enabled && fsmInt != null)
					{
						return PvpFamiliars.Active(fsmInt.Value > 0, this);
					}
					return PvpFamiliars.Active(vanilla: false, this);
				}
				return PvpFamiliars.Active(vanilla: false, this);
			}
		}
	}

	internal struct Contact
	{
		internal Attack Attack;

		internal PlayerSlot Target;

		internal int Damage;
	}

	[CompilerGenerated]
	private sealed class __iterator__Loop_d__38 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		private WaitForFixedUpdate __iterator__step_5__2;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		[DebuggerHidden]
		public __iterator__Loop_d__38(int __iterator___1__state)
		{
			this.__iterator___1__state = __iterator___1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			__iterator__step_5__2 = null;
			__iterator___1__state = -2;
		}

		private bool MoveNext()
		{
			int num = __iterator___1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				__iterator___1__state = -1;
				try
				{
					Tick();
				}
				catch (Exception ex)
				{
					Diagnostics.Throttled("PVP", ex);
				}
			}
			else
			{
				__iterator___1__state = -1;
				__iterator__step_5__2 = new WaitForFixedUpdate();
			}
			__iterator___2__current = __iterator__step_5__2;
			__iterator___1__state = 1;
			return true;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	internal static readonly Dictionary<int, Attack> tracked = new Dictionary<int, Attack>();

	private static readonly List<Attack> active = new List<Attack>(64);

	private static readonly List<int> expired = new List<int>();

	private static readonly Contact[] contacts = new Contact[8];

	private static readonly Collider2D[] hurtboxes = new Collider2D[8];

	private static readonly Vector2[] clashAway = new Vector2[8];

	private static readonly float[] nextHit = new float[8];

	private static readonly float[] parryLabel = new float[8];

	private static readonly float[] lastCast = new float[8];

	internal static readonly int[] Kills = new int[8];

	internal static readonly int[] Deaths = new int[8];

	internal static readonly int[] Parries = new int[8];

	internal static readonly int[] Wins = new int[8];

	internal static PlayerSlot Incoming;

	internal static PlayerSlot Victim;

	internal static int incomingMasks;

	private static bool installed;

	private static Material sparkMaterial;

	private static AudioClip parryAudio;

	private static GameObject tinkPrefab;

	internal static CoopSession Session
	{
		get
		{
			if (!(Plugin.Self == null))
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
			if (Local8Mod.Settings.PvpMode > 0 && Plugin.Self != null)
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
			On.NailSlash.StartSlash += SlashStart;
			On.DamageEnemies.FixedUpdate += EnemyDamageTick;
			On.PlayerData.TakeHealth += TakeHealth;
			ModHooks.AfterTakeDamageHook += AfterTakeDamage;
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			On.NailSlash.StartSlash -= SlashStart;
			On.DamageEnemies.FixedUpdate -= EnemyDamageTick;
			On.PlayerData.TakeHealth -= TakeHealth;
			ModHooks.AfterTakeDamageHook -= AfterTakeDamage;
			Reset(scores: true);
			if ((bool)sparkMaterial)
			{
				UnityEngine.Object.Destroy(sparkMaterial);
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

	private static void TakeHealth(On.PlayerData.orig_TakeHealth orig, PlayerData self, int amount)
	{
		if (Incoming != null && Victim != null && PlayerContext.Current == Victim && Session != null && self == Session.Data && amount > 0)
		{
			amount = incomingMasks;
		}
		orig(self, amount);
	}

	private static void SlashStart(On.NailSlash.orig_StartSlash orig, NailSlash self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(self);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
		if (session != null && session.Active && playerSlot != null)
		{
			Track(self.gameObject, playerSlot, reset: false);
			Collider2D component = self.GetComponent<Collider2D>();
			if ((bool)component && tracked.TryGetValue(component.GetInstanceID(), out var value))
			{
				value.Swing.Reset();
				value.WasActive = false;
			}
		}
	}

	private static void EnemyDamageTick(On.DamageEnemies.orig_FixedUpdate orig, DamageEnemies self)
	{
		orig(self);
		if (!Enabled || !self)
		{
			return;
		}
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			return;
		}
		Collider2D component = self.GetComponent<Collider2D>();
		if ((bool)component && !tracked.ContainsKey(component.GetInstanceID()))
		{
			PlayerSlot playerSlot = session.Resolve(self);
			if (playerSlot != null)
			{
				Track(self.gameObject, playerSlot, reset: false);
			}
		}
	}

	internal static void TrackFsm(Fsm fsm)
	{
		if (PvpFamiliars.TrackFsm(fsm) || fsm == null || !fsm.GameObject || fsm.Name != "damages_enemy" || PlayerContext.TargetingEnemy)
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
		owner = PvpFamiliars.Canonical(root, owner);
		if (!root || owner == null)
		{
			PvpFamiliars.Tracked(root, owner, reset);
			return;
		}
		Collider2D[] componentsInChildren = root.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Collider2D collider2D in componentsInChildren)
		{
			NailSlash component = collider2D.GetComponent<NailSlash>();
			DamageEnemies component2 = collider2D.GetComponent<DamageEnemies>();
			PlayMakerFSM playMakerFSM = null;
			PlayMakerFSM[] components = collider2D.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM playMakerFSM2 in components)
			{
				if (playMakerFSM2.FsmName == "damages_enemy")
				{
					playMakerFSM = playMakerFSM2;
					break;
				}
			}
			if (!component && !component2 && !playMakerFSM)
			{
				continue;
			}
			int instanceID = collider2D.GetInstanceID();
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
			value.Collider = collider2D;
			value.Owner = owner;
			value.Slash = component;
			value.Damage = component2;
			value.Fsm = playMakerFSM;
			int num = (component2 ? ((int)component2.attackType) : (((bool)playMakerFSM && playMakerFSM.FsmVariables.FindFsmInt("attackType") != null) ? playMakerFSM.FsmVariables.FindFsmInt("attackType").Value : 0));
			Attack attack = value;
			int kind;
			if (!component)
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
		PvpFamiliars.Tracked(root, owner, reset);
	}

	[IteratorStateMachine(typeof(__iterator__Loop_d__38))]
	internal static IEnumerator Loop()
	{
		return new __iterator__Loop_d__38(0);
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
		if (!hero.takeNoDamage && !hero.cState.shadowDashing && hero.parryInvulnTimer <= 0f && hero.damageMode == DamageMode.FULL_DAMAGE && !hero.cState.invulnerable && !hero.cState.recoiling)
		{
			return !hero.cState.dead;
		}
		return false;
	}

	internal static bool Overlap(Collider2D a, Collider2D b)
	{
		if (!a || !b || !a.enabled || !b.enabled)
		{
			return false;
		}
		Bounds bounds = a.bounds;
		Bounds bounds2 = b.bounds;
		if (bounds.min.x > bounds2.max.x || bounds.max.x < bounds2.min.x || bounds.min.y > bounds2.max.y || bounds.max.y < bounds2.min.y)
		{
			return false;
		}
		ColliderDistance2D colliderDistance2D = a.Distance(b);
		if (colliderDistance2D.isValid)
		{
			return colliderDistance2D.isOverlapped;
		}
		return false;
	}

	private static void Tick()
	{
		PvpFamiliars.BeforeCombat();
		CoopSession session = Session;
		GameManager instance = GameManager.instance;
		if (!Enabled || session == null || !session.Active || !session.Gameplay || session.TeamWipe || Plugin.Self.Panel || !instance || instance.isPaused || !PvpMatch.CanFight)
		{
			return;
		}
		foreach (PlayerSlot player in session.Players)
		{
			if (player.Alive && player.Ready && player.Actions != null && (player.Hero.cState.casting || player.Actions.cast.WasPressed || player.Actions.quickCast.WasPressed))
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
			if (!value.Collider || value.Owner == null || !value.Owner.Hero || !session.Players.Contains(value.Owner))
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
				if (attack.Kind != 0 && attack.Kind != PvpKind.Art)
				{
					continue;
				}
				for (int j = i + 1; j < active.Count; j++)
				{
					Attack attack2 = active[j];
					if ((attack2.Kind != 0 && attack2.Kind != PvpKind.Art) || !Opponents(attack.Owner, attack2.Owner) || !Overlap(attack.Collider, attack2.Collider))
					{
						continue;
					}
					attack.Swing.Parried = (attack2.Swing.Parried = true);
					num |= (1 << attack.Owner.Index) | (1 << attack2.Owner.Index);
					if (attack.Owner.Hero.cState.facingRight != attack2.Owner.Hero.cState.facingRight)
					{
						float x = Mathf.Sign(attack.Owner.Hero.transform.position.x - attack2.Owner.Hero.transform.position.x);
						if (Mathf.Abs(attack.Owner.Hero.transform.position.x - attack2.Owner.Hero.transform.position.x) < 0.05f)
						{
							x = ((attack.Owner.Index < attack2.Owner.Index) ? (-1f) : 1f);
						}
						clashAway[attack.Owner.Index] += new Vector2(x, 0f);
						clashAway[attack2.Owner.Index] -= new Vector2(x, 0f);
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
				Vector2 vector = clashAway[player2.Index];
				if (vector.x < -0.1f)
				{
					player2.Hero.RecoilLeft();
				}
				else if (vector.x > 0.1f)
				{
					player2.Hero.RecoilRight();
				}
			}
			Parries[player2.Index]++;
			parryLabel[player2.Index] = Time.unscaledTime + 0.4f;
			Spark(player2.Hero.transform.position + Vector3.up * 0.2f, player2.Color);
		}
		if (num != 0)
		{
			Vector3 zero = Vector3.zero;
			int num2 = 0;
			foreach (PlayerSlot player3 in session.Players)
			{
				if ((num & (1 << player3.Index)) != 0 && (bool)player3.Hero)
				{
					zero += player3.Hero.transform.position;
					num2++;
				}
			}
			if (num2 > 0)
			{
				PlayParrySound(zero / num2 + Vector3.up * 0.2f);
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
			Collider2D collider2D = hurtboxes[player4.Index];
			if (!collider2D || !collider2D.transform.IsChildOf(player4.Hero.transform))
			{
				HeroBox componentInChildren = player4.Hero.GetComponentInChildren<HeroBox>(includeInactive: true);
				collider2D = (componentInChildren ? componentInChildren.GetComponent<Collider2D>() : null);
				if (!collider2D)
				{
					collider2D = player4.Hero.GetComponent<Collider2D>();
				}
				hurtboxes[player4.Index] = collider2D;
			}
			if (!collider2D || !collider2D.enabled)
			{
				continue;
			}
			foreach (Attack item4 in active)
			{
				if (!item4.Swing.Parried && Opponents(item4.Owner, player4) && Overlap(item4.Collider, collider2D) && item4.Swing.Touch(player4.Index))
				{
					int num3 = Damage(item4.Kind);
					if (contacts[player4.Index].Attack == null || num3 > contacts[player4.Index].Damage)
					{
						contacts[player4.Index] = new Contact
						{
							Attack = item4,
							Target = player4,
							Damage = num3
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
		//Discarded unreachable code: IL_0286
		PlayerSlot playerSlot = null;
		float num = 6.25f;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Alive && player.Ready && Time.unscaledTime - lastCast[player.Index] < 0.6f)
			{
				float sqrMagnitude = (player.Hero.transform.position - a.Collider.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					playerSlot = player;
				}
			}
		}
		if (a.Kind != PvpKind.Spell || !a.Collider || a.Owner == null || !a.Owner.Hero)
		{
			return;
		}
		GameObject gameObject;
		if (playerSlot != null && playerSlot != a.Owner && Time.unscaledTime - lastCast[a.Owner.Index] > 0.6f)
		{
			a.Owner = playerSlot;
			gameObject = a.Collider.gameObject;
			(gameObject.GetComponent<OwnerTag>() ?? gameObject.AddComponent<OwnerTag>()).Player = playerSlot;
			s.RefreshOwnership(gameObject, playerSlot);
			a.Swing.touched &= 536870911;
		}
		gameObject = a.Collider.gameObject;
		Rigidbody2D componentInParent = gameObject.GetComponentInParent<Rigidbody2D>();
		if (!componentInParent || (!componentInParent.gameObject.name.ToLowerInvariant().Contains("fireball") && !componentInParent.gameObject.name.ToLowerInvariant().Contains("vengeful")))
		{
			return;
		}
		int touched = a.Swing.touched;
		float num2;
		if (((uint)touched & 0x20000000u) != 0)
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
		Vector3 localScale = componentInParent.transform.localScale;
		localScale.x = Mathf.Abs(localScale.x) * num2;
		componentInParent.transform.localScale = localScale;
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
		contact = CrystalDashCoop.PvpDamage(contact);
		Attack attack = contact.Attack;
		PlayerSlot target = contact.Target;
		CoopSession session = Session;
		if (!target.Alive || !attack.Collider || session == null)
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
				target.Hero.TakeDamage(attack.Collider.gameObject, (!(attack.Collider.bounds.center.x > target.Hero.transform.position.x)) ? CollisionSide.left : CollisionSide.right, contact.Damage, 1);
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
				float current = (attack.Slash ? Reflect.Get(attack.Slash, "slashAngle", 0f) : 0f);
				if ((bool)attack.Slash && Mathf.Abs(Mathf.DeltaAngle(current, 270f)) < 10f)
				{
					attack.Owner.Hero.Bounce();
				}
			}
		}
		string[] array = new string[12];
		PvpFamiliars.Hit(attack.Collider, attack.Owner);
		array[0] = "PVP HIT P";
		array[1] = (attack.Owner.Index + 1).ToString();
		array[2] = " -> P";
		array[3] = (target.Index + 1).ToString();
		array[4] = " kind=";
		array[5] = attack.Kind.ToString();
		array[6] = " masks=";
		array[7] = contact.Damage.ToString();
		array[8] = " hp=";
		array[9] = num.ToString();
		array[10] = "->";
		array[11] = (target.Vitals.Health + target.Vitals.Blue).ToString();
		Diagnostics.Write(string.Concat(array));
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
		if (!sparkMaterial)
		{
			Shader shader = Shader.Find("Sprites/Default");
			if (!shader)
			{
				return;
			}
			sparkMaterial = new Material(shader);
		}
		position.z = -3f;
		GameObject gameObject = new GameObject("Local8 Parry");
		gameObject.transform.position = position;
		gameObject.AddComponent<PvpSpark>().Init(sparkMaterial, Color.Lerp(color, Color.white, 0.7f));
	}

	private static void PlayParrySound(Vector3 position)
	{
		if (!tinkPrefab)
		{
			TinkEffect[] array = Resources.FindObjectsOfTypeAll<TinkEffect>();
			foreach (TinkEffect tinkEffect in array)
			{
				if ((bool)tinkEffect && (bool)tinkEffect.blockEffect)
				{
					tinkPrefab = tinkEffect.blockEffect;
					break;
				}
			}
		}
		if ((bool)tinkPrefab)
		{
			GameObject gameObject = tinkPrefab.Spawn(position, Quaternion.identity);
			if ((bool)gameObject)
			{
				AudioSource component = gameObject.GetComponent<AudioSource>();
				if ((bool)component)
				{
					component.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				}
				return;
			}
		}
		if (!parryAudio)
		{
			AudioClip[] array2 = Resources.FindObjectsOfTypeAll<AudioClip>();
			foreach (AudioClip audioClip in array2)
			{
				if ((bool)audioClip && audioClip.name.IndexOf("tink", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					parryAudio = audioClip;
					break;
				}
			}
		}
		if ((bool)parryAudio)
		{
			AudioSource.PlayClipAtPoint(parryAudio, position, 0.65f);
		}
	}
}
