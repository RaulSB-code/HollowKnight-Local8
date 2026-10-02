using System;
using System.Collections.Generic;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal sealed class PlayerSlot
{
	internal int Index;

	internal HeroController Hero;

	internal HeroActions Actions;

	internal bool OwnsActions;

	internal bool InputBlocked;

	internal float WakeUntil;

	internal float JoinDelayAt;

	internal float JoinVisualUntil;

	internal Vitals Vitals = new Vitals();

	internal CharmLoadout Charms = new CharmLoadout();

	internal InputDevice Device;

	internal string DeviceKey;

	internal string DeviceName;

	internal bool Connected;

	internal bool Down;

	internal bool Hazard;

	internal bool Ready;

	internal bool Retiring;

	internal bool SpawnPending;

	internal bool Faulted;

	internal int RecoveryErrors;

	internal float RetryAt;

	internal int LastInputFrame = -1;

	internal float DownAt;

	internal float HazardUntil;

	internal float ProtectionUntil;

	internal float FarSince = -1f;

	internal float LifeStartedAt;

	internal float SafeAt;

	internal Vector3 SafePoint;

	internal bool HasSafePoint;

	internal Vector3 PreviousSafePoint;

	internal bool HasPreviousSafePoint;

	internal bool HeroBoxInactive;

	internal float ReserveClock;

	internal float NextSafeCheck;

	internal float NextSkinUpdate;

	internal float NextCharmUpdate;

	internal Color Color;

	internal string SkinId;

	internal string AppliedSkinId;

	internal bool SkinApplied;

	internal Renderer SkinRenderer;

	internal Material SkinMaterial;

	internal Material SkinBaseMaterial;

	internal MaterialPropertyBlock SkinOriginalProperties;

	internal Texture2D SkinTexture;

	internal Texture SkinSourceTexture;

	internal bool SkinSuspended;

	internal Vector3 LastHazardPosition;

	internal bool AcidAssistActive;

	internal float ReviveProgress;

	internal float DamageFlashUntil;

	internal bool Reviving;

	internal bool FocusReleased = true;

	internal float FocusHeld;

	internal float HealFlashUntil;

	internal PlayerSlot ReviveTarget;

	internal int ReviveSoulCost;

	internal int ReviveSoulSpent;

	internal int HealthBeforeHit;

	internal float LastDamageAt = -10f;

	internal bool SpawnAsDown;

	internal float InvisibleSince = -1f;

	internal tk2dSpriteAnimator ReviveAnimator;

	internal HeroController LightOwner;

	internal Renderer[] LightRenderers;

	internal float ArenaAlpha = 1f;

	internal bool ArenaTransfer;

	internal Collider2D[] Colliders;

	internal Renderer[] Renderers;

	internal readonly Dictionary<Collider2D, bool> ColliderStates = new Dictionary<Collider2D, bool>();

	internal readonly Dictionary<Renderer, bool> RendererStates = new Dictionary<Renderer, bool>();

	internal int RootLayer;

	internal int CurrentMaxHealth
	{
		get
		{
			if (!BossSequenceController.BoundShell)
			{
				return Math.Max(1, Vitals.MaxHealth);
			}
			return Math.Min(Math.Max(1, Vitals.MaxHealth), BossSequenceController.BoundMaxHealth);
		}
	}

	internal bool Alive
	{
		get
		{
			if (Object.op_Implicit((Object)(object)Hero) && !Down && !Hazard && !Retiring)
			{
				return ((Component)Hero).gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	internal void Capture(PlayerData data)
	{
		Vitals.Read(data);
		Charms.Read(data);
	}

	internal void Apply(PlayerData data)
	{
		Vitals.Write(data);
		Charms.Write(data);
	}
}
