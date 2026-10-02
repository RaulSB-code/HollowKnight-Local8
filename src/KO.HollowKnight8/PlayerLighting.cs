using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CameraCallback _003C0_003E__BeforeCamera;

		public static CameraCallback _003C1_003E__AfterCamera;
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
		CameraCallback onPreCull = Camera.onPreCull;
		object obj = _003C_003EO._003C0_003E__BeforeCamera;
		if (obj == null)
		{
			CameraCallback val = BeforeCamera;
			_003C_003EO._003C0_003E__BeforeCamera = val;
			obj = (object)val;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreCull, (Delegate?)obj);
		CameraCallback onPostRender = Camera.onPostRender;
		object obj2 = _003C_003EO._003C1_003E__AfterCamera;
		if (obj2 == null)
		{
			CameraCallback val2 = AfterCamera;
			_003C_003EO._003C1_003E__AfterCamera = val2;
			obj2 = (object)val2;
		}
		Camera.onPostRender = (CameraCallback)Delegate.Combine((Delegate?)(object)onPostRender, (Delegate?)obj2);
	}

	internal static void Uninstall()
	{
		CameraCallback onPreCull = Camera.onPreCull;
		object obj = _003C_003EO._003C0_003E__BeforeCamera;
		if (obj == null)
		{
			CameraCallback val = BeforeCamera;
			_003C_003EO._003C0_003E__BeforeCamera = val;
			obj = (object)val;
		}
		Camera.onPreCull = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreCull, (Delegate?)obj);
		CameraCallback onPostRender = Camera.onPostRender;
		object obj2 = _003C_003EO._003C1_003E__AfterCamera;
		if (obj2 == null)
		{
			CameraCallback val2 = AfterCamera;
			_003C_003EO._003C1_003E__AfterCamera = val2;
			obj2 = (object)val2;
		}
		Camera.onPostRender = (CameraCallback)Delegate.Remove((Delegate?)(object)onPostRender, (Delegate?)obj2);
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
			if (Object.op_Implicit((Object)(object)hiddenMask.Key))
			{
				((Renderer)hiddenMask.Key).enabled = hiddenMask.Value;
			}
		}
		hiddenMasks.Clear();
		renderingCamera = null;
	}

	private static void HideMasks()
	{
		foreach (Layer layer in layers)
		{
			if (!layer.Light && Object.op_Implicit((Object)(object)layer.Source))
			{
				if (!hiddenMasks.ContainsKey(layer.Source))
				{
					hiddenMasks[layer.Source] = ((Renderer)layer.Source).enabled;
				}
				((Renderer)layer.Source).enabled = false;
			}
		}
	}

	private static void Add(SpriteRenderer r, bool light)
	{
		if (!Object.op_Implicit((Object)(object)r))
		{
			return;
		}
		foreach (Layer layer in layers)
		{
			if ((Object)(object)layer.Source == (Object)(object)r)
			{
				return;
			}
		}
		layers.Add(new Layer
		{
			Source = r,
			Light = light
		});
		Diagnostics.Write("LIGHT source=" + ((Object)r).name + " id=" + ((Object)r).GetInstanceID() + " parent=" + (Object.op_Implicit((Object)(object)((Component)r).transform.parent) ? ((Object)((Component)r).transform.parent).name : "none") + " layer=" + ((Component)r).gameObject.layer + " z=" + ((Component)r).transform.position.z + " sprite=" + (Object.op_Implicit((Object)(object)r.sprite) ? ((Object)r.sprite).name : "none") + " active=" + ((Component)r).gameObject.activeInHierarchy + " enabled=" + ((Renderer)r).enabled + " color=" + ((object)r.color/*cast due to .constrained prefix*/).ToString() + " scale=" + ((object)((Component)r).transform.lossyScale/*cast due to .constrained prefix*/).ToString() + " shader=" + ((Object.op_Implicit((Object)(object)((Renderer)r).sharedMaterial) && Object.op_Implicit((Object)(object)((Renderer)r).sharedMaterial.shader)) ? ((Object)((Renderer)r).sharedMaterial.shader).name : "none"));
	}

	private static void Probe(HeroController hero)
	{
		if ((Object)(object)cachedHero != (Object)(object)hero)
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
		if (Object.op_Implicit((Object)(object)instance))
		{
			Add(instance.heroLight, light: true);
		}
		if (Object.op_Implicit((Object)(object)hero.vignette))
		{
			SpriteRenderer[] componentsInChildren = ((Component)hero.vignette).GetComponentsInChildren<SpriteRenderer>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i], light: false);
			}
		}
		if (Object.op_Implicit((Object)(object)hero.vignetteFSM) && ((Object)hero.vignetteFSM).name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			SpriteRenderer[] componentsInChildren = ((Component)hero.vignetteFSM).GetComponentsInChildren<SpriteRenderer>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i], light: false);
			}
		}
		Transform[] componentsInChildren2 = ((Component)hero).GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren2)
		{
			if (((Object)val).name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				SpriteRenderer[] componentsInChildren = ((Component)val).GetComponentsInChildren<SpriteRenderer>(true);
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					Add(componentsInChildren[j], light: false);
				}
			}
		}
	}

	private static void BeforeCamera(Camera camera)
	{
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
		if (coopSession == null || !self.Enabled.Value || !coopSession.Active || coopSession.TeamWipe || coopSession.Primary == null || !Object.op_Implicit((Object)(object)coopSession.Primary.Hero))
		{
			return;
		}
		GameCameras instance = GameCameras.instance;
		GameManager instance2 = GameManager.instance;
		if (!Object.op_Implicit((Object)(object)instance) || (Object)(object)instance.mainCamera != (Object)(object)camera || !Object.op_Implicit((Object)(object)instance.mainCamera) || !Object.op_Implicit((Object)(object)instance2) || !instance2.IsGameplayScene() || instance2.IsLoadingSceneTransition)
		{
			return;
		}
		try
		{
			Probe(coopSession.Primary.Hero);
			foreach (PlayerSlot player in coopSession.Players)
			{
				if (player.Index > 0 && Object.op_Implicit((Object)(object)player.Hero))
				{
					Suppress(player);
				}
			}
			int num = 0;
			HideMasks();
			renderingCamera = camera;
			foreach (Layer layer in layers)
			{
				if (layer.Light && Object.op_Implicit((Object)(object)layer.Source) && layer.Composite.Render(camera, instance.mainCamera, coopSession, layer.Source, light: true))
				{
					num++;
				}
			}
			if ((Object)(object)camera == (Object)(object)instance.mainCamera)
			{
				visibility.Render(camera, coopSession, coopSession.Primary.Hero.vignette, self.GroupDarkness.Value);
			}
			if (!((Object)(object)camera == (Object)(object)instance.mainCamera) || !(Time.unscaledTime >= nextLog))
			{
				return;
			}
			nextLog = Time.unscaledTime + 15f;
			Diagnostics.Write("LIGHT shared camera=" + ((Object)camera).name + " native_masks_hidden=" + hiddenMasks.Count + " glow_layers=" + num + " " + visibility.Status);
			foreach (Layer layer2 in layers)
			{
				if (layer2.Light && Object.op_Implicit((Object)(object)layer2.Source))
				{
					Diagnostics.Write("LIGHT layer=" + ((Object)layer2.Source).name + " " + layer2.Composite.Status);
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
		if ((Object)(object)camera == (Object)(object)renderingCamera)
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
		foreach (Renderer val in lightRenderers)
		{
			if (!Object.op_Implicit((Object)(object)val) || !((Component)val).transform.IsChildOf(((Component)p.Hero).transform))
			{
				continue;
			}
			bool flag = false;
			foreach (Layer layer in layers)
			{
				if ((Object)(object)layer.Source == (Object)(object)val)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				val.enabled = false;
			}
		}
	}

	internal static void Sync(PlayerSlot primary, PlayerSlot extra)
	{
		if (primary == null || !Object.op_Implicit((Object)(object)primary.Hero) || extra == null || !Object.op_Implicit((Object)(object)extra.Hero))
		{
			return;
		}
		extra.Hero.wieldingLantern = primary.Hero.wieldingLantern;
		if (!((Object)(object)extra.LightOwner != (Object)(object)extra.Hero))
		{
			return;
		}
		extra.LightOwner = extra.Hero;
		List<Renderer> list = new List<Renderer>();
		HeroController hero = extra.Hero;
		if (Object.op_Implicit((Object)(object)hero.heroLight) && (Object)(object)hero.heroLight != (Object)(object)primary.Hero.heroLight && ((Component)hero.heroLight).transform.IsChildOf(((Component)hero).transform))
		{
			list.Add((Renderer)(object)hero.heroLight);
		}
		if (Object.op_Implicit((Object)(object)hero.vignette) && (Object)(object)hero.vignette != (Object)(object)primary.Hero.vignette && ((Component)hero.vignette).transform.IsChildOf(((Component)hero).transform))
		{
			list.AddRange(((Component)hero.vignette).GetComponentsInChildren<Renderer>(true));
		}
		Transform[] componentsInChildren = ((Component)hero).GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			string name = ((Object)val).name;
			if (name.IndexOf("vignette", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("HeroLight", StringComparison.OrdinalIgnoreCase) >= 0 || name.StartsWith("Local8 Hero Light"))
			{
				list.AddRange(((Component)val).GetComponentsInChildren<Renderer>(true));
			}
		}
		extra.LightRenderers = list.ToArray();
		Diagnostics.Write("LIGHT secondary P" + (extra.Index + 1) + " suppressed layers=" + list.Count);
	}
}
