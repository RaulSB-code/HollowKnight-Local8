using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class DreamSequence
{
	private static string scene;

	private static bool gathered;

	private static float nextGather;

	private static float nextFallCheck;

	internal static bool InStoryDream
	{
		get
		{
			Scene activeScene = SceneManager.GetActiveScene();
			return string.Equals(((Scene)(ref activeScene)).name, "Dream_Nailcollection", StringComparison.OrdinalIgnoreCase);
		}
	}

	internal static void Reset()
	{
		if (!InStoryDream)
		{
			scene = null;
			gathered = false;
		}
	}

	internal static void Tick(CoopSession s)
	{
		if (s == null || !s.Active || s.Players.Count < 2)
		{
			return;
		}
		Scene activeScene = SceneManager.GetActiveScene();
		string name = ((Scene)(ref activeScene)).name;
		if (scene != name)
		{
			scene = name;
			gathered = false;
			nextGather = (nextFallCheck = 0f);
		}
		if (name == "RestingGrounds_04")
		{
			GatherAtShrine(s);
		}
		if (!InStoryDream || !s.Gameplay || Time.unscaledTime < nextFallCheck)
		{
			return;
		}
		nextFallCheck = Time.unscaledTime + 0.08f;
		for (int i = 0; i < s.Players.Count; i++)
		{
			PlayerSlot playerSlot = s.Players[i];
			if (playerSlot.Index < 0 || !playerSlot.Ready || !playerSlot.Alive || playerSlot.ArenaTransfer || EmergencyWarp.Active(playerSlot) || playerSlot.Hero.cState.transitioning || ((Component)playerSlot.Hero).transform.position.y >= 5f)
			{
				continue;
			}
			if (!s.Data.GetBool("hasDreamNail") && !gathered)
			{
				gathered = true;
				nextFallCheck = Time.unscaledTime + 4f;
				if (playerSlot.Index != 0)
				{
					Vector3 position = ((Component)playerSlot.Hero).transform.position;
					playerSlot = s.Players[0];
					((Component)playerSlot.Hero).transform.position = position;
				}
			}
			else
			{
				s.Hazard(playerSlot);
			}
		}
	}

	private static void GatherAtShrine(CoopSession s)
	{
		if (gathered || Time.unscaledTime < nextGather || s.Data == null || s.Data.GetBool("hasDreamNail"))
		{
			return;
		}
		nextGather = Time.unscaledTime + 0.25f;
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(56f, 8f, 0f);
		PlayerSlot playerSlot = s.Nearest(val);
		if (playerSlot == null || !playerSlot.Ready || !playerSlot.Alive || !Object.op_Implicit((Object)(object)playerSlot.Hero))
		{
			return;
		}
		Vector3 position = ((Component)playerSlot.Hero).transform.position;
		if (position.x < 53f || position.x > 59f || position.y < 6f || position.y > 10f)
		{
			return;
		}
		for (int i = 0; i < s.Players.Count; i++)
		{
			PlayerSlot playerSlot2 = s.Players[i];
			if (playerSlot2 != playerSlot && playerSlot2.Ready && playerSlot2.Alive && Object.op_Implicit((Object)(object)playerSlot2.Hero))
			{
				float num = ((playerSlot2.Index % 2 == 0) ? 1f : (-1f)) * (0.85f + 0.45f * (float)((playerSlot2.Index - 1) / 2));
				((Vector3)(ref val))._002Ector(55.5f + num, 7.4f, (float)CoopRules.PlayerDepth(playerSlot2.Index));
				Vector3 position2 = ((Component)playerSlot2.Hero).transform.position;
				NativeDreamFx.TeleportTrail(playerSlot2, position2, val);
				((Component)playerSlot2.Hero).transform.position = val;
				Rigidbody2D component = ((Component)playerSlot2.Hero).GetComponent<Rigidbody2D>();
				if (Object.op_Implicit((Object)(object)component))
				{
					component.velocity = Vector2.zero;
				}
				playerSlot2.SafePoint = val;
				playerSlot2.HasSafePoint = true;
				playerSlot2.SafeAt = Time.unscaledTime;
			}
		}
		gathered = true;
	}
}
