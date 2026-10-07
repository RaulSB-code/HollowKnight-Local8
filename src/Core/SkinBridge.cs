using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class SkinBridge
{
	private static Type managerType;

	private static MethodInfo getInstalled;

	private static MethodInfo getCurrent;

	private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

	private static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

	private static bool installed;

	private static float nextProbe;

	private static float nextError;

	private static readonly HashSet<string> missing = new HashSet<string>();

	private static readonly Dictionary<string, Texture2D> defaults = new Dictionary<string, Texture2D>();

	internal static bool Available
	{
		get
		{
			Probe();
			return managerType != null;
		}
	}

	internal static void Install()
	{
		if (!installed)
		{
			installed = true;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(BeforeRender));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPreRender, new Camera.CameraCallback(BeforeRender));
		}
	}

	internal static void Uninstall()
	{
		if (installed)
		{
			installed = false;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(BeforeRender));
			Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPreRender, new Camera.CameraCallback(BeforeRender));
		}
	}

	private static void BeforeRender(Camera camera)
	{
		Local8Runtime self = Plugin.Self;
		if (!self || !self.Enabled.Value || self.Session == null || !self.Session.Active)
		{
			return;
		}
		foreach (PlayerSlot player in self.Session.Players)
		{
			if (!player.SkinApplied || !player.SkinRenderer || !player.SkinTexture)
			{
				continue;
			}
			if (SpecialSprite(player))
			{
				Suspend(player);
			}
			else if (!player.SkinSuspended)
			{
				player.SkinRenderer.GetPropertyBlock(block);
				block.SetTexture("_MainTex", player.SkinTexture);
				player.SkinRenderer.SetPropertyBlock(block);
				if (player.Down || player.Hazard)
				{
					player.SkinRenderer.enabled = false;
				}
			}
		}
	}

	internal static Texture2D DefaultTexture(string file)
	{
		Probe();
		if (managerType == null)
		{
			return null;
		}
		FieldInfo field = managerType.GetField("Skinables", BindingFlags.Static | BindingFlags.Public);
		IDictionary dictionary = ((field == null) ? null : (field.GetValue(null) as IDictionary));
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
		if (dictionary == null || !dictionary.Contains(fileNameWithoutExtension))
		{
			return null;
		}
		object obj = dictionary[fileNameWithoutExtension];
		PropertyInfo property = obj.GetType().GetProperty("ckTex");
		object obj2 = ((property == null) ? null : property.GetValue(obj, null));
		if (obj2 == null)
		{
			return null;
		}
		FieldInfo field2 = obj2.GetType().GetField("defaultTex");
		Texture2D texture2D = ((field2 == null) ? null : (field2.GetValue(obj2) as Texture2D));
		if ((bool)texture2D)
		{
			return texture2D;
		}
		FieldInfo field3 = obj2.GetType().GetField("defaultSprite");
		Sprite sprite = ((field3 == null) ? null : (field3.GetValue(obj2) as Sprite));
		if (!sprite)
		{
			return null;
		}
		if (defaults.TryGetValue(file, out var value) && (bool)value)
		{
			return value;
		}
		value = HudAssets.FromSprite(sprite);
		if ((bool)value)
		{
			defaults[file] = value;
		}
		return value;
	}

	internal static Texture2D ImageTexture(PlayerSlot p, string file)
	{
		try
		{
			object obj = Find(p.SkinId);
			if (obj == null)
			{
				return null;
			}
			string text = Id(obj) + "/" + file;
			if (textures.TryGetValue(text, out var value) && (bool)value)
			{
				return value;
			}
			if (missing.Contains(text))
			{
				return DefaultTexture(file);
			}
			MethodInfo method = obj.GetType().GetMethod("Exists");
			if (method != null && !(bool)method.Invoke(obj, new object[1] { file }))
			{
				missing.Add(text);
				return DefaultTexture(file);
			}
			MethodInfo method2 = obj.GetType().GetMethod("GetTexture");
			value = ((method2 == null) ? null : (method2.Invoke(obj, new object[1] { file }) as Texture2D));
			if ((bool)value)
			{
				textures[text] = value;
			}
			return value ?? DefaultTexture(file);
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("SKIN RESOURCE " + file, ex);
			return null;
		}
	}

	internal static void ClearCache()
	{
		foreach (Texture2D value in textures.Values)
		{
			if ((bool)value)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		textures.Clear();
		foreach (Texture2D value2 in defaults.Values)
		{
			if ((bool)value2)
			{
				UnityEngine.Object.Destroy(value2);
			}
		}
		defaults.Clear();
		missing.Clear();
	}

	private static void Probe()
	{
		if (!(managerType != null) && !(Time.unscaledTime < nextProbe))
		{
			nextProbe = Time.unscaledTime + 2f;
			managerType = Type.GetType("CustomKnight.SkinManager, CustomKnight", throwOnError: false);
			if (!(managerType == null))
			{
				getInstalled = managerType.GetMethod("GetInstalledSkins", BindingFlags.Static | BindingFlags.Public);
				getCurrent = managerType.GetMethod("GetCurrentSkin", BindingFlags.Static | BindingFlags.Public);
				Diagnostics.Write("CUSTOM KNIGHT detected");
			}
		}
	}

	private static string Id(object skin)
	{
		if (skin == null)
		{
			return "";
		}
		MethodInfo method = skin.GetType().GetMethod("GetId");
		if (!(method == null))
		{
			return (string)method.Invoke(skin, null);
		}
		return "";
	}

	private static string Name(object skin)
	{
		if (skin == null)
		{
			return "";
		}
		MethodInfo method = skin.GetType().GetMethod("GetName");
		if (!(method == null))
		{
			return (string)method.Invoke(skin, null);
		}
		return Id(skin);
	}

	private static object Find(string id)
	{
		Probe();
		if (managerType == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(id))
		{
			if (!(getCurrent == null))
			{
				return getCurrent.Invoke(null, null);
			}
			return null;
		}
		IEnumerable enumerable = ((getInstalled == null) ? null : (getInstalled.Invoke(null, null) as IEnumerable));
		if (enumerable == null)
		{
			return null;
		}
		foreach (object item in enumerable)
		{
			if (string.Equals(Id(item), id, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		if (!(getCurrent == null))
		{
			return getCurrent.Invoke(null, null);
		}
		return null;
	}

	private static Texture2D Texture(object skin)
	{
		if (skin == null)
		{
			return null;
		}
		string key = Id(skin);
		if (textures.TryGetValue(key, out var value) && (bool)value)
		{
			return value;
		}
		MethodInfo method = skin.GetType().GetMethod("Exists");
		if (method != null && !(bool)method.Invoke(skin, new object[1] { "Knight.png" }))
		{
			return null;
		}
		MethodInfo method2 = skin.GetType().GetMethod("GetTexture");
		value = ((method2 == null) ? null : (method2.Invoke(skin, new object[1] { "Knight.png" }) as Texture2D));
		if ((bool)value)
		{
			textures[key] = value;
		}
		return value;
	}

	private static bool SpecialSprite(PlayerSlot p)
	{
		if (p == null || !p.Hero || (!p.Hero.IsDreamReturning && (!p.AcidAssistActive || !AcidSwimming.HasSwimClip(p))))
		{
			return false;
		}
		return true;
	}

	private static void Suspend(PlayerSlot p)
	{
		if ((bool)p.SkinRenderer)
		{
			tk2dSprite component = p.Hero.GetComponent<tk2dSprite>();
			tk2dSpriteDefinition tk2dSpriteDefinition = (component ? component.GetCurrentSpriteDef() : null);
			if (tk2dSpriteDefinition != null && (bool)tk2dSpriteDefinition.material && p.SkinRenderer.sharedMaterial == p.SkinMaterial)
			{
				p.SkinRenderer.sharedMaterial = tk2dSpriteDefinition.material;
			}
			p.SkinRenderer.GetPropertyBlock(block);
			if (tk2dSpriteDefinition != null && (bool)tk2dSpriteDefinition.material && (bool)tk2dSpriteDefinition.material.mainTexture)
			{
				block.SetTexture("_MainTex", tk2dSpriteDefinition.material.mainTexture);
			}
			p.SkinRenderer.SetPropertyBlock(block);
			p.SkinSuspended = true;
		}
	}

	internal static void Apply(PlayerSlot p)
	{
		if (p == null || !p.Hero)
		{
			return;
		}
		try
		{
			object obj = Find(p.SkinId);
			if (obj == null)
			{
				p.SkinApplied = false;
				return;
			}
			string text = Id(obj);
			Texture2D texture2D = Texture(obj);
			if (!texture2D)
			{
				p.SkinApplied = false;
				return;
			}
			Renderer renderer = p.Hero.GetComponent<Renderer>();
			if (!renderer)
			{
				renderer = p.Hero.GetComponentInChildren<Renderer>(includeInactive: true);
			}
			if (!renderer)
			{
				p.SkinApplied = false;
				return;
			}
			if (p.SkinApplied && SpecialSprite(p))
			{
				Suspend(p);
				return;
			}
			if (!p.SkinApplied)
			{
				tk2dSprite component = p.Hero.GetComponent<tk2dSprite>();
				tk2dSpriteDefinition tk2dSpriteDefinition = (component ? component.GetCurrentSpriteDef() : null);
				if (tk2dSpriteDefinition != null && (bool)tk2dSpriteDefinition.material && p.Hero.IsDreamReturning)
				{
					return;
				}
			}
			if (p.AppliedSkinId != text || p.SkinRenderer != renderer || p.SkinTexture != texture2D || !p.SkinMaterial)
			{
				Release(p);
				p.SkinRenderer = renderer;
				p.SkinBaseMaterial = renderer.sharedMaterial;
				p.SkinTexture = texture2D;
				tk2dSprite component2 = p.Hero.GetComponent<tk2dSprite>();
				tk2dSpriteDefinition tk2dSpriteDefinition2 = (component2 ? component2.GetCurrentSpriteDef() : null);
				p.SkinSourceTexture = ((tk2dSpriteDefinition2 == null || !tk2dSpriteDefinition2.material) ? null : tk2dSpriteDefinition2.material.mainTexture);
				p.SkinOriginalProperties = new MaterialPropertyBlock();
				renderer.GetPropertyBlock(p.SkinOriginalProperties);
				if ((bool)p.SkinBaseMaterial)
				{
					p.SkinMaterial = new Material(p.SkinBaseMaterial);
					p.SkinMaterial.name = "Local8 P" + (p.Index + 1) + " " + text;
					p.SkinMaterial.mainTexture = texture2D;
					if (p.SkinMaterial.HasProperty("_MainTex"))
					{
						p.SkinMaterial.SetTexture("_MainTex", texture2D);
					}
				}
				p.AppliedSkinId = text;
				Diagnostics.Write("SKIN APPLY P" + (p.Index + 1) + "=" + text + " tex=" + texture2D.width + "x" + texture2D.height + " renderer=" + renderer.name);
			}
			if ((bool)p.SkinMaterial && renderer.sharedMaterial != p.SkinMaterial)
			{
				renderer.sharedMaterial = p.SkinMaterial;
			}
			p.SkinSuspended = false;
			renderer.GetPropertyBlock(block);
			block.SetTexture("_MainTex", texture2D);
			renderer.SetPropertyBlock(block);
			p.SkinApplied = p.SkinMaterial;
		}
		catch (Exception ex)
		{
			p.SkinApplied = false;
			if (Time.unscaledTime >= nextError)
			{
				nextError = Time.unscaledTime + 3f;
				Diagnostics.Write("SKIN ERROR P" + (p.Index + 1) + " " + ex);
			}
		}
	}

	internal static void Release(PlayerSlot p)
	{
		if (p != null)
		{
			if ((bool)p.SkinRenderer && (bool)p.SkinMaterial && p.SkinRenderer.sharedMaterial == p.SkinMaterial)
			{
				p.SkinRenderer.sharedMaterial = p.SkinBaseMaterial;
			}
			if ((bool)p.SkinRenderer && p.SkinOriginalProperties != null)
			{
				p.SkinRenderer.SetPropertyBlock(p.SkinOriginalProperties);
			}
			if ((bool)p.SkinMaterial)
			{
				UnityEngine.Object.Destroy(p.SkinMaterial);
			}
			p.SkinRenderer = null;
			p.SkinMaterial = null;
			p.SkinBaseMaterial = null;
			p.SkinTexture = null;
			p.SkinSourceTexture = null;
			p.SkinOriginalProperties = null;
			p.AppliedSkinId = null;
			p.SkinApplied = false;
			p.SkinSuspended = false;
		}
	}

	internal static bool HasSkin(PlayerSlot p)
	{
		return p?.SkinApplied ?? false;
	}

	internal static string Display(PlayerSlot p)
	{
		object obj = Find(p?.SkinId);
		if (obj != null)
		{
			return Name(obj);
		}
		return "Custom Knight no detectado";
	}

	internal static void Cycle(PlayerSlot p, int direction)
	{
		Probe();
		if (p == null || managerType == null)
		{
			return;
		}
		IEnumerable enumerable = ((getInstalled == null) ? null : (getInstalled.Invoke(null, null) as IEnumerable));
		if (enumerable == null)
		{
			return;
		}
		List<object> list = new List<object>();
		foreach (object item in enumerable)
		{
			list.Add(item);
		}
		if (list.Count != 0)
		{
			string current = Id(Find(p.SkinId));
			int num = list.FindIndex((object x) => Id(x) == current);
			if (num < 0)
			{
				num = 0;
			}
			num = (num + direction + list.Count) % list.Count;
			Release(p);
			p.SkinId = Id(list[num]);
			Local8Mod.Settings.SkinIds[p.Index] = p.SkinId;
			Diagnostics.Write("SKIN SELECT P" + (p.Index + 1) + "=" + p.SkinId);
		}
	}
}
