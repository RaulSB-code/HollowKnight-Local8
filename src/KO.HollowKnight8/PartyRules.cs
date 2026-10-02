using System;

namespace KO.HollowKnight8;

internal static class PartyRules
{
	internal const double LandingRadius = 3.5;

	internal static int Oldest(double[] born, bool[] eligible)
	{
		int num = -1;
		for (int i = 0; i < born.Length; i++)
		{
			if (eligible[i] && (num < 0 || born[i] < born[num]))
			{
				num = i;
			}
		}
		return num;
	}

	internal static int Nearest(double[] squared, bool[] eligible)
	{
		int num = -1;
		for (int i = 0; i < squared.Length; i++)
		{
			if (eligible[i] && (num < 0 || squared[i] < squared[num]))
			{
				num = i;
			}
		}
		return num;
	}

	internal static bool Near(double dx, double dy)
	{
		return dx * dx + dy * dy <= 12.25;
	}

	internal static int FreeSeat(bool[] occupied)
	{
		for (int i = 0; i < Math.Min(3, occupied.Length); i++)
		{
			if (!occupied[i])
			{
				return i;
			}
		}
		return -1;
	}

	internal static double SeatOffset(int seat)
	{
		return seat switch
		{
			2 => 0.72, 
			1 => -0.72, 
			_ => 0.0, 
		};
	}
}
