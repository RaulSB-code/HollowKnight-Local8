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
		Transform transform = p.transform;
		while ((bool)transform && text.Length < 180)
		{
			text = text + " " + transform.name;
			transform = transform.parent;
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
		foreach (ParticleSystem particleSystem in array)
		{
			if ((bool)particleSystem)
			{
				int num2 = ParticleScore(particleSystem);
				if (num2 > num)
				{
					num = num2;
					particle = particleSystem;
				}
			}
		}
		AudioClip[] array2 = Resources.FindObjectsOfTypeAll<AudioClip>();
		foreach (AudioClip audioClip in array2)
		{
			if ((bool)audioClip)
			{
				string text = audioClip.name.ToLowerInvariant();
				if (!focusClip && (text.Contains("focus") || text.Contains("dream nail")))
				{
					focusClip = audioClip;
				}
				if (!impactClip && (text.Contains("absorb") || (text.Contains("radiance") && text.Contains("burst")) || (text.Contains("dream") && text.Contains("impact"))))
				{
					impactClip = audioClip;
				}
			}
		}
		Diagnostics.Write("ENDING FX particle=" + (particle ? particle.name : "none") + " focus=" + (focusClip ? focusClip.name : "none") + " impact=" + (impactClip ? impactClip.name : "none"));
	}

	internal static void Celebrate(PlayerSlot player, bool climax)
	{
		if (player == null || !player.Hero)
		{
			return;
		}
		Find();
		Vector3 position = player.Hero.transform.position + Vector3.up * 0.8f;
		if ((bool)particle)
		{
			GameObject gameObject = null;
			GameObject gameObject2 = new GameObject("Local8 Ending staging");
			gameObject2.SetActive(value: false);
			try
			{
				gameObject = UnityEngine.Object.Instantiate(particle.gameObject, gameObject2.transform, worldPositionStays: false);
				gameObject.SetActive(value: false);
				gameObject.name = "Local8 Ending Focus P" + (player.Index + 1);
				Collider2D[] componentsInChildren = gameObject.GetComponentsInChildren<Collider2D>(includeInactive: true);
				foreach (Collider2D collider2D in componentsInChildren)
				{
					if ((bool)collider2D)
					{
						UnityEngine.Object.DestroyImmediate(collider2D);
					}
				}
				MonoBehaviour[] componentsInChildren2 = gameObject.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
				foreach (MonoBehaviour monoBehaviour in componentsInChildren2)
				{
					if ((bool)monoBehaviour)
					{
						UnityEngine.Object.DestroyImmediate(monoBehaviour);
					}
				}
				AudioSource[] componentsInChildren3 = gameObject.GetComponentsInChildren<AudioSource>(includeInactive: true);
				foreach (AudioSource audioSource in componentsInChildren3)
				{
					if ((bool)audioSource)
					{
						UnityEngine.Object.DestroyImmediate(audioSource);
					}
				}
				Rigidbody2D[] componentsInChildren4 = gameObject.GetComponentsInChildren<Rigidbody2D>(includeInactive: true);
				foreach (Rigidbody2D rigidbody2D in componentsInChildren4)
				{
					if ((bool)rigidbody2D)
					{
						UnityEngine.Object.DestroyImmediate(rigidbody2D);
					}
				}
				Renderer[] componentsInChildren5 = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer in componentsInChildren5)
				{
					if ((bool)renderer && !(renderer is ParticleSystemRenderer))
					{
						renderer.enabled = false;
					}
				}
				gameObject.transform.SetParent(null, worldPositionStays: false);
				gameObject.transform.position = position;
				gameObject.transform.rotation = particle.transform.rotation;
				ParticleSystem[] componentsInChildren6 = gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
				foreach (ParticleSystem obj in componentsInChildren6)
				{
					ParticleSystem.MainModule main = obj.main;
					main.loop = false;
					main.playOnAwake = false;
					ParticleSystem.EmissionModule emission = obj.emission;
					emission.enabled = true;
					obj.gameObject.SetActive(value: true);
					obj.Clear(withChildren: true);
				}
				gameObject.SetActive(value: true);
				componentsInChildren6 = gameObject.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren6.Length; i++)
				{
					componentsInChildren6[i].Play(withChildren: true);
				}
				UnityEngine.Object.Destroy(gameObject, 3f);
			}
			catch (Exception ex)
			{
				if ((bool)gameObject)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
				Diagnostics.Throttled("ENDING native particles", ex);
			}
			finally
			{
				UnityEngine.Object.Destroy(gameObject2);
			}
		}
		AudioClip audioClip = ((!climax) ? focusClip : (impactClip ? impactClip : focusClip));
		if ((bool)audioClip)
		{
			AudioSource.PlayClipAtPoint(audioClip, position, climax ? 0.8f : 0.6f);
		}
		if (climax && (bool)focusClip && focusClip != audioClip)
		{
			AudioSource.PlayClipAtPoint(focusClip, position, 0.42f);
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
