using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class MenuInput
{
	private static readonly KeyCode[] all = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	internal static bool Waiting;

	internal static KeyCode Key
	{
		get
		{
			//IL_0030: Invalid comparison between Unknown and I4
			if (!Enum.TryParse<KeyCode>(Local8Mod.Settings.MenuKey, out KeyCode result) || !Enum.IsDefined(typeof(KeyCode), result) || (int)result == 0 || (int)result == 27)
			{
				return (KeyCode)289;
			}
			return result;
		}
	}

	internal static string Label => Display(Key);

	internal static KeyCode[] Choices
	{
		get
		{
			List<KeyCode> list = new List<KeyCode>
			{
				(KeyCode)289,
				(KeyCode)282,
				(KeyCode)283,
				(KeyCode)284,
				(KeyCode)285,
				(KeyCode)286,
				(KeyCode)287,
				(KeyCode)288,
				(KeyCode)291,
				(KeyCode)292,
				(KeyCode)293,
				(KeyCode)277,
				(KeyCode)278,
				(KeyCode)9,
				(KeyCode)337
			};
			if (!list.Contains(Key))
			{
				list.Add(Key);
			}
			return list.ToArray();
		}
	}

	internal static string[] Names
	{
		get
		{
			KeyCode[] choices = Choices;
			string[] array = new string[choices.Length];
			for (int i = 0; i < choices.Length; i++)
			{
				array[i] = Display(choices[i]);
			}
			return array;
		}
	}

	internal static int Choice => Array.IndexOf(Choices, Key);

	internal unsafe static string Display(KeyCode key)
	{
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_000e: Invalid comparison between Unknown and I4
		if ((int)key < 330 || (int)key > 349)
		{
			return ((object)(*(KeyCode*)(&key))/*cast due to .constrained prefix*/).ToString();
		}
		return "Mando: boton " + (key - 330 + 1);
	}

	internal unsafe static void Set(KeyCode key)
	{
		//IL_0013: Invalid comparison between Unknown and I4
		//IL_0044: Invalid comparison between Unknown and I4
		if (key == ActionKeys.Pvp)
		{
			Local8Mod.Settings.PvpKey = ((object)(KeyCode)(((int)key == 287) ? 288 : 287)/*cast due to .constrained prefix*/).ToString();
		}
		if (key == ActionKeys.Rescue)
		{
			Local8Mod.Settings.RescueKey = ((object)(KeyCode)(((int)key == 49) ? 50 : 49)/*cast due to .constrained prefix*/).ToString();
		}
		Local8Mod.Settings.MenuKey = ((object)(*(KeyCode*)(&key))/*cast due to .constrained prefix*/).ToString();
		Waiting = false;
		if ((Object)(object)Plugin.Self != (Object)null)
		{
			Plugin.Self.ExportSettings();
		}
	}

	internal static bool Poll()
	{
		//IL_0043: Invalid comparison between Unknown and I4
		//IL_004b: Invalid comparison between Unknown and I4
		if (ActionKeys.Capturing != 0)
		{
			return false;
		}
		if (!Waiting)
		{
			return Input.GetKeyDown(Key);
		}
		if (Input.GetKeyDown((KeyCode)27))
		{
			Waiting = false;
			return false;
		}
		KeyCode[] array = all;
		foreach (KeyCode val in array)
		{
			if ((int)val != 0 && ((int)val < 323 || (int)val > 329) && Input.GetKeyDown(val))
			{
				Set(val);
				break;
			}
		}
		return false;
	}
}
