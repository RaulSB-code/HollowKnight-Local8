using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

internal sealed class ShadeBoolAction : FsmStateAction
{
	internal FsmBool Target;

	internal bool Value;

	public override void OnEnter()
	{
		if (Target != null)
		{
			Target.Value = Value;
		}
		((FsmStateAction)this).Finish();
	}
}
