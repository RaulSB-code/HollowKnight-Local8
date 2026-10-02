using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class VanillaHud
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CameraCallback _003C0_003E__BeforeCamera;
	}

	private static readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();

	private static readonly List<Renderer> renderers = new List<Renderer>(160);

	private static int scanned = -1;

	private static bool installed;

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			CameraCallback onPreCull = Camera.onPreCull;
			object obj = _003C_003EO._003C0_003E__BeforeCamera;
			if (obj == null)
			{
				CameraCallback val = BeforeCamera;
				_003C_003EO._003C0_003E__BeforeCamera = val;
				obj = (object)val;
			}
			Camera.onPreCull = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreCull, (Delegate?)obj);
			CameraCallback onPreRender = Camera.onPreRender;
			object obj2 = _003C_003EO._003C0_003E__BeforeCamera;
			if (obj2 == null)
			{
				CameraCallback val2 = BeforeCamera;
				_003C_003EO._003C0_003E__BeforeCamera = val2;
				obj2 = (object)val2;
			}
			Camera.onPreRender = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreRender, (Delegate?)obj2);
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			CameraCallback onPreCull = Camera.onPreCull;
			object obj = _003C_003EO._003C0_003E__BeforeCamera;
			if (obj == null)
			{
				CameraCallback val = BeforeCamera;
				_003C_003EO._003C0_003E__BeforeCamera = val;
				obj = (object)val;
			}
			Camera.onPreCull = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreCull, (Delegate?)obj);
			CameraCallback onPreRender = Camera.onPreRender;
			object obj2 = _003C_003EO._003C0_003E__BeforeCamera;
			if (obj2 == null)
			{
				CameraCallback val2 = BeforeCamera;
				_003C_003EO._003C0_003E__BeforeCamera = val2;
				obj2 = (object)val2;
			}
			Camera.onPreRender = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreRender, (Delegate?)obj2);
			Restore();
		}
	}

	internal static void Restore()
	{
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
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
		while (Object.op_Implicit((Object)(object)t) && (Object)(object)t != (Object)(object)root)
		{
			string name = ((Object)t).name;
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
		Local8Runtime self = Plugin.Self;
		CoopSession coopSession = (((Object)(object)self == (Object)null) ? null : self.Session);
		GameManager instance = GameManager.instance;
		if ((Object)(object)self == (Object)null || !self.Enabled.Value || !self.HideOriginalHud.Value || coopSession == null || !coopSession.Active || !Object.op_Implicit((Object)(object)instance) || !instance.IsGameplayScene() || instance.IsLoadingSceneTransition)
		{
			if (hidden.Count > 0)
			{
				Restore();
			}
			return;
		}
		GameCameras instance2 = GameCameras.instance;
		if (!Object.op_Implicit((Object)(object)instance2) || !Object.op_Implicit((Object)(object)instance2.hudCanvas))
		{
			return;
		}
		if (scanned != Time.frameCount)
		{
			scanned = Time.frameCount;
			renderers.Clear();
			instance2.hudCanvas.GetComponentsInChildren<Renderer>(true, renderers);
			foreach (Renderer renderer in renderers)
			{
				if (Object.op_Implicit((Object)(object)renderer) && !hidden.ContainsKey(renderer) && Vitals(((Component)renderer).transform, instance2.hudCanvas.transform))
				{
					hidden[renderer] = renderer.forceRenderingOff;
				}
			}
		}
		foreach (KeyValuePair<Renderer, bool> item in hidden)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				item.Key.forceRenderingOff = true;
			}
		}
	}
}
