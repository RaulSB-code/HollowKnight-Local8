using UnityEngine;

namespace KO.HollowKnight8;

internal static class ArenaLanding
{
	private static readonly RaycastHit2D[] floors = (RaycastHit2D[])(object)new RaycastHit2D[64];

	internal static bool Clear(CoopSession s, PlayerSlot p, Vector3 at, out string reason)
	{
		reason = "fuera de escena";
		if (s.InRoom(at) && ArenaGather.Allows(at))
		{
			return DuelGround.ClearArena(p, at, out reason);
		}
		return false;
	}

	internal static bool Near(CoopSession s, PlayerSlot p, Vector3 occupied, out Vector3 at, out string reason)
	{
		at = occupied;
		reason = "sin espacio junto al jugador interior";
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				float num = ((j == 0) ? 0f : ((float)((j + 1) / 2) * 0.55f * (float)((j % 2 != 1) ? 1 : (-1))));
				Vector3 val = occupied + new Vector3(num, (float)i * 0.22f, 0f);
				if (Clear(s, p, val, out reason))
				{
					at = val;
					return true;
				}
			}
		}
		return false;
	}

	internal static bool Floor(CoopSession s, PlayerSlot p, Vector3 origin, float radius, out Vector3 at, out string reason)
	{
		at = Vector3.zero;
		reason = "sin suelo";
		Bounds val = DuelGround.Body(p);
		float num = ((Bounds)(ref val)).extents.y - (((Bounds)(ref val)).center.y - ((Component)p.Hero).transform.position.y);
		Vector2 val2 = default(Vector2);
		Vector3 val5 = default(Vector3);
		for (int i = 0; (float)i <= radius; i++)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				((Vector2)(ref val2))._002Ector(origin.x + (float)(i * j), origin.y + 3f);
				int num2 = Physics2D.RaycastNonAlloc(val2, Vector2.down, floors, 16f, 33554688);
				float num3 = float.MaxValue;
				RaycastHit2D val3 = default(RaycastHit2D);
				for (int k = 0; k < num2; k++)
				{
					RaycastHit2D val4 = floors[k];
					if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val4)).collider) && !((RaycastHit2D)(ref val4)).collider.isTrigger && !(((RaycastHit2D)(ref val4)).normal.y < 0.6f) && !(((RaycastHit2D)(ref val4)).distance >= num3))
					{
						num3 = ((RaycastHit2D)(ref val4)).distance;
						val3 = val4;
					}
				}
				if (Object.op_Implicit((Object)(object)((RaycastHit2D)(ref val3)).collider))
				{
					((Vector3)(ref val5))._002Ector(val2.x, ((RaycastHit2D)(ref val3)).point.y + num + 0.1f, origin.z);
					if (Clear(s, p, val5, out reason))
					{
						at = val5;
						return true;
					}
				}
			}
		}
		return false;
	}
}
