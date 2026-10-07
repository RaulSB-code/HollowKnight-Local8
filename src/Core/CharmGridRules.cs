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
			int num7 = b.Y.CompareTo(a.Y);
			return (num7 == 0) ? a.X.CompareTo(b.X) : num7;
		});
		for (int i = 0; i < 4; i++)
		{
			list.Sort(i * 10, 10, Comparer<CharmCell>.Create((CharmCell a, CharmCell b) => a.X.CompareTo(b.X)));
		}
		int num = list.FindIndex((CharmCell c) => c.Id == current);
		if (num < 0)
		{
			return current;
		}
		int num2 = num / 10;
		int num3 = num % 10;
		if (dx != 0)
		{
			for (int j = num3 + dx; j >= 0 && j < 10; j += dx)
			{
				if (list[num2 * 10 + j].Owned)
				{
					return list[num2 * 10 + j].Id;
				}
			}
		}
		if (dy != 0)
		{
			int num4 = num2 - dy;
			while (num4 >= 0 && num4 < 4)
			{
				int num5 = -1;
				int num6 = int.MaxValue;
				for (int k = 0; k < 10; k++)
				{
					if (list[num4 * 10 + k].Owned && Math.Abs(k - num3) < num6)
					{
						num5 = num4 * 10 + k;
						num6 = Math.Abs(k - num3);
					}
				}
				if (num5 >= 0)
				{
					return list[num5].Id;
				}
				num4 -= dy;
			}
		}
		return current;
	}
}
