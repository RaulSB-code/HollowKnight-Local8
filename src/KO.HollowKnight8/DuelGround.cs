using UnityEngine;

namespace KO.HollowKnight8;

internal static class DuelGround
{
	private static readonly Collider2D[] hits = (Collider2D[])(object)new Collider2D[256];

	private static readonly RaycastHit2D[] floorHits = (RaycastHit2D[])(object)new RaycastHit2D[32];

	internal static Bounds Body(PlayerSlot p)
	{
		BoxCollider2D component = ((Component)p.Hero).GetComponent<BoxCollider2D>();
		if (Object.op_Implicit((Object)(object)component))
		{
			Vector3 lossyScale = ((Component)component).transform.lossyScale;
			return new Bounds(((Component)component).transform.TransformPoint(Vector2.op_Implicit(((Collider2D)component).offset)), new Vector3(Mathf.Abs(component.size.x * lossyScale.x), Mathf.Abs(component.size.y * lossyScale.y), 0.1f));
		}
		Collider2D component2 = ((Component)p.Hero).GetComponent<Collider2D>();
		if (Object.op_Implicit((Object)(object)component2) && ((Behaviour)component2).enabled)
		{
			Bounds bounds = component2.bounds;
			if (((Bounds)(ref bounds)).size.y > 0.1f)
			{
				return component2.bounds;
			}
		}
		return new Bounds(((Component)p.Hero).transform.position, new Vector3(0.8f, 2.4f, 0.1f));
	}

	internal static bool Safe(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, out string reason)
	{
		if (!Clear(p, at, offset, size, out reason))
		{
			return false;
		}
		Vector2 val = Vector2.op_Implicit(at + offset);
		bool flag = false;
		float num = val.y - size.y * 0.5f;
		for (int i = -1; i <= 1; i++)
		{
			if (flag)
			{
				break;
			}
			int num2 = Physics2D.RaycastNonAlloc(new Vector2(val.x + (float)i * size.x * 0.25f, num + 0.15f), Vector2.down, floorHits, 0.65f, 256);
			for (int j = 0; j < num2; j++)
			{
				if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref floorHits[j])).collider) && !((RaycastHit2D)(ref floorHits[j])).collider.isTrigger && ((RaycastHit2D)(ref floorHits[j])).normal.y > 0.45f)
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
		Bounds val = Body(p);
		return ClearBody(p, at, ((Bounds)(ref val)).center - ((Component)p.Hero).transform.position, ((Bounds)(ref val)).size, arena: true, out reason);
	}

	private static bool ClearBody(PlayerSlot p, Vector3 at, Vector3 offset, Vector3 size, bool arena, out string reason)
	{
		reason = "";
		Vector2 val = Vector2.op_Implicit(at + offset);
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector(Mathf.Max(0.1f, size.x - 0.14f), Mathf.Max(0.1f, size.y - 0.16f));
		int num = Physics2D.OverlapBoxNonAlloc(val, val2, 0f, hits);
		if (num == hits.Length)
		{
			reason = "zona demasiado ocupada";
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			Collider2D val3 = hits[i];
			if (!Object.op_Implicit((Object)(object)val3) || !((Behaviour)val3).enabled || Object.op_Implicit((Object)(object)((Component)val3).GetComponentInParent<HeroController>()))
			{
				continue;
			}
			if ((((Component)val3).gameObject.layer == 8 || ((Component)val3).gameObject.layer == 25) && !val3.isTrigger)
			{
				reason = "pared: " + ((Object)val3).name;
				return false;
			}
			DamageHero component = ((Component)val3).GetComponent<DamageHero>();
			if (Object.op_Implicit((Object)(object)component) && (component.damageDealt > 0 || component.hazardType > 1) && (!arena || component.hazardType > 1))
			{
				reason = "peligro: " + ((Object)val3).name + " type=" + component.hazardType;
				return false;
			}
			if (FSMUtility.ContainsFSM(((Component)val3).gameObject, "damages_hero"))
			{
				PlayMakerFSM val4 = FSMUtility.LocateFSM(((Component)val3).gameObject, "damages_hero");
				if (!arena || FSMUtility.GetInt(val4, "hazardType") > 1)
				{
					reason = "peligro FSM: " + ((Object)val3).name;
					return false;
				}
			}
		}
		return true;
	}
}
