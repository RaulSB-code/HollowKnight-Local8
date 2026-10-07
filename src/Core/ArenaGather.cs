using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ArenaGather
{
	private sealed class Battle
	{
		internal bool IsBattle;

		internal bool IsGate;

		internal bool OpenSeen;

		internal bool CloseUsed;

		internal bool GateSampled;

		internal bool Solid;

		internal PlayerSlot Entrant;

		internal Collider2D Trigger;

		internal float LastTouch;
	}

	private sealed class Transfer
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal tk2dSprite Sprite;

		internal Color Color;

		internal Vector3 Destination;

		internal bool Applied;
	}

	[CompilerGenerated]
	private sealed class __iterator__Gather_d__70 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		public bool initial;

		public int epoch;

		public CoopSession s;

		private PlayerSlot __iterator__anchor_5__2;

		private Dictionary<PlayerSlot, Vector3> __iterator__arrivals_5__3;

		private int __iterator__attempt_5__4;

		private float __iterator__t_5__5;

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
		public __iterator__Gather_d__70(int __iterator___1__state)
		{
			this.__iterator___1__state = __iterator___1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = __iterator___1__state;
			if (num == -3 || (uint)(num - 1) <= 3u)
			{
				try
				{
				}
				finally
				{
					__iterator___m__Finally1();
				}
			}
			__iterator__anchor_5__2 = null;
			__iterator__arrivals_5__3 = null;
			__iterator___1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				bool result;
				bool flag;
				switch (__iterator___1__state)
				{
				default:
					return false;
				case 0:
					__iterator___1__state = -1;
					__iterator___1__state = -3;
					if (initial)
					{
						__iterator___2__current = new WaitForSecondsRealtime(0.25f);
						__iterator___1__state = 1;
						return true;
					}
					goto IL_0065;
				case 1:
					__iterator___1__state = -3;
					goto IL_0065;
				case 2:
					__iterator___1__state = -3;
					goto IL_0727;
				case 3:
					__iterator___1__state = -3;
					__iterator__t_5__5 += Time.unscaledDeltaTime;
					goto IL_0a33;
				case 4:
					{
						__iterator___1__state = -3;
						__iterator__t_5__5 += Time.unscaledDeltaTime;
						goto IL_0d1a;
					}
					IL_0a33:
					if (__iterator__t_5__5 < 0.1f)
					{
						if (epoch != generation || !SceneReady(s))
						{
							result = false;
							break;
						}
						SetAlpha(1f - __iterator__t_5__5 / 0.1f);
						__iterator___2__current = null;
						__iterator___1__state = 3;
						return true;
					}
					foreach (Transfer item in moving)
					{
						PlayerSlot player = item.Player;
						if (!Valid(player) || player.Hero != item.Hero)
						{
							continue;
						}
						Vector3 destination = item.Destination;
						Vector3 vector;
						if (!ArenaLanding.Clear(s, player, destination, out landingFailure))
						{
							string[] obj = new string[6]
							{
								"ARENA landing changed P",
								(player.Index + 1).ToString(),
								" at=",
								null,
								null,
								null
							};
							vector = destination;
							obj[3] = vector.ToString();
							obj[4] = " reason=";
							obj[5] = landingFailure;
							Diagnostics.Write(string.Concat(obj));
							continue;
						}
						destination.z = (float)CoopRules.PlayerDepth(player.Index);
						Vector3 position = player.Hero.transform.position;
						NativeDreamFx.TeleportTrail(player, position, destination);
						player.Hero.transform.position = destination;
						Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
						if ((bool)component)
						{
							component.position = destination;
							component.velocity = Vector2.zero;
						}
						player.ProtectionUntil = Mathf.Max(player.ProtectionUntil, Time.time + 1.25f);
						player.Hero.cState.invulnerable = true;
						player.SafePoint = destination;
						player.HasSafePoint = s.IsSafe(destination, player);
						player.HasPreviousSafePoint = false;
						player.SafeAt = Time.unscaledTime;
						player.Vitals.HazardPoint = destination;
						s.Commit(player);
						player.FarSince = -1f;
						item.Applied = true;
						string[] obj2 = new string[6]
						{
							"ARENA gathered P",
							(player.Index + 1).ToString(),
							" anchor=",
							(__iterator__anchor_5__2 == null) ? "interior" : ("P" + (__iterator__anchor_5__2.Index + 1)),
							" pos=",
							null
						};
						vector = destination;
						obj2[5] = vector.ToString();
						Diagnostics.Write(string.Concat(obj2));
					}
					__iterator__t_5__5 = 0f;
					goto IL_0d1a;
					IL_0065:
					if (epoch != generation || !SceneReady(s) || Time.unscaledTime - closeAt > 4f)
					{
						result = false;
						break;
					}
					if (initial)
					{
						ScanGates(shutting: true);
						HealthManager healthManager = (((bool)pendingTarget && pendingTarget.hp > 0) ? pendingTarget : Fight(s, allowOrdinary: true));
						PlayerSlot playerSlot = ((Time.unscaledTime - contactAt < 2f && Valid(lastEntrant)) ? lastEntrant : SourceEntrant(pendingSender, s));
						if (!Valid(playerSlot))
						{
							playerSlot = pendingContext;
						}
						encounterTarget = healthManager;
						if ((bool)healthManager)
						{
							arenaPosition = healthManager.transform.position;
							if (Boss(healthManager) && !hasCameraBattleBounds)
							{
								PlayerSlot playerSlot2 = NearestBossPlayer(s, healthManager);
								if (Valid(playerSlot2) && Vector2.Distance(playerSlot2.Hero.transform.position, arenaPosition) < 22f)
								{
									combatAnchor = playerSlot2;
								}
								else if (!Valid(combatAnchor) || Vector2.Distance(combatAnchor.Hero.transform.position, arenaPosition) > 22f)
								{
									combatAnchor = null;
								}
							}
							else if (Valid(playerSlot))
							{
								combatAnchor = playerSlot;
							}
						}
						else
						{
							List<ArenaPoint> list = new List<ArenaPoint>();
							List<PlayerSlot> list2 = new List<PlayerSlot>();
							int preferred = -1;
							foreach (PlayerSlot player2 in s.Players)
							{
								if (Valid(player2))
								{
									if (player2 == playerSlot)
									{
										preferred = list2.Count;
									}
									list2.Add(player2);
									Vector3 position2 = player2.Hero.transform.position;
									list.Add(new ArenaPoint(position2.x, position2.y));
								}
							}
							int num = ArenaRules.SelectInside(list.ToArray(), geometry.ToArray(), preferred);
							if (num >= 0)
							{
								playerSlot = list2[num];
							}
							if (!Valid(playerSlot))
							{
								Diagnostics.Write("ARENA unresolved gates=" + geometry.Count);
								result = false;
								break;
							}
							arenaPosition = playerSlot.Hero.transform.position;
							combatAnchor = playerSlot;
						}
						ArenaGather.zone = ArenaRules.Zone(new ArenaPoint(arenaPosition.x, arenaPosition.y), geometry.ToArray());
						if (hasCameraBattleBounds)
						{
							Bounds cameraBattleBounds = ArenaGather.cameraBattleBounds;
							ArenaZone zone = default(ArenaZone);
							zone.Left = cameraBattleBounds.min.x - 0.75f;
							zone.Right = cameraBattleBounds.max.x + 0.75f;
							zone.Bottom = cameraBattleBounds.min.y - 0.75f;
							zone.Top = cameraBattleBounds.max.y + 0.75f;
							zone.Bounded = true;
							ArenaGather.zone = zone;
						}
						else if (Valid(combatAnchor) && !ArenaGather.zone.Contains(combatAnchor.Hero.transform.position.x, combatAnchor.Hero.transform.position.y))
						{
							ArenaGather.zone = default(ArenaZone);
						}
						closed = true;
					}
					__iterator__anchor_5__2 = Anchor;
					if (!rosterCaptured)
					{
						combatAnchor = __iterator__anchor_5__2;
						if (Valid(__iterator__anchor_5__2))
						{
							entrancePoint = __iterator__anchor_5__2.Hero.transform.position;
							hasEntrancePoint = true;
						}
						pendingPlayers.Clear();
						foreach (PlayerSlot player3 in s.Players)
						{
							if (Valid(player3) && NeedsEntry(player3))
							{
								pendingPlayers.Add(player3);
							}
						}
						rosterCaptured = true;
						string[] obj3 = new string[12]
						{
							"ARENA entrance fight=",
							encounterTarget ? encounterTarget.name : "none",
							" gates=",
							geometry.Count.ToString(),
							" anchor=",
							(__iterator__anchor_5__2 == null) ? "none" : ("P" + (__iterator__anchor_5__2.Index + 1)),
							" at=",
							((__iterator__anchor_5__2 == null) ? Vector3.zero : __iterator__anchor_5__2.Hero.transform.position).ToString(),
							" boss=",
							null,
							null,
							null
						};
						Vector3 vector = arenaPosition;
						obj3[9] = vector.ToString();
						obj3[10] = " bounded=";
						obj3[11] = ArenaGather.zone.Bounded.ToString();
						Diagnostics.Write(string.Concat(obj3));
						foreach (PlayerSlot player4 in s.Players)
						{
							if (Valid(player4))
							{
								Diagnostics.Write("ARENA entry P" + (player4.Index + 1) + " at=" + player4.Hero.transform.position.ToString() + " gather=" + pendingPlayers.Contains(player4));
							}
						}
						foreach (Collider2D barrier in barriers)
						{
							if ((bool)barrier)
							{
								Diagnostics.Write("ARENA barrier=" + barrier.name + " shape=" + Shape(barrier).ToString());
							}
						}
					}
					__iterator__arrivals_5__3 = new Dictionary<PlayerSlot, Vector3>(8);
					__iterator__attempt_5__4 = 0;
					goto IL_0727;
					IL_0d1a:
					if (__iterator__t_5__5 < 0.15f)
					{
						if (epoch != generation || !SceneReady(s))
						{
							result = false;
							break;
						}
						SetAlpha(__iterator__t_5__5 / 0.15f);
						__iterator___2__current = null;
						__iterator___1__state = 4;
						return true;
					}
					foreach (Transfer item2 in moving)
					{
						if (item2.Applied && Valid(item2.Player) && Vector2.Distance(item2.Player.Hero.transform.position, item2.Destination) < 3f)
						{
							pendingPlayers.Remove(item2.Player);
						}
					}
					__iterator__anchor_5__2 = null;
					__iterator__arrivals_5__3 = null;
					__iterator___m__Finally1();
					return false;
					IL_0727:
					flag = false;
					foreach (PlayerSlot pendingPlayer in pendingPlayers)
					{
						if (Valid(pendingPlayer) && !__iterator__arrivals_5__3.ContainsKey(pendingPlayer))
						{
							if (TryDestination(s, pendingPlayer, out var destination2))
							{
								__iterator__arrivals_5__3[pendingPlayer] = destination2;
							}
							else
							{
								flag = true;
							}
						}
					}
					if (flag && initial && __iterator__attempt_5__4++ < 6 && epoch == generation && SceneReady(s) && !(Time.unscaledTime - closeAt > 4f))
					{
						__iterator___2__current = new WaitForSecondsRealtime(0.1f);
						__iterator___1__state = 2;
						return true;
					}
					if (epoch != generation || !SceneReady(s))
					{
						result = false;
						break;
					}
					foreach (PlayerSlot pendingPlayer2 in pendingPlayers)
					{
						if (Valid(pendingPlayer2))
						{
							if (!__iterator__arrivals_5__3.TryGetValue(pendingPlayer2, out var value))
							{
								string[] obj4 = new string[8]
								{
									"ARENA no landing P",
									(pendingPlayer2.Index + 1).ToString(),
									" origin=",
									null,
									null,
									null,
									null,
									null
								};
								Vector3 vector = entrancePoint;
								obj4[3] = vector.ToString();
								obj4[4] = " current=";
								obj4[5] = pendingPlayer2.Hero.transform.position.ToString();
								obj4[6] = " reason=";
								obj4[7] = landingFailure;
								Diagnostics.Write(string.Concat(obj4));
							}
							else
							{
								Revival.End(pendingPlayer2);
								pendingPlayer2.ArenaTransfer = true;
								pendingPlayer2.ArenaAlpha = 1f;
								pendingPlayer2.ProtectionUntil = Mathf.Max(pendingPlayer2.ProtectionUntil, Time.time + 1f);
								tk2dSprite component2 = pendingPlayer2.Hero.GetComponent<tk2dSprite>();
								moving.Add(new Transfer
								{
									Player = pendingPlayer2,
									Hero = pendingPlayer2.Hero,
									Sprite = component2,
									Color = (component2 ? component2.color : Color.white),
									Destination = value
								});
							}
						}
					}
					if (moving.Count == 0)
					{
						result = false;
						break;
					}
					__iterator__t_5__5 = 0f;
					goto IL_0a33;
				}
				__iterator___m__Finally1();
				return result;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void __iterator___m__Finally1()
		{
			__iterator___1__state = -1;
			if (epoch == generation)
			{
				Restore();
				busy = (eventPending = false);
				pendingSender = null;
				pendingContext = null;
				nextScan = Time.unscaledTime + 0.4f;
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private static readonly Dictionary<Fsm, Battle> battles = new Dictionary<Fsm, Battle>();

	internal static readonly HashSet<HealthManager> engagedBosses = new HashSet<HealthManager>();

	private static readonly HashSet<CameraLockArea> usedBossLocks = new HashSet<CameraLockArea>();

	private static readonly Dictionary<CameraLockArea, float> bossLockTouches = new Dictionary<CameraLockArea, float>();

	private static readonly List<Transfer> moving = new List<Transfer>(8);

	private static readonly HashSet<GameObject> gateObjects = new HashSet<GameObject>();

	private static readonly HashSet<GameObject> completedGates = new HashSet<GameObject>();

	private static readonly List<Collider2D> barriers = new List<Collider2D>();

	private static readonly List<ArenaGate> geometry = new List<ArenaGate>();

	private static readonly List<Fsm> gateFsms = new List<Fsm>();

	private static readonly HashSet<PlayerSlot> pendingPlayers = new HashSet<PlayerSlot>();

	private static Fsm pendingSender;

	private static PlayerSlot pendingContext;

	private static PlayerSlot lastEntrant;

	private static PlayerSlot combatAnchor;

	internal static HealthManager encounterTarget;

	private static HealthManager endedTarget;

	private static HealthManager pendingTarget;

	private static Vector3 arenaPosition;

	private static ArenaZone zone;

	private static Bounds cameraBattleBounds;

	private static bool hasCameraBattleBounds;

	private static Vector3 entrancePoint;

	private static bool hasEntrancePoint;

	private static bool rosterCaptured;

	private static string landingFailure = "";

	private static float contactAt = -100f;

	private static float closeAt = -100f;

	private static float nextScan;

	private static bool closed;

	private static bool busy;

	private static bool eventPending;

	private static bool finished;

	private const float GatheringWindow = 4f;

	private static int generation;

	internal static bool TransferActive => busy;

	internal static bool EngagedBossAlive
	{
		get
		{
			foreach (HealthManager engagedBoss in engagedBosses)
			{
				if ((bool)engagedBoss && engagedBoss.gameObject.activeInHierarchy && !engagedBoss.GetIsDead() && engagedBoss.hp > 0)
				{
					return true;
				}
			}
			return false;
		}
	}

	internal static PlayerSlot Anchor
	{
		get
		{
			CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
			if (!closed || coopSession == null)
			{
				return null;
			}
			bool flag = Boss(encounterTarget);
			if (Valid(combatAnchor) && Allows(combatAnchor.Hero.transform.position) && (!flag || Vector2.Distance(combatAnchor.Hero.transform.position, arenaPosition) < 22f))
			{
				return combatAnchor;
			}
			PlayerSlot result = null;
			float num = float.MaxValue;
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (Valid(player) && Allows(player.Hero.transform.position))
				{
					float sqrMagnitude = ((Vector2)(player.Hero.transform.position - arenaPosition)).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						result = player;
						num = sqrMagnitude;
					}
				}
			}
			if (!flag || !(num > 484f))
			{
				return result;
			}
			return null;
		}
	}

	private static bool Valid(PlayerSlot p)
	{
		if (p != null && p.Alive && p.Ready)
		{
			return !EmergencyWarp.Active(p);
		}
		return false;
	}

	internal static bool Allows(Vector3 point)
	{
		if (closed)
		{
			return zone.Contains(point.x, point.y);
		}
		return true;
	}

	private static bool SceneReady(CoopSession s)
	{
		GameManager instance = GameManager.instance;
		if (s.Active && !s.TeamWipe && !Plugin.Self.Panel && !PvpMatch.Running && !CoopEnding.Active && (bool)instance && !instance.isPaused)
		{
			return CoopRules.CanGatherArena(instance.IsGameplayScene(), instance.IsLoadingSceneTransition, instance.HasFinishedEnteringScene);
		}
		return false;
	}

	private static string Names(Transform t)
	{
		string text = "";
		int num = 0;
		while ((bool)t && num < 4)
		{
			text = text + " " + t.name;
			num++;
			t = t.parent;
		}
		return text.ToLowerInvariant();
	}

	private static bool GateName(Collider2D c)
	{
		string text = Names(c.transform);
		if (text.Contains("battle gate") || text.Contains("battle_gate") || text.Contains("arena gate") || text.Contains("boss gate"))
		{
			return true;
		}
		foreach (Fsm gateFsm in gateFsms)
		{
			if ((bool)gateFsm.GameObject && (c.gameObject == gateFsm.GameObject || c.transform.IsChildOf(gateFsm.GameObject.transform)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool EventGate(Collider2D c)
	{
		foreach (GameObject gateObject in gateObjects)
		{
			if ((bool)gateObject && (c.gameObject == gateObject || c.transform.IsChildOf(gateObject.transform)))
			{
				return true;
			}
		}
		return false;
	}

	private static Bounds Shape(Collider2D c)
	{
		BoxCollider2D boxCollider2D = c as BoxCollider2D;
		if (!boxCollider2D)
		{
			return c.bounds;
		}
		Vector3 vector = c.transform.TransformPoint(boxCollider2D.offset - boxCollider2D.size * 0.5f);
		Vector3 vector2 = c.transform.TransformPoint(boxCollider2D.offset + boxCollider2D.size * 0.5f);
		return new Bounds((vector + vector2) * 0.5f, new Vector3(Mathf.Abs(vector2.x - vector.x), Mathf.Abs(vector2.y - vector.y), 1f));
	}

	internal static bool Boss(HealthManager h)
	{
		if (!h || (bool)h.GetComponent<LocalShade>())
		{
			return false;
		}
		string text = Names(h.transform);
		if (!BossSceneController.IsBossScene && !h.hasSpecialDeath && !text.Contains("boss") && !text.Contains("hollow knight") && !text.Contains("infected knight") && !text.Contains("hornet") && !text.Contains("false knight") && !text.Contains("mage lord") && !text.Contains("dung defender") && !text.Contains("mantis lord") && !text.Contains("grimm"))
		{
			return text.Contains("radiance");
		}
		return true;
	}

	internal static void CameraContact(CameraLockArea area, Collider2D body)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (!area || !body || coopSession == null || !SceneReady(coopSession) || coopSession.Players.Count < 2 || busy || closed || finished || usedBossLocks.Contains(area))
		{
			return;
		}
		string text = Names(area.transform);
		if (!text.Contains("boss") && !text.Contains("battle") && !text.Contains("arena"))
		{
			return;
		}
		PlayerSlot playerSlot = coopSession.Resolve(body);
		if (!Valid(playerSlot))
		{
			return;
		}
		Collider2D component = area.GetComponent<Collider2D>();
		if (!component || !component.enabled)
		{
			return;
		}
		Bounds bounds = component.bounds;
		if (bounds.size.x < 5f || bounds.size.y < 3f || !bounds.Contains(new Vector3(playerSlot.Hero.transform.position.x, playerSlot.Hero.transform.position.y, bounds.center.z)))
		{
			return;
		}
		if (!bossLockTouches.TryGetValue(area, out var value))
		{
			bossLockTouches[area] = Time.unscaledTime;
		}
		else
		{
			if (Time.unscaledTime - value < 0.2f)
			{
				return;
			}
			bool flag = false;
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (Valid(player) && player != playerSlot)
				{
					Vector3 position = player.Hero.transform.position;
					position.z = bounds.center.z;
					if (!bounds.Contains(position))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				usedBossLocks.Add(area);
				HealthManager healthManager = Fight(coopSession, allowOrdinary: false);
				if ((bool)healthManager)
				{
					engagedBosses.Add(healthManager);
				}
				string[] obj = new string[8]
				{
					"Room_Final_Boss_Atrium",
					area.name,
					" P",
					(playerSlot.Index + 1).ToString(),
					" boss=",
					healthManager ? healthManager.name : "none",
					" bounds=",
					null
				};
				Bounds bounds2 = bounds;
				obj[7] = bounds2.ToString();
				Diagnostics.Write(string.Concat(obj));
				StartGather(coopSession, playerSlot, healthManager, bounds);
			}
		}
	}

	private static void ScanBossLocks(CoopSession s)
	{
		CameraLockArea[] array = UnityEngine.Object.FindObjectsOfType<CameraLockArea>();
		foreach (CameraLockArea cameraLockArea in array)
		{
			if (!cameraLockArea || usedBossLocks.Contains(cameraLockArea))
			{
				continue;
			}
			string text = Names(cameraLockArea.transform);
			if (!text.Contains("boss") && !text.Contains("battle") && !text.Contains("arena"))
			{
				continue;
			}
			Collider2D component = cameraLockArea.GetComponent<Collider2D>();
			if (!component || !component.enabled)
			{
				continue;
			}
			Bounds bounds = component.bounds;
			foreach (PlayerSlot player in s.Players)
			{
				if (!Valid(player))
				{
					continue;
				}
				Vector3 position = player.Hero.transform.position;
				position.z = bounds.center.z;
				if (bounds.Contains(position))
				{
					Collider2D component2 = player.Hero.GetComponent<Collider2D>();
					if ((bool)component2)
					{
						CameraContact(cameraLockArea, component2);
					}
					if (busy)
					{
						return;
					}
				}
			}
		}
	}

	private static void StartGather(CoopSession s, PlayerSlot entrant, HealthManager boss, Bounds? cameraBounds = null)
	{
		generation++;
		Restore();
		ClearBattle();
		finished = false;
		busy = (eventPending = true);
		closeAt = Time.unscaledTime;
		pendingSender = null;
		pendingContext = entrant;
		pendingTarget = boss;
		combatAnchor = entrant;
		nextScan = Time.unscaledTime + 0.4f;
		if (cameraBounds.HasValue)
		{
			cameraBattleBounds = cameraBounds.Value;
			hasCameraBattleBounds = true;
		}
		Plugin.Self.StartCoroutine(Gather(s, generation, initial: true));
	}

	private static void ScanGates(bool shutting)
	{
		barriers.Clear();
		geometry.Clear();
		Collider2D[] array = UnityEngine.Object.FindObjectsOfType<Collider2D>();
		foreach (Collider2D collider2D in array)
		{
			if ((bool)collider2D && !collider2D.isTrigger && collider2D.gameObject.activeInHierarchy && (collider2D.enabled || (shutting && EventGate(collider2D))) && (GateName(collider2D) || EventGate(collider2D)) && !collider2D.GetComponentInParent<HeroController>() && !collider2D.GetComponentInParent<HealthManager>() && !collider2D.GetComponentInParent<TransitionPoint>())
			{
				Bounds bounds = Shape(collider2D);
				if (!(Mathf.Max(bounds.size.x, bounds.size.y) < 2.5f) && !(Mathf.Max(bounds.size.x, bounds.size.y) < Mathf.Min(bounds.size.x, bounds.size.y) * 1.2f))
				{
					barriers.Add(collider2D);
					geometry.Add(new ArenaGate(bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y));
				}
			}
		}
	}

	private static HealthManager Fight(CoopSession s, bool allowOrdinary)
	{
		HealthManager result = null;
		float num = float.MaxValue;
		HealthManager[] array = UnityEngine.Object.FindObjectsOfType<HealthManager>();
		foreach (HealthManager healthManager in array)
		{
			if (!healthManager || healthManager.hp <= 0 || !healthManager.gameObject.activeInHierarchy || (bool)healthManager.GetComponent<LocalShade>())
			{
				continue;
			}
			bool flag = Boss(healthManager);
			if (!flag && !allowOrdinary)
			{
				continue;
			}
			float num2 = float.MaxValue;
			foreach (PlayerSlot player in s.Players)
			{
				if (Valid(player))
				{
					num2 = Mathf.Min(num2, ((Vector2)(player.Hero.transform.position - healthManager.transform.position)).sqrMagnitude);
				}
			}
			if (!(num2 > 6400f))
			{
				float num3 = num2 + (float)((!flag) ? 10000 : 0);
				if (num3 < num)
				{
					num = num3;
					result = healthManager;
				}
			}
		}
		return result;
	}

	private static PlayerSlot NearestBossPlayer(CoopSession s, HealthManager boss)
	{
		if (!boss)
		{
			return null;
		}
		PlayerSlot result = null;
		float num = float.MaxValue;
		foreach (PlayerSlot player in s.Players)
		{
			if (Valid(player) && !player.Hero.cState.transitioning)
			{
				float sqrMagnitude = ((Vector2)(player.Hero.transform.position - boss.transform.position)).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = player;
				}
			}
		}
		return result;
	}

	private static bool NeedsEntry(PlayerSlot p)
	{
		float num = (hasEntrancePoint ? Vector2.Distance(p.Hero.transform.position, entrancePoint) : float.PositiveInfinity);
		return ArenaRules.NeedsEntry(p == combatAnchor, zone.Bounded, Allows(p.Hero.transform.position), num);
	}

	internal static void BossHit(HealthManager boss, PlayerSlot attacker)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (coopSession != null && SceneReady(coopSession) && coopSession.Players.Count >= 2 && Valid(attacker) && Boss(boss) && boss.hp > 0 && !engagedBosses.Contains(boss) && (!finished || !(boss == endedTarget)))
		{
			engagedBosses.Add(boss);
		}
	}

	internal static void Tick(CoopSession s)
	{
		if (PvpArena.Active || busy || !SceneReady(s) || Time.unscaledTime < nextScan)
		{
			return;
		}
		nextScan = Time.unscaledTime + 0.4f;
		ScanGateClosures(s);
		if (busy)
		{
			return;
		}
		ScanBossLocks(s);
		if (busy)
		{
			return;
		}
		ScanGates(eventPending || Time.unscaledTime - closeAt < 1.5f);
		if (closed && Time.unscaledTime - closeAt > 4f)
		{
			if (pendingPlayers.Count > 0)
			{
				Diagnostics.Write("ARENA entry window ended pending=" + pendingPlayers.Count + " reason=" + landingFailure);
			}
			EndBattle();
		}
		else
		{
			if (finished)
			{
				return;
			}
			if (closed && (object)encounterTarget != null && (!encounterTarget || !encounterTarget.gameObject.activeInHierarchy || encounterTarget.GetIsDead() || encounterTarget.hp <= 0))
			{
				EndBattle();
				return;
			}
			if (closed && pendingPlayers.Count > 0)
			{
				busy = true;
				Plugin.Self.StartCoroutine(Gather(s, generation, initial: false));
				return;
			}
			HealthManager healthManager = Fight(s, eventPending || closed);
			if (closed && !eventPending && !healthManager && Time.unscaledTime - closeAt > 2f)
			{
				EndBattle();
			}
			else
			{
				if (closed || !healthManager || geometry.Count == 0)
				{
					return;
				}
				arenaPosition = healthManager.transform.position;
				zone = ArenaRules.Zone(new ArenaPoint(arenaPosition.x, arenaPosition.y), geometry.ToArray());
				if (!zone.Bounded)
				{
					return;
				}
				bool flag = false;
				bool flag2 = false;
				foreach (PlayerSlot player in s.Players)
				{
					if (Valid(player))
					{
						if (zone.Contains(player.Hero.transform.position.x, player.Hero.transform.position.y))
						{
							flag = true;
						}
						else
						{
							flag2 = true;
						}
					}
				}
				if (flag)
				{
					closed = true;
					closeAt = Time.unscaledTime;
					encounterTarget = healthManager;
					Diagnostics.Write("ARENA detected fight=" + healthManager.name + " barriers=" + geometry.Count + " split=" + flag2);
					if (flag2)
					{
						busy = true;
						Plugin.Self.StartCoroutine(Gather(s, generation, initial: false));
					}
				}
			}
		}
	}

	private static Battle Binding(Fsm f)
	{
		if (f == null)
		{
			return null;
		}
		if (battles.TryGetValue(f, out var value))
		{
			return value;
		}
		string text = (f.Name + " " + (f.GameObject ? Names(f.GameObject.transform) : "")).ToLowerInvariant();
		value = new Battle
		{
			IsBattle = (text.Contains("battle") || text.Contains("arena")),
			IsGate = (text.Contains("bg control") || text.Contains("battle gate") || text.Contains("battle_gate") || text.Contains("arena gate"))
		};
		if (value.IsGate)
		{
			gateFsms.Add(f);
		}
		if ((bool)f.GameObject)
		{
			value.Trigger = f.GameObject.GetComponent<Collider2D>();
		}
		battles[f] = value;
		return value;
	}

	internal static void ObserveState(FsmState state)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (state == null || state.Fsm == null || coopSession == null || !coopSession.Active)
		{
			return;
		}
		Battle battle = Binding(state.Fsm);
		if (battle.IsGate)
		{
			string text = (state.Name ?? "").ToLowerInvariant();
			if (text.Contains("open") && !text.Contains("check"))
			{
				battle.OpenSeen = true;
			}
			if (ArenaRules.ClosingGateState(text, battle.OpenSeen) && !battle.CloseUsed && SceneReady(coopSession))
			{
				battle.CloseUsed = true;
				Cue(state.Fsm, state.Fsm, null, "gate state " + state.Name);
			}
		}
	}

	private static void ScanGateClosures(CoopSession s)
	{
		foreach (Fsm gateFsm in gateFsms)
		{
			if (!gateFsm.GameObject)
			{
				continue;
			}
			Battle battle = battles[gateFsm];
			bool flag = false;
			Collider2D[] componentsInChildren = gateFsm.GameObject.GetComponentsInChildren<Collider2D>(includeInactive: true);
			foreach (Collider2D collider2D in componentsInChildren)
			{
				if ((bool)collider2D && collider2D.enabled && !collider2D.isTrigger && collider2D.gameObject.activeInHierarchy && (collider2D.gameObject.layer == 8 || collider2D.gameObject.layer == 25))
				{
					flag = true;
					break;
				}
			}
			bool num = battle.GateSampled && !battle.Solid && flag;
			battle.Solid = flag;
			battle.GateSampled = true;
			if (num && !battle.CloseUsed)
			{
				battle.CloseUsed = true;
				Cue(gateFsm, gateFsm, null, "gate collider enabled");
				if (busy)
				{
					break;
				}
			}
		}
	}

	private static bool Present(Battle b)
	{
		if (b == null || !Valid(b.Entrant))
		{
			return false;
		}
		if (!b.Trigger)
		{
			return Time.unscaledTime - b.LastTouch < 1f;
		}
		Bounds bounds = b.Trigger.bounds;
		Vector3 position = b.Entrant.Hero.transform.position;
		position.z = bounds.center.z;
		bounds.Expand(new Vector3(0.5f, 0.5f, 0f));
		return bounds.Contains(position);
	}

	internal static void Contact(Fsm f, Collider2D other)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (coopSession == null || !coopSession.Active)
		{
			return;
		}
		Battle battle = Binding(f);
		PlayerSlot playerSlot = coopSession.Resolve(other);
		if (battle != null && Valid(playerSlot) && (!Present(battle) || battle.Entrant == playerSlot))
		{
			battle.Entrant = playerSlot;
			battle.LastTouch = Time.unscaledTime;
			if (battle.IsBattle && !battle.IsGate)
			{
				lastEntrant = playerSlot;
				contactAt = Time.unscaledTime;
			}
		}
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		Battle battle = Binding(f);
		if (battle == null || !battle.IsBattle)
		{
			return null;
		}
		if (Present(battle))
		{
			return battle.Entrant;
		}
		battle.Entrant = null;
		CoopSession session = Plugin.Self.Session;
		if (session == null || !battle.Trigger)
		{
			return null;
		}
		foreach (PlayerSlot player in session.Players)
		{
			if (Valid(player))
			{
				Vector3 position = player.Hero.transform.position;
				position.z = battle.Trigger.bounds.center.z;
				if (battle.Trigger.bounds.Contains(position))
				{
					battle.Entrant = player;
					battle.LastTouch = Time.unscaledTime;
					lastEntrant = player;
					contactAt = Time.unscaledTime;
					return player;
				}
			}
		}
		return null;
	}

	private static PlayerSlot SourceEntrant(Fsm sender, CoopSession s)
	{
		if (sender == null || !sender.GameObject)
		{
			return null;
		}
		Battle battle = Binding(sender);
		if (Present(battle))
		{
			return battle.Entrant;
		}
		foreach (KeyValuePair<Fsm, Battle> battle2 in battles)
		{
			if ((bool)battle2.Key.GameObject && battle2.Key.GameObject.transform.IsChildOf(sender.GameObject.transform) && Present(battle2.Value))
			{
				return battle2.Value.Entrant;
			}
		}
		Collider2D[] componentsInChildren = sender.GameObject.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Collider2D collider2D in componentsInChildren)
		{
			if (!collider2D.isTrigger)
			{
				continue;
			}
			foreach (PlayerSlot player in s.Players)
			{
				if (Valid(player) && collider2D.OverlapPoint(player.Hero.transform.position))
				{
					return player;
				}
			}
		}
		if (battle.IsBattle || (bool)sender.GameObject.GetComponentInParent<HealthManager>())
		{
			return s.Nearest(sender.GameObject.transform.position);
		}
		return null;
	}

	internal static void Observe(Fsm f, FsmEvent evt, FsmEventData data)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (coopSession != null && coopSession.Active && evt != null && !(evt.Name == "BG OPEN") && !(evt.Name == "BATTLE END") && CoopRules.StartsBattle(evt.Name) && SceneReady(coopSession))
		{
			Fsm sender = ((data != null && data.SentByFsm != null) ? data.SentByFsm : FsmExecutionStack.ExecutingFsm);
			Cue(f, sender, PlayerContext.Current, "event " + evt.Name);
		}
	}

	private static void Cue(Fsm f, Fsm sender, PlayerSlot context, string reason)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (coopSession == null || !SceneReady(coopSession) || coopSession.Players.Count < 2)
		{
			return;
		}
		if (closed && Time.unscaledTime - closeAt > 4f)
		{
			EndBattle();
			return;
		}
		if (finished)
		{
			HealthManager healthManager = Fight(coopSession, allowOrdinary: false);
			bool flag = f != null && (bool)f.GameObject && Binding(f).IsGate && !completedGates.Contains(f.GameObject);
			if (((bool)healthManager && healthManager == endedTarget) || (!healthManager && !flag))
			{
				return;
			}
			finished = false;
			ClearBattle();
		}
		if (!busy && !closed)
		{
			busy = (eventPending = true);
			closeAt = Time.unscaledTime;
			gateObjects.Clear();
			pendingSender = null;
			pendingContext = null;
			Diagnostics.Write("ARENA cue=" + reason + " source=" + ((sender != null && (bool)sender.GameObject) ? (sender.GameObject.name + "/" + sender.Name) : "none"));
			self.StartCoroutine(Gather(coopSession, generation, initial: true));
		}
		if (sender != null && (bool)sender.GameObject && (pendingSender == null || Binding(sender).IsBattle || (bool)sender.GameObject.GetComponentInParent<HealthManager>()))
		{
			pendingSender = sender;
		}
		if (Valid(context) && !Valid(pendingContext))
		{
			pendingContext = context;
		}
		if (f == null || !f.GameObject)
		{
			return;
		}
		if (Binding(f).IsBattle)
		{
			PlayerSlot p = Resolve(f);
			if (Valid(p))
			{
				lastEntrant = p;
				contactAt = Time.unscaledTime;
			}
		}
		string text = (f.Name + " " + Names(f.GameObject.transform)).ToLowerInvariant();
		if (text.Contains("bg control") || text.Contains("gate") || text.Contains("barrier"))
		{
			gateObjects.Add(f.GameObject);
		}
	}

	internal static bool TryDestination(CoopSession s, PlayerSlot p, out Vector3 destination)
	{
		destination = Vector3.zero;
		if (hasEntrancePoint)
		{
			if (ArenaLanding.Near(s, p, entrancePoint, out destination, out landingFailure))
			{
				return true;
			}
			if (ArenaLanding.Floor(s, p, entrancePoint, 5f, out destination, out landingFailure))
			{
				return true;
			}
		}
		if (closed && ArenaLanding.Floor(s, p, arenaPosition, 12f, out destination, out landingFailure))
		{
			return true;
		}
		return false;
	}

	[IteratorStateMachine(typeof(__iterator__Gather_d__70))]
	private static IEnumerator Gather(CoopSession s, int epoch, bool initial)
	{
		return new __iterator__Gather_d__70(0)
		{
			s = s,
			epoch = epoch,
			initial = initial
		};
	}

	private static void SetAlpha(float alpha)
	{
		foreach (Transfer item in moving)
		{
			item.Player.ArenaAlpha = Mathf.Clamp01(alpha);
		}
	}

	internal static void VisualTick(CoopSession s)
	{
		foreach (Transfer item in moving)
		{
			PlayerSlot player = item.Player;
			if ((bool)item.Sprite && !(player.Hero != item.Hero) && player.Alive)
			{
				Color color = item.Sprite.color;
				color.a = item.Color.a * player.ArenaAlpha;
				item.Sprite.color = color;
				Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					component.velocity = Vector2.zero;
				}
			}
		}
	}

	private static void Restore()
	{
		foreach (Transfer item in moving)
		{
			item.Player.ArenaTransfer = false;
			item.Player.ArenaAlpha = 1f;
			if ((bool)item.Sprite && item.Player.Hero == item.Hero && item.Player.Alive)
			{
				Color color = item.Sprite.color;
				color.a = item.Color.a;
				item.Sprite.color = color;
			}
		}
		moving.Clear();
	}

	private static void ClearBattle()
	{
		closed = false;
		encounterTarget = null;
		pendingTarget = null;
		combatAnchor = null;
		hasCameraBattleBounds = false;
		zone = default(ArenaZone);
		lastEntrant = null;
		gateObjects.Clear();
		pendingPlayers.Clear();
		hasEntrancePoint = (rosterCaptured = false);
		landingFailure = "";
		foreach (Battle value in battles.Values)
		{
			value.Entrant = null;
		}
	}

	private static void EndBattle()
	{
		endedTarget = encounterTarget;
		foreach (GameObject gateObject in gateObjects)
		{
			if ((bool)gateObject)
			{
				completedGates.Add(gateObject);
			}
		}
		generation++;
		Restore();
		ClearBattle();
		busy = (eventPending = false);
		pendingSender = null;
		pendingContext = null;
		finished = true;
		Diagnostics.Write("ARENA ended; gathering disabled");
	}

	internal static void Reset()
	{
		generation++;
		Restore();
		ClearBattle();
		endedTarget = null;
		engagedBosses.Clear();
		usedBossLocks.Clear();
		bossLockTouches.Clear();
		battles.Clear();
		gateFsms.Clear();
		completedGates.Clear();
		barriers.Clear();
		geometry.Clear();
		arenaPosition = Vector3.zero;
		busy = (eventPending = (finished = false));
		contactAt = (closeAt = -100f);
		nextScan = 0f;
		pendingSender = null;
		pendingContext = null;
	}
}
