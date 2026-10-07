using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CoopShades
{
	private static readonly LocalShade[] shades = new LocalShade[8];

	private static readonly PlayerSlot[] pending = new PlayerSlot[8];

	private static readonly float[] retryAt = new float[8];

	private static readonly float[] expires = new float[8];

	internal static void Spawn(CoopSession session, PlayerSlot owner)
	{
		Local8Runtime self = Plugin.Self;
		if (!self || !self.SpawnShades.Value || !session.Active || session.TeamWipe || !owner.Hero)
		{
			return;
		}
		GameManager instance = GameManager.instance;
		if (!instance || !instance.sm || !instance.sm.hollowShadeObject)
		{
			Queue(owner, "scene prefab missing");
			return;
		}
		Vector3 origin = (owner.HasSafePoint ? owner.SafePoint : owner.Hero.transform.position);
		if (!session.FindSafePosition(origin, owner, out var result) && !session.FindSafePosition(owner.Vitals.HazardPoint, owner, out result))
		{
			Queue(owner, "waiting for safe floor");
			return;
		}
		GameObject gameObject = null;
		GameObject gameObject2 = null;
		try
		{
			Remove(owner.Index);
			gameObject = new GameObject("Local8 shade staging");
			gameObject.SetActive(value: false);
			gameObject2 = UnityEngine.Object.Instantiate(instance.sm.hollowShadeObject, gameObject.transform, worldPositionStays: false);
			gameObject2.SetActive(value: false);
			gameObject2.name = "Local8 Sombra P" + (owner.Index + 1);
			Vector3 vector = result + Vector3.up * 0.6f;
			vector.z = 0.006f;
			gameObject2.transform.position = vector;
			LocalShade localShade = gameObject2.AddComponent<LocalShade>();
			localShade.OwnerIndex = owner.Index;
			localShade.Health = RecoveryRules.ShadeHealth(session.Data.nailDamage, owner.CurrentMaxHealth);
			localShade.Soul = Math.Max(0, owner.Vitals.Soul);
			localShade.Scene = instance.sm.gameObject.scene.name;
			localShade.Zone = instance.GetCurrentMapZone();
			localShade.Fireball = session.Data.fireballLevel;
			localShade.Quake = session.Data.quakeLevel;
			localShade.Scream = session.Data.screamLevel;
			MonoBehaviour[] componentsInChildren = gameObject2.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			foreach (MonoBehaviour monoBehaviour in componentsInChildren)
			{
				if ((bool)monoBehaviour && monoBehaviour.GetType().Name.StartsWith("Persistent", StringComparison.Ordinal))
				{
					UnityEngine.Object.DestroyImmediate(monoBehaviour);
				}
			}
			int num = 0;
			bool flag = false;
			PlayMakerFSM[] componentsInChildren2 = gameObject2.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			foreach (PlayMakerFSM playMakerFSM in componentsInChildren2)
			{
				playMakerFSM.Fsm.InitData();
				if (playMakerFSM.FsmName == "Shade Control")
				{
					flag = true;
				}
				FsmState[] fsmStates = playMakerFSM.FsmStates;
				foreach (FsmState fsmState in fsmStates)
				{
					if (playMakerFSM.FsmName == "Shade Control" && fsmState.Name == "Killed")
					{
						fsmState.Actions = new FsmStateAction[1]
						{
							new ShadeDefeatAction
							{
								Shade = localShade
							}
						};
						fsmState.Transitions = new FsmTransition[0];
						num++;
						continue;
					}
					List<FsmStateAction> list = new List<FsmStateAction>();
					FsmStateAction[] actions = fsmState.Actions;
					foreach (FsmStateAction fsmStateAction in actions)
					{
						if (fsmStateAction != null)
						{
							FsmStateAction fsmStateAction2 = LocalRead(fsmStateAction, localShade);
							if (fsmStateAction2 != null)
							{
								list.Add(fsmStateAction2);
								num++;
							}
							else if (ShadeRules.IsSaveWrite(fsmStateAction.GetType().Name))
							{
								num++;
							}
							else
							{
								list.Add(fsmStateAction);
							}
						}
					}
					fsmState.Actions = list.ToArray();
				}
			}
			if (!flag)
			{
				throw new InvalidOperationException("Shade Control FSM missing");
			}
			HealthManager component = gameObject2.GetComponent<HealthManager>();
			if (!component)
			{
				throw new InvalidOperationException("Shade HealthManager missing");
			}
			component.hp = localShade.Health;
			component.SetGeoSmall(0);
			component.SetGeoMedium(0);
			component.SetGeoLarge(0);
			shades[owner.Index] = localShade;
			gameObject2.transform.SetParent(null, worldPositionStays: true);
			gameObject2.SetActive(value: true);
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
			Vector3 vector2 = vector;
			obj[5] = vector2.ToString();
			obj[6] = " local_actions=";
			obj[7] = num.ToString();
			Diagnostics.Write(string.Concat(obj));
		}
		catch (Exception ex)
		{
			if ((bool)gameObject2)
			{
				gameObject2.SetActive(value: false);
				UnityEngine.Object.Destroy(gameObject2);
			}
			Diagnostics.Throttled("SHADE SPAWN", ex);
			self.Notice("No se pudo crear la Sombra. El resto del mod sigue activo; envia el log.");
		}
		finally
		{
			if ((bool)gameObject)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private static FsmStateAction LocalRead(FsmStateAction action, LocalShade shade)
	{
		if (action is GetPlayerDataInt getPlayerDataInt && getPlayerDataInt.intName != null)
		{
			string value = getPlayerDataInt.intName.Value;
			if (value != null)
			{
				int value2;
				switch (value.Length)
				{
				case 7:
				{
					char c = value[0];
					if (c != 'g')
					{
						if (c != 's' || !(value == "shadeMP"))
						{
							break;
						}
						value2 = shade.Soul;
						goto IL_013e;
					}
					if (!(value == "geoPool"))
					{
						break;
					}
					goto IL_0138;
				}
				case 16:
				{
					char c = value[6];
					if (c != 'c')
					{
						if (c != 'p' || !(value == "shadeSpecialType"))
						{
							break;
						}
						goto IL_0138;
					}
					if (!(value == "shadeScreamLevel"))
					{
						break;
					}
					value2 = shade.Scream;
					goto IL_013e;
				}
				case 11:
					if (!(value == "shadeHealth"))
					{
						break;
					}
					value2 = shade.Health;
					goto IL_013e;
				case 18:
					if (!(value == "shadeFireballLevel"))
					{
						break;
					}
					value2 = shade.Fireball;
					goto IL_013e;
				case 15:
					{
						if (!(value == "shadeQuakeLevel"))
						{
							break;
						}
						value2 = shade.Quake;
						goto IL_013e;
					}
					IL_0138:
					value2 = 0;
					goto IL_013e;
					IL_013e:
					return new ShadeIntAction
					{
						Target = getPlayerDataInt.storeValue,
						Value = value2
					};
				}
			}
			return null;
		}
		if (action is GetPlayerDataBool getPlayerDataBool && getPlayerDataBool.boolName != null && getPlayerDataBool.boolName.Value == "soulLimited")
		{
			return new ShadeBoolAction
			{
				Target = getPlayerDataBool.storeValue,
				Value = true
			};
		}
		if (action is GetPlayerDataString getPlayerDataString && getPlayerDataString.stringName != null)
		{
			if (getPlayerDataString.stringName.Value == "shadeScene")
			{
				return new ShadeStringAction
				{
					Target = getPlayerDataString.storeValue,
					Value = shade.Scene
				};
			}
			if (getPlayerDataString.stringName.Value == "shadeMapZone")
			{
				return new ShadeStringAction
				{
					Target = getPlayerDataString.storeValue,
					Value = shade.Zone
				};
			}
		}
		return null;
	}

	internal static bool HandleDeath(HealthManager health)
	{
		LocalShade localShade = (health ? health.GetComponent<LocalShade>() : null);
		if (!localShade)
		{
			return false;
		}
		localShade.Defeat();
		return true;
	}

	internal static bool IsSpawning(GameObject source)
	{
		LocalShade localShade = (source ? source.GetComponentInParent<LocalShade>() : null);
		if ((bool)localShade)
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
		if (!self || !self.SpawnShades.Value || !self.Enabled.Value || self.Session == null || !self.Session.Active)
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
			if ((bool)localShade)
			{
				localShade.gameObject.SetActive(value: false);
				UnityEngine.Object.Destroy(localShade.gameObject);
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
