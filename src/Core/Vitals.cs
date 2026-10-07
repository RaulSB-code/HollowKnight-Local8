using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class Vitals
{
	internal int Health;

	internal int Blue;

	internal int Soul;

	internal int Reserve;

	internal int MaxSoul;

	internal int PreviousHealth;

	internal int BlockerHits;

	internal int MaxHealth;

	internal int Joni;

	internal int FocusCost;

	internal bool Invincible;

	internal bool DisablePause;

	internal bool SoulLimited;

	internal bool DamagedBlue;

	internal bool AtBench;

	internal Vector3 HazardPoint;

	internal bool HazardRight;

	internal void Read(PlayerData p)
	{
		Health = p.health;
		Blue = p.healthBlue;
		Soul = p.MPCharge;
		Reserve = p.MPReserve;
		MaxSoul = p.maxMP;
		PreviousHealth = p.prevHealth;
		BlockerHits = p.blockerHits;
		Invincible = p.isInvincible;
		SoulLimited = p.soulLimited;
		HazardPoint = p.hazardRespawnLocation;
		HazardRight = p.hazardRespawnFacingRight;
		MaxHealth = p.maxHealth;
		Joni = p.joniHealthBlue;
		FocusCost = p.focusMP_amount;
		DamagedBlue = p.damagedBlue;
		AtBench = p.atBench;
	}

	internal void Write(PlayerData p)
	{
		p.health = Health;
		p.healthBlue = Blue;
		p.MPCharge = Soul;
		p.MPReserve = Reserve;
		p.maxMP = MaxSoul;
		p.prevHealth = PreviousHealth;
		p.blockerHits = BlockerHits;
		p.isInvincible = Invincible;
		p.soulLimited = SoulLimited;
		p.hazardRespawnLocation = HazardPoint;
		p.hazardRespawnFacingRight = HazardRight;
		p.maxHealth = MaxHealth;
		p.joniHealthBlue = Joni;
		p.focusMP_amount = FocusCost;
		p.damagedBlue = DamagedBlue;
		p.atBench = AtBench;
	}
}
