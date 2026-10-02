using System.Collections.Generic;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class EmergencyWarp
{
	private sealed class Trip
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

	private static readonly float[] cooldown = new float[8];

	private static readonly bool[] released = new bool[8];

	private static readonly float[] airborneAt = new float[8];

	private static readonly Vector2[] airborneAnchor = (Vector2[])(object)new Vector2[8];

	private static readonly Trip[] trips = new Trip[8];

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
		Vector2 val = Vector2.op_Implicit(((Component)p.Hero).transform.position);
		if (p.Hero.cState.onGround)
		{
			airborneAt[index] = 0f;
			return false;
		}
		float unscaledTime = Time.unscaledTime;
		if (airborneAt[index] <= 0f || Vector2.Distance(airborneAnchor[index], val) > 2f)
		{
			airborneAt[index] = unscaledTime;
			airborneAnchor[index] = val;
			return false;
		}
		return unscaledTime - airborneAt[index] >= 1.5f;
	}

	internal static void Tick(CoopSession s, PlayerSlot[] live)
	{
		//IL_02d7: Invalid comparison between Unknown and I4
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
				if (!player.Alive || !player.Ready || (Object)(object)player.Hero != (Object)(object)trip.Hero || PvpMatch.Running)
				{
					Cancel(player);
					continue;
				}
				trip.Elapsed += Time.unscaledDeltaTime;
				Vector2 zero = Vector2.zero;
				if (player.Actions != null)
				{
					((Vector2)(ref zero))._002Ector(((OneAxisInputControl)player.Actions.right).Value - ((OneAxisInputControl)player.Actions.left).Value, ((OneAxisInputControl)player.Actions.up).Value - ((OneAxisInputControl)player.Actions.down).Value);
				}
				trip.Position += Vector2.op_Implicit(Vector2.ClampMagnitude(zero, 1f)) * (22f * Time.unscaledDeltaTime);
				GameManager instance = GameManager.instance;
				if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)instance.cameraCtrl))
				{
					trip.Position.x = Mathf.Clamp(trip.Position.x, 0.75f, Mathf.Max(0.75f, instance.cameraCtrl.sceneWidth - 0.75f));
					trip.Position.y = Mathf.Clamp(trip.Position.y, 0.75f, Mathf.Max(0.75f, instance.cameraCtrl.sceneHeight - 0.75f));
				}
				((Component)trip.Hero).transform.position = trip.Position;
				if (Object.op_Implicit((Object)(object)trip.Effect))
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
				continue;
			}
			bool flag2 = Object.op_Implicit((Object)(object)player.Hero) && player.Ready && player.Alive && AirborneStuck(player);
			if (!flag)
			{
				held[index] = 0f;
				released[index] = true;
				continue;
			}
			GameManager instance2 = GameManager.instance;
			UIManager instance3 = UIManager.instance;
			bool flag3 = (Object)(object)instance2 == (Object)null || instance2.isPaused || !s.Gameplay || (Object)(object)instance3 == (Object)null || (int)instance3.uiState != 4 || Charms.NativeMenuOpen || ScriptedParty.Active || PickupCard.Owner != null || InteractionRouter.ActivePlayer != null || ShopMenuRouting.Buyer != null || StagMenuRouting.HasOwner;
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
	}

	private static void Start(CoopSession s, PlayerSlot p)
	{
		GameObject val = NativeDreamFx.Create(p, ((Component)p.Hero).transform.position);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		Trip trip = new Trip
		{
			Player = p,
			Hero = p.Hero,
			Origin = ((Component)p.Hero).transform.position,
			Position = ((Component)p.Hero).transform.position,
			Effect = val,
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
		Collider2D[] componentsInChildren = ((Component)p.Hero).GetComponentsInChildren<Collider2D>(true);
		foreach (Collider2D val2 in componentsInChildren)
		{
			trip.Colliders[val2] = ((Behaviour)val2).enabled;
			((Behaviour)val2).enabled = false;
		}
		Renderer[] componentsInChildren2 = ((Component)p.Hero).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val3 in componentsInChildren2)
		{
			if (!((Object)(object)val3 == (Object)(object)p.Hero.heroLight) && (!Object.op_Implicit((Object)(object)p.Hero.vignette) || !((Component)val3).transform.IsChildOf(((Component)p.Hero.vignette).transform)))
			{
				trip.Renderers[val3] = val3.enabled;
			}
		}
		Hide(trip);
		ActorRecovery.Freeze(p);
		Diagnostics.Write("DREAM rescue start P" + (p.Index + 1));
	}

	private unsafe static void Finish(CoopSession s, Trip trip)
	{
		double[] array = new double[s.Players.Count];
		bool[] array2 = new bool[s.Players.Count];
		for (int i = 0; i < s.Players.Count; i++)
		{
			PlayerSlot playerSlot = s.Players[i];
			array2[i] = playerSlot != trip.Player && playerSlot.Ready && playerSlot.Alive && playerSlot.Connected && !Active(playerSlot) && !playerSlot.Hero.cState.transitioning;
			int num = i;
			double num2;
			if (!array2[i])
			{
				num2 = double.MaxValue;
			}
			else
			{
				Vector2 val = Vector2.op_Implicit(((Component)playerSlot.Hero).transform.position - trip.Position);
				num2 = ((Vector2)(ref val)).sqrMagnitude;
			}
			array[num] = num2;
		}
		int num3 = PartyRules.Nearest(array, array2);
		PlayerSlot playerSlot2 = ((num3 < 0) ? null : s.Players[num3]);
		Vector3 result;
		if (DreamSequence.InStoryDream && trip.Player.HasSafePoint)
		{
			result = trip.Player.SafePoint;
			playerSlot2 = trip.Player;
		}
		else if (playerSlot2 == null || !s.NearAlly(playerSlot2, trip.Player, out result))
		{
			if (!(trip.Elapsed < 6f))
			{
				cooldown[trip.Player.Index] = Time.unscaledTime + 3f;
				Cancel(trip.Player);
			}
			return;
		}
		result.z = (float)CoopRules.PlayerDepth(trip.Player.Index);
		if (Object.op_Implicit((Object)(object)trip.Effect) && Object.op_Implicit((Object)(object)Plugin.Self) && Vector2.Distance(Vector2.op_Implicit(trip.Position), Vector2.op_Implicit(result)) > 5f)
		{
			GameObject effect = trip.Effect;
			trip.Effect = null;
			NativeDreamFx.MarkTeleport(trip.Player, result);
			NativeDreamFx.ContinueTrail(effect, trip.Position, result);
		}
		((Component)trip.Hero).transform.position = result;
		Restore(trip);
		trips[trip.Player.Index] = null;
		PlayerSlot player = trip.Player;
		player.SafePoint = result;
		player.HasSafePoint = s.IsSafe(result, player);
		player.SafeAt = Time.unscaledTime;
		player.Vitals.HazardPoint = result;
		s.Commit(player);
		string[] obj = new string[10]
		{
			"DREAM rescue arrived P",
			(player.Index + 1).ToString(),
			" to P",
			(playerSlot2.Index + 1).ToString(),
			" cloud=",
			null,
			null,
			null,
			null,
			null
		};
		Vector3 position = trip.Position;
		obj[5] = ((object)(*(Vector3*)(&position))/*cast due to .constrained prefix*/).ToString();
		obj[6] = " ally=";
		obj[7] = ((object)((Component)playerSlot2.Hero).transform.position/*cast due to .constrained prefix*/).ToString();
		obj[8] = " pos=";
		position = result;
		obj[9] = ((object)(*(Vector3*)(&position))/*cast due to .constrained prefix*/).ToString();
		Diagnostics.Write(string.Concat(obj));
	}

	private static void Hide(Trip trip)
	{
		foreach (KeyValuePair<Renderer, bool> renderer in trip.Renderers)
		{
			if (Object.op_Implicit((Object)(object)renderer.Key))
			{
				renderer.Key.enabled = false;
			}
		}
	}

	private static void Restore(Trip trip)
	{
		PlayerSlot player = trip.Player;
		if (Object.op_Implicit((Object)(object)trip.Effect))
		{
			Object.Destroy((Object)(object)trip.Effect);
		}
		if ((Object)(object)player.Hero != (Object)(object)trip.Hero || !Object.op_Implicit((Object)(object)trip.Hero) || player.Down || player.Hazard)
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
			if (Object.op_Implicit((Object)(object)collider.Key))
			{
				((Behaviour)collider.Key).enabled = collider.Value;
			}
		}
		foreach (KeyValuePair<Renderer, bool> renderer in trip.Renderers)
		{
			if (Object.op_Implicit((Object)(object)renderer.Key))
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
				if (Object.op_Implicit((Object)(object)trip.Hero))
				{
					ActorRecovery.Freeze(trip.Player);
				}
			}
		}
	}

	internal static void Cancel(PlayerSlot p)
	{
		if (Active(p))
		{
			Trip trip = trips[p.Index];
			trips[p.Index] = null;
			if (Object.op_Implicit((Object)(object)trip.Hero))
			{
				((Component)trip.Hero).transform.position = trip.Origin;
			}
			Restore(trip);
		}
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
