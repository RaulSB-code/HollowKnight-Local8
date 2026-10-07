using UnityEngine;

namespace KO.HollowKnight8;

internal static class DuelGround
{
	private static readonly Collider2D[] hits = new Collider2D[256];

	private static readonly RaycastHit2D[] floorHits = new RaycastHit2D[32];

	internal static Bounds Body(PlayerSlot p)
	{
		BoxCollider2D component = p.Hero.GetComponent<BoxCollider2D>();
		if ((bool)component)
		{
			Vector3 lossyScale = component.transform.lossyScale;
			return new Bounds(component.transform.TransformPoint(component.offset), new Vector3(Mathf.Abs(component.size.x * lossyScale.x), Mathf.Abs(component.size.y * lossyScale.y), 0.1f));
		}
		Collider2D component2 = p.Hero.GetComponent<Collider2D>();
		if ((bool)component2 && component2.enabled && component2.bounds.size.y > 0.1f)
		{
			return component2.bounds;
		}
		return new Bounds(p.Hero.transform.position, new Vector3(0.8f, 2.4f, 0.1f));
	}

	internal static bool Safe(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		if (!Clear(p, at, offset, size, out reason))
		{
			return false;
		}
		Vector2 vector = at + offset;
		bool flag = false;
		float num = vector.y - size.y * 0.5f;
		for (int i = -1; i <= 1; i++)
		{
			if (flag)
			{
				break;
			}
			int num2 = Physics2D.RaycastNonAlloc(new Vector2(vector.x + (float)i * size.x * 0.25f, num + 0.15f), Vector2.down, floorHits, 0.65f, 256);
			for (int j = 0; j < num2; j++)
			{
				if ((bool)floorHits[j].collider && !floorHits[j].collider.isTrigger && floorHits[j].normal.y > 0.45f)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			reason = "sin suelo bajo los pies";
		}
		return flag;
	}

	internal static bool Clear(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		return ClearBody(p, at, offset, size, arena: false, out reason);
	}

	internal static bool ClearArena(PlayerSlot p, Vector3 at, out string reason)
	{
		Bounds bounds = Body(p);
		return ClearBody(p, at, bounds.center - p.Hero.transform.position, bounds.size, arena: true, out reason);
	}

	private static bool ClearBody(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, bool arena, out string reason)
	{
		reason = "";
		Vector2 point = at + offset;
		Vector2 size2 = new Vector2(Mathf.Max(0.1f, size.x - 0.14f), Mathf.Max(0.1f, size.y - 0.16f));
		int num = Physics2D.OverlapBoxNonAlloc(point, size2, 0f, hits);
		if (num == hits.Length)
		{
			reason = "zona demasiado ocupada";
			return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
		}
		for (int i = 0; i < num; i++)
		{
			Collider2D collider2D = hits[i];
			if (!collider2D || !collider2D.enabled || (bool)collider2D.GetComponentInParent<HeroController>())
			{
				continue;
			}
			if ((collider2D.gameObject.layer == 8 || collider2D.gameObject.layer == 25) && !collider2D.isTrigger)
			{
				reason = "pared: " + collider2D.name;
				return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
			}
			DamageHero component = collider2D.GetComponent<DamageHero>();
			if ((bool)component && (component.damageDealt > 0 || component.hazardType > 1) && (!arena || component.hazardType > 1))
			{
				reason = "peligro: " + collider2D.name + " type=" + component.hazardType;
				return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
			}
			if (FSMUtility.ContainsFSM(collider2D.gameObject, "damages_hero"))
			{
				PlayMakerFSM fsm = FSMUtility.LocateFSM(collider2D.gameObject, "damages_hero");
				if (!arena || FSMUtility.GetInt(fsm, "hazardType") > 1)
				{
					reason = "peligro FSM: " + collider2D.name;
					return SpawnSafety.Footprint(normal: false, p, at, offset, size, arena);
				}
			}
		}
		return SpawnSafety.Footprint(normal: true, p, at, offset, size, arena);
	}
}
