using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class ShadeDefeatAction : FsmStateAction
{
	internal LocalShade Shade;

	public override void OnEnter()
	{
		if (Object.op_Implicit((Object)(object)Shade))
		{
			Shade.Defeat();
		}
		((FsmStateAction)this).Finish();
	}
}
