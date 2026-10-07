using System;

namespace KO.HollowKnight8;

internal struct GroupBounds
{
	internal int Count;

	internal double MinX;

	internal double MaxX;

	internal double MinY;

	internal double MaxY;

	internal double X => (MinX + MaxX) * 0.5;

	internal double Y => (MinY + MaxY) * 0.5;

	internal void Add(double x, double y)
	{
		if (Count++ == 0)
		{
			MinX = (MaxX = x);
			MinY = (MaxY = y);
			return;
		}
		MinX = Math.Min(MinX, x);
		MaxX = Math.Max(MaxX, x);
		MinY = Math.Min(MinY, y);
		MaxY = Math.Max(MaxY, y);
	}

	internal void AddBox(double left, double right, double bottom, double top)
	{
		if (Count++ == 0)
		{
			MinX = left;
			MaxX = right;
			MinY = bottom;
			MaxY = top;
		}
		else
		{
			MinX = Math.Min(MinX, left);
			MaxX = Math.Max(MaxX, right);
			MinY = Math.Min(MinY, bottom);
			MaxY = Math.Max(MaxY, top);
		}
	}
}
