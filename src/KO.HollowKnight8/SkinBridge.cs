using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class SkinBridge
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CameraCallback _003C0_003E__BeforeRender;
	}

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
			CameraCallback onPreCull = Camera.onPreCull;
			object obj = _003C_003EO._003C0_003E__BeforeRender;
			if (obj == null)
			{
				CameraCallback val = BeforeRender;
				_003C_003EO._003C0_003E__BeforeRender = val;
				obj = (object)val;
			}
			Camera.onPreCull = (CameraCallback)Delegate.Combine((Delegate?)(object)onPreCull, (Delegate?)obj);
			CameraCallback onPreRender = Camera.onPreRender;
			object obj2 = _003C_003EO._003C0_003E__BeforeRender;
			if (obj2 == null)
			{
				CameraCallback val2 = BeforeRender;
				_003C_003EO._003C0_003E__BeforeRender = val2;
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
			object obj = _003C_003EO._003C0_003E__BeforeRender;
			if (obj == null)
			{
				CameraCallback val = BeforeRender;
				_003C_003EO._003C0_003E__BeforeRender = val;
				obj = (object)val;
			}
			Camera.onPreCull = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreCull, (Delegate?)obj);
			CameraCallback onPreRender = Camera.onPreRender;
			object obj2 = _003C_003EO._003C0_003E__BeforeRender;
			if (obj2 == null)
			{
				CameraCallback val2 = BeforeRender;
				_003C_003EO._003C0_003E__BeforeRender = val2;
				obj2 = (object)val2;
			}
			Camera.onPreRender = (CameraCallback)Delegate.Remove((Delegate?)(object)onPreRender, (Delegate?)obj2);
		}
	}

	private static void BeforeRender(Camera camera)
	{
		Local8Runtime self = Plugin.Self;
		if (!Object.op_Implicit((Object)(object)self) || !self.Enabled.Value || self.Session == null || !self.Session.Active)
		{
			return;
		}
		foreach (PlayerSlot player in self.Session.Players)
		{
			if (!player.SkinApplied || !Object.op_Implicit((Object)(object)player.SkinRenderer) || !Object.op_Implicit((Object)(object)player.SkinTexture))
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
				block.SetTexture("_MainTex", (Texture)(object)player.SkinTexture);
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
		Texture2D val = (Texture2D)((field2 == null) ? null : /*isinst with value type is only supported in some contexts*/);
		if (Object.op_Implicit((Object)(object)val))
		{
			return val;
		}
		FieldInfo field3 = obj2.GetType().GetField("defaultSprite");
		Sprite val2 = (Sprite)((field3 == null) ? null : /*isinst with value type is only supported in some contexts*/);
		if (!Object.op_Implicit((Object)(object)val2))
		{
			return null;
		}
		if (defaults.TryGetValue(file, out var value) && Object.op_Implicit((Object)(object)value))
		{
			return value;
		}
		value = HudAssets.FromSprite(val2);
		if (Object.op_Implicit((Object)(object)value))
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
			if (textures.TryGetValue(text, out var value) && Object.op_Implicit((Object)(object)value))
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
			value = (Texture2D)((method2 == null) ? null : /*isinst with value type is only supported in some contexts*/);
			if (Object.op_Implicit((Object)(object)value))
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
			if (Object.op_Implicit((Object)(object)value))
			{
				Object.Destroy((Object)(object)value);
			}
		}
		textures.Clear();
		foreach (Texture2D value2 in defaults.Values)
		{
			if (Object.op_Implicit((Object)(object)value2))
			{
				Object.Destroy((Object)(object)value2);
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
		if (textures.TryGetValue(key, out var value) && Object.op_Implicit((Object)(object)value))
		{
			return value;
		}
		MethodInfo method = skin.GetType().GetMethod("Exists");
		if (method != null && !(bool)method.Invoke(skin, new object[1] { "Knight.png" }))
		{
			return null;
		}
		MethodInfo method2 = skin.GetType().GetMethod("GetTexture");
		value = (Texture2D)((method2 == null) ? null : /*isinst with value type is only supported in some contexts*/);
		if (Object.op_Implicit((Object)(object)value))
		{
			textures[key] = value;
		}
		return value;
	}

	private static bool SpecialSprite(PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero) || (!p.Hero.IsDreamReturning && (!p.AcidAssistActive || !AcidSwimming.HasSwimClip(p))))
		{
			return false;
		}
		return true;
	}

	private static void Suspend(PlayerSlot p)
	{
		if (Object.op_Implicit((Object)(object)p.SkinRenderer))
		{
			tk2dSprite component = ((Component)p.Hero).GetComponent<tk2dSprite>();
			tk2dSpriteDefinition val = (Object.op_Implicit((Object)(object)component) ? ((tk2dBaseSprite)component).GetCurrentSpriteDef() : null);
			if (val != null && Object.op_Implicit((Object)(object)val.material) && (Object)(object)p.SkinRenderer.sharedMaterial == (Object)(object)p.SkinMaterial)
			{
				p.SkinRenderer.sharedMaterial = val.material;
			}
			p.SkinRenderer.GetPropertyBlock(block);
			if (val != null && Object.op_Implicit((Object)(object)val.material) && Object.op_Implicit((Object)(object)val.material.mainTexture))
			{
				block.SetTexture("_MainTex", val.material.mainTexture);
			}
			p.SkinRenderer.SetPropertyBlock(block);
			p.SkinSuspended = true;
		}
	}

	internal static void Apply(PlayerSlot p)
	{
		if (p == null || !Object.op_Implicit((Object)(object)p.Hero))
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
			Texture2D val = Texture(obj);
			if (!Object.op_Implicit((Object)(object)val))
			{
				p.SkinApplied = false;
				return;
			}
			Renderer val2 = ((Component)p.Hero).GetComponent<Renderer>();
			if (!Object.op_Implicit((Object)(object)val2))
			{
				val2 = ((Component)p.Hero).GetComponentInChildren<Renderer>(true);
			}
			if (!Object.op_Implicit((Object)(object)val2))
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
				tk2dSprite component = ((Component)p.Hero).GetComponent<tk2dSprite>();
				tk2dSpriteDefinition val3 = (Object.op_Implicit((Object)(object)component) ? ((tk2dBaseSprite)component).GetCurrentSpriteDef() : null);
				if (val3 != null && Object.op_Implicit((Object)(object)val3.material) && p.Hero.IsDreamReturning)
				{
					return;
				}
			}
			if (p.AppliedSkinId != text || (Object)(object)p.SkinRenderer != (Object)(object)val2 || (Object)(object)p.SkinTexture != (Object)(object)val || !Object.op_Implicit((Object)(object)p.SkinMaterial))
			{
				Release(p);
				p.SkinRenderer = val2;
				p.SkinBaseMaterial = val2.sharedMaterial;
				p.SkinTexture = val;
				tk2dSprite component2 = ((Component)p.Hero).GetComponent<tk2dSprite>();
				tk2dSpriteDefinition val4 = (Object.op_Implicit((Object)(object)component2) ? ((tk2dBaseSprite)component2).GetCurrentSpriteDef() : null);
				p.SkinSourceTexture = ((val4 == null || !Object.op_Implicit((Object)(object)val4.material)) ? null : val4.material.mainTexture);
				p.SkinOriginalProperties = new MaterialPropertyBlock();
				val2.GetPropertyBlock(p.SkinOriginalProperties);
				if (Object.op_Implicit((Object)(object)p.SkinBaseMaterial))
				{
					p.SkinMaterial = new Material(p.SkinBaseMaterial);
					((Object)p.SkinMaterial).name = "Local8 P" + (p.Index + 1) + " " + text;
					p.SkinMaterial.mainTexture = (Texture)(object)val;
					if (p.SkinMaterial.HasProperty("_MainTex"))
					{
						p.SkinMaterial.SetTexture("_MainTex", (Texture)(object)val);
					}
				}
				p.AppliedSkinId = text;
				Diagnostics.Write("SKIN APPLY P" + (p.Index + 1) + "=" + text + " tex=" + ((Texture)val).width + "x" + ((Texture)val).height + " renderer=" + ((Object)val2).name);
			}
			if (Object.op_Implicit((Object)(object)p.SkinMaterial) && (Object)(object)val2.sharedMaterial != (Object)(object)p.SkinMaterial)
			{
				val2.sharedMaterial = p.SkinMaterial;
			}
			p.SkinSuspended = false;
			val2.GetPropertyBlock(block);
			block.SetTexture("_MainTex", (Texture)(object)val);
			val2.SetPropertyBlock(block);
			p.SkinApplied = Object.op_Implicit((Object)(object)p.SkinMaterial);
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
			if (Object.op_Implicit((Object)(object)p.SkinRenderer) && Object.op_Implicit((Object)(object)p.SkinMaterial) && (Object)(object)p.SkinRenderer.sharedMaterial == (Object)(object)p.SkinMaterial)
			{
				p.SkinRenderer.sharedMaterial = p.SkinBaseMaterial;
			}
			if (Object.op_Implicit((Object)(object)p.SkinRenderer) && p.SkinOriginalProperties != null)
			{
				p.SkinRenderer.SetPropertyBlock(p.SkinOriginalProperties);
			}
			if (Object.op_Implicit((Object)(object)p.SkinMaterial))
			{
				Object.Destroy((Object)(object)p.SkinMaterial);
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
			string current2 = Id(Find(p.SkinId));
			int num = list.FindIndex((object x) => Id(x) == current2);
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
