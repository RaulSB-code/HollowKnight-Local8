using System;

namespace KO.HollowKnight8;

internal static class FrameMath
{
	internal static double Clamp(double x, double a, double b)
	{
		return Math.Max(a, Math.Min(b, x));
	}

	internal static double HalfHeight(double minX, double maxX, double minY, double maxY, double aspect, double baseHalf, double zoomLimit, double roomW, double roomH, double sideMargin = 2.8)
	{
		aspect = Math.Max(0.1, aspect);
		double x = Math.Max((maxY - minY) * 0.5 + 2.1, ((maxX - minX) * 0.5 + Math.Max(2.8, sideMargin)) / aspect);
		double val = Math.Min(baseHalf * zoomLimit, Math.Min(roomH * 0.5, roomW * 0.5 / aspect));
		return Clamp(x, baseHalf, Math.Max(baseHalf, val));
	}

	internal static double Center(double value, double min, double max, double extent)
	{
		if (max - min <= 2.0 * extent)
		{
			return (min + max) * 0.5;
		}
		return Clamp(value, min + extent, max - extent);
	}

	internal static double Contain(double center, double low, double high, double half)
	{
		if (high - low > 2.0 * half)
		{
			return (low + high) * 0.5;
		}
		return Clamp(center, high - half, low + half);
	}
}
