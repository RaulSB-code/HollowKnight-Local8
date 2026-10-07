using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PlayerLighting
{
	private sealed class Layer : IDisposable
	{
		internal SpriteRenderer Source;

		internal bool Light;

		internal readonly GroupVignette Composite = new GroupVignette();

		public void Dispose()
		{
			Composite.Dispose();
		}
	}

	private static readonly List<Layer> layers = new List<Layer>();

	private static readonly SharedVisibility visibility = new SharedVisibility();

	private static readonly Dictionary<SpriteRenderer, bool> hiddenMasks = new Dictionary<SpriteRenderer, bool>();

	private static HeroController cachedHero;

	private static Camera renderingCamera;

	private static float nextProbe;

	private static float nextLog;

	internal static void Install()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
		Camera.onPostRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPostRender, new Camera.CameraCallback(AfterCamera));
	}

	internal static void Uninstall()
	{
		Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
		Camera.onPostRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPostRender, new Camera.CameraCallback(AfterCamera));
		Reset();
	}

	internal static void Reset()
	{
		RestoreMasks();
		visibility.Dispose();
		foreach (Layer layer in layers)
		{
			layer.Dispose();
		}
		layers.Clear();
		cachedHero = null;
		nextProbe = (nextLog = 0f);
	}

	private static void RestoreMasks()
	{
		foreach (KeyValuePair<SpriteRenderer, bool> hiddenMask in hiddenMasks)
		{
			if ((bool)hiddenMask.Key)
			{
				hiddenMask.Key.enabled = hiddenMask.Value;
			}
		}
		hiddenMasks.Clear();
		renderingCamera = null;
	}

	private static void HideMasks()
	{
		foreach (Layer layer in layers)
		{
			if (!layer.Light && (bool)layer.Source)
			{
				if (!hiddenMasks.ContainsKey(layer.Source))
				{
					hiddenMasks[layer.Source] = layer.Source.enabled;
				}
				layer.Source.enabled = false;
			}
		}
	}

	private static void Add(SpriteRenderer r, bool light)
	{
		if (!r)
		{
			return;
		}
		foreach (Layer layer in layers)
		{
			if (layer.Source == r)
			{
				return;
			}
		}
		layers.Add(new Layer
		{
			Source = r,
			Light = light
		});
		Diagnostics.Write("LIGHT source=" + r.name + " id=" + r.GetInstanceID() + " parent=" + (r.transform.parent ? r.transform.parent.name : "none") + " layer=" + r.gameObject.layer + " z=" + r.transform.position.z + " sprite=" + (r.sprite ? r.sprite.name : "none") + " active=" + r.gameObject.activeInHierarchy + " enabled=" + r.enabled + " color=" + r.color.ToString() + " scale=" + r.transform.lossyScale.ToString() + " shader=" + (((bool)r.sharedMaterial && (bool)r.sharedMaterial.shader) ? r.sharedMaterial.shader.name : "none"));
	}

	private static void Probe(HeroController hero)
	{
		if (cachedHero != hero)
		{
			Reset();
			cachedHero = hero;
		}
		if (Time.unscaledTime < nextProbe)
		{
			return;
		}
		nextProbe = Time.unscaledTime + 2f;
		Add(hero.heroLight, light: true);
		GameManager instance = GameManager.instance;
		if ((bool)instance)
		{
			Add(instance.heroLight, light: true);
		}
		if ((bool)hero.vignette)
		{
			SpriteRenderer[] componentsInChildren = hero.vignette.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i], light: false);
			}
		}
		if ((bool)hero.vignetteFSM && hero.vignetteFSM.name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			SpriteRenderer[] componentsInChildren = hero.vignetteFSM.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i], light: false);
			}
		}
		Transform[] componentsInChildren2 = hero.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren2)
		{
			if (transform.name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				SpriteRenderer[] componentsInChildren = transform.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					Add(componentsInChildren[j], light: false);
				}
			}
		}
	}

	private static void BeforeCamera(Camera camera)
	{
		//Discarded unreachable code: IL_007e
		ArenaVisualRecovery.BeforeCamera(camera);
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		if (coopSession == null || !self.Enabled.Value || !coopSession.Active || coopSession.TeamWipe || coopSession.Primary == null || !coopSession.Primary.Hero)
		{
			return;
		}
		GameCameras instance = GameCameras.instance;
		GameManager instance2 = GameManager.instance;
		if (!instance || instance.mainCamera != camera || !instance.mainCamera || !instance2 || !instance2.IsGameplayScene() || instance2.IsLoadingSceneTransition)
		{
			return;
		}
		try
		{
			Probe(coopSession.Primary.Hero);
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (player.Index > 0 && (bool)player.Hero)
				{
					Suppress(player);
				}
			}
			int num = 0;
			HideMasks();
			renderingCamera = camera;
			foreach (Layer layer in layers)
			{
				if (layer.Light && (bool)layer.Source && layer.Composite.Render(camera, instance.mainCamera, coopSession, layer.Source, light: true))
				{
					num++;
				}
			}
			if (camera == instance.mainCamera)
			{
				visibility.Render(camera, coopSession, coopSession.Primary.Hero.vignette, self.GroupDarkness.Value);
			}
			if (!(camera == instance.mainCamera) || !(Time.unscaledTime >= nextLog))
			{
				return;
			}
			nextLog = Time.unscaledTime + 15f;
			Diagnostics.Write("LIGHT shared camera=" + camera.name + " native_masks_hidden=" + hiddenMasks.Count + " glow_layers=" + num + " " + visibility.Status);
			foreach (Layer layer2 in layers)
			{
				if (layer2.Light && (bool)layer2.Source)
				{
					Diagnostics.Write("LIGHT layer=" + layer2.Source.name + " " + layer2.Composite.Status);
				}
			}
		}
		catch (Exception ex)
		{
			visibility.Hide();
			foreach (Layer layer3 in layers)
			{
				layer3.Composite.Restore();
			}
			Diagnostics.Throttled("GROUP LIGHT", ex);
		}
	}

	private static void AfterCamera(Camera camera)
	{
		if (camera == renderingCamera)
		{
			visibility.Hide();
			RestoreMasks();
		}
		foreach (Layer layer in layers)
		{
			layer.Composite.Finish(camera);
		}
	}

	private static void Suppress(PlayerSlot p)
	{
		if (p.LightRenderers == null)
		{
			return;
		}
		Renderer[] lightRenderers = p.LightRenderers;
		foreach (Renderer renderer in lightRenderers)
		{
			if (!renderer || !renderer.transform.IsChildOf(p.Hero.transform))
			{
				continue;
			}
			bool flag = false;
			foreach (Layer layer in layers)
			{
				if (layer.Source == renderer)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				renderer.enabled = false;
			}
		}
	}

	internal static void Sync(PlayerSlot primary, PlayerSlot extra)
	{
		if (primary == null || !primary.Hero || extra == null || !extra.Hero)
		{
			return;
		}
		extra.Hero.wieldingLantern = primary.Hero.wieldingLantern;
		if (!(extra.LightOwner != extra.Hero))
		{
			return;
		}
		extra.LightOwner = extra.Hero;
		List<Renderer> list = new List<Renderer>();
		HeroController hero = extra.Hero;
		if ((bool)hero.heroLight && hero.heroLight != primary.Hero.heroLight && hero.heroLight.transform.IsChildOf(hero.transform))
		{
			list.Add(hero.heroLight);
		}
		if ((bool)hero.vignette && hero.vignette != primary.Hero.vignette && hero.vignette.transform.IsChildOf(hero.transform))
		{
			list.AddRange(hero.vignette.GetComponentsInChildren<Renderer>(includeInactive: true));
		}
		Transform[] componentsInChildren = hero.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			string name = transform.name;
			if (name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("HeroLight", StringComparison.OrdinalIgnoreCase) >= 0 || name.StartsWith("Local8 Hero Light"))
			{
				list.AddRange(transform.GetComponentsInChildren<Renderer>(includeInactive: true));
			}
		}
		extra.LightRenderers = list.ToArray();
		Diagnostics.Write("LIGHT secondary P" + (extra.Index + 1) + " suppressed layers=" + list.Count);
	}
}
