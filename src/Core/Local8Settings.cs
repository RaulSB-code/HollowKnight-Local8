using UnityEngine;

namespace KO.HollowKnight8;

public sealed class Local8Settings
{
	public bool Enabled = true;

	public bool TintPlayers = true;

	public bool ShowLabels = true;

	public bool HideOriginalHUD = true;

	public bool ZoomByPlayerCount = true;

	public bool TimedRespawn;

	public bool ReduceHitEffects = true;

	public bool BasicHUD;

	public bool GroupDarkness;

	public bool WaitForParty = true;

	public bool SpawnShades;

	public float SideMargin = 6f;

	public float HUDScale = 1f;

	public float MaxZoomOut = 1.7f;

	public float ZoomPerExtraPlayer = 0.08f;

	public float CameraSmooth = 0.25f;

	public float GatherDistance = 27f;

	public float RespawnSeconds = 15f;

	public float ReviveSeconds = 3f;

	public int Difficulty = 1;

	public int PvpMode;

	public int PvpNailDamage = 1;

	public int PvpSpellDamage = 2;

	public int PvpArtDamage = 2;

	public int PvpCharmDamage = 1;

	public int DuelSoul = 99;

	public int DuelSeconds = 0;

	public bool PvpParry = true;

	public bool PvpCharmAttacks = true;

	public bool PvpSoulOnHit = true;

	public int[] PvpTeams = new int[8];

	public string PrimaryDevice = "Keyboard";

	public string MenuKey = "F8";

	public string RescueKey = "Alpha1";

	public string PvpKey = "F6";

	public int RescueButton;

	public int DuelBestOf = 3;

	public string[] Colors = new string[8] { "D1EBFF", "66D9FF", "FF8C73", "8CFF99", "F2A6FF", "FFE066", "8C9EFF", "FFA3D6" };

	public string[] SkinIds = new string[8];

	public string[] ExtraKeyboardBindings;

	public string PvpArenaConfig;

	public bool RoundTimerInitialized;

	public void Ensure()
	{
		PvpMode = Mathf.Clamp(PvpMode, 0, 2);
		PvpNailDamage = Mathf.Clamp(PvpNailDamage, 1, 5);
		PvpSpellDamage = Mathf.Clamp(PvpSpellDamage, 1, 5);
		PvpArtDamage = Mathf.Clamp(PvpArtDamage, 1, 5);
		PvpCharmDamage = Mathf.Clamp(PvpCharmDamage, 1, 5);
		DuelSoul = Mathf.Clamp(DuelSoul, 0, 99);
		DuelSeconds = Mathf.Clamp(DuelSeconds, 0, 600);
		DuelBestOf = PvpRules.SeriesLength(DuelBestOf);
		RescueButton = Mathf.Clamp(RescueButton, 0, 3);
		if (PvpTeams == null || PvpTeams.Length != 8)
		{
			PvpTeams = new int[8];
		}
		for (int i = 0; i < 8; i++)
		{
			PvpTeams[i] = Mathf.Clamp(PvpTeams[i], 0, 4);
		}
		if (Colors == null || Colors.Length != 8)
		{
			Colors = new string[8] { "D1EBFF", "66D9FF", "FF8C73", "8CFF99", "F2A6FF", "FFE066", "8C9EFF", "FFA3D6" };
		}
		if (SkinIds == null || SkinIds.Length != 8)
		{
			SkinIds = new string[8];
		}
		HUDScale = Mathf.Clamp(HUDScale, 0.7f, 1.5f);
		MaxZoomOut = Mathf.Clamp(MaxZoomOut, 1f, 2.5f);
		ZoomPerExtraPlayer = Mathf.Clamp(ZoomPerExtraPlayer, 0f, 0.25f);
		SideMargin = Mathf.Clamp(SideMargin, 2.8f, 10f);
		CameraSmooth = Mathf.Clamp(CameraSmooth, 0.08f, 1f);
		GatherDistance = Mathf.Clamp(GatherDistance, 12f, 60f);
		RespawnSeconds = Mathf.Clamp(RespawnSeconds, 3f, 60f);
		ReviveSeconds = Mathf.Clamp(ReviveSeconds, 1.5f, 8f);
		Difficulty = Mathf.Clamp(Difficulty, 0, 3);
	}
}
