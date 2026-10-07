using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class RoomWaitArt
{
	private static Texture2D frame;

	private static bool tried;

	private static Font gameFont;

	private static bool searchedFont;

	internal static Font GameFont
	{
		get
		{
			if (!searchedFont)
			{
				searchedFont = true;
				Font[] array = Resources.FindObjectsOfTypeAll<Font>();
				foreach (Font font in array)
				{
					if ((bool)font && (font.name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0 || font.name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						gameFont = font;
						break;
					}
				}
			}
			return gameFont;
		}
	}

	internal static void Draw(Rect area)
	{
		if (!tried)
		{
			tried = true;
			try
			{
				using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Local8.RoomWaitFrame");
				if (stream != null)
				{
					byte[] array = new byte[stream.Length];
					int num;
					for (int i = 0; i < array.Length; i += num)
					{
						num = stream.Read(array, i, array.Length - i);
						if (num <= 0)
						{
							break;
						}
					}
					frame = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: false);
					if (!frame.LoadImage(array))
					{
						frame = null;
					}
				}
			}
			catch (Exception ex)
			{
				Diagnostics.Throttled("ROOM art", ex);
			}
		}
		if ((bool)frame)
		{
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, 0.85f);
			GUI.DrawTextureWithTexCoords(area, frame, new Rect(0.19010417f, 0.26574075f, 0.62135416f, 55f / 108f), alphaBlend: true);
			GUI.color = color;
		}
	}

	internal static void Release()
	{
		if ((bool)frame)
		{
			UnityEngine.Object.Destroy(frame);
		}
		frame = null;
		tried = false;
		gameFont = null;
		searchedFont = false;
	}
}
