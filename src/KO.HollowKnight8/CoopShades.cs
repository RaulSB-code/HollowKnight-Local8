using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class CoopShades
{
	private static readonly LocalShade[] shades = new LocalShade[8];

	private static readonly PlayerSlot[] pending = new PlayerSlot[8];

	private static readonly float[] retryAt = new float[8];

	private static readonly float[] expires = new float[8];

	internal unsafe static void Spawn(CoopSession session, PlayerSlot owner)
	{
		Local8Runtime self = Plugin.Self;
		if (!Object.op_Implicit((Object)(object)self) || !self.SpawnShades.Value || !session.Active || session.TeamWipe || !Object.op_Implicit((Object)(object)owner.Hero))
		{
			return;
		}
		GameManager instance = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.sm) || !Object.op_Implicit((Object)(object)instance.sm.hollowShadeObject))
		{
			Queue(owner, "scene prefab missing");
			return;
		}
		Vector3 origin = (owner.HasSafePoint ? owner.SafePoint : ((Component)owner.Hero).transform.position);
		if (!session.FindSafePosition(origin, owner, out var result) && !session.FindSafePosition(owner.Vitals.HazardPoint, owner, out result))
		{
			Queue(owner, "waiting for safe floor");
			return;
		}
		GameObject val = null;
		GameObject val2 = null;
		try
		{
			Remove(owner.Index);
			val = new GameObject("Local8 shade staging");
			val.SetActive(false);
			val2 = Object.Instantiate<GameObject>(instance.sm.hollowShadeObject, val.transform, false);
			val2.SetActive(false);
			((Object)val2).name = "Local8 Sombra P" + (owner.Index + 1);
			Vector3 val3 = result + Vector3.up * 0.6f;
			val3.z = 0.006f;
			val2.transform.position = val3;
			LocalShade localShade = val2.AddComponent<LocalShade>();
			localShade.OwnerIndex = owner.Index;
			localShade.Health = RecoveryRules.ShadeHealth(session.Data.nailDamage, owner.CurrentMaxHealth);
			localShade.Soul = Math.Max(0, owner.Vitals.Soul);
			Scene scene = ((Component)instance.sm).gameObject.scene;
			localShade.Scene = ((Scene)(ref scene)).name;
			localShade.Zone = instance.GetCurrentMapZone();
			localShade.Fireball = session.Data.fireballLevel;
			localShade.Quake = session.Data.quakeLevel;
			localShade.Scream = session.Data.screamLevel;
			MonoBehaviour[] componentsInChildren = val2.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (MonoBehaviour val4 in componentsInChildren)
			{
				if (Object.op_Implicit((Object)(object)val4) && ((object)val4).GetType().Name.StartsWith("Persistent", StringComparison.Ordinal))
				{
					Object.DestroyImmediate((Object)(object)val4);
				}
			}
			int num = 0;
			bool flag = false;
			PlayMakerFSM[] componentsInChildren2 = val2.GetComponentsInChildren<PlayMakerFSM>(true);
			foreach (PlayMakerFSM val5 in componentsInChildren2)
			{
				val5.Fsm.InitData();
				if (val5.FsmName == "Shade Control")
				{
					flag = true;
				}
				FsmState[] fsmStates = val5.FsmStates;
				foreach (FsmState val6 in fsmStates)
				{
					if (val5.FsmName == "Shade Control" && val6.Name == "Killed")
					{
						val6.Actions = (FsmStateAction[])(object)new FsmStateAction[1]
						{
							new ShadeDefeatAction
							{
								Shade = localShade
							}
						};
						val6.Transitions = (FsmTransition[])(object)new FsmTransition[0];
						num++;
						continue;
					}
					List<FsmStateAction> list = new List<FsmStateAction>();
					FsmStateAction[] actions = val6.Actions;
					foreach (FsmStateAction val7 in actions)
					{
						if (val7 != null)
						{
							FsmStateAction val8 = LocalRead(val7, localShade);
							if (val8 != null)
							{
								list.Add(val8);
								num++;
							}
							else if (ShadeRules.IsSaveWrite(((object)val7).GetType().Name))
							{
								num++;
							}
							else
							{
								list.Add(val7);
							}
						}
					}
					val6.Actions = list.ToArray();
				}
			}
			if (!flag)
			{
				throw new InvalidOperationException("Shade Control FSM missing");
			}
			HealthManager component = val2.GetComponent<HealthManager>();
			if (!Object.op_Implicit((Object)(object)component))
			{
				throw new InvalidOperationException("Shade HealthManager missing");
			}
			component.hp = localShade.Health;
			component.SetGeoSmall(0);
			component.SetGeoMedium(0);
			component.SetGeoLarge(0);
			shades[owner.Index] = localShade;
			val2.transform.SetParent((Transform)null, true);
			val2.SetActive(true);
			component.hp = localShade.Health;
			string[] obj = new string[8]
			{
				"SHADE spawned P",
				(owner.Index + 1).ToString(),
				" hp=",
				localShade.Health.ToString(),
				" pos=",
				null,
				null,
				null
			};
			Vector3 val9 = val3;
			obj[5] = ((object)(*(Vector3*)(&val9))/*cast due to .constrained prefix*/).ToString();
			obj[6] = " local_actions=";
			obj[7] = num.ToString();
			Diagnostics.Write(string.Concat(obj));
		}
		catch (Exception ex)
		{
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.SetActive(false);
				Object.Destroy((Object)(object)val2);
			}
			Diagnostics.Throttled("SHADE SPAWN", ex);
			self.Notice("No se pudo crear la Sombra. El resto del mod sigue activo; envia el log.");
		}
		finally
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				Object.Destroy((Object)(object)val);
			}
		}
	}

	private static FsmStateAction LocalRead(FsmStateAction action, LocalShade shade)
	{
		GetPlayerDataInt val = (GetPlayerDataInt)(object)((action is GetPlayerDataInt) ? action : null);
		if (val != null && val.intName != null)
		{
			int value;
			switch (val.intName.Value)
			{
			case "shadeHealth":
				value = shade.Health;
				break;
			case "shadeMP":
				value = shade.Soul;
				break;
			case "shadeFireballLevel":
				value = shade.Fireball;
				break;
			case "shadeQuakeLevel":
				value = shade.Quake;
				break;
			case "shadeScreamLevel":
				value = shade.Scream;
				break;
			case "geoPool":
			case "shadeSpecialType":
				value = 0;
				break;
			default:
				return null;
			}
			return (FsmStateAction)(object)new ShadeIntAction
			{
				Target = val.storeValue,
				Value = value
			};
		}
		GetPlayerDataBool val2 = (GetPlayerDataBool)(object)((action is GetPlayerDataBool) ? action : null);
		if (val2 != null && val2.boolName != null && val2.boolName.Value == "soulLimited")
		{
			return (FsmStateAction)(object)new ShadeBoolAction
			{
				Target = val2.storeValue,
				Value = true
			};
		}
		GetPlayerDataString val3 = (GetPlayerDataString)(object)((action is GetPlayerDataString) ? action : null);
		if (val3 != null && val3.stringName != null)
		{
			if (val3.stringName.Value == "shadeScene")
			{
				return (FsmStateAction)(object)new ShadeStringAction
				{
					Target = val3.storeValue,
					Value = shade.Scene
				};
			}
			if (val3.stringName.Value == "shadeMapZone")
			{
				return (FsmStateAction)(object)new ShadeStringAction
				{
					Target = val3.storeValue,
					Value = shade.Zone
				};
			}
		}
		return null;
	}

	internal static bool HandleDeath(HealthManager health)
	{
		LocalShade localShade = (Object.op_Implicit((Object)(object)health) ? ((Component)health).GetComponent<LocalShade>() : null);
		if (!Object.op_Implicit((Object)(object)localShade))
		{
			return false;
		}
		localShade.Defeat();
		return true;
	}

	internal static bool IsSpawning(GameObject source)
	{
		LocalShade localShade = (Object.op_Implicit((Object)(object)source) ? source.GetComponentInParent<LocalShade>() : null);
		if (Object.op_Implicit((Object)(object)localShade))
		{
			return Time.time < localShade.SafeUntil;
		}
		return false;
	}

	private static void Queue(PlayerSlot owner, string reason)
	{
		int index = owner.Index;
		if (pending[index] == null)
		{
			expires[index] = Time.unscaledTime + 5f;
			Diagnostics.Write("SHADE delayed P" + (index + 1) + ": " + reason);
		}
		pending[index] = owner;
		retryAt[index] = Time.unscaledTime + 0.25f;
	}

	internal static void Tick()
	{
		Local8Runtime self = Plugin.Self;
		if (!Object.op_Implicit((Object)(object)self) || !self.SpawnShades.Value || !self.Enabled.Value || self.Session == null || !self.Session.Active)
		{
			Reset();
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			if (pending[i] != null && Time.unscaledTime >= retryAt[i])
			{
				if (Time.unscaledTime > expires[i] || self.Session.TeamWipe || pending[i].Retiring)
				{
					Diagnostics.Write("SHADE cancelled P" + (i + 1) + ": no valid spawn before timeout");
					pending[i] = null;
				}
				else
				{
					Spawn(self.Session, pending[i]);
				}
			}
		}
	}

	internal static void Remove(int index)
	{
		if (index >= 0 && index < shades.Length)
		{
			pending[index] = null;
			LocalShade localShade = shades[index];
			shades[index] = null;
			if (Object.op_Implicit((Object)(object)localShade))
			{
				((Component)localShade).gameObject.SetActive(false);
				Object.Destroy((Object)(object)((Component)localShade).gameObject);
			}
		}
	}

	internal static void Reset()
	{
		for (int i = 0; i < shades.Length; i++)
		{
			Remove(i);
		}
	}
}
