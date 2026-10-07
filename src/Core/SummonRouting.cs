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
		prefabs = new GameObject[3];
		parents = new Transform[3];
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
		if (!root)
		{
			return -1;
		}
		if ((bool)root.GetComponent<KnightHatchling>())
		{
			return 2;
		}
		string text = root.name.ToLowerInvariant();
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
		if ((bool)root)
		{
			if (Kind(root) < 0)
			{
				return root.name.IndexOf("weaverling", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return true;
		}
		return false;
	}

	internal static string DamageKind(GameObject source)
	{
		if (!source)
		{
			return null;
		}
		Transform transform = source.transform;
		while ((bool)transform)
		{
			string text = transform.name.ToLowerInvariant();
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
			if ((bool)transform.GetComponent<HeroController>())
			{
				break;
			}
			transform = transform.parent;
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
		if ((bool)prefab && Kind(prefab) == num)
		{
			prefabs[num] = prefab;
			parents[num] = parent;
		}
		if (spawning || !spawned)
		{
			return;
		}
		PlayerSlot playerSlot = ((Plugin.Self == null) ? null : Plugin.Self.Session)?.Resolve(spawned);
		if (playerSlot != null && playerSlot.Index != 0 && num != 2)
		{
			GameObject gameObject = manual[playerSlot.Index, num];
			if ((bool)gameObject && gameObject != spawned)
			{
				manual[playerSlot.Index, num] = null;
				ObjectPool.Recycle(gameObject);
				Diagnostics.Write("SUMMON native replaced fallback P" + (playerSlot.Index + 1) + " type=" + num);
			}
		}
	}

	internal static void Refresh(PlayerSlot player)
	{
		if (player == null || !player.Hero)
		{
			return;
		}
		int num = 0;
		using (PlayerContext.Enter(player))
		{
			PlayMakerFSM[] componentsInChildren = player.Hero.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			foreach (PlayMakerFSM playMakerFSM in componentsInChildren)
			{
				if (!playMakerFSM || playMakerFSM.Fsm == null)
				{
					continue;
				}
				if (player.Index > 0 && playMakerFSM.FsmVariables != null)
				{
					FsmGameObject[] gameObjectVariables = playMakerFSM.FsmVariables.GameObjectVariables;
					foreach (FsmGameObject fsmGameObject in gameObjectVariables)
					{
						if (fsmGameObject != null)
						{
							Remember(fsmGameObject.Value);
						}
					}
				}
				playMakerFSM.SendEvent("CHARM EQUIP CHECK");
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
		int handle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
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
			if ((bool)instance && instance.startupPools != null)
			{
				ObjectPool.StartupPool[] startupPools = instance.startupPools;
				foreach (ObjectPool.StartupPool startupPool in startupPools)
				{
					if (startupPool != null)
					{
						Remember(startupPool.prefab);
					}
				}
			}
		}
		foreach (PlayerSlot p in session.Players)
		{
			if (p.Index <= 0 || !p.Hero || Time.unscaledTime < nextCheck[p.Index])
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
					manualHatch[p.Index].RemoveAll((GameObject h) => !h || !h.activeInHierarchy || session.Resolve(h) != p);
				}
				GameObject gameObject = ((m == 2) ? null : manual[p.Index, m]);
				if ((bool)gameObject && gameObject.activeInHierarchy)
				{
					continue;
				}
				if (m != 2)
				{
					manual[p.Index, m] = null;
				}
				if (!prefabs[m] || CountOwned(p, m) >= ((m != 2) ? 1 : 4))
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
				Transform transform = parents[m];
				if ((bool)transform && session.Primary != null && (bool)session.Primary.Hero && transform.IsChildOf(session.Primary.Hero.transform))
				{
					GameObject gameObject2 = session.Remap(transform.gameObject, session.Primary, p);
					transform = (gameObject2 ? gameObject2.transform : null);
				}
				try
				{
					spawning = true;
					GameObject gameObject3;
					using (PlayerContext.Enter(p))
					{
						gameObject3 = ObjectPool.Spawn(prefabs[m], transform, p.Hero.transform.position + new Vector3(0f, 1.25f, 0f), Quaternion.identity);
					}
					if (!gameObject3)
					{
						continue;
					}
					if (m == 2)
					{
						manualHatch[p.Index].Add(gameObject3);
						using (PlayerContext.Enter(p))
						{
							session.Data.MPCharge = Math.Max(0, session.Data.MPCharge - 8);
							p.Capture(session.Data);
						}
					}
					else
					{
						manual[p.Index, m] = gameObject3;
					}
					Diagnostics.Write("SUMMON fallback P" + (p.Index + 1) + " type=" + m + " prefab=" + prefabs[m].name);
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
		if ((bool)prefab && !prefab.name.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
		{
			int num = Kind(prefab);
			if (num >= 0 && !prefabs[num])
			{
				prefabs[num] = prefab;
			}
		}
	}

	private static int CountOwned(PlayerSlot p, int type)
	{
		int num = 0;
		OwnerTag[] array = UnityEngine.Object.FindObjectsOfType<OwnerTag>();
		foreach (OwnerTag ownerTag in array)
		{
			if ((bool)ownerTag && ownerTag.Player == p && ownerTag.gameObject.activeInHierarchy && Kind(ownerTag.gameObject) == type)
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
				if ((bool)item && item.activeInHierarchy && (bool)item.GetComponent<OwnerTag>() && item.GetComponent<OwnerTag>().Player != null && item.GetComponent<OwnerTag>().Player.Index == player)
				{
					ObjectPool.Recycle(item);
				}
			}
			manualHatch[player].Clear();
		}
		else
		{
			GameObject gameObject = manual[player, type];
			manual[player, type] = null;
			if ((bool)gameObject)
			{
				ObjectPool.Recycle(gameObject);
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
