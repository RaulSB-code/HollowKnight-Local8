using System;
using System.Collections;
using System.Collections.Generic;
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
	private static class _003C_003EO
	{
		public static hook_FreezeMoment_float_float_float_float _003C0_003E__Freeze;

		public static hook_PlaySound _003C1_003E__HeroSound;

		public static hook_SpawnAndPlayOneShot _003C2_003E__Audio;
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
			if (Object.op_Implicit((Object)(object)Plugin.Self) && Plugin.Self.Session != null && Plugin.Self.Session.Active)
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
		object obj = _003C_003EO._003C0_003E__Freeze;
		if (obj == null)
		{
			hook_FreezeMoment_float_float_float_float val = Freeze;
			_003C_003EO._003C0_003E__Freeze = val;
			obj = (object)val;
		}
		GameManager.FreezeMoment_float_float_float_float += (hook_FreezeMoment_float_float_float_float)obj;
		object obj2 = _003C_003EO._003C1_003E__HeroSound;
		if (obj2 == null)
		{
			hook_PlaySound val2 = HeroSound;
			_003C_003EO._003C1_003E__HeroSound = val2;
			obj2 = (object)val2;
		}
		HeroAudioController.PlaySound += (hook_PlaySound)obj2;
		object obj3 = _003C_003EO._003C2_003E__Audio;
		if (obj3 == null)
		{
			hook_SpawnAndPlayOneShot val3 = Audio;
			_003C_003EO._003C2_003E__Audio = val3;
			obj3 = (object)val3;
		}
		AudioEvent.SpawnAndPlayOneShot += (hook_SpawnAndPlayOneShot)obj3;
	}

	internal static void Uninstall()
	{
		object obj = _003C_003EO._003C0_003E__Freeze;
		if (obj == null)
		{
			hook_FreezeMoment_float_float_float_float val = Freeze;
			_003C_003EO._003C0_003E__Freeze = val;
			obj = (object)val;
		}
		GameManager.FreezeMoment_float_float_float_float -= (hook_FreezeMoment_float_float_float_float)obj;
		object obj2 = _003C_003EO._003C1_003E__HeroSound;
		if (obj2 == null)
		{
			hook_PlaySound val2 = HeroSound;
			_003C_003EO._003C1_003E__HeroSound = val2;
			obj2 = (object)val2;
		}
		HeroAudioController.PlaySound -= (hook_PlaySound)obj2;
		object obj3 = _003C_003EO._003C2_003E__Audio;
		if (obj3 == null)
		{
			hook_SpawnAndPlayOneShot val3 = Audio;
			_003C_003EO._003C2_003E__Audio = val3;
			obj3 = (object)val3;
		}
		AudioEvent.SpawnAndPlayOneShot -= (hook_SpawnAndPlayOneShot)obj3;
		Reset();
	}

	private static IEnumerator Freeze(orig_FreezeMoment_float_float_float_float orig, GameManager self, float down, float wait, float up, float speed)
	{
		if (!InCombat || down + wait + up > 0.8f)
		{
			return orig.Invoke(self, down, wait, up, speed);
		}
		if (Time.unscaledTime < nextFreeze)
		{
			return Empty();
		}
		nextFreeze = Time.unscaledTime + 0.18f;
		return orig.Invoke(self, Mathf.Min(down, 0.008f), Mathf.Min(wait, 0.018f), Mathf.Min(up, 0.03f), Mathf.Max(speed, 0.35f));
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

	private static void HeroSound(orig_PlaySound orig, HeroAudioController self, HeroSounds sound)
	{
		//IL_0009: Invalid comparison between Unknown and I4
		if (!Enabled || (int)sound != 8 || AllowSound(-1))
		{
			orig.Invoke(self, sound);
		}
	}

	private static void Audio(orig_SpawnAndPlayOneShot orig, ref AudioEvent self, AudioSource prefab, Vector3 position)
	{
		if (!InCombat || !Object.op_Implicit((Object)(object)self.Clip))
		{
			orig.Invoke(ref self, prefab, position);
		}
		else if (AllowSound(((Object)self.Clip).GetInstanceID()))
		{
			float volume = self.Volume;
			self.Volume *= 1f / Mathf.Sqrt((float)Plugin.Self.Session.Players.Count);
			try
			{
				orig.Invoke(ref self, prefab, position);
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
			float num = 1f / Mathf.Sqrt((float)s.Players.Count);
			foreach (PlayerSlot player in s.Players)
			{
				if (!Object.op_Implicit((Object)(object)player.Hero))
				{
					continue;
				}
				AudioSource[] componentsInChildren = ((Component)player.Hero).GetComponentsInChildren<AudioSource>(true);
				foreach (AudioSource val in componentsInChildren)
				{
					int instanceID = ((Object)val).GetInstanceID();
					if (!volumes.TryGetValue(instanceID, out var value))
					{
						value = new Volume
						{
							Source = val,
							Owner = player,
							Base = val.volume,
							Applied = val.volume
						};
						volumes[instanceID] = value;
					}
					if (Math.Abs(val.volume - value.Applied) > 0.001f)
					{
						value.Base = val.volume;
					}
					value.Applied = value.Base * num;
					val.volume = value.Applied;
				}
			}
		}
	}

	internal static void Restore(PlayerSlot p)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, Volume> volume in volumes)
		{
			if (volume.Value.Owner == p || !Object.op_Implicit((Object)(object)volume.Value.Source))
			{
				if (Object.op_Implicit((Object)(object)volume.Value.Source))
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
			if (Object.op_Implicit((Object)(object)value.Source))
			{
				value.Source.volume = value.Base;
			}
		}
		volumes.Clear();
		sounds.Clear();
		nextScan = 0f;
		nextFreeze = 0f;
	}

	private static IEnumerator Empty()
	{
		yield break;
	}
}
