using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
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
		PlayMakerFSM[] array = Object.FindObjectsOfType<PlayMakerFSM>();
		foreach (PlayMakerFSM val in array)
		{
			if (!Object.op_Implicit((Object)(object)val) || !((Behaviour)val).enabled || !((Component)val).gameObject.activeInHierarchy || s.Resolve(val) != null || InteractionRouter.IsUi(val.Fsm) || Object.op_Implicit((Object)(object)((Component)val).GetComponentInParent<HealthManager>()))
			{
				continue;
			}
			Fsm fsm = val.Fsm;
			if (fsm == null || fsm.States == null)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			FsmState[] states = fsm.States;
			foreach (FsmState val2 in states)
			{
				if (val2.Actions == null)
				{
					continue;
				}
				FsmStateAction[] actions = val2.Actions;
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
			string text = (val.FsmName + " " + ((Object)val).name).ToLowerInvariant();
			if (!flag || (!flag2 && !text.Contains("door") && !text.Contains("exit")))
			{
				continue;
			}
			List<Collider2D> list = new List<Collider2D>();
			Transform val3 = ((Component)val).transform;
			int num = 0;
			while (Object.op_Implicit((Object)(object)val3) && num < 3 && (num <= 0 || val3.childCount <= 16))
			{
				Collider2D[] componentsInChildren = ((Component)val3).GetComponentsInChildren<Collider2D>(true);
				foreach (Collider2D val4 in componentsInChildren)
				{
					if (!Object.op_Implicit((Object)(object)val4) || !val4.isTrigger || Object.op_Implicit((Object)(object)((Component)val4).GetComponentInParent<HeroController>()))
					{
						continue;
					}
					Bounds bounds = val4.bounds;
					if (((Bounds)(ref bounds)).size.x < 12f)
					{
						bounds = val4.bounds;
						if (((Bounds)(ref bounds)).size.y < 14f && Vector2.Distance(Vector2.op_Implicit(((Component)val4).transform.position), Vector2.op_Implicit(((Component)val).transform.position)) < 8f && !list.Contains(val4))
						{
							list.Add(val4);
						}
					}
				}
				if (list.Count > 0)
				{
					break;
				}
				num++;
				val3 = val3.parent;
			}
			doors.Add(new Door
			{
				Machine = val,
				Colliders = list.ToArray()
			});
			if (reported.Add(((Object)val).GetInstanceID()))
			{
				Diagnostics.Write("DOOR found object=" + ((Object)val).name + " fsm=" + val.FsmName + " colliders=" + list.Count + " destination=" + (Destination(fsm, out var info) ? (info.SceneName + "/" + info.EntryGateName) : "multiple or unset"));
			}
		}
		List<TransitionPoint> transitionPoints = TransitionPoint.TransitionPoints;
		if (transitionPoints == null)
		{
			return;
		}
		foreach (TransitionPoint item in transitionPoints)
		{
			if (Object.op_Implicit((Object)(object)item) && item.isADoor && reported.Add(((Object)item).GetInstanceID()))
			{
				Diagnostics.Write("DOOR gate found object=" + ((Object)item).name + " destination=" + item.targetScene + "/" + item.entryPoint + " position=" + ((object)((Component)item).transform.position/*cast due to .constrained prefix*/).ToString());
			}
		}
	}

	private static bool Near(Door d, PlayerSlot p)
	{
		Vector3 position = ((Component)p.Hero).transform.position;
		Vector3 position2 = ((Component)d.Machine).transform.position;
		Collider2D[] colliders = d.Colliders;
		foreach (Collider2D val in colliders)
		{
			if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).enabled && ((Component)val).gameObject.activeInHierarchy)
			{
				Bounds bounds = val.bounds;
				if (Mathf.Abs(position.x - ((Bounds)(ref bounds)).center.x) <= ((Bounds)(ref bounds)).extents.x + 1.25f && Mathf.Abs(position.y - ((Bounds)(ref bounds)).center.y) <= ((Bounds)(ref bounds)).extents.y + 1.8f)
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
		Vector3 position = ((Component)p.Hero).transform.position;
		Vector3 position2 = ((Component)gate).transform.position;
		Collider2D component = ((Component)gate).GetComponent<Collider2D>();
		if (Object.op_Implicit((Object)(object)component) && ((Behaviour)component).enabled && ((Component)component).gameObject.activeInHierarchy)
		{
			Bounds bounds = component.bounds;
			if (Mathf.Abs(position.x - ((Bounds)(ref bounds)).center.x) <= ((Bounds)(ref bounds)).extents.x + 1.25f && Mathf.Abs(position.y - ((Bounds)(ref bounds)).center.y) <= ((Bounds)(ref bounds)).extents.y + 1.8f)
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
			if (!Object.op_Implicit((Object)(object)door.Machine) || !(Vector2.Distance(Vector2.op_Implicit(((Component)gate).transform.position), Vector2.op_Implicit(((Component)door.Machine).transform.position)) < 3f))
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
			foreach (FsmStateAction val in actions)
			{
				if (val != null && val.Enabled && val is ListenForUp)
				{
					return true;
				}
			}
		}
		if (activeState.Transitions != null)
		{
			FsmTransition[] transitions = activeState.Transitions;
			foreach (FsmTransition val2 in transitions)
			{
				if (val2 != null && (val2.EventName == "UP" || val2.EventName == "ENTER" || val2.EventName == "DOOR ENTER"))
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
		TMP_Text[] array = Object.FindObjectsOfType<TMP_Text>();
		foreach (TMP_Text val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && ((Behaviour)val).isActiveAndEnabled && EntryWord(val.text))
			{
				return true;
			}
		}
		tk2dTextMesh[] array2 = Object.FindObjectsOfType<tk2dTextMesh>();
		foreach (tk2dTextMesh val2 in array2)
		{
			if (Object.op_Implicit((Object)(object)val2) && ((Behaviour)val2).isActiveAndEnabled && EntryWord(val2.text))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Destination(Fsm f, out SceneLoadInfo info)
	{
		info = null;
		FsmState[] states = f.States;
		foreach (FsmState val in states)
		{
			if (val.Actions == null)
			{
				continue;
			}
			FsmStateAction[] actions = val.Actions;
			foreach (FsmStateAction obj in actions)
			{
				BeginSceneTransition val2 = (BeginSceneTransition)(object)((obj is BeginSceneTransition) ? obj : null);
				if (val2 == null || !((FsmStateAction)val2).Enabled || val2.sceneName == null || val2.entryGateName == null)
				{
					continue;
				}
				string value = val2.sceneName.Value;
				string value2 = val2.entryGateName.Value;
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
				info = new SceneLoadInfo
				{
					SceneName = value,
					EntryGateName = value2,
					HeroLeaveDirection = (GatePosition)4,
					EntryDelay = ((val2.entryDelay == null) ? 0f : val2.entryDelay.Value),
					WaitForSceneTransitionCameraFade = true,
					PreventCameraFadeOut = false,
					Visualization = (SceneLoadVisualizations)((val2.visualization != null && val2.visualization.Value is SceneLoadVisualizations) ? ((int)(SceneLoadVisualizations)(object)val2.visualization.Value) : 0)
				};
			}
		}
		return info != null;
	}

	internal static bool Tick(CoopSession s)
	{
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
		bool result;
		foreach (PlayerSlot player in s.Players)
		{
			bool flag = player.Actions != null && ((OneAxisInputControl)player.Actions.up).IsPressed;
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
				continue;
			}
			if (scanAt - Time.unscaledTime > 2.05f || !player.Alive || !player.Ready || !player.Connected || player.InputBlocked || EmergencyWarp.Active(player) || BenchSeats.Seated(player))
			{
				continue;
			}
			Door door = null;
			SceneLoadInfo val = null;
			float num2 = float.MaxValue;
			bool flag2 = EntryPromptVisible();
			List<TransitionPoint> transitionPoints = TransitionPoint.TransitionPoints;
			TransitionPoint val2 = null;
			float num3 = float.MaxValue;
			if (transitionPoints != null)
			{
				foreach (TransitionPoint item in transitionPoints)
				{
					if (!Object.op_Implicit((Object)(object)item) || !((Behaviour)item).enabled || !item.isADoor || !((Component)item).gameObject.activeInHierarchy || string.IsNullOrEmpty(item.targetScene) || string.IsNullOrEmpty(item.entryPoint) || !Near(item, player) || LockedNearby(item))
					{
						continue;
					}
					if (!flag2 || !PrerequisiteMet(item.targetScene, s))
					{
						string[] obj = new string[8]
						{
							"DOOR gate waiting for native unlock P",
							(player.Index + 1).ToString(),
							" gate=",
							((Object)item).name,
							" entryPrompt=",
							flag2.ToString(),
							" prerequisite=",
							null
						};
						result = PrerequisiteMet(item.targetScene, s);
						obj[7] = result.ToString();
						Diagnostics.Write(string.Concat(obj));
					}
					else
					{
						float num4 = Vector2.Distance(Vector2.op_Implicit(((Component)player.Hero).transform.position), Vector2.op_Implicit(((Component)item).transform.position));
						if (num4 < num3)
						{
							num3 = num4;
							val2 = item;
						}
					}
				}
			}
			if ((Object)(object)val2 != (Object)null)
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
						result = true;
					}
					else
					{
						Diagnostics.Write("DOOR gate enter P" + (player.Index + 1) + " gate=" + ((Object)val2).name + " to=" + val2.targetScene + "/" + val2.entryPoint);
						using (PlayerContext.Enter(player))
						{
							instance.BeginSceneTransition(new SceneLoadInfo
							{
								SceneName = val2.targetScene,
								EntryGateName = val2.entryPoint,
								HeroLeaveDirection = val2.GetGatePosition(),
								EntryDelay = val2.entryDelay,
								WaitForSceneTransitionCameraFade = true,
								Visualization = val2.sceneLoadVisualization,
								forceWaitFetch = val2.forceWaitFetch
							});
						}
						result = true;
					}
					goto IL_0a73;
				}
				Diagnostics.Write("DOOR gate rejected P" + (player.Index + 1) + " gate=" + ((Object)val2).name + " grounded=" + player.Hero.cState.onGround + " relinquished=" + player.Hero.controlReqlinquished);
			}
			foreach (Door door2 in doors)
			{
				if (!Object.op_Implicit((Object)(object)door2.Machine) || !((Behaviour)door2.Machine).enabled || !((Component)door2.Machine).gameObject.activeInHierarchy || !Near(door2, player))
				{
					continue;
				}
				bool flag4 = Ready(door2.Machine.Fsm);
				SceneLoadInfo info;
				bool flag5 = Destination(door2.Machine.Fsm, out info);
				Diagnostics.Write("DOOR request P" + (player.Index + 1) + " object=" + ((Object)door2.Machine).name + " fsm=" + door2.Machine.FsmName + " state=" + ((door2.Machine.Fsm.ActiveState == null) ? "none" : door2.Machine.Fsm.ActiveState.Name) + " ready=" + flag4 + " visiblePrompt=" + flag2 + " destination=" + (flag5 ? (info.SceneName + "/" + info.EntryGateName) : "unresolved"));
				if (!flag4 && flag2 && door2.Machine.Fsm.ActiveState != null)
				{
					string text = door2.Machine.Fsm.ActiveState.Name.ToLowerInvariant();
					flag4 = !text.Contains("lock") && !text.Contains("bloque") && !text.Contains("closed");
				}
				if (flag4 && flag5 && flag2 && PrerequisiteMet(info.SceneName, s))
				{
					float num5 = Vector2.Distance(Vector2.op_Implicit(((Component)player.Hero).transform.position), Vector2.op_Implicit(((Component)door2.Machine).transform.position));
					if (num5 < num2)
					{
						num2 = num5;
						door = door2;
						val = info;
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
				if (!flag6)
				{
					Diagnostics.Write("DOOR rejected actor P" + (player.Index + 1) + " grounded=" + player.Hero.cState.onGround + " relinquished=" + player.Hero.controlReqlinquished);
					continue;
				}
				if (PvpMatch.BlockExit(player))
				{
					result = true;
				}
				else
				{
					Diagnostics.Write("DOOR enter P" + (player.Index + 1) + " object=" + ((Object)door.Machine).name + " to=" + val.SceneName + " gate=" + val.EntryGateName);
					using (PlayerContext.Enter(player))
					{
						instance.BeginSceneTransition(val);
					}
					result = true;
				}
				goto IL_0a73;
			}
			if (!((Object)(object)val2 == (Object)null) || door != null)
			{
				continue;
			}
			TransitionPoint val3 = null;
			float num6 = float.MaxValue;
			if (transitionPoints != null)
			{
				foreach (TransitionPoint item2 in transitionPoints)
				{
					if (Object.op_Implicit((Object)(object)item2) && item2.isADoor)
					{
						float num7 = Vector2.Distance(Vector2.op_Implicit(((Component)player.Hero).transform.position), Vector2.op_Implicit(((Component)item2).transform.position));
						if (num7 < num6)
						{
							num6 = num7;
							val3 = item2;
						}
					}
				}
			}
			Diagnostics.Write("DOOR no route P" + (player.Index + 1) + " pos=" + ((object)((Component)player.Hero).transform.position/*cast due to .constrained prefix*/).ToString() + " gates=" + (transitionPoints?.Count ?? 0) + " controls=" + doors.Count + " prompt=" + flag2 + " nearest=" + (Object.op_Implicit((Object)(object)val3) ? (((Object)val3).name + " " + val3.targetScene + "/" + val3.entryPoint + " dist=" + num6 + " enabled=" + ((Behaviour)val3).enabled) : "none"));
		}
		return false;
		IL_0a73:
		return result;
	}

	internal static void Reset()
	{
		doors.Clear();
		reported.Clear();
		Array.Clear(held, 0, held.Length);
		scanAt = 0f;
	}
}
