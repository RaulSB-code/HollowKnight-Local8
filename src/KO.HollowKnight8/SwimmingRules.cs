namespace KO.HollowKnight8;

internal static class SwimmingRules
{
	internal static bool InRange(double pivot, double floatHeight, bool active)
	{
		if (pivot >= floatHeight - 3.0)
		{
			return pivot <= floatHeight + (active ? 0.35 : 0.12);
		}
		return false;
	}
}
