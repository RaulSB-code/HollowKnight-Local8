using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class FlowerRules
{
	private static float nextBlockedNotice;

	private static int SavedOwner
	{
		get
		{
			if (Local8Mod.Save != null)
			{
				return Local8Mod.Save.FlowerOwnerPlusOne;
			}
			return 0;
		}
	}

	private static void SetSavedOwner(int plusOne)
	{
		if (Local8Mod.Save != null)
		{
			Local8Mod.Save.FlowerOwnerPlusOne = Mathf.Clamp(plusOne, 0, 8);
		}
	}

	private static bool Raw(PlayerData data, string name)
	{
		if (data != null)
		{
			return Reflect.Get(data, name, fallback: false);
		}
		return false;
	}

	internal static bool Active(PlayerData data)
	{
		if (Raw(data, "hasXunFlower") && !Raw(data, "xunFlowerBroken"))
		{
			return !Raw(data, "xunFlowerGiven");
		}
		return false;
	}

	internal static PlayerSlot Carrier(CoopSession s)
	{
		if (s == null || !Active(s.Data))
		{
			return null;
		}
		int num = SavedOwner - 1;
		if (num >= 0 && num < s.Players.Count)
		{
			return s.Players[num];
		}
		return null;
	}

	internal static bool IsCarrier(PlayerSlot p)
	{
		CoopSession s = (((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (p != null)
		{
			return Carrier(s) == p;
		}
		return false;
	}

	internal static bool BlocksModTeleport(PlayerSlot p)
	{
		return IsCarrier(p);
	}

	internal static void Tick(CoopSession s)
	{
		if (s == null || s.Data == null)
		{
			return;
		}
		if (!Active(s.Data))
		{
			if (SavedOwner != 0)
			{
				SetSavedOwner(0);
			}
		}
		else if (Carrier(s) == null)
		{
			PlayerSlot playerSlot = InteractionRouter.ActivePlayer;
			if (playerSlot == null || !playerSlot.Alive)
			{
				playerSlot = s.Primary;
			}
			if (playerSlot != null)
			{
				SetSavedOwner(playerSlot.Index + 1);
				Diagnostics.Write("FLOWER legacy owner assigned P" + (playerSlot.Index + 1));
			}
		}
	}

	internal static bool HideFrom(PlayerData data, PlayerSlot actor)
	{
		if (actor == null || !Active(data))
		{
			return false;
		}
		PlayerSlot playerSlot = Carrier(((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (playerSlot == null || playerSlot == actor)
		{
			return false;
		}
		Scene activeScene = SceneManager.GetActiveScene();
		return !string.Equals(((Scene)(ref activeScene)).name, "Room_Mansion", StringComparison.OrdinalIgnoreCase);
	}

	internal static bool RejectBreak(PlayerData data, PlayerSlot actor, bool value)
	{
		if (!value || actor == null || !Active(data))
		{
			return false;
		}
		PlayerSlot playerSlot = Carrier(((Object)(object)Plugin.Self == (Object)null) ? null : Plugin.Self.Session);
		if (playerSlot == null || playerSlot == actor)
		{
			return false;
		}
		Diagnostics.Write("FLOWER ignored break from P" + (actor.Index + 1) + " carrier=P" + (playerSlot.Index + 1));
		return true;
	}

	internal static void BoolChanged(CoopSession s, PlayerData data, string name, bool oldValue, bool value, PlayerSlot actor)
	{
		if (s == null || data == null || s.Data != data || string.IsNullOrEmpty(name))
		{
			return;
		}
		if ((name == "hasXunFlower" && value && !oldValue) || (name == "xunFlowerBroken" && !value && oldValue && Raw(data, "hasXunFlower")))
		{
			PlayerSlot playerSlot = actor ?? InteractionRouter.ActivePlayer ?? s.Primary;
			if (playerSlot != null)
			{
				SetSavedOwner(playerSlot.Index + 1);
				EmergencyWarp.Cancel(playerSlot);
				NativeDreamFx.WhitePulse(playerSlot);
				Diagnostics.Write("FLOWER acquired by P" + (playerSlot.Index + 1));
			}
		}
		else if ((name == "hasXunFlower" && !value) || (name == "xunFlowerBroken" && value) || (name == "xunFlowerGiven" && value))
		{
			if (SavedOwner != 0)
			{
				Diagnostics.Write("FLOWER released P" + SavedOwner + " reason=" + name + "=" + value);
			}
			SetSavedOwner(0);
		}
	}

	internal static void WarnTeleportBlocked(PlayerSlot p)
	{
		if (p != null && !(Time.unscaledTime < nextBlockedNotice))
		{
			nextBlockedNotice = Time.unscaledTime + 2.5f;
			Local8Runtime self = Plugin.Self;
			if ((Object)(object)self != (Object)null)
			{
				self.Notice("P" + (p.Index + 1) + " lleva la Flor Palida: el teleporte de Local8 esta bloqueado.");
			}
			Diagnostics.Write("FLOWER teleport blocked P" + (p.Index + 1));
		}
	}
}
