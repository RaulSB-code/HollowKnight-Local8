using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ActorRecovery
{
	internal static void Freeze(PlayerSlot p)
	{
		if (p != null && (bool)p.Hero)
		{
			Rigidbody2D component = p.Hero.GetComponent<Rigidbody2D>();
			if ((bool)component)
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
		Rigidbody2D component = hero.GetComponent<Rigidbody2D>();
		float num = ((hero.DEFAULT_GRAVITY > 0f) ? hero.DEFAULT_GRAVITY : 1f);
		Reflect.Set(hero, "prevGravityScale", num);
		if ((bool)component)
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
		Collider2D component = p.Hero.GetComponent<Collider2D>();
		if ((bool)component)
		{
			component.enabled = true;
		}
		HeroBox componentInChildren = p.Hero.GetComponentInChildren<HeroBox>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			Collider2D component2 = componentInChildren.GetComponent<Collider2D>();
			if ((bool)component2)
			{
				component2.enabled = true;
			}
		}
	}

	internal static void Reset(PlayerSlot p)
	{
		HeroController hero = p.Hero;
		if (!hero)
		{
			return;
		}
		hero.StopAllCoroutines();
		InvulnerablePulse component = hero.GetComponent<InvulnerablePulse>();
		if ((bool)component)
		{
			component.stopInvulnerablePulse();
		}
		Reflect.Call(hero, "CancelAttack");
		PlayMakerFSM[] componentsInChildren = hero.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
		foreach (PlayMakerFSM playMakerFSM in componentsInChildren)
		{
			if (playMakerFSM.FsmName == "Spell Control" || playMakerFSM.FsmName == "Superdash" || playMakerFSM.FsmName == "Nail Arts" || playMakerFSM.FsmName == "Dream Nail")
			{
				playMakerFSM.SendEvent("CANCEL");
				FsmState fsmState = playMakerFSM.Fsm.GetState("Inactive") ?? playMakerFSM.Fsm.GetState("Idle");
				if (fsmState != null)
				{
					playMakerFSM.SetState(fsmState.Name);
				}
			}
		}
		hero.ResetState();
		hero.RegainControl();
		hero.StartAnimationControl();
		hero.AcceptInput();
		Physics(p);
		hero.SetDamageMode(DamageMode.FULL_DAMAGE);
		Reflect.Set(hero, "tilemapTestActive", false);
		Reflect.Set(hero, "enteringVertically", false);
		Reflect.Set(hero, "airDashed", false);
		Reflect.Set(hero, "doubleJumped", false);
		Reflect.Set(hero, "exitedQuake", false);
		Reflect.Set(hero, "exitedSuperDashing", false);
		Reflect.Set(hero, "transitionState", HeroTransitionState.WAITING_TO_TRANSITION);
		HeroAnimationController component2 = hero.GetComponent<HeroAnimationController>();
		if ((bool)component2)
		{
			Reflect.Set(component2, "waitingToEnter", false);
		}
		HeroBox componentInChildren = hero.GetComponentInChildren<HeroBox>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			Reflect.Set(componentInChildren, "isHitBuffered", false);
		}
	}
}
