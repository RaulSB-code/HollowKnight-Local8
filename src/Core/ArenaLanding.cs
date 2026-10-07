using UnityEngine;

namespace KO.HollowKnight8;

internal static class ArenaLanding
{
	private static readonly RaycastHit2D[] floors = new RaycastHit2D[64];

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
				float x = ((j == 0) ? 0f : ((float)((j + 1) / 2) * 0.55f * (float)((j % 2 != 1) ? 1 : (-1))));
				Vector3 vector = occupied + new Vector3(x, (float)i * 0.22f, 0f);
				if (Clear(s, p, vector, out reason))
				{
					at = vector;
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
		Bounds bounds = DuelGround.Body(p);
		float num = bounds.extents.y - (bounds.center.y - p.Hero.transform.position.y);
		for (int i = 0; (float)i <= radius; i++)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				Vector2 origin2 = new Vector2(origin.x + (float)(i * j), origin.y + 3f);
				int num2 = Physics2D.RaycastNonAlloc(origin2, Vector2.down, floors, 16f, 33554688);
				float num3 = float.MaxValue;
				RaycastHit2D raycastHit2D = default(RaycastHit2D);
				for (int k = 0; k < num2; k++)
				{
					RaycastHit2D raycastHit2D2 = floors[k];
					if ((bool)raycastHit2D2.collider && !raycastHit2D2.collider.isTrigger && !(raycastHit2D2.normal.y < 0.6f) && !(raycastHit2D2.distance >= num3))
					{
						num3 = raycastHit2D2.distance;
						raycastHit2D = raycastHit2D2;
					}
				}
				if ((bool)raycastHit2D.collider)
				{
					Vector3 vector = new Vector3(origin2.x, raycastHit2D.point.y + num + 0.1f, origin.z);
					if (Clear(s, p, vector, out reason))
					{
						at = vector;
						return true;
					}
				}
			}
		}
		return false;
	}
}
