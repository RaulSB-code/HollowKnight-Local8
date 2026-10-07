using System;

namespace KO.HollowKnight8;

internal static class ArenaRules
{
	internal static bool NeedsEntry(bool isEntrant, bool bounded, bool inside, double separation)
	{
		if (!isEntrant)
		{
			if (bounded && inside)
			{
				return separation > 6.0;
			}
			return true;
		}
		return false;
	}

	internal static bool ClosingGateState(string name, bool wasOpen)
	{
		name = (name ?? "").ToLowerInvariant();
		if (name.Contains("check") || name.Contains("open"))
		{
			return false;
		}
		if (name == "closed")
		{
			return wasOpen;
		}
		if (!(name == "close") && !name.StartsWith("close "))
		{
			return name == "closing";
		}
		return true;
	}

	internal static ArenaZone Zone(ArenaPoint inside, ArenaGate[] gates)
	{
		ArenaZone arenaZone = default(ArenaZone);
		arenaZone.Left = double.NegativeInfinity;
		arenaZone.Right = double.PositiveInfinity;
		arenaZone.Bottom = double.NegativeInfinity;
		arenaZone.Top = double.PositiveInfinity;
		ArenaZone result = arenaZone;
		for (int i = 0; i < gates.Length; i++)
		{
			ArenaGate arenaGate = gates[i];
			double num = arenaGate.Right - arenaGate.Left;
			double num2 = arenaGate.Top - arenaGate.Bottom;
			if (num2 >= 2.5 && num2 > num * 1.2 && inside.Y >= arenaGate.Bottom - 4.0 && inside.Y <= arenaGate.Top + 4.0)
			{
				if (arenaGate.Right < inside.X - 0.3)
				{
					result.Left = Math.Max(result.Left, arenaGate.Right);
					result.Bounded = true;
				}
				else if (arenaGate.Left > inside.X + 0.3)
				{
					result.Right = Math.Min(result.Right, arenaGate.Left);
					result.Bounded = true;
				}
			}
			else if (num >= 2.5 && num > num2 * 1.2 && inside.X >= arenaGate.Left - 3.0 && inside.X <= arenaGate.Right + 3.0)
			{
				if (arenaGate.Top < inside.Y - 0.3)
				{
					result.Bottom = Math.Max(result.Bottom, arenaGate.Top);
					result.Bounded = true;
				}
				else if (arenaGate.Bottom > inside.Y + 0.3)
				{
					result.Top = Math.Min(result.Top, arenaGate.Bottom);
					result.Bounded = true;
				}
			}
		}
		return result;
	}

	internal static int SelectInside(ArenaPoint[] players, ArenaGate[] gates, int preferred)
	{
		int result = -1;
		double num = -1.0;
		for (int i = 0; i < players.Length; i++)
		{
			ArenaPoint arenaPoint = players[i];
			double num2 = double.NegativeInfinity;
			double num3 = double.PositiveInfinity;
			for (int j = 0; j < gates.Length; j++)
			{
				ArenaGate arenaGate = gates[j];
				if (!(arenaPoint.Y < arenaGate.Bottom - 2.0) && !(arenaPoint.Y > arenaGate.Top + 2.0))
				{
					if (arenaGate.Right < arenaPoint.X - 0.5)
					{
						num2 = Math.Max(num2, arenaGate.Right);
					}
					if (arenaGate.Left > arenaPoint.X + 0.5)
					{
						num3 = Math.Min(num3, arenaGate.Left);
					}
				}
			}
			if (!double.IsInfinity(num2) && !double.IsInfinity(num3) && !(num3 - num2 < 4.0) && !(num3 - num2 > 120.0))
			{
				if (i == preferred)
				{
					return i;
				}
				double num4 = Math.Min(arenaPoint.X - num2, num3 - arenaPoint.X);
				if (num4 > num)
				{
					num = num4;
					result = i;
				}
			}
		}
		return result;
	}
}
