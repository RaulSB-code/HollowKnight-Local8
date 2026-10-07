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
			if (!Enum.TryParse<KeyCode>(Local8Mod.Settings.MenuKey, out var result) || !Enum.IsDefined(typeof(KeyCode), result) || result == KeyCode.None || result == KeyCode.Escape)
			{
				return KeyCode.F8;
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
				KeyCode.F8,
				KeyCode.F1,
				KeyCode.F2,
				KeyCode.F3,
				KeyCode.F4,
				KeyCode.F5,
				KeyCode.F6,
				KeyCode.F7,
				KeyCode.F10,
				KeyCode.F11,
				KeyCode.F12,
				KeyCode.Insert,
				KeyCode.Home,
				KeyCode.Tab,
				KeyCode.JoystickButton7
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

	internal static string Display(KeyCode key)
	{
		if (key < KeyCode.JoystickButton0 || key > KeyCode.JoystickButton19)
		{
			return key.ToString();
		}
		return "Mando: boton " + (int)(key - 330 + 1);
	}

	internal static void Set(KeyCode key)
	{
		if (key == ActionKeys.Pvp)
		{
			Local8Mod.Settings.PvpKey = ((key == KeyCode.F6) ? KeyCode.F7 : KeyCode.F6).ToString();
		}
		if (key == ActionKeys.Rescue)
		{
			Local8Mod.Settings.RescueKey = ((key == KeyCode.Alpha1) ? KeyCode.Alpha2 : KeyCode.Alpha1).ToString();
		}
		Local8Mod.Settings.MenuKey = key.ToString();
		Waiting = false;
		if (Plugin.Self != null)
		{
			Plugin.Self.ExportSettings();
		}
	}

	internal static bool Poll()
	{
		if (ActionKeys.Capturing != 0)
		{
			return false;
		}
		if (!Waiting)
		{
			return KeypadInput.Pressed(Key);
		}
		if (KeypadInput.Pressed(KeyCode.Escape))
		{
			Waiting = false;
			return false;
		}
		KeyCode[] array = all;
		foreach (KeyCode keyCode in array)
		{
			switch (keyCode)
			{
			default:
				if (!KeypadInput.Pressed(keyCode))
				{
					continue;
				}
				break;
			case KeyCode.None:
			case KeyCode.Mouse0:
			case KeyCode.Mouse1:
			case KeyCode.Mouse2:
			case KeyCode.Mouse3:
			case KeyCode.Mouse4:
			case KeyCode.Mouse5:
			case KeyCode.Mouse6:
				continue;
			}
			Set(keyCode);
			break;
		}
		return false;
	}
}
