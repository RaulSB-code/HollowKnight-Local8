using UnityEngine;

namespace KO.HollowKnight8;

internal static class SoulReserve
{
	internal static void Tick(PlayerSlot p)
	{
		if (!p.Alive || !p.Ready || p.Reviving || p.Hero.cState.focusing || p.Hero.controlReqlinquished || BossSequenceController.BoundSoul)
		{
			p.ReserveClock = 0f;
			return;
		}
		p.ReserveClock = Mathf.Min(0.25f, p.ReserveClock + Time.deltaTime);
		int num = Mathf.FloorToInt(p.ReserveClock * 30f);
		if (num != 0)
		{
			p.ReserveClock -= (float)num / 30f;
			int num2 = RecoveryRules.ReserveTransfer(p.Vitals.Soul, p.Vitals.MaxSoul, p.Vitals.Reserve, num);
			if (num2 != 0)
			{
				p.Vitals.Reserve -= num2;
				p.Vitals.Soul += num2;
				Plugin.Self.Session.Commit(p);
			}
		}
	}
}
