using System.Collections;
using System.Collections.Generic;
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

	private static readonly Dictionary<Fsm, Battle> battles = new Dictionary<Fsm, Battle>();

	private static readonly HashSet<HealthManager> engagedBosses = new HashSet<HealthManager>();

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

	private static HealthManager encounterTarget;

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
				if (Object.op_Implicit((Object)(object)engagedBoss) && ((Component)engagedBoss).gameObject.activeInHierarchy && !engagedBoss.GetIsDead() && engagedBoss.hp > 0)
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
			CoopSession coopSession = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
			if (!closed || coopSession == null)
			{
				return null;
			}
			bool flag = Boss(encounterTarget);
			if (Valid(combatAnchor) && Allows(((Component)combatAnchor.Hero).transform.position) && (!flag || Vector2.Distance(Vector2.op_Implicit(((Component)combatAnchor.Hero).transform.position), Vector2.op_Implicit(arenaPosition)) < 22f))
			{
				return combatAnchor;
			}
			PlayerSlot result = null;
			float num = float.MaxValue;
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (Valid(player) && Allows(((Component)player.Hero).transform.position))
				{
					Vector2 val = Vector2.op_Implicit(((Component)player.Hero).transform.position - arenaPosition);
					float sqrMagnitude = ((Vector2)(ref val)).sqrMagnitude;
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
		if (s.Active && !s.TeamWipe && !Plugin.Self.Panel && !PvpMatch.Running && !CoopEnding.Active && Object.op_Implicit((Object)(object)instance) && !instance.isPaused)
		{
			return CoopRules.CanGatherArena(instance.IsGameplayScene(), instance.IsLoadingSceneTransition, instance.HasFinishedEnteringScene);
		}
		return false;
	}

	private static string Names(Transform t)
	{
		string text = "";
		int num = 0;
		while (Object.op_Implicit((Object)(object)t) && num < 4)
		{
			text = text + " " + ((Object)t).name;
			num++;
			t = t.parent;
		}
		return text.ToLowerInvariant();
	}

	private static bool GateName(Collider2D c)
	{
		string text = Names(((Component)c).transform);
		if (text.Contains("battle gate") || text.Contains("battle_gate") || text.Contains("arena gate") || text.Contains("boss gate"))
		{
			return true;
		}
		foreach (Fsm gateFsm in gateFsms)
		{
			if (Object.op_Implicit((Object)(object)gateFsm.GameObject) && ((Object)(object)((Component)c).gameObject == (Object)(object)gateFsm.GameObject || ((Component)c).transform.IsChildOf(gateFsm.GameObject.transform)))
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
			if (Object.op_Implicit((Object)(object)gateObject) && ((Object)(object)((Component)c).gameObject == (Object)(object)gateObject || ((Component)c).transform.IsChildOf(gateObject.transform)))
			{
				return true;
			}
		}
		return false;
	}

	private static Bounds Shape(Collider2D c)
	{
		BoxCollider2D val = (BoxCollider2D)(object)((c is BoxCollider2D) ? c : null);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return c.bounds;
		}
		Vector3 val2 = ((Component)c).transform.TransformPoint(Vector2.op_Implicit(((Collider2D)val).offset - val.size * 0.5f));
		Vector3 val3 = ((Component)c).transform.TransformPoint(Vector2.op_Implicit(((Collider2D)val).offset + val.size * 0.5f));
		return new Bounds((val2 + val3) * 0.5f, new Vector3(Mathf.Abs(val3.x - val2.x), Mathf.Abs(val3.y - val2.y), 1f));
	}

	private static bool Boss(HealthManager h)
	{
		if (!Object.op_Implicit((Object)(object)h) || Object.op_Implicit((Object)(object)((Component)h).GetComponent<LocalShade>()))
		{
			return false;
		}
		string text = Names(((Component)h).transform);
		if (!BossSceneController.IsBossScene && !h.hasSpecialDeath && !text.Contains("boss") && !text.Contains("hollow knight") && !text.Contains("infected knight") && !text.Contains("hornet") && !text.Contains("false knight") && !text.Contains("mage lord") && !text.Contains("dung defender") && !text.Contains("mantis lord") && !text.Contains("grimm"))
		{
			return text.Contains("radiance");
		}
		return true;
	}

	internal unsafe static void CameraContact(CameraLockArea area, Collider2D body)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
		if (!Object.op_Implicit((Object)(object)area) || !Object.op_Implicit((Object)(object)body) || coopSession == null || !SceneReady(coopSession) || coopSession.Players.Count < 2 || busy || closed || finished || usedBossLocks.Contains(area))
		{
			return;
		}
		string text = Names(((Component)area).transform);
		if (!text.Contains("boss") && !text.Contains("battle") && !text.Contains("arena"))
		{
			return;
		}
		PlayerSlot playerSlot = coopSession.Resolve(body);
		if (!Valid(playerSlot))
		{
			return;
		}
		Collider2D component = ((Component)area).GetComponent<Collider2D>();
		if (!Object.op_Implicit((Object)(object)component) || !((Behaviour)component).enabled)
		{
			return;
		}
		Bounds bounds = component.bounds;
		if (((Bounds)(ref bounds)).size.x < 5f || ((Bounds)(ref bounds)).size.y < 3f || !((Bounds)(ref bounds)).Contains(new Vector3(((Component)playerSlot.Hero).transform.position.x, ((Component)playerSlot.Hero).transform.position.y, ((Bounds)(ref bounds)).center.z)))
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
					Vector3 position = ((Component)player.Hero).transform.position;
					position.z = ((Bounds)(ref bounds)).center.z;
					if (!((Bounds)(ref bounds)).Contains(position))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				usedBossLocks.Add(area);
				HealthManager val = Fight(coopSession, allowOrdinary: false);
				if (Object.op_Implicit((Object)(object)val))
				{
					engagedBosses.Add(val);
				}
				string[] obj = new string[8]
				{
					"Room_Final_Boss_Atrium",
					((Object)area).name,
					" P",
					(playerSlot.Index + 1).ToString(),
					" boss=",
					Object.op_Implicit((Object)(object)val) ? ((Object)val).name : "none",
					" bounds=",
					null
				};
				Bounds val2 = bounds;
				obj[7] = ((object)(*(Bounds*)(&val2))/*cast due to .constrained prefix*/).ToString();
				Diagnostics.Write(string.Concat(obj));
				StartGather(coopSession, playerSlot, val, bounds);
			}
		}
	}

	private static void ScanBossLocks(CoopSession s)
	{
		CameraLockArea[] array = Object.FindObjectsOfType<CameraLockArea>();
		foreach (CameraLockArea val in array)
		{
			if (!Object.op_Implicit((Object)(object)val) || usedBossLocks.Contains(val))
			{
				continue;
			}
			string text = Names(((Component)val).transform);
			if (!text.Contains("boss") && !text.Contains("battle") && !text.Contains("arena"))
			{
				continue;
			}
			Collider2D component = ((Component)val).GetComponent<Collider2D>();
			if (!Object.op_Implicit((Object)(object)component) || !((Behaviour)component).enabled)
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
				Vector3 position = ((Component)player.Hero).transform.position;
				position.z = ((Bounds)(ref bounds)).center.z;
				if (((Bounds)(ref bounds)).Contains(position))
				{
					Collider2D component2 = ((Component)player.Hero).GetComponent<Collider2D>();
					if (Object.op_Implicit((Object)(object)component2))
					{
						CameraContact(val, component2);
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
		((MonoBehaviour)Plugin.Self).StartCoroutine(Gather(s, generation, initial: true));
	}

	private static void ScanGates(bool shutting)
	{
		barriers.Clear();
		geometry.Clear();
		Collider2D[] array = Object.FindObjectsOfType<Collider2D>();
		foreach (Collider2D val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && !val.isTrigger && ((Component)val).gameObject.activeInHierarchy && (((Behaviour)val).enabled || (shutting && EventGate(val))) && (GateName(val) || EventGate(val)) && !Object.op_Implicit((Object)(object)((Component)val).GetComponentInParent<HeroController>()) && !Object.op_Implicit((Object)(object)((Component)val).GetComponentInParent<HealthManager>()) && !Object.op_Implicit((Object)(object)((Component)val).GetComponentInParent<TransitionPoint>()))
			{
				Bounds val2 = Shape(val);
				if (!(Mathf.Max(((Bounds)(ref val2)).size.x, ((Bounds)(ref val2)).size.y) < 2.5f) && !(Mathf.Max(((Bounds)(ref val2)).size.x, ((Bounds)(ref val2)).size.y) < Mathf.Min(((Bounds)(ref val2)).size.x, ((Bounds)(ref val2)).size.y) * 1.2f))
				{
					barriers.Add(val);
					geometry.Add(new ArenaGate(((Bounds)(ref val2)).min.x, ((Bounds)(ref val2)).max.x, ((Bounds)(ref val2)).min.y, ((Bounds)(ref val2)).max.y));
				}
			}
		}
	}

	private static HealthManager Fight(CoopSession s, bool allowOrdinary)
	{
		HealthManager result = null;
		float num = float.MaxValue;
		HealthManager[] array = Object.FindObjectsOfType<HealthManager>();
		foreach (HealthManager val in array)
		{
			if (!Object.op_Implicit((Object)(object)val) || val.hp <= 0 || !((Component)val).gameObject.activeInHierarchy || Object.op_Implicit((Object)(object)((Component)val).GetComponent<LocalShade>()))
			{
				continue;
			}
			bool flag = Boss(val);
			if (!flag && !allowOrdinary)
			{
				continue;
			}
			float num2 = float.MaxValue;
			foreach (PlayerSlot player in s.Players)
			{
				if (Valid(player))
				{
					float num3 = num2;
					Vector2 val2 = Vector2.op_Implicit(((Component)player.Hero).transform.position - ((Component)val).transform.position);
					num2 = Mathf.Min(num3, ((Vector2)(ref val2)).sqrMagnitude);
				}
			}
			if (!(num2 > 6400f))
			{
				float num4 = num2 + (float)((!flag) ? 10000 : 0);
				if (num4 < num)
				{
					num = num4;
					result = val;
				}
			}
		}
		return result;
	}

	private static PlayerSlot NearestBossPlayer(CoopSession s, HealthManager boss)
	{
		if (!Object.op_Implicit((Object)(object)boss))
		{
			return null;
		}
		PlayerSlot result = null;
		float num = float.MaxValue;
		foreach (PlayerSlot player in s.Players)
		{
			if (Valid(player) && !player.Hero.cState.transitioning)
			{
				Vector2 val = Vector2.op_Implicit(((Component)player.Hero).transform.position - ((Component)boss).transform.position);
				float sqrMagnitude = ((Vector2)(ref val)).sqrMagnitude;
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
		float num = (hasEntrancePoint ? Vector2.Distance(Vector2.op_Implicit(((Component)p.Hero).transform.position), Vector2.op_Implicit(entrancePoint)) : float.PositiveInfinity);
		return ArenaRules.NeedsEntry(p == combatAnchor, zone.Bounded, Allows(((Component)p.Hero).transform.position), num);
	}

	internal static void BossHit(HealthManager boss, PlayerSlot attacker)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
		if (coopSession == null || !SceneReady(coopSession) || coopSession.Players.Count < 2 || !Valid(attacker) || !Boss(boss) || boss.hp <= 0 || engagedBosses.Contains(boss) || (finished && (Object)(object)boss == (Object)(object)endedTarget))
		{
			return;
		}
		engagedBosses.Add(boss);
		bool flag = false;
		foreach (PlayerSlot player in coopSession.Players)
		{
			if (Valid(player) && player != attacker && Vector2.Distance(Vector2.op_Implicit(((Component)player.Hero).transform.position), Vector2.op_Implicit(((Component)attacker.Hero).transform.position)) > 8f)
			{
				flag = true;
				break;
			}
		}
		Diagnostics.Write("ARENA boss engaged=" + ((Object)boss).name + " by P" + (attacker.Index + 1) + " split=" + flag);
		if (!flag)
		{
			return;
		}
		if (closed)
		{
			if (!busy && Time.unscaledTime - closeAt < 4f)
			{
				busy = true;
				((MonoBehaviour)Plugin.Self).StartCoroutine(Gather(coopSession, generation, initial: false));
			}
		}
		else
		{
			StartGather(coopSession, attacker, boss);
		}
	}

	internal static void Tick(CoopSession s)
	{
		if (busy || !SceneReady(s) || Time.unscaledTime < nextScan)
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
			if (closed && encounterTarget != null && (!Object.op_Implicit((Object)(object)encounterTarget) || !((Component)encounterTarget).gameObject.activeInHierarchy || encounterTarget.GetIsDead() || encounterTarget.hp <= 0))
			{
				EndBattle();
				return;
			}
			if (closed && pendingPlayers.Count > 0)
			{
				busy = true;
				((MonoBehaviour)Plugin.Self).StartCoroutine(Gather(s, generation, initial: false));
				return;
			}
			HealthManager val = Fight(s, eventPending || closed);
			if (closed && !eventPending && !Object.op_Implicit((Object)(object)val) && Time.unscaledTime - closeAt > 2f)
			{
				EndBattle();
			}
			else
			{
				if (closed || !Object.op_Implicit((Object)(object)val) || geometry.Count == 0)
				{
					return;
				}
				arenaPosition = ((Component)val).transform.position;
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
						if (zone.Contains(((Component)player.Hero).transform.position.x, ((Component)player.Hero).transform.position.y))
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
					encounterTarget = val;
					Diagnostics.Write("ARENA detected fight=" + ((Object)val).name + " barriers=" + geometry.Count + " split=" + flag2);
					if (flag2)
					{
						busy = true;
						((MonoBehaviour)Plugin.Self).StartCoroutine(Gather(s, generation, initial: false));
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
		string text = (f.Name + " " + (Object.op_Implicit((Object)(object)f.GameObject) ? Names(f.GameObject.transform) : "")).ToLowerInvariant();
		value = new Battle
		{
			IsBattle = (text.Contains("battle") || text.Contains("arena")),
			IsGate = (text.Contains("bg control") || text.Contains("battle gate") || text.Contains("battle_gate") || text.Contains("arena gate"))
		};
		if (value.IsGate)
		{
			gateFsms.Add(f);
		}
		if (Object.op_Implicit((Object)(object)f.GameObject))
		{
			value.Trigger = f.GameObject.GetComponent<Collider2D>();
		}
		battles[f] = value;
		return value;
	}

	internal static void ObserveState(FsmState state)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
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
			if (!Object.op_Implicit((Object)(object)gateFsm.GameObject))
			{
				continue;
			}
			Battle battle = battles[gateFsm];
			bool flag = false;
			Collider2D[] componentsInChildren = gateFsm.GameObject.GetComponentsInChildren<Collider2D>(true);
			foreach (Collider2D val in componentsInChildren)
			{
				if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).enabled && !val.isTrigger && ((Component)val).gameObject.activeInHierarchy && (((Component)val).gameObject.layer == 8 || ((Component)val).gameObject.layer == 25))
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
		if (!Object.op_Implicit((Object)(object)b.Trigger))
		{
			return Time.unscaledTime - b.LastTouch < 1f;
		}
		Bounds bounds = b.Trigger.bounds;
		Vector3 position = ((Component)b.Entrant.Hero).transform.position;
		position.z = ((Bounds)(ref bounds)).center.z;
		((Bounds)(ref bounds)).Expand(new Vector3(0.5f, 0.5f, 0f));
		return ((Bounds)(ref bounds)).Contains(position);
	}

	internal static void Contact(Fsm f, Collider2D other)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
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
		if (session == null || !Object.op_Implicit((Object)(object)battle.Trigger))
		{
			return null;
		}
		foreach (PlayerSlot player in session.Players)
		{
			if (Valid(player))
			{
				Vector3 position = ((Component)player.Hero).transform.position;
				Bounds bounds = battle.Trigger.bounds;
				position.z = ((Bounds)(ref bounds)).center.z;
				bounds = battle.Trigger.bounds;
				if (((Bounds)(ref bounds)).Contains(position))
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
		if (sender == null || !Object.op_Implicit((Object)(object)sender.GameObject))
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
			if (Object.op_Implicit((Object)(object)battle2.Key.GameObject) && battle2.Key.GameObject.transform.IsChildOf(sender.GameObject.transform) && Present(battle2.Value))
			{
				return battle2.Value.Entrant;
			}
		}
		Collider2D[] componentsInChildren = sender.GameObject.GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D val in componentsInChildren)
		{
			if (!val.isTrigger)
			{
				continue;
			}
			foreach (PlayerSlot player in s.Players)
			{
				if (Valid(player) && val.OverlapPoint(Vector2.op_Implicit(((Component)player.Hero).transform.position)))
				{
					return player;
				}
			}
		}
		if (battle.IsBattle || Object.op_Implicit((Object)(object)sender.GameObject.GetComponentInParent<HealthManager>()))
		{
			return s.Nearest(sender.GameObject.transform.position);
		}
		return null;
	}

	internal static void Observe(Fsm f, FsmEvent evt, FsmEventData data)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
		if (coopSession != null && coopSession.Active && evt != null && !(evt.Name == "BG OPEN") && !(evt.Name == "BATTLE END") && CoopRules.StartsBattle(evt.Name) && SceneReady(coopSession))
		{
			Fsm sender = ((data != null && data.SentByFsm != null) ? data.SentByFsm : FsmExecutionStack.ExecutingFsm);
			Cue(f, sender, PlayerContext.Current, "event " + evt.Name);
		}
	}

	private static void Cue(Fsm f, Fsm sender, PlayerSlot context, string reason)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
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
			HealthManager val = Fight(coopSession, allowOrdinary: false);
			bool flag = f != null && Object.op_Implicit((Object)(object)f.GameObject) && Binding(f).IsGate && !completedGates.Contains(f.GameObject);
			if ((Object.op_Implicit((Object)(object)val) && (Object)(object)val == (Object)(object)endedTarget) || (!Object.op_Implicit((Object)(object)val) && !flag))
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
			Diagnostics.Write("ARENA cue=" + reason + " source=" + ((sender != null && Object.op_Implicit((Object)(object)sender.GameObject)) ? (((Object)sender.GameObject).name + "/" + sender.Name) : "none"));
			((MonoBehaviour)self).StartCoroutine(Gather(coopSession, generation, initial: true));
		}
		if (sender != null && Object.op_Implicit((Object)(object)sender.GameObject) && (pendingSender == null || Binding(sender).IsBattle || Object.op_Implicit((Object)(object)sender.GameObject.GetComponentInParent<HealthManager>())))
		{
			pendingSender = sender;
		}
		if (Valid(context) && !Valid(pendingContext))
		{
			pendingContext = context;
		}
		if (f == null || !Object.op_Implicit((Object)(object)f.GameObject))
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

	private unsafe static IEnumerator Gather(CoopSession s, int epoch, bool initial)
	{
		try
		{
			if (initial)
			{
				yield return (object)new WaitForSecondsRealtime(0.25f);
			}
			if (epoch != generation || !SceneReady(s) || Time.unscaledTime - closeAt > 4f)
			{
				yield break;
			}
			if (initial)
			{
				ScanGates(shutting: true);
				HealthManager val = ((Object.op_Implicit((Object)(object)pendingTarget) && pendingTarget.hp > 0) ? pendingTarget : Fight(s, allowOrdinary: true));
				PlayerSlot playerSlot = ((Time.unscaledTime - contactAt < 2f && Valid(lastEntrant)) ? lastEntrant : SourceEntrant(pendingSender, s));
				if (!Valid(playerSlot))
				{
					playerSlot = pendingContext;
				}
				encounterTarget = val;
				if (Object.op_Implicit((Object)(object)val))
				{
					arenaPosition = ((Component)val).transform.position;
					if (Boss(val) && !hasCameraBattleBounds)
					{
						PlayerSlot playerSlot2 = NearestBossPlayer(s, val);
						if (Valid(playerSlot2) && Vector2.Distance(Vector2.op_Implicit(((Component)playerSlot2.Hero).transform.position), Vector2.op_Implicit(arenaPosition)) < 22f)
						{
							combatAnchor = playerSlot2;
						}
						else if (!Valid(combatAnchor) || Vector2.Distance(Vector2.op_Implicit(((Component)combatAnchor.Hero).transform.position), Vector2.op_Implicit(arenaPosition)) > 22f)
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
							Vector3 position = ((Component)player2.Hero).transform.position;
							list.Add(new ArenaPoint(position.x, position.y));
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
						yield break;
					}
					arenaPosition = ((Component)playerSlot.Hero).transform.position;
					combatAnchor = playerSlot;
				}
				zone = ArenaRules.Zone(new ArenaPoint(arenaPosition.x, arenaPosition.y), geometry.ToArray());
				if (hasCameraBattleBounds)
				{
					Bounds val2 = cameraBattleBounds;
					zone = new ArenaZone
					{
						Left = ((Bounds)(ref val2)).min.x - 0.75f,
						Right = ((Bounds)(ref val2)).max.x + 0.75f,
						Bottom = ((Bounds)(ref val2)).min.y - 0.75f,
						Top = ((Bounds)(ref val2)).max.y + 0.75f,
						Bounded = true
					};
				}
				else if (Valid(combatAnchor) && !zone.Contains(((Component)combatAnchor.Hero).transform.position.x, ((Component)combatAnchor.Hero).transform.position.y))
				{
					zone = default(ArenaZone);
				}
				closed = true;
			}
			PlayerSlot anchor = Anchor;
			if (!rosterCaptured)
			{
				combatAnchor = anchor;
				if (Valid(anchor))
				{
					entrancePoint = ((Component)anchor.Hero).transform.position;
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
				string[] obj = new string[12]
				{
					"ARENA entrance fight=",
					Object.op_Implicit((Object)(object)encounterTarget) ? ((Object)encounterTarget).name : "none",
					" gates=",
					geometry.Count.ToString(),
					" anchor=",
					(anchor == null) ? "none" : ("P" + (anchor.Index + 1)),
					" at=",
					((object)((anchor == null) ? Vector3.zero : ((Component)anchor.Hero).transform.position)/*cast due to .constrained prefix*/).ToString(),
					" boss=",
					null,
					null,
					null
				};
				Vector3 val3 = arenaPosition;
				obj[9] = ((object)(*(Vector3*)(&val3))/*cast due to .constrained prefix*/).ToString();
				obj[10] = " bounded=";
				obj[11] = zone.Bounded.ToString();
				Diagnostics.Write(string.Concat(obj));
				foreach (PlayerSlot player4 in s.Players)
				{
					if (Valid(player4))
					{
						Diagnostics.Write("ARENA entry P" + (player4.Index + 1) + " at=" + ((object)((Component)player4.Hero).transform.position/*cast due to .constrained prefix*/).ToString() + " gather=" + pendingPlayers.Contains(player4));
					}
				}
				foreach (Collider2D barrier in barriers)
				{
					if (Object.op_Implicit((Object)(object)barrier))
					{
						Diagnostics.Write("ARENA barrier=" + ((Object)barrier).name + " shape=" + ((object)Shape(barrier)/*cast due to .constrained prefix*/).ToString());
					}
				}
			}
			Dictionary<PlayerSlot, Vector3> arrivals = new Dictionary<PlayerSlot, Vector3>(8);
			int attempt = 0;
			while (true)
			{
				bool flag = false;
				foreach (PlayerSlot pendingPlayer in pendingPlayers)
				{
					if (Valid(pendingPlayer) && !arrivals.ContainsKey(pendingPlayer))
					{
						if (TryDestination(s, pendingPlayer, out var destination))
						{
							arrivals[pendingPlayer] = destination;
						}
						else
						{
							flag = true;
						}
					}
				}
				if (!flag || !initial || attempt++ >= 6 || epoch != generation || !SceneReady(s) || Time.unscaledTime - closeAt > 4f)
				{
					break;
				}
				yield return (object)new WaitForSecondsRealtime(0.1f);
			}
			if (epoch != generation || !SceneReady(s))
			{
				yield break;
			}
			foreach (PlayerSlot pendingPlayer2 in pendingPlayers)
			{
				if (Valid(pendingPlayer2))
				{
					if (!arrivals.TryGetValue(pendingPlayer2, out var value))
					{
						string[] obj2 = new string[8]
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
						Vector3 val3 = entrancePoint;
						obj2[3] = ((object)(*(Vector3*)(&val3))/*cast due to .constrained prefix*/).ToString();
						obj2[4] = " current=";
						obj2[5] = ((object)((Component)pendingPlayer2.Hero).transform.position/*cast due to .constrained prefix*/).ToString();
						obj2[6] = " reason=";
						obj2[7] = landingFailure;
						Diagnostics.Write(string.Concat(obj2));
					}
					else
					{
						Revival.End(pendingPlayer2);
						pendingPlayer2.ArenaTransfer = true;
						pendingPlayer2.ArenaAlpha = 1f;
						pendingPlayer2.ProtectionUntil = Mathf.Max(pendingPlayer2.ProtectionUntil, Time.time + 1f);
						tk2dSprite component = ((Component)pendingPlayer2.Hero).GetComponent<tk2dSprite>();
						moving.Add(new Transfer
						{
							Player = pendingPlayer2,
							Hero = pendingPlayer2.Hero,
							Sprite = component,
							Color = (Object.op_Implicit((Object)(object)component) ? ((tk2dBaseSprite)component).color : Color.white),
							Destination = value
						});
					}
				}
			}
			if (moving.Count == 0)
			{
				yield break;
			}
			for (float t = 0f; t < 0.1f; t += Time.unscaledDeltaTime)
			{
				if (epoch != generation || !SceneReady(s))
				{
					yield break;
				}
				SetAlpha(1f - t / 0.1f);
				yield return null;
			}
			foreach (Transfer item in moving)
			{
				PlayerSlot player = item.Player;
				if (!Valid(player) || (Object)(object)player.Hero != (Object)(object)item.Hero)
				{
					continue;
				}
				Vector3 destination2 = item.Destination;
				Vector3 val3;
				if (!ArenaLanding.Clear(s, player, destination2, out landingFailure))
				{
					string[] obj3 = new string[6]
					{
						"ARENA landing changed P",
						(player.Index + 1).ToString(),
						" at=",
						null,
						null,
						null
					};
					val3 = destination2;
					obj3[3] = ((object)(*(Vector3*)(&val3))/*cast due to .constrained prefix*/).ToString();
					obj3[4] = " reason=";
					obj3[5] = landingFailure;
					Diagnostics.Write(string.Concat(obj3));
					continue;
				}
				destination2.z = (float)CoopRules.PlayerDepth(player.Index);
				Vector3 position2 = ((Component)player.Hero).transform.position;
				NativeDreamFx.TeleportTrail(player, position2, destination2);
				((Component)player.Hero).transform.position = destination2;
				Rigidbody2D component2 = ((Component)player.Hero).GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component2))
				{
					component2.position = Vector2.op_Implicit(destination2);
					component2.velocity = Vector2.zero;
				}
				player.ProtectionUntil = Mathf.Max(player.ProtectionUntil, Time.time + 1.25f);
				player.Hero.cState.invulnerable = true;
				player.SafePoint = destination2;
				player.HasSafePoint = s.IsSafe(destination2, player);
				player.HasPreviousSafePoint = false;
				player.SafeAt = Time.unscaledTime;
				player.Vitals.HazardPoint = destination2;
				s.Commit(player);
				player.FarSince = -1f;
				item.Applied = true;
				string[] obj4 = new string[6]
				{
					"ARENA gathered P",
					(player.Index + 1).ToString(),
					" anchor=",
					(anchor == null) ? "interior" : ("P" + (anchor.Index + 1)),
					" pos=",
					null
				};
				val3 = destination2;
				obj4[5] = ((object)(*(Vector3*)(&val3))/*cast due to .constrained prefix*/).ToString();
				Diagnostics.Write(string.Concat(obj4));
			}
			for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
			{
				if (epoch != generation || !SceneReady(s))
				{
					yield break;
				}
				SetAlpha(t / 0.15f);
				yield return null;
			}
			foreach (Transfer item2 in moving)
			{
				if (item2.Applied && Valid(item2.Player) && Vector2.Distance(Vector2.op_Implicit(((Component)item2.Player.Hero).transform.position), Vector2.op_Implicit(item2.Destination)) < 3f)
				{
					pendingPlayers.Remove(item2.Player);
				}
			}
		}
		finally
		{
			if (epoch == generation)
			{
				Restore();
				busy = (eventPending = false);
				pendingSender = null;
				pendingContext = null;
				nextScan = Time.unscaledTime + 0.4f;
			}
		}
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
			if (Object.op_Implicit((Object)(object)item.Sprite) && !((Object)(object)player.Hero != (Object)(object)item.Hero) && player.Alive)
			{
				Color color = ((tk2dBaseSprite)item.Sprite).color;
				color.a = item.Color.a * player.ArenaAlpha;
				((tk2dBaseSprite)item.Sprite).color = color;
				Rigidbody2D component = ((Component)player.Hero).GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component))
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
			if (Object.op_Implicit((Object)(object)item.Sprite) && (Object)(object)item.Player.Hero == (Object)(object)item.Hero && item.Player.Alive)
			{
				Color color = ((tk2dBaseSprite)item.Sprite).color;
				color.a = item.Color.a;
				((tk2dBaseSprite)item.Sprite).color = color;
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
			if (Object.op_Implicit((Object)(object)gateObject))
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
