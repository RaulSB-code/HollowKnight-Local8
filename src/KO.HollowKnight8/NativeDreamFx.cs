using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class NativeDreamFx
{
	private static ParticleSystem source;

	private static AudioClip rescueSound;

	private static string soundScene;

	private static bool ownsSound;

	private static readonly float[] trailAt = new float[8];

	private static readonly Vector3[] trailDestination = (Vector3[])(object)new Vector3[8];

	private static readonly Vector3[] observedPosition = (Vector3[])(object)new Vector3[8];

	private static readonly bool[] observedPositionValid = new bool[8];

	private static string observedScene;

	internal static void PlayActivation(Vector3 at)
	{
		Scene activeScene = SceneManager.GetActiveScene();
		string name = ((Scene)(ref activeScene)).name;
		if (soundScene != name || !Object.op_Implicit((Object)(object)rescueSound))
		{
			if (ownsSound && Object.op_Implicit((Object)(object)rescueSound))
			{
				Object.Destroy((Object)(object)rescueSound);
			}
			ownsSound = false;
			soundScene = name;
			rescueSound = null;
			int num = -1;
			AudioClip[] array = Resources.FindObjectsOfTypeAll<AudioClip>();
			foreach (AudioClip val in array)
			{
				if (!Object.op_Implicit((Object)(object)val) || !(val.length > 0.07f) || !(val.length < 4f))
				{
					continue;
				}
				string text = ((Object)val).name.ToLowerInvariant();
				if (text.Contains("dream") && !text.Contains("fail") && !text.Contains("cancel") && !text.Contains("error"))
				{
					int num2 = 1 + (text.Contains("gate") ? 8 : 0) + ((text.Contains("warp") || text.Contains("teleport")) ? 7 : 0) + ((text.Contains("appear") || text.Contains("start") || text.Contains("activate")) ? 5 : 0) + (text.Contains("nail") ? 2 : 0);
					if (num2 > num)
					{
						num = num2;
						rescueSound = val;
					}
				}
			}
			if (!Object.op_Implicit((Object)(object)rescueSound))
			{
				try
				{
					rescueSound = AudioClip.Create("Local8 soft dream activation", 22050, 1, 44100, false);
					float[] array2 = new float[22050];
					for (int j = 0; j < 22050; j++)
					{
						float num3 = (float)j / 44100f;
						float num4 = Mathf.Min(1f, num3 / 0.025f) * Mathf.Exp(-7f * num3);
						array2[j] = num4 * (0.23f * Mathf.Sin((float)Math.PI * 2f * (740f * num3 - 120f * num3 * num3)) + 0.12f * Mathf.Sin((float)Math.PI * 2f * (1100f * num3 - 160f * num3 * num3)));
					}
					rescueSound.SetData(array2, 0);
					ownsSound = true;
				}
				catch (Exception ex)
				{
					Diagnostics.Throttled("DREAM rescue sound", ex);
					rescueSound = null;
				}
			}
			Diagnostics.Write("DREAM rescue activation audio=" + (Object.op_Implicit((Object)(object)rescueSound) ? ((Object)rescueSound).name : "unavailable"));
		}
		if (Object.op_Implicit((Object)(object)rescueSound))
		{
			GameObject val2 = new GameObject("Local8 Dream cue");
			AudioSource obj = val2.AddComponent<AudioSource>();
			obj.playOnAwake = false;
			obj.spatialBlend = 0f;
			obj.volume = (ownsSound ? 0.45f : 0.325f);
			obj.PlayOneShot(rescueSound);
			Object.Destroy((Object)val2, Mathf.Max(1f, rescueSound.length + 0.25f));
		}
	}

	internal static void ResetAudio()
	{
		if (ownsSound && Object.op_Implicit((Object)(object)rescueSound))
		{
			Object.Destroy((Object)(object)rescueSound);
		}
		rescueSound = null;
		ownsSound = false;
		soundScene = null;
		ResetTeleportObservation();
	}

	internal static void ResetTeleportObservation()
	{
		observedScene = null;
		for (int i = 0; i < observedPositionValid.Length; i++)
		{
			observedPositionValid[i] = false;
		}
	}

	internal static void MarkTeleport(PlayerSlot p, Vector3 destination)
	{
		if (p != null && p.Index >= 0 && p.Index < 8)
		{
			trailAt[p.Index] = Time.unscaledTime;
			trailDestination[p.Index] = destination;
			observedPosition[p.Index] = destination;
			observedPositionValid[p.Index] = true;
		}
	}

	internal static void ObserveTeleports(CoopSession s)
	{
		if (s == null || !s.Active)
		{
			return;
		}
		GameManager instance = GameManager.instance;
		Scene activeScene = SceneManager.GetActiveScene();
		string name = ((Scene)(ref activeScene)).name;
		if (observedScene != name)
		{
			observedScene = name;
			for (int i = 0; i < observedPositionValid.Length; i++)
			{
				observedPositionValid[i] = false;
			}
		}
		bool flag = Object.op_Implicit((Object)(object)instance) && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene;
		foreach (PlayerSlot player in s.Players)
		{
			int index = player.Index;
			if (index < 0 || index >= 8)
			{
				continue;
			}
			if (!flag || !Object.op_Implicit((Object)(object)player.Hero) || !player.Ready || player.SpawnPending || player.Hero.cState.transitioning)
			{
				observedPositionValid[index] = false;
				continue;
			}
			Vector3 position = ((Component)player.Hero).transform.position;
			if (observedPositionValid[index] && position.y > -100f && observedPosition[index].y > -100f && Vector2.Distance(Vector2.op_Implicit(observedPosition[index]), Vector2.op_Implicit(position)) > 9f)
			{
				TeleportTrail(player, observedPosition[index], position);
			}
			observedPosition[index] = position;
			observedPositionValid[index] = true;
		}
	}

	private static int Score(ParticleSystem candidate)
	{
		string text = "";
		Transform val = ((Component)candidate).transform;
		while (Object.op_Implicit((Object)(object)val))
		{
			text = text + " " + ((Object)val).name;
			if (text.Length > 250)
			{
				break;
			}
			val = val.parent;
		}
		if (text.IndexOf("Local8", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return -1;
		}
		ParticleSystemRenderer component = ((Component)candidate).GetComponent<ParticleSystemRenderer>();
		if (Object.op_Implicit((Object)(object)component) && Object.op_Implicit((Object)(object)((Renderer)component).sharedMaterial))
		{
			text = text + " " + ((Object)((Renderer)component).sharedMaterial).name;
			if (Object.op_Implicit((Object)(object)((Renderer)component).sharedMaterial.mainTexture))
			{
				text = text + " " + ((Object)((Renderer)component).sharedMaterial.mainTexture).name;
			}
		}
		text = text.ToLowerInvariant();
		if (text.Contains("dream"))
		{
			return 40 + (text.Contains("gate") ? 20 : 0) + ((text.Contains("warp") || text.Contains("teleport")) ? 30 : 0) + ((text.Contains("pt") || text.Contains("particle")) ? 5 : 0);
		}
		if (text.Contains("teleport") || text.Contains("warp") || text.Contains("white") || text.Contains("light"))
		{
			return 10;
		}
		return 0;
	}

	internal static GameObject Create(PlayerSlot p, Vector3 position)
	{
		if (!Object.op_Implicit((Object)(object)source))
		{
			int num = -1;
			ParticleSystem[] array = Resources.FindObjectsOfTypeAll<ParticleSystem>();
			foreach (ParticleSystem val in array)
			{
				if (Object.op_Implicit((Object)(object)val))
				{
					int num2 = Score(val);
					if (num2 > num)
					{
						num = num2;
						source = val;
					}
				}
			}
		}
		if (!Object.op_Implicit((Object)(object)source))
		{
			Diagnostics.Throttled("DREAM rescue native particles unavailable", new InvalidOperationException("No loaded Dream particle asset"));
			return null;
		}
		GameObject val2 = new GameObject("Local8 Dream staging");
		val2.SetActive(false);
		GameObject val3 = null;
		try
		{
			val3 = Object.Instantiate<GameObject>(((Component)source).gameObject, val2.transform, false);
			val3.SetActive(false);
			((Object)val3).name = "Local8 Dream Rescue P" + (p.Index + 1);
			MonoBehaviour[] componentsInChildren = val3.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (MonoBehaviour val4 in componentsInChildren)
			{
				if (Object.op_Implicit((Object)(object)val4))
				{
					Object.DestroyImmediate((Object)(object)val4);
				}
			}
			Collider2D[] componentsInChildren2 = val3.GetComponentsInChildren<Collider2D>(true);
			foreach (Collider2D val5 in componentsInChildren2)
			{
				if (Object.op_Implicit((Object)(object)val5))
				{
					Object.DestroyImmediate((Object)(object)val5);
				}
			}
			AudioSource[] componentsInChildren3 = val3.GetComponentsInChildren<AudioSource>(true);
			foreach (AudioSource val6 in componentsInChildren3)
			{
				if (Object.op_Implicit((Object)(object)val6))
				{
					Object.DestroyImmediate((Object)(object)val6);
				}
			}
			Rigidbody2D[] componentsInChildren4 = val3.GetComponentsInChildren<Rigidbody2D>(true);
			foreach (Rigidbody2D val7 in componentsInChildren4)
			{
				if (Object.op_Implicit((Object)(object)val7))
				{
					Object.DestroyImmediate((Object)(object)val7);
				}
			}
			Renderer[] componentsInChildren5 = val3.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer val8 in componentsInChildren5)
			{
				if (Object.op_Implicit((Object)(object)val8) && !(val8 is ParticleSystemRenderer))
				{
					val8.enabled = false;
				}
			}
			val3.transform.SetParent((Transform)null, false);
			val3.transform.position = position;
			val3.transform.localScale = Vector3.one;
			ParticleSystem[] array = val3.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem obj in array)
			{
				obj.Stop(true, (ParticleSystemStopBehavior)0);
				MainModule main = obj.main;
				((MainModule)(ref main)).loop = true;
				((MainModule)(ref main)).playOnAwake = false;
				((MainModule)(ref main)).maxParticles = 90;
				((MainModule)(ref main)).simulationSpeed = 1.8f;
				((MainModule)(ref main)).startLifetime = MinMaxCurve.op_Implicit(0.65f);
				((MainModule)(ref main)).startSpeed = MinMaxCurve.op_Implicit(0.8f);
				((MainModule)(ref main)).startSize = MinMaxCurve.op_Implicit(0.45f);
				((MainModule)(ref main)).simulationSpace = (ParticleSystemSimulationSpace)1;
				((MainModule)(ref main)).stopAction = (ParticleSystemStopAction)0;
				((MainModule)(ref main)).startColor = MinMaxGradient.op_Implicit(Color.Lerp(Color.white, p.Color, 0.25f));
				EmissionModule emission = obj.emission;
				((EmissionModule)(ref emission)).enabled = true;
				((EmissionModule)(ref emission)).rateOverTime = MinMaxCurve.op_Implicit(40f);
				ShapeModule shape = obj.shape;
				((ShapeModule)(ref shape)).enabled = true;
				((ShapeModule)(ref shape)).shapeType = (ParticleSystemShapeType)0;
				((ShapeModule)(ref shape)).radius = 0.65f;
				ParticleSystemRenderer component = ((Component)obj).GetComponent<ParticleSystemRenderer>();
				if (Object.op_Implicit((Object)(object)component))
				{
					((Renderer)component).enabled = true;
				}
				((Component)obj).gameObject.SetActive(true);
			}
			val3.SetActive(true);
			array = val3.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			Diagnostics.Write("DREAM rescue particles=" + ((Object)source).name + " P" + (p.Index + 1));
			return val3;
		}
		catch (Exception ex)
		{
			if (Object.op_Implicit((Object)(object)val3))
			{
				Object.Destroy((Object)(object)val3);
			}
			Diagnostics.Throttled("DREAM native effect", ex);
			return null;
		}
		finally
		{
			Object.Destroy((Object)(object)val2);
		}
	}

	internal static void WhitePulse(PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero))
		{
			return;
		}
		GameObject val = Create(p, ((Component)p.Hero).transform.position);
		if (Object.op_Implicit((Object)(object)val))
		{
			((Object)val).name = "Local8 Pale Flower P" + (p.Index + 1);
			val.transform.localScale = Vector3.one * 0.72f;
			ParticleSystem[] componentsInChildren = val.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem obj in componentsInChildren)
			{
				MainModule main = obj.main;
				((MainModule)(ref main)).startColor = MinMaxGradient.op_Implicit(Color.white);
				((MainModule)(ref main)).startLifetime = MinMaxCurve.op_Implicit(0.42f);
				((MainModule)(ref main)).startSpeed = MinMaxCurve.op_Implicit(0.35f);
				((MainModule)(ref main)).startSize = MinMaxCurve.op_Implicit(0.38f);
				((MainModule)(ref main)).loop = false;
				EmissionModule emission = obj.emission;
				((EmissionModule)(ref emission)).rateOverTime = MinMaxCurve.op_Implicit(0f);
				obj.Emit(28);
			}
			if (Object.op_Implicit((Object)(object)Plugin.Self))
			{
				((MonoBehaviour)Plugin.Self).StartCoroutine(WhitePulseLife(val, p));
			}
			else
			{
				Object.Destroy((Object)(object)val, 0.8f);
			}
		}
	}

	private static IEnumerator WhitePulseLife(GameObject effect, PlayerSlot p)
	{
		float until = Time.unscaledTime + 0.7f;
		while (Object.op_Implicit((Object)(object)effect) && Time.unscaledTime < until)
		{
			if (p != null && Object.op_Implicit((Object)(object)p.Hero))
			{
				effect.transform.position = ((Component)p.Hero).transform.position;
			}
			yield return null;
		}
		if (Object.op_Implicit((Object)(object)effect))
		{
			Object.Destroy((Object)(object)effect);
		}
	}

	internal unsafe static void TeleportTrail(PlayerSlot p, Vector3 from, Vector3 to, bool sound = false)
	{
		if (p == null || p.Index < 0 || p.Index >= 8 || Vector2.Distance(Vector2.op_Implicit(from), Vector2.op_Implicit(to)) < 1.75f)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime - trailAt[p.Index] < 0.2f && Vector2.Distance(Vector2.op_Implicit(trailDestination[p.Index]), Vector2.op_Implicit(to)) < 1.5f)
		{
			return;
		}
		trailAt[p.Index] = unscaledTime;
		trailDestination[p.Index] = to;
		observedPosition[p.Index] = to;
		observedPositionValid[p.Index] = true;
		GameObject val = Create(p, from);
		if (Object.op_Implicit((Object)(object)val))
		{
			if (sound)
			{
				PlayActivation(from);
			}
			ContinueTrail(val, from, to);
			string[] obj = new string[6]
			{
				"DREAM teleport trail P",
				(p.Index + 1).ToString(),
				" ",
				null,
				null,
				null
			};
			Vector3 val2 = from;
			obj[3] = ((object)(*(Vector3*)(&val2))/*cast due to .constrained prefix*/).ToString();
			obj[4] = " -> ";
			val2 = to;
			obj[5] = ((object)(*(Vector3*)(&val2))/*cast due to .constrained prefix*/).ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	internal static void ContinueTrail(GameObject effect, Vector3 from, Vector3 to, float linger = 0.8f)
	{
		if (Object.op_Implicit((Object)(object)effect))
		{
			if (Object.op_Implicit((Object)(object)Plugin.Self))
			{
				((MonoBehaviour)Plugin.Self).StartCoroutine(Trail(effect, from, to, linger));
			}
			else
			{
				Object.Destroy((Object)(object)effect);
			}
		}
	}

	private static IEnumerator Trail(GameObject effect, Vector3 from, Vector3 to, float linger)
	{
		float duration = Mathf.Clamp(Vector2.Distance(Vector2.op_Implicit(from), Vector2.op_Implicit(to)) / 55f, 0.2f, 0.55f);
		float elapsed = 0f;
		while (Object.op_Implicit((Object)(object)effect) && elapsed < duration)
		{
			elapsed += Mathf.Max(0.001f, Time.unscaledDeltaTime);
			effect.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
			yield return null;
		}
		if (Object.op_Implicit((Object)(object)effect))
		{
			effect.transform.position = to;
			yield return (object)new WaitForSecondsRealtime(linger);
			if (Object.op_Implicit((Object)(object)effect))
			{
				Object.Destroy((Object)(object)effect);
			}
		}
	}
}
