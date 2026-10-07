using System;

namespace KO.HollowKnight8;

internal static class VisibilityRules
{
	internal static double Alpha(double squaredDistance, double inner, double outer, double opacity)
	{
		if (squaredDistance <= inner * inner)
		{
			return 0.0;
		}
		if (squaredDistance >= outer * outer)
		{
			return opacity;
		}
		double num = (Math.Sqrt(squaredDistance) - inner) / Math.Max(0.001, outer - inner);
		return opacity * num * num * (3.0 - 2.0 * num);
	}
}
