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

	internal static bool InStoryDream => string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Dream_Nailcollection", StringComparison.OrdinalIgnoreCase);

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
		//Discarded unreachable code: IL_019f
		if (s == null || !s.Active || s.Players.Count < 2)
		{
			return;
		}
		string name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
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
			if (playerSlot.Index < 0 || !playerSlot.Ready || !playerSlot.Alive || playerSlot.ArenaTransfer || EmergencyWarp.Active(playerSlot) || playerSlot.Hero.cState.transitioning || playerSlot.Hero.transform.position.y >= 5f)
			{
				continue;
			}
			if (!s.Data.GetBool("hasDreamNail") && !gathered)
			{
				gathered = true;
				nextFallCheck = Time.unscaledTime + 4f;
				if (playerSlot.Index != 0)
				{
					Vector3 position = playerSlot.Hero.transform.position;
					playerSlot = s.Players[0];
					playerSlot.Hero.transform.position = position;
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
		Vector3 point = new Vector3(56f, 8f, 0f);
		PlayerSlot playerSlot = s.Nearest(point);
		if (playerSlot == null || !playerSlot.Ready || !playerSlot.Alive || !playerSlot.Hero)
		{
			return;
		}
		Vector3 position = playerSlot.Hero.transform.position;
		if (position.x < 53f || position.x > 59f || position.y < 6f || position.y > 10f)
		{
			return;
		}
		for (int i = 0; i < s.Players.Count; i++)
		{
			PlayerSlot playerSlot2 = s.Players[i];
			if (playerSlot2 != playerSlot && playerSlot2.Ready && playerSlot2.Alive && (bool)playerSlot2.Hero)
			{
				float num = ((playerSlot2.Index % 2 == 0) ? 1f : (-1f)) * (0.85f + 0.45f * (float)((playerSlot2.Index - 1) / 2));
				point = new Vector3(55.5f + num, 7.4f, (float)CoopRules.PlayerDepth(playerSlot2.Index));
				Vector3 position2 = playerSlot2.Hero.transform.position;
				NativeDreamFx.TeleportTrail(playerSlot2, position2, point);
				playerSlot2.Hero.transform.position = point;
				Rigidbody2D component = playerSlot2.Hero.GetComponent<Rigidbody2D>();
				if ((bool)component)
				{
					component.velocity = Vector2.zero;
				}
				playerSlot2.SafePoint = point;
				playerSlot2.HasSafePoint = true;
				playerSlot2.SafeAt = Time.unscaledTime;
			}
		}
		gathered = true;
	}
}
