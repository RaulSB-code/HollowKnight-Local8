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
			if (Object.op_Implicit((Object)(object)t))
			{
				Owned.Add(t);
			}
			return t;
		}

		internal void Dispose()
		{
			foreach (Texture2D item in Owned)
			{
				if (Object.op_Implicit((Object)(object)item))
				{
					Object.Destroy((Object)(object)item);
				}
			}
			Owned.Clear();
			Frames.Clear();
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
			tk2dSpriteAnimationFrame val = Hurt.frames[num];
			if (!Object.op_Implicit((Object)(object)val.spriteCollection))
			{
				return null;
			}
			value = Own(FromTk2d(val.spriteCollection.inst.spriteDefinitions[val.spriteId], Atlas));
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
			if (Object.op_Implicit((Object)(object)Mask))
			{
				return Object.op_Implicit((Object)(object)Soul);
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
		tk2dSpriteAnimator val = (Object.op_Implicit((Object)(object)maskSource) ? ((Component)maskSource).GetComponent<tk2dSpriteAnimator>() : null);
		if (!Object.op_Implicit((Object)(object)val) || !Object.op_Implicit((Object)(object)val.Library))
		{
			return null;
		}
		tk2dSpriteAnimationClip val2 = ((IEnumerable<tk2dSpriteAnimationClip>)val.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip x) => x.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
		if (val2 != null && val2.frames != null && val2.frames.Length != 0)
		{
			return val2.frames[0].spriteCollection.inst.spriteDefinitions[val2.frames[0].spriteId];
		}
		return null;
	}

	private static void PreparePlayers(GameObject canvas)
	{
		if (!Ready || !Object.op_Implicit((Object)(object)maskSource))
		{
			return;
		}
		tk2dSpriteDefinition val = FullMask(maskSource);
		if (val == null)
		{
			return;
		}
		tk2dBaseSprite val2 = ((IEnumerable<tk2dBaseSprite>)canvas.GetComponentsInChildren<tk2dBaseSprite>(true)).FirstOrDefault((Func<tk2dBaseSprite, bool>)((tk2dBaseSprite x) => ((Object)x).name.IndexOf("orb", StringComparison.OrdinalIgnoreCase) >= 0 && x.GetCurrentSpriteDef() != null && Object.op_Implicit((Object)(object)x.GetCurrentSpriteDef().material) && (Object)(object)x.GetCurrentSpriteDef().material.mainTexture == (Object)(object)maskAtlas));
		tk2dSpriteAnimator component = ((Component)maskSource).GetComponent<tk2dSpriteAnimator>();
		foreach (PlayerSlot player in Plugin.Self.Session.Players)
		{
			Texture val3 = (Texture)(((object)SkinBridge.ImageTexture(player, "Hud.png")) ?? ((object)maskAtlas));
			Texture val4 = (Texture)(((object)SkinBridge.ImageTexture(player, "OrbFull.png")) ?? ((object)soulAtlas));
			Images images = playerImages[player.Index];
			if (images != null && (Object)(object)images.Atlas == (Object)(object)val3 && (Object)(object)images.Orb == (Object)(object)val4)
			{
				continue;
			}
			images?.Dispose();
			Images images2 = new Images
			{
				Atlas = val3,
				Orb = val4
			};
			playerImages[player.Index] = images2;
			images2.Mask = images2.Own(FromTk2d(val, val3));
			tk2dSpriteDefinition val5 = ClipMask("empty");
			tk2dSpriteDefinition val6 = ClipMask("blue");
			if (val5 != null)
			{
				images2.Empty = images2.Own(FromTk2d(val5, val3));
			}
			if (val6 != null)
			{
				images2.Blue = images2.Own(FromTk2d(val6, val3));
			}
			if (Object.op_Implicit((Object)(object)val2))
			{
				images2.Frame = images2.Own(FromTk2d(val2.GetCurrentSpriteDef(), val3));
			}
			images2.Soul = (((Object)(object)val4 == (Object)(object)soulAtlas) ? Soul : images2.Own(Extract(val4, Vector2.zero, Vector2.right, Vector2.up, (float)val4.width / (float)val4.height)));
			if (Object.op_Implicit((Object)(object)component) && Object.op_Implicit((Object)(object)component.Library))
			{
				images2.Hurt = ((IEnumerable<tk2dSpriteAnimationClip>)component.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip c) => c.name.IndexOf("lose", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("break", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0));
			}
			Diagnostics.Write("HUD P" + (player.Index + 1) + " skin=" + SkinBridge.Display(player) + " frame=" + (Object.op_Implicit((Object)(object)val2) ? ((Object)val2).name : "none") + " hit=" + ((images2.Hurt == null) ? "flash" : images2.Hurt.name));
		}
	}

	internal static void Prepare()
	{
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)Event.current.type != 7 || Time.unscaledTime < nextProbe)
		{
			return;
		}
		nextProbe = Time.unscaledTime + 2f;
		try
		{
			GameCameras instance = GameCameras.instance;
			if (!Object.op_Implicit((Object)(object)instance) || !Object.op_Implicit((Object)(object)instance.hudCanvas))
			{
				return;
			}
			if (!Object.op_Implicit((Object)(object)maskSource))
			{
				maskSource = ((IEnumerable<tk2dBaseSprite>)instance.hudCanvas.GetComponentsInChildren<tk2dBaseSprite>(true)).FirstOrDefault((Func<tk2dBaseSprite, bool>)((tk2dBaseSprite s) => ((Object)s).name == "Health 1"));
			}
			if (!Object.op_Implicit((Object)(object)soulSource))
			{
				soulSource = ((IEnumerable<SpriteRenderer>)instance.hudCanvas.GetComponentsInChildren<SpriteRenderer>(true)).FirstOrDefault((Func<SpriteRenderer, bool>)((SpriteRenderer s) => ((Object)s).name == "Orb Full"));
			}
			if (Object.op_Implicit((Object)(object)maskSource))
			{
				tk2dSpriteDefinition val = FullMask(maskSource);
				Texture val2 = ((val == null || !Object.op_Implicit((Object)(object)val.material)) ? null : val.material.mainTexture);
				if (Object.op_Implicit((Object)(object)val2) && ((Object)(object)val2 != (Object)(object)maskAtlas || !Object.op_Implicit((Object)(object)Mask)))
				{
					Texture2D val3 = FromTk2d(val, val2);
					if (Object.op_Implicit((Object)(object)val3))
					{
						if (Object.op_Implicit((Object)(object)Mask))
						{
							Object.Destroy((Object)(object)Mask);
						}
						Mask = val3;
						maskAtlas = val2;
						Diagnostics.Write("HUD mask=" + val.name + " atlas=" + ((Object)val2).name);
					}
				}
			}
			if (Object.op_Implicit((Object)(object)soulSource) && Object.op_Implicit((Object)(object)soulSource.sprite))
			{
				Sprite sprite = soulSource.sprite;
				Texture2D texture = sprite.texture;
				if (Object.op_Implicit((Object)(object)texture) && ((Object)(object)texture != (Object)(object)soulAtlas || !Object.op_Implicit((Object)(object)Soul)))
				{
					Texture2D val4 = FromSprite(sprite);
					if (Object.op_Implicit((Object)(object)val4))
					{
						if (Object.op_Implicit((Object)(object)Soul))
						{
							Object.Destroy((Object)(object)Soul);
						}
						Soul = val4;
						soulAtlas = (Texture)(object)texture;
						Diagnostics.Write("HUD soul=" + ((Object)sprite).name);
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
		tk2dSpriteAnimator component = ((Component)sprite).GetComponent<tk2dSpriteAnimator>();
		if (Object.op_Implicit((Object)(object)component) && Object.op_Implicit((Object)(object)component.Library) && component.Library.clips != null)
		{
			tk2dSpriteAnimationClip val = ((IEnumerable<tk2dSpriteAnimationClip>)component.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip c) => string.Equals(c.name, "Idle", StringComparison.OrdinalIgnoreCase))) ?? ((IEnumerable<tk2dSpriteAnimationClip>)component.Library.clips).FirstOrDefault((Func<tk2dSpriteAnimationClip, bool>)((tk2dSpriteAnimationClip c) => c.name.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0));
			if (val != null && val.frames != null && val.frames.Length != 0)
			{
				tk2dSpriteAnimationFrame val2 = val.frames[0];
				if (Object.op_Implicit((Object)(object)val2.spriteCollection))
				{
					return val2.spriteCollection.inst.spriteDefinitions[val2.spriteId];
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
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(def.positions.Min((Vector3 p) => p.x), def.positions.Min((Vector3 p) => p.y));
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector(def.positions.Max((Vector3 p) => p.x), def.positions.Max((Vector3 p) => p.y));
		if (val2.x <= val.x || val2.y <= val.y)
		{
			return null;
		}
		int num = Closest(def.positions, new Vector2(val.x, val.y));
		int num2 = Closest(def.positions, new Vector2(val2.x, val.y));
		int num3 = Closest(def.positions, new Vector2(val.x, val2.y));
		return Extract(atlas, def.uvs[num], def.uvs[num2], def.uvs[num3], (val2.x - val.x) / (val2.y - val.y));
	}

	private static int Closest(Vector3[] points, Vector2 target)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < points.Length; i++)
		{
			Vector2 val = Vector2.op_Implicit(points[i]) - target;
			float sqrMagnitude = ((Vector2)(ref val)).sqrMagnitude;
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
		Vector2 val = vertices[0];
		Vector2 val2 = vertices[0];
		Vector2 val3 = uv[0];
		Vector2 val4 = uv[0];
		Vector2[] array = vertices;
		foreach (Vector2 val5 in array)
		{
			val = Vector2.Min(val, val5);
			val2 = Vector2.Max(val2, val5);
		}
		array = uv;
		foreach (Vector2 val6 in array)
		{
			val3 = Vector2.Min(val3, val6);
			val4 = Vector2.Max(val4, val6);
		}
		Vector2 val7 = val4 - val3;
		Vector2 val8 = val2 - val;
		if (val7.x <= 0f || val7.y <= 0f || val8.x <= 0f || val8.y <= 0f)
		{
			return null;
		}
		Texture2D texture = sprite.texture;
		int num = Mathf.Clamp(Mathf.CeilToInt(val7.x * (float)((Texture)texture).width), 2, 256);
		int num2 = Mathf.Clamp(Mathf.CeilToInt(val7.y * (float)((Texture)texture).height), 2, 256);
		int num3 = 96;
		int num4 = Mathf.Clamp(Mathf.RoundToInt((float)num3 * val8.x / val8.y), 12, 256);
		RenderTexture active = RenderTexture.active;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, (RenderTextureFormat)0);
		Texture2D val9 = null;
		try
		{
			Graphics.Blit((Texture)(object)texture, temporary, val7, val3);
			RenderTexture.active = temporary;
			val9 = new Texture2D(num, num2, (TextureFormat)4, false);
			val9.ReadPixels(new Rect(0f, 0f, (float)num, (float)num2), 0, 0);
			val9.Apply();
			Color[] array2 = (Color[])(object)new Color[num4 * num3];
			Vector2 val10 = default(Vector2);
			for (int j = 0; j < num3; j++)
			{
				for (int k = 0; k < num4; k++)
				{
					((Vector2)(ref val10))._002Ector(val.x + ((float)k + 0.5f) / (float)num4 * val8.x, val.y + ((float)j + 0.5f) / (float)num3 * val8.y);
					for (int l = 0; l + 2 < triangles.Length; l += 3)
					{
						int num5 = triangles[l];
						int num6 = triangles[l + 1];
						int num7 = triangles[l + 2];
						Vector2 val11 = vertices[num6] - vertices[num5];
						Vector2 val12 = vertices[num7] - vertices[num5];
						Vector2 val13 = val10 - vertices[num5];
						float num8 = val11.x * val12.y - val11.y * val12.x;
						if (!(Mathf.Abs(num8) < 1E-06f))
						{
							float num9 = (val13.x * val12.y - val13.y * val12.x) / num8;
							float num10 = (val11.x * val13.y - val11.y * val13.x) / num8;
							if (!(num10 < -0.0001f) && !(num9 < -0.0001f) && !(num10 + num9 > 1.0001f))
							{
								Vector2 val14 = uv[num5] * (1f - num10 - num9) + uv[num6] * num9 + uv[num7] * num10;
								array2[j * num4 + k] = val9.GetPixelBilinear(Mathf.Clamp01((val14.x - val3.x) / val7.x), Mathf.Clamp01((val14.y - val3.y) / val7.y));
								break;
							}
						}
					}
				}
			}
			Texture2D val15 = new Texture2D(num4, num3, (TextureFormat)4, false)
			{
				filterMode = (FilterMode)1,
				wrapMode = (TextureWrapMode)1,
				hideFlags = (HideFlags)61
			};
			val15.SetPixels(array2);
			val15.Apply();
			return val15;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			if (Object.op_Implicit((Object)(object)val9))
			{
				Object.Destroy((Object)(object)val9);
			}
		}
	}

	private static Texture2D Extract(Texture atlas, Vector2 bl, Vector2 br, Vector2 tl, float aspect)
	{
		Vector2 val = br + tl - bl;
		Vector2 val2 = Vector2.Min(Vector2.Min(bl, br), Vector2.Min(tl, val));
		Vector2 val3 = Vector2.Max(Vector2.Max(bl, br), Vector2.Max(tl, val)) - val2;
		if (val3.x <= 0f || val3.y <= 0f)
		{
			return null;
		}
		int num = Mathf.Clamp(Mathf.CeilToInt(val3.x * (float)atlas.width), 2, 256);
		int num2 = Mathf.Clamp(Mathf.CeilToInt(val3.y * (float)atlas.height), 2, 256);
		RenderTexture active = RenderTexture.active;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, (RenderTextureFormat)0);
		Texture2D val4 = null;
		try
		{
			Graphics.Blit(atlas, temporary, val3, val2);
			RenderTexture.active = temporary;
			val4 = new Texture2D(num, num2, (TextureFormat)4, false);
			val4.ReadPixels(new Rect(0f, 0f, (float)num, (float)num2), 0, 0);
			val4.Apply();
			int num3 = 96;
			int num4 = Mathf.Clamp(Mathf.RoundToInt((float)num3 * aspect), 12, 256);
			Color[] array = (Color[])(object)new Color[num4 * num3];
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < num4; j++)
				{
					Vector2 val5 = bl + (br - bl) * (((float)j + 0.5f) / (float)num4) + (tl - bl) * (((float)i + 0.5f) / (float)num3);
					array[i * num4 + j] = val4.GetPixelBilinear((val5.x - val2.x) / val3.x, (val5.y - val2.y) / val3.y);
				}
			}
			Texture2D val6 = new Texture2D(num4, num3, (TextureFormat)4, false)
			{
				filterMode = (FilterMode)1,
				wrapMode = (TextureWrapMode)1,
				hideFlags = (HideFlags)61
			};
			val6.SetPixels(array);
			val6.Apply();
			return val6;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(temporary);
			if (Object.op_Implicit((Object)(object)val4))
			{
				Object.Destroy((Object)(object)val4);
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
		if (Object.op_Implicit((Object)(object)Mask))
		{
			Object.Destroy((Object)(object)Mask);
		}
		if (Object.op_Implicit((Object)(object)Soul))
		{
			Object.Destroy((Object)(object)Soul);
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
