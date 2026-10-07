using System.Collections.Generic;
using GlobalEnums;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class EmergencyWarp
{
	internal sealed class Trip
	{
		internal PlayerSlot Player;

		internal HeroController Hero;

		internal Vector3 Origin;

		internal Vector3 Position;

		internal GameObject Effect;

		internal float Elapsed;

		internal bool PreviousHeld;

		internal readonly Dictionary<Renderer, bool> Renderers = new Dictionary<Renderer, bool>();

		internal readonly Dictionary<Collider2D, bool> Colliders = new Dictionary<Collider2D, bool>();
	}

	private static readonly float[] held = new float[8];

	internal static readonly float[] cooldown = new float[8];

	private static readonly bool[] released = new bool[8];

	private static readonly float[] airborneAt = new float[8];

	private static readonly Vector2[] airborneAnchor = new Vector2[8];

	internal static readonly Trip[] trips = new Trip[8];

	internal static bool Active(PlayerSlot p)
	{
		if (p != null && p.Index >= 0 && p.Index < 8)
		{
			return trips[p.Index] != null;
		}
		return false;
	}

	internal static bool Holding(PlayerSlot p)
	{
		if (ActionKeys.Held(p) && !PvpMatch.Running)
		{
			return !FlowerRules.BlocksModTeleport(p);
		}
		return false;
	}

	private static bool AirborneStuck(PlayerSlot p)
	{
		int index = p.Index;
		Vector2 vector = p.Hero.transform.position;
		if (p.Hero.cState.onGround)
		{
			airborneAt[index] = 0f;
			return false;
		}
		float unscaledTime = Time.unscaledTime;
		if (airborneAt[index] <= 0f || Vector2.Distance(airborneAnchor[index], vector) > 2f)
		{
			airborneAt[index] = unscaledTime;
			airborneAnchor[index] = vector;
			return false;
		}
		return unscaledTime - airborneAt[index] >= 1.5f;
	}

	internal static void Tick(CoopSession s, PlayerSlot[] live)
	{
		DreamRescueFeedback.Before(s, live);
		foreach (PlayerSlot player in s.Players)
		{
			int index = player.Index;
			Trip trip = trips[index];
			bool flag = ActionKeys.Held(player);
			if (trip == null && flag && FlowerRules.BlocksModTeleport(player))
			{
				held[index] = 0f;
				released[index] = false;
				FlowerRules.WarnTeleportBlocked(player);
				continue;
			}
			if (trip != null)
			{
				if (!player.Alive || !player.Ready || player.Hero != trip.Hero || PvpMatch.Running)
				{
					Cancel(player);
				}
				else if (!DreamRescueArrival.Advance(s, trip))
				{
					trip.Elapsed += Time.unscaledDeltaTime;
					Vector2 vector = Vector2.zero;
					if (player.Actions != null)
					{
						vector = new Vector2(player.Actions.right.Value - player.Actions.left.Value, player.Actions.up.Value - player.Actions.down.Value);
					}
					trip.Position = CameraPresenceGuard.RescueMove(trip.Position, (Vector3)Vector2.ClampMagnitude(vector, 1f) * (22f * Time.unscaledDeltaTime), player);
					GameManager instance = GameManager.instance;
					if ((bool)instance && (bool)instance.cameraCtrl)
					{
						trip.Position.x = Mathf.Clamp(trip.Position.x, 0.75f, Mathf.Max(0.75f, instance.cameraCtrl.sceneWidth - 0.75f));
						trip.Position.y = Mathf.Clamp(trip.Position.y, 0.75f, Mathf.Max(0.75f, instance.cameraCtrl.sceneHeight - 0.75f));
					}
					trip.Hero.transform.position = trip.Position;
					if ((bool)trip.Effect)
					{
						trip.Effect.transform.position = trip.Position;
					}
					ActorRecovery.Freeze(player);
					Hide(trip);
					bool num = flag && !trip.PreviousHeld && trip.Elapsed > 0.25f;
					trip.PreviousHeld = flag;
					if (num || trip.Elapsed >= 4f)
					{
						Finish(s, trip);
					}
				}
				continue;
			}
			bool flag2 = (bool)player.Hero && player.Ready && player.Alive && AirborneStuck(player);
			if (!flag)
			{
				held[index] = 0f;
				released[index] = true;
				continue;
			}
			GameManager instance2 = GameManager.instance;
			UIManager instance3 = UIManager.instance;
			bool flag3 = instance2 == null || instance2.isPaused || !s.Gameplay || instance3 == null || instance3.uiState != UIState.PLAYING || Charms.NativeMenuOpen || ScriptedParty.Active || PickupCard.Owner != null || InteractionRouter.ActivePlayer != null || ShopMenuRouting.Buyer != null || StagMenuRouting.HasOwner;
			if (!released[index] || flag3 || PvpMatch.Running || CoopEnding.Active || ArenaGather.TransferActive || live.Length < 2 || !player.Alive || !player.Ready || !player.Connected || player.InputBlocked || player.Reviving || player.Vitals.AtBench || ((player.Hero.cState.transitioning || player.Hero.controlReqlinquished) && !flag2) || Time.unscaledTime < cooldown[index])
			{
				held[index] = 0f;
				continue;
			}
			held[index] += Time.unscaledDeltaTime;
			if (!(held[index] < 0.35f))
			{
				held[index] = 0f;
				released[index] = false;
				Start(s, player);
			}
		}
		DreamRescueFeedback.After(s, live);
	}

	private static void Start(CoopSession s, PlayerSlot p)
	{
		GameObject gameObject = NativeDreamFx.Create(p, p.Hero.transform.position);
		if (!gameObject)
		{
			CameraPresenceGuard.BeginRescue(s, p);
			return;
		}
		Trip trip = new Trip
		{
			Player = p,
			Hero = p.Hero,
			Origin = p.Hero.transform.position,
			Position = p.Hero.transform.position,
			Effect = gameObject,
			PreviousHeld = true
		};
		trips[p.Index] = trip;
		cooldown[p.Index] = Time.unscaledTime + 25f;
		Revival.End(p);
		NativeDreamFx.PlayActivation(trip.Position);
		using (PlayerContext.Enter(p))
		{
			ActorRecovery.Reset(p);
			p.Hero.StopAnimationControl();
		}
		Collider2D[] componentsInChildren = p.Hero.GetComponentsInChildren<Collider2D>(includeInactive: true);
		foreach (Collider2D collider2D in componentsInChildren)
		{
			trip.Colliders[collider2D] = collider2D.enabled;
			collider2D.enabled = false;
		}
		Renderer[] componentsInChildren2 = p.Hero.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren2)
		{
			if (!(renderer == p.Hero.heroLight) && (!p.Hero.vignette || !renderer.transform.IsChildOf(p.Hero.vignette.transform)))
			{
				trip.Renderers[renderer] = renderer.enabled;
			}
		}
		Hide(trip);
		ActorRecovery.Freeze(p);
		Diagnostics.Write("DREAM rescue start P" + (p.Index + 1));
		CameraPresenceGuard.BeginRescue(s, p);
	}

	private static void Finish(CoopSession s, Trip trip)
	{
		DreamRescueArrival.Begin(s, trip);
	}

	internal static void Hide(Trip trip)
	{
		foreach (KeyValuePair<Renderer, bool> renderer in trip.Renderers)
		{
			if ((bool)renderer.Key)
			{
				renderer.Key.enabled = false;
			}
		}
	}

	internal static void Restore(Trip trip)
	{
		PlayerSlot player = trip.Player;
		if ((bool)trip.Effect)
		{
			Object.Destroy(trip.Effect);
		}
		if (player.Hero != trip.Hero || !trip.Hero || player.Down || player.Hazard)
		{
			return;
		}
		using (PlayerContext.Enter(player))
		{
			ActorRecovery.Reset(player);
			CoopSession.RestoreLivingVisuals(player);
		}
		foreach (KeyValuePair<Collider2D, bool> collider in trip.Colliders)
		{
			if ((bool)collider.Key)
			{
				collider.Key.enabled = collider.Value;
			}
		}
		foreach (KeyValuePair<Renderer, bool> renderer in trip.Renderers)
		{
			if ((bool)renderer.Key)
			{
				renderer.Key.enabled = renderer.Value;
			}
		}
		player.ProtectionUntil = Time.time + 1.25f;
	}

	internal static void VisualTick()
	{
		Trip[] array = trips;
		foreach (Trip trip in array)
		{
			if (trip != null)
			{
				Hide(trip);
				if ((bool)trip.Hero)
				{
					ActorRecovery.Freeze(trip.Player);
				}
			}
		}
	}

	internal static void Cancel(PlayerSlot p)
	{
		if (!Active(p))
		{
			DreamRescueArrival.Forget(p);
			return;
		}
		Trip trip = trips[p.Index];
		trips[p.Index] = null;
		if ((bool)trip.Hero)
		{
			trip.Hero.transform.position = trip.Origin;
		}
		Restore(trip);
		DreamRescueArrival.Forget(p);
	}

	internal static void Reset()
	{
		for (int i = 0; i < 8; i++)
		{
			Trip trip = trips[i];
			trips[i] = null;
			if (trip != null)
			{
				Restore(trip);
			}
			held[i] = 0f;
			released[i] = false;
			airborneAt[i] = 0f;
		}
		NativeDreamFx.ResetAudio();
		DreamRescueArrival.Reset();
	}

	internal static void CancelAll()
	{
		for (int i = 0; i < 8; i++)
		{
			if (trips[i] != null)
			{
				Cancel(trips[i].Player);
			}
			held[i] = 0f;
			released[i] = false;
		}
	}
}
