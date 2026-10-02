using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

internal static class CharmGridRules
{
	internal static int Next(int current, int dx, int dy, IEnumerable<CharmCell> cells)
	{
		List<CharmCell> list = new List<CharmCell>();
		foreach (CharmCell cell in cells)
		{
			if (cell.Id >= 1 && cell.Id <= 40)
			{
				list.Add(cell);
			}
		}
		if (list.Count != 40)
		{
			return current;
		}
		list.Sort(delegate(CharmCell a, CharmCell b)
		{
			int num10 = b.Y.CompareTo(a.Y);
			return (num10 == 0) ? a.X.CompareTo(b.X) : num10;
		});
		for (int num = 0; num < 4; num++)
		{
			list.Sort(num * 10, 10, Comparer<CharmCell>.Create((CharmCell a, CharmCell b) => a.X.CompareTo(b.X)));
		}
		int num2 = list.FindIndex((CharmCell c) => c.Id == current);
		if (num2 < 0)
		{
			return current;
		}
		int num3 = num2 / 10;
		int num4 = num2 % 10;
		if (dx != 0)
		{
			for (int num5 = num4 + dx; num5 >= 0 && num5 < 10; num5 += dx)
			{
				if (list[num3 * 10 + num5].Owned)
				{
					return list[num3 * 10 + num5].Id;
				}
			}
		}
		if (dy != 0)
		{
			int num6 = num3 - dy;
			while (num6 >= 0 && num6 < 4)
			{
				int num7 = -1;
				int num8 = int.MaxValue;
				for (int num9 = 0; num9 < 10; num9++)
				{
					if (list[num6 * 10 + num9].Owned && Math.Abs(num9 - num4) < num8)
					{
						num7 = num6 * 10 + num9;
						num8 = Math.Abs(num9 - num4);
					}
				}
				if (num7 >= 0)
				{
					return list[num7].Id;
				}
				num6 -= dy;
			}
		}
		return current;
	}
}
