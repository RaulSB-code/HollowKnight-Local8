using System;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ActionKeys
{
	private static readonly KeyCode[] keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	internal static int Capturing;

	internal static KeyCode Rescue => Parse(Local8Mod.Settings.RescueKey, KeyCode.Alpha1);

	internal static KeyCode Pvp => Parse(Local8Mod.Settings.PvpKey, KeyCode.F6);

	internal static string RescueLabel => KeyLabel(Rescue);

	internal static string PvpLabel => KeyLabel(Pvp);

	internal static string ButtonLabel => (new string[4] { "RB", "Start", "LB", "R3" })[Mathf.Clamp(Local8Mod.Settings.RescueButton, 0, 3)];

	internal static InputControlType RescueControl => (new InputControlType[4]
	{
		InputControlType.RightBumper,
		InputControlType.Start,
		InputControlType.LeftBumper,
		InputControlType.RightStickButton
	})[Mathf.Clamp(Local8Mod.Settings.RescueButton, 0, 3)];

	private static KeyCode Parse(string value, KeyCode fallback)
	{
		if (!Enum.TryParse<KeyCode>(value, out var result) || !Enum.IsDefined(typeof(KeyCode), result) || result == KeyCode.None || result == KeyCode.Escape)
		{
			return fallback;
		}
		return result;
	}

	internal static string KeyLabel(KeyCode key)
	{
		string text = key.ToString();
		if (!text.StartsWith("Alpha"))
		{
			return text;
		}
		return text.Substring(5);
	}

	internal static bool Held(PlayerSlot p)
	{
		if (p != null && p.Connected && Capturing == 0 && !MenuInput.Waiting && Application.isFocused)
		{
			if (!Controls.IsKeyboard(p.Device))
			{
				if (Controls.IsVJoy(p.Device) && p.Device.IsUnknown)
				{
					int num = (new int[4] { 8, 8, 5, 10 })[Mathf.Clamp(Local8Mod.Settings.RescueButton, 0, 3)];
					if (p.Device.NumUnknownButtons >= num)
					{
						return p.Device.GetControl((InputControlType)(499 + num)).IsPressed;
					}
					return false;
				}
				return p.Device.GetControl(RescueControl).IsPressed;
			}
			if (p.Index <= 0)
			{
				return KeypadInput.Held(Rescue);
			}
			if (p.Actions != null)
			{
				return p.Actions.paneRight.IsPressed;
			}
		}
		return false;
	}

	internal static void Tick(CoopSession s)
	{
		KeyboardMemory.BeforeCapture();
		if (Capturing != 0)
		{
			if (KeypadInput.Pressed(KeyCode.Escape))
			{
				Capturing = 0;
				KeyboardMemory.AfterCapture(s);
				return;
			}
			KeyCode[] array = keys;
			for (int i = 0; i < array.Length; i++)
			{
				KeyCode keyCode = array[i];
				if (keyCode <= KeyCode.None || keyCode >= KeyCode.Mouse0 || keyCode == MenuInput.Key || !KeypadInput.Pressed(keyCode))
				{
					continue;
				}
				if (Capturing == 1)
				{
					if (keyCode == Pvp)
					{
						continue;
					}
					Local8Mod.Settings.RescueKey = keyCode.ToString();
				}
				else
				{
					if (Capturing != 2)
					{
						if (Capturing < 10 || Capturing > 22)
						{
							continue;
						}
						if (Hud.selected >= 1 && Plugin.Self.Session.Players.Count > Hud.selected && Controls.IsKeyboard(Plugin.Self.Session.Players[Hud.selected].Device) && Plugin.Self.Session.Players[Hud.selected].Actions != null)
						{
							if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
							{
								i = (int)(keyCode - 61);
							}
							else if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
							{
								i = (int)(keyCode - 22);
							}
							else if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
							{
								i = (int)(keyCode - 169);
							}
							else if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
							{
								i = (int)(keyCode - 268);
							}
							else if (keyCode >= KeyCode.F13 && keyCode <= KeyCode.F15)
							{
								i = (int)(keyCode - 188);
							}
							else
							{
								switch (keyCode)
								{
								case KeyCode.Backspace:
									i = 65;
									break;
								case KeyCode.Tab:
									i = 66;
									break;
								case KeyCode.Return:
									i = 72;
									break;
								case KeyCode.Space:
									i = 76;
									break;
								case KeyCode.Comma:
									i = 73;
									break;
								case KeyCode.Minus:
									i = 63;
									break;
								case KeyCode.Period:
									i = 74;
									break;
								case KeyCode.Slash:
									i = 75;
									break;
								case KeyCode.Semicolon:
									i = 70;
									break;
								case KeyCode.Equals:
									i = 64;
									break;
								case KeyCode.LeftBracket:
									i = 67;
									break;
								case KeyCode.Backslash:
									i = 69;
									break;
								case KeyCode.RightBracket:
									i = 68;
									break;
								case KeyCode.BackQuote:
									i = 62;
									break;
								case KeyCode.Delete:
									i = 78;
									break;
								case KeyCode.KeypadPeriod:
									i = 103;
									break;
								case KeyCode.KeypadDivide:
									i = 98;
									break;
								case KeyCode.KeypadMultiply:
									i = 99;
									break;
								case KeyCode.KeypadMinus:
									i = 100;
									break;
								case KeyCode.KeypadPlus:
									i = 101;
									break;
								case KeyCode.KeypadEnter:
									i = 102;
									break;
								case KeyCode.KeypadEquals:
									i = 105;
									break;
								case KeyCode.UpArrow:
									i = 85;
									break;
								case KeyCode.DownArrow:
									i = 86;
									break;
								case KeyCode.RightArrow:
									i = 84;
									break;
								case KeyCode.LeftArrow:
									i = 83;
									break;
								case KeyCode.Insert:
									i = 77;
									break;
								case KeyCode.Home:
									i = 79;
									break;
								case KeyCode.End:
									i = 80;
									break;
								case KeyCode.PageUp:
									i = 81;
									break;
								case KeyCode.PageDown:
									i = 82;
									break;
								case KeyCode.Numlock:
									i = 97;
									break;
								case KeyCode.CapsLock:
									i = 110;
									break;
								case KeyCode.RightShift:
									i = 9;
									break;
								case KeyCode.LeftShift:
									i = 5;
									break;
								case KeyCode.RightControl:
									i = 12;
									break;
								case KeyCode.LeftControl:
									i = 8;
									break;
								case KeyCode.RightAlt:
									i = 10;
									break;
								case KeyCode.LeftAlt:
									i = 6;
									break;
								case KeyCode.RightCommand:
									i = 7;
									break;
								case KeyCode.RightWindows:
									i = 11;
									break;
								case KeyCode.AltGr:
									i = 109;
									break;
								default:
									KeyboardMemory.AfterCapture(s);
									return;
								}
							}
							PlayerAction playerAction;
							if (Capturing != 10)
							{
								if (Capturing != 11)
								{
									if (Capturing != 12)
									{
										if (Capturing != 13)
										{
											if (Capturing != 14)
											{
												if (Capturing != 15)
												{
													if (Capturing != 16)
													{
														if (Capturing != 17)
														{
															if (Capturing != 18)
															{
																if (Capturing != 19)
																{
																	if (Capturing != 20)
																	{
																		if (Capturing != 21)
																		{
																			if (Capturing != 22)
																			{
																				goto IL_07c8;
																			}
																			playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.paneRight;
																		}
																		else
																		{
																			playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.quickMap;
																		}
																	}
																	else
																	{
																		playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.superDash;
																	}
																}
																else
																{
																	playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.dreamNail;
																}
															}
															else
															{
																playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.focus;
															}
														}
														else
														{
															playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.cast;
														}
													}
													else
													{
														playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.dash;
													}
												}
												else
												{
													playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.attack;
												}
											}
											else
											{
												playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.jump;
											}
										}
										else
										{
											playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.right;
										}
									}
									else
									{
										playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.left;
									}
								}
								else
								{
									playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.down;
								}
							}
							else
							{
								playerAction = Plugin.Self.Session.Players[Hud.selected].Actions.up;
							}
							playerAction.ClearBindings();
							playerAction.AddBinding(new KeyBindingSource((Key)i));
							Capturing = 0;
							KeyboardMemory.AfterCapture(s);
							return;
						}
						goto IL_07c8;
					}
					if (keyCode == Rescue)
					{
						continue;
					}
					Local8Mod.Settings.PvpKey = keyCode.ToString();
				}
				Capturing = 0;
				Plugin.Self.ExportSettings();
				KeyboardMemory.AfterCapture(s);
				return;
				IL_07c8:
				Capturing = 0;
				KeyboardMemory.AfterCapture(s);
				return;
			}
			KeyboardMemory.AfterCapture(s);
		}
		else if (s == null || !s.Active || !s.Gameplay || Plugin.Self.Panel || MenuInput.Waiting || !Application.isFocused)
		{
			KeyboardMemory.AfterCapture(s);
		}
		else
		{
			if (KeypadInput.Pressed(Pvp))
			{
				PvpMatch.Toggle();
			}
			KeyboardMemory.AfterCapture(s);
		}
	}
}
