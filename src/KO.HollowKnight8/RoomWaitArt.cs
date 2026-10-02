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
				foreach (Font val in array)
				{
					if (Object.op_Implicit((Object)(object)val) && (((Object)val).name.IndexOf("Perpetua", StringComparison.OrdinalIgnoreCase) >= 0 || ((Object)val).name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						gameFont = val;
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
					frame = new Texture2D(2, 2, (TextureFormat)5, false);
					if (!ImageConversion.LoadImage(frame, array))
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
		if (Object.op_Implicit((Object)(object)frame))
		{
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, 0.85f);
			GUI.DrawTextureWithTexCoords(area, (Texture)(object)frame, new Rect(0.19010417f, 0.26574075f, 0.62135416f, 55f / 108f), true);
			GUI.color = color;
		}
	}

	internal static void Release()
	{
		if (Object.op_Implicit((Object)(object)frame))
		{
			Object.Destroy((Object)(object)frame);
		}
		frame = null;
		tried = false;
		gameFont = null;
		searchedFont = false;
	}
}
