namespace KO.HollowKnight8;

internal sealed class PvpSwing
{
	internal int touched;

	internal bool Parried;

	internal void Reset()
	{
		touched = 0;
		Parried = false;
	}

	internal bool Touch(int player)
	{
		int num = 1 << player;
		if (Parried || (touched & num) != 0)
		{
			return false;
		}
		touched |= num;
		return true;
	}
}
