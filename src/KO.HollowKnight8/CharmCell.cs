namespace KO.HollowKnight8;

internal struct CharmCell
{
	internal int Id;

	internal double X;

	internal double Y;

	internal bool Owned;

	internal CharmCell(int id, double x, double y, bool owned)
	{
		Id = id;
		X = x;
		Y = y;
		Owned = owned;
	}
}
