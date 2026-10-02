using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Lifeblood
{
	private sealed class Pickup
	{
		internal PlayerSlot Player;

		internal float When;
	}

	private sealed class HitRecord
	{
		internal PlayerSlot Player;

		internal Vector3 Position;

		internal float When;
	}

	private sealed class Scope : IDisposable
	{
		private readonly GameObject before;

		internal Scope(GameObject value)
		{
			before = source;
			source = value;
		}

		public void Dispose()
		{
			source = before;
		}
	}

	private static readonly Dictionary<int, PlayerSlot> killers = new Dictionary<int, PlayerSlot>();

	private static readonly Dictionary<int, float> awarded = new Dictionary<int, float>();

	private static readonly Dictionary<int, Pickup> collectors = new Dictionary<int, Pickup>();

	private static readonly Dictionary<Fsm, bool> pickupFsms = new Dictionary<Fsm, bool>();

	private static Pickup lastTouched;

	private static readonly List<HitRecord> recent = new List<HitRecord>();

	[ThreadStatic]
	private static GameObject source;

	private static bool IsSeed(GameObject go)
	{
		if (!Object.op_Implicit((Object)(object)go))
		{
			return false;
		}
		string name = ((Object)go).name;
		if (name.IndexOf("health scuttler", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("healthscuttler", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("life seed", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("lifeseed", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("lifeblood", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("blue health", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return name.IndexOf("bluehealth", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static bool IsPickup(Fsm f)
	{
		if (f == null)
		{
			return false;
		}
		if (pickupFsms.TryGetValue(f, out var value))
		{
			return value;
		}
		string text = (Object.op_Implicit((Object)(object)f.GameObject) ? ((Object)f.GameObject).name : "");
		string text2 = f.Name ?? "";
		value = Object.op_Implicit((Object)(object)f.GameObject) && (IsSeed(f.GameObject) || text2.IndexOf("blue health", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("lifeblood", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("health orb", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("health pickup", StringComparison.OrdinalIgnoreCase) >= 0);
		pickupFsms[f] = value;
		return value;
	}

	private static GameObject Root(GameObject go)
	{
		HealthManager val = (Object.op_Implicit((Object)(object)go) ? go.GetComponentInParent<HealthManager>() : null);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return go;
		}
		return ((Component)val).gameObject;
	}

	internal static void Hit(HealthManager hm, PlayerSlot p)
	{
		if (Object.op_Implicit((Object)(object)hm) && p != null && IsSeed(((Component)hm).gameObject))
		{
			killers[((Object)((Component)hm).gameObject).GetInstanceID()] = p;
			recent.Add(new HitRecord
			{
				Player = p,
				Position = ((Component)hm).transform.position,
				When = Time.unscaledTime
			});
			if (recent.Count > 24)
			{
				recent.RemoveAt(0);
			}
			Diagnostics.Write("LIFEBLOOD hit by P" + (p.Index + 1));
		}
	}

	private static PlayerSlot Killer(GameObject root)
	{
		if (killers.TryGetValue(((Object)root).GetInstanceID(), out var value) && value.Alive)
		{
			return value;
		}
		for (int num = recent.Count - 1; num >= 0; num--)
		{
			HitRecord hitRecord = recent[num];
			if (Time.unscaledTime - hitRecord.When > 12f)
			{
				break;
			}
			if (hitRecord.Player.Alive && Vector2.Distance(Vector2.op_Implicit(root.transform.position), Vector2.op_Implicit(hitRecord.Position)) < 6f)
			{
				killers[((Object)root).GetInstanceID()] = hitRecord.Player;
				return hitRecord.Player;
			}
		}
		return Plugin.Self.Session.Nearest(root.transform.position);
	}

	internal static PlayerSlot Track(Fsm f, Collider2D other)
	{
		if (!IsPickup(f) || !Object.op_Implicit((Object)(object)other))
		{
			return null;
		}
		PlayerSlot playerSlot = Plugin.Self.Session?.Resolve(other);
		if (playerSlot == null || !playerSlot.Alive)
		{
			return null;
		}
		int instanceID = ((Object)f.GameObject).GetInstanceID();
		if (!collectors.TryGetValue(instanceID, out var value) || value.Player != playerSlot || Time.unscaledTime - value.When > 1f)
		{
			Diagnostics.Write("LIFEBLOOD contact P" + (playerSlot.Index + 1) + " object=" + ((Object)f.GameObject).name + " fsm=" + f.Name);
		}
		collectors[instanceID] = new Pickup
		{
			Player = playerSlot,
			When = Time.unscaledTime
		};
		lastTouched = collectors[instanceID];
		return playerSlot;
	}

	private static PlayerSlot Collector(GameObject root)
	{
		if (Object.op_Implicit((Object)(object)root) && collectors.TryGetValue(((Object)root).GetInstanceID(), out var value) && value.Player.Alive && Time.unscaledTime - value.When < 3f)
		{
			return value.Player;
		}
		return null;
	}

	internal static PlayerSlot RecentCollector()
	{
		if (lastTouched == null || !lastTouched.Player.Alive || !(Time.unscaledTime - lastTouched.When < 0.8f))
		{
			return null;
		}
		return lastTouched.Player;
	}

	internal static PlayerSlot Resolve(Fsm f)
	{
		if (!IsPickup(f))
		{
			return null;
		}
		GameObject root = Root(f.GameObject);
		return Collector(f.GameObject) ?? Collector(root) ?? Killer(root);
	}

	internal static IDisposable Enter(Fsm f)
	{
		if (!IsPickup(f))
		{
			return null;
		}
		GameObject val = Root(f.GameObject);
		if (!((Object)(object)val != (Object)(object)source))
		{
			return null;
		}
		return new Scope(val);
	}

	internal static bool Award(FsmEvent evt)
	{
		if (!Object.op_Implicit((Object)(object)source) || evt == null || evt.Name != "ADD BLUE HEALTH")
		{
			return false;
		}
		int instanceID = ((Object)source).GetInstanceID();
		if (awarded.TryGetValue(instanceID, out var value) && Time.unscaledTime - value < 0.25f)
		{
			return true;
		}
		CoopSession session = Plugin.Self.Session;
		PlayerSlot playerSlot = Collector(source) ?? Killer(source);
		if (playerSlot == null)
		{
			return false;
		}
		awarded[instanceID] = Time.unscaledTime;
		using (PlayerContext.Enter(playerSlot))
		{
			PlayerData data = session.Data;
			data.healthBlue++;
		}
		playerSlot.HealFlashUntil = Time.unscaledTime + 0.45f;
		Diagnostics.Write("LIFEBLOOD awarded P" + (playerSlot.Index + 1) + " source=" + ((Object)source).name);
		return true;
	}

	internal static void Reset()
	{
		killers.Clear();
		awarded.Clear();
		collectors.Clear();
		pickupFsms.Clear();
		recent.Clear();
		source = null;
		lastTouched = null;
	}
}
