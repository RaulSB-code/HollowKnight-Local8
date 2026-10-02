namespace KO.HollowKnight8;

internal static class LightBlendRules
{
	internal static double Weight(LightPoint[] points, int count, int index, double x, double y, double feather, double[] scratch)
	{
		if (index == 0)
		{
			return 1.0;
		}
		int num;
		if (!Plugin.Self.GroupDarkness.Value && Plugin.Self.GatherDistance.Value < 27.5f)
		{
			if (index != 0)
			{
				return 0.0;
			}
			num = 1;
			for (int i = 1; i < count; i++)
			{
				double num2 = points[0].X - points[i].X;
				double num3 = points[0].Y - points[i].Y;
				if (num2 * num2 + num3 * num3 <= 16.0)
				{
					num++;
				}
			}
			return 1.0 / (double)num;
		}
		for (int i = 0; i < index; i++)
		{
			double num2 = points[index].X - points[i].X;
			double num3 = points[index].Y - points[i].Y;
			if (num2 * num2 + num3 * num3 <= 16.0)
			{
				return 0.0;
			}
		}
		num = 1;
		for (int i = 0; i < count; i++)
		{
			if (i != index)
			{
				double num2 = points[index].X - points[i].X;
				double num3 = points[index].Y - points[i].Y;
				if (num2 * num2 + num3 * num3 <= 16.0)
				{
					num++;
				}
			}
		}
		return 1.0 / (double)num;
	}
}
