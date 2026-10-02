using System;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CoopEndingFx
{
	private static ParticleSystem particle;

	internal static AudioClip focusClip;

	internal static AudioClip impactClip;

	private static bool searched;

	private static int ParticleScore(ParticleSystem p)
	{
		string text = "";
		Transform val = ((Component)p).transform;
		while (Object.op_Implicit((Object)(object)val) && text.Length < 180)
		{
			text = text + " " + ((Object)val).name;
			val = val.parent;
		}
		text = text.ToLowerInvariant();
		if (text.Contains("local8") || text.Contains("shade"))
		{
			return -1;
		}
		if (text.Contains("focus") || text.Contains("absorb"))
		{
			return 40;
		}
		if (text.Contains("infection") || text.Contains("dream"))
		{
			return 25;
		}
		return -1;
	}

	private static void Find()
	{
		if (searched)
		{
			return;
		}
		searched = true;
		int num = -1;
		ParticleSystem[] array = Resources.FindObjectsOfTypeAll<ParticleSystem>();
		foreach (ParticleSystem val in array)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				int num2 = ParticleScore(val);
				if (num2 > num)
				{
					num = num2;
					particle = val;
				}
			}
		}
		AudioClip[] array2 = Resources.FindObjectsOfTypeAll<AudioClip>();
		foreach (AudioClip val2 in array2)
		{
			if (Object.op_Implicit((Object)(object)val2))
			{
				string text = ((Object)val2).name.ToLowerInvariant();
				if (!Object.op_Implicit((Object)(object)focusClip) && (text.Contains("focus") || text.Contains("dream nail")))
				{
					focusClip = val2;
				}
				if (!Object.op_Implicit((Object)(object)impactClip) && (text.Contains("absorb") || (text.Contains("radiance") && text.Contains("burst")) || (text.Contains("dream") && text.Contains("impact"))))
				{
					impactClip = val2;
				}
			}
		}
		Diagnostics.Write("ENDING FX particle=" + (Object.op_Implicit((Object)(object)particle) ? ((Object)particle).name : "none") + " focus=" + (Object.op_Implicit((Object)(object)focusClip) ? ((Object)focusClip).name : "none") + " impact=" + (Object.op_Implicit((Object)(object)impactClip) ? ((Object)impactClip).name : "none"));
	}

	internal static void Celebrate(PlayerSlot player, bool climax)
	{
		if (player == null || !Object.op_Implicit((Object)(object)player.Hero))
		{
			return;
		}
		Find();
		Vector3 val = ((Component)player.Hero).transform.position + Vector3.up * 0.8f;
		if (Object.op_Implicit((Object)(object)particle))
		{
			GameObject val2 = null;
			GameObject val3 = new GameObject("Local8 Ending staging");
			val3.SetActive(false);
			try
			{
				val2 = Object.Instantiate<GameObject>(((Component)particle).gameObject, val3.transform, false);
				val2.SetActive(false);
				((Object)val2).name = "Local8 Ending Focus P" + (player.Index + 1);
				Collider2D[] componentsInChildren = val2.GetComponentsInChildren<Collider2D>(true);
				foreach (Collider2D val4 in componentsInChildren)
				{
					if (Object.op_Implicit((Object)(object)val4))
					{
						Object.DestroyImmediate((Object)(object)val4);
					}
				}
				MonoBehaviour[] componentsInChildren2 = val2.GetComponentsInChildren<MonoBehaviour>(true);
				foreach (MonoBehaviour val5 in componentsInChildren2)
				{
					if (Object.op_Implicit((Object)(object)val5))
					{
						Object.DestroyImmediate((Object)(object)val5);
					}
				}
				AudioSource[] componentsInChildren3 = val2.GetComponentsInChildren<AudioSource>(true);
				foreach (AudioSource val6 in componentsInChildren3)
				{
					if (Object.op_Implicit((Object)(object)val6))
					{
						Object.DestroyImmediate((Object)(object)val6);
					}
				}
				Rigidbody2D[] componentsInChildren4 = val2.GetComponentsInChildren<Rigidbody2D>(true);
				foreach (Rigidbody2D val7 in componentsInChildren4)
				{
					if (Object.op_Implicit((Object)(object)val7))
					{
						Object.DestroyImmediate((Object)(object)val7);
					}
				}
				Renderer[] componentsInChildren5 = val2.GetComponentsInChildren<Renderer>(true);
				foreach (Renderer val8 in componentsInChildren5)
				{
					if (Object.op_Implicit((Object)(object)val8) && !(val8 is ParticleSystemRenderer))
					{
						val8.enabled = false;
					}
				}
				val2.transform.SetParent((Transform)null, false);
				val2.transform.position = val;
				val2.transform.rotation = ((Component)particle).transform.rotation;
				ParticleSystem[] componentsInChildren6 = val2.GetComponentsInChildren<ParticleSystem>(true);
				foreach (ParticleSystem obj in componentsInChildren6)
				{
					MainModule main = obj.main;
					((MainModule)(ref main)).loop = false;
					((MainModule)(ref main)).playOnAwake = false;
					EmissionModule emission = obj.emission;
					((EmissionModule)(ref emission)).enabled = true;
					((Component)obj).gameObject.SetActive(true);
					obj.Clear(true);
				}
				val2.SetActive(true);
				componentsInChildren6 = val2.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren6.Length; i++)
				{
					componentsInChildren6[i].Play(true);
				}
				Object.Destroy((Object)(object)val2, 3f);
			}
			catch (Exception ex)
			{
				if (Object.op_Implicit((Object)(object)val2))
				{
					Object.Destroy((Object)(object)val2);
				}
				Diagnostics.Throttled("ENDING native particles", ex);
			}
			finally
			{
				Object.Destroy((Object)(object)val3);
			}
		}
		AudioClip val9 = ((!climax) ? focusClip : (Object.op_Implicit((Object)(object)impactClip) ? impactClip : focusClip));
		if (Object.op_Implicit((Object)(object)val9))
		{
			AudioSource.PlayClipAtPoint(val9, val, climax ? 0.8f : 0.6f);
		}
		if (climax && Object.op_Implicit((Object)(object)focusClip) && (Object)(object)focusClip != (Object)(object)val9)
		{
			AudioSource.PlayClipAtPoint(focusClip, val, 0.42f);
		}
	}

	internal static void Reset()
	{
		particle = null;
		focusClip = null;
		impactClip = null;
		searched = false;
	}
}
