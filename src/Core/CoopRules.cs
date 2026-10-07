using System;

namespace KO.HollowKnight8;

internal static class CoopRules
{
	internal static int RevivalCost(int maxSoul)
	{
		if (maxSoul <= 0)
		{
			return 99;
		}
		return Math.Min(99, maxSoul);
	}

	internal static int RevivalSpent(int cost, double progress)
	{
		return Math.Min(cost, Math.Max(0, (int)Math.Floor((double)cost * Math.Max(0.0, Math.Min(1.0, progress)) + 1E-06)));
	}

	internal static bool CanGatherArena(bool gameplayScene, bool loading, bool entered)
	{
		return gameplayScene && !loading && entered;
	}

	internal static bool StartsBattle(string name)
	{
		if (!(name == "BG CLOSE") && !(name == "BATTLE START"))
		{
			return name == "START BATTLE";
		}
		return true;
	}

	internal static bool CanQueueJoin(bool gameplayScene, bool loading, bool entered, bool playing, bool paused)
	{
		if (gameplayScene && !loading && entered)
		{
			return playing || paused;
		}
		return false;
	}

	internal static bool ResetRoster(bool menu, bool title, bool loading)
	{
		if (!loading)
		{
			return menu || title;
		}
		return false;
	}

	internal static bool RunHeroFrame(int index, bool ready, bool down, bool hazard, bool retiring, bool blocked, bool gameplay, bool reviving)
	{
		if (ready && !down && !hazard && !retiring && !reviving)
		{
			if (index != 0)
			{
				return !blocked && gameplay;
			}
			return true;
		}
		return false;
	}

	internal static double DamageDivisor(int players, int difficulty)
	{
		double[] array = new double[4] { 0.45, 0.65, 0.85, 1.0 };
		return 1.0 + array[Math.Max(0, Math.Min(3, difficulty))] * (double)Math.Max(0, players - 1);
	}

	internal static bool CanEquip(int used, int cost, int slots, bool overcharm)
	{
		if (cost != 0 && used + cost > slots)
		{
			if (overcharm)
			{
				return used < slots;
			}
			return false;
		}
		return true;
	}

	internal static double PlayerDepth(int index)
	{
		return 0.004 - 0.012 * (double)Math.Max(0, Math.Min(7, index));
	}

	internal static double HudScale(int count, double width, double userScale)
	{
		count = Math.Max(1, Math.Min(8, count));
		double num = 1.0 / (1.0 + 0.105 * (double)Math.Max(0, count - 2));
		double val = (Math.Max(120.0, width) - 32.0 - (double)((count - 1) * 8)) / (double)(count * 254);
		return Math.Max(0.15, Math.Min(num * Math.Max(0.7, Math.Min(1.5, userScale)), val));
	}
}
