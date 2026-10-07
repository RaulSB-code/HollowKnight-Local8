using System;

namespace KO.HollowKnight8;

internal static class PvpRules
{
	internal const int Ongoing = int.MinValue;

	internal static int SeriesLength(int rounds)
	{
		if (rounds > 0)
		{
			return Math.Min(15, Math.Max(1, rounds) | 1);
		}
		return 0;
	}

	internal static int WinsNeeded(int rounds)
	{
		int num = SeriesLength(rounds);
		if (num != 0)
		{
			return num / 2 + 1;
		}
		return int.MaxValue;
	}

	internal static bool SeriesComplete(int rounds, int wins)
	{
		if (rounds > 0)
		{
			return wins >= WinsNeeded(rounds);
		}
		return false;
	}

	internal static int Side(int player, int team)
	{
		if (team <= 0)
		{
			return -player - 1;
		}
		return team;
	}

	internal static bool Opponents(int a, int b, int teamA, int teamB)
	{
		if (a != b)
		{
			if (teamA != 0 && teamB != 0)
			{
				return teamA != teamB;
			}
			return true;
		}
		return false;
	}

	internal static int Outcome(int[] sides, bool[] alive, int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (alive[i])
			{
				if (num == 0)
				{
					num = sides[i];
				}
				else if (num != sides[i])
				{
					return int.MinValue;
				}
			}
		}
		return num;
	}
}
