using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class SummonRouting
{
	private const int Shield = 0;

	private const int Grimm = 1;

	private const int Hatch = 2;

	private static readonly GameObject[] prefabs;

	private static readonly Transform[] parents;

	private static readonly GameObject[,] manual;

	private static readonly List<GameObject>[] manualHatch;

	private static readonly float[] nextCheck;

	private static readonly float[] nextHatch;

	private static readonly int[] ids;

	private static int sceneHandle;

	private static int discoveredScene;

	private static bool spawning;

	static SummonRouting()
	{
		prefabs = (GameObject[])(object)new GameObject[3];
		parents = (Transform[])(object)new Transform[3];
		manual = new GameObject[8, 3];
		manualHatch = new List<GameObject>[8];
		nextCheck = new float[8];
		nextHatch = new float[8];
		ids = new int[3] { 38, 40, 22 };
		sceneHandle = -1;
		discoveredScene = -1;
		for (int i = 0; i < 8; i++)
		{
			manualHatch[i] = new List<GameObject>(4);
		}
	}

	private static int Kind(GameObject root)
	{
		if (!Object.op_Implicit((Object)(object)root))
		{
			return -1;
		}
		if (Object.op_Implicit((Object)(object)root.GetComponent<KnightHatchling>()))
		{
			return 2;
		}
		string text = ((Object)root).name.ToLowerInvariant();
		if (text.Contains("orbit shield") || text.Contains("dreamshield") || text.Contains("dream shield"))
		{
			return 0;
		}
		if (text.Contains("grimmchild") || text.Contains("grimm child"))
		{
			return 1;
		}
		if (text.Contains("knight hatchling"))
		{
			return 2;
		}
		return -1;
	}

	internal static bool Familiar(GameObject root)
	{
		if (Object.op_Implicit((Object)(object)root))
		{
			if (Kind(root) < 0)
			{
				return ((Object)root).name.IndexOf("weaverling", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return true;
		}
		return false;
	}

	internal static string DamageKind(GameObject source)
	{
		if (!Object.op_Implicit((Object)(object)source))
		{
			return null;
		}
		Transform val = source.transform;
		while (Object.op_Implicit((Object)(object)val))
		{
			string text = ((Object)val).name.ToLowerInvariant();
			if (text.Contains("knight hatchling"))
			{
				return "hatchling";
			}
			if (text.Contains("weaverling"))
			{
				return "weaverling";
			}
			if (text.Contains("grimmchild") || text.Contains("grimm child"))
			{
				return "grimmchild";
			}
			if (text.Contains("orbit shield") || text.Contains("dreamshield") || text.Contains("dream shield"))
			{
				return "shield";
			}
			if (Object.op_Implicit((Object)(object)((Component)val).GetComponent<HeroController>()))
			{
				break;
			}
			val = val.parent;
		}
		return null;
	}

	internal static void Observed(GameObject prefab, Transform parent, GameObject spawned)
	{
		int num = Kind(spawned);
		if (num < 0)
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)prefab) && Kind(prefab) == num)
		{
			prefabs[num] = prefab;
			parents[num] = parent;
		}
		if (spawning || !Object.op_Implicit((Object)(object)spawned))
		{
			return;
		}
		PlayerSlot playerSlot = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session)?.Resolve(spawned);
		if (playerSlot != null && playerSlot.Index != 0 && num != 2)
		{
			GameObject val = manual[playerSlot.Index, num];
			if (Object.op_Implicit((Object)(object)val) && (Object)(object)val != (Object)(object)spawned)
			{
				manual[playerSlot.Index, num] = null;
				ObjectPool.Recycle(val);
				Diagnostics.Write("SUMMON native replaced fallback P" + (playerSlot.Index + 1) + " type=" + num);
			}
		}
	}

	internal static void Refresh(PlayerSlot player)
	{
		if (player == null || !Object.op_Implicit((Object)(object)player.Hero))
		{
			return;
		}
		int num = 0;
		using (PlayerContext.Enter(player))
		{
			PlayMakerFSM[] componentsInChildren = ((Component)player.Hero).GetComponentsInChildren<PlayMakerFSM>(true);
			foreach (PlayMakerFSM val in componentsInChildren)
			{
				if (!Object.op_Implicit((Object)(object)val) || val.Fsm == null)
				{
					continue;
				}
				if (player.Index > 0 && val.FsmVariables != null)
				{
					FsmGameObject[] gameObjectVariables = val.FsmVariables.GameObjectVariables;
					foreach (FsmGameObject val2 in gameObjectVariables)
					{
						if (val2 != null)
						{
							Remember(val2.Value);
						}
					}
				}
				val.SendEvent("CHARM EQUIP CHECK");
				num++;
			}
		}
		if (player.Index > 0)
		{
			nextCheck[player.Index] = 0f;
			Diagnostics.Write("SUMMON refresh P" + (player.Index + 1) + " hero-controllers=" + num + " charms=" + string.Join(",", player.Charms.Ids()));
		}
	}

	internal static void Tick(CoopSession session)
	{
		if (session == null || !session.Active)
		{
			return;
		}
		Scene activeScene = SceneManager.GetActiveScene();
		int handle = ((Scene)(ref activeScene)).handle;
		if (sceneHandle != handle)
		{
			sceneHandle = handle;
			for (int i = 1; i < 8; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					RecycleManual(i, j);
				}
			}
			Array.Clear(nextCheck, 0, 8);
		}
		if (discoveredScene != handle)
		{
			discoveredScene = handle;
			ObjectPool instance = ObjectPool.instance;
			if (Object.op_Implicit((Object)(object)instance) && instance.startupPools != null)
			{
				StartupPool[] startupPools = instance.startupPools;
				foreach (StartupPool val in startupPools)
				{
					if (val != null)
					{
						Remember(val.prefab);
					}
				}
			}
		}
		foreach (PlayerSlot p in session.Players)
		{
			if (p.Index <= 0 || !Object.op_Implicit((Object)(object)p.Hero) || Time.unscaledTime < nextCheck[p.Index])
			{
				continue;
			}
			nextCheck[p.Index] = Time.unscaledTime + 2f;
			if (!p.Alive || !p.Ready)
			{
				for (int l = 0; l < 3; l++)
				{
					RecycleManual(p.Index, l);
				}
				continue;
			}
			for (int m = 0; m < 3; m++)
			{
				if (!p.Charms.Equipped[ids[m] - 1])
				{
					RecycleManual(p.Index, m);
					continue;
				}
				if (m == 2)
				{
					manualHatch[p.Index].RemoveAll((GameObject h) => !Object.op_Implicit((Object)(object)h) || !h.activeInHierarchy || session.Resolve(h) != p);
				}
				GameObject val2 = ((m == 2) ? null : manual[p.Index, m]);
				if (Object.op_Implicit((Object)(object)val2) && val2.activeInHierarchy)
				{
					continue;
				}
				if (m != 2)
				{
					manual[p.Index, m] = null;
				}
				if (!Object.op_Implicit((Object)(object)prefabs[m]) || CountOwned(p, m) >= ((m != 2) ? 1 : 4))
				{
					continue;
				}
				if (m == 2)
				{
					if (Time.time < nextHatch[p.Index] || p.Vitals.Soul < 8)
					{
						continue;
					}
					nextHatch[p.Index] = Time.time + 4f;
				}
				Transform val3 = parents[m];
				if (Object.op_Implicit((Object)(object)val3) && session.Primary != null && Object.op_Implicit((Object)(object)session.Primary.Hero) && val3.IsChildOf(((Component)session.Primary.Hero).transform))
				{
					GameObject val4 = session.Remap(((Component)val3).gameObject, session.Primary, p);
					val3 = (Object.op_Implicit((Object)(object)val4) ? val4.transform : null);
				}
				try
				{
					spawning = true;
					GameObject val5;
					using (PlayerContext.Enter(p))
					{
						val5 = ObjectPool.Spawn(prefabs[m], val3, ((Component)p.Hero).transform.position + new Vector3(0f, 1.25f, 0f), Quaternion.identity);
					}
					if (!Object.op_Implicit((Object)(object)val5))
					{
						continue;
					}
					if (m == 2)
					{
						manualHatch[p.Index].Add(val5);
						using (PlayerContext.Enter(p))
						{
							session.Data.MPCharge = Math.Max(0, session.Data.MPCharge - 8);
							p.Capture(session.Data);
						}
					}
					else
					{
						manual[p.Index, m] = val5;
					}
					Diagnostics.Write("SUMMON fallback P" + (p.Index + 1) + " type=" + m + " prefab=" + ((Object)prefabs[m]).name);
				}
				catch (Exception ex)
				{
					Diagnostics.Throttled("SUMMON fallback", ex);
				}
				finally
				{
					spawning = false;
				}
			}
		}
	}

	private static void Remember(GameObject prefab)
	{
		if (Object.op_Implicit((Object)(object)prefab) && !((Object)prefab).name.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
		{
			int num = Kind(prefab);
			if (num >= 0 && !Object.op_Implicit((Object)(object)prefabs[num]))
			{
				prefabs[num] = prefab;
			}
		}
	}

	private static int CountOwned(PlayerSlot p, int type)
	{
		int num = 0;
		OwnerTag[] array = Object.FindObjectsOfType<OwnerTag>();
		foreach (OwnerTag ownerTag in array)
		{
			if (Object.op_Implicit((Object)(object)ownerTag) && ownerTag.Player == p && ((Component)ownerTag).gameObject.activeInHierarchy && Kind(((Component)ownerTag).gameObject) == type)
			{
				num++;
			}
		}
		return num;
	}

	private static void RecycleManual(int player, int type)
	{
		if (type == 2)
		{
			foreach (GameObject item in manualHatch[player])
			{
				if (Object.op_Implicit((Object)(object)item) && item.activeInHierarchy && Object.op_Implicit((Object)(object)item.GetComponent<OwnerTag>()) && item.GetComponent<OwnerTag>().Player != null && item.GetComponent<OwnerTag>().Player.Index == player)
				{
					ObjectPool.Recycle(item);
				}
			}
			manualHatch[player].Clear();
		}
		else
		{
			GameObject val = manual[player, type];
			manual[player, type] = null;
			if (Object.op_Implicit((Object)(object)val))
			{
				ObjectPool.Recycle(val);
			}
		}
	}

	internal static void Reset()
	{
		for (int i = 1; i < 8; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				RecycleManual(i, j);
			}
		}
		Array.Clear(nextCheck, 0, 8);
		Array.Clear(nextHatch, 0, 8);
		sceneHandle = (discoveredScene = -1);
	}
}
