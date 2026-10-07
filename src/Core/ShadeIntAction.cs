using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

internal sealed class ShadeIntAction : FsmStateAction
{
	internal FsmInt Target;

	internal int Value;

	public override void OnEnter()
	{
		if (Target != null)
		{
			Target.Value = Value;
		}
		Finish();
	}
}
