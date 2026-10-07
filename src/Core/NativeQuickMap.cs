using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class NativeQuickMap
{
	private static PlayerSlot owner;

	internal static PlayerSlot Owner => owner;

	internal static void Observe(CoopSession s)
	{
		if (s == null || !s.Active || !s.Gameplay || Plugin.Self.Panel)
		{
			Reset();
		}
		else if (owner != null && (!owner.Alive || !owner.Ready || owner.Actions == null))
		{
			Reset();
		}
		else
		{
			if (owner != null)
			{
				return;
			}
			foreach (PlayerSlot player in s.Players)
			{
				if (player.Alive && player.Ready && player.Actions != null && player.Actions.quickMap.WasPressed)
				{
					owner = player;
					Diagnostics.Write("MAP vanilla P" + (player.Index + 1));
					break;
				}
			}
		}
	}

	internal static void AfterListener()
	{
		if (owner != null && owner.Actions != null && !owner.Actions.quickMap.IsPressed)
		{
			Reset();
		}
	}

	internal static void MapUpdate(GameMap map, On.GameMap.orig_Update orig)
	{
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (owner == null || coopSession == null || !coopSession.Active || !owner.Alive)
		{
			orig(map);
			return;
		}
		Reflect.Set(map, "hero", owner.Hero.gameObject);
		using (PlayerContext.Enter(owner))
		{
			orig(map);
		}
	}

	internal static void Reset()
	{
		if (owner == null)
		{
			return;
		}
		CoopSession coopSession = ((Plugin.Self == null) ? null : Plugin.Self.Session);
		if (coopSession != null && coopSession.Primary != null && (bool)coopSession.Primary.Hero)
		{
			GameMap[] array = Object.FindObjectsOfType<GameMap>();
			foreach (GameMap gameMap in array)
			{
				if ((bool)gameMap)
				{
					Reflect.Set(gameMap, "hero", coopSession.Primary.Hero.gameObject);
				}
			}
		}
		owner = null;
	}
}
