using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GlobalEnums;
using On;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CombatEffects
{
	private sealed class Volume
	{
		internal AudioSource Source;

		internal PlayerSlot Owner;

		internal float Base;

		internal float Applied;
	}

	[CompilerGenerated]
	private sealed class __iterator__Empty_d__20 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return __iterator___2__current;
			}
		}

		[DebuggerHidden]
		public __iterator__Empty_d__20(int __iterator___1__state)
		{
			this.__iterator___1__state = __iterator___1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			__iterator___1__state = -2;
		}

		private bool MoveNext()
		{
			if (__iterator___1__state != 0)
			{
				return false;
			}
			__iterator___1__state = -1;
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private static readonly Dictionary<int, Volume> volumes = new Dictionary<int, Volume>();

	private static readonly Dictionary<int, float> sounds = new Dictionary<int, float>();

	private static float recentCombat = -10f;

	private static float nextFreeze;

	private static float nextScan;

	internal static bool Enabled
	{
		get
		{
			if ((bool)Plugin.Self && Plugin.Self.Session != null && Plugin.Self.Session.Active)
			{
				return Plugin.Self.ReduceHitEffects.Value;
			}
			return false;
		}
	}

	private static bool InCombat
	{
		get
		{
			if (Enabled && !Plugin.Self.Session.TeamWipe && Plugin.Self.Session.Gameplay)
			{
				if (PlayerContext.Current == null)
				{
					return Time.unscaledTime - recentCombat < 0.15f;
				}
				return true;
			}
			return false;
		}
	}

	internal static void MarkCombat()
	{
		recentCombat = Time.unscaledTime;
	}

	internal static void Install()
	{
		On.GameManager.FreezeMoment_float_float_float_float += Freeze;
		On.HeroAudioController.PlaySound += HeroSound;
		On.AudioEvent.SpawnAndPlayOneShot += Audio;
	}

	internal static void Uninstall()
	{
		On.GameManager.FreezeMoment_float_float_float_float -= Freeze;
		On.HeroAudioController.PlaySound -= HeroSound;
		On.AudioEvent.SpawnAndPlayOneShot -= Audio;
		Reset();
	}

	private static IEnumerator Freeze(On.GameManager.orig_FreezeMoment_float_float_float_float orig, GameManager self, float down, float wait, float up, float speed)
	{
		if (!InCombat || down + wait + up > 0.8f)
		{
			return orig(self, down, wait, up, speed);
		}
		if (Time.unscaledTime < nextFreeze)
		{
			return Empty();
		}
		nextFreeze = Time.unscaledTime + 0.18f;
		return orig(self, Mathf.Min(down, 0.008f), Mathf.Min(wait, 0.018f), Mathf.Min(up, 0.03f), Mathf.Max(speed, 0.35f));
	}

	private static bool AllowSound(int key)
	{
		if (sounds.TryGetValue(key, out var value) && Time.unscaledTime - value < 0.045f)
		{
			return false;
		}
		sounds[key] = Time.unscaledTime;
		return true;
	}

	private static void HeroSound(On.HeroAudioController.orig_PlaySound orig, HeroAudioController self, HeroSounds sound)
	{
		if (!Enabled || sound != HeroSounds.TAKE_HIT || AllowSound(-1))
		{
			orig(self, sound);
		}
	}

	private static void Audio(On.AudioEvent.orig_SpawnAndPlayOneShot orig, ref AudioEvent self, AudioSource prefab, Vector3 position)
	{
		if (!InCombat || !self.Clip)
		{
			orig(ref self, prefab, position);
		}
		else if (AllowSound(self.Clip.GetInstanceID()))
		{
			float volume = self.Volume;
			self.Volume *= 1f / Mathf.Sqrt(Plugin.Self.Session.Players.Count);
			try
			{
				orig(ref self, prefab, position);
			}
			finally
			{
				self.Volume = volume;
			}
		}
	}

	internal static void UpdateVolumes(CoopSession s)
	{
		if (!Enabled)
		{
			if (volumes.Count > 0)
			{
				Reset();
			}
		}
		else
		{
			if (Time.unscaledTime < nextScan)
			{
				return;
			}
			nextScan = Time.unscaledTime + 1f;
			float num = 1f / Mathf.Sqrt(s.Players.Count);
			foreach (PlayerSlot player in s.Players)
			{
				if (!player.Hero)
				{
					continue;
				}
				AudioSource[] componentsInChildren = player.Hero.GetComponentsInChildren<AudioSource>(includeInactive: true);
				foreach (AudioSource audioSource in componentsInChildren)
				{
					int instanceID = audioSource.GetInstanceID();
					if (!volumes.TryGetValue(instanceID, out var value))
					{
						value = new Volume
						{
							Source = audioSource,
							Owner = player,
							Base = audioSource.volume,
							Applied = audioSource.volume
						};
						volumes[instanceID] = value;
					}
					if (Math.Abs(audioSource.volume - value.Applied) > 0.001f)
					{
						value.Base = audioSource.volume;
					}
					value.Applied = value.Base * num;
					audioSource.volume = value.Applied;
				}
			}
		}
	}

	internal static void Restore(PlayerSlot p)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Volume> volume in volumes)
		{
			if (volume.Value.Owner == p || !volume.Value.Source)
			{
				if ((bool)volume.Value.Source)
				{
					volume.Value.Source.volume = volume.Value.Base;
				}
				list.Add(volume.Key);
			}
		}
		foreach (int item in list)
		{
			volumes.Remove(item);
		}
	}

	internal static void Reset()
	{
		foreach (Volume value in volumes.Values)
		{
			if ((bool)value.Source)
			{
				value.Source.volume = value.Base;
			}
		}
		volumes.Clear();
		sounds.Clear();
		nextScan = 0f;
		nextFreeze = 0f;
	}

	[IteratorStateMachine(typeof(__iterator__Empty_d__20))]
	private static IEnumerator Empty()
	{
		return new __iterator__Empty_d__20(0);
	}
}
