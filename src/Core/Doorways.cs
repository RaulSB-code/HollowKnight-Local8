using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using TMPro;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Doorways
{
	private sealed class Door
	{
		internal PlayMakerFSM Machine;

		internal Collider2D[] Colliders;
	}

	private static readonly List<Door> doors = new List<Door>();

	private static readonly bool[] held = new bool[8];

	internal static float scanAt;

	private static readonly HashSet<int> reported = new HashSet<int>();

	private static void Scan(CoopSession s)
	{
		doors.Clear();
		scanAt = Time.unscaledTime + 2f;
		PlayMakerFSM[] array = UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>();
		foreach (PlayMakerFSM playMakerFSM in array)
		{
			if (!playMakerFSM || !playMakerFSM.enabled || !playMakerFSM.gameObject.activeInHierarchy || s.Resolve(playMakerFSM) != null || InteractionRouter.IsUi(playMakerFSM.Fsm) || (bool)playMakerFSM.GetComponentInParent<HealthManager>())
			{
				continue;
			}
			Fsm fsm = playMakerFSM.Fsm;
			if (fsm == null || fsm.States == null)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			FsmState[] states = fsm.States;
			foreach (FsmState fsmState in states)
			{
				if (fsmState.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = fsmState.Actions;
				foreach (FsmStateAction obj in actions)
				{
					if (obj is BeginSceneTransition)
					{
						flag = true;
					}
					if (obj is ListenForUp)
					{
						flag2 = true;
					}
				}
			}
			string text = (playMakerFSM.FsmName + " " + playMakerFSM.name).ToLowerInvariant();
			if (!flag || (!flag2 && !text.Contains("door") && !text.Contains("exit")))
			{
				continue;
			}
			List<Collider2D> list = new List<Collider2D>();
			Transform transform = playMakerFSM.transform;
			int num = 0;
			while ((bool)transform && num < 3 && (num <= 0 || transform.childCount <= 16))
			{
				Collider2D[] componentsInChildren = transform.GetComponentsInChildren<Collider2D>(includeInactive: true);
				foreach (Collider2D collider2D in componentsInChildren)
				{
					if ((bool)collider2D && collider2D.isTrigger && !collider2D.GetComponentInParent<HeroController>() && collider2D.bounds.size.x < 12f && collider2D.bounds.size.y < 14f && Vector2.Distance(collider2D.transform.position, playMakerFSM.transform.position) < 8f && !list.Contains(collider2D))
					{
						list.Add(collider2D);
					}
				}
				if (list.Count > 0)
				{
					break;
				}
				num++;
				transform = transform.parent;
			}
			doors.Add(new Door
			{
				Machine = playMakerFSM,
				Colliders = list.ToArray()
			});
			if (reported.Add(playMakerFSM.GetInstanceID()))
			{
				Diagnostics.Write("DOOR found object=" + playMakerFSM.name + " fsm=" + playMakerFSM.FsmName + " colliders=" + list.Count + " destination=" + (Destination(fsm, out var info) ? (info.SceneName + "/" + info.EntryGateName) : "multiple or unset"));
			}
		}
		List<TransitionPoint> transitionPoints = TransitionPoint.TransitionPoints;
		if (transitionPoints == null)
		{
			return;
		}
		foreach (TransitionPoint item in transitionPoints)
		{
			if ((bool)item && item.isADoor && reported.Add(item.GetInstanceID()))
			{
				Diagnostics.Write("DOOR gate found object=" + item.name + " destination=" + item.targetScene + "/" + item.entryPoint + " position=" + item.transform.position.ToString());
			}
		}
	}

	private static bool Near(Door d, PlayerSlot p)
	{
		Vector3 position = p.Hero.transform.position;
		Vector3 position2 = d.Machine.transform.position;
		Collider2D[] colliders = d.Colliders;
		foreach (Collider2D collider2D in colliders)
		{
			if ((bool)collider2D && collider2D.enabled && collider2D.gameObject.activeInHierarchy)
			{
				UnityEngine.Bounds bounds = collider2D.bounds;
				if (Mathf.Abs(position.x - bounds.center.x) <= bounds.extents.x + 1.25f && Mathf.Abs(position.y - bounds.center.y) <= bounds.extents.y + 1.8f)
				{
					return true;
				}
			}
		}
		if (Mathf.Abs(position.x - position2.x) < 3.2f)
		{
			return Mathf.Abs(position.y - position2.y) < 4.2f;
		}
		return false;
	}

	private static bool Near(TransitionPoint gate, PlayerSlot p)
	{
		Vector3 position = p.Hero.transform.position;
		Vector3 position2 = gate.transform.position;
		Collider2D component = gate.GetComponent<Collider2D>();
		if ((bool)component && component.enabled && component.gameObject.activeInHierarchy)
		{
			UnityEngine.Bounds bounds = component.bounds;
			if (Mathf.Abs(position.x - bounds.center.x) <= bounds.extents.x + 1.25f && Mathf.Abs(position.y - bounds.center.y) <= bounds.extents.y + 1.8f)
			{
				return true;
			}
		}
		if (Mathf.Abs(position.x - position2.x) < 3.2f)
		{
			return Mathf.Abs(position.y - position2.y) < 4.2f;
		}
		return false;
	}

	private static bool LockedNearby(TransitionPoint gate)
	{
		foreach (Door door in doors)
		{
			if (!door.Machine || !(Vector2.Distance(gate.transform.position, door.Machine.transform.position) < 3f))
			{
				continue;
			}
			FsmState activeState = door.Machine.Fsm.ActiveState;
			if (activeState != null)
			{
				string text = activeState.Name.ToLowerInvariant();
				if (text.Contains("lock") || text.Contains("bloque") || text.Contains("closed"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool PrerequisiteMet(string scene, CoopSession s)
	{
		if (!(scene != "Room_Ouiji"))
		{
			return s.Data.jijiDoorUnlocked;
		}
		return true;
	}

	private static bool Ready(Fsm f)
	{
		FsmState activeState = f.ActiveState;
		if (activeState == null)
		{
			return false;
		}
		if (activeState.Actions != null)
		{
			FsmStateAction[] actions = activeState.Actions;
			foreach (FsmStateAction fsmStateAction in actions)
			{
				if (fsmStateAction != null && fsmStateAction.Enabled && fsmStateAction is ListenForUp)
				{
					return true;
				}
			}
		}
		if (activeState.Transitions != null)
		{
			FsmTransition[] transitions = activeState.Transitions;
			foreach (FsmTransition fsmTransition in transitions)
			{
				if (fsmTransition != null && (fsmTransition.EventName == "UP" || fsmTransition.EventName == "ENTER" || fsmTransition.EventName == "DOOR ENTER"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool EntryWord(string word)
	{
		if (string.IsNullOrEmpty(word))
		{
			return false;
		}
		word = word.Trim().ToUpperInvariant();
		if (!word.Contains("ENTRAR") && !word.Contains("ENTER") && !word.Contains("SUBIR") && !word.Contains("ASCEND") && !word.Contains("BAJAR") && !word.Contains("DESCEND") && !word.Contains("SALIR"))
		{
			return word == "EXIT";
		}
		return true;
	}

	private static bool EntryPromptVisible()
	{
		TMP_Text[] array = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text tMP_Text in array)
		{
			if ((bool)tMP_Text && tMP_Text.isActiveAndEnabled && EntryWord(tMP_Text.text))
			{
				return true;
			}
		}
		tk2dTextMesh[] array2 = UnityEngine.Object.FindObjectsOfType<tk2dTextMesh>();
		foreach (tk2dTextMesh tk2dTextMesh in array2)
		{
			if ((bool)tk2dTextMesh && tk2dTextMesh.isActiveAndEnabled && EntryWord(tk2dTextMesh.text))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Destination(Fsm f, out GameManager.SceneLoadInfo info)
	{
		info = null;
		FsmState[] states = f.States;
		foreach (FsmState fsmState in states)
		{
			if (fsmState.Actions == null)
			{
				continue;
			}
			FsmStateAction[] actions = fsmState.Actions;
			for (int j = 0; j < actions.Length; j++)
			{
				if (!(actions[j] is BeginSceneTransition beginSceneTransition) || !beginSceneTransition.Enabled || beginSceneTransition.sceneName == null || beginSceneTransition.entryGateName == null)
				{
					continue;
				}
				string value = beginSceneTransition.sceneName.Value;
				string value2 = beginSceneTransition.entryGateName.Value;
				if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(value2))
				{
					continue;
				}
				if (info != null)
				{
					if (info.SceneName != value || info.EntryGateName != value2)
					{
						info = null;
						return false;
					}
					continue;
				}
				info = new GameManager.SceneLoadInfo
				{
					SceneName = value,
					EntryGateName = value2,
					HeroLeaveDirection = GatePosition.door,
					EntryDelay = ((beginSceneTransition.entryDelay == null) ? 0f : beginSceneTransition.entryDelay.Value),
					WaitForSceneTransitionCameraFade = true,
					PreventCameraFadeOut = false,
					Visualization = ((beginSceneTransition.visualization != null && beginSceneTransition.visualization.Value is GameManager.SceneLoadVisualizations) ? ((GameManager.SceneLoadVisualizations)(object)beginSceneTransition.visualization.Value) : GameManager.SceneLoadVisualizations.Default)
				};
			}
		}
		return info != null;
	}

	internal static bool Tick(CoopSession s)
	{
		//Discarded unreachable code: IL_0147, IL_0168
		GameManager instance = GameManager.instance;
		if (s == null || !s.Active || !s.Gameplay || instance.isPaused || Plugin.Self.Panel || Charms.NativeMenuOpen)
		{
			Array.Clear(held, 0, held.Length);
			return false;
		}
		if (Time.unscaledTime >= scanAt)
		{
			Scan(s);
		}
		foreach (PlayerSlot player in s.Players)
		{
			bool flag = player.Actions != null && player.Actions.up.IsPressed;
			bool num = flag && !held[player.Index];
			held[player.Index] = flag;
			if (!num || InteractionRouter.ActivePlayer != null)
			{
				continue;
			}
			if (ShopMenuRouting.Buyer != null)
			{
				if (GameManager.instance.GetSceneNameString().Length >= 9 && GameManager.instance.GetSceneNameString()[5] == 'T' && GameManager.instance.GetSceneNameString()[6] == 'r' && GameManager.instance.GetSceneNameString()[7] == 'a' && GameManager.instance.GetSceneNameString()[8] == 'm')
				{
					scanAt = Time.unscaledTime + 12f;
				}
			}
			else
			{
				if (scanAt - Time.unscaledTime > 2.05f || !player.Alive || !player.Ready || !player.Connected || player.InputBlocked || EmergencyWarp.Active(player) || BenchSeats.Seated(player))
				{
					continue;
				}
				Door door = null;
				GameManager.SceneLoadInfo sceneLoadInfo = null;
				float num2 = float.MaxValue;
				bool flag2 = EntryPromptVisible();
				List<TransitionPoint> transitionPoints = TransitionPoint.TransitionPoints;
				TransitionPoint transitionPoint = null;
				float num3 = float.MaxValue;
				if (transitionPoints != null)
				{
					foreach (TransitionPoint item in transitionPoints)
					{
						if (!item || !item.enabled || !item.isADoor || !item.gameObject.activeInHierarchy || string.IsNullOrEmpty(DoorRouteFix.GateScene(item)) || string.IsNullOrEmpty(DoorRouteFix.GateEntry(item)) || !Near(item, player) || LockedNearby(item))
						{
							continue;
						}
						if (!flag2 || !PrerequisiteMet(DoorRouteFix.GateScene(item), s))
						{
							Diagnostics.Write("DOOR gate waiting for native unlock P" + (player.Index + 1) + " gate=" + item.name + " entryPrompt=" + flag2 + " prerequisite=" + PrerequisiteMet(DoorRouteFix.GateScene(item), s));
						}
						else
						{
							float num4 = Vector2.Distance(player.Hero.transform.position, item.transform.position);
							if (num4 < num3)
							{
								num3 = num4;
								transitionPoint = item;
							}
						}
					}
				}
				if (transitionPoint != null)
				{
					bool flag3;
					using (PlayerContext.Enter(player))
					{
						flag3 = player.Hero.CanInteract();
					}
					if (flag3)
					{
						if (PvpMatch.BlockExit(player))
						{
							return true;
						}
						Diagnostics.Write("DOOR gate enter P" + (player.Index + 1) + " gate=" + transitionPoint.name + " to=" + DoorRouteFix.GateScene(transitionPoint) + "/" + DoorRouteFix.GateEntry(transitionPoint));
						using (PlayerContext.Enter(player))
						{
							InteractionMotion.Door(instance, new GameManager.SceneLoadInfo
							{
								SceneName = DoorRouteFix.GateScene(transitionPoint),
								EntryGateName = DoorRouteFix.GateEntry(transitionPoint),
								HeroLeaveDirection = transitionPoint.GetGatePosition(),
								EntryDelay = transitionPoint.entryDelay,
								WaitForSceneTransitionCameraFade = true,
								Visualization = transitionPoint.sceneLoadVisualization,
								forceWaitFetch = transitionPoint.forceWaitFetch
							});
						}
						return true;
					}
					Diagnostics.Write("DOOR gate rejected P" + (player.Index + 1) + " gate=" + transitionPoint.name + " grounded=" + player.Hero.cState.onGround + " relinquished=" + player.Hero.controlReqlinquished);
				}
				foreach (Door door2 in doors)
				{
					if (!door2.Machine || !door2.Machine.enabled || !door2.Machine.gameObject.activeInHierarchy || !Near(door2, player))
					{
						continue;
					}
					bool flag4 = Ready(door2.Machine.Fsm);
					GameManager.SceneLoadInfo info;
					bool flag5 = Destination(door2.Machine.Fsm, out info);
					Diagnostics.Write("DOOR request P" + (player.Index + 1) + " object=" + door2.Machine.name + " fsm=" + door2.Machine.FsmName + " state=" + ((door2.Machine.Fsm.ActiveState == null) ? "none" : door2.Machine.Fsm.ActiveState.Name) + " ready=" + flag4 + " visiblePrompt=" + flag2 + " destination=" + (flag5 ? (info.SceneName + "/" + info.EntryGateName) : "unresolved"));
					if (!flag4 && flag2 && door2.Machine.Fsm.ActiveState != null)
					{
						string text = door2.Machine.Fsm.ActiveState.Name.ToLowerInvariant();
						flag4 = !text.Contains("lock") && !text.Contains("bloque") && !text.Contains("closed");
					}
					if (flag4 && flag5 && flag2 && PrerequisiteMet(info.SceneName, s))
					{
						float num5 = Vector2.Distance(player.Hero.transform.position, door2.Machine.transform.position);
						if (num5 < num2)
						{
							num2 = num5;
							door = door2;
							sceneLoadInfo = info;
						}
					}
				}
				if (door != null)
				{
					bool flag6;
					using (PlayerContext.Enter(player))
					{
						flag6 = player.Hero.CanInteract();
					}
					if (flag6)
					{
						if (PvpMatch.BlockExit(player))
						{
							return true;
						}
						Diagnostics.Write("DOOR enter P" + (player.Index + 1) + " object=" + door.Machine.name + " to=" + sceneLoadInfo.SceneName + " gate=" + sceneLoadInfo.EntryGateName);
						using (PlayerContext.Enter(player))
						{
							InteractionMotion.Door(instance, sceneLoadInfo);
						}
						return true;
					}
					Diagnostics.Write("DOOR rejected actor P" + (player.Index + 1) + " grounded=" + player.Hero.cState.onGround + " relinquished=" + player.Hero.controlReqlinquished);
				}
				else
				{
					if (!(transitionPoint == null) || door != null)
					{
						continue;
					}
					TransitionPoint transitionPoint2 = null;
					float num6 = float.MaxValue;
					if (transitionPoints != null)
					{
						foreach (TransitionPoint item2 in transitionPoints)
						{
							if ((bool)item2 && item2.isADoor)
							{
								float num7 = Vector2.Distance(player.Hero.transform.position, item2.transform.position);
								if (num7 < num6)
								{
									num6 = num7;
									transitionPoint2 = item2;
								}
							}
						}
					}
					Diagnostics.Write("DOOR no route P" + (player.Index + 1) + " pos=" + player.Hero.transform.position.ToString() + " gates=" + (transitionPoints?.Count ?? 0) + " controls=" + doors.Count + " prompt=" + flag2 + " nearest=" + (transitionPoint2 ? (transitionPoint2.name + " " + DoorRouteFix.GateScene(transitionPoint2) + "/" + DoorRouteFix.GateEntry(transitionPoint2) + " dist=" + num6 + " enabled=" + transitionPoint2.enabled) : "none"));
				}
			}
		}
		return false;
	}

	internal static void Reset()
	{
		doors.Clear();
		reported.Clear();
		Array.Clear(held, 0, held.Length);
		scanAt = 0f;
		InteractionMotion.ResetDoors();
	}
}
