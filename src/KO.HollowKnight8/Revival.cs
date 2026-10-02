using System;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Revival
{
	internal static void Tick(CoopSession s, PlayerSlot[] live)
	{
		PlayerSlot playerSlot = (from p in s.Players
			where p.Down
			orderby p.DownAt
			select p).FirstOrDefault();
		foreach (PlayerSlot playerSlot2 in live)
		{
			bool flag = playerSlot2.Actions != null && (((OneAxisInputControl)playerSlot2.Actions.cast).IsPressed || ((OneAxisInputControl)playerSlot2.Actions.focus).IsPressed);
			if (!flag)
			{
				playerSlot2.FocusReleased = true;
			}
			bool flag2 = ((playerSlot2.Vitals.Joni > 0) ? (playerSlot2.Vitals.Blue >= playerSlot2.Vitals.Joni) : (playerSlot2.Vitals.Health >= playerSlot2.CurrentMaxHealth));
			PlayerSlot playerSlot3 = (playerSlot2.Reviving ? playerSlot2.ReviveTarget : playerSlot);
			int num2 = (playerSlot2.Reviving ? playerSlot2.ReviveSoulCost : CoopRules.RevivalCost(playerSlot2.Vitals.MaxSoul));
			bool flag3 = playerSlot2.Reviving || playerSlot2.Vitals.Soul >= num2;
			HeroControllerStates cState = playerSlot2.Hero.cState;
			if (!((playerSlot3?.Down ?? false) && flag && flag2 && flag3) || !playerSlot2.FocusReleased || !playerSlot2.Connected || !cState.onGround || cState.recoiling || cState.recoilFrozen || cState.transitioning || cState.dashing || cState.attacking || playerSlot2.Hero.controlReqlinquished || !(Math.Abs(((TwoAxisInputControl)playerSlot2.Actions.moveVector).X) < 0.2f))
			{
				End(playerSlot2);
				continue;
			}
			if (!playerSlot2.Reviving)
			{
				playerSlot2.Reviving = true;
				playerSlot2.ReviveTarget = playerSlot3;
				playerSlot2.ReviveSoulCost = num2;
				playerSlot2.ReviveSoulSpent = 0;
				playerSlot2.FocusHeld = 0f;
				cState.focusing = true;
				playerSlot2.Hero.StopAnimationControl();
				Rigidbody2D component = ((Component)playerSlot2.Hero).GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component))
				{
					component.velocity = Vector2.zero;
				}
				playerSlot2.ReviveAnimator = ((Component)playerSlot2.Hero).GetComponent<tk2dSpriteAnimator>();
				if (Object.op_Implicit((Object)(object)playerSlot2.ReviveAnimator) && Object.op_Implicit((Object)(object)playerSlot2.ReviveAnimator.Library))
				{
					tk2dSpriteAnimationClip val = ((IEnumerable<tk2dSpriteAnimationClip>)playerSlot2.ReviveAnimator.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip x) => x.name == "Focus")) ?? ((IEnumerable<tk2dSpriteAnimationClip>)playerSlot2.ReviveAnimator.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip x) => x.name.IndexOf("focus", StringComparison.OrdinalIgnoreCase) >= 0 && x.name.IndexOf("end", StringComparison.OrdinalIgnoreCase) < 0));
					if (val != null)
					{
						playerSlot2.ReviveAnimator.Play(val);
					}
				}
				Diagnostics.Write("REVIVE START P" + (playerSlot2.Index + 1) + " -> P" + (playerSlot3.Index + 1) + " soul_cost=" + num2 + " hp=" + playerSlot2.Vitals.Health + " reserve=" + playerSlot2.Vitals.Reserve);
			}
			playerSlot2.FocusHeld += Time.deltaTime;
			playerSlot2.ReviveProgress = Mathf.Clamp01((playerSlot2.FocusHeld - 0.2f) / Mathf.Max(1.5f, Plugin.Self.ReviveSeconds.Value));
			int num3 = CoopRules.RevivalSpent(num2, playerSlot2.ReviveProgress);
			int num4 = num3 - playerSlot2.ReviveSoulSpent;
			if (playerSlot2.Vitals.Soul < num4)
			{
				End(playerSlot2);
				playerSlot2.FocusReleased = false;
				continue;
			}
			if (num4 > 0)
			{
				playerSlot2.Vitals.Soul -= num4;
				playerSlot2.ReviveSoulSpent = num3;
				s.Commit(playerSlot2);
			}
			if (!(playerSlot2.ReviveProgress < 1f))
			{
				End(playerSlot2);
				playerSlot2.FocusReleased = false;
				if (!Object.op_Implicit((Object)(object)playerSlot3.Hero))
				{
					playerSlot3.Down = false;
					playerSlot3.Vitals.Health = Math.Max(1, (playerSlot3.CurrentMaxHealth + 1) / 2);
					playerSlot3.RetryAt = 0f;
				}
				else
				{
					playerSlot3.SafePoint = ((Component)playerSlot2.Hero).transform.position;
					playerSlot3.HasSafePoint = true;
					s.Recover(playerSlot3, respawn: true, ((Component)playerSlot2.Hero).transform.position);
				}
				playerSlot3.HealFlashUntil = Time.unscaledTime + 0.7f;
				Plugin.Self.Notice("P" + (playerSlot2.Index + 1) + " ha reanimado a P" + (playerSlot3.Index + 1) + ".");
				Diagnostics.Write("REVIVE COMPLETE P" + (playerSlot3.Index + 1) + " by P" + (playerSlot2.Index + 1) + " soul=" + playerSlot2.Vitals.Soul + " hp=" + playerSlot2.Vitals.Health + " reserve=" + playerSlot2.Vitals.Reserve);
				for (int num5 = 0; num5 < live.Length; num5++)
				{
					End(live[num5]);
				}
				break;
			}
		}
	}

	internal static void End(PlayerSlot p)
	{
		if (p == null)
		{
			return;
		}
		if (p.Reviving && Object.op_Implicit((Object)(object)p.Hero))
		{
			p.Hero.cState.focusing = false;
			p.Hero.StartAnimationControl();
			if (Object.op_Implicit((Object)(object)p.ReviveAnimator) && p.ReviveAnimator.GetClipByName("Idle") != null)
			{
				p.ReviveAnimator.Play("Idle");
			}
		}
		p.Reviving = false;
		p.ReviveProgress = 0f;
		p.FocusHeld = 0f;
		p.ReviveTarget = null;
		p.ReviveSoulCost = (p.ReviveSoulSpent = 0);
	}
}
