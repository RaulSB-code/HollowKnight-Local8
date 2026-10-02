using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using HutongGames.PlayMaker;
using InControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal sealed class CoopSession : IDisposable
{
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

	private static readonly Collider2D[] safetyHits = (Collider2D[])(object)new Collider2D[32];

	private bool suspended;

	private Vector3 benchPosition;

	private bool restedBench;

	private bool benchWasActive;

	private float bindingCheck;

	private string bindingSignature;

	private RestBench[] roomBenches = (RestBench[])(object)new RestBench[0];

	private float nextBenchScan;

	private readonly Dictionary<PlayerSlot, float> stuckSince = new Dictionary<PlayerSlot, float>();

	private readonly Dictionary<PlayerSlot, Vector3> stuckPosition = new Dictionary<PlayerSlot, Vector3>();

	internal static readonly Color[] Colors = (Color[])(object)new Color[8]
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
			if (Active && !TeamWipe && Primary != null && Primary.Down && Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene())
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
			//IL_002d: Invalid comparison between Unknown and I4
			GameManager instance = GameManager.instance;
			if (Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene)
			{
				return (int)instance.gameState == 4;
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
			//IL_0034: Invalid comparison between Unknown and I4
			//IL_0045: Invalid comparison between Unknown and I4
			GameManager instance = GameManager.instance;
			if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.hero_ctrl))
			{
				return CoopRules.CanQueueJoin(instance.IsGameplayScene(), instance.IsLoadingSceneTransition, instance.HasFinishedEnteringScene, (int)instance.gameState == 4, instance.isPaused || (int)instance.gameState == 5);
			}
			return false;
		}
	}

	internal CoopSession(Local8Runtime p)
	{
		plugin = p;
		Scene activeScene = SceneManager.GetActiveScene();
		scene = ((Scene)(ref activeScene)).name;
	}

	internal void Tick()
	{
		RescueHint.Tick(this);
		GameManager gm = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)gm))
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
		else if (!Object.op_Implicit((Object)(object)gm.hero_ctrl) || !gm.IsGameplayScene() || gm.IsLoadingSceneTransition)
		{
			Suspend();
		}
		else
		{
			if (PlayerContext.Current != null)
			{
				return;
			}
			if (Primary != null && (Object)(object)Primary.Hero != (Object)(object)gm.hero_ctrl)
			{
				if (Players.Skip(1).Any((PlayerSlot playerSlot3) => (Object)(object)playerSlot3.Hero == (Object)(object)gm.hero_ctrl))
				{
					return;
				}
				SkinBridge.Release(Primary);
				Primary.Hero = gm.hero_ctrl;
				Primary.RootLayer = ((Component)gm.hero_ctrl).gameObject.layer;
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
				InputDevice val = Controls.Find(plugin.PrimaryDevice.Value);
				Controls.InitPrimary(playerSlot, originalActions, val ?? InputDevice.Null);
				Players.Add(playerSlot);
				RegisterHierarchy(playerSlot);
				Hooks.PatchActor(((Component)playerSlot.Hero).gameObject);
				playerSlot.RootLayer = ((Component)playerSlot.Hero).gameObject.layer;
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
			bool flag = Gameplay && Players.Any((PlayerSlot playerSlot3) => Object.op_Implicit((Object)(object)playerSlot3.Hero) && playerSlot3.Vitals.AtBench && ConfirmedBench(playerSlot3));
			if (flag && !benchWasActive)
			{
				PlayerSlot playerSlot2 = Players.First((PlayerSlot playerSlot3) => Object.op_Implicit((Object)(object)playerSlot3.Hero) && playerSlot3.Vitals.AtBench && ConfirmedBench(playerSlot3));
				BenchRest(playerSlot2.Hero);
			}
			benchWasActive = flag;
			foreach (PlayerSlot player2 in Players)
			{
				if (player2.Device == null || (player2.Device != InputDevice.Null && !player2.Device.IsAttached))
				{
					InputDevice val2 = Controls.Find(player2.DeviceKey);
					if (val2 != null)
					{
						Controls.Bind(player2, val2);
					}
				}
			}
			foreach (InputDevice d in Controls.Devices())
			{
				if (!plugin.Panel && Controls.JoinPressed(d) && !Players.Any((PlayerSlot playerSlot3) => playerSlot3.Device == d) && CanJoin)
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
				if (!Object.op_Implicit((Object)(object)item.Hero) && item.Ready)
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
			if (Object.op_Implicit((Object)(object)Primary.Hero) && Gameplay)
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
					string[] obj = new string[12]
					{
						"PERF players=",
						Players.Count.ToString(),
						" fps_avg=",
						Mathf.RoundToInt((float)performanceFrames / num).ToString(),
						" worst_ms=",
						Mathf.RoundToInt(worstFrame * 1000f).ToString(),
						" frames_over_50ms=",
						slowFrames.ToString(),
						" gc0=",
						(GC.CollectionCount(0) - gcCollections).ToString(),
						" scene=",
						null
					};
					Scene activeScene = SceneManager.GetActiveScene();
					obj[11] = ((Scene)(ref activeScene)).name;
					Diagnostics.Write(string.Concat(obj));
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
			foreach (PlayerSlot p in array)
			{
				try
				{
					if (p.Index > 0 && !p.Ready && !p.SpawnPending && !p.Faulted && Time.unscaledTime >= Mathf.Max(Mathf.Max(readyAt, p.RetryAt), p.JoinDelayAt) && Object.op_Implicit((Object)(object)Primary.Hero) && !Primary.Hazard && !Primary.Hero.cState.transitioning)
					{
						if (Object.op_Implicit((Object)(object)p.Hero))
						{
							ResumeParked(p);
						}
						else
						{
							Spawn(p);
						}
					}
					if (!Object.op_Implicit((Object)(object)p.Hero) || !p.Ready)
					{
						continue;
					}
					CheckStuckControl(p);
					if (ScriptedParty.Holds(p) || EmergencyWarp.Active(p) || CoopEnding.HoldsActor(p) || BenchSeats.Custom(p))
					{
						continue;
					}
					if (p.Index > 0 && Time.unscaledTime >= p.NextCharmUpdate)
					{
						p.NextCharmUpdate = Time.unscaledTime + 0.5f;
						Charms.UpdateMaximum(p, Data, heal: false);
					}
					if (p.Hazard && Time.time >= p.HazardUntil)
					{
						Recover(p, respawn: false);
					}
					if (p.Down && !PvpMatch.Running && plugin.TimedRespawn.Value && Time.time - p.DownAt >= plugin.RespawnSeconds.Value && Players.Any((PlayerSlot q) => q != p && q.Alive))
					{
						Recover(p, respawn: true);
					}
					if (!p.Alive)
					{
						if ((p.Down || p.Hazard) && !EntryProxy)
						{
							ActorRecovery.Freeze(p);
						}
						continue;
					}
					if (p.Index > 0)
					{
						SoulReserve.Tick(p);
					}
					if (p.Index > 0)
					{
						AcidSwimming.Tick(p, Data.hasAcidArmour);
					}
					if (p.Vitals.AtBench && restedBench && Vector2.Distance(Vector2.op_Implicit(((Component)p.Hero).transform.position), Vector2.op_Implicit(benchPosition)) > 5f)
					{
						p.Vitals.AtBench = false;
					}
					if (Time.unscaledTime >= p.NextSafeCheck)
					{
						p.NextSafeCheck = Time.unscaledTime + 0.3f;
						SaveSafePoint(p);
					}
					if (p.Vitals.Health <= 0)
					{
						Down(p);
						continue;
					}
					if (Time.time < p.ProtectionUntil)
					{
						p.Hero.cState.invulnerable = true;
					}
					else if (p.ProtectionUntil > 0f)
					{
						p.ProtectionUntil = 0f;
						p.Hero.cState.invulnerable = false;
					}
					if (Time.unscaledTime - p.LastDamageAt > 4f && p.ProtectionUntil <= 0f && !p.Vitals.Invincible && !p.Hero.cState.shadowDashing && !p.Hero.cState.transitioning)
					{
						InvulnerablePulse component = ((Component)p.Hero).GetComponent<InvulnerablePulse>();
						if (Object.op_Implicit((Object)(object)component) && Reflect.Get(component, "pulsing", fallback: false))
						{
							component.stopInvulnerablePulse();
							p.Hero.cState.invulnerable = false;
							p.Hero.cState.recoiling = false;
							Diagnostics.Write("DAMAGE pulse reset P" + (p.Index + 1));
						}
					}
				}
				catch (Exception ex)
				{
					if (p.Index == 0)
					{
						throw;
					}
					PlayerFault(p, "tick/recover", ex);
				}
			}
			AcidSwimming.GuardPrimary();
			AcidSwimming.GuardPrimaryPosition();
			SummonRouting.Tick(this);
			if (PvpMatch.Tick(this))
			{
				return;
			}
			PlayerSlot[] array2 = Players.Where((PlayerSlot playerSlot3) => playerSlot3.Alive).ToArray();
			if (array2.Length == 0 && Players.All((PlayerSlot playerSlot3) => playerSlot3.Down))
			{
				Wipe();
				return;
			}
			if (!CoopEnding.Active)
			{
				TickRevive(array2.Where((PlayerSlot p2) => !EmergencyWarp.Active(p2)).ToArray());
			}
			EmergencyWarp.Tick(this, array2);
			NativeDreamFx.ObserveTeleports(this);
		}
	}

	internal void VisualTick()
	{
		//IL_01c9: Invalid comparison between Unknown and I4
		if (suspended)
		{
			return;
		}
		foreach (PlayerSlot player in Players)
		{
			if (!Object.op_Implicit((Object)(object)player.Hero))
			{
				continue;
			}
			if ((player.Down || player.Hazard) && (player != Primary || !EntryProxy))
			{
				ActorRecovery.Freeze(player);
				foreach (Renderer key in player.RendererStates.Keys)
				{
					if (Object.op_Implicit((Object)(object)key))
					{
						key.enabled = false;
					}
				}
			}
			Vector3 position = ((Component)player.Hero).transform.position;
			position.z = (float)CoopRules.PlayerDepth(player.Index);
			((Component)player.Hero).transform.position = position;
			if (player.Index > 0 && player.Ready)
			{
				PlayerLighting.Sync(Primary, player);
			}
			if (player.Alive && player.Ready && !TransitionVote.Holding(player) && !ScriptedParty.Active && !EmergencyWarp.Active(player) && !CoopEnding.HoldsActor(player))
			{
				Renderer component = ((Component)player.Hero).GetComponent<Renderer>();
				tk2dSprite component2 = ((Component)player.Hero).GetComponent<tk2dSprite>();
				if (((Object.op_Implicit((Object)(object)component) && !component.enabled) || (Object.op_Implicit((Object)(object)component2) && ((tk2dBaseSprite)component2).color.a < 0.02f)) && !player.Hero.cState.transitioning && (player.Index > 0 || (Gameplay && Object.op_Implicit((Object)(object)UIManager.instance) && (int)UIManager.instance.uiState == 4 && !player.Hero.IsDreamReturning && !Reflect.Get(player.Hero, "<IsEnteringDream>k__BackingField", fallback: false) && !BenchSeats.Seated(player))) && !CoopEnding.Active)
				{
					if (player.InvisibleSince < 0f)
					{
						player.InvisibleSince = Time.unscaledTime;
					}
					else if (Time.unscaledTime - player.InvisibleSince > ((player.Index == 0) ? 2f : 1f))
					{
						if (player.Index == 0 && player.Hero.controlReqlinquished)
						{
							if (player.Actions == null || (!(Mathf.Abs(((TwoAxisInputControl)player.Actions.moveVector).X) > 0.25f) && !(Mathf.Abs(((TwoAxisInputControl)player.Actions.moveVector).Y) > 0.25f)) || Time.unscaledTime - player.InvisibleSince < 4f)
							{
								continue;
							}
							using (PlayerContext.Enter(player))
							{
								ActorRecovery.Reset(player);
							}
							Diagnostics.Write("SCRIPT primary recovered after invisible control lock");
						}
						if (Object.op_Implicit((Object)(object)component))
						{
							component.enabled = true;
						}
						if (Object.op_Implicit((Object)(object)component2))
						{
							Color color = ((tk2dBaseSprite)component2).color;
							color.a = 1f;
							((tk2dBaseSprite)component2).color = color;
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
				tk2dSprite component3 = ((Component)player.Hero).GetComponent<tk2dSprite>();
				if (Object.op_Implicit((Object)(object)component3) && !player.Down && !player.Hazard)
				{
					((tk2dBaseSprite)component3).color = Color.Lerp(Color.white, player.Color, 0.32f);
				}
			}
		}
		ArenaGather.VisualTick(this);
		CombatEffects.UpdateVolumes(this);
		EmergencyWarp.VisualTick();
		CoopEnding.VisualTick(this);
		BenchSeats.VisualTick();
		TransitionVote.HideWaiting();
	}

	private unsafe void CheckStuckControl(PlayerSlot p)
	{
		//IL_00c2: Invalid comparison between Unknown and I4
		UIManager instance = UIManager.instance;
		if (!Gameplay || !p.Alive || !p.Connected || p.Actions == null || p.InputBlocked || p.Hero.cState.transitioning || p.Hero.IsDreamReturning || DreamSequence.InStoryDream || ScriptedParty.Active || CoopEnding.Active || PvpMatch.Running || EmergencyWarp.Active(p) || TransitionVote.Holding(p) || BenchSeats.Seated(p) || PickupCard.Owner != null || InteractionRouter.ActivePlayer != null || Charms.NativeMenuOpen || ShopMenuRouting.MenuVisible || StagMenuRouting.HasOwner || !Object.op_Implicit((Object)(object)instance) || (int)instance.uiState != 4 || Mathf.Abs(((TwoAxisInputControl)p.Actions.moveVector).X) < 0.35f)
		{
			stuckSince.Remove(p);
			stuckPosition.Remove(p);
			return;
		}
		Rigidbody2D component = ((Component)p.Hero).GetComponent<Rigidbody2D>();
		HeroAnimationController component2 = ((Component)p.Hero).GetComponent<HeroAnimationController>();
		if (!p.Hero.controlReqlinquished && p.Hero.acceptingInput && (!Object.op_Implicit((Object)(object)component2) || component2.controlEnabled) && Object.op_Implicit((Object)(object)component) && component.simulated && !component.isKinematic && (p.Hero.cState.onGround || p.Hero.cState.swimming || p.AcidAssistActive || !(component.gravityScale <= 0.01f)))
		{
			stuckSince.Remove(p);
			stuckPosition.Remove(p);
			return;
		}
		Vector3 position = ((Component)p.Hero).transform.position;
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
			Vector3 val = position;
			Diagnostics.Write("CONTROL recovered P" + text + " after sustained movement input at=" + ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString());
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
		playerSlot.Vitals.HazardPoint = ((Component)Primary.Hero).transform.position;
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
		InputDevice val = Controls.Devices().FirstOrDefault((Func<InputDevice, bool>)((InputDevice d) => Players.All((PlayerSlot p) => p.Device != d)));
		if (val == null && Players.All((PlayerSlot p) => !Controls.IsKeyboard(p.Device)))
		{
			val = InputDevice.Null;
		}
		if (val == null)
		{
			plugin.Notice("No hay dispositivos libres. Usa Asignar P1 en Teclado para liberar el primer mando.");
		}
		else
		{
			Join(val);
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
		Component[] componentsInChildren = ((Component)p.Hero).GetComponentsInChildren<Component>(true);
		foreach (Component val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				owners[((Object)val).GetInstanceID()] = p;
			}
		}
		(((Component)p.Hero).GetComponent<OwnerTag>() ?? ((Component)p.Hero).gameObject.AddComponent<OwnerTag>()).Player = p;
		PvpCombat.Track(((Component)p.Hero).gameObject, p, reset: true);
	}

	internal void RefreshOwnership(GameObject root, PlayerSlot player)
	{
		Component[] componentsInChildren = root.GetComponentsInChildren<Component>(true);
		foreach (Component val in componentsInChildren)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				noOwnerThisFrame.Remove(((Object)val).GetInstanceID());
				if (player == null)
				{
					owners.Remove(((Object)val).GetInstanceID());
				}
				else
				{
					owners[((Object)val).GetInstanceID()] = player;
				}
			}
		}
		OwnerTag[] componentsInChildren2 = root.GetComponentsInChildren<OwnerTag>(true);
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
		Fsm val = (Fsm)((obj is Fsm) ? obj : null);
		if (val != null)
		{
			obj = val.Owner;
		}
		FsmStateAction val2 = (FsmStateAction)((obj is FsmStateAction) ? obj : null);
		if (val2 != null)
		{
			obj = val2.Fsm.Owner;
		}
		GameObject val3 = (GameObject)((obj is GameObject) ? obj : null);
		if (Object.op_Implicit((Object)(object)val3))
		{
			obj = val3.transform;
		}
		Component val4 = (Component)((obj is Component) ? obj : null);
		if (!Object.op_Implicit((Object)(object)val4))
		{
			return null;
		}
		if (owners.TryGetValue(((Object)val4).GetInstanceID(), out var value) && Object.op_Implicit((Object)(object)value.Hero))
		{
			return value;
		}
		if (ownerFrame != Time.frameCount)
		{
			ownerFrame = Time.frameCount;
			noOwnerThisFrame.Clear();
		}
		if (noOwnerThisFrame.Contains(((Object)val4).GetInstanceID()))
		{
			return null;
		}
		OwnerTag componentInParent = val4.GetComponentInParent<OwnerTag>();
		if (Object.op_Implicit((Object)(object)componentInParent) && componentInParent.Player != null && !componentInParent.Player.Retiring)
		{
			owners[((Object)val4).GetInstanceID()] = componentInParent.Player;
			return componentInParent.Player;
		}
		HeroController hero = val4.GetComponentInParent<HeroController>();
		if (Object.op_Implicit((Object)(object)hero))
		{
			value = Players.FirstOrDefault((PlayerSlot x) => (Object)(object)x.Hero == (Object)(object)hero);
			if (value == null && Cloning != null && (Object)(object)hero != (Object)(object)Primary.Hero)
			{
				value = Cloning;
				value.Hero = hero;
			}
			if (value != null)
			{
				owners[((Object)val4).GetInstanceID()] = value;
			}
			return value;
		}
		noOwnerThisFrame.Add(((Object)val4).GetInstanceID());
		return null;
	}

	internal GameObject Remap(GameObject go, PlayerSlot from, PlayerSlot to)
	{
		if (!Object.op_Implicit((Object)(object)go) || !Object.op_Implicit((Object)(object)from.Hero) || !Object.op_Implicit((Object)(object)to.Hero))
		{
			return go;
		}
		if ((Object)(object)go == (Object)(object)((Component)from.Hero).gameObject)
		{
			return ((Component)to.Hero).gameObject;
		}
		Transform val = go.transform;
		if (!val.IsChildOf(((Component)from.Hero).transform))
		{
			return go;
		}
		string text = ((Object)val).name;
		while (Object.op_Implicit((Object)(object)val.parent) && (Object)(object)val.parent != (Object)(object)((Component)from.Hero).transform)
		{
			val = val.parent;
			text = ((Object)val).name + "/" + text;
		}
		Transform val2 = ((Component)to.Hero).transform.Find(text);
		if (!Object.op_Implicit((Object)(object)val2))
		{
			return go;
		}
		return ((Component)val2).gameObject;
	}

	private void Spawn(PlayerSlot s)
	{
		if (!Object.op_Implicit((Object)(object)Primary.Hero) || Cloning != null)
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
		GameObject gameObject = ((Component)Primary.Hero).gameObject;
		if (s.SpawnAsDown)
		{
			s.Vitals.Health = Math.Max(1, (s.CurrentMaxHealth + 1) / 2);
		}
		Cloning = s;
		try
		{
			GameObject val = Object.Instantiate<GameObject>(gameObject, JoinDestination(), gameObject.transform.rotation);
			s.Hero = val.GetComponent<HeroController>();
			((Object)val).name = "Knight P" + (s.Index + 1);
			PlayerLighting.Sync(Primary, s);
			if (Primary.Down)
			{
				foreach (KeyValuePair<Renderer, bool> rendererState in Primary.RendererStates)
				{
					if (!Object.op_Implicit((Object)(object)rendererState.Key))
					{
						continue;
					}
					string text = RelativePath(((Component)rendererState.Key).transform, ((Component)Primary.Hero).transform);
					Transform val2 = val.transform.Find(text);
					if (Object.op_Implicit((Object)(object)val2))
					{
						Renderer component = ((Component)val2).GetComponent<Renderer>();
						if (Object.op_Implicit((Object)(object)component))
						{
							component.enabled = rendererState.Value;
						}
					}
				}
				foreach (KeyValuePair<Collider2D, bool> colliderState in Primary.ColliderStates)
				{
					if (!Object.op_Implicit((Object)(object)colliderState.Key))
					{
						continue;
					}
					string text2 = RelativePath(((Component)colliderState.Key).transform, ((Component)Primary.Hero).transform);
					Transform val3 = val.transform.Find(text2);
					if (Object.op_Implicit((Object)(object)val3))
					{
						Collider2D component2 = ((Component)val3).GetComponent<Collider2D>();
						if (Object.op_Implicit((Object)(object)component2))
						{
							((Behaviour)component2).enabled = colliderState.Value;
						}
					}
				}
			}
			Object.DontDestroyOnLoad((Object)(object)val);
			s.Hero.playerData = Data;
			s.RootLayer = Primary.RootLayer;
			OwnerTag[] componentsInChildren = val.GetComponentsInChildren<OwnerTag>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Player = s;
			}
			RegisterHierarchy(s);
			Hooks.RebindActor(s);
			PlayerLighting.Sync(Primary, s);
			s.Colliders = val.GetComponentsInChildren<Collider2D>(true);
			s.Renderers = val.GetComponentsInChildren<Renderer>(true);
			foreach (PlayerSlot player in Players)
			{
				if (player == s || !Object.op_Implicit((Object)(object)player.Hero))
				{
					continue;
				}
				Collider2D[] colliders = s.Colliders;
				foreach (Collider2D val4 in colliders)
				{
					Collider2D[] componentsInChildren2 = ((Component)player.Hero).GetComponentsInChildren<Collider2D>(true);
					foreach (Collider2D val5 in componentsInChildren2)
					{
						if (Object.op_Implicit((Object)(object)val4) && Object.op_Implicit((Object)(object)val5))
						{
							Physics2D.IgnoreCollision(val4, val5, true);
						}
					}
				}
			}
			AcidSwimming.PrepareCollisions(s, Data.hasAcidArmour);
			PlayMakerFSM[] componentsInChildren3 = val.GetComponentsInChildren<PlayMakerFSM>(true);
			foreach (PlayMakerFSM val6 in componentsInChildren3)
			{
				string fsmName = val6.FsmName;
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
					((Behaviour)val6).enabled = false;
				}
			}
			((MonoBehaviour)plugin).StartCoroutine(FinishSpawn(s, sceneEpoch));
		}
		catch
		{
			if (Object.op_Implicit((Object)(object)s.Hero))
			{
				Object.Destroy((Object)(object)((Component)s.Hero).gameObject);
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
		if (Object.op_Implicit((Object)(object)p.Hero))
		{
			Revival.End(p);
			CombatEffects.Restore(p);
			p.Ready = false;
			p.SpawnPending = false;
			p.InputBlocked = true;
			if (((Component)p.Hero).gameObject.activeSelf)
			{
				((MonoBehaviour)p.Hero).StopAllCoroutines();
				((Component)p.Hero).gameObject.SetActive(false);
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
			((Component)p.Hero).transform.position = JoinDestination();
			using (PlayerContext.Enter(p))
			{
				((Component)p.Hero).gameObject.SetActive(true);
			}
			PlayerLighting.Sync(Primary, p);
			RegisterHierarchy(p);
			Hooks.RebindActor(p);
			((MonoBehaviour)plugin).StartCoroutine(FinishSpawn(p, sceneEpoch));
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

	private IEnumerator FinishSpawn(PlayerSlot s, int epoch)
	{
		yield return null;
		yield return null;
		if (epoch != sceneEpoch)
		{
			yield break;
		}
		if (!Object.op_Implicit((Object)(object)s.Hero) || s.Retiring)
		{
			s.SpawnPending = false;
			yield break;
		}
		try
		{
			Hooks.RebindActor(s);
			using (PlayerContext.Enter(s))
			{
				int health = Data.health;
				int healthBlue = Data.healthBlue;
				s.Hero.CharmUpdate();
				Data.health = Math.Min(Math.Max(1, health), Data.CurrentMaxHealth);
				Data.healthBlue = healthBlue;
				ActorRecovery.Reset(s);
				s.Hero.AcceptInput();
				s.Hero.AffectedByGravity(true);
				s.Hero.SetDamageMode((DamageMode)0);
				Reflect.Set(s.Hero, "isGameplayScene", true);
				Reflect.Set(s.Hero, "transitionState", (object)(HeroTransitionState)0);
				Reflect.Set(s.Hero, "tilemapTestActive", false);
				HeroAnimationController component = ((Component)s.Hero).GetComponent<HeroAnimationController>();
				if (Object.op_Implicit((Object)(object)component))
				{
					Reflect.Set(component, "waitingToEnter", false);
				}
				((Component)s.Hero).GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			}
			PlayerLighting.Sync(Primary, s);
			s.Ready = true;
			s.ProtectionUntil = Time.time + 2f;
			((Component)s.Hero).transform.position = JoinDestination();
			if (float.IsPositiveInfinity(s.JoinVisualUntil))
			{
				s.JoinVisualUntil = (plugin.Panel ? 0f : (Time.unscaledTime + 0.65f));
			}
			SummonRouting.Refresh(s);
			RestoreLivingVisuals(s);
			if (s.Vitals.HazardPoint == Vector3.zero && Primary != null)
			{
				s.Vitals.HazardPoint = Primary.Vitals.HazardPoint;
			}
			s.SafePoint = ((Component)s.Hero).transform.position;
			s.HasSafePoint = IsSafe(s.SafePoint, s);
			if (s.SpawnAsDown)
			{
				s.SpawnAsDown = false;
				Down(s, spawnShade: false);
			}
			Diagnostics.Snapshot(this, "spawn-ready-P" + (s.Index + 1));
		}
		catch (Exception ex)
		{
			PlayerFault(s, "finish-spawn", ex);
		}
		finally
		{
			s.SpawnPending = false;
		}
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
		if (Object.op_Implicit((Object)(object)p.Hero))
		{
			HeroController hero = p.Hero;
			((MonoBehaviour)hero).StopAllCoroutines();
			((Component)hero).gameObject.SetActive(false);
			Object.Destroy((Object)(object)((Component)hero).gameObject);
		}
		p.Hero = null;
	}

	private static string RelativePath(Transform child, Transform root)
	{
		string text = ((Object)child).name;
		while (Object.op_Implicit((Object)(object)child.parent) && (Object)(object)child.parent != (Object)(object)root)
		{
			child = child.parent;
			text = ((Object)child).name + "/" + text;
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
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.inputHandler) && originalActions != null)
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
			if (item.Faulted || !Object.op_Implicit((Object)(object)item.Hero))
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
		roomBenches = (RestBench[])(object)new RestBench[0];
		nextBenchScan = 0f;
		foreach (PlayerSlot item in Players.Skip(1))
		{
			Park(item);
			if (!Object.op_Implicit((Object)(object)item.Hero))
			{
				item.Hero = null;
				item.Ready = false;
				item.SpawnPending = false;
			}
		}
		owners.Clear();
		if (Object.op_Implicit((Object)(object)Primary.Hero))
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
					if (Object.op_Implicit((Object)(object)keyValuePair.Key))
					{
						((Behaviour)keyValuePair.Key).enabled = keyValuePair.Value;
					}
				}
				KeyValuePair<Renderer, bool>[] array2 = player.RendererStates.ToArray();
				for (int i = 0; i < array2.Length; i++)
				{
					KeyValuePair<Renderer, bool> keyValuePair2 = array2[i];
					if (Object.op_Implicit((Object)(object)keyValuePair2.Key))
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
			if (player.Index > 0 && Object.op_Implicit((Object)(object)player.Hero))
			{
				KeyValuePair<Collider2D, bool>[] array = player.ColliderStates.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					KeyValuePair<Collider2D, bool> keyValuePair3 = array[i];
					if (Object.op_Implicit((Object)(object)keyValuePair3.Key))
					{
						((Behaviour)keyValuePair3.Key).enabled = keyValuePair3.Value;
					}
				}
				KeyValuePair<Renderer, bool>[] array2 = player.RendererStates.ToArray();
				for (int i = 0; i < array2.Length; i++)
				{
					KeyValuePair<Renderer, bool> keyValuePair4 = array2[i];
					if (Object.op_Implicit((Object)(object)keyValuePair4.Key))
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
		Scene activeScene = SceneManager.GetActiveScene();
		scene = ((Scene)(ref activeScene)).name;
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
		if (primary == null || !Object.op_Implicit((Object)(object)primary.Hero))
		{
			return;
		}
		if (entrant != null && Object.op_Implicit((Object)(object)entrant.Hero) && entrant != primary)
		{
			((Component)primary.Hero).transform.position = ((Component)entrant.Hero).transform.position;
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
				if (Object.op_Implicit((Object)(object)colliderState.Key))
				{
					((Behaviour)colliderState.Key).enabled = colliderState.Value;
				}
			}
			ActorRecovery.EnableBody(primary);
		}
	}

	internal unsafe void Entered(HeroController hero)
	{
		if (Primary != null && !((Object)(object)hero != (Object)(object)Primary.Hero))
		{
			Arrival = ((Component)hero).transform.position;
			HasArrival = true;
			if (transitionProxy)
			{
				RestoreTransitionProxy();
			}
			Vector3 arrival = Arrival;
			Diagnostics.Write("ARRIVAL anchor=" + ((object)(*(Vector3*)(&arrival))/*cast due to .constrained prefix*/).ToString() + " downed_P1=" + Primary.Down);
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
		if (!p.Down && !TeamWipe)
		{
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
		}
	}

	internal void Hazard(PlayerSlot p)
	{
		if (!p.Down && !p.Hazard)
		{
			p.LastHazardPosition = ((Component)p.Hero).transform.position;
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
		if (!Object.op_Implicit((Object)(object)p.Hero))
		{
			return;
		}
		using (PlayerContext.Enter(p))
		{
			((MonoBehaviour)p.Hero).StopAllCoroutines();
			Reflect.Call(p.Hero, "CancelAttack");
			p.Hero.RelinquishControl();
			p.Hero.StopAnimationControl();
			((Component)p.Hero).GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			p.Hero.AffectedByGravity(false);
			ActorRecovery.Freeze(p);
		}
		Collider2D[] componentsInChildren = ((Component)p.Hero).GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D val in componentsInChildren)
		{
			if (!p.ColliderStates.ContainsKey(val))
			{
				p.ColliderStates[val] = ((Behaviour)val).enabled;
			}
			((Behaviour)val).enabled = false;
		}
		Renderer[] componentsInChildren2 = ((Component)p.Hero).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val2 in componentsInChildren2)
		{
			if (!p.RendererStates.ContainsKey(val2))
			{
				p.RendererStates[val2] = val2.enabled;
			}
			val2.enabled = false;
		}
	}

	internal unsafe void Recover(PlayerSlot p, bool respawn, Vector3? reviveAt = null)
	{
		if (!Object.op_Implicit((Object)(object)p.Hero))
		{
			p.Hazard = false;
			p.RetryAt = 0f;
			return;
		}
		EmergencyWarp.Cancel(p);
		Vector3 position = ((Component)p.Hero).transform.position;
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
					else if ((Primary == null || Primary == p || !Primary.Alive || !FindSafePosition(((Component)Primary.Hero).transform.position, p, out result)) && !FindSafePosition(HasArrival ? Arrival : position, p, out result, p.LastHazardPosition))
					{
						Diagnostics.Throttled("RECOVER waiting for safe floor P" + (p.Index + 1), new InvalidOperationException("No valid destination"));
						p.HazardUntil = Time.time + 0.5f;
						return;
					}
				}
			}
		}
		if (!ArenaGather.Allows(result) && !PvpMatch.Running)
		{
			if (!ArenaGather.TryDestination(this, p, out var destination))
			{
				p.HazardUntil = Time.time + 0.5f;
				return;
			}
			result = destination;
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
		Vector3 position2 = ((Component)p.Hero).transform.position;
		NativeDreamFx.TeleportTrail(p, position2, result);
		using (PlayerContext.Enter(p))
		{
			((Component)p.Hero).transform.position = result;
			((Component)p.Hero).gameObject.layer = p.RootLayer;
			ActorRecovery.Reset(p);
			p.Hero.AffectedByGravity(true);
			p.Hero.AcceptInput();
			p.Hero.SetDamageMode((DamageMode)0);
			Rigidbody2D component = ((Component)p.Hero).GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component))
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
			Reflect.Set(p.Hero, "transitionState", (object)(HeroTransitionState)0);
			HeroAnimationController component2 = ((Component)p.Hero).GetComponent<HeroAnimationController>();
			if (Object.op_Implicit((Object)(object)component2))
			{
				Reflect.Set(component2, "waitingToEnter", false);
			}
			KeyValuePair<Collider2D, bool>[] array = p.ColliderStates.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<Collider2D, bool> keyValuePair = array[i];
				if (Object.op_Implicit((Object)(object)keyValuePair.Key))
				{
					((Behaviour)keyValuePair.Key).enabled = keyValuePair.Value;
				}
			}
			KeyValuePair<Renderer, bool>[] array2 = p.RendererStates.ToArray();
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<Renderer, bool> keyValuePair2 = array2[i];
				if (Object.op_Implicit((Object)(object)keyValuePair2.Key))
				{
					keyValuePair2.Key.enabled = keyValuePair2.Value;
				}
			}
			RestoreLivingVisuals(p);
			PlayMakerFSM proxyFSM = p.Hero.proxyFSM;
			if (Object.op_Implicit((Object)(object)proxyFSM))
			{
				proxyFSM.SendEvent("HeroCtrl-HeroInPosition");
			}
		}
		p.Down = false;
		p.Hazard = false;
		p.ColliderStates.Clear();
		p.RendererStates.Clear();
		p.WakeUntil = ((respawn && restedBench && Vector2.Distance(Vector2.op_Implicit(result), Vector2.op_Implicit(benchPosition)) < 3f) ? (Time.unscaledTime + 0.7f) : 0f);
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
		Vector3 val = result;
		obj[7] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
		obj[8] = " checkpoint=";
		val = p.Vitals.HazardPoint;
		obj[9] = ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString();
		Diagnostics.Write(string.Concat(obj));
	}

	internal static void RestoreLivingVisuals(PlayerSlot p)
	{
		HeroController hero = p.Hero;
		if (!Object.op_Implicit((Object)(object)hero))
		{
			return;
		}
		ActorRecovery.EnableBody(p);
		GameObject heroDeathPrefab = hero.heroDeathPrefab;
		if (Object.op_Implicit((Object)(object)heroDeathPrefab) && (Object)(object)heroDeathPrefab != (Object)(object)((Component)hero).gameObject && heroDeathPrefab.transform.IsChildOf(((Component)hero).transform))
		{
			heroDeathPrefab.SetActive(false);
		}
		tk2dSprite component = ((Component)hero).GetComponent<tk2dSprite>();
		Renderer component2 = ((Component)hero).GetComponent<Renderer>();
		if (Object.op_Implicit((Object)(object)component2))
		{
			component2.enabled = true;
		}
		if (Object.op_Implicit((Object)(object)component))
		{
			((Behaviour)component).enabled = true;
			Color color = ((tk2dBaseSprite)component).color;
			color.a = 1f;
			((tk2dBaseSprite)component).color = color;
		}
		tk2dSpriteAnimator component3 = ((Component)hero).GetComponent<tk2dSpriteAnimator>();
		if (Object.op_Implicit((Object)(object)component3))
		{
			((Behaviour)component3).enabled = true;
			component3.Resume();
			component3.Stop();
			if (component3.GetClipByName("Idle") != null)
			{
				component3.Play("Idle");
			}
		}
		HeroAnimationController component4 = ((Component)hero).GetComponent<HeroAnimationController>();
		if (Object.op_Implicit((Object)(object)component4))
		{
			((Behaviour)component4).enabled = true;
			component4.StartControl();
		}
		ParticleSystem[] componentsInChildren = ((Component)hero).GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem val in componentsInChildren)
		{
			string text = ((Object)val).name.ToLowerInvariant();
			if (text.Contains("death") || text.Contains("shade") || text.Contains("void"))
			{
				val.Stop();
				val.Clear();
			}
		}
		HeroBox.inactive = false;
		SkinBridge.Apply(p);
		p.NextSkinUpdate = 0f;
		Diagnostics.Write("REVIVE VISUAL P" + (p.Index + 1) + " body=" + (Object.op_Implicit((Object)(object)component2) && component2.enabled) + " death=" + (Object.op_Implicit((Object)(object)heroDeathPrefab) && heroDeathPrefab.activeInHierarchy) + " clip=" + ((Object.op_Implicit((Object)(object)component3) && component3.CurrentClip != null) ? component3.CurrentClip.name : "none"));
	}

	private bool GameCheckpoint(PlayerSlot p, out Vector3 point)
	{
		point = Vector3.zero;
		Vector3 hazardPoint = p.Vitals.HazardPoint;
		if (hazardPoint == Vector3.zero || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return false;
		}
		GameManager instance = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || hazardPoint.x < 0f || hazardPoint.y < 0f || hazardPoint.x > instance.cameraCtrl.sceneWidth || hazardPoint.y > instance.cameraCtrl.sceneHeight)
		{
			return false;
		}
		RaycastHit2D val = Physics2D.Raycast(Vector2.op_Implicit(hazardPoint), Vector2.down, 20f, 256);
		if (!Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val)).collider))
		{
			return false;
		}
		point = p.Hero.FindGroundPoint(Vector2.op_Implicit(hazardPoint), true);
		return IsSafe(point, p);
	}

	private void Wipe()
	{
		TeamWipe = true;
		CoopShades.Reset();
		plugin.Notice("Equipo derrotado. Regresando al punto de reaparicion del juego.");
		foreach (PlayerSlot item in Players.Skip(1))
		{
			if (Object.op_Implicit((Object)(object)item.Hero))
			{
				Retire(item);
			}
		}
		PlayerSlot primary = Primary;
		foreach (KeyValuePair<Collider2D, bool> colliderState in primary.ColliderStates)
		{
			if (Object.op_Implicit((Object)(object)colliderState.Key))
			{
				((Behaviour)colliderState.Key).enabled = colliderState.Value;
			}
		}
		foreach (KeyValuePair<Renderer, bool> rendererState in primary.RendererStates)
		{
			if (Object.op_Implicit((Object)(object)rendererState.Key))
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
				((MonoBehaviour)primary.Hero).StartCoroutine((IEnumerator)Reflect.Call(primary.Hero, "Die"));
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
				Vector3 val = ((Component)player.Hero).transform.position - point;
				float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					playerSlot = player;
				}
			}
		}
		return playerSlot ?? Primary;
	}

	private unsafe void SaveSafePoint(PlayerSlot p)
	{
		if (!p.Hero.cState.onGround || p.Hero.cState.recoiling || p.Hero.cState.transitioning)
		{
			return;
		}
		Vector3 position = ((Component)p.Hero).transform.position;
		if (IsSafe(position, p))
		{
			if (p.HasSafePoint && Vector2.Distance(Vector2.op_Implicit(position), Vector2.op_Implicit(p.SafePoint)) > 1f)
			{
				p.PreviousSafePoint = p.SafePoint;
				p.HasPreviousSafePoint = true;
			}
			if (!p.HasSafePoint || Vector2.Distance(Vector2.op_Implicit(position), Vector2.op_Implicit(p.SafePoint)) > 4f)
			{
				string text = (p.Index + 1).ToString();
				Vector3 val = position;
				Diagnostics.Write("SAFEPOINT P" + text + " " + ((object)(*(Vector3*)(&val))/*cast due to .constrained prefix*/).ToString());
			}
			p.SafePoint = position;
			p.HasSafePoint = true;
			p.SafeAt = Time.unscaledTime;
		}
	}

	internal bool IsSafe(Vector3 pos, PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || !InRoom(pos))
		{
			return false;
		}
		Bounds val = DuelGround.Body(p);
		string reason;
		return DuelGround.Safe(p, pos, ((Bounds)(ref val)).center - ((Component)p.Hero).transform.position, ((Bounds)(ref val)).size, out reason);
	}

	private static bool AwayFromHazard(Vector3 at, PlayerSlot p)
	{
		return Vector2.Distance(Vector2.op_Implicit(at), Vector2.op_Implicit(p.LastHazardPosition)) > 3.5f;
	}

	internal bool InRoom(Vector3 at)
	{
		GameManager instance = GameManager.instance;
		if (Object.op_Implicit((Object)(object)instance))
		{
			return RecoveryRules.InRoom(at.x, at.y, instance.sceneWidth, instance.sceneHeight);
		}
		return false;
	}

	internal bool FindSafePosition(Vector3 origin, PlayerSlot p, out Vector3 result, Vector3? avoid = null)
	{
		result = Vector3.zero;
		if (!Object.op_Implicit((Object)(object)p.Hero))
		{
			return false;
		}
		if ((!avoid.HasValue || Vector2.Distance(Vector2.op_Implicit(origin), Vector2.op_Implicit(avoid.Value)) > 3.5f) && IsSafe(origin, p))
		{
			result = origin;
			return true;
		}
		GameManager instance = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.cameraCtrl))
		{
			return false;
		}
		origin.x = Mathf.Clamp(origin.x, 1f, Mathf.Max(1f, instance.cameraCtrl.sceneWidth - 1f));
		origin.y = Mathf.Clamp(origin.y, 1f, Mathf.Max(1f, instance.cameraCtrl.sceneHeight - 1f));
		Bounds val = DuelGround.Body(p);
		float num = ((Bounds)(ref val)).extents.y - (((Bounds)(ref val)).center.y - ((Component)p.Hero).transform.position.y);
		Vector3 val3 = default(Vector3);
		for (int i = 0; i <= 24; i++)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				float num2 = origin.x + (float)(i * j);
				RaycastHit2D val2 = Physics2D.Raycast(new Vector2(num2, Mathf.Min(instance.cameraCtrl.sceneHeight - 0.2f, origin.y + 3f)), Vector2.down, 20f, 256);
				if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val2)).collider) && !((RaycastHit2D)(ref val2)).collider.isTrigger && !(((RaycastHit2D)(ref val2)).normal.y < 0.6f))
				{
					((Vector3)(ref val3))._002Ector(num2, ((RaycastHit2D)(ref val2)).point.y + num + 0.06f, origin.z);
					if ((!avoid.HasValue || Vector2.Distance(Vector2.op_Implicit(val3), Vector2.op_Implicit(avoid.Value)) > 3.5f) && IsSafe(val3, p))
					{
						result = val3;
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
			array2[i] = playerSlot != except && playerSlot.Ready && playerSlot.Alive && playerSlot.Connected && !EmergencyWarp.Active(playerSlot) && !playerSlot.Hero.cState.transitioning && ArenaGather.Allows(((Component)playerSlot.Hero).transform.position);
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
		result = Vector3.zero;
		if (ally == null || !Object.op_Implicit((Object)(object)ally.Hero) || !Object.op_Implicit((Object)(object)arriving.Hero))
		{
			return false;
		}
		Vector3 position = ((Component)ally.Hero).transform.position;
		Bounds val = DuelGround.Body(arriving);
		Vector3 val2 = ((Bounds)(ref val)).center - ((Component)arriving.Hero).transform.position;
		if (!InRoom(position) || !ArenaGather.Allows(position))
		{
			return false;
		}
		if (IsSafe(position, arriving))
		{
			result = position;
			return true;
		}
		float num = ((Bounds)(ref val)).extents.y - val2.y;
		Vector3 val4 = default(Vector3);
		for (int i = 0; i <= 5; i++)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				float num2 = position.x + (float)i * 0.6f * (float)j;
				RaycastHit2D val3 = Physics2D.Raycast(new Vector2(num2, position.y + 1.5f), Vector2.down, 5f, 256);
				if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val3)).collider) && !((RaycastHit2D)(ref val3)).collider.isTrigger && !(((RaycastHit2D)(ref val3)).normal.y < 0.6f))
				{
					((Vector3)(ref val4))._002Ector(num2, ((RaycastHit2D)(ref val3)).point.y + num + 0.06f, position.z);
					if (PartyRules.Near(val4.x - position.x, val4.y - position.y) && ArenaGather.Allows(val4) && IsSafe(val4, arriving))
					{
						result = val4;
						return true;
					}
				}
			}
		}
		if (DuelGround.Clear(arriving, position, val2, ((Bounds)(ref val)).size, out var _))
		{
			result = position;
			return true;
		}
		if (ally.HasSafePoint && Time.unscaledTime - ally.SafeAt < 5f && PartyRules.Near(ally.SafePoint.x - position.x, ally.SafePoint.y - position.y) && ArenaGather.Allows(ally.SafePoint) && IsSafe(ally.SafePoint, arriving))
		{
			result = ally.SafePoint;
			return true;
		}
		return false;
	}

	internal Vector3 SafeDestination(PlayerSlot anchor)
	{
		if (anchor.HasSafePoint && IsSafe(anchor.SafePoint, anchor))
		{
			return anchor.SafePoint;
		}
		return ((Component)anchor.Hero).transform.position;
	}

	private Vector3 JoinDestination()
	{
		if (Primary == null || !Object.op_Implicit((Object)(object)Primary.Hero))
		{
			return Vector3.zero;
		}
		PlayerSlot anchor = ArenaGather.Anchor;
		if (anchor != null && anchor != Primary && IsSafe(((Component)anchor.Hero).transform.position, anchor))
		{
			return ((Component)anchor.Hero).transform.position;
		}
		if (Primary.Down)
		{
			PlayerSlot playerSlot = Players.FirstOrDefault((PlayerSlot p) => p != Primary && p.Alive && p.Ready && IsSafe(((Component)p.Hero).transform.position, p));
			if (playerSlot != null)
			{
				return ((Component)playerSlot.Hero).transform.position;
			}
		}
		Vector3 val = ((HasArrival && !Primary.Alive) ? Arrival : ((Component)Primary.Hero).transform.position);
		if (IsSafe(val, Primary))
		{
			return val;
		}
		if (Primary.HasSafePoint && Vector2.Distance(Vector2.op_Implicit(val), Vector2.op_Implicit(Primary.SafePoint)) < 8f && IsSafe(Primary.SafePoint, Primary))
		{
			return Primary.SafePoint;
		}
		GameManager instance = GameManager.instance;
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.cameraCtrl))
		{
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector(Mathf.Clamp(val.x, 1.5f, Mathf.Max(1.5f, instance.cameraCtrl.sceneWidth - 1.5f)), Mathf.Clamp(val.y, 1.5f, Mathf.Max(1.5f, instance.cameraCtrl.sceneHeight - 1.5f)), val.z);
			if (IsSafe(val2, Primary))
			{
				return val2;
			}
			RaycastHit2D val3 = Physics2D.Raycast(new Vector2(val2.x, val2.y + 2f), Vector2.down, 12f, 256);
			if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val3)).collider))
			{
				Vector3 val4 = Primary.Hero.FindGroundPoint(new Vector2(val2.x, val2.y + 2f), true);
				if (IsSafe(val4, Primary))
				{
					return val4;
				}
			}
			return val2;
		}
		return val;
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
				Vector3 position = ((Component)player.Hero).transform.position;
				NativeDreamFx.TeleportTrail(player, position, playerSlot.SafePoint);
				((Component)player.Hero).transform.position = playerSlot.SafePoint;
				((Component)player.Hero).GetComponent<Rigidbody2D>().velocity = Vector2.zero;
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
		benchPosition = ((Component)playerSlot.Hero).transform.position;
		restedBench = true;
		foreach (PlayerSlot player in Players)
		{
			Revival.End(player);
			if (!Object.op_Implicit((Object)(object)player.Hero))
			{
				player.Down = false;
				player.Vitals.Health = player.CurrentMaxHealth;
				player.RetryAt = 0f;
				continue;
			}
			if (player.Down || player.Hazard)
			{
				player.SafePoint = ((Component)playerSlot.Hero).transform.position;
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
			roomBenches = Object.FindObjectsOfType<RestBench>();
			nextBenchScan = Time.unscaledTime + 1f;
		}
		RestBench[] array = roomBenches;
		foreach (RestBench val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && ((Component)val).gameObject.activeInHierarchy && Mathf.Abs(((Component)val).transform.position.x - ((Component)p.Hero).transform.position.x) < 3f && Mathf.Abs(((Component)val).transform.position.y - ((Component)p.Hero).transform.position.y) < 1.8f)
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
		if (restedBench && Object.op_Implicit((Object)(object)p.Hero))
		{
			return Vector2.Distance(Vector2.op_Implicit(((Component)p.Hero).transform.position), Vector2.op_Implicit(benchPosition)) < 7f;
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
		Color result = default(Color);
		if (index >= 0 && index < Local8Mod.Settings.Colors.Length && ColorUtility.TryParseHtmlString("#" + Local8Mod.Settings.Colors[index], ref result))
		{
			return result;
		}
		return Colors[Mathf.Clamp(index, 0, Colors.Length - 1)];
	}

	public void Dispose()
	{
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
		if (Primary != null && Object.op_Implicit((Object)(object)Primary.Hero))
		{
			tk2dSprite component = ((Component)Primary.Hero).GetComponent<tk2dSprite>();
			if (Object.op_Implicit((Object)(object)component))
			{
				((tk2dBaseSprite)component).color = Color.white;
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
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.inputHandler) && originalActions != null)
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
	}
}
