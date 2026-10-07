using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class TransitionVote
{
	private sealed class Vote
	{
		internal PlayerSlot Player;

		internal string Key;

		internal float At;

		internal int Order;

		internal Action Commit;

		internal bool WasBlocked;

		internal bool WasKinematic;

		internal float Gravity;

		internal Vector2 Velocity;

		internal readonly Dictionary<Renderer, bool> Renderers = new Dictionary<Renderer, bool>();
	}

	private static readonly Dictionary<int, Vote> votes = new Dictionary<int, Vote>();

	private static float firstAt;

	private static int nextOrder;

	private static bool committing;

	internal static bool Pending => votes.Count > 0;

	internal static float Progress
	{
		get
		{
			if (votes.Count != 0)
			{
				return Mathf.Clamp01((Time.unscaledTime - firstAt) / 5f);
			}
			return 0f;
		}
	}

	internal static int Count
	{
		get
		{
			Local8Runtime self = Plugin.Self;
			if ((object)self == null)
			{
				return 0;
			}
			CoopSession session = self.Session;
			if (session == null)
			{
				return 0;
			}
			int num = session.Players.Count - votes.Count;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	internal static bool Holding(PlayerSlot p)
	{
		if (p != null)
		{
			return votes.ContainsKey(p.Index);
		}
		return false;
	}

	internal static bool ApproachingExit(CoopSession s)
	{
		if (Pending)
		{
			return true;
		}
		if (s == null || s.Players.Count < 2 || TransitionPoint.TransitionPoints == null)
		{
			return false;
		}
		foreach (TransitionPoint transitionPoint in TransitionPoint.TransitionPoints)
		{
			if (!transitionPoint || transitionPoint.isADoor || string.IsNullOrEmpty(transitionPoint.targetScene))
			{
				continue;
			}
			Collider2D component = transitionPoint.GetComponent<Collider2D>();
			foreach (PlayerSlot player in s.Players)
			{
				if (!player.Alive || !player.Ready)
				{
					continue;
				}
				Vector3 position = player.Hero.transform.position;
				if ((bool)component)
				{
					Bounds bounds = component.bounds;
					if (position.x > bounds.min.x - 3f && position.x < bounds.max.x + 3f && position.y > bounds.min.y - 3f && position.y < bounds.max.y + 3f)
					{
						return true;
					}
				}
				else if ((position - transitionPoint.transform.position).sqrMagnitude < 36f)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool CanDefer(CoopSession s, PlayerSlot p)
	{
		if (!committing && Plugin.Self != null && Plugin.Self.WaitForParty.Value && s != null && s.Active && !BossSequenceController.IsInSequence && s.Gameplay && !s.TeamWipe && s.Players.Count > 1 && p != null && (bool)p.Hero && p.Ready && p.Alive && p.Connected && !p.ArenaTransfer && !EmergencyWarp.Active(p))
		{
			return !PvpMatch.Running;
		}
		return false;
	}

	internal static bool Gate(CoopSession s, PlayerSlot p, TransitionPoint gate, Collider2D collider, Action original)
	{
		if (Holding(p))
		{
			return true;
		}
		if (gate != null && (BossFlow(gate.targetScene) || DreamFlow(gate.targetScene)))
		{
			return false;
		}
		if (gate == null || collider == null || collider.gameObject.layer != 9 || gate.isADoor || string.IsNullOrEmpty(gate.targetScene) || string.IsNullOrEmpty(gate.entryPoint) || !CanDefer(s, p))
		{
			return false;
		}
		Add(p, gate.targetScene, original);
		return true;
	}

	internal static bool Direct(CoopSession s, PlayerSlot p, GameManager manager, GameManager.SceneLoadInfo info)
	{
		if (Holding(p))
		{
			return true;
		}
		if (info != null && (BossFlow(info.SceneName) || DreamFlow(info.SceneName)))
		{
			return false;
		}
		if (info == null || info.GetType() != typeof(GameManager.SceneLoadInfo) || string.IsNullOrEmpty(info.SceneName) || string.IsNullOrEmpty(info.EntryGateName) || !CanDefer(s, p) || ScriptedParty.Active || CoopEnding.Active || ArenaGather.TransferActive)
		{
			return false;
		}
		Add(p, info.SceneName, delegate
		{
			manager.BeginSceneTransition(info);
		});
		return true;
	}

	private static bool BossFlow(string scene)
	{
		if (!string.IsNullOrEmpty(scene))
		{
			return scene.StartsWith("GG_", StringComparison.Ordinal);
		}
		return false;
	}

	private static bool DreamFlow(string scene)
	{
		if (string.IsNullOrEmpty(scene))
		{
			return false;
		}
		if (scene.StartsWith("Dream_", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("Dream_", StringComparison.OrdinalIgnoreCase);
	}

	private static void Add(PlayerSlot p, string key, Action commit)
	{
		CrystalDashTransit.CaptureVote(p, key);
		if (votes.ContainsKey(p.Index))
		{
			return;
		}
		Rigidbody2D component = p.Hero.GetComponent<Rigidbody2D>();
		Vote vote = new Vote
		{
			Player = p,
			Key = key,
			At = Time.unscaledTime,
			Order = nextOrder++,
			Commit = commit,
			WasBlocked = p.InputBlocked,
			WasKinematic = ((bool)component && component.isKinematic),
			Gravity = (component ? component.gravityScale : 0f),
			Velocity = (component ? component.velocity : Vector2.zero)
		};
		if (votes.Count == 0)
		{
			firstAt = vote.At;
		}
		votes[p.Index] = vote;
		p.InputBlocked = true;
		if ((bool)component)
		{
			component.velocity = Vector2.zero;
			component.gravityScale = 0f;
			component.isKinematic = true;
		}
		Renderer[] componentsInChildren = p.Hero.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if ((bool)renderer)
			{
				vote.Renderers[renderer] = renderer.enabled;
			}
		}
		Hide(vote);
		RescueHint.HideDistant();
		Diagnostics.Write("ROOM WAIT P" + (p.Index + 1) + " vote=" + key + " count=" + votes.Count);
	}

	private static void Hide(Vote v)
	{
		foreach (Renderer key in v.Renderers.Keys)
		{
			if ((bool)key)
			{
				key.enabled = false;
			}
		}
	}

	internal static void HideWaiting()
	{
		foreach (Vote value in votes.Values)
		{
			Hide(value);
		}
	}

	private static void Release(Vote v)
	{
		PlayerSlot player = v.Player;
		if (player == null || !player.Hero)
		{
			return;
		}
		player.InputBlocked = v.WasBlocked;
		Rigidbody2D component = player.Hero.GetComponent<Rigidbody2D>();
		if ((bool)component)
		{
			component.isKinematic = v.WasKinematic;
			component.gravityScale = v.Gravity;
			component.velocity = v.Velocity;
		}
		if (!player.Alive)
		{
			return;
		}
		foreach (KeyValuePair<Renderer, bool> renderer in v.Renderers)
		{
			if ((bool)renderer.Key)
			{
				renderer.Key.enabled = renderer.Value;
			}
		}
	}

	internal static void Tick(CoopSession s)
	{
		if (votes.Count == 0)
		{
			return;
		}
		if (s == null || !s.Active || s.TeamWipe)
		{
			Reset();
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Vote> vote2 in votes)
		{
			if (!vote2.Value.Player.Alive || !vote2.Value.Player.Ready || !vote2.Value.Player.Connected || !s.Players.Contains(vote2.Value.Player))
			{
				list.Add(vote2.Key);
			}
		}
		foreach (int item in list)
		{
			Release(votes[item]);
			votes.Remove(item);
		}
		if (votes.Count == 0)
		{
			firstAt = 0f;
			return;
		}
		if (list.Count > 0)
		{
			firstAt = float.MaxValue;
			foreach (Vote value2 in votes.Values)
			{
				firstAt = Mathf.Min(firstAt, value2.At);
			}
		}
		if (((bool)GameManager.instance && GameManager.instance.isPaused) || Plugin.Self.Panel)
		{
			firstAt += Time.unscaledDeltaTime;
			return;
		}
		int num = 0;
		foreach (PlayerSlot player in s.Players)
		{
			if (player.Alive && player.Ready && player.Connected)
			{
				num++;
			}
		}
		if (Time.unscaledTime - firstAt < 5f && votes.Count < num && Plugin.Self.WaitForParty.Value)
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Vote value3 in votes.Values)
		{
			dictionary.TryGetValue(value3.Key, out var value);
			dictionary[value3.Key] = value + 1;
		}
		Vote vote = null;
		int num2 = -1;
		foreach (Vote value4 in votes.Values)
		{
			int num3 = dictionary[value4.Key];
			if (num3 > num2 || (num3 == num2 && (vote == null || value4.Order < vote.Order)))
			{
				num2 = num3;
				vote = value4;
			}
		}
		List<Vote> list2 = new List<Vote>(votes.Values);
		votes.Clear();
		if (vote == null)
		{
			return;
		}
		Diagnostics.Write("ROOM CHOICE " + vote.Key + " votes=" + num2 + " total=" + num);
		foreach (Vote item2 in list2)
		{
			Release(item2);
		}
		Commit(vote);
	}

	private static void Commit(Vote winner)
	{
		committing = true;
		try
		{
			winner.Commit();
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("ROOM WAIT commit", ex);
		}
		finally
		{
			committing = false;
		}
	}

	internal static void Reset()
	{
		foreach (Vote value in votes.Values)
		{
			Release(value);
		}
		votes.Clear();
		firstAt = 0f;
		nextOrder = 0;
		committing = false;
	}
}
