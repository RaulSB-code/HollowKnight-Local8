using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
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

		internal FsmState State;
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
	private static class _003C_003EO
	{
		public static hook_SelectCharm _003C0_003E__SelectBoard;

		public static hook_GetListNumber _003C1_003E__SelectEquipped;

		public static hook_FinishedEnteringScene _003C2_003E__Entered;

		public static hook_OnCollisionEnter2D _003C3_003E__CollisionEnter;

		public static hook_OnCollisionStay2D _003C4_003E__CollisionStay;

		public static hook_OnCollisionExit2D _003C5_003E__CollisionExit;

		public static hook_OnTriggerEnter2D _003C6_003E__HazardMarker;

		public static hook_Start _003C7_003E__BoxStart;

		public static hook_OnTriggerEnter2D _003C8_003E__BoxEnter;

		public static hook_OnTriggerStay2D _003C9_003E__BoxStay;

		public static hook_LateUpdate _003C10_003E__BoxLate;

		public static hook_AddMPCharge _003C11_003E__AddSoul;

		public static hook_SoulGain _003C12_003E__GainSoul;

		public static hook_TryAddMPChargeSpa _003C13_003E__SpaSoul;

		public static hook_SetMPCharge _003C14_003E__SetSoul;

		public static hook_TakeReserveMP _003C15_003E__TakeReserve;

		public static hook_AddHealth _003C16_003E__AddHealth;

		public static hook_StartRecoil _003C17_003E__Recoil;

		public static hook_TakeMP _003C18_003E__TakeMP;

		public static hook_TakeMPQuick _003C19_003E__TakeMPQuick;

		public static hook_OnTriggerEnter2D _003C20_003E__BenchEnter;

		public static hook_OnTriggerExit2D _003C21_003E__BenchExit;

		public static hook_OnTriggerEnter2D _003C22_003E__WalkEnter;

		public static hook_OnTriggerStay2D _003C23_003E__WalkStay;

		public static hook_OnTriggerExit2D _003C24_003E__WalkExit;

		public static hook_OnTriggerEnter2D _003C25_003E__WorldTriggerEnter;

		public static hook_OnTriggerStay2D _003C26_003E__WorldTriggerStay;

		public static hook_OnTriggerExit2D _003C27_003E__WorldTriggerExit;

		public static hook_Awake _003C28_003E__HeroAwake;

		public static hook_Start _003C29_003E__HeroStart;

		public static hook_Update _003C30_003E__HeroUpdate;

		public static hook_FixedUpdate _003C31_003E__HeroFixed;

		public static hook_IsSwimming _003C32_003E__IsSwimming;

		public static hook_SetBackOnGround _003C33_003E__SetBackOnGround;

		public static hook_BackOnGround _003C34_003E__BackOnGround;

		public static hook_Update _003C35_003E__AnimationUpdate;

		public static hook_TakeDamage _003C36_003E__Damage;

		public static hook_CharmUpdate _003C37_003E__CharmUpdate;

		public static hook_MaxHealth _003C38_003E__MaxHealth;

		public static hook_SetInt _003C39_003E__SetInt;

		public static hook_IntAdd _003C40_003E__IntAdd;

		public static hook_IncrementInt _003C41_003E__IncrementInt;

		public static hook_SetBool _003C42_003E__SetBool;

		public static hook_GetBool _003C43_003E__GetBool;

		public static hook_Die _003C44_003E__Die;

		public static hook_DieFromHazard _003C45_003E__Hazard;

		public static hook_Hit _003C46_003E__Hit;

		public static hook_Die _003C47_003E__EnemyDie;

		public static hook_LateUpdate _003C48_003E__CameraLate;

		public static hook_ApplyMusicCue _003C49_003E__MusicCueChange;

		public static hook_OnTriggerEnter2D _003C50_003E__TransitionEnter;

		public static hook_OnTriggerStay2D _003C51_003E__TransitionStay;

		public static hook_OnTriggerEnter2D _003C52_003E__CameraEnter;

		public static hook_OnTriggerStay2D _003C53_003E__CameraStay;

		public static hook_OnTriggerExit2D _003C54_003E__CameraExit;

		public static hook_BeginSceneTransition _003C55_003E__BeginTransition;

		public static hook_LoadScene _003C56_003E__LoadScene;

		public static hook_PauseGameToggle _003C57_003E__PauseToggle;

		public static hook_PlayerDead _003C58_003E__PlayerDead;

		public static hook_PlayerDeadFromHazard _003C59_003E__PlayerDeadHazard;

		public static hook_SaveGame _003C60_003E__SaveGame;

		public static hook_Update _003C61_003E__InputUpdate;

		public static hook_Spawn_GameObject_Transform_Vector3_Quaternion _003C62_003E__PoolSpawn;

		public static hook_Update _003C63_003E__EnemyUpdate;

		public static hook_FixedUpdate _003C64_003E__HatchlingFixed;

		public static hook_Spawn _003C65_003E__HatchlingSpawn;

		public static hook_TeleEnd _003C66_003E__HatchlingTeleEnd;

		public static hook_OnEnable _003C67_003E__SpellOrb;

		public static hook_Awake _003C68_003E__FsmAwake;

		public static hook_OnEnable _003C69_003E__FsmEnable;

		public static hook_Start _003C70_003E__FsmStart;

		public static hook_OnEnter _003C71_003E__StateEnter;

		public static hook_Update _003C72_003E__FsmUpdate;

		public static hook_FixedUpdate _003C73_003E__FsmFixed;

		public static hook_LateUpdate _003C74_003E__FsmLate;

		public static hook_ProcessEvent _003C75_003E__FsmEvent;

		public static hook_OnTriggerEnter2D _003C76_003E__FsmTriggerEnter;

		public static hook_OnTriggerStay2D _003C77_003E__FsmTriggerStay;

		public static hook_OnTriggerExit2D _003C78_003E__FsmTriggerExit;

		public static hook_OnCollisionEnter2D _003C79_003E__WaterCollisionEnter;

		public static hook_OnCollisionStay2D _003C80_003E__WaterCollisionStay;

		public static hook_OnCollisionExit2D _003C81_003E__WaterCollisionExit;

		public static hook_CheckForInput _003C82_003E__ListenUp;

		public static hook_CheckForInput _003C83_003E__ListenDown;

		public static hook_OnEnter _003C84_003E__InteractionCall;

		public static hook_OnEnter _003C85_003E__CreatePickupCard;

		public static hook_OnUpdate _003C86_003E__PickupJump;

		public static hook_OnUpdate _003C87_003E__PickupMenuActions;

		public static hook_OnUpdate _003C88_003E__PickupMenuSubmit;

		public static hook_OnUpdate _003C89_003E__PickupMenuCancel;

		public static hook_OnUpdate _003C90_003E__QuickMapUpdate;

		public static hook_Update _003C91_003E__MapUpdate;

		public static hook_OnEnter _003C92_003E__SpellPoolAction;

		public static hook_OnEnter _003C93_003E__SpellCreateAction;
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

	private static readonly Dictionary<Fsm, EnemyBinding> enemies = new Dictionary<Fsm, EnemyBinding>();

	private static readonly Dictionary<Fsm, FsmBinding> fsmBindings = new Dictionary<Fsm, FsmBinding>();

	private static readonly Dictionary<PlayerSlot, HashSet<WalkArea>> walkAreas = new Dictionary<PlayerSlot, HashSet<WalkArea>>();

	private static readonly HashSet<string> localEvents = new HashSet<string> { "HERO DAMAGED", "HERO HEALED", "HERO LANDED", "HERO DEATH", "HERO DEAD", "FOCUS COMPLETED", "HERO RECOIL", "HERO DASH" };

	private static CoopSession Session
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
			object obj = _003C_003EO._003C0_003E__SelectBoard;
			if (obj == null)
			{
				hook_SelectCharm val = SelectBoard;
				_003C_003EO._003C0_003E__SelectBoard = val;
				obj = (object)val;
			}
			InvCharmBackboard.SelectCharm += (hook_SelectCharm)obj;
			object obj2 = _003C_003EO._003C1_003E__SelectEquipped;
			if (obj2 == null)
			{
				hook_GetListNumber val2 = SelectEquipped;
				_003C_003EO._003C1_003E__SelectEquipped = val2;
				obj2 = (object)val2;
			}
			CharmItem.GetListNumber += (hook_GetListNumber)obj2;
			object obj3 = _003C_003EO._003C2_003E__Entered;
			if (obj3 == null)
			{
				hook_FinishedEnteringScene val3 = Entered;
				_003C_003EO._003C2_003E__Entered = val3;
				obj3 = (object)val3;
			}
			HeroController.FinishedEnteringScene += (hook_FinishedEnteringScene)obj3;
			object obj4 = _003C_003EO._003C3_003E__CollisionEnter;
			if (obj4 == null)
			{
				hook_OnCollisionEnter2D val4 = CollisionEnter;
				_003C_003EO._003C3_003E__CollisionEnter = val4;
				obj4 = (object)val4;
			}
			HeroController.OnCollisionEnter2D += (hook_OnCollisionEnter2D)obj4;
			object obj5 = _003C_003EO._003C4_003E__CollisionStay;
			if (obj5 == null)
			{
				hook_OnCollisionStay2D val5 = CollisionStay;
				_003C_003EO._003C4_003E__CollisionStay = val5;
				obj5 = (object)val5;
			}
			HeroController.OnCollisionStay2D += (hook_OnCollisionStay2D)obj5;
			object obj6 = _003C_003EO._003C5_003E__CollisionExit;
			if (obj6 == null)
			{
				hook_OnCollisionExit2D val6 = CollisionExit;
				_003C_003EO._003C5_003E__CollisionExit = val6;
				obj6 = (object)val6;
			}
			HeroController.OnCollisionExit2D += (hook_OnCollisionExit2D)obj6;
			object obj7 = _003C_003EO._003C6_003E__HazardMarker;
			if (obj7 == null)
			{
				hook_OnTriggerEnter2D val7 = HazardMarker;
				_003C_003EO._003C6_003E__HazardMarker = val7;
				obj7 = (object)val7;
			}
			HazardRespawnTrigger.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj7;
			object obj8 = _003C_003EO._003C7_003E__BoxStart;
			if (obj8 == null)
			{
				hook_Start val8 = BoxStart;
				_003C_003EO._003C7_003E__BoxStart = val8;
				obj8 = (object)val8;
			}
			HeroBox.Start += (hook_Start)obj8;
			object obj9 = _003C_003EO._003C8_003E__BoxEnter;
			if (obj9 == null)
			{
				hook_OnTriggerEnter2D val9 = BoxEnter;
				_003C_003EO._003C8_003E__BoxEnter = val9;
				obj9 = (object)val9;
			}
			HeroBox.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj9;
			object obj10 = _003C_003EO._003C9_003E__BoxStay;
			if (obj10 == null)
			{
				hook_OnTriggerStay2D val10 = BoxStay;
				_003C_003EO._003C9_003E__BoxStay = val10;
				obj10 = (object)val10;
			}
			HeroBox.OnTriggerStay2D += (hook_OnTriggerStay2D)obj10;
			object obj11 = _003C_003EO._003C10_003E__BoxLate;
			if (obj11 == null)
			{
				hook_LateUpdate val11 = BoxLate;
				_003C_003EO._003C10_003E__BoxLate = val11;
				obj11 = (object)val11;
			}
			HeroBox.LateUpdate += (hook_LateUpdate)obj11;
			object obj12 = _003C_003EO._003C11_003E__AddSoul;
			if (obj12 == null)
			{
				hook_AddMPCharge val12 = AddSoul;
				_003C_003EO._003C11_003E__AddSoul = val12;
				obj12 = (object)val12;
			}
			HeroController.AddMPCharge += (hook_AddMPCharge)obj12;
			object obj13 = _003C_003EO._003C12_003E__GainSoul;
			if (obj13 == null)
			{
				hook_SoulGain val13 = GainSoul;
				_003C_003EO._003C12_003E__GainSoul = val13;
				obj13 = (object)val13;
			}
			HeroController.SoulGain += (hook_SoulGain)obj13;
			object obj14 = _003C_003EO._003C13_003E__SpaSoul;
			if (obj14 == null)
			{
				hook_TryAddMPChargeSpa val14 = SpaSoul;
				_003C_003EO._003C13_003E__SpaSoul = val14;
				obj14 = (object)val14;
			}
			HeroController.TryAddMPChargeSpa += (hook_TryAddMPChargeSpa)obj14;
			object obj15 = _003C_003EO._003C14_003E__SetSoul;
			if (obj15 == null)
			{
				hook_SetMPCharge val15 = SetSoul;
				_003C_003EO._003C14_003E__SetSoul = val15;
				obj15 = (object)val15;
			}
			HeroController.SetMPCharge += (hook_SetMPCharge)obj15;
			object obj16 = _003C_003EO._003C15_003E__TakeReserve;
			if (obj16 == null)
			{
				hook_TakeReserveMP val16 = TakeReserve;
				_003C_003EO._003C15_003E__TakeReserve = val16;
				obj16 = (object)val16;
			}
			HeroController.TakeReserveMP += (hook_TakeReserveMP)obj16;
			object obj17 = _003C_003EO._003C16_003E__AddHealth;
			if (obj17 == null)
			{
				hook_AddHealth val17 = AddHealth;
				_003C_003EO._003C16_003E__AddHealth = val17;
				obj17 = (object)val17;
			}
			HeroController.AddHealth += (hook_AddHealth)obj17;
			object obj18 = _003C_003EO._003C17_003E__Recoil;
			if (obj18 == null)
			{
				hook_StartRecoil val18 = Recoil;
				_003C_003EO._003C17_003E__Recoil = val18;
				obj18 = (object)val18;
			}
			HeroController.StartRecoil += (hook_StartRecoil)obj18;
			object obj19 = _003C_003EO._003C18_003E__TakeMP;
			if (obj19 == null)
			{
				hook_TakeMP val19 = TakeMP;
				_003C_003EO._003C18_003E__TakeMP = val19;
				obj19 = (object)val19;
			}
			HeroController.TakeMP += (hook_TakeMP)obj19;
			object obj20 = _003C_003EO._003C19_003E__TakeMPQuick;
			if (obj20 == null)
			{
				hook_TakeMPQuick val20 = TakeMPQuick;
				_003C_003EO._003C19_003E__TakeMPQuick = val20;
				obj20 = (object)val20;
			}
			HeroController.TakeMPQuick += (hook_TakeMPQuick)obj20;
			object obj21 = _003C_003EO._003C20_003E__BenchEnter;
			if (obj21 == null)
			{
				hook_OnTriggerEnter2D val21 = BenchEnter;
				_003C_003EO._003C20_003E__BenchEnter = val21;
				obj21 = (object)val21;
			}
			RestBench.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj21;
			object obj22 = _003C_003EO._003C21_003E__BenchExit;
			if (obj22 == null)
			{
				hook_OnTriggerExit2D val22 = BenchExit;
				_003C_003EO._003C21_003E__BenchExit = val22;
				obj22 = (object)val22;
			}
			RestBench.OnTriggerExit2D += (hook_OnTriggerExit2D)obj22;
			object obj23 = _003C_003EO._003C22_003E__WalkEnter;
			if (obj23 == null)
			{
				hook_OnTriggerEnter2D val23 = WalkEnter;
				_003C_003EO._003C22_003E__WalkEnter = val23;
				obj23 = (object)val23;
			}
			WalkArea.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj23;
			object obj24 = _003C_003EO._003C23_003E__WalkStay;
			if (obj24 == null)
			{
				hook_OnTriggerStay2D val24 = WalkStay;
				_003C_003EO._003C23_003E__WalkStay = val24;
				obj24 = (object)val24;
			}
			WalkArea.OnTriggerStay2D += (hook_OnTriggerStay2D)obj24;
			object obj25 = _003C_003EO._003C24_003E__WalkExit;
			if (obj25 == null)
			{
				hook_OnTriggerExit2D val25 = WalkExit;
				_003C_003EO._003C24_003E__WalkExit = val25;
				obj25 = (object)val25;
			}
			WalkArea.OnTriggerExit2D += (hook_OnTriggerExit2D)obj25;
			object obj26 = _003C_003EO._003C25_003E__WorldTriggerEnter;
			if (obj26 == null)
			{
				hook_OnTriggerEnter2D val26 = WorldTriggerEnter;
				_003C_003EO._003C25_003E__WorldTriggerEnter = val26;
				obj26 = (object)val26;
			}
			TriggerEnterEvent.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj26;
			object obj27 = _003C_003EO._003C26_003E__WorldTriggerStay;
			if (obj27 == null)
			{
				hook_OnTriggerStay2D val27 = WorldTriggerStay;
				_003C_003EO._003C26_003E__WorldTriggerStay = val27;
				obj27 = (object)val27;
			}
			TriggerEnterEvent.OnTriggerStay2D += (hook_OnTriggerStay2D)obj27;
			object obj28 = _003C_003EO._003C27_003E__WorldTriggerExit;
			if (obj28 == null)
			{
				hook_OnTriggerExit2D val28 = WorldTriggerExit;
				_003C_003EO._003C27_003E__WorldTriggerExit = val28;
				obj28 = (object)val28;
			}
			TriggerEnterEvent.OnTriggerExit2D += (hook_OnTriggerExit2D)obj28;
			object obj29 = _003C_003EO._003C28_003E__HeroAwake;
			if (obj29 == null)
			{
				hook_Awake val29 = HeroAwake;
				_003C_003EO._003C28_003E__HeroAwake = val29;
				obj29 = (object)val29;
			}
			HeroController.Awake += (hook_Awake)obj29;
			object obj30 = _003C_003EO._003C29_003E__HeroStart;
			if (obj30 == null)
			{
				hook_Start val30 = HeroStart;
				_003C_003EO._003C29_003E__HeroStart = val30;
				obj30 = (object)val30;
			}
			HeroController.Start += (hook_Start)obj30;
			object obj31 = _003C_003EO._003C30_003E__HeroUpdate;
			if (obj31 == null)
			{
				hook_Update val31 = HeroUpdate;
				_003C_003EO._003C30_003E__HeroUpdate = val31;
				obj31 = (object)val31;
			}
			HeroController.Update += (hook_Update)obj31;
			object obj32 = _003C_003EO._003C31_003E__HeroFixed;
			if (obj32 == null)
			{
				hook_FixedUpdate val32 = HeroFixed;
				_003C_003EO._003C31_003E__HeroFixed = val32;
				obj32 = (object)val32;
			}
			HeroController.FixedUpdate += (hook_FixedUpdate)obj32;
			object obj33 = _003C_003EO._003C32_003E__IsSwimming;
			if (obj33 == null)
			{
				hook_IsSwimming val33 = IsSwimming;
				_003C_003EO._003C32_003E__IsSwimming = val33;
				obj33 = (object)val33;
			}
			HeroController.IsSwimming += (hook_IsSwimming)obj33;
			object obj34 = _003C_003EO._003C33_003E__SetBackOnGround;
			if (obj34 == null)
			{
				hook_SetBackOnGround val34 = SetBackOnGround;
				_003C_003EO._003C33_003E__SetBackOnGround = val34;
				obj34 = (object)val34;
			}
			HeroController.SetBackOnGround += (hook_SetBackOnGround)obj34;
			object obj35 = _003C_003EO._003C34_003E__BackOnGround;
			if (obj35 == null)
			{
				hook_BackOnGround val35 = BackOnGround;
				_003C_003EO._003C34_003E__BackOnGround = val35;
				obj35 = (object)val35;
			}
			HeroController.BackOnGround += (hook_BackOnGround)obj35;
			object obj36 = _003C_003EO._003C35_003E__AnimationUpdate;
			if (obj36 == null)
			{
				hook_Update val36 = AnimationUpdate;
				_003C_003EO._003C35_003E__AnimationUpdate = val36;
				obj36 = (object)val36;
			}
			HeroAnimationController.Update += (hook_Update)obj36;
			object obj37 = _003C_003EO._003C36_003E__Damage;
			if (obj37 == null)
			{
				hook_TakeDamage val37 = Damage;
				_003C_003EO._003C36_003E__Damage = val37;
				obj37 = (object)val37;
			}
			HeroController.TakeDamage += (hook_TakeDamage)obj37;
			object obj38 = _003C_003EO._003C37_003E__CharmUpdate;
			if (obj38 == null)
			{
				hook_CharmUpdate val38 = CharmUpdate;
				_003C_003EO._003C37_003E__CharmUpdate = val38;
				obj38 = (object)val38;
			}
			HeroController.CharmUpdate += (hook_CharmUpdate)obj38;
			object obj39 = _003C_003EO._003C38_003E__MaxHealth;
			if (obj39 == null)
			{
				hook_MaxHealth val39 = MaxHealth;
				_003C_003EO._003C38_003E__MaxHealth = val39;
				obj39 = (object)val39;
			}
			HeroController.MaxHealth += (hook_MaxHealth)obj39;
			object obj40 = _003C_003EO._003C39_003E__SetInt;
			if (obj40 == null)
			{
				hook_SetInt val40 = SetInt;
				_003C_003EO._003C39_003E__SetInt = val40;
				obj40 = (object)val40;
			}
			PlayerData.SetInt += (hook_SetInt)obj40;
			object obj41 = _003C_003EO._003C40_003E__IntAdd;
			if (obj41 == null)
			{
				hook_IntAdd val41 = IntAdd;
				_003C_003EO._003C40_003E__IntAdd = val41;
				obj41 = (object)val41;
			}
			PlayerData.IntAdd += (hook_IntAdd)obj41;
			object obj42 = _003C_003EO._003C41_003E__IncrementInt;
			if (obj42 == null)
			{
				hook_IncrementInt val42 = IncrementInt;
				_003C_003EO._003C41_003E__IncrementInt = val42;
				obj42 = (object)val42;
			}
			PlayerData.IncrementInt += (hook_IncrementInt)obj42;
			object obj43 = _003C_003EO._003C42_003E__SetBool;
			if (obj43 == null)
			{
				hook_SetBool val43 = SetBool;
				_003C_003EO._003C42_003E__SetBool = val43;
				obj43 = (object)val43;
			}
			PlayerData.SetBool += (hook_SetBool)obj43;
			object obj44 = _003C_003EO._003C43_003E__GetBool;
			if (obj44 == null)
			{
				hook_GetBool val44 = GetBool;
				_003C_003EO._003C43_003E__GetBool = val44;
				obj44 = (object)val44;
			}
			PlayerData.GetBool += (hook_GetBool)obj44;
			object obj45 = _003C_003EO._003C44_003E__Die;
			if (obj45 == null)
			{
				hook_Die val45 = Die;
				_003C_003EO._003C44_003E__Die = val45;
				obj45 = (object)val45;
			}
			HeroController.Die += (hook_Die)obj45;
			object obj46 = _003C_003EO._003C45_003E__Hazard;
			if (obj46 == null)
			{
				hook_DieFromHazard val46 = Hazard;
				_003C_003EO._003C45_003E__Hazard = val46;
				obj46 = (object)val46;
			}
			HeroController.DieFromHazard += (hook_DieFromHazard)obj46;
			object obj47 = _003C_003EO._003C46_003E__Hit;
			if (obj47 == null)
			{
				hook_Hit val47 = Hit;
				_003C_003EO._003C46_003E__Hit = val47;
				obj47 = (object)val47;
			}
			HealthManager.Hit += (hook_Hit)obj47;
			object obj48 = _003C_003EO._003C47_003E__EnemyDie;
			if (obj48 == null)
			{
				hook_Die val48 = EnemyDie;
				_003C_003EO._003C47_003E__EnemyDie = val48;
				obj48 = (object)val48;
			}
			HealthManager.Die += (hook_Die)obj48;
			object obj49 = _003C_003EO._003C48_003E__CameraLate;
			if (obj49 == null)
			{
				hook_LateUpdate val49 = CameraLate;
				_003C_003EO._003C48_003E__CameraLate = val49;
				obj49 = (object)val49;
			}
			CameraController.LateUpdate += (hook_LateUpdate)obj49;
			object obj50 = _003C_003EO._003C49_003E__MusicCueChange;
			if (obj50 == null)
			{
				hook_ApplyMusicCue val50 = MusicCueChange;
				_003C_003EO._003C49_003E__MusicCueChange = val50;
				obj50 = (object)val50;
			}
			AudioManager.ApplyMusicCue += (hook_ApplyMusicCue)obj50;
			object obj51 = _003C_003EO._003C50_003E__TransitionEnter;
			if (obj51 == null)
			{
				hook_OnTriggerEnter2D val51 = TransitionEnter;
				_003C_003EO._003C50_003E__TransitionEnter = val51;
				obj51 = (object)val51;
			}
			TransitionPoint.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj51;
			object obj52 = _003C_003EO._003C51_003E__TransitionStay;
			if (obj52 == null)
			{
				hook_OnTriggerStay2D val52 = TransitionStay;
				_003C_003EO._003C51_003E__TransitionStay = val52;
				obj52 = (object)val52;
			}
			TransitionPoint.OnTriggerStay2D += (hook_OnTriggerStay2D)obj52;
			object obj53 = _003C_003EO._003C52_003E__CameraEnter;
			if (obj53 == null)
			{
				hook_OnTriggerEnter2D val53 = CameraEnter;
				_003C_003EO._003C52_003E__CameraEnter = val53;
				obj53 = (object)val53;
			}
			CameraLockArea.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj53;
			object obj54 = _003C_003EO._003C53_003E__CameraStay;
			if (obj54 == null)
			{
				hook_OnTriggerStay2D val54 = CameraStay;
				_003C_003EO._003C53_003E__CameraStay = val54;
				obj54 = (object)val54;
			}
			CameraLockArea.OnTriggerStay2D += (hook_OnTriggerStay2D)obj54;
			object obj55 = _003C_003EO._003C54_003E__CameraExit;
			if (obj55 == null)
			{
				hook_OnTriggerExit2D val55 = CameraExit;
				_003C_003EO._003C54_003E__CameraExit = val55;
				obj55 = (object)val55;
			}
			CameraLockArea.OnTriggerExit2D += (hook_OnTriggerExit2D)obj55;
			object obj56 = _003C_003EO._003C55_003E__BeginTransition;
			if (obj56 == null)
			{
				hook_BeginSceneTransition val56 = BeginTransition;
				_003C_003EO._003C55_003E__BeginTransition = val56;
				obj56 = (object)val56;
			}
			GameManager.BeginSceneTransition += (hook_BeginSceneTransition)obj56;
			object obj57 = _003C_003EO._003C56_003E__LoadScene;
			if (obj57 == null)
			{
				hook_LoadScene val57 = LoadScene;
				_003C_003EO._003C56_003E__LoadScene = val57;
				obj57 = (object)val57;
			}
			GameManager.LoadScene += (hook_LoadScene)obj57;
			object obj58 = _003C_003EO._003C57_003E__PauseToggle;
			if (obj58 == null)
			{
				hook_PauseGameToggle val58 = PauseToggle;
				_003C_003EO._003C57_003E__PauseToggle = val58;
				obj58 = (object)val58;
			}
			GameManager.PauseGameToggle += (hook_PauseGameToggle)obj58;
			object obj59 = _003C_003EO._003C58_003E__PlayerDead;
			if (obj59 == null)
			{
				hook_PlayerDead val59 = PlayerDead;
				_003C_003EO._003C58_003E__PlayerDead = val59;
				obj59 = (object)val59;
			}
			GameManager.PlayerDead += (hook_PlayerDead)obj59;
			object obj60 = _003C_003EO._003C59_003E__PlayerDeadHazard;
			if (obj60 == null)
			{
				hook_PlayerDeadFromHazard val60 = PlayerDeadHazard;
				_003C_003EO._003C59_003E__PlayerDeadHazard = val60;
				obj60 = (object)val60;
			}
			GameManager.PlayerDeadFromHazard += (hook_PlayerDeadFromHazard)obj60;
			object obj61 = _003C_003EO._003C60_003E__SaveGame;
			if (obj61 == null)
			{
				hook_SaveGame val61 = SaveGame;
				_003C_003EO._003C60_003E__SaveGame = val61;
				obj61 = (object)val61;
			}
			GameManager.SaveGame += (hook_SaveGame)obj61;
			object obj62 = _003C_003EO._003C61_003E__InputUpdate;
			if (obj62 == null)
			{
				hook_Update val62 = InputUpdate;
				_003C_003EO._003C61_003E__InputUpdate = val62;
				obj62 = (object)val62;
			}
			InputHandler.Update += (hook_Update)obj62;
			object obj63 = _003C_003EO._003C62_003E__PoolSpawn;
			if (obj63 == null)
			{
				hook_Spawn_GameObject_Transform_Vector3_Quaternion val63 = PoolSpawn;
				_003C_003EO._003C62_003E__PoolSpawn = val63;
				obj63 = (object)val63;
			}
			ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion += (hook_Spawn_GameObject_Transform_Vector3_Quaternion)obj63;
			object obj64 = _003C_003EO._003C63_003E__EnemyUpdate;
			if (obj64 == null)
			{
				hook_Update val64 = EnemyUpdate;
				_003C_003EO._003C63_003E__EnemyUpdate = val64;
				obj64 = (object)val64;
			}
			LineOfSightDetector.Update += (hook_Update)obj64;
			object obj65 = _003C_003EO._003C64_003E__HatchlingFixed;
			if (obj65 == null)
			{
				hook_FixedUpdate val65 = HatchlingFixed;
				_003C_003EO._003C64_003E__HatchlingFixed = val65;
				obj65 = (object)val65;
			}
			KnightHatchling.FixedUpdate += (hook_FixedUpdate)obj65;
			object obj66 = _003C_003EO._003C65_003E__HatchlingSpawn;
			if (obj66 == null)
			{
				hook_Spawn val66 = HatchlingSpawn;
				_003C_003EO._003C65_003E__HatchlingSpawn = val66;
				obj66 = (object)val66;
			}
			KnightHatchling.Spawn += (hook_Spawn)obj66;
			object obj67 = _003C_003EO._003C66_003E__HatchlingTeleEnd;
			if (obj67 == null)
			{
				hook_TeleEnd val67 = HatchlingTeleEnd;
				_003C_003EO._003C66_003E__HatchlingTeleEnd = val67;
				obj67 = (object)val67;
			}
			KnightHatchling.TeleEnd += (hook_TeleEnd)obj67;
			object obj68 = _003C_003EO._003C67_003E__SpellOrb;
			if (obj68 == null)
			{
				hook_OnEnable val68 = SpellOrb;
				_003C_003EO._003C67_003E__SpellOrb = val68;
				obj68 = (object)val68;
			}
			SpellGetOrb.OnEnable += (hook_OnEnable)obj68;
			object obj69 = _003C_003EO._003C68_003E__FsmAwake;
			if (obj69 == null)
			{
				hook_Awake val69 = FsmAwake;
				_003C_003EO._003C68_003E__FsmAwake = val69;
				obj69 = (object)val69;
			}
			Fsm.Awake += (hook_Awake)obj69;
			object obj70 = _003C_003EO._003C69_003E__FsmEnable;
			if (obj70 == null)
			{
				hook_OnEnable val70 = FsmEnable;
				_003C_003EO._003C69_003E__FsmEnable = val70;
				obj70 = (object)val70;
			}
			Fsm.OnEnable += (hook_OnEnable)obj70;
			object obj71 = _003C_003EO._003C70_003E__FsmStart;
			if (obj71 == null)
			{
				hook_Start val71 = FsmStart;
				_003C_003EO._003C70_003E__FsmStart = val71;
				obj71 = (object)val71;
			}
			Fsm.Start += (hook_Start)obj71;
			object obj72 = _003C_003EO._003C71_003E__StateEnter;
			if (obj72 == null)
			{
				hook_OnEnter val72 = StateEnter;
				_003C_003EO._003C71_003E__StateEnter = val72;
				obj72 = (object)val72;
			}
			FsmState.OnEnter += (hook_OnEnter)obj72;
			object obj73 = _003C_003EO._003C72_003E__FsmUpdate;
			if (obj73 == null)
			{
				hook_Update val73 = FsmUpdate;
				_003C_003EO._003C72_003E__FsmUpdate = val73;
				obj73 = (object)val73;
			}
			Fsm.Update += (hook_Update)obj73;
			object obj74 = _003C_003EO._003C73_003E__FsmFixed;
			if (obj74 == null)
			{
				hook_FixedUpdate val74 = FsmFixed;
				_003C_003EO._003C73_003E__FsmFixed = val74;
				obj74 = (object)val74;
			}
			Fsm.FixedUpdate += (hook_FixedUpdate)obj74;
			object obj75 = _003C_003EO._003C74_003E__FsmLate;
			if (obj75 == null)
			{
				hook_LateUpdate val75 = FsmLate;
				_003C_003EO._003C74_003E__FsmLate = val75;
				obj75 = (object)val75;
			}
			Fsm.LateUpdate += (hook_LateUpdate)obj75;
			object obj76 = _003C_003EO._003C75_003E__FsmEvent;
			if (obj76 == null)
			{
				hook_ProcessEvent val76 = FsmEvent;
				_003C_003EO._003C75_003E__FsmEvent = val76;
				obj76 = (object)val76;
			}
			Fsm.ProcessEvent += (hook_ProcessEvent)obj76;
			object obj77 = _003C_003EO._003C76_003E__FsmTriggerEnter;
			if (obj77 == null)
			{
				hook_OnTriggerEnter2D val77 = FsmTriggerEnter;
				_003C_003EO._003C76_003E__FsmTriggerEnter = val77;
				obj77 = (object)val77;
			}
			Fsm.OnTriggerEnter2D += (hook_OnTriggerEnter2D)obj77;
			object obj78 = _003C_003EO._003C77_003E__FsmTriggerStay;
			if (obj78 == null)
			{
				hook_OnTriggerStay2D val78 = FsmTriggerStay;
				_003C_003EO._003C77_003E__FsmTriggerStay = val78;
				obj78 = (object)val78;
			}
			Fsm.OnTriggerStay2D += (hook_OnTriggerStay2D)obj78;
			object obj79 = _003C_003EO._003C78_003E__FsmTriggerExit;
			if (obj79 == null)
			{
				hook_OnTriggerExit2D val79 = FsmTriggerExit;
				_003C_003EO._003C78_003E__FsmTriggerExit = val79;
				obj79 = (object)val79;
			}
			Fsm.OnTriggerExit2D += (hook_OnTriggerExit2D)obj79;
			object obj80 = _003C_003EO._003C79_003E__WaterCollisionEnter;
			if (obj80 == null)
			{
				hook_OnCollisionEnter2D val80 = WaterCollisionEnter;
				_003C_003EO._003C79_003E__WaterCollisionEnter = val80;
				obj80 = (object)val80;
			}
			Fsm.OnCollisionEnter2D += (hook_OnCollisionEnter2D)obj80;
			object obj81 = _003C_003EO._003C80_003E__WaterCollisionStay;
			if (obj81 == null)
			{
				hook_OnCollisionStay2D val81 = WaterCollisionStay;
				_003C_003EO._003C80_003E__WaterCollisionStay = val81;
				obj81 = (object)val81;
			}
			Fsm.OnCollisionStay2D += (hook_OnCollisionStay2D)obj81;
			object obj82 = _003C_003EO._003C81_003E__WaterCollisionExit;
			if (obj82 == null)
			{
				hook_OnCollisionExit2D val82 = WaterCollisionExit;
				_003C_003EO._003C81_003E__WaterCollisionExit = val82;
				obj82 = (object)val82;
			}
			Fsm.OnCollisionExit2D += (hook_OnCollisionExit2D)obj82;
			object obj83 = _003C_003EO._003C82_003E__ListenUp;
			if (obj83 == null)
			{
				hook_CheckForInput val83 = ListenUp;
				_003C_003EO._003C82_003E__ListenUp = val83;
				obj83 = (object)val83;
			}
			ListenForUp.CheckForInput += (hook_CheckForInput)obj83;
			object obj84 = _003C_003EO._003C83_003E__ListenDown;
			if (obj84 == null)
			{
				hook_CheckForInput val84 = ListenDown;
				_003C_003EO._003C83_003E__ListenDown = val84;
				obj84 = (object)val84;
			}
			ListenForDown.CheckForInput += (hook_CheckForInput)obj84;
			object obj85 = _003C_003EO._003C84_003E__InteractionCall;
			if (obj85 == null)
			{
				hook_OnEnter val85 = InteractionCall;
				_003C_003EO._003C84_003E__InteractionCall = val85;
				obj85 = (object)val85;
			}
			CallMethodProper.OnEnter += (hook_OnEnter)obj85;
			object obj86 = _003C_003EO._003C85_003E__CreatePickupCard;
			if (obj86 == null)
			{
				hook_OnEnter val86 = CreatePickupCard;
				_003C_003EO._003C85_003E__CreatePickupCard = val86;
				obj86 = (object)val86;
			}
			CreateUIMsgGetItem.OnEnter += (hook_OnEnter)obj86;
			object obj87 = _003C_003EO._003C86_003E__PickupJump;
			if (obj87 == null)
			{
				hook_OnUpdate val87 = PickupJump;
				_003C_003EO._003C86_003E__PickupJump = val87;
				obj87 = (object)val87;
			}
			ListenForJump.OnUpdate += (hook_OnUpdate)obj87;
			object obj88 = _003C_003EO._003C87_003E__PickupMenuActions;
			if (obj88 == null)
			{
				hook_OnUpdate val88 = PickupMenuActions;
				_003C_003EO._003C87_003E__PickupMenuActions = val88;
				obj88 = (object)val88;
			}
			ListenForMenuActions.OnUpdate += (hook_OnUpdate)obj88;
			object obj89 = _003C_003EO._003C88_003E__PickupMenuSubmit;
			if (obj89 == null)
			{
				hook_OnUpdate val89 = PickupMenuSubmit;
				_003C_003EO._003C88_003E__PickupMenuSubmit = val89;
				obj89 = (object)val89;
			}
			ListenForMenuSubmit.OnUpdate += (hook_OnUpdate)obj89;
			object obj90 = _003C_003EO._003C89_003E__PickupMenuCancel;
			if (obj90 == null)
			{
				hook_OnUpdate val90 = PickupMenuCancel;
				_003C_003EO._003C89_003E__PickupMenuCancel = val90;
				obj90 = (object)val90;
			}
			ListenForMenuCancel.OnUpdate += (hook_OnUpdate)obj90;
			object obj91 = _003C_003EO._003C90_003E__QuickMapUpdate;
			if (obj91 == null)
			{
				hook_OnUpdate val91 = QuickMapUpdate;
				_003C_003EO._003C90_003E__QuickMapUpdate = val91;
				obj91 = (object)val91;
			}
			ListenForQuickMap.OnUpdate += (hook_OnUpdate)obj91;
			object obj92 = _003C_003EO._003C91_003E__MapUpdate;
			if (obj92 == null)
			{
				hook_Update val92 = MapUpdate;
				_003C_003EO._003C91_003E__MapUpdate = val92;
				obj92 = (object)val92;
			}
			GameMap.Update += (hook_Update)obj92;
			object obj93 = _003C_003EO._003C92_003E__SpellPoolAction;
			if (obj93 == null)
			{
				hook_OnEnter val93 = SpellPoolAction;
				_003C_003EO._003C92_003E__SpellPoolAction = val93;
				obj93 = (object)val93;
			}
			SpawnObjectFromGlobalPool.OnEnter += (hook_OnEnter)obj93;
			object obj94 = _003C_003EO._003C93_003E__SpellCreateAction;
			if (obj94 == null)
			{
				hook_OnEnter val94 = SpellCreateAction;
				_003C_003EO._003C93_003E__SpellCreateAction = val94;
				obj94 = (object)val94;
			}
			CreateObject.OnEnter += (hook_OnEnter)obj94;
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
			object obj = _003C_003EO._003C0_003E__SelectBoard;
			if (obj == null)
			{
				hook_SelectCharm val = SelectBoard;
				_003C_003EO._003C0_003E__SelectBoard = val;
				obj = (object)val;
			}
			InvCharmBackboard.SelectCharm -= (hook_SelectCharm)obj;
			object obj2 = _003C_003EO._003C1_003E__SelectEquipped;
			if (obj2 == null)
			{
				hook_GetListNumber val2 = SelectEquipped;
				_003C_003EO._003C1_003E__SelectEquipped = val2;
				obj2 = (object)val2;
			}
			CharmItem.GetListNumber -= (hook_GetListNumber)obj2;
			object obj3 = _003C_003EO._003C2_003E__Entered;
			if (obj3 == null)
			{
				hook_FinishedEnteringScene val3 = Entered;
				_003C_003EO._003C2_003E__Entered = val3;
				obj3 = (object)val3;
			}
			HeroController.FinishedEnteringScene -= (hook_FinishedEnteringScene)obj3;
			object obj4 = _003C_003EO._003C3_003E__CollisionEnter;
			if (obj4 == null)
			{
				hook_OnCollisionEnter2D val4 = CollisionEnter;
				_003C_003EO._003C3_003E__CollisionEnter = val4;
				obj4 = (object)val4;
			}
			HeroController.OnCollisionEnter2D -= (hook_OnCollisionEnter2D)obj4;
			object obj5 = _003C_003EO._003C4_003E__CollisionStay;
			if (obj5 == null)
			{
				hook_OnCollisionStay2D val5 = CollisionStay;
				_003C_003EO._003C4_003E__CollisionStay = val5;
				obj5 = (object)val5;
			}
			HeroController.OnCollisionStay2D -= (hook_OnCollisionStay2D)obj5;
			object obj6 = _003C_003EO._003C5_003E__CollisionExit;
			if (obj6 == null)
			{
				hook_OnCollisionExit2D val6 = CollisionExit;
				_003C_003EO._003C5_003E__CollisionExit = val6;
				obj6 = (object)val6;
			}
			HeroController.OnCollisionExit2D -= (hook_OnCollisionExit2D)obj6;
			object obj7 = _003C_003EO._003C6_003E__HazardMarker;
			if (obj7 == null)
			{
				hook_OnTriggerEnter2D val7 = HazardMarker;
				_003C_003EO._003C6_003E__HazardMarker = val7;
				obj7 = (object)val7;
			}
			HazardRespawnTrigger.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj7;
			object obj8 = _003C_003EO._003C7_003E__BoxStart;
			if (obj8 == null)
			{
				hook_Start val8 = BoxStart;
				_003C_003EO._003C7_003E__BoxStart = val8;
				obj8 = (object)val8;
			}
			HeroBox.Start -= (hook_Start)obj8;
			object obj9 = _003C_003EO._003C8_003E__BoxEnter;
			if (obj9 == null)
			{
				hook_OnTriggerEnter2D val9 = BoxEnter;
				_003C_003EO._003C8_003E__BoxEnter = val9;
				obj9 = (object)val9;
			}
			HeroBox.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj9;
			object obj10 = _003C_003EO._003C9_003E__BoxStay;
			if (obj10 == null)
			{
				hook_OnTriggerStay2D val10 = BoxStay;
				_003C_003EO._003C9_003E__BoxStay = val10;
				obj10 = (object)val10;
			}
			HeroBox.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj10;
			object obj11 = _003C_003EO._003C10_003E__BoxLate;
			if (obj11 == null)
			{
				hook_LateUpdate val11 = BoxLate;
				_003C_003EO._003C10_003E__BoxLate = val11;
				obj11 = (object)val11;
			}
			HeroBox.LateUpdate -= (hook_LateUpdate)obj11;
			object obj12 = _003C_003EO._003C11_003E__AddSoul;
			if (obj12 == null)
			{
				hook_AddMPCharge val12 = AddSoul;
				_003C_003EO._003C11_003E__AddSoul = val12;
				obj12 = (object)val12;
			}
			HeroController.AddMPCharge -= (hook_AddMPCharge)obj12;
			object obj13 = _003C_003EO._003C12_003E__GainSoul;
			if (obj13 == null)
			{
				hook_SoulGain val13 = GainSoul;
				_003C_003EO._003C12_003E__GainSoul = val13;
				obj13 = (object)val13;
			}
			HeroController.SoulGain -= (hook_SoulGain)obj13;
			object obj14 = _003C_003EO._003C13_003E__SpaSoul;
			if (obj14 == null)
			{
				hook_TryAddMPChargeSpa val14 = SpaSoul;
				_003C_003EO._003C13_003E__SpaSoul = val14;
				obj14 = (object)val14;
			}
			HeroController.TryAddMPChargeSpa -= (hook_TryAddMPChargeSpa)obj14;
			object obj15 = _003C_003EO._003C14_003E__SetSoul;
			if (obj15 == null)
			{
				hook_SetMPCharge val15 = SetSoul;
				_003C_003EO._003C14_003E__SetSoul = val15;
				obj15 = (object)val15;
			}
			HeroController.SetMPCharge -= (hook_SetMPCharge)obj15;
			object obj16 = _003C_003EO._003C15_003E__TakeReserve;
			if (obj16 == null)
			{
				hook_TakeReserveMP val16 = TakeReserve;
				_003C_003EO._003C15_003E__TakeReserve = val16;
				obj16 = (object)val16;
			}
			HeroController.TakeReserveMP -= (hook_TakeReserveMP)obj16;
			object obj17 = _003C_003EO._003C16_003E__AddHealth;
			if (obj17 == null)
			{
				hook_AddHealth val17 = AddHealth;
				_003C_003EO._003C16_003E__AddHealth = val17;
				obj17 = (object)val17;
			}
			HeroController.AddHealth -= (hook_AddHealth)obj17;
			object obj18 = _003C_003EO._003C17_003E__Recoil;
			if (obj18 == null)
			{
				hook_StartRecoil val18 = Recoil;
				_003C_003EO._003C17_003E__Recoil = val18;
				obj18 = (object)val18;
			}
			HeroController.StartRecoil -= (hook_StartRecoil)obj18;
			object obj19 = _003C_003EO._003C18_003E__TakeMP;
			if (obj19 == null)
			{
				hook_TakeMP val19 = TakeMP;
				_003C_003EO._003C18_003E__TakeMP = val19;
				obj19 = (object)val19;
			}
			HeroController.TakeMP -= (hook_TakeMP)obj19;
			object obj20 = _003C_003EO._003C19_003E__TakeMPQuick;
			if (obj20 == null)
			{
				hook_TakeMPQuick val20 = TakeMPQuick;
				_003C_003EO._003C19_003E__TakeMPQuick = val20;
				obj20 = (object)val20;
			}
			HeroController.TakeMPQuick -= (hook_TakeMPQuick)obj20;
			object obj21 = _003C_003EO._003C20_003E__BenchEnter;
			if (obj21 == null)
			{
				hook_OnTriggerEnter2D val21 = BenchEnter;
				_003C_003EO._003C20_003E__BenchEnter = val21;
				obj21 = (object)val21;
			}
			RestBench.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj21;
			object obj22 = _003C_003EO._003C21_003E__BenchExit;
			if (obj22 == null)
			{
				hook_OnTriggerExit2D val22 = BenchExit;
				_003C_003EO._003C21_003E__BenchExit = val22;
				obj22 = (object)val22;
			}
			RestBench.OnTriggerExit2D -= (hook_OnTriggerExit2D)obj22;
			object obj23 = _003C_003EO._003C22_003E__WalkEnter;
			if (obj23 == null)
			{
				hook_OnTriggerEnter2D val23 = WalkEnter;
				_003C_003EO._003C22_003E__WalkEnter = val23;
				obj23 = (object)val23;
			}
			WalkArea.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj23;
			object obj24 = _003C_003EO._003C23_003E__WalkStay;
			if (obj24 == null)
			{
				hook_OnTriggerStay2D val24 = WalkStay;
				_003C_003EO._003C23_003E__WalkStay = val24;
				obj24 = (object)val24;
			}
			WalkArea.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj24;
			object obj25 = _003C_003EO._003C24_003E__WalkExit;
			if (obj25 == null)
			{
				hook_OnTriggerExit2D val25 = WalkExit;
				_003C_003EO._003C24_003E__WalkExit = val25;
				obj25 = (object)val25;
			}
			WalkArea.OnTriggerExit2D -= (hook_OnTriggerExit2D)obj25;
			object obj26 = _003C_003EO._003C25_003E__WorldTriggerEnter;
			if (obj26 == null)
			{
				hook_OnTriggerEnter2D val26 = WorldTriggerEnter;
				_003C_003EO._003C25_003E__WorldTriggerEnter = val26;
				obj26 = (object)val26;
			}
			TriggerEnterEvent.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj26;
			object obj27 = _003C_003EO._003C26_003E__WorldTriggerStay;
			if (obj27 == null)
			{
				hook_OnTriggerStay2D val27 = WorldTriggerStay;
				_003C_003EO._003C26_003E__WorldTriggerStay = val27;
				obj27 = (object)val27;
			}
			TriggerEnterEvent.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj27;
			object obj28 = _003C_003EO._003C27_003E__WorldTriggerExit;
			if (obj28 == null)
			{
				hook_OnTriggerExit2D val28 = WorldTriggerExit;
				_003C_003EO._003C27_003E__WorldTriggerExit = val28;
				obj28 = (object)val28;
			}
			TriggerEnterEvent.OnTriggerExit2D -= (hook_OnTriggerExit2D)obj28;
			object obj29 = _003C_003EO._003C28_003E__HeroAwake;
			if (obj29 == null)
			{
				hook_Awake val29 = HeroAwake;
				_003C_003EO._003C28_003E__HeroAwake = val29;
				obj29 = (object)val29;
			}
			HeroController.Awake -= (hook_Awake)obj29;
			object obj30 = _003C_003EO._003C29_003E__HeroStart;
			if (obj30 == null)
			{
				hook_Start val30 = HeroStart;
				_003C_003EO._003C29_003E__HeroStart = val30;
				obj30 = (object)val30;
			}
			HeroController.Start -= (hook_Start)obj30;
			object obj31 = _003C_003EO._003C30_003E__HeroUpdate;
			if (obj31 == null)
			{
				hook_Update val31 = HeroUpdate;
				_003C_003EO._003C30_003E__HeroUpdate = val31;
				obj31 = (object)val31;
			}
			HeroController.Update -= (hook_Update)obj31;
			object obj32 = _003C_003EO._003C31_003E__HeroFixed;
			if (obj32 == null)
			{
				hook_FixedUpdate val32 = HeroFixed;
				_003C_003EO._003C31_003E__HeroFixed = val32;
				obj32 = (object)val32;
			}
			HeroController.FixedUpdate -= (hook_FixedUpdate)obj32;
			object obj33 = _003C_003EO._003C32_003E__IsSwimming;
			if (obj33 == null)
			{
				hook_IsSwimming val33 = IsSwimming;
				_003C_003EO._003C32_003E__IsSwimming = val33;
				obj33 = (object)val33;
			}
			HeroController.IsSwimming -= (hook_IsSwimming)obj33;
			object obj34 = _003C_003EO._003C33_003E__SetBackOnGround;
			if (obj34 == null)
			{
				hook_SetBackOnGround val34 = SetBackOnGround;
				_003C_003EO._003C33_003E__SetBackOnGround = val34;
				obj34 = (object)val34;
			}
			HeroController.SetBackOnGround -= (hook_SetBackOnGround)obj34;
			object obj35 = _003C_003EO._003C34_003E__BackOnGround;
			if (obj35 == null)
			{
				hook_BackOnGround val35 = BackOnGround;
				_003C_003EO._003C34_003E__BackOnGround = val35;
				obj35 = (object)val35;
			}
			HeroController.BackOnGround -= (hook_BackOnGround)obj35;
			object obj36 = _003C_003EO._003C35_003E__AnimationUpdate;
			if (obj36 == null)
			{
				hook_Update val36 = AnimationUpdate;
				_003C_003EO._003C35_003E__AnimationUpdate = val36;
				obj36 = (object)val36;
			}
			HeroAnimationController.Update -= (hook_Update)obj36;
			object obj37 = _003C_003EO._003C36_003E__Damage;
			if (obj37 == null)
			{
				hook_TakeDamage val37 = Damage;
				_003C_003EO._003C36_003E__Damage = val37;
				obj37 = (object)val37;
			}
			HeroController.TakeDamage -= (hook_TakeDamage)obj37;
			object obj38 = _003C_003EO._003C37_003E__CharmUpdate;
			if (obj38 == null)
			{
				hook_CharmUpdate val38 = CharmUpdate;
				_003C_003EO._003C37_003E__CharmUpdate = val38;
				obj38 = (object)val38;
			}
			HeroController.CharmUpdate -= (hook_CharmUpdate)obj38;
			object obj39 = _003C_003EO._003C38_003E__MaxHealth;
			if (obj39 == null)
			{
				hook_MaxHealth val39 = MaxHealth;
				_003C_003EO._003C38_003E__MaxHealth = val39;
				obj39 = (object)val39;
			}
			HeroController.MaxHealth -= (hook_MaxHealth)obj39;
			object obj40 = _003C_003EO._003C44_003E__Die;
			if (obj40 == null)
			{
				hook_Die val40 = Die;
				_003C_003EO._003C44_003E__Die = val40;
				obj40 = (object)val40;
			}
			HeroController.Die -= (hook_Die)obj40;
			object obj41 = _003C_003EO._003C45_003E__Hazard;
			if (obj41 == null)
			{
				hook_DieFromHazard val41 = Hazard;
				_003C_003EO._003C45_003E__Hazard = val41;
				obj41 = (object)val41;
			}
			HeroController.DieFromHazard -= (hook_DieFromHazard)obj41;
			object obj42 = _003C_003EO._003C39_003E__SetInt;
			if (obj42 == null)
			{
				hook_SetInt val42 = SetInt;
				_003C_003EO._003C39_003E__SetInt = val42;
				obj42 = (object)val42;
			}
			PlayerData.SetInt -= (hook_SetInt)obj42;
			object obj43 = _003C_003EO._003C40_003E__IntAdd;
			if (obj43 == null)
			{
				hook_IntAdd val43 = IntAdd;
				_003C_003EO._003C40_003E__IntAdd = val43;
				obj43 = (object)val43;
			}
			PlayerData.IntAdd -= (hook_IntAdd)obj43;
			object obj44 = _003C_003EO._003C41_003E__IncrementInt;
			if (obj44 == null)
			{
				hook_IncrementInt val44 = IncrementInt;
				_003C_003EO._003C41_003E__IncrementInt = val44;
				obj44 = (object)val44;
			}
			PlayerData.IncrementInt -= (hook_IncrementInt)obj44;
			object obj45 = _003C_003EO._003C42_003E__SetBool;
			if (obj45 == null)
			{
				hook_SetBool val45 = SetBool;
				_003C_003EO._003C42_003E__SetBool = val45;
				obj45 = (object)val45;
			}
			PlayerData.SetBool -= (hook_SetBool)obj45;
			object obj46 = _003C_003EO._003C43_003E__GetBool;
			if (obj46 == null)
			{
				hook_GetBool val46 = GetBool;
				_003C_003EO._003C43_003E__GetBool = val46;
				obj46 = (object)val46;
			}
			PlayerData.GetBool -= (hook_GetBool)obj46;
			object obj47 = _003C_003EO._003C46_003E__Hit;
			if (obj47 == null)
			{
				hook_Hit val47 = Hit;
				_003C_003EO._003C46_003E__Hit = val47;
				obj47 = (object)val47;
			}
			HealthManager.Hit -= (hook_Hit)obj47;
			object obj48 = _003C_003EO._003C47_003E__EnemyDie;
			if (obj48 == null)
			{
				hook_Die val48 = EnemyDie;
				_003C_003EO._003C47_003E__EnemyDie = val48;
				obj48 = (object)val48;
			}
			HealthManager.Die -= (hook_Die)obj48;
			object obj49 = _003C_003EO._003C48_003E__CameraLate;
			if (obj49 == null)
			{
				hook_LateUpdate val49 = CameraLate;
				_003C_003EO._003C48_003E__CameraLate = val49;
				obj49 = (object)val49;
			}
			CameraController.LateUpdate -= (hook_LateUpdate)obj49;
			object obj50 = _003C_003EO._003C49_003E__MusicCueChange;
			if (obj50 == null)
			{
				hook_ApplyMusicCue val50 = MusicCueChange;
				_003C_003EO._003C49_003E__MusicCueChange = val50;
				obj50 = (object)val50;
			}
			AudioManager.ApplyMusicCue -= (hook_ApplyMusicCue)obj50;
			object obj51 = _003C_003EO._003C50_003E__TransitionEnter;
			if (obj51 == null)
			{
				hook_OnTriggerEnter2D val51 = TransitionEnter;
				_003C_003EO._003C50_003E__TransitionEnter = val51;
				obj51 = (object)val51;
			}
			TransitionPoint.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj51;
			object obj52 = _003C_003EO._003C51_003E__TransitionStay;
			if (obj52 == null)
			{
				hook_OnTriggerStay2D val52 = TransitionStay;
				_003C_003EO._003C51_003E__TransitionStay = val52;
				obj52 = (object)val52;
			}
			TransitionPoint.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj52;
			object obj53 = _003C_003EO._003C52_003E__CameraEnter;
			if (obj53 == null)
			{
				hook_OnTriggerEnter2D val53 = CameraEnter;
				_003C_003EO._003C52_003E__CameraEnter = val53;
				obj53 = (object)val53;
			}
			CameraLockArea.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj53;
			object obj54 = _003C_003EO._003C53_003E__CameraStay;
			if (obj54 == null)
			{
				hook_OnTriggerStay2D val54 = CameraStay;
				_003C_003EO._003C53_003E__CameraStay = val54;
				obj54 = (object)val54;
			}
			CameraLockArea.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj54;
			object obj55 = _003C_003EO._003C54_003E__CameraExit;
			if (obj55 == null)
			{
				hook_OnTriggerExit2D val55 = CameraExit;
				_003C_003EO._003C54_003E__CameraExit = val55;
				obj55 = (object)val55;
			}
			CameraLockArea.OnTriggerExit2D -= (hook_OnTriggerExit2D)obj55;
			object obj56 = _003C_003EO._003C55_003E__BeginTransition;
			if (obj56 == null)
			{
				hook_BeginSceneTransition val56 = BeginTransition;
				_003C_003EO._003C55_003E__BeginTransition = val56;
				obj56 = (object)val56;
			}
			GameManager.BeginSceneTransition -= (hook_BeginSceneTransition)obj56;
			object obj57 = _003C_003EO._003C56_003E__LoadScene;
			if (obj57 == null)
			{
				hook_LoadScene val57 = LoadScene;
				_003C_003EO._003C56_003E__LoadScene = val57;
				obj57 = (object)val57;
			}
			GameManager.LoadScene -= (hook_LoadScene)obj57;
			object obj58 = _003C_003EO._003C57_003E__PauseToggle;
			if (obj58 == null)
			{
				hook_PauseGameToggle val58 = PauseToggle;
				_003C_003EO._003C57_003E__PauseToggle = val58;
				obj58 = (object)val58;
			}
			GameManager.PauseGameToggle -= (hook_PauseGameToggle)obj58;
			object obj59 = _003C_003EO._003C58_003E__PlayerDead;
			if (obj59 == null)
			{
				hook_PlayerDead val59 = PlayerDead;
				_003C_003EO._003C58_003E__PlayerDead = val59;
				obj59 = (object)val59;
			}
			GameManager.PlayerDead -= (hook_PlayerDead)obj59;
			object obj60 = _003C_003EO._003C59_003E__PlayerDeadHazard;
			if (obj60 == null)
			{
				hook_PlayerDeadFromHazard val60 = PlayerDeadHazard;
				_003C_003EO._003C59_003E__PlayerDeadHazard = val60;
				obj60 = (object)val60;
			}
			GameManager.PlayerDeadFromHazard -= (hook_PlayerDeadFromHazard)obj60;
			object obj61 = _003C_003EO._003C60_003E__SaveGame;
			if (obj61 == null)
			{
				hook_SaveGame val61 = SaveGame;
				_003C_003EO._003C60_003E__SaveGame = val61;
				obj61 = (object)val61;
			}
			GameManager.SaveGame -= (hook_SaveGame)obj61;
			object obj62 = _003C_003EO._003C61_003E__InputUpdate;
			if (obj62 == null)
			{
				hook_Update val62 = InputUpdate;
				_003C_003EO._003C61_003E__InputUpdate = val62;
				obj62 = (object)val62;
			}
			InputHandler.Update -= (hook_Update)obj62;
			object obj63 = _003C_003EO._003C62_003E__PoolSpawn;
			if (obj63 == null)
			{
				hook_Spawn_GameObject_Transform_Vector3_Quaternion val63 = PoolSpawn;
				_003C_003EO._003C62_003E__PoolSpawn = val63;
				obj63 = (object)val63;
			}
			ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion -= (hook_Spawn_GameObject_Transform_Vector3_Quaternion)obj63;
			object obj64 = _003C_003EO._003C63_003E__EnemyUpdate;
			if (obj64 == null)
			{
				hook_Update val64 = EnemyUpdate;
				_003C_003EO._003C63_003E__EnemyUpdate = val64;
				obj64 = (object)val64;
			}
			LineOfSightDetector.Update -= (hook_Update)obj64;
			object obj65 = _003C_003EO._003C64_003E__HatchlingFixed;
			if (obj65 == null)
			{
				hook_FixedUpdate val65 = HatchlingFixed;
				_003C_003EO._003C64_003E__HatchlingFixed = val65;
				obj65 = (object)val65;
			}
			KnightHatchling.FixedUpdate -= (hook_FixedUpdate)obj65;
			object obj66 = _003C_003EO._003C65_003E__HatchlingSpawn;
			if (obj66 == null)
			{
				hook_Spawn val66 = HatchlingSpawn;
				_003C_003EO._003C65_003E__HatchlingSpawn = val66;
				obj66 = (object)val66;
			}
			KnightHatchling.Spawn -= (hook_Spawn)obj66;
			object obj67 = _003C_003EO._003C66_003E__HatchlingTeleEnd;
			if (obj67 == null)
			{
				hook_TeleEnd val67 = HatchlingTeleEnd;
				_003C_003EO._003C66_003E__HatchlingTeleEnd = val67;
				obj67 = (object)val67;
			}
			KnightHatchling.TeleEnd -= (hook_TeleEnd)obj67;
			object obj68 = _003C_003EO._003C67_003E__SpellOrb;
			if (obj68 == null)
			{
				hook_OnEnable val68 = SpellOrb;
				_003C_003EO._003C67_003E__SpellOrb = val68;
				obj68 = (object)val68;
			}
			SpellGetOrb.OnEnable -= (hook_OnEnable)obj68;
			object obj69 = _003C_003EO._003C68_003E__FsmAwake;
			if (obj69 == null)
			{
				hook_Awake val69 = FsmAwake;
				_003C_003EO._003C68_003E__FsmAwake = val69;
				obj69 = (object)val69;
			}
			Fsm.Awake -= (hook_Awake)obj69;
			object obj70 = _003C_003EO._003C69_003E__FsmEnable;
			if (obj70 == null)
			{
				hook_OnEnable val70 = FsmEnable;
				_003C_003EO._003C69_003E__FsmEnable = val70;
				obj70 = (object)val70;
			}
			Fsm.OnEnable -= (hook_OnEnable)obj70;
			object obj71 = _003C_003EO._003C70_003E__FsmStart;
			if (obj71 == null)
			{
				hook_Start val71 = FsmStart;
				_003C_003EO._003C70_003E__FsmStart = val71;
				obj71 = (object)val71;
			}
			Fsm.Start -= (hook_Start)obj71;
			object obj72 = _003C_003EO._003C71_003E__StateEnter;
			if (obj72 == null)
			{
				hook_OnEnter val72 = StateEnter;
				_003C_003EO._003C71_003E__StateEnter = val72;
				obj72 = (object)val72;
			}
			FsmState.OnEnter -= (hook_OnEnter)obj72;
			object obj73 = _003C_003EO._003C72_003E__FsmUpdate;
			if (obj73 == null)
			{
				hook_Update val73 = FsmUpdate;
				_003C_003EO._003C72_003E__FsmUpdate = val73;
				obj73 = (object)val73;
			}
			Fsm.Update -= (hook_Update)obj73;
			object obj74 = _003C_003EO._003C73_003E__FsmFixed;
			if (obj74 == null)
			{
				hook_FixedUpdate val74 = FsmFixed;
				_003C_003EO._003C73_003E__FsmFixed = val74;
				obj74 = (object)val74;
			}
			Fsm.FixedUpdate -= (hook_FixedUpdate)obj74;
			object obj75 = _003C_003EO._003C74_003E__FsmLate;
			if (obj75 == null)
			{
				hook_LateUpdate val75 = FsmLate;
				_003C_003EO._003C74_003E__FsmLate = val75;
				obj75 = (object)val75;
			}
			Fsm.LateUpdate -= (hook_LateUpdate)obj75;
			object obj76 = _003C_003EO._003C75_003E__FsmEvent;
			if (obj76 == null)
			{
				hook_ProcessEvent val76 = FsmEvent;
				_003C_003EO._003C75_003E__FsmEvent = val76;
				obj76 = (object)val76;
			}
			Fsm.ProcessEvent -= (hook_ProcessEvent)obj76;
			object obj77 = _003C_003EO._003C76_003E__FsmTriggerEnter;
			if (obj77 == null)
			{
				hook_OnTriggerEnter2D val77 = FsmTriggerEnter;
				_003C_003EO._003C76_003E__FsmTriggerEnter = val77;
				obj77 = (object)val77;
			}
			Fsm.OnTriggerEnter2D -= (hook_OnTriggerEnter2D)obj77;
			object obj78 = _003C_003EO._003C77_003E__FsmTriggerStay;
			if (obj78 == null)
			{
				hook_OnTriggerStay2D val78 = FsmTriggerStay;
				_003C_003EO._003C77_003E__FsmTriggerStay = val78;
				obj78 = (object)val78;
			}
			Fsm.OnTriggerStay2D -= (hook_OnTriggerStay2D)obj78;
			object obj79 = _003C_003EO._003C78_003E__FsmTriggerExit;
			if (obj79 == null)
			{
				hook_OnTriggerExit2D val79 = FsmTriggerExit;
				_003C_003EO._003C78_003E__FsmTriggerExit = val79;
				obj79 = (object)val79;
			}
			Fsm.OnTriggerExit2D -= (hook_OnTriggerExit2D)obj79;
			object obj80 = _003C_003EO._003C79_003E__WaterCollisionEnter;
			if (obj80 == null)
			{
				hook_OnCollisionEnter2D val80 = WaterCollisionEnter;
				_003C_003EO._003C79_003E__WaterCollisionEnter = val80;
				obj80 = (object)val80;
			}
			Fsm.OnCollisionEnter2D -= (hook_OnCollisionEnter2D)obj80;
			object obj81 = _003C_003EO._003C80_003E__WaterCollisionStay;
			if (obj81 == null)
			{
				hook_OnCollisionStay2D val81 = WaterCollisionStay;
				_003C_003EO._003C80_003E__WaterCollisionStay = val81;
				obj81 = (object)val81;
			}
			Fsm.OnCollisionStay2D -= (hook_OnCollisionStay2D)obj81;
			object obj82 = _003C_003EO._003C81_003E__WaterCollisionExit;
			if (obj82 == null)
			{
				hook_OnCollisionExit2D val82 = WaterCollisionExit;
				_003C_003EO._003C81_003E__WaterCollisionExit = val82;
				obj82 = (object)val82;
			}
			Fsm.OnCollisionExit2D -= (hook_OnCollisionExit2D)obj82;
			object obj83 = _003C_003EO._003C82_003E__ListenUp;
			if (obj83 == null)
			{
				hook_CheckForInput val83 = ListenUp;
				_003C_003EO._003C82_003E__ListenUp = val83;
				obj83 = (object)val83;
			}
			ListenForUp.CheckForInput -= (hook_CheckForInput)obj83;
			object obj84 = _003C_003EO._003C83_003E__ListenDown;
			if (obj84 == null)
			{
				hook_CheckForInput val84 = ListenDown;
				_003C_003EO._003C83_003E__ListenDown = val84;
				obj84 = (object)val84;
			}
			ListenForDown.CheckForInput -= (hook_CheckForInput)obj84;
			object obj85 = _003C_003EO._003C84_003E__InteractionCall;
			if (obj85 == null)
			{
				hook_OnEnter val85 = InteractionCall;
				_003C_003EO._003C84_003E__InteractionCall = val85;
				obj85 = (object)val85;
			}
			CallMethodProper.OnEnter -= (hook_OnEnter)obj85;
			object obj86 = _003C_003EO._003C85_003E__CreatePickupCard;
			if (obj86 == null)
			{
				hook_OnEnter val86 = CreatePickupCard;
				_003C_003EO._003C85_003E__CreatePickupCard = val86;
				obj86 = (object)val86;
			}
			CreateUIMsgGetItem.OnEnter -= (hook_OnEnter)obj86;
			object obj87 = _003C_003EO._003C86_003E__PickupJump;
			if (obj87 == null)
			{
				hook_OnUpdate val87 = PickupJump;
				_003C_003EO._003C86_003E__PickupJump = val87;
				obj87 = (object)val87;
			}
			ListenForJump.OnUpdate -= (hook_OnUpdate)obj87;
			object obj88 = _003C_003EO._003C87_003E__PickupMenuActions;
			if (obj88 == null)
			{
				hook_OnUpdate val88 = PickupMenuActions;
				_003C_003EO._003C87_003E__PickupMenuActions = val88;
				obj88 = (object)val88;
			}
			ListenForMenuActions.OnUpdate -= (hook_OnUpdate)obj88;
			object obj89 = _003C_003EO._003C88_003E__PickupMenuSubmit;
			if (obj89 == null)
			{
				hook_OnUpdate val89 = PickupMenuSubmit;
				_003C_003EO._003C88_003E__PickupMenuSubmit = val89;
				obj89 = (object)val89;
			}
			ListenForMenuSubmit.OnUpdate -= (hook_OnUpdate)obj89;
			object obj90 = _003C_003EO._003C89_003E__PickupMenuCancel;
			if (obj90 == null)
			{
				hook_OnUpdate val90 = PickupMenuCancel;
				_003C_003EO._003C89_003E__PickupMenuCancel = val90;
				obj90 = (object)val90;
			}
			ListenForMenuCancel.OnUpdate -= (hook_OnUpdate)obj90;
			object obj91 = _003C_003EO._003C90_003E__QuickMapUpdate;
			if (obj91 == null)
			{
				hook_OnUpdate val91 = QuickMapUpdate;
				_003C_003EO._003C90_003E__QuickMapUpdate = val91;
				obj91 = (object)val91;
			}
			ListenForQuickMap.OnUpdate -= (hook_OnUpdate)obj91;
			object obj92 = _003C_003EO._003C91_003E__MapUpdate;
			if (obj92 == null)
			{
				hook_Update val92 = MapUpdate;
				_003C_003EO._003C91_003E__MapUpdate = val92;
				obj92 = (object)val92;
			}
			GameMap.Update -= (hook_Update)obj92;
			object obj93 = _003C_003EO._003C92_003E__SpellPoolAction;
			if (obj93 == null)
			{
				hook_OnEnter val93 = SpellPoolAction;
				_003C_003EO._003C92_003E__SpellPoolAction = val93;
				obj93 = (object)val93;
			}
			SpawnObjectFromGlobalPool.OnEnter -= (hook_OnEnter)obj93;
			object obj94 = _003C_003EO._003C93_003E__SpellCreateAction;
			if (obj94 == null)
			{
				hook_OnEnter val94 = SpellCreateAction;
				_003C_003EO._003C93_003E__SpellCreateAction = val94;
				obj94 = (object)val94;
			}
			CreateObject.OnEnter -= (hook_OnEnter)obj94;
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
			if (session.Cloning == null || !((Object)(object)hero != (Object)(object)session.Primary.Hero))
			{
				return null;
			}
			playerSlot = session.Cloning;
		}
		return playerSlot;
	}

	private static void HeroAwake(orig_Awake orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self);
		}
	}

	private static void HeroStart(orig_Start orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self);
		}
	}

	private static void HeroUpdate(orig_Update orig, HeroController self)
	{
		if (HeroFrame(self, out var p))
		{
			AcidSwimming.PrimaryState before = ((p != null && p.Index > 0 && p.AcidAssistActive) ? AcidSwimming.CapturePrimary() : default(AcidSwimming.PrimaryState));
			using (PlayerContext.Enter(p))
			{
				orig.Invoke(self);
			}
			AcidSwimming.Float(p);
			AcidSwimming.RestorePrimary(before, "hero update");
			if (before.Valid)
			{
				AcidSwimming.GuardPrimary();
			}
		}
	}

	private static void HeroFixed(orig_FixedUpdate orig, HeroController self)
	{
		if (HeroFrame(self, out var p))
		{
			AcidSwimming.PrimaryState before = ((p != null && p.Index > 0 && p.AcidAssistActive) ? AcidSwimming.CapturePrimary() : default(AcidSwimming.PrimaryState));
			using (PlayerContext.Enter(p))
			{
				orig.Invoke(self);
			}
			AcidSwimming.Float(p);
			AcidSwimming.RestorePrimary(before, "hero physics");
			if (before.Valid)
			{
				AcidSwimming.GuardPrimary();
			}
		}
	}

	private static void IsSwimming(orig_IsSwimming orig, HeroController self)
	{
		if (AcidSwimming.WrongPrimarySwim(self))
		{
			AcidSwimming.GuardPrimary();
			return;
		}
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot == null || playerSlot.Index <= 0 || !playerSlot.AcidAssistActive || AcidSwimming.HasSwimClip(playerSlot))
		{
			orig.Invoke(self);
		}
	}

	private static void SetBackOnGround(orig_SetBackOnGround orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && playerSlot.AcidAssistActive)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
	}

	private static void BackOnGround(orig_BackOnGround orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && playerSlot.AcidAssistActive)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
	}

	private static void AnimationUpdate(orig_Update orig, HeroAnimationController self)
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
			orig.Invoke(self);
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

	private static void Damage(orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide side, int amount, int hazardType)
	{
		//IL_017d: Invalid comparison between Unknown and I4
		//IL_024d: Invalid comparison between Unknown and I4
		if (CoopShades.IsSpawning(go))
		{
			return;
		}
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig.Invoke(self, go, side, amount, hazardType);
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
			if (Object.op_Implicit((Object)(object)go) && session.Resolve(go) != null && !flag2)
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
				if (!flag2 && amount > 0 && num3 >= num && num > 0 && (bool)Reflect.Call(self, "CanTakeDamage") && !session.Data.GetBool("invinciTest") && !self.takeNoDamage && (int)self.damageMode != 1 && !self.cState.shadowDashing && (hazardType != 1 || self.parryInvulnTimer <= 0f) && !Reflect.Get(self, "carefreeShieldEquipped", fallback: false) && (!session.Data.GetBool("equippedCharm_5") || session.Data.blockerHits <= 0 || !self.cState.focusing))
				{
					flag3 = true;
				}
				else
				{
					orig.Invoke(self, go, side, amount, hazardType);
					num2 = session.Data.health + session.Data.healthBlue;
					flag4 = num2 < num;
				}
			}
			if (flag && !flag3 && !playerSlot.Down && !playerSlot.Hazard && session.Gameplay && !self.cState.transitioning && (int)self.damageMode != 2 && !playerSlot.Vitals.Invincible)
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

	private static void CharmUpdate(orig_CharmUpdate orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			try
			{
				orig.Invoke(self);
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

	private static bool GetBool(orig_GetBool orig, PlayerData self, string name)
	{
		CoopSession session = Session;
		PlayerSlot current = PlayerContext.Current;
		if (session != null && session.Active && self == session.Data && name == "hasXunFlower" && FlowerRules.HideFrom(self, current))
		{
			return false;
		}
		return orig.Invoke(self, name);
	}

	private static void SetBool(orig_SetBool orig, PlayerData self, string name, bool value)
	{
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
			if ((playerSlot == null || !playerSlot.Alive) && WorldRouting.Current != null && Object.op_Implicit((Object)(object)WorldRouting.Current.GameObject))
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
		orig.Invoke(self, name, value);
		if (session != null && session.Active && self == session.Data)
		{
			FlowerRules.BoolChanged(session, self, name, oldValue, value, current);
		}
	}

	private static void SetInt(orig_SetInt orig, PlayerData self, string name, int value)
	{
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
					orig.Invoke(self, name, self.healthBlue + num);
					return;
				}
			}
			Diagnostics.Write("BLUE HEALTH +" + num + " context=" + ((PlayerContext.Current == null) ? "vanilla" : ("P" + (PlayerContext.Current.Index + 1))));
		}
		orig.Invoke(self, name, value);
	}

	private static void IntAdd(orig_IntAdd orig, PlayerData self, string name, int amount)
	{
		if (name == "heartPieces" && amount > 0 && Session != null && Session.Active && self == Session.Data)
		{
			PickupCard.MaskAcquired(ShopMenuRouting.Buyer, WorldRouting.Current, name);
		}
		using (PlayerContext.Enter(BlueHealthRecipient(self, name, amount)))
		{
			orig.Invoke(self, name, amount);
		}
	}

	private static void IncrementInt(orig_IncrementInt orig, PlayerData self, string name)
	{
		if (name == "heartPieces" && Session != null && Session.Active && self == Session.Data)
		{
			PickupCard.MaskAcquired(ShopMenuRouting.Buyer, WorldRouting.Current, name);
		}
		using (PlayerContext.Enter(BlueHealthRecipient(self, name, 1)))
		{
			orig.Invoke(self, name);
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

	private static void MaxHealth(orig_MaxHealth orig, HeroController self)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
		CoopSession session = Session;
		if (session != null && session.Active && playerSlot != null && InteractionRouter.BenchActor(playerSlot))
		{
			session.BenchRest(self);
		}
	}

	private static IEnumerator Recoil(orig_StartRecoil orig, HeroController self, CollisionSide side, bool effect, int amount)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		IEnumerator enumerator = orig.Invoke(self, side, effect, amount);
		if (playerSlot == null || !Session.Active)
		{
			return enumerator;
		}
		return new ScopedRoutine(playerSlot, enumerator);
	}

	private static void TakeMP(orig_TakeMP orig, HeroController self, int amount)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Reviving)
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, amount);
		}
	}

	private static void TakeMPQuick(orig_TakeMPQuick orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self, amount);
		}
	}

	private static void AddSoul(orig_AddMPCharge orig, HeroController self, int amount)
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
			orig.Invoke(self, amount);
		}
	}

	private static void GainSoul(orig_SoulGain orig, HeroController self)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self);
		}
	}

	private static void SetSoul(orig_SetMPCharge orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self, amount);
		}
	}

	private static void TakeReserve(orig_TakeReserveMP orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self, amount);
		}
	}

	private static void AddHealth(orig_AddHealth orig, HeroController self, int amount)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self, amount);
		}
	}

	private static bool SpaSoul(orig_TryAddMPChargeSpa orig, HeroController self, int amount)
	{
		List<PlayerSlot> list = ((Session != null && Session.Active) ? WorldRouting.BathPlayers() : null);
		if (list == null)
		{
			object obj = Session;
			if (obj != null)
			{
				object obj2 = WorldRouting.Current;
				if (obj2 != null)
				{
					obj2 = ((Fsm)obj2).GameObject;
					if (Object.op_Implicit((Object)obj2))
					{
						obj = ((CoopSession)obj).Nearest(((GameObject)obj2).transform.position);
						if (obj != null)
						{
							self = ((PlayerSlot)obj).Hero;
							goto IL_006b;
						}
					}
				}
			}
			PlayerSlot current = PlayerContext.Current;
			if (current != null)
			{
				self = current.Hero;
			}
		}
		goto IL_006b;
		IL_006b:
		if (list == null)
		{
			using (PlayerContext.Enter(HeroPlayer(self)))
			{
				return orig.Invoke(self, amount);
			}
		}
		bool flag = false;
		foreach (PlayerSlot item in list)
		{
			using (PlayerContext.Enter(item))
			{
				flag = orig.Invoke(item.Hero, amount) || flag;
			}
		}
		return flag;
	}

	private static void Entered(orig_FinishedEnteringScene orig, HeroController self, bool marker, bool bob)
	{
		using (PlayerContext.Enter(HeroPlayer(self)))
		{
			orig.Invoke(self, marker, bob);
		}
		if (Session != null && Session.Active)
		{
			Session.Entered(self);
		}
	}

	private static void CollisionEnter(orig_OnCollisionEnter2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void CollisionStay(orig_OnCollisionStay2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void CollisionExit(orig_OnCollisionExit2D orig, HeroController self, Collision2D c)
	{
		PlayerSlot playerSlot = HeroPlayer(self);
		if (playerSlot != null && playerSlot.Index > 0 && AcidSwimming.WaterCollision(c))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void HazardMarker(orig_OnTriggerEnter2D orig, HazardRespawnTrigger self, Collider2D other)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(other);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig.Invoke(self, other);
		}
		else if (playerSlot.Alive && Object.op_Implicit((Object)(object)self.respawnMarker) && ((Component)other).gameObject.layer == 9)
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
		if (playerSlot != null && Object.op_Implicit((Object)(object)playerSlot.Hero))
		{
			Reflect.Set(box, "heroCtrl", playerSlot.Hero);
		}
		return playerSlot;
	}

	private static void BoxStart(orig_Start orig, HeroBox self)
	{
		using (PlayerContext.Enter(BoxOwner(self)))
		{
			orig.Invoke(self);
		}
		BoxOwner(self);
	}

	private static void BoxEnter(orig_OnTriggerEnter2D orig, HeroBox self, Collider2D other)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, other);
		}
	}

	private static void BoxStay(orig_OnTriggerStay2D orig, HeroBox self, Collider2D other)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, other);
		}
	}

	private static void BoxLate(orig_LateUpdate orig, HeroBox self)
	{
		PlayerSlot playerSlot = BoxOwner(self);
		if (playerSlot != null && (playerSlot.Down || playerSlot.Hazard || !playerSlot.Ready))
		{
			return;
		}
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
	}

	private static void SelectBoard(orig_SelectCharm orig, InvCharmBackboard self)
	{
		orig.Invoke(self);
		Charms.SelectedBoard(self);
	}

	private static int SelectEquipped(orig_GetListNumber orig, CharmItem self)
	{
		Charms.SelectedEquipped();
		return orig.Invoke(self);
	}

	private static void BenchEnter(orig_OnTriggerEnter2D orig, RestBench self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(c);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig.Invoke(self, c);
		}
		else
		{
			playerSlot.Hero.NearBench(true);
		}
	}

	private static bool ExtraWalk(Collider2D c, out PlayerSlot p)
	{
		CoopSession session = Session;
		p = session?.Resolve(c);
		if (session != null && session.Active && p != null && p.Index > 0)
		{
			return ((Component)c).gameObject.layer == 9;
		}
		return false;
	}

	private static void WalkEnter(orig_OnTriggerEnter2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig.Invoke(self, c);
		}
		else
		{
			WalkTouch(self, p, inside: true);
		}
	}

	private static void WalkStay(orig_OnTriggerStay2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig.Invoke(self, c);
		}
		else
		{
			WalkTouch(self, p, inside: true);
		}
	}

	private static void WalkExit(orig_OnTriggerExit2D orig, WalkArea self, Collider2D c)
	{
		if (!ExtraWalk(c, out var p))
		{
			orig.Invoke(self, c);
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
		if (!Object.op_Implicit((Object)(object)p.Hero))
		{
			return;
		}
		bool flag = false;
		if (walkAreas.TryGetValue(p, out var value))
		{
			Collider2D heroCollider = ((Component)p.Hero).GetComponent<Collider2D>();
			value.RemoveWhere(delegate(WalkArea area)
			{
				if (!Object.op_Implicit((Object)(object)area) || !((Behaviour)area).enabled || !((Component)area).gameObject.activeInHierarchy)
				{
					return true;
				}
				Collider2D component = ((Component)area).GetComponent<Collider2D>();
				if (!Object.op_Implicit((Object)(object)component) || !((Behaviour)component).enabled)
				{
					return true;
				}
				Bounds bounds;
				if (!Object.op_Implicit((Object)(object)heroCollider))
				{
					bounds = component.bounds;
					return !((Bounds)(ref bounds)).Contains(((Component)p.Hero).transform.position);
				}
				bounds = component.bounds;
				return !((Bounds)(ref bounds)).Intersects(heroCollider.bounds);
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

	private static void WorldTriggerEnter(orig_OnTriggerEnter2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(((Component)self).gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(((Component)self).gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: false);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void WorldTriggerStay(orig_OnTriggerStay2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(((Component)self).gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(((Component)self).gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: false);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void WorldTriggerExit(orig_OnTriggerExit2D orig, TriggerEnterEvent self, Collider2D c)
	{
		PlayerSlot playerSlot = ((Session == null) ? null : Session.Resolve(c));
		if (AcidSwimming.WorldWaterContact(((Component)self).gameObject, playerSlot))
		{
			AcidSwimming.NoteBlocked(((Component)self).gameObject, playerSlot);
			return;
		}
		InteractionRouter.TriggerContact(self, c, exiting: true);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self, c);
		}
	}

	private static void BenchExit(orig_OnTriggerExit2D orig, RestBench self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = session?.Resolve(c);
		if (session == null || !session.Active || playerSlot == null)
		{
			orig.Invoke(self, c);
		}
		else
		{
			playerSlot.Hero.NearBench(false);
		}
	}

	private static IEnumerator Die(orig_Die orig, HeroController self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || session.AllowVanillaDeath || playerSlot == null)
		{
			return orig.Invoke(self);
		}
		session.Down(playerSlot);
		return Empty();
	}

	private static IEnumerator Hazard(orig_DieFromHazard orig, HeroController self, HazardType type, float angle)
	{
		//IL_002f: Invalid comparison between Unknown and I4
		CoopSession session = Session;
		PlayerSlot playerSlot = HeroPlayer(self);
		if (session == null || !session.Active || session.TeamWipe || playerSlot == null)
		{
			return orig.Invoke(self, type, angle);
		}
		if ((int)type == 2 && session.Data.hasAcidArmour)
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

	private static void EnemyDie(orig_Die orig, HealthManager self, float? direction, AttackTypes type, bool ignoreEvasion)
	{
		if (!CoopShades.HandleDeath(self))
		{
			orig.Invoke(self, direction, type, ignoreEvasion);
		}
	}

	private static void Hit(orig_Hit orig, HealthManager self, HitInstance hit)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig.Invoke(self, hit);
			return;
		}
		PlayerSlot playerSlot = session.Resolve(hit.Source) ?? PlayerContext.Current;
		string text = SummonRouting.DamageKind(hit.Source);
		int damageDealt = hit.DamageDealt;
		if (playerSlot != null && session.Players.Count > 1 && !Object.op_Implicit((Object)(object)((Component)self).GetComponent<LocalShade>()))
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
			orig.Invoke(self, hit);
		}
		if (playerSlot != null && Object.op_Implicit((Object)(object)self) && self.hp > 0 && self.hp < hp)
		{
			ArenaGather.BossHit(self, playerSlot);
		}
	}

	private static void CameraLate(orig_LateUpdate orig, CameraController self)
	{
		CoopSession session = Session;
		if (session != null && session.Active && session.Gameplay && !session.TeamWipe)
		{
			session.Camera.Prepare(self);
		}
		orig.Invoke(self);
		if (session != null && session.Active && session.Gameplay && !session.TeamWipe)
		{
			session.Camera.Apply(self, session);
		}
	}

	private static void MusicCueChange(orig_ApplyMusicCue orig, AudioManager self, MusicCue cue, float delay, float transition, bool snapshot)
	{
		CoopSession session = Session;
		GameManager instance = GameManager.instance;
		if (session != null && session.Active && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && (Object)(object)cue == (Object)(object)instance.noMusicCue && (Object)(object)self.CurrentMusicCue != (Object)(object)instance.noMusicCue && ArenaGather.EngagedBossAlive)
		{
			Diagnostics.Write("MUSIC kept boss cue=" + (Object.op_Implicit((Object)(object)self.CurrentMusicCue) ? ((Object)self.CurrentMusicCue).name : "none") + " during living boss");
			return;
		}
		if (session != null && session.Active && ArenaGather.EngagedBossAlive && (Object)(object)cue != (Object)(object)self.CurrentMusicCue)
		{
			Diagnostics.Write("MUSIC boss cue " + (Object.op_Implicit((Object)(object)self.CurrentMusicCue) ? ((Object)self.CurrentMusicCue).name : "none") + " -> " + (Object.op_Implicit((Object)(object)cue) ? ((Object)cue).name : "none"));
		}
		orig.Invoke(self, cue, delay, transition, snapshot);
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

	private static void TransitionEnter(orig_OnTriggerEnter2D orig, TransitionPoint self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot p = session?.Resolve(c);
		if (!TransitionAllowed(self, p) || TransitionVote.Gate(session, p, self, c, delegate
		{
			using (PlayerContext.Enter(p))
			{
				orig.Invoke(self, c);
			}
		}))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig.Invoke(self, c);
		}
	}

	private static void TransitionStay(orig_OnTriggerStay2D orig, TransitionPoint self, Collider2D c)
	{
		CoopSession session = Session;
		PlayerSlot p = session?.Resolve(c);
		if (!TransitionAllowed(self, p) || TransitionVote.Gate(session, p, self, c, delegate
		{
			using (PlayerContext.Enter(p))
			{
				orig.Invoke(self, c);
			}
		}))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig.Invoke(self, c);
		}
	}

	private static void CameraEnter(orig_OnTriggerEnter2D orig, CameraLockArea self, Collider2D c)
	{
		ArenaGather.CameraContact(self, c);
		if (PrimaryCollider(c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void CameraStay(orig_OnTriggerStay2D orig, CameraLockArea self, Collider2D c)
	{
		ArenaGather.CameraContact(self, c);
		if (PrimaryCollider(c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void CameraExit(orig_OnTriggerExit2D orig, CameraLockArea self, Collider2D c)
	{
		if (PrimaryCollider(c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void BeginTransition(orig_BeginSceneTransition orig, GameManager self, SceneLoadInfo info)
	{
		PlayerSlot playerSlot = PlayerContext.Current ?? InteractionRouter.ActivePlayer;
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig.Invoke(self, info);
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
			session.PrepareTransition(playerSlot);
			using (PlayerContext.Enter(session.Primary))
			{
				orig.Invoke(self, info);
			}
		}
	}

	private static void LoadScene(orig_LoadScene orig, GameManager self, string scene)
	{
		if (!CoopEnding.DeferDirect(scene, Session))
		{
			if (Session != null && Session.Active)
			{
				InteractionRouter.CloseForTransition();
			}
			orig.Invoke(self, scene);
		}
	}

	private static IEnumerator PauseToggle(orig_PauseGameToggle orig, GameManager self)
	{
		CoopSession session = Session;
		PlayerSlot p = PlayerContext.Current ?? InteractionRouter.ActivePlayer ?? session?.Primary;
		if (session != null && session.Active && !self.isPaused && (EmergencyWarp.Active(p) || EmergencyWarp.Holding(p)))
		{
			return Empty();
		}
		return orig.Invoke(self);
	}

	private static bool FreeToPause(CoopSession s, GameManager gm, InputHandler input)
	{
		//IL_0070: Invalid comparison between Unknown and I4
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
		if (!s.Gameplay || gm.isPaused || !Object.op_Implicit((Object)(object)instance) || (int)instance.uiState != 4 || Plugin.Self.Panel || !input.acceptingInput || !input.pauseAllowed || Charms.NativeMenuOpen || flag || ScriptedParty.Active || CoopEnding.Active)
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

	private static void InputUpdate(orig_Update orig, InputHandler self)
	{
		//IL_0235: Invalid comparison between Unknown and I4
		//IL_00d0: Invalid comparison between Unknown and I4
		CoopSession session = Session;
		GameManager instance = GameManager.instance;
		bool flag = false;
		if (session != null && session.Active && Object.op_Implicit((Object)(object)instance) && (Object)(object)instance.inputHandler == (Object)(object)self && session.Primary != null && session.Primary.Actions != null && PlayerContext.Current == null)
		{
			PlayerSlot playerSlot = PickupCard.Owner ?? session.Primary;
			if (self.inputActions != playerSlot.Actions)
			{
				self.inputActions = playerSlot.Actions;
			}
			if (Object.op_Implicit((Object)(object)playerSlot.Hero) && instance.IsGameplayScene() && instance.HasFinishedEnteringScene)
			{
				self.AttachHeroController(playerSlot.Hero);
			}
			bool flag2 = FreeToPause(session, instance, self);
			if (Input.GetKeyDown((KeyCode)27) && (int)instance.gameState == 4)
			{
				if (flag2 && session.Data.disablePause && pauseRecoveryCandidate)
				{
					session.Data.disablePause = false;
					pauseRecoveryCandidate = false;
					Diagnostics.Write("PAUSE restored stale disablePause after interaction");
				}
				if (flag2 && !session.Data.disablePause && !((OneAxisInputControl)self.inputActions.pause).WasPressed)
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
		orig.Invoke(self);
		if (flag && Object.op_Implicit((Object)(object)instance) && !instance.isPaused && (int)instance.gameState == 4 && self.acceptingInput && self.pauseAllowed)
		{
			Diagnostics.Write("PAUSE keyboard fallback (controller action did not receive Escape)");
			((MonoBehaviour)self).StartCoroutine(instance.PauseGameToggle());
		}
	}

	private static IEnumerator PlayerDead(orig_PlayerDead orig, GameManager self, float wait)
	{
		if (PlayerContext.Current != null && PlayerContext.Current.Index != 0)
		{
			return Empty();
		}
		return orig.Invoke(self, wait);
	}

	private static IEnumerator PlayerDeadHazard(orig_PlayerDeadFromHazard orig, GameManager self, float wait)
	{
		if (PlayerContext.Current != null && PlayerContext.Current.Index != 0)
		{
			return Empty();
		}
		return orig.Invoke(self, wait);
	}

	private static void SaveGame(orig_SaveGame orig, GameManager self)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig.Invoke(self);
			return;
		}
		using (new SaveScope(session))
		{
			orig.Invoke(self);
		}
	}

	private static GameObject PoolSpawn(orig_Spawn_GameObject_Transform_Vector3_Quaternion orig, GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
	{
		GameObject val = orig.Invoke(prefab, parent, position, rotation);
		Spawned(val);
		SummonRouting.Observed(prefab, parent, val);
		return val;
	}

	private static void HatchlingFixed(orig_FixedUpdate orig, KnightHatchling self)
	{
		CoopSession session = Session;
		using (PlayerContext.Enter((session != null && session.Active) ? session.Resolve(self) : null))
		{
			orig.Invoke(self);
		}
	}

	private static IEnumerator HatchlingSpawn(orig_Spawn orig, KnightHatchling self)
	{
		IEnumerator enumerator = orig.Invoke(self);
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? session.Resolve(self) : null);
		if (playerSlot == null)
		{
			return enumerator;
		}
		return new ScopedRoutine(playerSlot, enumerator);
	}

	private static IEnumerator HatchlingTeleEnd(orig_TeleEnd orig, KnightHatchling self)
	{
		IEnumerator enumerator = orig.Invoke(self);
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? session.Resolve(self) : null);
		if (playerSlot == null)
		{
			return enumerator;
		}
		return new ScopedRoutine(playerSlot, enumerator);
	}

	private unsafe static void Spawned(GameObject result)
	{
		PlayerSlot playerSlot = PlayerContext.Current;
		CoopSession session = Session;
		if (!Object.op_Implicit((Object)(object)result) || session == null || !session.Active || Object.op_Implicit((Object)(object)result.GetComponent<HeroController>()))
		{
			return;
		}
		bool flag = SummonRouting.Familiar(result);
		bool flag2 = (Object)(object)result.GetComponentInChildren<DamageEnemies>(true) != (Object)null;
		PlayerSlot playerSlot2;
		Vector3 val;
		if (flag2 && !flag && !PlayerContext.TargetingEnemy)
		{
			playerSlot2 = null;
			float num = 9f;
			foreach (PlayerSlot player in session.Players)
			{
				if (player.Alive && player.Ready)
				{
					val = ((Component)player.Hero).transform.position - result.transform.position;
					float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						playerSlot2 = player;
					}
				}
			}
			if (playerSlot2 != null)
			{
				if (playerSlot == null)
				{
					goto IL_011e;
				}
				if (playerSlot != playerSlot2)
				{
					val = ((Component)playerSlot.Hero).transform.position - result.transform.position;
					if (((Vector3)(ref val)).sqrMagnitude > num + 4f)
					{
						goto IL_011e;
					}
				}
			}
		}
		goto IL_0121;
		IL_0121:
		if (PlayerContext.TargetingEnemy || playerSlot == null || Object.op_Implicit((Object)(object)result.GetComponentInChildren<HealthManager>(true)) || (Object.op_Implicit((Object)(object)result.GetComponentInChildren<DamageHero>(true)) && !Object.op_Implicit((Object)(object)result.GetComponentInChildren<DamageEnemies>(true))))
		{
			session.RefreshOwnership(result, null);
			return;
		}
		(result.GetComponent<OwnerTag>() ?? result.AddComponent<OwnerTag>()).Player = playerSlot;
		session.RefreshOwnership(result, playerSlot);
		RebindReferences(result, playerSlot);
		PvpCombat.Track(result, playerSlot, reset: true);
		if (flag)
		{
			Diagnostics.Write("SUMMON born P" + (playerSlot.Index + 1) + " prefab=" + ((Object)result).name);
		}
		if (!flag2)
		{
			return;
		}
		string text = ((Object)result).name.ToLowerInvariant();
		if (text.Contains("fireball") || text.Contains("vengeful"))
		{
			float num2 = (playerSlot.Hero.cState.facingRight ? 1f : (-1f));
			Rigidbody2D component = result.GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component) && Mathf.Abs(component.velocity.x) > 0.1f && Mathf.Sign(component.velocity.x) != num2)
			{
				component.velocity = new Vector2(Mathf.Abs(component.velocity.x) * num2, component.velocity.y);
			}
			Vector3 localScale = result.transform.localScale;
			if (Mathf.Abs(localScale.x) > 0.01f && Mathf.Sign(localScale.x) != num2)
			{
				localScale.x = Mathf.Abs(localScale.x) * num2;
				result.transform.localScale = localScale;
			}
		}
		string[] obj = new string[8]
		{
			"SPELL projectile P",
			(playerSlot.Index + 1).ToString(),
			" prefab=",
			((Object)result).name,
			" at=",
			null,
			null,
			null
		};
		val = result.transform.position;
		obj[5] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
		obj[6] = " facing=";
		obj[7] = playerSlot.Hero.cState.facingRight.ToString();
		Diagnostics.Write(string.Concat(obj));
		return;
		IL_011e:
		playerSlot = playerSlot2;
		goto IL_0121;
	}

	private static void SpellPoolAction(orig_OnEnter orig, SpawnObjectFromGlobalPool self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active && ((FsmStateAction)self).Fsm != null && ((FsmStateAction)self).Fsm.Name == "Spell Control") ? session.Resolve(((FsmStateAction)self).Fsm) : null);
		orig.Invoke(self);
		if (playerSlot != null && self.storeObject != null)
		{
			SpellBorn(self.storeObject.Value, playerSlot);
		}
	}

	private static void SpellCreateAction(orig_OnEnter orig, CreateObject self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active && ((FsmStateAction)self).Fsm != null && ((FsmStateAction)self).Fsm.Name == "Spell Control") ? session.Resolve(((FsmStateAction)self).Fsm) : null);
		orig.Invoke(self);
		if (playerSlot != null && self.storeObject != null)
		{
			SpellBorn(self.storeObject.Value, playerSlot);
		}
	}

	private unsafe static void SpellBorn(GameObject projectile, PlayerSlot caster)
	{
		CoopSession session = Session;
		if (!Object.op_Implicit((Object)(object)projectile) || caster == null || !Object.op_Implicit((Object)(object)caster.Hero) || session == null || !session.Active)
		{
			return;
		}
		bool flag = (Object)(object)projectile.GetComponentInChildren<DamageEnemies>(true) != (Object)null;
		if (!flag)
		{
			PlayMakerFSM[] componentsInChildren = projectile.GetComponentsInChildren<PlayMakerFSM>(true);
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
		string text = ((Object)projectile).name.ToLowerInvariant();
		Vector3 val;
		if (text.Contains("fireball") || text.Contains("vengeful"))
		{
			float num = (caster.Hero.cState.facingRight ? 1f : (-1f));
			val = projectile.transform.position - ((Component)caster.Hero).transform.position;
			if (((Vector3)(ref val)).sqrMagnitude > 16f)
			{
				Vector3 val2 = ((Component)caster.Hero).transform.position + new Vector3(num * 1.15f, 0.15f, 0f);
				projectile.transform.position = val2;
				Rigidbody2D component = projectile.GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component))
				{
					component.position = Vector2.op_Implicit(val2);
				}
			}
			Rigidbody2D component2 = projectile.GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component2) && Mathf.Abs(component2.velocity.x) > 0.1f)
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
		string[] obj = new string[8]
		{
			"SPELL caster P",
			(caster.Index + 1).ToString(),
			" via=",
			text,
			" pos=",
			null,
			null,
			null
		};
		val = projectile.transform.position;
		obj[5] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
		obj[6] = " face=";
		obj[7] = caster.Hero.cState.facingRight.ToString();
		Diagnostics.Write(string.Concat(obj));
	}

	private static void EnemyUpdate(orig_Update orig, LineOfSightDetector self)
	{
		CoopSession session = Session;
		using (PlayerContext.Enter((session != null && session.Active) ? session.Nearest(((Component)self).transform.position) : null, enemy: true))
		{
			orig.Invoke(self);
		}
	}

	private static void SpellOrb(orig_OnEnable orig, SpellGetOrb self)
	{
		orig.Invoke(self);
		ScriptedParty.SpellOrb(self);
	}

	private static PlayerSlot FsmPlayer(Fsm fsm, out bool enemy)
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
		if (playerSlot == null && Object.op_Implicit((Object)(object)fsm.GameObject))
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
			if ((Object.op_Implicit((Object)(object)value.Health) && !value.Health.GetIsDead()) || Object.op_Implicit((Object)(object)value.Damage))
			{
				playerSlot = session.Nearest(fsm.GameObject.transform.position);
				enemy = true;
			}
		}
		PlayerSlot playerSlot2 = playerSlot;
		GameObject gameObject = fsm.GameObject;
		if (Object.op_Implicit((Object)(object)gameObject) && ((Object)gameObject).name.ToLowerInvariant().Contains("soul"))
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
				WorldRouting.warpAt = new Vector3(Time.unscaledTime, (float)playerSlot.Index, 104f);
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
	}

	private static void RunFsm(Action action, Fsm self, bool frame)
	{
		bool enemy;
		PlayerSlot playerSlot = FsmPlayer(self, out enemy);
		if ((frame && playerSlot != null && (ScriptedParty.Holds(playerSlot) || playerSlot.InputBlocked || playerSlot.Down || playerSlot.Hazard || playerSlot.Retiring || !playerSlot.Ready || (playerSlot.Index > 0 && Charms.NativeMenuOpen) || (Plugin.Self.Panel && Charms.Editing && playerSlot.Index == Charms.Selected))) || (frame && playerSlot != null && Session.Resolve(self) == playerSlot && self.Name == "Spell Control" && (playerSlot.Reviving || (!playerSlot.FocusReleased && playerSlot.Actions != null && ((OneAxisInputControl)playerSlot.Actions.cast).IsPressed))))
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

	private static void FsmAwake(orig_Awake orig, Fsm self)
	{
		RunFsm(delegate
		{
			orig.Invoke(self);
		}, self, frame: false);
	}

	private static void FsmEnable(orig_OnEnable orig, Fsm self)
	{
		RunFsm(delegate
		{
			orig.Invoke(self);
		}, self, frame: false);
		PvpCombat.TrackFsm(self);
	}

	private static void FsmStart(orig_Start orig, Fsm self)
	{
		InteractionRouter.Ready(self);
		RunFsm(delegate
		{
			orig.Invoke(self);
		}, self, frame: false);
		PvpCombat.TrackFsm(self);
	}

	private static void StateEnter(orig_OnEnter orig, FsmState self)
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
					FsmStateAction[] actions = self.Actions;
					foreach (FsmStateAction val in actions)
					{
						if (val != null)
						{
							RebindObject(val, playerSlot);
						}
					}
				}
				orig.Invoke(self);
			}
		}
		BenchSeats.Observe(self.Fsm, playerSlot);
	}

	private static bool FrameAllowed(Fsm self, out PlayerSlot p, out bool enemy)
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
		if (flag && (ScriptedParty.Holds(p) || ChallengeSequence.Holds(p) || EmergencyWarp.Active(p) || CoopEnding.BlocksFsm(p, self) || BenchSeats.Custom(p)))
		{
			return false;
		}
		if (flag && (p.ArenaTransfer || p.Down || p.Hazard || p.Retiring || !p.Ready || (p.Index > 0 && (p.InputBlocked || Charms.NativeMenuOpen)) || (Plugin.Self.Panel && Charms.Editing && p.Index == Charms.Selected)))
		{
			return false;
		}
		if (session != null && session.Resolve(self) == p && self.Name == "Spell Control" && (p.Reviving || (!p.FocusReleased && p.Actions != null && ((OneAxisInputControl)p.Actions.cast).IsPressed)))
		{
			return false;
		}
		return true;
	}

	private static void FsmUpdate(orig_Update orig, Fsm self)
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
						orig.Invoke(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmFixed(orig_FixedUpdate orig, Fsm self)
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
						orig.Invoke(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmLate(orig_LateUpdate orig, Fsm self)
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
						orig.Invoke(self);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmEvent(orig_ProcessEvent orig, Fsm self, FsmEvent evt, FsmEventData data)
	{
		if (ChallengeSequence.Intercept(self, evt) || InteractionRouter.Blocks(self) || (Session != null && Session.Active && !InteractionRouter.EventAllowed(self, data)))
		{
			return;
		}
		if (Session != null && Session.Active)
		{
			StagMenuRouting.Observe(self, evt, data);
		}
		ShareRoar(self, evt);
		ArenaGather.Observe(self, evt, data);
		if (Session != null && Session.Active && Lifeblood.Award(evt))
		{
			return;
		}
		PlayerSlot current = PlayerContext.Current;
		CoopSession session = Session;
		if (session != null && session.Active && current != null && evt != null && localEvents.Contains(evt.Name))
		{
			PlayerSlot playerSlot = session.Resolve(self);
			if (playerSlot != null && playerSlot != current)
			{
				return;
			}
		}
		if (session != null && session.Active && current != null && current.Index > 0 && evt != null && IsVitalsHud(self) && (localEvents.Contains(evt.Name) || evt.Name.StartsWith("MP ") || evt.Name == "ADD BLUE HEALTH"))
		{
			return;
		}
		try
		{
			RunFsm(delegate
			{
				orig.Invoke(self, evt, data);
			}, self, frame: false);
		}
		finally
		{
			StagMenuRouting.AfterEvent(self, evt);
		}
	}

	private static void ShareRoar(Fsm source, FsmEvent evt)
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
		FsmGameObject val = source.Variables.FindFsmGameObject("Roar Object");
		sharingRoar = true;
		try
		{
			foreach (PlayerSlot player in session.Players)
			{
				if (player == playerSlot || !player.Ready || !player.Alive)
				{
					continue;
				}
				PlayMakerFSM val2 = PlayMakerFSM.FindFsmOnGameObject(((Component)player.Hero).gameObject, "Roar Lock");
				if (Object.op_Implicit((Object)(object)val2))
				{
					FsmGameObject val3 = val2.FsmVariables.FindFsmGameObject("Roar Object");
					if (val3 != null && val != null)
					{
						val3.Value = val.Value;
					}
					using (PlayerContext.Enter(player))
					{
						val2.SendEvent(evt.Name);
					}
				}
			}
		}
		finally
		{
			sharingRoar = false;
		}
	}

	private static bool AllowInteraction(Fsm self, Collider2D other, bool entering)
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

	private static bool SecondaryWaterCollision(Fsm self, Collision2D collision)
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

	private static void WaterCollisionEnter(orig_OnCollisionEnter2D orig, Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void WaterCollisionStay(orig_OnCollisionStay2D orig, Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void WaterCollisionExit(orig_OnCollisionExit2D orig, Fsm self, Collision2D c)
	{
		if (!SecondaryWaterCollision(self, c))
		{
			orig.Invoke(self, c);
		}
	}

	private static void ListenUp(orig_CheckForInput orig, ListenForUp self)
	{
		PlayerSlot playerSlot = StagMenuRouting.Resolve(((FsmStateAction)self).Fsm);
		if (playerSlot != null)
		{
			using (PlayerContext.Enter(playerSlot))
			{
				orig.Invoke(self);
				return;
			}
		}
		CoopSession session = Session;
		PlayerSlot p = null;
		if (session != null && session.Active && !InteractionRouter.InputOwner(((FsmStateAction)self).Fsm, out p))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig.Invoke(self);
		}
	}

	private static void ListenDown(orig_CheckForInput orig, ListenForDown self)
	{
		PlayerSlot playerSlot = StagMenuRouting.Resolve(((FsmStateAction)self).Fsm);
		if (playerSlot != null)
		{
			using (PlayerContext.Enter(playerSlot))
			{
				orig.Invoke(self);
				return;
			}
		}
		CoopSession session = Session;
		PlayerSlot p = null;
		if (session != null && session.Active && !InteractionRouter.InputOwner(((FsmStateAction)self).Fsm, out p))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			orig.Invoke(self);
		}
	}

	private static void InteractionCall(orig_OnEnter orig, CallMethodProper self)
	{
		CoopSession session = Session;
		PlayerSlot playerSlot = ((session != null && session.Active) ? (PickupCard.Resolve(((FsmStateAction)self).Fsm) ?? ShopMenuRouting.Resolve(((FsmStateAction)self).Fsm) ?? InteractionRouter.Resolve(((FsmStateAction)self).Fsm) ?? WorldRouting.Resolve(((FsmStateAction)self).Fsm) ?? PlayerContext.Current) : null);
		if (playerSlot != null)
		{
			RebindObject(self, playerSlot);
			Reflect.Set(self, "cachedBehaviour", null);
			Reflect.Set(self, "cachedMethodInfo", null);
			Reflect.Set(self, "cachedType", null);
			Reflect.Set(self, "component", null);
		}
		PickupCard.ObserveCall(((FsmStateAction)self).Fsm, playerSlot, (self.methodName == null) ? null : self.methodName.Value);
		using (PlayerContext.Enter(playerSlot))
		{
			orig.Invoke(self);
		}
	}

	private static void CreatePickupCard(orig_OnEnter orig, CreateUIMsgGetItem self)
	{
		orig.Invoke(self);
		PickupCard.Created(((FsmStateAction)self).Fsm, (self.storeObject == null) ? null : self.storeObject.Value);
	}

	private static void PickupJump(orig_OnUpdate orig, ListenForJump self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(((FsmStateAction)self).Fsm)))
		{
			orig.Invoke(self);
		}
	}

	private static void PickupMenuActions(orig_OnUpdate orig, ListenForMenuActions self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(((FsmStateAction)self).Fsm) ?? ShopMenuRouting.ListenerOwner(((FsmStateAction)self).Fsm)))
		{
			orig.Invoke(self);
		}
	}

	private static void PickupMenuSubmit(orig_OnUpdate orig, ListenForMenuSubmit self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(((FsmStateAction)self).Fsm) ?? ShopMenuRouting.ListenerOwner(((FsmStateAction)self).Fsm)))
		{
			orig.Invoke(self);
		}
	}

	private static void PickupMenuCancel(orig_OnUpdate orig, ListenForMenuCancel self)
	{
		using (PlayerContext.Enter(PickupCard.InputOwner(((FsmStateAction)self).Fsm) ?? ShopMenuRouting.ListenerOwner(((FsmStateAction)self).Fsm)))
		{
			orig.Invoke(self);
		}
	}

	private static void QuickMapUpdate(orig_OnUpdate orig, ListenForQuickMap self)
	{
		CoopSession session = Session;
		if (session == null || !session.Active)
		{
			orig.Invoke(self);
			return;
		}
		NativeQuickMap.Observe(session);
		using (PlayerContext.Enter(NativeQuickMap.Owner ?? session.Primary))
		{
			orig.Invoke(self);
		}
		NativeQuickMap.AfterListener();
	}

	private static void MapUpdate(orig_Update orig, GameMap self)
	{
		NativeQuickMap.MapUpdate(self, orig);
	}

	private static void FsmTriggerEnter(orig_OnTriggerEnter2D orig, Fsm self, Collider2D other)
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
						orig.Invoke(self, other);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmTriggerStay(orig_OnTriggerStay2D orig, Fsm self, Collider2D other)
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
						orig.Invoke(self, other);
					}
					finally
					{
						WorldRouting.End(self, before);
					}
				}
			}
		}
	}

	private static void FsmTriggerExit(orig_OnTriggerExit2D orig, Fsm self, Collider2D other)
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
				orig.Invoke(self, other);
			}
			finally
			{
				WorldRouting.End(self, before);
			}
		}
	}

	private static bool IsVitalsHud(Fsm f)
	{
		if (!Object.op_Implicit((Object)(object)f.GameObject))
		{
			return false;
		}
		GameCameras instance = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.hudCanvas) || !f.GameObject.transform.IsChildOf(instance.hudCanvas.transform))
		{
			return false;
		}
		string text = (f.Name + " " + ((Object)f.GameObject).name).ToLowerInvariant();
		if (!text.Contains("health") && !text.Contains("soul"))
		{
			return text.Contains("orb");
		}
		return true;
	}

	private static IEnumerator Empty()
	{
		yield break;
	}

	internal static void PatchActor(GameObject go)
	{
	}

	internal static void RebindActor(PlayerSlot p)
	{
		if (p != null && Object.op_Implicit((Object)(object)p.Hero))
		{
			using (PlayerContext.Enter(p))
			{
				RebindReferences(((Component)p.Hero).gameObject, p);
			}
		}
	}

	private static void RebindReferences(GameObject root, PlayerSlot p)
	{
		if (Session == null || p == null)
		{
			return;
		}
		MonoBehaviour[] componentsInChildren = root.GetComponentsInChildren<MonoBehaviour>(true);
		foreach (MonoBehaviour val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val) && !(val is OwnerTag) && !(val is PlayMakerFSM))
			{
				RebindObject(val, p);
			}
		}
		PlayMakerFSM[] componentsInChildren2 = root.GetComponentsInChildren<PlayMakerFSM>(true);
		foreach (PlayMakerFSM val2 in componentsInChildren2)
		{
			fsmBindings.Remove(val2.Fsm);
			Retarget(val2.Fsm, p);
		}
	}

	private static FieldInfo[] GetReferenceFields(Type type)
	{
		if (referenceFields.TryGetValue(type, out var value))
		{
			return value;
		}
		List<FieldInfo> list = new List<FieldInfo>();
		Type type2 = type;
		while (type2 != null && type2 != typeof(MonoBehaviour) && type2 != typeof(FsmStateAction))
		{
			FieldInfo[] fields = type2.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				Type fieldType = fieldInfo.FieldType;
				if (!fieldInfo.IsInitOnly && (fieldType == typeof(HeroControllerStates) || fieldType == typeof(PlayerData) || fieldType == typeof(HeroActions) || fieldType == typeof(FsmEventTarget) || typeof(Object).IsAssignableFrom(fieldType) || fieldType == typeof(FsmGameObject) || fieldType == typeof(FsmObject) || fieldType == typeof(FsmOwnerDefault)))
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

	internal static Object Mapped(Object value, PlayerSlot p)
	{
		if (!Object.op_Implicit(value))
		{
			return value;
		}
		CoopSession session = Session;
		PlayerSlot playerSlot = session.Resolve(value);
		if (playerSlot == null || playerSlot == p)
		{
			return value;
		}
		GameObject val = (GameObject)(object)((value is GameObject) ? value : null);
		if (Object.op_Implicit((Object)(object)val))
		{
			return (Object)(object)session.Remap(val, playerSlot, p);
		}
		Component val2 = (Component)(object)((value is Component) ? value : null);
		if (!Object.op_Implicit((Object)(object)val2))
		{
			return value;
		}
		GameObject val3 = session.Remap(val2.gameObject, playerSlot, p);
		if (!Object.op_Implicit((Object)(object)val3))
		{
			return value;
		}
		PlayMakerFSM val4 = (PlayMakerFSM)(object)((val2 is PlayMakerFSM) ? val2 : null);
		if (Object.op_Implicit((Object)(object)val4))
		{
			PlayMakerFSM[] components = val3.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM val5 in components)
			{
				if (val5.FsmName == val4.FsmName)
				{
					return (Object)(object)val5;
				}
			}
			return value;
		}
		Component[] components2 = val2.gameObject.GetComponents(((object)val2).GetType());
		Component[] components3 = val3.GetComponents(((object)val2).GetType());
		for (int j = 0; j < components2.Length; j++)
		{
			if ((Object)(object)components2[j] == (Object)(object)val2)
			{
				if (j >= components3.Length)
				{
					return value;
				}
				return (Object)(object)components3[j];
			}
		}
		return value;
	}

	private static void RebindObject(object obj, PlayerSlot p)
	{
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
			FsmGameObject val = (FsmGameObject)((value is FsmGameObject) ? value : null);
			if (val != null)
			{
				_003F val2 = val;
				Object obj2 = Mapped((Object)(object)val.Value, p);
				((FsmGameObject)val2).Value = (GameObject)(object)((obj2 is GameObject) ? obj2 : null);
				continue;
			}
			FsmObject val3 = (FsmObject)((value is FsmObject) ? value : null);
			if (val3 != null)
			{
				val3.Value = Mapped(val3.Value, p);
				continue;
			}
			FsmOwnerDefault val4 = (FsmOwnerDefault)((value is FsmOwnerDefault) ? value : null);
			if (val4 != null)
			{
				if (val4.GameObject != null)
				{
					_003F val5 = val4.GameObject;
					Object obj3 = Mapped((Object)(object)val4.GameObject.Value, p);
					((FsmGameObject)val5).Value = (GameObject)(object)((obj3 is GameObject) ? obj3 : null);
				}
				continue;
			}
			FsmEventTarget val6 = (FsmEventTarget)((value is FsmEventTarget) ? value : null);
			if (val6 != null)
			{
				if (val6.gameObject != null && val6.gameObject.GameObject != null)
				{
					_003F val7 = val6.gameObject.GameObject;
					Object obj4 = Mapped((Object)(object)val6.gameObject.GameObject.Value, p);
					((FsmGameObject)val7).Value = (GameObject)(object)((obj4 is GameObject) ? obj4 : null);
				}
				if (Object.op_Implicit((Object)(object)val6.fsmComponent))
				{
					ref PlayMakerFSM fsmComponent = ref val6.fsmComponent;
					Object obj5 = Mapped((Object)(object)val6.fsmComponent, p);
					fsmComponent = (PlayMakerFSM)(object)((obj5 is PlayMakerFSM) ? obj5 : null);
				}
				continue;
			}
			Object val8 = (Object)((value is Object) ? value : null);
			if (Object.op_Implicit(val8))
			{
				Object val9 = Mapped(val8, p);
				if (val9 != val8 && val9 != (Object)null && fieldType.IsInstanceOfType(val9))
				{
					fieldInfo.SetValue(obj, val9);
				}
			}
		}
	}

	internal static void Retarget(Fsm fsm, PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || fsm.Variables == null)
		{
			return;
		}
		FsmGameObject[] gameObjectVariables = fsm.Variables.GameObjectVariables;
		foreach (FsmGameObject val in gameObjectVariables)
		{
			if (val != null)
			{
				_003F val2 = val;
				Object obj = Mapped((Object)(object)val.Value, p);
				((FsmGameObject)val2).Value = (GameObject)(object)((obj is GameObject) ? obj : null);
			}
		}
		FsmObject[] objectVariables = fsm.Variables.ObjectVariables;
		foreach (FsmObject val3 in objectVariables)
		{
			if (val3 != null)
			{
				val3.Value = Mapped(val3.Value, p);
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
			FsmState[] states = fsm.States;
			foreach (FsmState val4 in states)
			{
				if (val4.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = val4.Actions;
				foreach (FsmStateAction val5 in actions)
				{
					if (val5 != null)
					{
						RebindObject(val5, p);
					}
				}
			}
		}
		else if (fsm.ActiveState != null && fsm.ActiveState.Actions != null)
		{
			FsmStateAction[] actions = fsm.ActiveState.Actions;
			foreach (FsmStateAction val6 in actions)
			{
				if (val6 != null)
				{
					RebindObject(val6, p);
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
