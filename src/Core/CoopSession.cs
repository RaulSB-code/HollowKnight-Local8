using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using GlobalEnums;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal sealed class CoopSession : IDisposable
{
	[CompilerGenerated]
	private sealed class __iterator__FinishSpawn_d__62 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		public int epoch;

		public CoopSession __iterator___4__this;

		public PlayerSlot s;

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
		public __iterator__FinishSpawn_d__62(int __iterator___1__state)
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
			int num = __iterator___1__state;
			CoopSession coopSession = __iterator___4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				__iterator___1__state = -1;
				__iterator___2__current = null;
				__iterator___1__state = 1;
				return true;
			case 1:
				__iterator___1__state = -1;
				__iterator___2__current = null;
				__iterator___1__state = 2;
				return true;
			case 2:
				__iterator___1__state = -1;
				if (epoch != coopSession.sceneEpoch)
				{
					return false;
				}
				if (!s.Hero || s.Retiring)
				{
					s.SpawnPending = false;
					return false;
				}
				try
				{
					Hooks.RebindActor(s);
					using (PlayerContext.Enter(s))
					{
						int health = coopSession.Data.health;
						int healthBlue = coopSession.Data.healthBlue;
						s.Hero.CharmUpdate();
						coopSession.Data.health = Math.Min(Math.Max(1, health), coopSession.Data.CurrentMaxHealth);
						coopSession.Data.healthBlue = healthBlue;
						ActorRecovery.Reset(s);
						s.Hero.AcceptInput();
						s.Hero.AffectedByGravity(gravityApplies: true);
						s.Hero.SetDamageMode(DamageMode.FULL_DAMAGE);
						Reflect.Set(s.Hero, "isGameplayScene", true);
						Reflect.Set(s.Hero, "transitionState", HeroTransitionState.WAITING_TO_TRANSITION);
						Reflect.Set(s.Hero, "tilemapTestActive", false);
						HeroAnimationController component = s.Hero.GetComponent<HeroAnimationController>();
						if ((bool)component)
						{
							Reflect.Set(component, "waitingToEnter", false);
						}
						s.Hero.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
					}
					PlayerLighting.Sync(coopSession.Primary, s);
					s.Ready = true;
					s.ProtectionUntil = Time.time + 2f;
					s.Hero.transform.position = coopSession.JoinDestination();
					if (float.IsPositiveInfinity(s.JoinVisualUntil))
					{
						s.JoinVisualUntil = (coopSession.plugin.Panel ? 0f : (Time.unscaledTime + 0.65f));
					}
					SummonRouting.Refresh(s);
					RestoreLivingVisuals(s);
					if (s.Vitals.HazardPoint == Vector3.zero && coopSession.Primary != null)
					{
						s.Vitals.HazardPoint = coopSession.Primary.Vitals.HazardPoint;
					}
					s.SafePoint = s.Hero.transform.position;
					s.HasSafePoint = coopSession.IsSafe(s.SafePoint, s);
					if (s.SpawnAsDown)
					{
						s.SpawnAsDown = false;
						coopSession.Down(s, spawnShade: false);
					}
					Diagnostics.Snapshot(coopSession, "spawn-ready-P" + (s.Index + 1));
				}
				catch (Exception ex)
				{
					coopSession.PlayerFault(s, "finish-spawn", ex);
				}
				finally
				{
					s.SpawnPending = false;
				}
				return false;
			}
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

	internal readonly List<PlayerSlot> Players = new List<PlayerSlot>();

	internal PlayerSlot Cloning;

	internal PlayerData Data;

	internal bool Active;

	internal bool TeamWipe;

	internal bool AllowVanillaDeath;

	internal readonly CameraRig Camera = new CameraRig();

	private readonly Local8Runtime plugin;

	private readonly Dictionary<int, PlayerSlot> owners = new Dictionary<int, PlayerSlot>();

	private readonly HashSet<int> noOwnerThisFrame = new HashSet<int>();

	private int ownerFrame = -1;

	private HeroActions originalActions;

	private string scene;

	private float readyAt;

	private bool transitionProxy;

	internal Vector3 Arrival;

	internal bool HasArrival;

	private int sceneEpoch;

	private float nextPerformanceLog;

	private float performanceStart;

	private float worstFrame;

	private int performanceFrames;

	private int slowFrames;

	private int gcCollections;

	private int perfPlayers;

	private static readonly Collider2D[] safetyHits = new Collider2D[32];

	private bool suspended;

	private Vector3 benchPosition;

	private bool restedBench;

	private bool benchWasActive;

	private float bindingCheck;

	private string bindingSignature;

	private RestBench[] roomBenches = new RestBench[0];

	private float nextBenchScan;

	private readonly Dictionary<PlayerSlot, float> stuckSince = new Dictionary<PlayerSlot, float>();

	private readonly Dictionary<PlayerSlot, Vector3> stuckPosition = new Dictionary<PlayerSlot, Vector3>();

	internal static readonly Color[] Colors = new Color[8]
	{
		new Color(0.82f, 0.92f, 1f),
		new Color(0.4f, 0.85f, 1f),
		new Color(1f, 0.55f, 0.45f),
		new Color(0.55f, 1f, 0.6f),
		new Color(0.95f, 0.65f, 1f),
		new Color(1f, 0.88f, 0.4f),
		new Color(0.55f, 0.62f, 1f),
		new Color(1f, 0.64f, 0.84f)
	};

	internal PlayerSlot Primary
	{
		get
		{
			if (Players.Count != 0)
			{
				return Players[0];
			}
			return null;
		}
	}

	internal bool EntryProxy
	{
		get
		{
			GameManager instance = GameManager.instance;
			if (Active && !TeamWipe && Primary != null && Primary.Down && (bool)instance && instance.IsGameplayScene())
			{
				return !instance.HasFinishedEnteringScene;
			}
			return false;
		}
	}

	internal bool Gameplay
	{
		get
		{
			GameManager instance = GameManager.instance;
			if ((bool)instance && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene)
			{
				return instance.gameState == GameState.PLAYING;
			}
			return false;
		}
	}

	internal int LivingCombatants
	{
		get
		{
			int num = 0;
			foreach (PlayerSlot player in Players)
			{
				if (player.Ready && player.Alive && player.Vitals.Health + player.Vitals.Blue > 0)
				{
					num++;
				}
			}
			return num;
		}
	}

	internal bool CanJoin
	{
		get
		{
			GameManager instance = GameManager.instance;
			if ((bool)instance && (bool)instance.hero_ctrl)
			{
				return CoopRules.CanQueueJoin(instance.IsGameplayScene(), instance.IsLoadingSceneTransition, instance.HasFinishedEnteringScene, instance.gameState == GameState.PLAYING, instance.isPaused || instance.gameState == GameState.PAUSED);
			}
			return false;
		}
	}

	internal CoopSession(Local8Runtime p)
	{
		plugin = p;
		scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
	}

	internal void Tick()
	{
		if (RoleSystem.PauseSession(this))
		{
			return;
		}
		RescueHint.Tick(this);
		GameManager gm = GameManager.instance;
		if (!gm)
		{
			return;
		}
		Controls.ProbeVirtual();
		if (CoopRules.ResetRoster(gm.IsMenuScene(), gm.IsTitleScreenScene(), gm.IsLoadingSceneTransition))
		{
			if (Players.Count > 0)
			{
				Diagnostics.Snapshot(this, "exit-to-menu");
				Dispose();
			}
		}
		else if (!gm.hero_ctrl || !gm.IsGameplayScene() || gm.IsLoadingSceneTransition)
		{
			Suspend();
		}
		else
		{
			if (PlayerContext.Current != null)
			{
				return;
			}
			if (Primary != null && Primary.Hero != gm.hero_ctrl)
			{
				if (Players.Skip(1).Any((PlayerSlot p) => p.Hero == gm.hero_ctrl))
				{
					return;
				}
				SkinBridge.Release(Primary);
				Primary.Hero = gm.hero_ctrl;
				Primary.RootLayer = gm.hero_ctrl.gameObject.layer;
				Primary.Ready = true;
				Data = gm.playerData;
				Primary.Capture(Data);
				owners.Clear();
				RegisterHierarchy(Primary);
				if (Primary.Down && !TeamWipe && !EntryProxy)
				{
					Hide(Primary);
				}
				Diagnostics.Snapshot(this, "primary-replaced-roster-preserved");
			}
			if (Primary == null)
			{
				if (!CanJoin)
				{
					return;
				}
				Data = gm.playerData;
				PlayerSlot playerSlot = new PlayerSlot
				{
					Index = 0,
					Hero = gm.hero_ctrl,
					Color = ColorFor(0),
					Ready = true,
					LifeStartedAt = Time.unscaledTime,
					SkinId = Local8Mod.Settings.SkinIds[0]
				};
				playerSlot.Capture(Data);
				originalActions = gm.inputHandler.inputActions;
				InputDevice inputDevice = Controls.Find(plugin.PrimaryDevice.Value);
				Controls.InitPrimary(playerSlot, originalActions, inputDevice ?? InputDevice.Null);
				Players.Add(playerSlot);
				RegisterHierarchy(playerSlot);
				Hooks.PatchActor(playerSlot.Hero.gameObject);
				playerSlot.RootLayer = playerSlot.Hero.gameObject.layer;
			}
			if (suspended)
			{
				suspended = false;
				readyAt = Time.unscaledTime + 0.35f;
				Diagnostics.Snapshot(this, "resume-roster");
			}
			if (transitionProxy && gm.HasFinishedEnteringScene)
			{
				RestoreTransitionProxy();
			}
			TransitionVote.Tick(this);
			Data = gm.playerData;
			if (PlayerContext.Current == null)
			{
				Primary.Capture(Data);
			}
			if (Primary.Down && Primary.RendererStates.Count == 0 && !TeamWipe && gm.HasFinishedEnteringScene)
			{
				Hide(Primary);
			}
			if (Time.unscaledTime >= bindingCheck)
			{
				bindingCheck = Time.unscaledTime + 1f;
				string text = Controls.Signature(originalActions);
				if (bindingSignature != null && text != bindingSignature)
				{
					foreach (PlayerSlot player in Players)
					{
						Controls.RebindPrimary(player, originalActions, player.Device);
					}
					plugin.Notice("Controles vanilla actualizados.");
				}
				bindingSignature = text;
			}
			Charms.InputTick(this);
			bool flag = Gameplay && Players.Any((PlayerSlot p) => (bool)p.Hero && p.Vitals.AtBench && ConfirmedBench(p));
			if (flag && !benchWasActive)
			{
				PlayerSlot playerSlot2 = Players.First((PlayerSlot p) => (bool)p.Hero && p.Vitals.AtBench && ConfirmedBench(p));
				BenchRest(playerSlot2.Hero);
			}
			benchWasActive = flag;
			foreach (PlayerSlot player2 in Players)
			{
				if (player2.Device == null || (player2.Device != InputDevice.Null && !player2.Device.IsAttached))
				{
					InputDevice inputDevice2 = Controls.Find(player2.DeviceKey);
					if (inputDevice2 != null)
					{
						Controls.Bind(player2, inputDevice2);
					}
				}
			}
			foreach (InputDevice d in Controls.Devices())
			{
				if (!plugin.Panel && Controls.JoinPressed(d) && !Players.Any((PlayerSlot p) => p.Device == d) && CanJoin)
				{
					Join(d);
				}
			}
			if (!Active)
			{
				return;
			}
			NativeQuickMap.Observe(this);
			foreach (PlayerSlot item in Players.Skip(1))
			{
				if (!item.Hero && item.Ready)
				{
					item.Hero = null;
					item.Ready = false;
					item.SpawnPending = false;
					Diagnostics.Write("ACTOR lost P" + (item.Index + 1) + " scene=" + scene + "; queued to reappear");
				}
			}
			ShopMenuRouting.Tick();
			StagMenuRouting.Tick();
			ScriptedParty.Tick(this);
			CameraFadeRecovery.Tick(this);
			DreamSequence.Tick(this);
			FlowerRules.Tick(this);
			PickupCard.Tick(this);
			ChallengeSequence.Tick(this);
			InteractionRouter.RecoverChallenge(this);
			WorldRouting.ObserveFrame(this);
			bool nativeMenuOpen = Charms.NativeMenuOpen;
			gm.inputHandler.inputActions = ((Gameplay || nativeMenuOpen) ? Primary.Actions : originalActions);
			if ((bool)Primary.Hero && Gameplay)
			{
				gm.inputHandler.AttachHeroController(Primary.Hero);
			}
			PlayerSlot activePlayer = InteractionRouter.ActivePlayer;
			PlayerSlot owner = PickupCard.Owner;
			bool flag2 = gm.isPaused || !Gameplay;
			foreach (PlayerSlot player3 in Players)
			{
				Controls.Pump(player3, ChallengeSequence.Holds(player3) || TransitionVote.Holding(player3) || (flag2 && !nativeMenuOpen && !CoopEnding.Active && ((activePlayer != player3 && owner != player3) || gm.isPaused)) || (plugin.Panel && (!Charms.Editing || player3.Index != Charms.Selected)) || player3.Down || player3.Hazard || PvpMatch.BlocksInput);
			}
			CoopEnding.Tick(this);
			ArenaGather.Tick(this);
			if (flag2 || plugin.Panel)
			{
				performanceStart = 0f;
				performanceFrames = (slowFrames = 0);
				worstFrame = 0f;
				return;
			}
			if (Players.Count > 1)
			{
				if (performanceStart <= 0f || perfPlayers != Players.Count)
				{
					performanceStart = Time.unscaledTime;
					nextPerformanceLog = performanceStart + 15f;
					performanceFrames = (slowFrames = 0);
					worstFrame = 0f;
					gcCollections = GC.CollectionCount(0);
					perfPlayers = Players.Count;
				}
				performanceFrames++;
				worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
				if (Time.unscaledDeltaTime > 0.05f)
				{
					slowFrames++;
				}
				if (Time.unscaledTime >= nextPerformanceLog)
				{
					float num = Mathf.Max(0.1f, Time.unscaledTime - performanceStart);
					Diagnostics.Write("PERF players=" + Players.Count + " fps_avg=" + Mathf.RoundToInt((float)performanceFrames / num) + " worst_ms=" + Mathf.RoundToInt(worstFrame * 1000f) + " frames_over_50ms=" + slowFrames + " gc0=" + (GC.CollectionCount(0) - gcCollections) + " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
					performanceFrames = (slowFrames = 0);
					worstFrame = 0f;
					gcCollections = GC.CollectionCount(0);
					performanceStart = Time.unscaledTime;
					nextPerformanceLog = performanceStart + 15f;
				}
			}
			if (TeamWipe || InteractionRouter.DoorFallbackTick(this))
			{
				return;
			}
			InteractionRouter.ObserveBenches();
			BenchSeats.Tick(this);
			WorldRouting.Tick(this);
			PlayerSlot[] array = Players.ToArray();
			foreach (PlayerSlot p2 in array)
			{
				try
				{
					if (p2.Index > 0 && !p2.Ready && !p2.SpawnPending && !p2.Faulted && Time.unscaledTime >= Mathf.Max(Mathf.Max(readyAt, p2.RetryAt), p2.JoinDelayAt) && (bool)Primary.Hero && !Primary.Hazard && !Primary.Hero.cState.transitioning)
					{
						if ((bool)p2.Hero)
						{
							ResumeParked(p2);
						}
						else
						{
							Spawn(p2);
						}
					}
					if (!p2.Hero || !p2.Ready)
					{
						continue;
					}
					CheckStuckControl(p2);
					if (ScriptedParty.Holds(p2) || EmergencyWarp.Active(p2) || CoopEnding.HoldsActor(p2) || BenchSeats.Custom(p2))
					{
						continue;
					}
					if (p2.Index > 0 && Time.unscaledTime >= p2.NextCharmUpdate)
					{
						p2.NextCharmUpdate = Time.unscaledTime + 0.5f;
						Charms.UpdateMaximum(p2, Data, heal: false);
					}
					if (p2.Hazard && Time.time >= p2.HazardUntil)
					{
						Recover(p2, respawn: false);
					}
					if (p2.Down && !PvpMatch.Running && plugin.TimedRespawn.Value && Time.time - p2.DownAt >= plugin.RespawnSeconds.Value && Players.Any((PlayerSlot q) => q != p2 && q.Alive))
					{
						Recover(p2, respawn: true);
					}
					if (!p2.Alive)
					{
						if ((p2.Down || p2.Hazard) && !EntryProxy)
						{
							ActorRecovery.Freeze(p2);
						}
						continue;
					}
					if (p2.Index > 0)
					{
						SoulReserve.Tick(p2);
					}
					if (p2.Index > 0)
					{
						AcidSwimming.Tick(p2, Data.hasAcidArmour);
					}
					if (p2.Vitals.AtBench && restedBench && Vector2.Distance(p2.Hero.transform.position, benchPosition) > 5f)
					{
						p2.Vitals.AtBench = false;
					}
					if (Time.unscaledTime >= p2.NextSafeCheck)
					{
						p2.NextSafeCheck = Time.unscaledTime + 0.3f;
						SaveSafePoint(p2);
					}
					if (p2.Vitals.Health <= 0)
					{
						Down(p2);
						continue;
					}
					if (Time.time < p2.ProtectionUntil)
					{
						p2.Hero.cState.invulnerable = true;
					}
					else if (p2.ProtectionUntil > 0f)
					{
						p2.ProtectionUntil = 0f;
						p2.Hero.cState.invulnerable = false;
					}
					if (Time.unscaledTime - p2.LastDamageAt > 4f && p2.ProtectionUntil <= 0f && !p2.Vitals.Invincible && !p2.Hero.cState.shadowDashing && !p2.Hero.cState.transitioning)
					{
						InvulnerablePulse component = p2.Hero.GetComponent<InvulnerablePulse>();
						if ((bool)component && Reflect.Get(component, "pulsing", fallback: false))
						{
							component.stopInvulnerablePulse();
							p2.Hero.cState.invulnerable = false;
							p2.Hero.cState.recoiling = false;
							Diagnostics.Write("DAMAGE pulse reset P" + (p2.Index + 1));
						}
					}
				}
				catch (Exception ex)
				{
					if (p2.Index == 0)
					{
						throw;
					}
					PlayerFault(p2, "tick/recover", ex);
				}
			}
			AcidSwimming.GuardPrimary();
			AcidSwimming.GuardPrimaryPosition();
			SummonRouting.Tick(this);
			if (PvpMatch.Tick(this))
			{
				return;
			}
			PlayerSlot[] array2 = Players.Where((PlayerSlot p) => p.Alive).ToArray();
			if (array2.Length == 0 && Players.All((PlayerSlot p) => p.Down))
			{
				Wipe();
				return;
			}
			if (!CoopEnding.Active)
			{
				TickRevive(array2.Where((PlayerSlot p) => !EmergencyWarp.Active(p)).ToArray());
			}
			EmergencyWarp.Tick(this, array2);
			NativeDreamFx.ObserveTeleports(this);
		}
	}

	internal void VisualTick()
	{
		if (suspended)
		{
			ShadeCloakRitual.VisualTick();
			return;
		}
		foreach (PlayerSlot player in Players)
		{
			if (!player.Hero)
			{
				continue;
			}
			if ((player.Down || player.Hazard) && (player != Primary || !EntryProxy))
			{
				ActorRecovery.Freeze(player);
				foreach (Renderer key in player.RendererStates.Keys)
				{
					if ((bool)key)
					{
						key.enabled = false;
					}
				}
			}
			Vector3 position = player.Hero.transform.position;
			position.z = (float)CoopRules.PlayerDepth(player.Index);
			player.Hero.transform.position = position;
			if (player.Index > 0 && player.Ready)
			{
				PlayerLighting.Sync(Primary, player);
			}
			if (player.Alive && player.Ready && !TransitionVote.Holding(player) && !ScriptedParty.Active && !EmergencyWarp.Active(player) && !CoopEnding.HoldsActor(player))
			{
				Renderer component = player.Hero.GetComponent<Renderer>();
				tk2dSprite component2 = player.Hero.GetComponent<tk2dSprite>();
				if ((((bool)component && !component.enabled) || ((bool)component2 && component2.color.a < 0.02f)) && !player.Hero.cState.transitioning && (player.Index > 0 || (Gameplay && (bool)UIManager.instance && UIManager.instance.uiState == UIState.PLAYING && !player.Hero.IsDreamReturning && !Reflect.Get(player.Hero, "<IsEnteringDream>k__BackingField", fallback: false) && !BenchSeats.Seated(player))) && !CoopEnding.Active)
				{
					if (player.InvisibleSince < 0f)
					{
						player.InvisibleSince = Time.unscaledTime;
					}
					else if (Time.unscaledTime - player.InvisibleSince > ((player.Index == 0) ? 2f : 1f))
					{
						if (player.Index == 0 && player.Hero.controlReqlinquished)
						{
							if (player.Actions == null || (!(Mathf.Abs(player.Actions.moveVector.X) > 0.25f) && !(Mathf.Abs(player.Actions.moveVector.Y) > 0.25f)) || Time.unscaledTime - player.InvisibleSince < 4f)
							{
								continue;
							}
							using (PlayerContext.Enter(player))
							{
								ActorRecovery.Reset(player);
							}
							Diagnostics.Write("SCRIPT primary recovered after invisible control lock");
						}
						if ((bool)component)
						{
							component.enabled = true;
						}
						if ((bool)component2)
						{
							Color color = component2.color;
							color.a = 1f;
							component2.color = color;
						}
						player.InvisibleSince = -1f;
						Diagnostics.Write("VISIBILITY restored P" + (player.Index + 1) + " scene=" + scene);
					}
				}
				else
				{
					player.InvisibleSince = -1f;
				}
			}
			if (Time.unscaledTime >= player.NextSkinUpdate)
			{
				player.NextSkinUpdate = Time.unscaledTime + 0.75f;
				SkinBridge.Apply(player);
			}
			if (plugin.TintPlayers.Value && !SkinBridge.HasSkin(player))
			{
				tk2dSprite component3 = player.Hero.GetComponent<tk2dSprite>();
				if ((bool)component3 && !player.Down && !player.Hazard)
				{
					component3.color = Color.Lerp(Color.white, player.Color, 0.32f);
				}
			}
		}
		ArenaGather.VisualTick(this);
		CombatEffects.UpdateVolumes(this);
		EmergencyWarp.VisualTick();
		CoopEnding.VisualTick(this);
		BenchSeats.VisualTick();
		TransitionVote.HideWaiting();
		ShadeCloakRitual.VisualTick();
	}

	private void CheckStuckControl(PlayerSlot p)
	{
		UIManager instance = UIManager.instance;
		if (!Gameplay || !p.Alive || !p.Connected || p.Actions == null || p.InputBlocked || p.Hero.cState.transitioning || p.Hero.IsDreamReturning || DreamSequence.InStoryDream || ScriptedParty.Active || CoopEnding.Active || PvpMatch.Running || EmergencyWarp.Active(p) || TransitionVote.Holding(p) || BenchSeats.Seated(p) || PickupCard.Owner != null || InteractionRouter.ActivePlayer != null || Charms.NativeMenuOpen || ShopMenuRouting.MenuVisible || StagMenuRouting.HasOwner || !instance || instance.uiState != UIState.PLAYING || Mathf.Abs(p.Actions.moveVector.X) < 0.35f)
		{
			stuckSince.Remove(p);
			stuckPosition.Remove(p);
			return;
		}
		Rigidbody2D component = p.Hero.GetComponent<Rigidbody2D>();
		HeroAnimationController component2 = p.Hero.GetComponent<HeroAnimationController>();
		if (!p.Hero.controlReqlinquished && p.Hero.acceptingInput && (!component2 || component2.controlEnabled) && (bool)component && component.simulated && !component.isKinematic && (p.Hero.cState.onGround || p.Hero.cState.swimming || p.AcidAssistActive || !(component.gravityScale <= 0.01f)))
		{
			stuckSince.Remove(p);
			stuckPosition.Remove(p);
			return;
		}
		Vector3 position = p.Hero.transform.position;
		if (!stuckSince.TryGetValue(p, out var value) || !stuckPosition.TryGetValue(p, out var value2) || Mathf.Abs(value2.x - position.x) > 0.4f)
		{
			stuckSince[p] = Time.unscaledTime;
			stuckPosition[p] = position;
		}
		else if (!(Time.unscaledTime - value < 2.8f))
		{
			stuckSince.Remove(p);
			stuckPosition.Remove(p);
			using (PlayerContext.Enter(p))
			{
				ActorRecovery.Reset(p);
				RestoreLivingVisuals(p);
			}
			p.ProtectionUntil = Time.time + 1f;
			string text = (p.Index + 1).ToString();
			Vector3 vector = position;
			Diagnostics.Write("CONTROL recovered P" + text + " after sustained movement input at=" + vector.ToString());
		}
	}

	internal void Join(InputDevice device)
	{
		if (Players.Count >= 8)
		{
			plugin.Notice("El grupo ya tiene ocho jugadores.");
			return;
		}
		if (!CanJoin || Primary == null)
		{
			plugin.Notice("Espera a terminar la carga de la sala para unirte.");
			return;
		}
		if (device != InputDevice.Null && Players.Any((PlayerSlot p) => p.Device == device))
		{
			plugin.Notice("Ese dispositivo ya esta asignado a un jugador.");
			return;
		}
		if (device == InputDevice.Null)
		{
			int num = 0;
			if (Players.Count > 0)
			{
				num += (Controls.IsKeyboard(Players[0].Device) ? 1 : 0);
			}
			if (Players.Count > 1)
			{
				num += (Controls.IsKeyboard(Players[1].Device) ? 1 : 0);
			}
			if (Players.Count > 2)
			{
				num += (Controls.IsKeyboard(Players[2].Device) ? 1 : 0);
			}
			if (Players.Count > 3)
			{
				num += (Controls.IsKeyboard(Players[3].Device) ? 1 : 0);
			}
			if (Players.Count > 4)
			{
				num += (Controls.IsKeyboard(Players[4].Device) ? 1 : 0);
			}
			if (Players.Count > 5)
			{
				num += (Controls.IsKeyboard(Players[5].Device) ? 1 : 0);
			}
			if (Players.Count > 6)
			{
				num += (Controls.IsKeyboard(Players[6].Device) ? 1 : 0);
			}
			if (Players.Count > 7)
			{
				num += (Controls.IsKeyboard(Players[7].Device) ? 1 : 0);
			}
			if (num >= 4)
			{
				return;
			}
		}
		PlayerSlot playerSlot = new PlayerSlot
		{
			Index = Players.Count,
			Color = ColorFor(Players.Count),
			LifeStartedAt = Time.unscaledTime,
			SkinId = Local8Mod.Settings.SkinIds[Players.Count]
		};
		playerSlot.Vitals.Read(Data);
		playerSlot.Vitals.Health = Data.maxHealth;
		playerSlot.Vitals.Soul = 0;
		playerSlot.Vitals.Reserve = 0;
		Charms.Load(playerSlot, Data);
		playerSlot.Vitals.Health = playerSlot.CurrentMaxHealth;
		playerSlot.Vitals.AtBench = false;
		playerSlot.Vitals.Blue = ((playerSlot.Vitals.Joni > 0) ? playerSlot.Vitals.Joni : ((playerSlot.Charms.Equipped[7] ? 2 : 0) + (playerSlot.Charms.Equipped[8] ? 4 : 0)));
		playerSlot.Vitals.HazardPoint = Primary.Hero.transform.position;
		playerSlot.Vitals.HazardRight = Primary.Vitals.HazardRight;
		playerSlot.Vitals.Invincible = false;
		playerSlot.Vitals.DisablePause = false;
		Controls.InitSecondary(playerSlot, originalActions, device);
		Players.Add(playerSlot);
		playerSlot.JoinDelayAt = Time.unscaledTime + (plugin.Panel ? 0f : 0.22f);
		playerSlot.JoinVisualUntil = float.PositiveInfinity;
		if (Players.Count == 2)
		{
			Hud.QueueRescueHint();
		}
		Active = true;
		playerSlot.InputBlocked = true;
		plugin.Notice("P" + (playerSlot.Index + 1) + " conectado: " + playerSlot.DeviceName + ". Aparecera al reanudar y cerrar F8.");
		Diagnostics.Snapshot(this, "join-queued");
	}

	internal void JoinFirstAvailable()
	{
		if (Players.Count >= 8)
		{
			plugin.Notice("El grupo ya tiene ocho jugadores.");
			return;
		}
		InputDevice inputDevice = Controls.Devices().FirstOrDefault((InputDevice d) => Players.All((PlayerSlot p) => p.Device != d));
		if (inputDevice == null && Players.All((PlayerSlot p) => !Controls.IsKeyboard(p.Device)))
		{
			inputDevice = InputDevice.Null;
		}
		if (inputDevice == null)
		{
			plugin.Notice("No hay dispositivos libres. Usa Asignar P1 en Teclado para liberar el primer mando.");
		}
		else
		{
			Join(inputDevice);
		}
	}

	internal void AssignPrimary(InputDevice device)
	{
		if (Primary == null || (device != InputDevice.Null && (device == null || Players.Skip(1).Any((PlayerSlot p) => p.Device == device))))
		{
			return;
		}
		if (device == InputDevice.Null)
		{
			int num = 0;
			if (Players.Count > 1)
			{
				num += (Controls.IsKeyboard(Players[1].Device) ? 1 : 0);
			}
			if (Players.Count > 2)
			{
				num += (Controls.IsKeyboard(Players[2].Device) ? 1 : 0);
			}
			if (Players.Count > 3)
			{
				num += (Controls.IsKeyboard(Players[3].Device) ? 1 : 0);
			}
			if (Players.Count > 4)
			{
				num += (Controls.IsKeyboard(Players[4].Device) ? 1 : 0);
			}
			if (Players.Count > 5)
			{
				num += (Controls.IsKeyboard(Players[5].Device) ? 1 : 0);
			}
			if (Players.Count > 6)
			{
				num += (Controls.IsKeyboard(Players[6].Device) ? 1 : 0);
			}
			if (Players.Count > 7)
			{
				num += (Controls.IsKeyboard(Players[7].Device) ? 1 : 0);
			}
			if (num >= 4)
			{
				return;
			}
		}
		Controls.RebindPrimary(Primary, originalActions, device);
		if (Players.Count > 1 && Controls.IsKeyboard(Players[1].Device))
		{
			Controls.Dispose(Players[1]);
			Controls.InitSecondary(Players[1], originalActions, InputDevice.Null);
		}
		if (Players.Count > 2 && Controls.IsKeyboard(Players[2].Device))
		{
			Controls.Dispose(Players[2]);
			Controls.InitSecondary(Players[2], originalActions, InputDevice.Null);
		}
		if (Players.Count > 3 && Controls.IsKeyboard(Players[3].Device))
		{
			Controls.Dispose(Players[3]);
			Controls.InitSecondary(Players[3], originalActions, InputDevice.Null);
		}
		if (Players.Count > 4 && Controls.IsKeyboard(Players[4].Device))
		{
			Controls.Dispose(Players[4]);
			Controls.InitSecondary(Players[4], originalActions, InputDevice.Null);
		}
		if (Players.Count > 5 && Controls.IsKeyboard(Players[5].Device))
		{
			Controls.Dispose(Players[5]);
			Controls.InitSecondary(Players[5], originalActions, InputDevice.Null);
		}
		if (Players.Count > 6 && Controls.IsKeyboard(Players[6].Device))
		{
			Controls.Dispose(Players[6]);
			Controls.InitSecondary(Players[6], originalActions, InputDevice.Null);
		}
		if (Players.Count > 7 && Controls.IsKeyboard(Players[7].Device))
		{
			Controls.Dispose(Players[7]);
			Controls.InitSecondary(Players[7], originalActions, InputDevice.Null);
		}
		plugin.PrimaryDevice.Value = Primary.DeviceKey;
		plugin.Notice("P1: " + Primary.DeviceName);
	}

	internal void RemoveLast()
	{
		if (Players.Count < 2)
		{
			return;
		}
		if (Primary.Down || Primary.Hazard)
		{
			plugin.Notice("Espera a que P1 reaparezca antes de quitar jugadores.");
			return;
		}
		TransitionVote.Reset();
		PvpMatch.Stop(restorePosition: false);
		PlayerSlot playerSlot = Players[Players.Count - 1];
		Charms.ReleaseNative();
		Charms.Save(this);
		CoopShades.Remove(playerSlot.Index);
		PvpCombat.Forget(playerSlot);
		Retire(playerSlot);
		Controls.Dispose(playerSlot);
		Players.RemoveAt(Players.Count - 1);
		if (Players.Count == 1)
		{
			Active = false;
			GameManager.instance.inputHandler.inputActions = originalActions;
			Camera.Reset();
			Hud.Restore();
		}
	}

	internal void RegisterHierarchy(PlayerSlot p)
	{
		Component[] componentsInChildren = p.Hero.GetComponentsInChildren<Component>(includeInactive: true);
		foreach (Component component in componentsInChildren)
		{
			if ((bool)component)
			{
				owners[component.GetInstanceID()] = p;
			}
		}
		(p.Hero.GetComponent<OwnerTag>() ?? p.Hero.gameObject.AddComponent<OwnerTag>()).Player = p;
		PvpCombat.Track(p.Hero.gameObject, p, reset: true);
	}

	internal void RefreshOwnership(GameObject root, PlayerSlot player)
	{
		Component[] componentsInChildren = root.GetComponentsInChildren<Component>(includeInactive: true);
		foreach (Component component in componentsInChildren)
		{
			if ((bool)component)
			{
				noOwnerThisFrame.Remove(component.GetInstanceID());
				if (player == null)
				{
					owners.Remove(component.GetInstanceID());
				}
				else
				{
					owners[component.GetInstanceID()] = player;
				}
			}
		}
		OwnerTag[] componentsInChildren2 = root.GetComponentsInChildren<OwnerTag>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].Player = player;
		}
	}

	internal PlayerSlot Resolve(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (obj is Fsm fsm)
		{
			obj = fsm.Owner;
		}
		if (obj is FsmStateAction fsmStateAction)
		{
			obj = fsmStateAction.Fsm.Owner;
		}
		GameObject gameObject = obj as GameObject;
		if ((bool)gameObject)
		{
			obj = gameObject.transform;
		}
		Component component = obj as Component;
		if (!component)
		{
			return null;
		}
		if (owners.TryGetValue(component.GetInstanceID(), out var value) && (bool)value.Hero)
		{
			return value;
		}
		if (ownerFrame != Time.frameCount)
		{
			ownerFrame = Time.frameCount;
			noOwnerThisFrame.Clear();
		}
		if (noOwnerThisFrame.Contains(component.GetInstanceID()))
		{
			return null;
		}
		OwnerTag componentInParent = component.GetComponentInParent<OwnerTag>();
		if ((bool)componentInParent && componentInParent.Player != null && !componentInParent.Player.Retiring)
		{
			owners[component.GetInstanceID()] = componentInParent.Player;
			return componentInParent.Player;
		}
		HeroController hero = component.GetComponentInParent<HeroController>();
		if ((bool)hero)
		{
			value = Players.FirstOrDefault((PlayerSlot x) => x.Hero == hero);
			if (value == null && Cloning != null && hero != Primary.Hero)
			{
				value = Cloning;
				value.Hero = hero;
			}
			if (value != null)
			{
				owners[component.GetInstanceID()] = value;
			}
			return value;
		}
		noOwnerThisFrame.Add(component.GetInstanceID());
		return null;
	}

	internal GameObject Remap(GameObject go, PlayerSlot from, PlayerSlot to)
	{
		if (!go || !from.Hero || !to.Hero)
		{
			return go;
		}
		if (go == from.Hero.gameObject)
		{
			return to.Hero.gameObject;
		}
		Transform transform = go.transform;
		if (!transform.IsChildOf(from.Hero.transform))
		{
			return go;
		}
		string text = transform.name;
		while ((bool)transform.parent && transform.parent != from.Hero.transform)
		{
			transform = transform.parent;
			text = transform.name + "/" + text;
		}
		Transform transform2 = to.Hero.transform.Find(text);
		if (!transform2)
		{
			return go;
		}
		return transform2.gameObject;
	}

	private void Spawn(PlayerSlot s)
	{
		if (!Primary.Hero || Cloning != null)
		{
			return;
		}
		s.SpawnPending = true;
		s.SpawnAsDown = s.Down;
		s.Retiring = false;
		s.Ready = false;
		s.Down = false;
		s.Hazard = false;
		s.ColliderStates.Clear();
		s.RendererStates.Clear();
		s.HasSafePoint = false;
		GameObject gameObject = Primary.Hero.gameObject;
		if (s.SpawnAsDown)
		{
			s.Vitals.Health = Math.Max(1, (s.CurrentMaxHealth + 1) / 2);
		}
		Cloning = s;
		try
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, JoinDestination(), gameObject.transform.rotation);
			s.Hero = gameObject2.GetComponent<HeroController>();
			gameObject2.name = "Knight P" + (s.Index + 1);
			PlayerLighting.Sync(Primary, s);
			if (Primary.Down)
			{
				foreach (KeyValuePair<Renderer, bool> rendererState in Primary.RendererStates)
				{
					if (!rendererState.Key)
					{
						continue;
					}
					string n = RelativePath(rendererState.Key.transform, Primary.Hero.transform);
					Transform transform = gameObject2.transform.Find(n);
					if ((bool)transform)
					{
						Renderer component = transform.GetComponent<Renderer>();
						if ((bool)component)
						{
							component.enabled = rendererState.Value;
						}
					}
				}
				foreach (KeyValuePair<Collider2D, bool> colliderState in Primary.ColliderStates)
				{
					if (!colliderState.Key)
					{
						continue;
					}
					string n2 = RelativePath(colliderState.Key.transform, Primary.Hero.transform);
					Transform transform2 = gameObject2.transform.Find(n2);
					if ((bool)transform2)
					{
						Collider2D component2 = transform2.GetComponent<Collider2D>();
						if ((bool)component2)
						{
							component2.enabled = colliderState.Value;
						}
					}
				}
			}
			UnityEngine.Object.DontDestroyOnLoad(gameObject2);
			s.Hero.playerData = Data;
			s.RootLayer = Primary.RootLayer;
			OwnerTag[] componentsInChildren = gameObject2.GetComponentsInChildren<OwnerTag>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Player = s;
			}
			RegisterHierarchy(s);
			Hooks.RebindActor(s);
			PlayerLighting.Sync(Primary, s);
			s.Colliders = gameObject2.GetComponentsInChildren<Collider2D>(includeInactive: true);
			s.Renderers = gameObject2.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (PlayerSlot player in Players)
			{
				if (player == s || !player.Hero)
				{
					continue;
				}
				Collider2D[] colliders = s.Colliders;
				foreach (Collider2D collider2D in colliders)
				{
					Collider2D[] componentsInChildren2 = player.Hero.GetComponentsInChildren<Collider2D>(includeInactive: true);
					foreach (Collider2D collider2D2 in componentsInChildren2)
					{
						if ((bool)collider2D && (bool)collider2D2)
						{
							Physics2D.IgnoreCollision(collider2D, collider2D2, ignore: true);
						}
					}
				}
			}
			AcidSwimming.PrepareCollisions(s, Data.hasAcidArmour);
			PlayMakerFSM[] componentsInChildren3 = gameObject2.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			foreach (PlayMakerFSM playMakerFSM in componentsInChildren3)
			{
				string fsmName = playMakerFSM.FsmName;
				switch (fsmName)
				{
				case "Dream Nail":
				case "Spell Control":
				case "Superdash":
				case "Nail Arts":
					continue;
				}
				if (fsmName.IndexOf("Dream Gate", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					playMakerFSM.enabled = false;
				}
			}
			plugin.StartCoroutine(FinishSpawn(s, sceneEpoch));
		}
		catch
		{
			if ((bool)s.Hero)
			{
				UnityEngine.Object.Destroy(s.Hero.gameObject);
			}
			s.Hero = null;
			s.SpawnPending = false;
			throw;
		}
		finally
		{
			if (s.SpawnAsDown)
			{
				s.Vitals.Health = 0;
			}
			Cloning = null;
		}
	}

	private void Park(PlayerSlot p)
	{
		if ((bool)p.Hero)
		{
			Revival.End(p);
			CombatEffects.Restore(p);
			p.Ready = false;
			p.SpawnPending = false;
			p.InputBlocked = true;
			if (p.Hero.gameObject.activeSelf)
			{
				p.Hero.StopAllCoroutines();
				p.Hero.gameObject.SetActive(value: false);
			}
		}
	}

	private void ResumeParked(PlayerSlot p)
	{
		p.SpawnPending = true;
		p.SpawnAsDown = p.Down;
		p.Down = false;
		p.Hazard = false;
		p.Retiring = false;
		p.ProtectionUntil = Time.time + 2f;
		if (p.SpawnAsDown)
		{
			p.Vitals.Health = Math.Max(1, (p.CurrentMaxHealth + 1) / 2);
		}
		try
		{
			p.Hero.transform.position = JoinDestination();
			using (PlayerContext.Enter(p))
			{
				p.Hero.gameObject.SetActive(value: true);
			}
			PlayerLighting.Sync(Primary, p);
			RegisterHierarchy(p);
			Hooks.RebindActor(p);
			plugin.StartCoroutine(FinishSpawn(p, sceneEpoch));
			Diagnostics.Write("REUSE P" + (p.Index + 1) + " scene=" + scene);
		}
		catch
		{
			p.SpawnPending = false;
			throw;
		}
		finally
		{
			if (p.SpawnAsDown)
			{
				p.Vitals.Health = 0;
			}
		}
	}

	[IteratorStateMachine(typeof(__iterator__FinishSpawn_d__62))]
	private IEnumerator FinishSpawn(PlayerSlot s, int epoch)
	{
		return new __iterator__FinishSpawn_d__62(0)
		{
			__iterator___4__this = this,
			s = s,
			epoch = epoch
		};
	}

	private void Retire(PlayerSlot p)
	{
		BenchSeats.Leave(p);
		Revival.End(p);
		CombatEffects.Restore(p);
		p.Retiring = true;
		p.Ready = false;
		try
		{
			SkinBridge.Release(p);
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("RETIRE SKIN", ex);
		}
		if ((bool)p.Hero)
		{
			HeroController hero = p.Hero;
			hero.StopAllCoroutines();
			hero.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(hero.gameObject);
		}
		p.Hero = null;
	}

	private static string RelativePath(Transform child, Transform root)
	{
		string text = child.name;
		while ((bool)child.parent && child.parent != root)
		{
			child = child.parent;
			text = child.name + "/" + text;
		}
		return text;
	}

	private void Suspend()
	{
		if (Players.Count == 0 || suspended)
		{
			return;
		}
		TransitionVote.Reset();
		stuckSince.Clear();
		stuckPosition.Clear();
		NativeQuickMap.Reset();
		suspended = true;
		sceneEpoch++;
		PickupCard.Reset();
		Diagnostics.Snapshot(this, "suspend-roster");
		foreach (PlayerSlot item in Players.Skip(1))
		{
			Park(item);
		}
		GameManager instance = GameManager.instance;
		if ((bool)instance && (bool)instance.inputHandler && originalActions != null)
		{
			instance.inputHandler.inputActions = originalActions;
		}
		Camera.Reset();
		Hud.Restore();
	}

	internal void PlayerFault(PlayerSlot p, string phase, Exception ex)
	{
		Diagnostics.Write("PLAYER ERROR P" + (p.Index + 1) + " phase=" + phase + " " + ex);
		Diagnostics.Snapshot(this, "player-fault");
		if (p.Index == 0)
		{
			throw ex;
		}
		p.RecoveryErrors++;
		p.Faulted = p.RecoveryErrors > 2;
		p.RetryAt = Time.unscaledTime + 2f;
		p.Down = false;
		p.Hazard = false;
		p.InputBlocked = true;
		Retire(p);
		plugin.Notice(p.Faulted ? ("P" + (p.Index + 1) + ": error repetido. F8 > Recuperar jugadores. Envia los logs.") : ("Recuperando P" + (p.Index + 1) + " sin desconectar el grupo. Error registrado."));
	}

	internal void RepairPlayers()
	{
		foreach (PlayerSlot item in Players.Skip(1))
		{
			if (item.Faulted || !item.Hero)
			{
				item.Faulted = false;
				item.RecoveryErrors = 0;
				item.RetryAt = 0f;
			}
			else if (item.Hazard)
			{
				item.HazardUntil = Time.time;
			}
		}
		plugin.Notice("Recuperacion solicitada; cierra F8 y reanuda la partida.");
		Diagnostics.Snapshot(this, "manual-repair");
	}

	internal void SceneChanged()
	{
		TransitionVote.Reset();
		DreamSequence.Reset();
		CameraFadeRecovery.Reset();
		stuckSince.Clear();
		stuckPosition.Clear();
		NativeQuickMap.Reset();
		InteractionRouter.CloseForTransition();
		BenchSeats.Reset();
		if (Players.Count == 0)
		{
			return;
		}
		sceneEpoch++;
		HasArrival = false;
		PvpMatch.SceneChanged();
		ArenaGather.Reset();
		EmergencyWarp.Reset();
		if (Players.Count > 1 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)8).ToString()))
		{
			Players[1].InputBlocked = false;
		}
		if (Players.Count > 2 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)12).ToString()))
		{
			Players[2].InputBlocked = false;
		}
		if (Players.Count > 3 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)16).ToString()))
		{
			Players[3].InputBlocked = false;
		}
		if (Players.Count > 4 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)20).ToString()))
		{
			Players[4].InputBlocked = false;
		}
		if (Players.Count > 5 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)24).ToString()))
		{
			Players[5].InputBlocked = false;
		}
		if (Players.Count > 6 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)28).ToString()))
		{
			Players[6].InputBlocked = false;
		}
		if (Players.Count > 7 && CoopEnding.observed.Contains("local8 native p1 focus fx" + ((object)32).ToString()))
		{
			Players[7].InputBlocked = false;
		}
		CoopEnding.Reset();
		ScriptedParty.Reset();
		ChallengeSequence.Reset();
		PickupCard.Reset();
		PlayerLighting.Reset();
		CoopShades.Reset();
		noOwnerThisFrame.Clear();
		Hooks.ClearSceneCache();
		Charms.Close();
		Charms.ReleaseNative();
		ShopMenuRouting.Reset();
		StagMenuRouting.Reset();
		InteractionRouter.Reset();
		Lifeblood.Reset();
		WorldRouting.Reset();
		restedBench = false;
		benchWasActive = false;
		roomBenches = new RestBench[0];
		nextBenchScan = 0f;
		foreach (PlayerSlot item in Players.Skip(1))
		{
			Park(item);
			if (!item.Hero)
			{
				item.Hero = null;
				item.Ready = false;
				item.SpawnPending = false;
			}
		}
		owners.Clear();
		if ((bool)Primary.Hero)
		{
			RegisterHierarchy(Primary);
		}
		foreach (PlayerSlot player in Players)
		{
			if (player.Index == 0 && (player.Hazard || player.Down))
			{
				KeyValuePair<Collider2D, bool>[] array = player.ColliderStates.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<Collider2D, bool> keyValuePair = array[i];
					if ((bool)keyValuePair.Key)
					{
						keyValuePair.Key.enabled = keyValuePair.Value;
					}
				}
				KeyValuePair<Renderer, bool>[] array2 = player.RendererStates.ToArray();
				for (int i = 0; i < array2.Length; i++)
				{
					KeyValuePair<Renderer, bool> keyValuePair2 = array2[i];
					if ((bool)keyValuePair2.Key)
					{
						keyValuePair2.Key.enabled = keyValuePair2.Value;
					}
				}
				if (!TeamWipe)
				{
					ActorRecovery.EnableBody(player);
				}
			}
			bool down = player.Down;
			player.HasSafePoint = false;
			player.HasPreviousSafePoint = false;
			player.Hazard = false;
			player.Down = down && !TeamWipe;
			player.FarSince = -1f;
			player.Faulted = false;
			player.RecoveryErrors = 0;
			player.RetryAt = 0f;
			player.SpawnPending = false;
			player.NextSafeCheck = 0f;
			player.Vitals.AtBench = false;
			player.WakeUntil = 0f;
			if (player.Index > 0 && (bool)player.Hero)
			{
				KeyValuePair<Collider2D, bool>[] array = player.ColliderStates.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<Collider2D, bool> keyValuePair3 = array[i];
					if ((bool)keyValuePair3.Key)
					{
						keyValuePair3.Key.enabled = keyValuePair3.Value;
					}
				}
				KeyValuePair<Renderer, bool>[] array2 = player.RendererStates.ToArray();
				for (int i = 0; i < array2.Length; i++)
				{
					KeyValuePair<Renderer, bool> keyValuePair4 = array2[i];
					if ((bool)keyValuePair4.Key)
					{
						keyValuePair4.Key.enabled = keyValuePair4.Value;
					}
				}
			}
			player.ColliderStates.Clear();
			player.RendererStates.Clear();
			player.Vitals.HazardPoint = Vector3.zero;
			if (player.Index > 0)
			{
				if (TeamWipe)
				{
					player.Vitals.Health = player.CurrentMaxHealth;
				}
				else if (player.Vitals.Health <= 0)
				{
					player.Down = true;
					player.Vitals.Health = 0;
				}
				player.Vitals.DisablePause = false;
				player.Vitals.Invincible = false;
			}
		}
		scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		readyAt = Time.unscaledTime + 0.5f;
		if (Data != null)
		{
			Data.atBench = false;
		}
		TeamWipe = false;
		Camera.Reset();
		Hud.Restore();
		Diagnostics.Snapshot(this, "scene-roster-retained");
	}

	internal void PrepareTransition(PlayerSlot entrant)
	{
		Charms.ReleaseNative();
		PlayerSlot primary = Primary;
		if (primary == null || !primary.Hero)
		{
			return;
		}
		if (entrant != null && (bool)entrant.Hero && entrant != primary)
		{
			primary.Hero.transform.position = entrant.Hero.transform.position;
		}
		if (!primary.Down && !primary.Hazard)
		{
			return;
		}
		if (primary.Down && !TeamWipe)
		{
			transitionProxy = true;
			primary.Vitals.Health = 1;
			Data.health = 1;
			Diagnostics.Write("TRANSITION proxy P1 entrant=" + ((entrant == null) ? "unknown" : ("P" + (entrant.Index + 1))));
		}
		using (PlayerContext.Enter(primary))
		{
			ActorRecovery.Reset(primary);
			foreach (KeyValuePair<Collider2D, bool> colliderState in primary.ColliderStates)
			{
				if ((bool)colliderState.Key)
				{
					colliderState.Key.enabled = colliderState.Value;
				}
			}
			ActorRecovery.EnableBody(primary);
		}
	}

	internal void Entered(HeroController hero)
	{
		if (Primary != null && !(hero != Primary.Hero))
		{
			Arrival = hero.transform.position;
			HasArrival = true;
			if (transitionProxy)
			{
				RestoreTransitionProxy();
			}
			Vector3 arrival = Arrival;
			Diagnostics.Write("ARRIVAL anchor=" + arrival.ToString() + " downed_P1=" + Primary.Down);
		}
	}

	private void RestoreTransitionProxy()
	{
		transitionProxy = false;
		if (Primary != null && Primary.Down)
		{
			Primary.Vitals.Health = 0;
			if (Data != null)
			{
				Data.health = 0;
			}
			Hide(Primary);
			Diagnostics.Write("TRANSITION proxy restored P1 down");
		}
	}

	internal void Down(PlayerSlot p, bool spawnShade = true)
	{
		CrystalDashCoop.StopPlayer(p);
		if (p.Down || TeamWipe)
		{
			BenchPoseRecovery.Downed(p);
			ArenaVisualRecovery.Downed(p);
			return;
		}
		EmergencyWarp.Cancel(p);
		BenchSeats.Leave(p);
		Revival.End(p);
		p.Down = true;
		p.DownAt = Time.time;
		p.Vitals.Health = 0;
		p.Vitals.Blue = 0;
		Commit(p);
		Hide(p);
		if (spawnShade)
		{
			PvpCombat.Down(p);
		}
		if (spawnShade && !PvpMatch.Running)
		{
			CoopShades.Spawn(this, p);
		}
		plugin.Notice(PvpMatch.Running ? ("P" + (p.Index + 1) + " eliminado de la ronda.") : ("P" + (p.Index + 1) + " caido. Reanimacion por Focus o banco."));
		Diagnostics.Write("DOWN P" + (p.Index + 1));
		BenchPoseRecovery.Downed(p);
		ArenaVisualRecovery.Downed(p);
	}

	internal void Hazard(PlayerSlot p)
	{
		//Discarded unreachable code: IL_0070
		CrystalDashCoop.StopPlayer(p);
		if (!p.Down && !p.Hazard)
		{
			p.LastHazardPosition = p.Hero.transform.position;
			EmergencyWarp.Cancel(p);
			BenchSeats.Leave(p);
			Revival.End(p);
			p.Hazard = true;
			p.HazardUntil = Time.time + ((!DreamSequence.InStoryDream) ? 0.7f : 0.08f);
			Diagnostics.Write("HAZARD P");
		}
	}

	private void Hide(PlayerSlot p)
	{
		if (!p.Hero)
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			p.Hero.StopAllCoroutines();
			Reflect.Call(p.Hero, "CancelAttack");
			p.Hero.RelinquishControl();
			p.Hero.StopAnimationControl();
			p.Hero.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			p.Hero.AffectedByGravity(gravityApplies: false);
			ActorRecovery.Freeze(p);
		}
		Collider2D[] componentsInChildren = p.Hero.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Collider2D collider2D in componentsInChildren)
		{
			if (!p.ColliderStates.ContainsKey(collider2D))
			{
				p.ColliderStates[collider2D] = collider2D.enabled;
			}
			collider2D.enabled = false;
		}
		Renderer[] componentsInChildren2 = p.Hero.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren2)
		{
			if (!p.RendererStates.ContainsKey(renderer))
			{
				p.RendererStates[renderer] = renderer.enabled;
			}
			renderer.enabled = false;
		}
	}

	internal void Recover(PlayerSlot p, bool respawn, Vector3? reviveAt = null)
	{
		//Discarded unreachable code: IL_011e
		if (!RadianceAscentCheckpoint.BeforeRecovery(this, p, ref reviveAt))
		{
			return;
		}
		if (!p.Hero)
		{
			p.Hazard = false;
			p.RetryAt = 0f;
			return;
		}
		EmergencyWarp.Cancel(p);
		Vector3 position = p.Hero.transform.position;
		PlayerSlot playerSlot = LongestLiving(p);
		bool down = p.Down;
		Vector3 point = Vector3.zero;
		bool flag = !respawn && GameCheckpoint(p, out point);
		PlayerSlot anchor = ArenaGather.Anchor;
		Vector3 result;
		if (reviveAt.HasValue && IsSafe(reviveAt.Value, p))
		{
			result = reviveAt.Value;
		}
		else if (respawn && playerSlot != null)
		{
			if (!NearAlly(playerSlot, p, out result))
			{
				p.HazardUntil = Time.time + 0.3f;
				return;
			}
		}
		else if (anchor == null || anchor == p || !NearAlly(anchor, p, out result))
		{
			if (!respawn && DreamSequence.InStoryDream && p.HasSafePoint)
			{
				result = p.SafePoint;
			}
			else if ((respawn || !p.HasPreviousSafePoint || !AwayFromHazard(p.PreviousSafePoint, p) || !FindSafePosition(p.PreviousSafePoint, p, out result, p.LastHazardPosition)) && (respawn || !p.HasSafePoint || !FindSafePosition(p.SafePoint, p, out result, p.LastHazardPosition)))
			{
				if (flag && AwayFromHazard(point, p))
				{
					result = point;
				}
				else if (playerSlot == null || !NearAlly(playerSlot, p, out result))
				{
					if (p.HasSafePoint && AwayFromHazard(p.SafePoint, p) && IsSafe(p.SafePoint, p))
					{
						result = p.SafePoint;
					}
					else if (p.Vitals.HazardPoint != Vector3.zero && AwayFromHazard(p.Vitals.HazardPoint, p) && IsSafe(p.Vitals.HazardPoint, p))
					{
						result = p.Vitals.HazardPoint;
					}
					else if ((Primary == null || Primary == p || !Primary.Alive || !FindSafePosition(Primary.Hero.transform.position, p, out result)) && !FindSafePosition(HasArrival ? Arrival : position, p, out result, p.LastHazardPosition))
					{
						Diagnostics.Throttled("RECOVER waiting for safe floor P" + (p.Index + 1), new InvalidOperationException("No valid destination"));
						p.HazardUntil = Time.time + 0.5f;
						return;
					}
				}
			}
		}
		if (!RadianceAscentCheckpoint.Allows(ArenaGather.Allows(result), this, p, result) && !PvpMatch.Running)
		{
			if (!ArenaGather.TryDestination(this, p, out var destination))
			{
				p.HazardUntil = Time.time + 0.5f;
				return;
			}
			result = destination;
		}
		if (!SpawnSafety.Recovery(this, p, ref result))
		{
			return;
		}
		p.ProtectionUntil = Time.time + 2f;
		p.Vitals.Invincible = false;
		p.Vitals.DisablePause = false;
		if (respawn)
		{
			p.Vitals.Health = Mathf.Max(1, (p.CurrentMaxHealth + 1) / 2);
			p.Vitals.Blue = ((p.Vitals.Joni > 0) ? Math.Max(1, p.Vitals.Joni / 2) : 0);
			p.Vitals.Soul = 0;
			p.Vitals.Reserve = 0;
		}
		Commit(p);
		Vector3 position2 = p.Hero.transform.position;
		NativeDreamFx.TeleportTrail(p, position2, result);
		using (PlayerContext.Enter(p))
		{
			p.Hero.transform.position = result;
			p.Hero.gameObject.layer = p.RootLayer;
			ActorRecovery.Reset(p);
			p.Hero.AffectedByGravity(gravityApplies: true);
			p.Hero.AcceptInput();
			p.Hero.SetDamageMode(DamageMode.FULL_DAMAGE);
			Rigidbody2D component = p.Hero.GetComponent<Rigidbody2D>();
			if ((bool)component)
			{
				component.isKinematic = false;
				component.simulated = true;
				component.velocity = Vector2.zero;
			}
			p.Hero.cState.hazardRespawning = false;
			p.Hero.cState.hazardDeath = false;
			p.Hero.cState.dead = false;
			p.Hero.cState.invulnerable = true;
			Reflect.Set(p.Hero, "tilemapTestActive", false);
			Reflect.Set(p.Hero, "enteringVertically", false);
			Reflect.Set(p.Hero, "airDashed", false);
			Reflect.Set(p.Hero, "doubleJumped", false);
			Reflect.Set(p.Hero, "transitionState", HeroTransitionState.WAITING_TO_TRANSITION);
			HeroAnimationController component2 = p.Hero.GetComponent<HeroAnimationController>();
			if ((bool)component2)
			{
				Reflect.Set(component2, "waitingToEnter", false);
			}
			KeyValuePair<Collider2D, bool>[] array = p.ColliderStates.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<Collider2D, bool> keyValuePair = array[i];
				if ((bool)keyValuePair.Key)
				{
					keyValuePair.Key.enabled = keyValuePair.Value;
				}
			}
			KeyValuePair<Renderer, bool>[] array2 = p.RendererStates.ToArray();
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<Renderer, bool> keyValuePair2 = array2[i];
				if ((bool)keyValuePair2.Key)
				{
					keyValuePair2.Key.enabled = keyValuePair2.Value;
				}
			}
			RestoreLivingVisuals(p);
			PlayMakerFSM proxyFSM = p.Hero.proxyFSM;
			if ((bool)proxyFSM)
			{
				proxyFSM.SendEvent("HeroCtrl-HeroInPosition");
			}
		}
		p.Down = false;
		p.Hazard = false;
		p.ColliderStates.Clear();
		p.RendererStates.Clear();
		p.WakeUntil = ((respawn && restedBench && Vector2.Distance(result, benchPosition) < 3f) ? (Time.unscaledTime + 0.7f) : 0f);
		if (down)
		{
			p.LifeStartedAt = Time.unscaledTime;
		}
		PvpCombat.Recovered(p);
		p.LastInputFrame = -1;
		p.ProtectionUntil = Time.time + 2f;
		p.SafePoint = result;
		p.HasSafePoint = IsSafe(result, p);
		p.SafeAt = Time.unscaledTime;
		string[] obj = new string[10]
		{
			"RECOVER P",
			(p.Index + 1).ToString(),
			" respawn=",
			respawn.ToString(),
			" source=",
			(respawn && playerSlot != null) ? ("oldest-P" + (playerSlot.Index + 1)) : (flag ? "game-checkpoint" : "safe-floor"),
			" pos=",
			null,
			null,
			null
		};
		Vector3 vector = result;
		obj[7] = vector.ToString();
		obj[8] = " checkpoint=";
		vector = p.Vitals.HazardPoint;
		obj[9] = vector.ToString();
		Diagnostics.Write(string.Concat(obj));
	}

	internal static void RestoreLivingVisuals(PlayerSlot p)
	{
		HeroController hero = p.Hero;
		if (!hero)
		{
			return;
		}
		ActorRecovery.EnableBody(p);
		GameObject heroDeathPrefab = hero.heroDeathPrefab;
		if ((bool)heroDeathPrefab && heroDeathPrefab != hero.gameObject && heroDeathPrefab.transform.IsChildOf(hero.transform))
		{
			heroDeathPrefab.SetActive(value: false);
		}
		tk2dSprite component = hero.GetComponent<tk2dSprite>();
		Renderer component2 = hero.GetComponent<Renderer>();
		if ((bool)component2)
		{
			component2.enabled = true;
		}
		if ((bool)component)
		{
			component.enabled = true;
			Color color = component.color;
			color.a = 1f;
			component.color = color;
		}
		tk2dSpriteAnimator component3 = hero.GetComponent<tk2dSpriteAnimator>();
		if ((bool)component3)
		{
			component3.enabled = true;
			component3.Resume();
			component3.Stop();
			if (component3.GetClipByName("Idle") != null)
			{
				component3.Play("Idle");
			}
		}
		HeroAnimationController component4 = hero.GetComponent<HeroAnimationController>();
		if ((bool)component4)
		{
			component4.enabled = true;
			component4.StartControl();
		}
		ParticleSystem[] componentsInChildren = hero.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		foreach (ParticleSystem particleSystem in componentsInChildren)
		{
			string text = particleSystem.name.ToLowerInvariant();
			if (text.Contains("death") || text.Contains("shade") || text.Contains("void"))
			{
				particleSystem.Stop();
				particleSystem.Clear();
			}
		}
		HeroBox.inactive = false;
		SkinBridge.Apply(p);
		p.NextSkinUpdate = 0f;
		Diagnostics.Write("REVIVE VISUAL P" + (p.Index + 1) + " body=" + ((bool)component2 && component2.enabled) + " death=" + ((bool)heroDeathPrefab && heroDeathPrefab.activeInHierarchy) + " clip=" + (((bool)component3 && component3.CurrentClip != null) ? component3.CurrentClip.name : "none"));
	}

	private bool GameCheckpoint(PlayerSlot p, out Vector3 point)
	{
		point = Vector3.zero;
		Vector3 hazardPoint = p.Vitals.HazardPoint;
		if (hazardPoint == Vector3.zero || !p.Hero)
		{
			return false;
		}
		GameManager instance = GameManager.instance;
		if (!instance || hazardPoint.x < 0f || hazardPoint.y < 0f || hazardPoint.x > instance.cameraCtrl.sceneWidth || hazardPoint.y > instance.cameraCtrl.sceneHeight)
		{
			return false;
		}
		if (!Physics2D.Raycast(hazardPoint, Vector2.down, 20f, 256).collider)
		{
			return false;
		}
		point = p.Hero.FindGroundPoint(hazardPoint, useExtended: true);
		return IsSafe(point, p);
	}

	private void Wipe()
	{
		TeamWipe = true;
		CoopShades.Reset();
		plugin.Notice("Equipo derrotado. Regresando al punto de reaparicion del juego.");
		foreach (PlayerSlot item in Players.Skip(1))
		{
			if ((bool)item.Hero)
			{
				Retire(item);
			}
		}
		PlayerSlot primary = Primary;
		foreach (KeyValuePair<Collider2D, bool> colliderState in primary.ColliderStates)
		{
			if ((bool)colliderState.Key)
			{
				colliderState.Key.enabled = colliderState.Value;
			}
		}
		foreach (KeyValuePair<Renderer, bool> rendererState in primary.RendererStates)
		{
			if ((bool)rendererState.Key)
			{
				rendererState.Key.enabled = rendererState.Value;
			}
		}
		primary.Down = false;
		primary.Hazard = false;
		ActorRecovery.Physics(primary);
		primary.Hero.cState.dead = false;
		primary.Hero.cState.hazardDeath = false;
		primary.Vitals.Health = 0;
		primary.Vitals.DisablePause = false;
		primary.Vitals.Invincible = false;
		primary.Vitals.Write(Data);
		Data.disablePause = false;
		AllowVanillaDeath = true;
		try
		{
			using (PlayerContext.Enter(primary))
			{
				primary.Hero.StartCoroutine((IEnumerator)Reflect.Call(primary.Hero, "Die"));
			}
		}
		finally
		{
			AllowVanillaDeath = false;
		}
	}

	internal PlayerSlot Nearest(Vector3 point)
	{
		PlayerSlot playerSlot = null;
		float num = float.MaxValue;
		foreach (PlayerSlot player in Players)
		{
			if (player.Ready && player.Alive && !EmergencyWarp.Active(player))
			{
				float sqrMagnitude = (player.Hero.transform.position - point).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					playerSlot = player;
				}
			}
		}
		return playerSlot ?? Primary;
	}

	private void SaveSafePoint(PlayerSlot p)
	{
		if (!p.Hero.cState.onGround || p.Hero.cState.recoiling || p.Hero.cState.transitioning)
		{
			RadianceAscentCheckpoint.Record(this, p);
			return;
		}
		Vector3 position = p.Hero.transform.position;
		if (IsSafe(position, p))
		{
			if (p.HasSafePoint && Vector2.Distance(position, p.SafePoint) > 1f)
			{
				p.PreviousSafePoint = p.SafePoint;
				p.HasPreviousSafePoint = true;
			}
			if (!p.HasSafePoint || Vector2.Distance(position, p.SafePoint) > 4f)
			{
				string text = (p.Index + 1).ToString();
				Vector3 vector = position;
				Diagnostics.Write("SAFEPOINT P" + text + " " + vector.ToString());
			}
			p.SafePoint = position;
			p.HasSafePoint = true;
			p.SafeAt = Time.unscaledTime;
		}
		RadianceAscentCheckpoint.Record(this, p);
	}

	internal bool IsSafe(Vector3 pos, PlayerSlot p)
	{
		if (p == null || !p.Hero || !InRoom(pos))
		{
			return PvpArena.PlacementSafe(normal: false, pos, p);
		}
		Bounds bounds = DuelGround.Body(p);
		string reason;
		return PvpArena.PlacementSafe(DuelGround.Safe(p, pos, bounds.center - p.Hero.transform.position, bounds.size, out reason), pos, p);
	}

	private static bool AwayFromHazard(Vector3 at, PlayerSlot p)
	{
		return Vector2.Distance(at, p.LastHazardPosition) > 3.5f;
	}

	internal bool InRoom(Vector3 at)
	{
		GameManager instance = GameManager.instance;
		if ((bool)instance)
		{
			return RecoveryRules.InRoom(at.x, at.y, instance.sceneWidth, instance.sceneHeight);
		}
		return false;
	}

	internal bool FindSafePosition(Vector3 origin, PlayerSlot p, out Vector3 result, Vector3? avoid = null)
	{
		result = Vector3.zero;
		if (!p.Hero)
		{
			return false;
		}
		if ((!avoid.HasValue || Vector2.Distance(origin, avoid.Value) > 3.5f) && IsSafe(origin, p))
		{
			result = origin;
			return true;
		}
		GameManager instance = GameManager.instance;
		if (!instance || !instance.cameraCtrl)
		{
			return false;
		}
		origin.x = Mathf.Clamp(origin.x, 1f, Mathf.Max(1f, instance.cameraCtrl.sceneWidth - 1f));
		origin.y = Mathf.Clamp(origin.y, 1f, Mathf.Max(1f, instance.cameraCtrl.sceneHeight - 1f));
		Bounds bounds = DuelGround.Body(p);
		float num = bounds.extents.y - (bounds.center.y - p.Hero.transform.position.y);
		for (int i = 0; i <= 24; i++)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				float x = origin.x + (float)(i * j);
				RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(x, Mathf.Min(instance.cameraCtrl.sceneHeight - 0.2f, origin.y + 3f)), Vector2.down, 20f, 256);
				if ((bool)raycastHit2D.collider && !raycastHit2D.collider.isTrigger && !(raycastHit2D.normal.y < 0.6f))
				{
					Vector3 vector = new Vector3(x, raycastHit2D.point.y + num + 0.06f, origin.z);
					if ((!avoid.HasValue || Vector2.Distance(vector, avoid.Value) > 3.5f) && IsSafe(vector, p))
					{
						result = vector;
						return true;
					}
				}
			}
		}
		return false;
	}

	internal PlayerSlot LongestLiving(PlayerSlot except)
	{
		double[] array = new double[Players.Count];
		bool[] array2 = new bool[Players.Count];
		for (int i = 0; i < Players.Count; i++)
		{
			PlayerSlot playerSlot = Players[i];
			array[i] = playerSlot.LifeStartedAt;
			array2[i] = playerSlot != except && playerSlot.Ready && playerSlot.Alive && playerSlot.Connected && !EmergencyWarp.Active(playerSlot) && !playerSlot.Hero.cState.transitioning && ArenaGather.Allows(playerSlot.Hero.transform.position);
		}
		int num = PartyRules.Oldest(array, array2);
		if (num >= 0)
		{
			return Players[num];
		}
		return null;
	}

	internal bool NearAlly(PlayerSlot ally, PlayerSlot arriving, out Vector3 result)
	{
		return SpawnSafety.NearAlly(this, ally, arriving, out result);
	}

	internal Vector3 SafeDestination(PlayerSlot anchor)
	{
		if (anchor.HasSafePoint && IsSafe(anchor.SafePoint, anchor))
		{
			return anchor.SafePoint;
		}
		return anchor.Hero.transform.position;
	}

	private Vector3 JoinDestination()
	{
		if (Primary == null || !Primary.Hero)
		{
			return Vector3.zero;
		}
		PlayerSlot anchor = ArenaGather.Anchor;
		if (anchor != null && anchor != Primary && IsSafe(anchor.Hero.transform.position, anchor))
		{
			return anchor.Hero.transform.position;
		}
		if (Primary.Down)
		{
			PlayerSlot playerSlot = Players.FirstOrDefault((PlayerSlot p) => p != Primary && p.Alive && p.Ready && IsSafe(p.Hero.transform.position, p));
			if (playerSlot != null)
			{
				return playerSlot.Hero.transform.position;
			}
		}
		Vector3 vector = ((HasArrival && !Primary.Alive) ? Arrival : Primary.Hero.transform.position);
		if (IsSafe(vector, Primary))
		{
			return vector;
		}
		if (Primary.HasSafePoint && Vector2.Distance(vector, Primary.SafePoint) < 8f && IsSafe(Primary.SafePoint, Primary))
		{
			return Primary.SafePoint;
		}
		GameManager instance = GameManager.instance;
		if ((bool)instance && (bool)instance.cameraCtrl)
		{
			Vector3 vector2 = new Vector3(Mathf.Clamp(vector.x, 1.5f, Mathf.Max(1.5f, instance.cameraCtrl.sceneWidth - 1.5f)), Mathf.Clamp(vector.y, 1.5f, Mathf.Max(1.5f, instance.cameraCtrl.sceneHeight - 1.5f)), vector.z);
			if (IsSafe(vector2, Primary))
			{
				return vector2;
			}
			if ((bool)Physics2D.Raycast(new Vector2(vector2.x, vector2.y + 2f), Vector2.down, 12f, 256).collider)
			{
				Vector3 vector3 = Primary.Hero.FindGroundPoint(new Vector2(vector2.x, vector2.y + 2f), useExtended: true);
				if (IsSafe(vector3, Primary))
				{
					return vector3;
				}
			}
			return vector2;
		}
		return vector;
	}

	internal void GatherNow()
	{
		PlayerSlot playerSlot = Players.FirstOrDefault((PlayerSlot p) => p.Alive && p.HasSafePoint);
		if (playerSlot == null)
		{
			plugin.Notice("Espera a estar sobre suelo seguro.");
			return;
		}
		foreach (PlayerSlot player in Players)
		{
			if (player != playerSlot && player.Alive && IsSafe(playerSlot.SafePoint, player))
			{
				if (FlowerRules.BlocksModTeleport(player))
				{
					FlowerRules.WarnTeleportBlocked(player);
					continue;
				}
				Vector3 position = player.Hero.transform.position;
				NativeDreamFx.TeleportTrail(player, position, playerSlot.SafePoint);
				player.Hero.transform.position = playerSlot.SafePoint;
				player.Hero.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			}
		}
	}

	internal void BenchRest(HeroController actor)
	{
		if (!Gameplay)
		{
			return;
		}
		PlayerSlot playerSlot = Resolve(actor) ?? Primary;
		if (playerSlot == null || !ConfirmedBench(playerSlot) || (!playerSlot.Vitals.AtBench && !Data.atBench))
		{
			return;
		}
		if (PvpMatch.Running)
		{
			PvpMatch.Stop(restorePosition: false);
		}
		benchPosition = playerSlot.Hero.transform.position;
		restedBench = true;
		foreach (PlayerSlot player in Players)
		{
			Revival.End(player);
			if (!player.Hero)
			{
				player.Down = false;
				player.Vitals.Health = player.CurrentMaxHealth;
				player.RetryAt = 0f;
				continue;
			}
			if (player.Down || player.Hazard)
			{
				player.SafePoint = playerSlot.Hero.transform.position;
				player.HasSafePoint = true;
				Recover(player, respawn: true);
			}
			using (PlayerContext.Enter(player))
			{
				Data.MaxHealth();
				Data.health = Data.CurrentMaxHealth;
				Data.joniHealthBlue = player.Vitals.Joni;
				if (player.Index > 0 && player.Vitals.Joni > 0)
				{
					Data.healthBlue = Math.Max(Data.healthBlue, player.Vitals.Joni);
				}
				Data.atBench = player == playerSlot || BenchSeats.Seated(player);
			}
			player.ReviveProgress = 0f;
			player.HealFlashUntil = Time.unscaledTime + 0.6f;
		}
		Charms.Save(this);
		BenchSave.Rest(this, actor);
		Diagnostics.Write("BENCH REST by P" + (playerSlot.Index + 1));
	}

	private bool ConfirmedBench(PlayerSlot p)
	{
		if (BenchSeats.Seated(p))
		{
			return true;
		}
		if (Time.unscaledTime >= nextBenchScan)
		{
			roomBenches = UnityEngine.Object.FindObjectsOfType<RestBench>();
			nextBenchScan = Time.unscaledTime + 1f;
		}
		RestBench[] array = roomBenches;
		foreach (RestBench restBench in array)
		{
			if ((bool)restBench && restBench.gameObject.activeInHierarchy && Mathf.Abs(restBench.transform.position.x - p.Hero.transform.position.x) < 3f && Mathf.Abs(restBench.transform.position.y - p.Hero.transform.position.y) < 1.8f)
			{
				return true;
			}
		}
		return false;
	}

	private void TickRevive(PlayerSlot[] live)
	{
		Revival.Tick(this, live);
	}

	internal bool NearRestedBench(PlayerSlot p)
	{
		if (restedBench && (bool)p.Hero)
		{
			return Vector2.Distance(p.Hero.transform.position, benchPosition) < 7f;
		}
		return false;
	}

	internal void Commit(PlayerSlot p)
	{
		if (PlayerContext.Current == p || (PlayerContext.Current == null && p == Primary))
		{
			p.Apply(Data);
		}
	}

	private Color ColorFor(int index)
	{
		if (index >= 0 && index < Local8Mod.Settings.Colors.Length && ColorUtility.TryParseHtmlString("#" + Local8Mod.Settings.Colors[index], out var color))
		{
			return color;
		}
		return Colors[Mathf.Clamp(index, 0, Colors.Length - 1)];
	}

	public void Dispose()
	{
		PvpCharms.End();
		RoleSystem.Reset();
		SummonRouting.Reset();
		DreamSequence.Reset();
		CameraFadeRecovery.Reset();
		stuckSince.Clear();
		stuckPosition.Clear();
		BenchSeats.Reset();
		PvpMatch.Stop(restorePosition: false);
		PvpCombat.Reset(scores: true);
		ArenaGather.Reset();
		EmergencyWarp.Reset();
		CoopEnding.Reset();
		ScriptedParty.Reset();
		ChallengeSequence.Reset();
		PickupCard.Reset();
		PlayerLighting.Reset();
		CoopShades.Reset();
		noOwnerThisFrame.Clear();
		Hooks.ClearSceneCache();
		Charms.Save(this);
		Charms.Reset();
		ShopMenuRouting.Reset();
		StagMenuRouting.Reset();
		InteractionRouter.Reset();
		Lifeblood.Reset();
		CombatEffects.Reset();
		if (Primary != null && (bool)Primary.Hero)
		{
			tk2dSprite component = Primary.Hero.GetComponent<tk2dSprite>();
			if ((bool)component)
			{
				component.color = Color.white;
			}
		}
		SkinBridge.Release(Primary);
		WorldRouting.Reset();
		PlayerSlot[] array = Players.Skip(1).ToArray();
		foreach (PlayerSlot p in array)
		{
			Retire(p);
		}
		GameManager instance = GameManager.instance;
		if ((bool)instance && (bool)instance.inputHandler && originalActions != null)
		{
			instance.inputHandler.inputActions = originalActions;
		}
		foreach (PlayerSlot player in Players)
		{
			Controls.Dispose(player);
		}
		Players.Clear();
		owners.Clear();
		Active = false;
		TeamWipe = false;
		suspended = false;
		Camera.Reset();
		Hud.Restore();
		CrystalDashTransit.Reset();
		ShadeCloakRitual.Reset();
		RadianceAscentCheckpoint.Reset();
		NativeDashEffects.Clear();
		CrystalDashCoop.Reset();
		DreamRescueFeedback.Reset();
		ArenaVisualRecovery.Reset();
		ArenaReturnRecovery.Reset();
		InteractionMotion.Reset();
		MenuInputRecovery.Reset();
		PvpGeoHud.Reset();
		FlowerAura.Reset();
	}
}
