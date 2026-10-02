using System;

namespace KO.HollowKnight8;

internal static class ShadeRules
{
	internal static bool IsSaveWrite(string action)
	{
		if (action.IndexOf("PlayerData", StringComparison.Ordinal) >= 0)
		{
			if (!action.StartsWith("Set", StringComparison.Ordinal) && !action.StartsWith("Increment", StringComparison.Ordinal) && !action.StartsWith("Decrement", StringComparison.Ordinal))
			{
				return action.StartsWith("Add", StringComparison.Ordinal);
			}
			return true;
		}
		return false;
	}
}
