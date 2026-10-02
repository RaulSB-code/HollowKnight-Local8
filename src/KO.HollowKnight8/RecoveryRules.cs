using System;

namespace KO.HollowKnight8;

internal static class RecoveryRules
{
	internal static bool InRoom(double x, double y, double width, double height)
	{
		if (!double.IsNaN(x) && !double.IsNaN(y) && x >= 0.0 && y >= 0.0 && x <= width)
		{
			return y <= height;
		}
		return false;
	}

	internal static bool EnvironmentalHazard(int type)
	{
		if (type >= 2)
		{
			return type <= 5;
		}
		return false;
	}

	internal static bool RecoverAfterHazard(int type, bool hasAcidArmour)
	{
		if (EnvironmentalHazard(type))
		{
			return !(type == 3 && hasAcidArmour);
		}
		return false;
	}

	internal static int ReserveTransfer(int soul, int maximum, int reserve, int budget)
	{
		return Math.Max(0, Math.Min(Math.Max(0, maximum - soul), Math.Min(reserve, budget)));
	}

	internal static int ShadeHealth(int nail, int masks)
	{
		return Math.Max(5, Math.Min(50, Math.Max(1, nail) * Math.Max(2, (masks + 1) / 2)));
	}
}
