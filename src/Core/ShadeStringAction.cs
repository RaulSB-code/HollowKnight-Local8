using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

internal sealed class ShadeStringAction : FsmStateAction
{
	internal FsmString Target;

	internal string Value;

	public override void OnEnter()
	{
		if (Target != null)
		{
			Target.Value = Value;
		}
		Finish();
	}
}
