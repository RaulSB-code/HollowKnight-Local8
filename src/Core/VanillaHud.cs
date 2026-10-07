using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class VanillaHud
{
	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly List<Renderer> renderers = new List<Renderer>(160);

	private static int scanned = -1;

	private static bool installed;

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPreRender, new Camera.CameraCallback(BeforeCamera));
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeCamera));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPreRender, new Camera.CameraCallback(BeforeCamera));
			Restore();
		}
	}

	internal static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = item.Value;
			}
		}
		hidden.Clear();
		renderers.Clear();
		scanned = -1;
	}

	private static bool Vitals(Transform t, Transform root)
	{
		while ((bool)t && t != root)
		{
			string name = t.name;
			if (name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("soul", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("orb", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			t = t.parent;
		}
		return false;
	}

	private static void BeforeCamera(Camera camera)
	{
		PvpGeoHud.BeforeCamera(camera);
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = ((self == null) ? null : self.Session);
		GameManager instance = GameManager.instance;
		if (self == null || !self.Enabled.Value || !self.HideOriginalHud.Value || coopSession == null || !coopSession.Active || !instance || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition)
		{
			if (hidden.Count > 0)
			{
				Restore();
			}
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!instance2 || !instance2.hudCanvas)
		{
			return;
		}
		if (scanned != Time.frameCount)
		{
			scanned = Time.frameCount;
			renderers.Clear();
			instance2.hudCanvas.GetComponentsInChildren(includeInactive: true, renderers);
			foreach (Renderer renderer in renderers)
			{
				if ((bool)renderer && !hidden.ContainsKey(renderer) && Vitals(renderer.transform, instance2.hudCanvas.transform))
				{
					hidden[renderer] = renderer.forceRenderingOff;
				}
			}
		}
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if ((bool)item.Key)
			{
				item.Key.forceRenderingOff = true;
			}
		}
	}
}
