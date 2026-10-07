using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KO.HollowKnight8;

internal static class NativeDreamFx
{
	[CompilerGenerated]
	private sealed class __iterator__Trail_d__20 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		public Vector3 from;

		public Vector3 to;

		public GameObject effect;

		public float linger;

		private float __iterator__duration_5__2;

		private float __iterator__elapsed_5__3;

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
		public __iterator__Trail_d__20(int __iterator___1__state)
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
			switch (__iterator___1__state)
			{
			default:
				return false;
			case 0:
				__iterator___1__state = -1;
				__iterator__duration_5__2 = Mathf.Clamp(Vector2.Distance(from, to) / 55f, 0.2f, 0.55f);
				__iterator__elapsed_5__3 = 0f;
				goto IL_00cb;
			case 1:
				__iterator___1__state = -1;
				goto IL_00cb;
			case 2:
				{
					__iterator___1__state = -1;
					if ((bool)effect)
					{
						UnityEngine.Object.Destroy(effect);
					}
					break;
				}
				IL_00cb:
				if ((bool)effect && __iterator__elapsed_5__3 < __iterator__duration_5__2)
				{
					__iterator__elapsed_5__3 += Mathf.Max(0.001f, Time.unscaledDeltaTime);
					effect.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(__iterator__elapsed_5__3 / __iterator__duration_5__2));
					__iterator___2__current = null;
					__iterator___1__state = 1;
					return true;
				}
				if ((bool)effect)
				{
					effect.transform.position = to;
					__iterator___2__current = new WaitForSecondsRealtime(linger);
					__iterator___1__state = 2;
					return true;
				}
				break;
			}
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

	[CompilerGenerated]
	private sealed class __iterator__WhitePulseLife_d__17 : IEnumerator<object>, IDisposable, IEnumerator
	{
		private int __iterator___1__state;

		private object __iterator___2__current;

		public PlayerSlot p;

		public GameObject effect;

		private float __iterator__until_5__2;

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
		public __iterator__WhitePulseLife_d__17(int __iterator___1__state)
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
			switch (__iterator___1__state)
			{
			default:
				return false;
			case 0:
				__iterator___1__state = -1;
				__iterator__until_5__2 = Time.unscaledTime + 0.7f;
				break;
			case 1:
				__iterator___1__state = -1;
				break;
			}
			if ((bool)effect && Time.unscaledTime < __iterator__until_5__2)
			{
				if (p != null && (bool)p.Hero)
				{
					effect.transform.position = p.Hero.transform.position;
				}
				__iterator___2__current = null;
				__iterator___1__state = 1;
				return true;
			}
			if ((bool)effect)
			{
				UnityEngine.Object.Destroy(effect);
			}
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

	private static ParticleSystem source;

	private static AudioClip rescueSound;

	private static string soundScene;

	private static bool ownsSound;

	private static readonly float[] trailAt = new float[8];

	private static readonly Vector3[] trailDestination = new Vector3[8];

	private static readonly Vector3[] observedPosition = new Vector3[8];

	private static readonly bool[] observedPositionValid = new bool[8];

	private static string observedScene;

	internal static void PlayActivation(Vector3 at)
	{
		string name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		if (soundScene != name || !rescueSound)
		{
			if (ownsSound && (bool)rescueSound)
			{
				UnityEngine.Object.Destroy(rescueSound);
			}
			ownsSound = false;
			soundScene = name;
			rescueSound = null;
			int num = -1;
			AudioClip[] array = Resources.FindObjectsOfTypeAll<AudioClip>();
			foreach (AudioClip audioClip in array)
			{
				if (!audioClip || !(audioClip.length > 0.07f) || !(audioClip.length < 4f))
				{
					continue;
				}
				string text = audioClip.name.ToLowerInvariant();
				if (text.Contains("dream") && !text.Contains("fail") && !text.Contains("cancel") && !text.Contains("error"))
				{
					int num2 = 1 + (text.Contains("gate") ? 8 : 0) + ((text.Contains("warp") || text.Contains("teleport")) ? 7 : 0) + ((text.Contains("appear") || text.Contains("start") || text.Contains("activate")) ? 5 : 0) + (text.Contains("nail") ? 2 : 0);
					if (num2 > num)
					{
						num = num2;
						rescueSound = audioClip;
					}
				}
			}
			if (!rescueSound)
			{
				try
				{
					rescueSound = AudioClip.Create("Local8 soft dream activation", 22050, 1, 44100, stream: false);
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
			Diagnostics.Write("DREAM rescue activation audio=" + (rescueSound ? rescueSound.name : "unavailable"));
		}
		if ((bool)rescueSound)
		{
			GameObject gameObject = new GameObject("Local8 Dream cue");
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.spatialBlend = 0f;
			audioSource.volume = (ownsSound ? 0.3825f : 0.27625f);
			audioSource.PlayOneShot(rescueSound);
			UnityEngine.Object.Destroy(gameObject, Mathf.Max(1f, rescueSound.length + 0.25f));
		}
	}

	internal static void ResetAudio()
	{
		if (ownsSound && (bool)rescueSound)
		{
			UnityEngine.Object.Destroy(rescueSound);
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
		string name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		if (observedScene != name)
		{
			observedScene = name;
			for (int i = 0; i < observedPositionValid.Length; i++)
			{
				observedPositionValid[i] = false;
			}
		}
		bool flag = (bool)instance && instance.IsGameplayScene() && !instance.IsLoadingSceneTransition && instance.HasFinishedEnteringScene;
		foreach (PlayerSlot player in s.Players)
		{
			int index = player.Index;
			if (index < 0 || index >= 8)
			{
				continue;
			}
			if (!flag || !player.Hero || !player.Ready || player.SpawnPending || player.Hero.cState.transitioning)
			{
				observedPositionValid[index] = false;
				continue;
			}
			Vector3 position = player.Hero.transform.position;
			if (observedPositionValid[index] && position.y > -100f && observedPosition[index].y > -100f && Vector2.Distance(observedPosition[index], position) > 9f)
			{
				TeleportTrail(player, observedPosition[index], position);
			}
			observedPosition[index] = position;
			observedPositionValid[index] = true;
		}
	}

	internal static int Score(ParticleSystem candidate)
	{
		string text = "";
		Transform transform = candidate.transform;
		while ((bool)transform)
		{
			text = text + " " + transform.name;
			if (text.Length > 250)
			{
				break;
			}
			transform = transform.parent;
		}
		if (text.IndexOf("Local8", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return -1;
		}
		ParticleSystemRenderer component = candidate.GetComponent<ParticleSystemRenderer>();
		if ((bool)component && (bool)component.sharedMaterial)
		{
			text = text + " " + component.sharedMaterial.name;
			if ((bool)component.sharedMaterial.mainTexture)
			{
				text = text + " " + component.sharedMaterial.mainTexture.name;
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
		if (!source)
		{
			int num = -1;
			ParticleSystem[] array = Resources.FindObjectsOfTypeAll<ParticleSystem>();
			foreach (ParticleSystem particleSystem in array)
			{
				if ((bool)particleSystem)
				{
					int num2 = Score(particleSystem);
					if (num2 > num)
					{
						num = num2;
						source = particleSystem;
					}
				}
			}
		}
		if (!source)
		{
			Diagnostics.Throttled("DREAM rescue native particles unavailable", new InvalidOperationException("No loaded Dream particle asset"));
			return null;
		}
		GameObject gameObject = new GameObject("Local8 Dream staging");
		gameObject.SetActive(value: false);
		GameObject gameObject2 = null;
		try
		{
			gameObject2 = UnityEngine.Object.Instantiate(source.gameObject, gameObject.transform, worldPositionStays: false);
			gameObject2.SetActive(value: false);
			gameObject2.name = "Local8 Dream Rescue P" + (p.Index + 1);
			MonoBehaviour[] componentsInChildren = gameObject2.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			foreach (MonoBehaviour monoBehaviour in componentsInChildren)
			{
				if ((bool)monoBehaviour)
				{
					UnityEngine.Object.DestroyImmediate(monoBehaviour);
				}
			}
			Collider2D[] componentsInChildren2 = gameObject2.GetComponentsInChildren<Collider2D>(includeInactive: true);
			foreach (Collider2D collider2D in componentsInChildren2)
			{
				if ((bool)collider2D)
				{
					UnityEngine.Object.DestroyImmediate(collider2D);
				}
			}
			AudioSource[] componentsInChildren3 = gameObject2.GetComponentsInChildren<AudioSource>(includeInactive: true);
			foreach (AudioSource audioSource in componentsInChildren3)
			{
				if ((bool)audioSource)
				{
					UnityEngine.Object.DestroyImmediate(audioSource);
				}
			}
			Rigidbody2D[] componentsInChildren4 = gameObject2.GetComponentsInChildren<Rigidbody2D>(includeInactive: true);
			foreach (Rigidbody2D rigidbody2D in componentsInChildren4)
			{
				if ((bool)rigidbody2D)
				{
					UnityEngine.Object.DestroyImmediate(rigidbody2D);
				}
			}
			Renderer[] componentsInChildren5 = gameObject2.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren5)
			{
				if ((bool)renderer && !(renderer is ParticleSystemRenderer))
				{
					renderer.enabled = false;
				}
			}
			gameObject2.transform.SetParent(null, worldPositionStays: false);
			gameObject2.transform.position = position;
			gameObject2.transform.localScale = Vector3.one;
			ParticleSystem[] array = gameObject2.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			foreach (ParticleSystem obj in array)
			{
				obj.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
				ParticleSystem.MainModule main = obj.main;
				main.loop = true;
				main.playOnAwake = false;
				main.maxParticles = 90;
				main.simulationSpeed = 1.8f;
				main.startLifetime = 0.65f;
				main.startSpeed = 0.8f;
				main.startSize = 0.45f;
				main.simulationSpace = ParticleSystemSimulationSpace.World;
				main.stopAction = ParticleSystemStopAction.None;
				main.startColor = Color.Lerp(Color.white, p.Color, 0.25f);
				ParticleSystem.EmissionModule emission = obj.emission;
				emission.enabled = true;
				emission.rateOverTime = 40f;
				ParticleSystem.ShapeModule shape = obj.shape;
				shape.enabled = true;
				shape.shapeType = ParticleSystemShapeType.Sphere;
				shape.radius = 0.65f;
				ParticleSystemRenderer component = obj.GetComponent<ParticleSystemRenderer>();
				if ((bool)component)
				{
					component.enabled = true;
				}
				obj.gameObject.SetActive(value: true);
			}
			gameObject2.SetActive(value: true);
			array = gameObject2.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Play();
			}
			Diagnostics.Write("DREAM rescue particles=" + source.name + " P" + (p.Index + 1));
			return gameObject2;
		}
		catch (Exception ex)
		{
			if ((bool)gameObject2)
			{
				UnityEngine.Object.Destroy(gameObject2);
			}
			Diagnostics.Throttled("DREAM native effect", ex);
			return null;
		}
		finally
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	internal static void WhitePulse(PlayerSlot p)
	{
		if (p == null || !p.Hero)
		{
			return;
		}
		GameObject gameObject = Create(p, p.Hero.transform.position);
		if ((bool)gameObject)
		{
			gameObject.name = "Local8 Pale Flower P" + (p.Index + 1);
			gameObject.transform.localScale = Vector3.one * 0.72f;
			ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			foreach (ParticleSystem obj in componentsInChildren)
			{
				ParticleSystem.MainModule main = obj.main;
				main.startColor = Color.white;
				main.startLifetime = 0.42f;
				main.startSpeed = 0.35f;
				main.startSize = 0.38f;
				main.loop = false;
				ParticleSystem.EmissionModule emission = obj.emission;
				emission.rateOverTime = 0f;
				obj.Emit(28);
			}
			if ((bool)Plugin.Self)
			{
				Plugin.Self.StartCoroutine(WhitePulseLife(gameObject, p));
			}
			else
			{
				UnityEngine.Object.Destroy(gameObject, 0.8f);
			}
		}
	}

	[IteratorStateMachine(typeof(__iterator__WhitePulseLife_d__17))]
	private static IEnumerator WhitePulseLife(GameObject effect, PlayerSlot p)
	{
		return new __iterator__WhitePulseLife_d__17(0)
		{
			effect = effect,
			p = p
		};
	}

	internal static void TeleportTrail(PlayerSlot p, Vector3 from, Vector3 to, bool sound = false)
	{
		if (p == null || p.Index < 0 || p.Index >= 8 || Vector2.Distance(from, to) < 1.75f)
		{
			return;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime - trailAt[p.Index] < 0.2f && Vector2.Distance(trailDestination[p.Index], to) < 1.5f)
		{
			return;
		}
		trailAt[p.Index] = unscaledTime;
		trailDestination[p.Index] = to;
		observedPosition[p.Index] = to;
		observedPositionValid[p.Index] = true;
		GameObject gameObject = Create(p, from);
		if ((bool)gameObject)
		{
			if (sound)
			{
				PlayActivation(from);
			}
			ContinueTrail(gameObject, from, to);
			string[] obj = new string[6]
			{
				"DREAM teleport trail P",
				(p.Index + 1).ToString(),
				" ",
				null,
				null,
				null
			};
			Vector3 vector = from;
			obj[3] = vector.ToString();
			obj[4] = " -> ";
			vector = to;
			obj[5] = vector.ToString();
			Diagnostics.Write(string.Concat(obj));
		}
	}

	internal static void ContinueTrail(GameObject effect, Vector3 from, Vector3 to, float linger = 0.8f)
	{
		if ((bool)effect)
		{
			if ((bool)Plugin.Self)
			{
				Plugin.Self.StartCoroutine(Trail(effect, from, to, linger));
			}
			else
			{
				UnityEngine.Object.Destroy(effect);
			}
		}
	}

	[IteratorStateMachine(typeof(__iterator__Trail_d__20))]
	private static IEnumerator Trail(GameObject effect, Vector3 from, Vector3 to, float linger)
	{
		return new __iterator__Trail_d__20(0)
		{
			effect = effect,
			from = from,
			to = to,
			linger = linger
		};
	}
}
