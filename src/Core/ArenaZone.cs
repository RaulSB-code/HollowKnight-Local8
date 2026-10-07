namespace KO.HollowKnight8;

internal struct ArenaZone
{
	internal double Left;

	internal double Right;

	internal double Bottom;

	internal double Top;

	internal bool Bounded;

	internal bool Contains(double x, double y, double margin = 0.65)
	{
		if (Bounded)
		{
			if (x >= Left + margin && x <= Right - margin && y >= Bottom + margin)
			{
				return y <= Top - margin;
			}
			return false;
		}
		return true;
	}
}
