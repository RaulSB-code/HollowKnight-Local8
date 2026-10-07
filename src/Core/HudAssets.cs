using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class HudAssets
{
	internal sealed class Images
	{
		internal Texture2D Mask;

		internal Texture2D Empty;

		internal Texture2D Blue;

		internal Texture2D Soul;

		internal Texture2D Frame;

		internal Texture Atlas;

		internal Texture Orb;

		internal tk2dSpriteAnimationClip Hurt;

		internal readonly Dictionary<int, Texture2D> Frames = new Dictionary<int, Texture2D>();

		internal readonly List<Texture2D> Owned = new List<Texture2D>();

		internal Texture2D Own(Texture2D t)
		{
			if ((bool)t)
			{
				Owned.Add(t);
			}
			return t;
		}

		internal void Dispose()
		{
			foreach (Texture2D item in Owned)
			{
				if ((bool)item)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
			Owned.Clear();
			Frames.Clear();
			MaskDisplay.Clear();
		}

		internal Texture2D Damage(float elapsed)
		{
			if (Hurt == null || Hurt.frames == null || Hurt.frames.Length == 0)
			{
				return null;
			}
			int num = Mathf.FloorToInt(elapsed * Hurt.fps);
			if (num < 0 || num >= Hurt.frames.Length)
			{
				return null;
			}
			if (Frames.TryGetValue(num, out var value))
			{
				return value;
			}
			tk2dSpriteAnimationFrame tk2dSpriteAnimationFrame = Hurt.frames[num];
			if (!tk2dSpriteAnimationFrame.spriteCollection)
			{
				return null;
			}
			value = Own(FromTk2d(tk2dSpriteAnimationFrame.spriteCollection.inst.spriteDefinitions[tk2dSpriteAnimationFrame.spriteId], Atlas));
			Frames[num] = value;
			return value;
		}
	}

	internal static Texture2D Mask;

	internal static Texture2D Soul;

	private static tk2dBaseSprite maskSource;

	private static SpriteRenderer soulSource;

	private static Texture maskAtlas;

	private static Texture soulAtlas;

	private static float nextProbe;

	private static readonly Images[] playerImages = new Images[8];

	internal static bool Ready
	{
		get
		{
			if ((bool)Mask)
			{
				return Soul;
			}
			return false;
		}
	}

	internal static Images For(PlayerSlot p)
	{
		return playerImages[p.Index];
	}

	private static tk2dSpriteDefinition ClipMask(string keyword)
	{
		tk2dSpriteAnimator tk2dSpriteAnimator = (maskSource ? maskSource.GetComponent<tk2dSpriteAnimator>() : null);
		if (!tk2dSpriteAnimator || !tk2dSpriteAnimator.Library)
		{
			return null;
		}
		tk2dSpriteAnimationClip tk2dSpriteAnimationClip = tk2dSpriteAnimator.Library.clips.FirstOrDefault((tk2dSpriteAnimationClip x) => x.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
		if (tk2dSpriteAnimationClip != null && tk2dSpriteAnimationClip.frames != null && tk2dSpriteAnimationClip.frames.Length != 0)
		{
			return tk2dSpriteAnimationClip.frames[0].spriteCollection.inst.spriteDefinitions[tk2dSpriteAnimationClip.frames[0].spriteId];
		}
		return null;
	}

	private static void PreparePlayers(GameObject canvas)
	{
		if (!Ready || !maskSource)
		{
			return;
		}
		tk2dSpriteDefinition tk2dSpriteDefinition = FullMask(maskSource);
		if (tk2dSpriteDefinition == null)
		{
			return;
		}
		tk2dBaseSprite tk2dBaseSprite = canvas.GetComponentsInChildren<tk2dBaseSprite>(includeInactive: true).FirstOrDefault((tk2dBaseSprite x) => x.name.IndexOf("orb", StringComparison.OrdinalIgnoreCase) >= 0 && x.GetCurrentSpriteDef() != null && (bool)x.GetCurrentSpriteDef().material && x.GetCurrentSpriteDef().material.mainTexture == maskAtlas);
		tk2dSpriteAnimator component = maskSource.GetComponent<tk2dSpriteAnimator>();
		foreach (PlayerSlot player in Plugin.Self.Session.Players)
		{
			Texture texture = SkinBridge.ImageTexture(player, "Hud.png") ?? maskAtlas;
			Texture texture2 = SkinBridge.ImageTexture(player, "OrbFull.png") ?? soulAtlas;
			Images images = playerImages[player.Index];
			if (images != null && images.Atlas == texture && images.Orb == texture2)
			{
				continue;
			}
			images?.Dispose();
			Images images2 = new Images
			{
				Atlas = texture,
				Orb = texture2
			};
			playerImages[player.Index] = images2;
			images2.Mask = images2.Own(FromTk2d(tk2dSpriteDefinition, texture));
			tk2dSpriteDefinition tk2dSpriteDefinition2 = ClipMask("empty");
			tk2dSpriteDefinition tk2dSpriteDefinition3 = ClipMask("blue");
			if (tk2dSpriteDefinition2 != null)
			{
				images2.Empty = images2.Own(FromTk2d(tk2dSpriteDefinition2, texture));
			}
			if (tk2dSpriteDefinition3 != null)
			{
				images2.Blue = images2.Own(FromTk2d(tk2dSpriteDefinition3, texture));
			}
			if ((bool)tk2dBaseSprite)
			{
				images2.Frame = images2.Own(FromTk2d(tk2dBaseSprite.GetCurrentSpriteDef(), texture));
			}
			images2.Soul = ((texture2 == soulAtlas) ? Soul : images2.Own(Extract(texture2, Vector2.zero, Vector2.right, Vector2.up, (float)texture2.width / (float)texture2.height)));
			if ((bool)component && (bool)component.Library)
			{
				images2.Hurt = component.Library.clips.FirstOrDefault((tk2dSpriteAnimationClip c) => c.name.IndexOf("lose", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("break", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0);
			}
			Diagnostics.Write("HUD P" + (player.Index + 1) + " skin=" + SkinBridge.Display(player) + " frame=" + (tk2dBaseSprite ? tk2dBaseSprite.name : "none") + " hit=" + ((images2.Hurt == null) ? "flash" : images2.Hurt.name));
		}
	}

	internal static void Prepare()
	{
		if (Event.current.type != EventType.Repaint || Time.unscaledTime < nextProbe)
		{
			return;
		}
		nextProbe = Time.unscaledTime + 2f;
		try
		{
			GameCameras instance = GameCameras.instance;
			if (!instance || !instance.hudCanvas)
			{
				return;
			}
			if (!maskSource)
			{
				maskSource = instance.hudCanvas.GetComponentsInChildren<tk2dBaseSprite>(includeInactive: true).FirstOrDefault((tk2dBaseSprite s) => s.name == "Health 1");
			}
			if (!soulSource)
			{
				soulSource = instance.hudCanvas.GetComponentsInChildren<SpriteRenderer>(includeInactive: true).FirstOrDefault((SpriteRenderer s) => s.name == "Orb Full");
			}
			if ((bool)maskSource)
			{
				tk2dSpriteDefinition tk2dSpriteDefinition = FullMask(maskSource);
				Texture texture = ((tk2dSpriteDefinition == null || !tk2dSpriteDefinition.material) ? null : tk2dSpriteDefinition.material.mainTexture);
				if ((bool)texture && (texture != maskAtlas || !Mask))
				{
					Texture2D texture2D = FromTk2d(tk2dSpriteDefinition, texture);
					if ((bool)texture2D)
					{
						if ((bool)Mask)
						{
							UnityEngine.Object.Destroy(Mask);
						}
						Mask = texture2D;
						maskAtlas = texture;
						Diagnostics.Write("HUD mask=" + tk2dSpriteDefinition.name + " atlas=" + texture.name);
					}
				}
			}
			if ((bool)soulSource && (bool)soulSource.sprite)
			{
				Sprite sprite = soulSource.sprite;
				Texture2D texture2 = sprite.texture;
				if ((bool)texture2 && (texture2 != soulAtlas || !Soul))
				{
					Texture2D texture2D2 = FromSprite(sprite);
					if ((bool)texture2D2)
					{
						if ((bool)Soul)
						{
							UnityEngine.Object.Destroy(Soul);
						}
						Soul = texture2D2;
						soulAtlas = texture2;
						Diagnostics.Write("HUD soul=" + sprite.name);
					}
				}
			}
			PreparePlayers(instance.hudCanvas);
		}
		catch (Exception ex)
		{
			Diagnostics.Throttled("HUD ASSETS", ex);
		}
	}

	private static tk2dSpriteDefinition FullMask(tk2dBaseSprite sprite)
	{
		tk2dSpriteAnimator component = sprite.GetComponent<tk2dSpriteAnimator>();
		if ((bool)component && (bool)component.Library && component.Library.clips != null)
		{
			tk2dSpriteAnimationClip tk2dSpriteAnimationClip = component.Library.clips.FirstOrDefault((tk2dSpriteAnimationClip c) => string.Equals(c.name, "Idle", StringComparison.OrdinalIgnoreCase)) ?? component.Library.clips.FirstOrDefault((tk2dSpriteAnimationClip c) => c.name.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0);
			if (tk2dSpriteAnimationClip != null && tk2dSpriteAnimationClip.frames != null && tk2dSpriteAnimationClip.frames.Length != 0)
			{
				tk2dSpriteAnimationFrame tk2dSpriteAnimationFrame = tk2dSpriteAnimationClip.frames[0];
				if ((bool)tk2dSpriteAnimationFrame.spriteCollection)
				{
					return tk2dSpriteAnimationFrame.spriteCollection.inst.spriteDefinitions[tk2dSpriteAnimationFrame.spriteId];
				}
			}
		}
		return sprite.GetCurrentSpriteDef();
	}

	private static Texture2D FromTk2d(tk2dSpriteDefinition def, Texture atlas)
	{
		if (def.positions == null || def.uvs == null || def.positions.Length < 4 || def.positions.Length != def.uvs.Length)
		{
			return null;
		}
		Vector2 vector = new Vector2(def.positions.Min((Vector3 p) => p.x), def.positions.Min((Vector3 p) => p.y));
		Vector2 vector2 = new Vector2(def.positions.Max((Vector3 p) => p.x), def.positions.Max((Vector3 p) => p.y));
		if (vector2.x <= vector.x || vector2.y <= vector.y)
		{
			return null;
		}
		int num = Closest(def.positions, new Vector2(vector.x, vector.y));
		int num2 = Closest(def.positions, new Vector2(vector2.x, vector.y));
		int num3 = Closest(def.positions, new Vector2(vector.x, vector2.y));
		return Extract(atlas, def.uvs[num], def.uvs[num2], def.uvs[num3], (vector2.x - vector.x) / (vector2.y - vector.y));
	}

	private static int Closest(Vector3[] points, Vector2 target)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < points.Length; i++)
		{
			float sqrMagnitude = ((Vector2)points[i] - target).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = i;
			}
		}
		return result;
	}

	internal static Texture2D FromSprite(Sprite sprite)
	{
		Vector2[] vertices = sprite.vertices;
		Vector2[] uv = sprite.uv;
		ushort[] triangles = sprite.triangles;
		if (vertices == null || uv == null || triangles == null || vertices.Length != uv.Length || triangles.Length < 3)
		{
			return null;
		}
		Vector2 vector = vertices[0];
		Vector2 vector2 = vertices[0];
		Vector2 vector3 = uv[0];
		Vector2 vector4 = uv[0];
		Vector2[] array = vertices;
		foreach (Vector2 rhs in array)
		{
			vector = Vector2.Min(vector, rhs);
			vector2 = Vector2.Max(vector2, rhs);
		}
		array = uv;
		foreach (Vector2 rhs2 in array)
		{
			vector3 = Vector2.Min(vector3, rhs2);
			vector4 = Vector2.Max(vector4, rhs2);
		}
		Vector2 scale = vector4 - vector3;
		Vector2 vector5 = vector2 - vector;
		if (scale.x <= 0f || scale.y <= 0f || vector5.x <= 0f || vector5.y <= 0f)
		{
			return null;
		}
		Texture2D texture = sprite.texture;
		int num = Mathf.Clamp(Mathf.CeilToInt(scale.x * (float)texture.width), 2, 256);
		int num2 = Mathf.Clamp(Mathf.CeilToInt(scale.y * (float)texture.height), 2, 256);
		int num3 = 96;
		int num4 = Mathf.Clamp(Mathf.RoundToInt((float)num3 * vector5.x / vector5.y), 12, 256);
		RenderTexture active = RenderTexture.active;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
		Texture2D texture2D = null;
		try
		{
			Graphics.Blit(texture, temporary, scale, vector3);
			RenderTexture.active = temporary;
			texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
			texture2D.Apply();
			Color[] array2 = new Color[num4 * num3];
			for (int j = 0; j < num3; j++)
			{
				for (int k = 0; k < num4; k++)
				{
					Vector2 vector6 = new Vector2(vector.x + ((float)k + 0.5f) / (float)num4 * vector5.x, vector.y + ((float)j + 0.5f) / (float)num3 * vector5.y);
					for (int l = 0; l + 2 < triangles.Length; l += 3)
					{
						int num5 = triangles[l];
						int num6 = triangles[l + 1];
						int num7 = triangles[l + 2];
						Vector2 vector7 = vertices[num6] - vertices[num5];
						Vector2 vector8 = vertices[num7] - vertices[num5];
						Vector2 vector9 = vector6 - vertices[num5];
						float num8 = vector7.x * vector8.y - vector7.y * vector8.x;
						if (!(Mathf.Abs(num8) < 1E-06f))
						{
							float num9 = (vector9.x * vector8.y - vector9.y * vector8.x) / num8;
							float num10 = (vector7.x * vector9.y - vector7.y * vector9.x) / num8;
							if (!(num10 < -0.0001f) && !(num9 < -0.0001f) && !(num10 + num9 > 1.0001f))
							{
								Vector2 vector10 = uv[num5] * (1f - num10 - num9) + uv[num6] * num9 + uv[num7] * num10;
								array2[j * num4 + k] = texture2D.GetPixelBilinear(Mathf.Clamp01((vector10.x - vector3.x) / scale.x), Mathf.Clamp01((vector10.y - vector3.y) / scale.y));
								break;
							}
						}
					}
				}
			}
			Texture2D texture2D2 = new Texture2D(num4, num3, TextureFormat.RGBA32, mipChain: false);
			texture2D2.filterMode = FilterMode.Bilinear;
			texture2D2.wrapMode = TextureWrapMode.Clamp;
			texture2D2.hideFlags = HideFlags.HideAndDontSave;
			texture2D2.SetPixels(array2);
			texture2D2.Apply();
			return texture2D2;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			if ((bool)texture2D)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
		}
	}

	private static Texture2D Extract(Texture atlas, Vector2 bl, Vector2 br, Vector2 tl, float aspect)
	{
		Vector2 rhs = br + tl - bl;
		Vector2 vector = Vector2.Min(Vector2.Min(bl, br), Vector2.Min(tl, rhs));
		Vector2 scale = Vector2.Max(Vector2.Max(bl, br), Vector2.Max(tl, rhs)) - vector;
		if (scale.x <= 0f || scale.y <= 0f)
		{
			return null;
		}
		int num = Mathf.Clamp(Mathf.CeilToInt(scale.x * (float)atlas.width), 2, 256);
		int num2 = Mathf.Clamp(Mathf.CeilToInt(scale.y * (float)atlas.height), 2, 256);
		RenderTexture active = RenderTexture.active;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
		Texture2D texture2D = null;
		try
		{
			Graphics.Blit(atlas, temporary, scale, vector);
			RenderTexture.active = temporary;
			texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
			texture2D.Apply();
			int num3 = 96;
			int num4 = Mathf.Clamp(Mathf.RoundToInt((float)num3 * aspect), 12, 256);
			Color[] array = new Color[num4 * num3];
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < num4; j++)
				{
					Vector2 vector2 = bl + (br - bl) * (((float)j + 0.5f) / (float)num4) + (tl - bl) * (((float)i + 0.5f) / (float)num3);
					array[i * num4 + j] = texture2D.GetPixelBilinear((vector2.x - vector.x) / scale.x, (vector2.y - vector.y) / scale.y);
				}
			}
			Texture2D texture2D2 = new Texture2D(num4, num3, TextureFormat.RGBA32, mipChain: false);
			texture2D2.filterMode = FilterMode.Bilinear;
			texture2D2.wrapMode = TextureWrapMode.Clamp;
			texture2D2.hideFlags = HideFlags.HideAndDontSave;
			texture2D2.SetPixels(array);
			texture2D2.Apply();
			return texture2D2;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			if ((bool)texture2D)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
		}
	}

	internal static void Release()
	{
		Images[] array = playerImages;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Dispose();
		}
		Array.Clear(playerImages, 0, 8);
		if ((bool)Mask)
		{
			UnityEngine.Object.Destroy(Mask);
		}
		if ((bool)Soul)
		{
			UnityEngine.Object.Destroy(Soul);
		}
		Mask = null;
		Soul = null;
		maskSource = null;
		soulSource = null;
		maskAtlas = null;
		soulAtlas = null;
		nextProbe = 0f;
	}
}
