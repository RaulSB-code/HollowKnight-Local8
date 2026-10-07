using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

internal sealed class ShadeDefeatAction : FsmStateAction
{
	internal LocalShade Shade;

	public override void OnEnter()
	{
		if ((bool)Shade)
		{
			Shade.Defeat();
		}
		Finish();
	}
}
