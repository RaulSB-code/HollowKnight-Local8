using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ActorRecovery
{
	internal static void Freeze(PlayerSlot p)
	{
		if (p != null && Object.op_Implicit((Object)(object)p.Hero))
		{
			Rigidbody2D component = ((Component)p.Hero).GetComponent<Rigidbody2D>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.velocity = Vector2.zero;
				component.angularVelocity = 0f;
				component.gravityScale = 0f;
				component.isKinematic = true;
				component.simulated = false;
			}
		}
	}

	internal static void Physics(PlayerSlot p)
	{
		HeroController hero = p.Hero;
		Rigidbody2D component = ((Component)hero).GetComponent<Rigidbody2D>();
		float num = ((hero.DEFAULT_GRAVITY > 0f) ? hero.DEFAULT_GRAVITY : 1f);
		Reflect.Set(hero, "prevGravityScale", num);
		if (Object.op_Implicit((Object)(object)component))
		{
			component.simulated = true;
			component.isKinematic = false;
			component.gravityScale = num;
			component.velocity = Vector2.zero;
			component.angularVelocity = 0f;
		}
		hero.inAcid = false;
		hero.cState.inAcid = false;
		hero.cState.swimming = false;
		p.AcidAssistActive = false;
		p.HeroBoxInactive = false;
		HeroBox.inactive = false;
		EnableBody(p);
	}

	internal static void EnableBody(PlayerSlot p)
	{
		Collider2D component = ((Component)p.Hero).GetComponent<Collider2D>();
		if (Object.op_Implicit((Object)(object)component))
		{
			((Behaviour)component).enabled = true;
		}
		HeroBox componentInChildren = ((Component)p.Hero).GetComponentInChildren<HeroBox>(true);
		if (Object.op_Implicit((Object)(object)componentInChildren))
		{
			Collider2D component2 = ((Component)componentInChildren).GetComponent<Collider2D>();
			if (Object.op_Implicit((Object)(object)component2))
			{
				((Behaviour)component2).enabled = true;
			}
		}
	}

	internal static void Reset(PlayerSlot p)
	{
		HeroController hero = p.Hero;
		if (!Object.op_Implicit((Object)(object)hero))
		{
			return;
		}
		((MonoBehaviour)hero).StopAllCoroutines();
		InvulnerablePulse component = ((Component)hero).GetComponent<InvulnerablePulse>();
		if (Object.op_Implicit((Object)(object)component))
		{
			component.stopInvulnerablePulse();
		}
		Reflect.Call(hero, "CancelAttack");
		PlayMakerFSM[] componentsInChildren = ((Component)hero).GetComponentsInChildren<PlayMakerFSM>(true);
		foreach (PlayMakerFSM val in componentsInChildren)
		{
			if (val.FsmName == "Spell Control" || val.FsmName == "Superdash" || val.FsmName == "Nail Arts" || val.FsmName == "Dream Nail")
			{
				val.SendEvent("CANCEL");
				FsmState val2 = val.Fsm.GetState("Inactive") ?? val.Fsm.GetState("Idle");
				if (val2 != null)
				{
					val.SetState(val2.Name);
				}
			}
		}
		hero.ResetState();
		hero.RegainControl();
		hero.StartAnimationControl();
		hero.AcceptInput();
		Physics(p);
		hero.SetDamageMode((DamageMode)0);
		Reflect.Set(hero, "tilemapTestActive", false);
		Reflect.Set(hero, "enteringVertically", false);
		Reflect.Set(hero, "airDashed", false);
		Reflect.Set(hero, "doubleJumped", false);
		Reflect.Set(hero, "exitedQuake", false);
		Reflect.Set(hero, "exitedSuperDashing", false);
		Reflect.Set(hero, "transitionState", (object)(HeroTransitionState)0);
		HeroAnimationController component2 = ((Component)hero).GetComponent<HeroAnimationController>();
		if (Object.op_Implicit((Object)(object)component2))
		{
			Reflect.Set(component2, "waitingToEnter", false);
		}
		HeroBox componentInChildren = ((Component)hero).GetComponentInChildren<HeroBox>(true);
		if (Object.op_Implicit((Object)(object)componentInChildren))
		{
			Reflect.Set(componentInChildren, "isHitBuffered", false);
		}
	}
}
