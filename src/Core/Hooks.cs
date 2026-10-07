using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using On;
using On.HutongGames.PlayMaker;
using On.HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Hooks
{
	private sealed class EnemyBinding
	{
		internal HealthManager Health;

		internal DamageHero Damage;
	}

	private sealed class FsmBinding
	{
		internal PlayerSlot Owner;

		internal HutongGames.PlayMaker.FsmState State;
	}

	private sealed class SaveScope : IDisposable
	{
		private readonly PlayerContext context;

		private readonly PlayerData data;

		private readonly int health;

		private readonly int blue;

		private readonly int soul;

		private readonly int reserve;

		internal SaveScope(CoopSession s)
		{
			context = PlayerContext.Enter(s.Primary);
			data = s.Data;
			health = data.health;
			blue = data.healthBlue;
			soul = data.MPCharge;
			reserve = data.MPReserve;
			PvpMatch.Before before = PvpMatch.Saved(s.Primary);
			if (before != null)
			{
				data.health = Math.Max(1, before.Health);
				data.healthBlue = before.Blue;
				data.MPCharge = before.Soul;
				data.MPReserve = before.Reserve;
			}
			else if (s.Primary.Down)
			{
				data.health = Math.Max(1, health);
			}
		}

		public void Dispose()
		{
			data.health = health;
			data.healthBlue = blue;
			data.MPCharge = soul;
			data.MPReserve = reserve;
			if (context != null)
			{
				context.Dispose();
			}
		}
	}

	[CompilerGenerated]
	private sealed class __iterator__Empty_d__134 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

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
		public __iterator__Empty_d__134(int __iterator___1__state)
		{
			this.__iterator___1__state = __iterator___1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			__iterator___1__state = -2;
		}

		private bool MoveNext()
		{
			if (__iterator___1__state != 0)
			{
				return false;
			}
			__iterator___1__state = -1;
			return false;
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

	private static bool installed;

	private static int lastDamageCount = -1;

	private static readonly HashSet<string> familiarDamageLogged = new HashSet<string>();

	private static bool sharingRoar;

	private static int roarEnterFrame = -1;

	private static int roarExitFrame = -1;

	private static float lastPauseModalAt;

	private static bool pauseRecoveryCandidate;

	private static readonly Dictionary<Type, FieldInfo[]> referenceFields = new Dictionary<Type, FieldInfo[]>();

	private static readonly Dictionary<HutongGames.PlayMaker.Fsm, EnemyBinding> enemies = new Dictionary<HutongGames.PlayMaker.Fsm, EnemyBinding>();

	private static readonly Dictionary<HutongGames.PlayMaker.Fsm, FsmBinding> fsmBindings = new Dictionary<HutongGames.PlayMaker.Fsm, FsmBinding>();

	private static readonly Dictionary<PlayerSlot, HashSet<WalkArea>> walkAreas = new Dictionary<PlayerSlot, HashSet<WalkArea>>();

	private static readonly HashSet<string> localEvents = new HashSet<string> { "HERO DAMAGED", "HERO HEALED", "HERO LANDED", "HERO DEATH", "HERO DEAD", "FOCUS COMPLETED", "HERO RECOIL", "HERO DASH" };

	private static CoopSession Session
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

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			DialogueCleanup.Install();
			CharmNativeUi.Install();
			CombatEffects.Install();
			PlayerLighting.Install();
			PvpCombat.Install();
			VanillaHud.Install();
			SkinBridge.Install();
			On.InvCharmBackboard.SelectCharm += SelectBoard;
			On.CharmItem.GetListNumber += SelectEquipped;
			On.HeroController.FinishedEnteringScene += Entered;
			On.HeroController.OnCollisionEnter2D += CollisionEnter;
			On.HeroController.OnCollisionStay2D += CollisionStay;
			On.HeroController.OnCollisionExit2D += CollisionExit;
			On.HazardRespawnTrigger.OnTriggerEnter2D += HazardMarker;
			On.HeroBox.Start += BoxStart;
			On.HeroBox.OnTriggerEnter2D += BoxEnter;
			On.HeroBox.OnTriggerStay2D += BoxStay;
			On.HeroBox.LateUpdate += BoxLate;
			On.HeroController.AddMPCharge += AddSoul;
			On.HeroController.SoulGain += GainSoul;
			On.HeroController.TryAddMPChargeSpa += SpaSoul;
			On.HeroController.SetMPCharge += SetSoul;
			On.HeroController.TakeReserveMP += TakeReserve;
			On.HeroController.AddHealth += AddHealth;
			On.HeroController.StartRecoil += Recoil;
			On.HeroController.TakeMP += TakeMP;
			On.HeroController.TakeMPQuick += TakeMPQuick;
			On.RestBench.OnTriggerEnter2D += BenchEnter;
			On.RestBench.OnTriggerExit2D += BenchExit;
			On.WalkArea.OnTriggerEnter2D += WalkEnter;
			On.WalkArea.OnTriggerStay2D += WalkStay;
			On.WalkArea.OnTriggerExit2D += WalkExit;
			On.TriggerEnterEvent.OnTriggerEnter2D += WorldTriggerEnter;
			On.TriggerEnterEvent.OnTriggerStay2D += WorldTriggerStay;
			On.TriggerEnterEvent.OnTriggerExit2D += WorldTriggerExit;
			On.HeroController.Awake += HeroAwake;
			On.HeroController.Start += HeroStart;
			On.HeroController.Update += HeroUpdate;
			On.HeroController.FixedUpdate += HeroFixed;
			On.HeroController.IsSwimming += IsSwimming;
			On.HeroController.SetBackOnGround += SetBackOnGround;
			On.HeroController.BackOnGround += BackOnGround;
			On.HeroAnimationController.Update += AnimationUpdate;
			On.HeroController.TakeDamage += Damage;
			On.HeroController.CharmUpdate += CharmUpdate;
			On.HeroController.MaxHealth += MaxHealth;
			On.PlayerData.SetInt += SetInt;
			On.PlayerData.IntAdd += IntAdd;
			On.PlayerData.IncrementInt += IncrementInt;
			On.PlayerData.SetBool += SetBool;
			On.PlayerData.GetBool += GetBool;
			On.HeroController.Die += Die;
			On.HeroController.DieFromHazard += Hazard;
			On.HealthManager.Hit += Hit;
			On.HealthManager.Die += EnemyDie;
			On.CameraController.LateUpdate += CameraLate;
			On.AudioManager.ApplyMusicCue += MusicCueChange;
			On.TransitionPoint.OnTriggerEnter2D += TransitionEnter;
			On.TransitionPoint.OnTriggerStay2D += TransitionStay;
			On.CameraLockArea.OnTriggerEnter2D += CameraEnter;
			On.CameraLockArea.OnTriggerStay2D += CameraStay;
			On.CameraLockArea.OnTriggerExit2D += CameraExit;
			On.GameManager.BeginSceneTransition += BeginTransition;
			On.GameManager.LoadScene += LoadScene;
			On.GameManager.PauseGameToggle += PauseToggle;
			On.GameManager.PlayerDead += PlayerDead;
			On.GameManager.PlayerDeadFromHazard += PlayerDeadHazard;
			On.GameManager.SaveGame += SaveGame;
			On.InputHandler.Update += InputUpdate;
			On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion += PoolSpawn;
			On.LineOfSightDetector.Update += EnemyUpdate;
			On.KnightHatchling.FixedUpdate += HatchlingFixed;
			On.KnightHatchling.Spawn += HatchlingSpawn;
			On.KnightHatchling.TeleEnd += HatchlingTeleEnd;
			On.SpellGetOrb.OnEnable += SpellOrb;
			On.HutongGames.PlayMaker.Fsm.Awake += FsmAwake;
			On.HutongGames.PlayMaker.Fsm.OnEnable += FsmEnable;
			On.HutongGames.PlayMaker.Fsm.Start += FsmStart;
			On.HutongGames.PlayMaker.FsmState.OnEnter += StateEnter;
			On.HutongGames.PlayMaker.Fsm.Update += FsmUpdate;
			On.HutongGames.PlayMaker.Fsm.FixedUpdate += FsmFixed;
			On.HutongGames.PlayMaker.Fsm.LateUpdate += FsmLate;
			On.HutongGames.PlayMaker.Fsm.ProcessEvent += FsmEvent;
			On.HutongGames.PlayMaker.Fsm.OnTriggerEnter2D += FsmTriggerEnter;
			On.HutongGames.PlayMaker.Fsm.OnTriggerStay2D += FsmTriggerStay;
			On.HutongGames.PlayMaker.Fsm.OnTriggerExit2D += FsmTriggerExit;
			On.HutongGames.PlayMaker.Fsm.OnCollisionEnter2D += WaterCollisionEnter;
			On.HutongGames.PlayMaker.Fsm.OnCollisionStay2D += WaterCollisionStay;
			On.HutongGames.PlayMaker.Fsm.OnCollisionExit2D += WaterCollisionExit;
			On.HutongGames.PlayMaker.Actions.ListenForUp.CheckForInput += ListenUp;
			On.HutongGames.PlayMaker.Actions.ListenForDown.CheckForInput += ListenDown;
			On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter += InteractionCall;
			On.HutongGames.PlayMaker.Actions.CreateUIMsgGetItem.OnEnter += CreatePickupCard;
			On.HutongGames.PlayMaker.Actions.ListenForJump.OnUpdate += PickupJump;
			On.HutongGames.PlayMaker.Actions.ListenForMenuActions.OnUpdate += PickupMenuActions;
			On.HutongGames.PlayMaker.Actions.ListenForMenuSubmit.OnUpdate += PickupMenuSubmit;
			On.HutongGames.PlayMaker.Actions.ListenForMenuCancel.OnUpdate += PickupMenuCancel;
			On.HutongGames.PlayMaker.Actions.ListenForQuickMap.OnUpdate += QuickMapUpdate;
			On.GameMap.Update += MapUpdate;
			On.HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool.OnEnter += SpellPoolAction;
			On.HutongGames.PlayMaker.Actions.CreateObject.OnEnter += SpellCreateAction;
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			ClearSceneCache();
			DialogueCleanup.Uninstall();
			CharmNativeUi.Uninstall();
			CombatEffects.Uninstall();
			PlayerLighting.Uninstall();
			PvpCombat.Uninstall();
			VanillaHud.Uninstall();
			SkinBridge.Uninstall();
			On.InvCharmBackboard.SelectCharm -= SelectBoard;
			On.CharmItem.GetListNumber -= SelectEquipped;
			On.HeroController.FinishedEnteringScene -= Entered;
			On.HeroController.OnCollisionEnter2D -= CollisionEnter;
			On.HeroController.OnCollisionStay2D -= CollisionStay;
			On.HeroController.OnCollisionExit2D -= CollisionExit;
			On.HazardRespawnTrigger.OnTriggerEnter2D -= HazardMarker;
			On.HeroBox.Start -= BoxStart;
			On.HeroBox.OnTriggerEnter2D -= BoxEnter;
			On.HeroBox.OnTriggerStay2D -= BoxStay;
			On.HeroBox.LateUpdate -= BoxLate;
			On.HeroController.AddMPCharge -= AddSoul;
			On.HeroController.SoulGain -= GainSoul;
			On.HeroController.TryAddMPChargeSpa -= SpaSoul;
			On.HeroController.SetMPCharge -= SetSoul;
			On.HeroController.TakeReserveMP -= TakeReserve;
			On.HeroController.AddHealth -= AddHealth;
			On.HeroController.StartRecoil -= Recoil;
			On.HeroController.TakeMP -= TakeMP;
			On.HeroController.TakeMPQuick -= TakeMPQuick;
			On.RestBench.OnTriggerEnter2D -= BenchEnter;
			On.RestBench.OnTriggerExit2D -= BenchExit;
			On.WalkArea.OnTriggerEnter2D -= WalkEnter;
			On.WalkArea.OnTriggerStay2D -= WalkStay;
			On.WalkArea.OnTriggerExit2D -= WalkExit;
			On.TriggerEnterEvent.OnTriggerEnter2D -= WorldTriggerEnter;
			On.TriggerEnterEvent.OnTriggerStay2D -= WorldTriggerStay;
			On.TriggerEnterEvent.OnTriggerExit2D -= WorldTriggerExit;
			On.HeroController.Awake -= HeroAwake;
			On.HeroController.Start -= HeroStart;
			On.HeroController.Update -= HeroUpdate;
			On.HeroController.FixedUpdate -= HeroFixed;
			On.HeroController.IsSwimming -= IsSwimming;
			On.HeroController.SetBackOnGround -= SetBackOnGround;
			On.HeroController.BackOnGround -= BackOnGround;
			On.HeroAnimationController.Update -= AnimationUpdate;
			On.HeroController.TakeDamage -= Damage;
			On.HeroController.CharmUpdate -= CharmUpdate;
			On.HeroController.MaxHealth -= MaxHealth;
			On.HeroController.Die -= Die;
			On.HeroController.DieFromHazard -= Hazard;
			On.PlayerData.SetInt -= SetInt;
			On.PlayerData.IntAdd -= IntAdd;
			On.PlayerData.IncrementInt -= IncrementInt;
			On.PlayerData.SetBool -= SetBool;
			On.PlayerData.GetBool -= GetBool;
			On.HealthManager.Hit -= Hit;
			On.HealthManager.Die -= EnemyDie;
			On.CameraController.LateUpdate -= CameraLate;
			On.AudioManager.ApplyMusicCue -= MusicCueChange;
			On.TransitionPoint.OnTriggerEnter2D -= TransitionEnter;
			On.TransitionPoint.OnTriggerStay2D -= TransitionStay;
			On.CameraLockArea.OnTriggerEnter2D -= CameraEnter;
			On.CameraLockArea.OnTriggerStay2D -= CameraStay;
			On.CameraLockArea.OnTriggerExit2D -= CameraExit;
			On.GameManager.BeginSceneTransition -= BeginTransition;
			On.GameManager.LoadScene -= LoadScene;
			On.GameManager.PauseGameToggle -= PauseToggle;
			On.GameManager.PlayerDead -= PlayerDead;
			On.GameManager.PlayerDeadFromHazard -= PlayerDeadHazard;
			On.GameManager.SaveGame -= SaveGame;
			On.InputHandler.Update -= InputUpdate;
			On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion -= PoolSpawn;
			On.LineOfSightDetector.Update -= EnemyUpdate;
			On.KnightHatchling.FixedUpdate -= HatchlingFixed;
			On.KnightHatchling.Spawn -= HatchlingSpawn;
			On.KnightHatchling.TeleEnd -= HatchlingTeleEnd;
			On.SpellGetOrb.OnEnable -= SpellOrb;
			On.HutongGames.PlayMaker.Fsm.Awake -= FsmAwake;
			On.HutongGames.PlayMaker.Fsm.OnEnable -= FsmEnable;
			On.HutongGames.PlayMaker.Fsm.Start -= FsmStart;
			On.HutongGames.PlayMaker.FsmState.OnEnter -= StateEnter;
			On.HutongGames.PlayMaker.Fsm.Update -= FsmUpdate;
			On.HutongGames.PlayMaker.Fsm.FixedUpdate -= FsmFixed;
			On.HutongGames.PlayMaker.Fsm.LateUpdate -= FsmLate;
			On.HutongGames.PlayMaker.Fsm.ProcessEvent -= FsmEvent;
			On.HutongGames.PlayMaker.Fsm.OnTriggerEnter2D -= FsmTriggerEnter;
			On.HutongGames.PlayMaker.Fsm.OnTriggerStay2D -= FsmTriggerStay;
			On.HutongGames.PlayMaker.Fsm.OnTriggerExit2D -= FsmTriggerExit;
			On.HutongGames.PlayMaker.Fsm.OnCollisionEnter2D -= WaterCollisionEnter;
			On.HutongGames.PlayMaker.Fsm.OnCollisionStay2D -= WaterCollisionStay;
			On.HutongGames.PlayMaker.Fsm.OnCollisionExit2D -= WaterCollisionExit;
			On.HutongGames.PlayMaker.Actions.ListenForUp.CheckForInput -= ListenUp;
			On.HutongGames.PlayMaker.Actions.ListenForDown.CheckForInput -= ListenDown;
			On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter -= InteractionCall;
			On.HutongGames.PlayMaker.Actions.CreateUIMsgGetItem.OnEnter -= CreatePickupCard;
			On.HutongGames.PlayMaker.Actions.ListenForJump.OnUpdate -= PickupJump;
			On.HutongGames.PlayMaker.Actions.ListenForMenuActions.OnUpdate -= PickupMenuActions;
			On.HutongGames.PlayMaker.Actions.ListenForMenuSubmit.OnUpdate -= PickupMenuSubmit;
			On.HutongGames.PlayMaker.Actions.ListenForMenuCancel.OnUpdate -= PickupMenuCancel;
			On.HutongGames.PlayMaker.Actions.ListenForQuickMap.OnUpdate -= QuickMapUpdate;
			On.GameMap.Update -= MapUpdate;
			On.HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool.OnEnter -= SpellPoolAction;
			On.HutongGames.PlayMaker.Actions.CreateObject.OnEnter -= SpellCreateAction;
		}
	}

	private static PlayerSlot HeroPlayer(HeroController hero)
	{
		CoopSession session = Session;
		if (session == null)
		{
			return null;
		}
		PlayerSlot playerSlot = session.Resolve(hero);
		if (playerSlot == null)
		{
			if (session.Cloning == null || !(hero != session.Primary.Hero))
			{
				return null;
			}
			playerSlot = session.Cloning;
		}
		return playerSlot;
	}

	private static void HeroAwake(On.HeroController.orig_Awake orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self);
		}
	}

	private static void HeroStart(On.HeroController.orig_Start orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self);
		}
	}

	private static void HeroUpdate(On.HeroController.orig_Update orig, HeroController self)
	{
		if (!HeroFrame(self, out var p))
		{
			LiquidPolish.AfterHero(self);
			return;
		}
		AcidSwimming.PrimaryState before = ((p != null && p.Index > 0 && p.AcidAssistActive) ? AcidSwimming.CapturePrimary() : default(AcidSwimming.PrimaryState));
		using (PlayerContext.Enter(p))
		{
			orig(self);
		}
		AcidSwimming.Float(p);
		AcidSwimming.RestorePrimary(before, "hero update");
		if (before.Valid)
		{
			AcidSwimming.GuardPrimary();
		}
		LiquidPolish.AfterHero(self);
	}

	private static void HeroFixed(On.HeroController.orig_FixedUpdate orig, HeroController self)
	{
		if (HeroFrame(self, out var p))
		{
			AcidSwimming.PrimaryState before = ((p != null && p.Index > 0 && p.AcidAssistActive) ? AcidSwimming.CapturePrimary() : default(AcidSwimming.PrimaryState));
			using (PlayerContext.Enter(p))
			{
				orig(self);
			}
			AcidSwimming.Float(p);
			AcidSwimming.RestorePrimary(before, "hero physics");
			if (before.Valid)
			{
				AcidSwimming.GuardPrimary();
			}
		}
	}

	private static void IsSwimming(On.HeroController.orig_IsSwimming orig, HeroController self)
	{
		if (AcidSwimming.WrongPrimarySwim(self))
		{
			AcidSwimming.GuardPrimary();
			return;
		}
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot == null || playerSlot.Index <= 0 || !playerSlot.AcidAssistActive || AcidSwimming.HasSwimClip(playerSlot))
		{
			orig(self);
		}
	}

	private static void SetBackOnGround(On.HeroController.orig_SetBackOnGround orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && playerSlot.AcidAssistActive)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
	}

	private static void BackOnGround(On.HeroController.orig_BackOnGround orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && playerSlot.AcidAssistActive)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
	}

	private static void AnimationUpdate(On.HeroAnimationController.orig_Update orig, HeroAnimationController self)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(self));
		if (playerSlot != null && playerSlot.Index == 0)
		{
			AcidSwimming.GuardPrimary();
		}
		if (playerSlot != null && AcidSwimming.SurfacePose(playerSlot))
		{
			return;
		}
		if (playerSlot != null && playerSlot.Index > 0 && playerSlot.AcidAssistActive && !AcidSwimming.HasSwimClip(playerSlot))
		{
			playerSlot.Hero.cState.swimming = false;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
	}

	private static bool HeroFrame(HeroController self, out PlayerSlot p)
	{
		CoopSession session = Session;
		p = HeroPlayer(self);
		if (session == null || !session.Active || p == null)
		{
			return true;
		}
		if (p.Index > 0)
		{
			RefreshWalk(p);
		}
		if (Time.unscaledTime < p.WakeUntil)
		{
			Controls.Pump(p, block: true);
			return false;
		}
		if (p == session.Primary && session.EntryProxy)
		{
			Controls.Pump(p, block: true);
			return true;
		}
		if (ScriptedParty.Holds(p) || ChallengeSequence.Holds(p))
		{
			return false;
		}
		if (p.ArenaTransfer)
		{
			return false;
		}
		if (BenchSeats.Custom(p))
		{
			return false;
		}
		if (EmergencyWarp.Active(p) || CoopEnding.HoldsActor(p))
		{
			return false;
		}
		if ((p.Index > 0 && Charms.NativeMenuOpen) || (Plugin.Self.Panel && Charms.Editing && p.Index == Charms.Selected))
		{
			return false;
		}
		if (!CoopRules.RunHeroFrame(p.Index, p.Ready, p.Down, p.Hazard, p.Retiring, p.InputBlocked, session.Gameplay, p.Reviving))
		{
			return false;
		}
		Controls.Pump(p, TransitionVote.Holding(p) || Plugin.Self.Panel || p.Down || p.Hazard || !p.Connected || PvpMatch.BlocksInput);
		return true;
	}

	private static void Damage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide side, int amount, int hazardType)
	{
		if (CoopShades.IsSpawning(go))
		{
			return;
		}
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig(self, go, side, amount, hazardType);
		}
		else
		{
			if (hazardType == 3 && session.Data.hasAcidArmour)
			{
				return;
			}
			bool flag = RecoveryRules.RecoverAfterHazard(hazardType, session.Data.hasAcidArmour);
			if (session.TeamWipe || playerSlot.Down || playerSlot.Hazard || EmergencyWarp.Active(playerSlot) || CoopEnding.HoldsActor(playerSlot) || ((Time.time < playerSlot.ProtectionUntil || PvpMatch.Protects) && !flag))
			{
				return;
			}
			bool flag2 = PvpCombat.Applying(playerSlot);
			if ((bool)go && session.Resolve(go) != null && !flag2)
			{
				return;
			}
			bool flag3 = false;
			bool flag4 = false;
			int num = playerSlot.Vitals.Health + playerSlot.Vitals.Blue;
			int num2 = num;
			using (PlayerContext.Enter(playerSlot))
			{
				int num3 = amount;
				if (BossSceneController.IsBossScene)
				{
					switch (BossSceneController.Instance.BossLevel)
					{
					case 2:
						num3 = 9999;
						break;
					case 1:
						num3 *= 2;
						break;
					}
				}
				if (session.Data.overcharmed)
				{
					num3 *= 2;
				}
				if (!flag2 && amount > 0 && num3 >= num && num > 0 && (bool)Reflect.Call(self, "CanTakeDamage") && !session.Data.GetBool("invinciTest") && !self.takeNoDamage && self.damageMode != DamageMode.HAZARD_ONLY && !self.cState.shadowDashing && (hazardType != 1 || self.parryInvulnTimer <= 0f) && !Reflect.Get(self, "carefreeShieldEquipped", fallback: false) && (!session.Data.GetBool("equippedCharm_5") || session.Data.blockerHits <= 0 || !self.cState.focusing))
				{
					flag3 = true;
				}
				else
				{
					orig(self, go, side, amount, hazardType);
					num2 = session.Data.health + session.Data.healthBlue;
					flag4 = num2 < num;
				}
			}
			if (flag && !flag3 && !playerSlot.Down && !playerSlot.Hazard && session.Gameplay && !self.cState.transitioning && self.damageMode != DamageMode.NO_DAMAGE && !playerSlot.Vitals.Invincible)
			{
				session.Hazard(playerSlot);
			}
			if (playerSlot.Down || playerSlot.Hazard)
			{
				ActorRecovery.Freeze(playerSlot);
			}
			if (flag3)
			{
				Diagnostics.Write("FATAL HIT P" + (playerSlot.Index + 1) + " amount=" + amount + " hazard=" + hazardType + " hp=" + num);
				session.Down(playerSlot);
			}
			else if (flag4)
			{
				Revival.End(playerSlot);
				CombatEffects.MarkCombat();
				playerSlot.LastDamageAt = Time.unscaledTime;
				playerSlot.DamageFlashUntil = Time.unscaledTime + 0.35f;
				Diagnostics.Write("DAMAGE P" + (playerSlot.Index + 1) + " hp=" + num + "->" + num2 + " hazard=" + hazardType);
			}
		}
	}

	private static void CharmUpdate(On.HeroController.orig_CharmUpdate orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			try
			{
				orig(self);
			}
			catch (Exception)
			{
				PlayerSlot playerSlot = HeroPlayer(self);
				if (playerSlot != null && playerSlot.Index != 0)
				{
					return;
				}
				throw;
			}
		}
	}

	private static bool GetBool(On.PlayerData.orig_GetBool orig, PlayerData self, string name)
	{
		CoopSession session = Session;
		PlayerSlot current = PlayerContext.Current;
		if (session != null && session.Active && self == session.Data && name == "hasXunFlower" && FlowerRules.HideFrom(self, current))
		{
			return false;
		}
		return orig(self, name);
	}

	private static void SetBool(On.PlayerData.orig_SetBool orig, PlayerData self, string name, bool value)
	{
		ShadeCloakRitual.BoolChanged(self, name, value);
		CoopSession session = Session;
		PlayerSlot current = PlayerContext.Current;
		bool oldValue = false;
		if (session != null && session.Active && self == session.Data && name != null)
		{
			oldValue = Reflect.Get(self, name, fallback: false);
		}
		if (session != null && session.Active && self == session.Data && name == "xunFlowerBroken" && FlowerRules.RejectBreak(self, current, value))
		{
			return;
		}
		if (value && name != null && session != null && session.Active && self == session.Data && name.StartsWith("slyShellFrag", StringComparison.Ordinal) && !self.GetBool(name))
		{
			PickupCard.MaskAcquired(ShopMenuRouting.Buyer, WorldRouting.Current, name);
		}
		if (value && name != null && name.StartsWith("gotCharm_", StringComparison.Ordinal) && session != null && session.Active && self == session.Data && !self.GetBool(name))
		{
			PlayerSlot playerSlot = PlayerContext.Current ?? ((WorldRouting.Current == null) ? null : (ShopMenuRouting.Resolve(WorldRouting.Current) ?? WorldRouting.Resolve(WorldRouting.Current)));
			if ((playerSlot == null || !playerSlot.Alive) && WorldRouting.Current != null && (bool)WorldRouting.Current.GameObject)
			{
				playerSlot = session.Nearest(WorldRouting.Current.GameObject.transform.position);
			}
			if (playerSlot == null || !playerSlot.Alive)
			{
				playerSlot = null;
				foreach (PlayerSlot player in session.Players)
				{
					if (player.Alive)
					{
						if (playerSlot != null)
						{
							playerSlot = null;
							break;
						}
						playerSlot = player;
					}
				}
			}
			PickupCard.CharmAcquired(playerSlot, WorldRouting.Current);
		}
		orig(self, name, value);
		if (session != null && session.Active && self == session.Data)
		{
			FlowerRules.BoolChanged(session, self, name, oldValue, value, current);
		}
	}

	private static void SetInt(On.PlayerData.orig_SetInt orig, PlayerData self, string name, int value)
	{
		ShadeCloakRitual.LevelChanged(self, name, value);
		CoopSession session = Session;
		if (session != null && session.Active && self == session.Data && name == "heartPieces" && value > self.heartPieces)
		{
			PlayerSlot buyer = ShopMenuRouting.Buyer;
			if (buyer != null)
			{
				PickupCard.MaskAcquired(buyer, WorldRouting.Current, name);
			}
		}
		if (session != null && session.Active && self == session.Data && name == "healthBlue" && value > self.healthBlue)
		{
			PlayerSlot playerSlot = Lifeblood.RecentCollector();
			int num = value - self.healthBlue;
			if (playerSlot != null && playerSlot.Index > 0 && PlayerContext.Current != playerSlot)
			{
				Diagnostics.Write("LIFEBLOOD reroute +" + num + " to P" + (playerSlot.Index + 1));
				using (PlayerContext.Enter(playerSlot))
				{
					orig(self, name, self.healthBlue + num);
					return;
				}
			}
			Diagnostics.Write("BLUE HEALTH +" + num + " context=" + ((PlayerContext.Current == null) ? "vanilla" : ("P" + (PlayerContext.Current.Index + 1))));
		}
		orig(self, name, value);
	}

	private static void IntAdd(On.PlayerData.orig_IntAdd orig, PlayerData self, string name, int amount)
	{
		if (name == "heartPieces" && amount > 0 && Session != null && Session.Active && self == Session.Data)
		{
			PickupCard.MaskAcquired(ShopMenuRouting.Buyer, WorldRouting.Current, name);
		}
		using (PlayerContext.Enter(BlueHealthRecipient(self, name, amount)))
		{
			orig(self, name, amount);
		}
	}

	private static void IncrementInt(On.PlayerData.orig_IncrementInt orig, PlayerData self, string name)
	{
		if (name == "heartPieces" && Session != null && Session.Active && self == Session.Data)
		{
			PickupCard.MaskAcquired(ShopMenuRouting.Buyer, WorldRouting.Current, name);
		}
		using (PlayerContext.Enter(BlueHealthRecipient(self, name, 1)))
		{
			orig(self, name);
		}
	}

	private static PlayerSlot BlueHealthRecipient(PlayerData data, string name, int gain)
	{
		CoopSession session = Session;
		if (session == null || !session.Active || session.Data != data || name != "healthBlue" || gain <= 0)
		{
			return null;
		}
		PlayerSlot playerSlot = Lifeblood.RecentCollector();
		if (playerSlot != null && playerSlot.Index > 0 && PlayerContext.Current != playerSlot)
		{
			Diagnostics.Write("LIFEBLOOD add +" + gain + " to P" + (playerSlot.Index + 1));
		}
		if (playerSlot == null || playerSlot.Index <= 0)
		{
			return null;
		}
		return playerSlot;
	}

	private static void MaxHealth(On.HeroController.orig_MaxHealth orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
		CoopSession session = Session;
		if (session != null && session.Active && playerSlot != null && InteractionRouter.BenchActor(playerSlot))
		{
			session.BenchRest(self);
		}
	}

	private static IEnumerator Recoil(On.HeroController.orig_StartRecoil orig, HeroController self, CollisionSide side, bool effect, int amount)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		IEnumerator enumerator = orig(self, side, effect, amount);
		if (playerSlot == null || !Session.Active)
		{
			return HitStopIsolation.Wrap(enumerator, self);
		}
		IEnumerator original = new ScopedRoutine(playerSlot, enumerator);
		return HitStopIsolation.Wrap(original, self);
	}

	private static void TakeMP(On.HeroController.orig_TakeMP orig, HeroController self, int amount)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Reviving)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, amount);
		}
	}

	private static void TakeMPQuick(On.HeroController.orig_TakeMPQuick orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, amount);
		}
	}

	private static void AddSoul(On.HeroController.orig_AddMPCharge orig, HeroController self, int amount)
	{
		if (WorldRouting.warp == null && WorldRouting.warpAt.z == 104f && Time.unscaledTime - WorldRouting.warpAt.x < 3f)
		{
			object obj = HeroPlayer(self);
			if (obj != null && ((PlayerSlot)obj).Index == 0)
			{
				obj = Session.Players;
				if ((uint)((List<PlayerSlot>)obj).Count > (uint)(int)WorldRouting.warpAt.y)
				{
					obj = ((List<PlayerSlot>)obj)[(int)WorldRouting.warpAt.y];
					if (obj != null && ((PlayerSlot)obj).Alive)
					{
						self = ((PlayerSlot)obj).Hero;
					}
				}
			}
		}
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, amount);
		}
	}

	private static void GainSoul(On.HeroController.orig_SoulGain orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self);
		}
	}

	private static void SetSoul(On.HeroController.orig_SetMPCharge orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, amount);
		}
	}

	private static void TakeReserve(On.HeroController.orig_TakeReserveMP orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, amount);
		}
	}

	private static void AddHealth(On.HeroController.orig_AddHealth orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, amount);
		}
	}

	private static bool SpaSoul(On.HeroController.orig_TryAddMPChargeSpa orig, HeroController self, int amount)
	{
		return CoopBath.Refill(orig, self, amount);
	}

	private static void Entered(On.HeroController.orig_FinishedEnteringScene orig, HeroController self, bool marker, bool bob)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig(self, marker, bob);
		}
		if (Session != null && Session.Active)
		{
			Session.Entered(self);
		}
	}

	private static void CollisionEnter(On.HeroController.orig_OnCollisionEnter2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void CollisionStay(On.HeroController.orig_OnCollisionStay2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void CollisionExit(On.HeroController.orig_OnCollisionExit2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void HazardMarker(On.HazardRespawnTrigger.orig_OnTriggerEnter2D orig, HazardRespawnTrigger self, Collider2D other)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(other);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig(self, other);
		}
		else if (playerSlot.Alive && (bool)self.respawnMarker && other.gameObject.layer == 9)
		{
			using (PlayerContext.Enter(playerSlot))
			{
				session.Data.SetHazardRespawn(self.respawnMarker);
			}
		}
	}

	private static PlayerSlot BoxOwner(HeroBox box)
	{
		PlayerSlot playerSlot = Session?.Resolve(box);
		if (playerSlot != null && (bool)playerSlot.Hero)
		{
			Reflect.Set(box, "heroCtrl", playerSlot.Hero);
		}
		return playerSlot;
	}

	private static void BoxStart(On.HeroBox.orig_Start orig, HeroBox self)
	{
		using (PlayerContext.Enter(BoxOwner(self)))
		{
			orig(self);
		}
		BoxOwner(self);
	}

	private static void BoxEnter(On.HeroBox.orig_OnTriggerEnter2D orig, HeroBox self, Collider2D other)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, other);
		}
	}

	private static void BoxStay(On.HeroBox.orig_OnTriggerStay2D orig, HeroBox self, Collider2D other)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, other);
		}
	}

	private static void BoxLate(On.HeroBox.orig_LateUpdate orig, HeroBox self)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
	}

	private static void SelectBoard(On.InvCharmBackboard.orig_SelectCharm orig, InvCharmBackboard self)
	{
		orig(self);
		Charms.SelectedBoard(self);
	}

	private static int SelectEquipped(On.CharmItem.orig_GetListNumber orig, CharmItem self)
	{
		Charms.SelectedEquipped();
		return orig(self);
	}

	private static void BenchEnter(On.RestBench.orig_OnTriggerEnter2D orig, RestBench self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(c);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig(self, c);
		}
		else
		{
			playerSlot.Hero.NearBench(isNearBench: true);
		}
	}

	private static bool ExtraWalk(Collider2D c, out PlayerSlot p)
	{
		CoopSession session = Session;
		p = session?.Resolve(c);
		if (session != null && session.Active && p != null && p.Index > 0)
		{
			return c.gameObject.layer == 9;
		}
		return false;
	}

	private static void WalkEnter(On.WalkArea.orig_OnTriggerEnter2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig(self, c);
		}
		else
		{
			WalkTouch(self, p, inside: true);
		}
	}

	private static void WalkStay(On.WalkArea.orig_OnTriggerStay2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig(self, c);
		}
		else
		{
			WalkTouch(self, p, inside: true);
		}
	}

	private static void WalkExit(On.WalkArea.orig_OnTriggerExit2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig(self, c);
		}
		else
		{
			WalkTouch(self, p, inside: false);
		}
	}

	private static void WalkTouch(WalkArea area, PlayerSlot p, bool inside)
	{
		if (!walkAreas.TryGetValue(p, out var value))
		{
			value = (walkAreas[p] = new HashSet<WalkArea>());
		}
		if (inside)
		{
			value.Add(area);
		}
		else
		{
			value.Remove(area);
		}
		RefreshWalk(p);
	}

	private static void RefreshWalk(PlayerSlot p)
	{
		if (!p.Hero)
		{
			return;
		}
		bool flag = false;
		if (walkAreas.TryGetValue(p, out var value))
		{
			Collider2D heroCollider = p.Hero.GetComponent<Collider2D>();
			value.RemoveWhere(delegate(WalkArea area)
			{
				if (!area || !area.enabled || !area.gameObject.activeInHierarchy)
				{
					return true;
				}
				Collider2D component = area.GetComponent<Collider2D>();
				if (!component || !component.enabled)
				{
					return true;
				}
				return (!heroCollider) ? (!component.bounds.Contains(p.Hero.transform.position)) : (!component.bounds.Intersects(heroCollider.bounds));
			});
			flag = value.Count > 0;
		}
		if (p.Hero.cState.inWalkZone != flag)
		{
			if (p.Hero.cState.inWalkZone && !flag)
			{
				Diagnostics.Write("WALK zone cleared P" + (p.Index + 1));
			}
			p.Hero.SetWalkZone(flag);
		}
	}

	private static void WorldTriggerEnter(On.TriggerEnterEvent.orig_OnTriggerEnter2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(self.gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(self.gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: false);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void WorldTriggerStay(On.TriggerEnterEvent.orig_OnTriggerStay2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(self.gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(self.gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: false);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void WorldTriggerExit(On.TriggerEnterEvent.orig_OnTriggerExit2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(self.gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(self.gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: true);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, c);
		}
	}

	private static void BenchExit(On.RestBench.orig_OnTriggerExit2D orig, RestBench self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(c);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig(self, c);
		}
		else
		{
			playerSlot.Hero.NearBench(isNearBench: false);
		}
	}

	private static IEnumerator Die(On.HeroController.orig_Die orig, HeroController self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || session.AllowVanillaDeath || playerSlot == null)
		{
			return orig(self);
		}
		session.Down(playerSlot);
		return Empty();
	}

	private static IEnumerator Hazard(On.HeroController.orig_DieFromHazard orig, HeroController self, HazardType type, float angle)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || session.TeamWipe || playerSlot == null)
		{
			return orig(self, type, angle);
		}
		if (type == HazardType.ACID && session.Data.hasAcidArmour)
		{
			return Empty();
		}
		using (PlayerContext.Enter(playerSlot))
		{
			if (session.Data.health <= 0)
			{
				session.Down(playerSlot);
			}
			else
			{
				session.Hazard(playerSlot);
			}
		}
		return Empty();
	}

	private static void EnemyDie(On.HealthManager.orig_Die orig, HealthManager self, float? direction, AttackTypes type, bool ignoreEvasion)
	{
		if (!CoopShades.HandleDeath(self))
		{
			orig(self, direction, type, ignoreEvasion);
		}
	}

	private static void Hit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hit)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig(self, hit);
			return;
		}
		PlayerSlot playerSlot = session.Resolve(hit.Source) ?? PlayerContext.Current;
		string text = SummonRouting.DamageKind(hit.Source);
		int damageDealt = hit.DamageDealt;
		if (playerSlot != null && session.Players.Count > 1 && !self.GetComponent<LocalShade>())
		{
			Lifeblood.Hit(self, playerSlot);
			CombatEffects.MarkCombat();
			int livingCombatants = session.LivingCombatants;
			float num = (float)CoopRules.DamageDivisor(livingCombatants, Plugin.Self.Difficulty.Value);
			if (lastDamageCount != livingCombatants)
			{
				lastDamageCount = livingCombatants;
				Diagnostics.Write("DAMAGE SCALE living=" + livingCombatants + " divisor=" + num);
			}
			if (hit.DamageDealt > 0)
			{
				hit.DamageDealt = Mathf.Max(1, Mathf.RoundToInt((float)hit.DamageDealt / num));
			}
		}
		if (text != null && damageDealt > 0)
		{
			string item = text + "/" + ((playerSlot == null) ? "unowned" : playerSlot.Index.ToString()) + "/" + session.LivingCombatants;
			if (familiarDamageLogged.Add(item))
			{
				Diagnostics.Write("FAMILIAR DAMAGE kind=" + text + " owner=" + ((playerSlot == null) ? "none" : ("P" + (playerSlot.Index + 1))) + " living=" + session.LivingCombatants + " base=" + damageDealt + " scaled=" + hit.DamageDealt);
			}
		}
		int hp = self.hp;
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self, hit);
		}
		if (playerSlot != null && (bool)self && self.hp > 0 && self.hp < hp)
		{
			ArenaGather.BossHit(self, playerSlot);
		}
	}

	private static void CameraLate(On.CameraController.orig_LateUpdate orig, CameraController self)
	{
		CoopSession session = Session;
		if (session != null && session.Active && session.Gameplay && !session.TeamWipe)
		{
			session.Camera.Prepare(self);
		}
		orig(self);
		if (session != null && session.Active && session.Gameplay && !session.TeamWipe)
		{
			session.Camera.Apply(self, session);
		}
	}

	private static void MusicCueChange(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self, MusicCue cue, float delay, float transition, bool snapshot)
	{
		BossMusicGuard.Cue(orig, self, cue, delay, transition, snapshot);
	}

	private static bool PrimaryCollider(Collider2D c)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			return true;
		}
		PlayerSlot playerSlot = session.Resolve(c);
		if (playerSlot != null)
		{
			return playerSlot.Index == 0;
		}
		return true;
	}

	private static bool TransitionAllowed(TransitionPoint gate, PlayerSlot p)
	{
		if (Session == null || !Session.Active || p == null)
		{
			return true;
		}
		if (!p.Ready || !p.Alive || EmergencyWarp.Active(p) || CoopEnding.Active)
		{
			return false;
		}
		if (!gate.isADoor && !string.IsNullOrEmpty(gate.targetScene) && !string.IsNullOrEmpty(gate.entryPoint) && PvpMatch.BlockExit(p))
		{
			return false;
		}
		return true;
	}

	private static void TransitionEnter(On.TransitionPoint.orig_OnTriggerEnter2D orig, TransitionPoint self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot p = session?.Resolve(c);
		if (!TransitionAllowed(self, p) || TransitionVote.Gate(session, p, self, c, delegate
		{
			using (PlayerContext.Enter(p))
			{
				orig(self, c);
			}
		}))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig(self, c);
		}
	}

	private static void TransitionStay(On.TransitionPoint.orig_OnTriggerStay2D orig, TransitionPoint self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot p = session?.Resolve(c);
		if (!TransitionAllowed(self, p) || TransitionVote.Gate(session, p, self, c, delegate
		{
			using (PlayerContext.Enter(p))
			{
				orig(self, c);
			}
		}))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig(self, c);
		}
	}

	private static void CameraEnter(On.CameraLockArea.orig_OnTriggerEnter2D orig, CameraLockArea self, Collider2D c)
	{
		ArenaGather.CameraContact(self, c);
		if (PrimaryCollider(c))
		{
			orig(self, c);
		}
	}

	private static void CameraStay(On.CameraLockArea.orig_OnTriggerStay2D orig, CameraLockArea self, Collider2D c)
	{
		ArenaGather.CameraContact(self, c);
		if (PrimaryCollider(c))
		{
			orig(self, c);
		}
	}

	private static void CameraExit(On.CameraLockArea.orig_OnTriggerExit2D orig, CameraLockArea self, Collider2D c)
	{
		if (PrimaryCollider(c))
		{
			orig(self, c);
		}
	}

	private static void BeginTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
	{
		if (PvpArena.Transition(orig, self, info) || !DoorRouteFix.BeforeTransition(info))
		{
			return;
		}
		PlayerSlot playerSlot = PlayerContext.Current ?? InteractionRouter.ActivePlayer;
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig(self, info);
		}
		else if (!PvpMatch.BlockExit(playerSlot) && !self.IsLoadingSceneTransition && !CoopEnding.Defer(info, session) && !TransitionVote.Direct(session, playerSlot, self, info))
		{
			EmergencyWarp.Reset();
			try
			{
				InteractionRouter.CloseForTransition();
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("DIALOGUE close", ex);
			}
			CrystalDashTransit.Prepare(session, playerSlot, info);
			using (PlayerContext.Enter(session.Primary))
			{
				orig(self, info);
			}
		}
	}

	private static void LoadScene(On.GameManager.orig_LoadScene orig, GameManager self, string scene)
	{
		if (!CoopEnding.DeferDirect(scene, Session))
		{
			if (Session != null && Session.Active)
			{
				InteractionRouter.CloseForTransition();
			}
			orig(self, scene);
		}
	}

	private static IEnumerator PauseToggle(On.GameManager.orig_PauseGameToggle orig, GameManager self)
	{
		CoopSession session = Session;
		PlayerSlot p = PlayerContext.Current ?? InteractionRouter.ActivePlayer ?? session?.Primary;
		if (session != null && session.Active && !self.isPaused && (EmergencyWarp.Active(p) || EmergencyWarp.Holding(p)))
		{
			return Empty();
		}
		return orig(self);
	}

	private static bool FreeToPause(CoopSession s, GameManager gm, InputHandler input)
	{
		UIManager instance = UIManager.instance;
		PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
		bool flag = ShopMenuRouting.Buyer != null || StagMenuRouting.HasOwner || PickupCard.Owner != null || activePlayer != null;
		if (s.Data.disablePause && flag)
		{
			pauseRecoveryCandidate = true;
		}
		if (!s.Data.disablePause)
		{
			pauseRecoveryCandidate = false;
		}
		if (!s.Gameplay || gm.isPaused || !instance || instance.uiState != UIState.PLAYING || Plugin.Self.Panel || !input.acceptingInput || !input.pauseAllowed || Charms.NativeMenuOpen || flag || ScriptedParty.Active || CoopEnding.Active)
		{
			lastPauseModalAt = Time.unscaledTime;
			return false;
		}
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Ready && player.Alive && (player.Hero.controlReqlinquished || EmergencyWarp.Active(player) || EmergencyWarp.Holding(player)))
			{
				lastPauseModalAt = Time.unscaledTime;
				return false;
			}
		}
		return Time.unscaledTime - lastPauseModalAt > 0.75f;
	}

	private static void InputUpdate(On.InputHandler.orig_Update orig, InputHandler self)
	{
		CoopSession session = Session;
		GameManager instance = GameManager.instance;
		bool flag = false;
		if (session != null && session.Active && (bool)instance && instance.inputHandler == self && session.Primary != null && session.Primary.Actions != null && PlayerContext.Current == null)
		{
			PlayerSlot playerSlot = PickupCard.Owner ?? session.Primary;
			if (self.inputActions != playerSlot.Actions)
			{
				self.inputActions = playerSlot.Actions;
			}
			if ((bool)playerSlot.Hero && instance.IsGameplayScene() && instance.HasFinishedEnteringScene)
			{
				self.AttachHeroController(playerSlot.Hero);
			}
			bool flag2 = FreeToPause(session, instance, self);
			if (KeypadInput.Pressed(KeyCode.Escape) && instance.gameState == GameState.PLAYING)
			{
				if (flag2 && session.Data.disablePause && pauseRecoveryCandidate)
				{
					session.Data.disablePause = false;
					pauseRecoveryCandidate = false;
					Diagnostics.Write("PAUSE restored stale disablePause after interaction");
				}
				if (flag2 && !session.Data.disablePause && !self.inputActions.pause.WasPressed)
				{
					flag = true;
				}
				else if (!flag2 || session.Data.disablePause)
				{
					Diagnostics.Write("PAUSE denied accepting=" + self.acceptingInput + " allowed=" + self.pauseAllowed + " disable=" + session.Data.disablePause + " bindings=" + self.inputActions.pause.Bindings.Count + " primaryReady=" + session.Primary.Ready + " scripted=" + ScriptedParty.Active);
				}
			}
		}
		if (session == null || !session.Active)
		{
			pauseRecoveryCandidate = false;
		}
		orig(self);
		if (flag && (bool)instance && !instance.isPaused && instance.gameState == GameState.PLAYING && self.acceptingInput && self.pauseAllowed)
		{
			Diagnostics.Write("PAUSE keyboard fallback (controller action did not receive Escape)");
			self.StartCoroutine(instance.PauseGameToggle());
		}
	}

	private static IEnumerator PlayerDead(On.GameManager.orig_PlayerDead orig, GameManager self, float wait)
	{
		if (PlayerContext.Current != null && PlayerContext.Current.Index != 0)
		{
			return Empty();
		}
		return orig(self, wait);
	}

	private static IEnumerator PlayerDeadHazard(On.GameManager.orig_PlayerDeadFromHazard orig, GameManager self, float wait)
	{
		if (PlayerContext.Current != null && PlayerContext.Current.Index != 0)
		{
			return Empty();
		}
		return orig(self, wait);
	}

	private static void SaveGame(On.GameManager.orig_SaveGame orig, GameManager self)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig(self);
			return;
		}
		using (new SaveScope(session))
		{
			orig(self);
		}
	}

	private static GameObject PoolSpawn(On.ObjectPool.orig_Spawn_GameObject_Transform_Vector3_Quaternion orig, GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
	{
		GameObject gameObject = orig(prefab, parent, position, rotation);
		Spawned(gameObject);
		SummonRouting.Observed(prefab, parent, gameObject);
		return gameObject;
	}

	private static void HatchlingFixed(On.KnightHatchling.orig_FixedUpdate orig, KnightHatchling self)
	{
		CoopSession session = Session;
		using (PlayerContext.Enter((session != null && session.Active) ? session.Resolve(self) : null))
		{
			orig(self);
		}
	}

	private static IEnumerator HatchlingSpawn(On.KnightHatchling.orig_Spawn orig, KnightHatchling self)
	{
		IEnumerator enumerator = orig(self);
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? session.Resolve(self) : null);
		if (playerSlot == null)
		{
			return enumerator;
		}
		return new ScopedRoutine(playerSlot, enumerator);
	}

	private static IEnumerator HatchlingTeleEnd(On.KnightHatchling.orig_TeleEnd orig, KnightHatchling self)
	{
		IEnumerator enumerator = orig(self);
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? session.Resolve(self) : null);
		if (playerSlot == null)
		{
			return enumerator;
		}
		return new ScopedRoutine(playerSlot, enumerator);
	}

	private static void Spawned(GameObject result)
	{
		SpellSafety.Spawned(result);
		NativeDashEffects.Spawned(result);
		CrystalDashCoop.Spawned(result);
	}

	private static void SpellPoolAction(On.HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool.orig_OnEnter orig, HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool self)
	{
		SpellSafety.Pool(orig, self);
	}

	private static void SpellCreateAction(On.HutongGames.PlayMaker.Actions.CreateObject.orig_OnEnter orig, HutongGames.PlayMaker.Actions.CreateObject self)
	{
		SpellSafety.Create(orig, self);
	}

	internal static void SpellBorn(GameObject projectile, PlayerSlot caster)
	{
		CoopSession session = Session;
		if (!projectile || caster == null || !caster.Hero || session == null || !session.Active)
		{
			return;
		}
		bool flag = projectile.GetComponentInChildren<DamageEnemies>(includeInactive: true) != null;
		if (!flag)
		{
			PlayMakerFSM[] componentsInChildren = projectile.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].FsmName == "damages_enemy")
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			return;
		}
		(projectile.GetComponent<OwnerTag>() ?? projectile.AddComponent<OwnerTag>()).Player = caster;
		session.RefreshOwnership(projectile, caster);
		RebindReferences(projectile, caster);
		PvpCombat.Track(projectile, caster, reset: true);
		string text = projectile.name.ToLowerInvariant();
		if (text.Contains("fireball") || text.Contains("vengeful"))
		{
			float num = (caster.Hero.cState.facingRight ? 1f : (-1f));
			if ((projectile.transform.position - caster.Hero.transform.position).sqrMagnitude > 16f)
			{
				Vector3 vector = caster.Hero.transform.position + new Vector3(num * 1.15f, 0.15f, 0f);
				projectile.transform.position = vector;
				Rigidbody2D component = projectile.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					component.position = vector;
				}
			}
			Rigidbody2D component2 = projectile.GetComponent<Rigidbody2D>();
			if ((bool)component2 && Mathf.Abs(component2.velocity.x) > 0.1f)
			{
				component2.velocity = new Vector2(Mathf.Abs(component2.velocity.x) * num, component2.velocity.y);
			}
			Vector3 localScale = projectile.transform.localScale;
			if (Mathf.Abs(localScale.x) > 0.01f)
			{
				localScale.x = Mathf.Abs(localScale.x) * num;
				projectile.transform.localScale = localScale;
			}
		}
		Diagnostics.Write("SPELL caster P" + (caster.Index + 1) + " via=" + text + " pos=" + projectile.transform.position.ToString() + " face=" + caster.Hero.cState.facingRight);
	}

	private static void EnemyUpdate(On.LineOfSightDetector.orig_Update orig, LineOfSightDetector self)
	{
		CoopSession session = Session;
		using (PlayerContext.Enter((session != null && session.Active) ? session.Nearest(self.transform.position) : null, enemy: true))
		{
			orig(self);
		}
	}

	private static void SpellOrb(On.SpellGetOrb.orig_OnEnable orig, SpellGetOrb self)
	{
		orig(self);
		ScriptedParty.SpellOrb(self);
	}

	private static PlayerSlot FsmPlayer(HutongGames.PlayMaker.Fsm fsm, out bool enemy)
	{
		enemy = false;
		CoopSession session = Session;
		if (session == null || !session.Active || fsm == null)
		{
			return null;
		}
		if (AcidSwimming.WorldSurface(fsm))
		{
			return session.Primary;
		}
		PlayerSlot playerSlot = PickupCard.Resolve(fsm) ?? StagMenuRouting.Resolve(fsm) ?? ShopMenuRouting.Resolve(fsm) ?? session.Resolve(fsm) ?? InteractionRouter.Resolve(fsm) ?? Lifeblood.Resolve(fsm) ?? ArenaGather.Resolve(fsm) ?? WorldRouting.Resolve(fsm);
		if (playerSlot == null && (bool)fsm.GameObject)
		{
			if (!enemies.TryGetValue(fsm, out var value))
			{
				value = new EnemyBinding
				{
					Health = fsm.GameObject.GetComponentInParent<HealthManager>(),
					Damage = fsm.GameObject.GetComponentInParent<DamageHero>()
				};
				enemies[fsm] = value;
			}
			if (((bool)value.Health && !value.Health.GetIsDead()) || (bool)value.Damage)
			{
				playerSlot = session.Nearest(fsm.GameObject.transform.position);
				enemy = true;
			}
		}
		PlayerSlot playerSlot2 = playerSlot;
		GameObject gameObject = fsm.GameObject;
		if ((bool)gameObject && gameObject.name.ToLowerInvariant().Contains("soul"))
		{
			if (playerSlot2 == null)
			{
				playerSlot2 = session.Nearest(fsm.GameObject.transform.position);
				if (playerSlot2 == null)
				{
					goto IL_018a;
				}
				playerSlot = playerSlot2;
				playerSlot2 = playerSlot;
				enemy = true;
			}
			playerSlot = playerSlot2;
			if (WorldRouting.warp == null && playerSlot2.Index > 0)
			{
				WorldRouting.warpAt = new Vector3(Time.unscaledTime, playerSlot.Index, 104f);
			}
		}
		goto IL_018a;
		IL_018a:
		return playerSlot2;
	}

	internal static void ClearSceneCache()
	{
		AcidSwimming.Reset();
		NativeQuickMap.Reset();
		fsmBindings.Clear();
		enemies.Clear();
		walkAreas.Clear();
		familiarDamageLogged.Clear();
		lastDamageCount = -1;
		lastPauseModalAt = Time.unscaledTime;
		ShadeCloakRitual.Reset();
		RadianceAscentCheckpoint.Reset();
		CrystalDashCoop.Reset();
		DreamRescueFeedback.Reset();
	}

	private static void RunFsm(Action action, HutongGames.PlayMaker.Fsm self, bool frame)
	{
		bool enemy;
		PlayerSlot playerSlot = FsmPlayer(self, out enemy);
		if ((frame && playerSlot != null && (ShadeCloakRitual.HoldsFsm(playerSlot, self) || playerSlot.InputBlocked || playerSlot.Down || playerSlot.Hazard || playerSlot.Retiring || !playerSlot.Ready || (playerSlot.Index > 0 && Charms.NativeMenuOpen) || (Plugin.Self.Panel && Charms.Editing && playerSlot.Index == Charms.Selected))) || (frame && playerSlot != null && Session.Resolve(self) == playerSlot && self.Name == "Spell Control" && (playerSlot.Reviving || (!playerSlot.FocusReleased && playerSlot.Actions != null && playerSlot.Actions.cast.IsPressed))))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, playerSlot);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						action();
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmAwake(On.HutongGames.PlayMaker.Fsm.orig_Awake orig, HutongGames.PlayMaker.Fsm self)
	{
		RunFsm(delegate
		{
			orig(self);
		}, self, frame: false);
	}

	private static void FsmEnable(On.HutongGames.PlayMaker.Fsm.orig_OnEnable orig, HutongGames.PlayMaker.Fsm self)
	{
		RunFsm(delegate
		{
			orig(self);
		}, self, frame: false);
		PvpCombat.TrackFsm(self);
	}

	private static void FsmStart(On.HutongGames.PlayMaker.Fsm.orig_Start orig, HutongGames.PlayMaker.Fsm self)
	{
		InteractionRouter.Ready(self);
		RunFsm(delegate
		{
			orig(self);
		}, self, frame: false);
		PvpCombat.TrackFsm(self);
	}

	private static void StateEnter(On.HutongGames.PlayMaker.FsmState.orig_OnEnter orig, HutongGames.PlayMaker.FsmState self)
	{
		ArenaGather.ObserveState(self);
		CoopEnding.BeforeState(self);
		bool enemy;
		PlayerSlot playerSlot = FsmPlayer(self.Fsm, out enemy);
		ChallengeSequence.ObserveState(self, playerSlot);
		using (PlayerContext.Enter(playerSlot, enemy))
		{
			using (CharmNativeUi.Enter(self.Fsm))
			{
				if (playerSlot != null && self.Actions != null)
				{
					HutongGames.PlayMaker.FsmStateAction[] actions = self.Actions;
					foreach (HutongGames.PlayMaker.FsmStateAction fsmStateAction in actions)
					{
						if (fsmStateAction != null)
						{
							RebindObject(fsmStateAction, playerSlot);
						}
					}
				}
				orig(self);
			}
		}
		BenchSeats.Observe(self.Fsm, playerSlot);
		RadianceAscentCheckpoint.Observed(self);
		ChallengeAnimationSync.AfterState(self);
	}

	private static bool FrameAllowed(HutongGames.PlayMaker.Fsm self, out PlayerSlot p, out bool enemy)
	{
		p = FsmPlayer(self, out enemy);
		CoopSession session = Session;
		if (session != null && session.Active && AcidSwimming.SuppressWorldFsm(self))
		{
			return false;
		}
		if (InteractionRouter.Blocks(self))
		{
			return false;
		}
		if (p == null)
		{
			return true;
		}
		bool flag = session != null && session.Resolve(self) == p;
		if (flag && p == session.Primary && session.EntryProxy)
		{
			return true;
		}
		if (flag && (ShadeCloakRitual.HoldsFsm(p, self) || ChallengeSequence.Holds(p) || EmergencyWarp.Active(p) || CoopEnding.BlocksFsm(p, self) || BenchSeats.Custom(p)))
		{
			return false;
		}
		if (flag && (p.ArenaTransfer || p.Down || p.Hazard || p.Retiring || !p.Ready || (p.Index > 0 && (p.InputBlocked || Charms.NativeMenuOpen)) || (Plugin.Self.Panel && Charms.Editing && p.Index == Charms.Selected)))
		{
			return false;
		}
		if (session != null && session.Resolve(self) == p && self.Name == "Spell Control" && (p.Reviving || (!p.FocusReleased && p.Actions != null && p.Actions.cast.IsPressed)))
		{
			return false;
		}
		return true;
	}

	private static void FsmUpdate(On.HutongGames.PlayMaker.Fsm.orig_Update orig, HutongGames.PlayMaker.Fsm self)
	{
		if (!FrameAllowed(self, out var p, out var enemy))
		{
			return;
		}
		using (PlayerContext.Enter(p, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, p);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						orig(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmFixed(On.HutongGames.PlayMaker.Fsm.orig_FixedUpdate orig, HutongGames.PlayMaker.Fsm self)
	{
		if (!FrameAllowed(self, out var p, out var enemy))
		{
			return;
		}
		using (PlayerContext.Enter(p, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, p);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						orig(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmLate(On.HutongGames.PlayMaker.Fsm.orig_LateUpdate orig, HutongGames.PlayMaker.Fsm self)
	{
		if (!FrameAllowed(self, out var p, out var enemy))
		{
			return;
		}
		using (PlayerContext.Enter(p, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, p);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						orig(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmEvent(On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent orig, HutongGames.PlayMaker.Fsm self, HutongGames.PlayMaker.FsmEvent evt, HutongGames.PlayMaker.FsmEventData data)
	{
		ShadeCloakRitual.BeforeEvent(self, evt, data);
		HutongGames.PlayMaker.Fsm self2 = self;
		HutongGames.PlayMaker.FsmEvent evt2 = evt;
		HutongGames.PlayMaker.FsmEventData data2 = data;
		if (ChallengeSequence.Intercept(self2, evt2) || InteractionRouter.Blocks(self2) || (Session != null && Session.Active && !InteractionRouter.EventAllowed(self2, data2)))
		{
			return;
		}
		if (Session != null && Session.Active)
		{
			StagMenuRouting.Observe(self2, evt2, data2);
		}
		ShareRoar(self2, evt2);
		ArenaGather.Observe(self2, evt2, data2);
		if (Session != null && Session.Active && Lifeblood.Award(evt2))
		{
			return;
		}
		PlayerSlot current = PlayerContext.Current;
		CoopSession session = Session;
		if (session != null && session.Active && current != null && evt2 != null && localEvents.Contains(evt2.Name))
		{
			PlayerSlot playerSlot = session.Resolve(self2);
			if (playerSlot != null && playerSlot != current)
			{
				return;
			}
		}
		if (session != null && session.Active && current != null && current.Index > 0 && evt2 != null && IsVitalsHud(self2) && (localEvents.Contains(evt2.Name) || evt2.Name.StartsWith("MP ") || evt2.Name == "ADD BLUE HEALTH"))
		{
			return;
		}
		try
		{
			RunFsm(delegate
			{
				orig(self2, evt2, data2);
			}, self2, frame: false);
		}
		finally
		{
			StagMenuRouting.AfterEvent(self2, evt2);
		}
	}

	private static void ShareRoar(HutongGames.PlayMaker.Fsm source, HutongGames.PlayMaker.FsmEvent evt)
	{
		CoopSession session = Session;
		if (sharingRoar || session == null || !session.Active || source.Name != "Roar Lock" || evt == null || (evt.Name != "ROAR ENTER" && evt.Name != "ROAR EXIT"))
		{
			return;
		}
		bool flag = evt.Name == "ROAR ENTER";
		if ((flag ? roarEnterFrame : roarExitFrame) == Time.frameCount)
		{
			return;
		}
		if (flag)
		{
			roarEnterFrame = Time.frameCount;
		}
		else
		{
			roarExitFrame = Time.frameCount;
		}
		PlayerSlot playerSlot = session.Resolve(source);
		if (playerSlot == null)
		{
			return;
		}
		HutongGames.PlayMaker.FsmGameObject fsmGameObject = source.Variables.FindFsmGameObject("Roar Object");
		sharingRoar = true;
		try
		{
			foreach (PlayerSlot player in session.Players)
			{
				if (player == playerSlot || !player.Ready || !player.Alive)
				{
					continue;
				}
				PlayMakerFSM playMakerFSM = PlayMakerFSM.FindFsmOnGameObject(player.Hero.gameObject, "Roar Lock");
				if ((bool)playMakerFSM)
				{
					HutongGames.PlayMaker.FsmGameObject fsmGameObject2 = playMakerFSM.FsmVariables.FindFsmGameObject("Roar Object");
					if (fsmGameObject2 != null && fsmGameObject != null)
					{
						fsmGameObject2.Value = fsmGameObject.Value;
					}
					using (PlayerContext.Enter(player))
					{
						playMakerFSM.SendEvent(evt.Name);
					}
				}
			}
		}
		finally
		{
			sharingRoar = false;
		}
	}

	private static bool AllowInteraction(HutongGames.PlayMaker.Fsm self, Collider2D other, bool entering)
	{
		CoopSession session = Session;
		if (session != null && session.Active)
		{
			PlayerSlot playerSlot = session.Resolve(other);
			if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WorldWaterContact(self.GameObject, playerSlot))
			{
				AcidSwimming.NoteBlocked(self, playerSlot);
				return false;
			}
		}
		if (session != null && session.Active)
		{
			return InteractionRouter.Trigger(self, other, exiting: false);
		}
		return true;
	}

	private static bool SecondaryWaterCollision(HutongGames.PlayMaker.Fsm self, Collision2D collision)
	{
		CoopSession session = Session;
		if (session == null || !session.Active || collision == null || !AcidSwimming.WorldSurface(self))
		{
			return false;
		}
		PlayerSlot playerSlot = session.Resolve(collision.collider) ?? session.Resolve(collision.gameObject);
		if (playerSlot == null || playerSlot.Index == 0)
		{
			return false;
		}
		AcidSwimming.NoteBlocked(self, playerSlot);
		return true;
	}

	private static void WaterCollisionEnter(On.HutongGames.PlayMaker.Fsm.orig_OnCollisionEnter2D orig, HutongGames.PlayMaker.Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig(self, c);
		}
	}

	private static void WaterCollisionStay(On.HutongGames.PlayMaker.Fsm.orig_OnCollisionStay2D orig, HutongGames.PlayMaker.Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig(self, c);
		}
	}

	private static void WaterCollisionExit(On.HutongGames.PlayMaker.Fsm.orig_OnCollisionExit2D orig, HutongGames.PlayMaker.Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig(self, c);
		}
	}

	private static void ListenUp(On.HutongGames.PlayMaker.Actions.ListenForUp.orig_CheckForInput orig, HutongGames.PlayMaker.Actions.ListenForUp self)
	{
		PlayerSlot playerSlot = StagMenuRouting.Resolve(self.Fsm);
		if (playerSlot != null)
		{
			using (PlayerContext.Enter(playerSlot))
			{
				orig(self);
				return;
			}
		}
		CoopSession session = Session;
		PlayerSlot p = null;
		if (session != null && session.Active && !InteractionRouter.InputOwner(self.Fsm, out p))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig(self);
		}
	}

	private static void ListenDown(On.HutongGames.PlayMaker.Actions.ListenForDown.orig_CheckForInput orig, HutongGames.PlayMaker.Actions.ListenForDown self)
	{
		PlayerSlot playerSlot = StagMenuRouting.Resolve(self.Fsm);
		if (playerSlot != null)
		{
			using (PlayerContext.Enter(playerSlot))
			{
				orig(self);
				return;
			}
		}
		CoopSession session = Session;
		PlayerSlot p = null;
		if (session != null && session.Active && !InteractionRouter.InputOwner(self.Fsm, out p))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig(self);
		}
	}

	private static void InteractionCall(On.HutongGames.PlayMaker.Actions.CallMethodProper.orig_OnEnter orig, HutongGames.PlayMaker.Actions.CallMethodProper self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? (PickupCard.Resolve(self.Fsm) ?? ShopMenuRouting.Resolve(self.Fsm) ?? InteractionRouter.Resolve(self.Fsm) ?? WorldRouting.Resolve(self.Fsm) ?? PlayerContext.Current) : null);
		if (playerSlot != null)
		{
			RebindObject(self, playerSlot);
			Reflect.Set(self, "cachedBehaviour", null);
			Reflect.Set(self, "cachedMethodInfo", null);
			Reflect.Set(self, "cachedType", null);
			Reflect.Set(self, "component", null);
		}
		PickupCard.ObserveCall(self.Fsm, playerSlot, (self.methodName == null) ? null : self.methodName.Value);
		using (PlayerContext.Enter(playerSlot))
		{
			orig(self);
		}
	}

	private static void CreatePickupCard(On.HutongGames.PlayMaker.Actions.CreateUIMsgGetItem.orig_OnEnter orig, HutongGames.PlayMaker.Actions.CreateUIMsgGetItem self)
	{
		orig(self);
		PickupCard.Created(self.Fsm, (self.storeObject == null) ? null : self.storeObject.Value);
	}

	private static void PickupJump(On.HutongGames.PlayMaker.Actions.ListenForJump.orig_OnUpdate orig, HutongGames.PlayMaker.Actions.ListenForJump self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(self.Fsm)))
		{
			orig(self);
		}
	}

	private static void PickupMenuActions(On.HutongGames.PlayMaker.Actions.ListenForMenuActions.orig_OnUpdate orig, HutongGames.PlayMaker.Actions.ListenForMenuActions self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(self.Fsm) ?? ShopMenuRouting.ListenerOwner(self.Fsm)))
		{
			orig(self);
		}
	}

	private static void PickupMenuSubmit(On.HutongGames.PlayMaker.Actions.ListenForMenuSubmit.orig_OnUpdate orig, HutongGames.PlayMaker.Actions.ListenForMenuSubmit self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(self.Fsm) ?? ShopMenuRouting.ListenerOwner(self.Fsm)))
		{
			orig(self);
		}
	}

	private static void PickupMenuCancel(On.HutongGames.PlayMaker.Actions.ListenForMenuCancel.orig_OnUpdate orig, HutongGames.PlayMaker.Actions.ListenForMenuCancel self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(self.Fsm) ?? ShopMenuRouting.ListenerOwner(self.Fsm)))
		{
			orig(self);
		}
	}

	private static void QuickMapUpdate(On.HutongGames.PlayMaker.Actions.ListenForQuickMap.orig_OnUpdate orig, HutongGames.PlayMaker.Actions.ListenForQuickMap self)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig(self);
			return;
		}
		NativeQuickMap.Observe(session);
		using (PlayerContext.Enter(NativeQuickMap.Owner ?? session.Primary))
		{
			orig(self);
		}
		NativeQuickMap.AfterListener();
	}

	private static void MapUpdate(On.GameMap.orig_Update orig, GameMap self)
	{
		NativeQuickMap.MapUpdate(self, orig);
	}

	private static void FsmTriggerEnter(On.HutongGames.PlayMaker.Fsm.orig_OnTriggerEnter2D orig, HutongGames.PlayMaker.Fsm self, Collider2D other)
	{
		if (!AllowInteraction(self, other, entering: true))
		{
			return;
		}
		ArenaGather.Contact(self, other);
		PlayerSlot playerSlot = Lifeblood.Track(self, other);
		PlayerSlot playerSlot2 = WorldRouting.Touch(self, other, exit: false);
		bool enemy = false;
		PlayerSlot playerSlot3 = ((Session == null) ? null : Session.Resolve(self));
		playerSlot3 = playerSlot3 ?? playerSlot ?? playerSlot2 ?? FsmPlayer(self, out enemy);
		using (PlayerContext.Enter(playerSlot3, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, playerSlot3);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						orig(self, other);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmTriggerStay(On.HutongGames.PlayMaker.Fsm.orig_OnTriggerStay2D orig, HutongGames.PlayMaker.Fsm self, Collider2D other)
	{
		if (!AllowInteraction(self, other, entering: true))
		{
			return;
		}
		ArenaGather.Contact(self, other);
		PlayerSlot playerSlot = Lifeblood.Track(self, other);
		PlayerSlot playerSlot2 = WorldRouting.Touch(self, other, exit: false);
		bool enemy = false;
		PlayerSlot playerSlot3 = ((Session == null) ? null : Session.Resolve(self));
		playerSlot3 = playerSlot3 ?? playerSlot ?? playerSlot2 ?? FsmPlayer(self, out enemy);
		using (PlayerContext.Enter(playerSlot3, enemy))
		{
			using (CharmNativeUi.Enter(self))
			{
				using (Lifeblood.Enter(self))
				{
					Retarget(self, playerSlot3);
					WorldRouting.Call before = WorldRouting.Begin(self);
					try
					{
						orig(self, other);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmTriggerExit(On.HutongGames.PlayMaker.Fsm.orig_OnTriggerExit2D orig, HutongGames.PlayMaker.Fsm self, Collider2D other)
	{
		if (Session != null && Session.Active)
		{
			PlayerSlot playerSlot = Session.Resolve(other);
			if (AcidSwimming.WorldWaterContact(self.GameObject, playerSlot))
			{
				AcidSwimming.NoteBlocked(self, playerSlot);
				return;
			}
			if (!InteractionRouter.Trigger(self, other, exiting: true))
			{
				return;
			}
		}
		PlayerSlot playerSlot2 = WorldRouting.Touch(self, other, exit: true);
		bool enemy = false;
		PlayerSlot playerSlot3 = ((Session == null) ? null : Session.Resolve(self));
		playerSlot3 = playerSlot3 ?? playerSlot2 ?? FsmPlayer(self, out enemy);
		using (PlayerContext.Enter(playerSlot3, enemy))
		{
			Retarget(self, playerSlot3);
			WorldRouting.Call before = WorldRouting.Begin(self);
			try
			{
				orig(self, other);
			}
			finally
			{
				WorldRouting.End(self, before);
			}
		}
	}

	private static bool IsVitalsHud(HutongGames.PlayMaker.Fsm f)
	{
		if (!f.GameObject)
		{
			return false;
		}
		GameCameras instance = GameCameras.instance;
		if (!instance || !instance.hudCanvas || !f.GameObject.transform.IsChildOf(instance.hudCanvas.transform))
		{
			return false;
		}
		string text = (f.Name + " " + f.GameObject.name).ToLowerInvariant();
		if (!text.Contains("health") && !text.Contains("soul"))
		{
			return text.Contains("orb");
		}
		return true;
	}

	[IteratorStateMachine(typeof(__iterator__Empty_d__134))]
	private static IEnumerator Empty()
	{
		return new __iterator__Empty_d__134(0);
	}

	internal static void PatchActor(GameObject go)
	{
	}

	internal static void RebindActor(PlayerSlot p)
	{
		if (p != null && (bool)p.Hero)
		{
			using (PlayerContext.Enter(p))
			{
				RebindReferences(p.Hero.gameObject, p);
			}
		}
	}

	internal static void RebindReferences(GameObject root, PlayerSlot p)
	{
		if (Session == null || p == null)
		{
			return;
		}
		MonoBehaviour[] componentsInChildren = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
		foreach (MonoBehaviour monoBehaviour in componentsInChildren)
		{
			if ((bool)monoBehaviour && !(monoBehaviour is OwnerTag) && !(monoBehaviour is PlayMakerFSM))
			{
				RebindObject(monoBehaviour, p);
			}
		}
		PlayMakerFSM[] componentsInChildren2 = root.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
		foreach (PlayMakerFSM playMakerFSM in componentsInChildren2)
		{
			fsmBindings.Remove(playMakerFSM.Fsm);
			Retarget(playMakerFSM.Fsm, p);
		}
	}

	internal static FieldInfo[] GetReferenceFields(Type type)
	{
		if (referenceFields.TryGetValue(type, out var value))
		{
			return value;
		}
		List<FieldInfo> list = new List<FieldInfo>();
		Type type2 = type;
		while (type2 != null && type2 != typeof(MonoBehaviour) && type2 != typeof(HutongGames.PlayMaker.FsmStateAction))
		{
			FieldInfo[] fields = type2.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				Type fieldType = fieldInfo.FieldType;
				if (!fieldInfo.IsInitOnly && (fieldType == typeof(HeroControllerStates) || fieldType == typeof(PlayerData) || fieldType == typeof(HeroActions) || fieldType == typeof(HutongGames.PlayMaker.FsmEventTarget) || typeof(UnityEngine.Object).IsAssignableFrom(fieldType) || fieldType == typeof(HutongGames.PlayMaker.FsmGameObject) || fieldType == typeof(HutongGames.PlayMaker.FsmObject) || fieldType == typeof(HutongGames.PlayMaker.FsmOwnerDefault)))
				{
					list.Add(fieldInfo);
				}
			}
			type2 = type2.BaseType;
		}
		value = list.ToArray();
		referenceFields[type] = value;
		return value;
	}

	internal static UnityEngine.Object Mapped(UnityEngine.Object value, PlayerSlot p)
	{
		if (!value)
		{
			return value;
		}
		CoopSession session = Session;
		PlayerSlot playerSlot = session.Resolve(value);
		if (playerSlot == null || playerSlot == p)
		{
			return value;
		}
		GameObject gameObject = value as GameObject;
		if ((bool)gameObject)
		{
			return session.Remap(gameObject, playerSlot, p);
		}
		Component component = value as Component;
		if (!component)
		{
			return value;
		}
		GameObject gameObject2 = session.Remap(component.gameObject, playerSlot, p);
		if (!gameObject2)
		{
			return value;
		}
		PlayMakerFSM playMakerFSM = component as PlayMakerFSM;
		if ((bool)playMakerFSM)
		{
			PlayMakerFSM[] components = gameObject2.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM playMakerFSM2 in components)
			{
				if (playMakerFSM2.FsmName == playMakerFSM.FsmName)
				{
					return playMakerFSM2;
				}
			}
			return value;
		}
		Component[] components2 = component.gameObject.GetComponents(component.GetType());
		Component[] components3 = gameObject2.GetComponents(component.GetType());
		for (int j = 0; j < components2.Length; j++)
		{
			if (components2[j] == component)
			{
				if (j >= components3.Length)
				{
					return value;
				}
				return components3[j];
			}
		}
		return value;
	}

	internal static void RebindObject(object obj, PlayerSlot p)
	{
		if (PvpFamiliars.Rebind(obj, p))
		{
			return;
		}
		FieldInfo[] array = GetReferenceFields(obj.GetType());
		foreach (FieldInfo fieldInfo in array)
		{
			Type fieldType = fieldInfo.FieldType;
			if (fieldType == typeof(HeroControllerStates))
			{
				fieldInfo.SetValue(obj, p.Hero.cState);
				continue;
			}
			if (fieldType == typeof(PlayerData))
			{
				fieldInfo.SetValue(obj, Session.Data);
				continue;
			}
			if (fieldType == typeof(HeroActions))
			{
				fieldInfo.SetValue(obj, p.Actions);
				continue;
			}
			object value = fieldInfo.GetValue(obj);
			if (value is HutongGames.PlayMaker.FsmGameObject fsmGameObject)
			{
				fsmGameObject.Value = Mapped(fsmGameObject.Value, p) as GameObject;
				continue;
			}
			if (value is HutongGames.PlayMaker.FsmObject fsmObject)
			{
				fsmObject.Value = Mapped(fsmObject.Value, p);
				continue;
			}
			if (value is HutongGames.PlayMaker.FsmOwnerDefault fsmOwnerDefault)
			{
				if (fsmOwnerDefault.GameObject != null)
				{
					fsmOwnerDefault.GameObject.Value = Mapped(fsmOwnerDefault.GameObject.Value, p) as GameObject;
				}
				continue;
			}
			if (value is HutongGames.PlayMaker.FsmEventTarget fsmEventTarget)
			{
				if (fsmEventTarget.gameObject != null && fsmEventTarget.gameObject.GameObject != null)
				{
					fsmEventTarget.gameObject.GameObject.Value = Mapped(fsmEventTarget.gameObject.GameObject.Value, p) as GameObject;
				}
				if ((bool)fsmEventTarget.fsmComponent)
				{
					fsmEventTarget.fsmComponent = Mapped(fsmEventTarget.fsmComponent, p) as PlayMakerFSM;
				}
				continue;
			}
			UnityEngine.Object @object = value as UnityEngine.Object;
			if ((bool)@object)
			{
				UnityEngine.Object object2 = Mapped(@object, p);
				if (object2 != @object && object2 != null && fieldType.IsInstanceOfType(object2))
				{
					fieldInfo.SetValue(obj, object2);
				}
			}
		}
	}

	internal static void Retarget(HutongGames.PlayMaker.Fsm fsm, PlayerSlot p)
	{
		if (PvpFamiliars.Retarget(fsm, p) || p == null || !p.Hero || fsm.Variables == null)
		{
			return;
		}
		HutongGames.PlayMaker.FsmGameObject[] gameObjectVariables = fsm.Variables.GameObjectVariables;
		foreach (HutongGames.PlayMaker.FsmGameObject fsmGameObject in gameObjectVariables)
		{
			if (fsmGameObject != null)
			{
				fsmGameObject.Value = Mapped(fsmGameObject.Value, p) as GameObject;
			}
		}
		HutongGames.PlayMaker.FsmObject[] objectVariables = fsm.Variables.ObjectVariables;
		foreach (HutongGames.PlayMaker.FsmObject fsmObject in objectVariables)
		{
			if (fsmObject != null)
			{
				fsmObject.Value = Mapped(fsmObject.Value, p);
			}
		}
		FsmBinding value;
		bool flag = !fsmBindings.TryGetValue(fsm, out value) || value.Owner != p;
		if (!flag && value.State == fsm.ActiveState)
		{
			return;
		}
		if (flag)
		{
			HutongGames.PlayMaker.FsmState[] states = fsm.States;
			foreach (HutongGames.PlayMaker.FsmState fsmState in states)
			{
				if (fsmState.Actions == null)
				{
					continue;
				}
				HutongGames.PlayMaker.FsmStateAction[] actions = fsmState.Actions;
				foreach (HutongGames.PlayMaker.FsmStateAction fsmStateAction in actions)
				{
					if (fsmStateAction != null)
					{
						RebindObject(fsmStateAction, p);
					}
				}
			}
		}
		else if (fsm.ActiveState != null && fsm.ActiveState.Actions != null)
		{
			HutongGames.PlayMaker.FsmStateAction[] actions = fsm.ActiveState.Actions;
			foreach (HutongGames.PlayMaker.FsmStateAction fsmStateAction2 in actions)
			{
				if (fsmStateAction2 != null)
				{
					RebindObject(fsmStateAction2, p);
				}
			}
		}
		if (value == null)
		{
			fsmBindings[fsm] = new FsmBinding
			{
				Owner = p,
				State = fsm.ActiveState
			};
		}
		else
		{
			value.Owner = p;
			value.State = fsm.ActiveState;
		}
	}
}
