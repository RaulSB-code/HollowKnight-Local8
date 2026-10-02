using System;
using System.Runtime.CompilerServices;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class ActionKeys
{
	private static readonly KeyCode[] keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	internal static int Capturing;

	internal static KeyCode Rescue => Parse(Local8Mod.Settings.RescueKey, (KeyCode)49);

	internal static KeyCode Pvp => Parse(Local8Mod.Settings.PvpKey, (KeyCode)287);

	internal static string RescueLabel => KeyLabel(Rescue);

	internal static string PvpLabel => KeyLabel(Pvp);

	internal static string ButtonLabel => (new string[4] { "RB", "Start", "LB", "R3" })[Mathf.Clamp(Local8Mod.Settings.RescueButton, 0, 3)];

	internal static InputControlType RescueControl
	{
		get
		{
			InputControlType[] array = new InputControlType[4];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			return array[Mathf.Clamp(Local8Mod.Settings.RescueButton, 0, 3)];
		}
	}

	private static KeyCode Parse(string value, KeyCode fallback)
	{
		//IL_0027: Invalid comparison between Unknown and I4
		if (!Enum.TryParse<KeyCode>(value, out KeyCode result) || !Enum.IsDefined(typeof(KeyCode), result) || (int)result == 0 || (int)result == 27)
		{
			return fallback;
		}
		return result;
	}

	internal unsafe static string KeyLabel(KeyCode key)
	{
		string text = ((object)(*(KeyCode*)(&key))/*cast due to .constrained prefix*/).ToString();
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
						return ((OneAxisInputControl)p.Device.GetControl((InputControlType)(499 + num))).IsPressed;
					}
					return false;
				}
				return ((OneAxisInputControl)p.Device.GetControl(RescueControl)).IsPressed;
			}
			if (p.Index <= 0)
			{
				return Input.GetKey(Rescue);
			}
			if (p.Actions != null)
			{
				return ((OneAxisInputControl)p.Actions.paneRight).IsPressed;
			}
		}
		return false;
	}

	internal unsafe static void Tick(CoopSession s)
	{
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0155: Invalid comparison between Unknown and I4
		//IL_016f: Invalid comparison between Unknown and I4
		//IL_015d: Invalid comparison between Unknown and I4
		//IL_018c: Invalid comparison between Unknown and I4
		//IL_0177: Invalid comparison between Unknown and I4
		//IL_01af: Invalid comparison between Unknown and I4
		//IL_0197: Invalid comparison between Unknown and I4
		//IL_01d2: Invalid comparison between Unknown and I4
		//IL_01ba: Invalid comparison between Unknown and I4
		//IL_01f1: Invalid comparison between Unknown and I4
		//IL_01dd: Invalid comparison between Unknown and I4
		//IL_0201: Invalid comparison between Unknown and I4
		//IL_0211: Invalid comparison between Unknown and I4
		//IL_0221: Invalid comparison between Unknown and I4
		//IL_0231: Invalid comparison between Unknown and I4
		//IL_0241: Invalid comparison between Unknown and I4
		//IL_0251: Invalid comparison between Unknown and I4
		//IL_0261: Invalid comparison between Unknown and I4
		//IL_0271: Invalid comparison between Unknown and I4
		//IL_0281: Invalid comparison between Unknown and I4
		//IL_0291: Invalid comparison between Unknown and I4
		//IL_02a1: Invalid comparison between Unknown and I4
		//IL_02b1: Invalid comparison between Unknown and I4
		//IL_02c1: Invalid comparison between Unknown and I4
		//IL_02d1: Invalid comparison between Unknown and I4
		//IL_02e4: Invalid comparison between Unknown and I4
		//IL_02f7: Invalid comparison between Unknown and I4
		//IL_030a: Invalid comparison between Unknown and I4
		//IL_031d: Invalid comparison between Unknown and I4
		//IL_0330: Invalid comparison between Unknown and I4
		//IL_0343: Invalid comparison between Unknown and I4
		//IL_0356: Invalid comparison between Unknown and I4
		//IL_0369: Invalid comparison between Unknown and I4
		//IL_037c: Invalid comparison between Unknown and I4
		//IL_038f: Invalid comparison between Unknown and I4
		//IL_03a2: Invalid comparison between Unknown and I4
		//IL_03b5: Invalid comparison between Unknown and I4
		//IL_03c8: Invalid comparison between Unknown and I4
		//IL_03db: Invalid comparison between Unknown and I4
		//IL_03ee: Invalid comparison between Unknown and I4
		//IL_0401: Invalid comparison between Unknown and I4
		//IL_0414: Invalid comparison between Unknown and I4
		//IL_0427: Invalid comparison between Unknown and I4
		//IL_043a: Invalid comparison between Unknown and I4
		//IL_044d: Invalid comparison between Unknown and I4
		//IL_045f: Invalid comparison between Unknown and I4
		//IL_0472: Invalid comparison between Unknown and I4
		//IL_0484: Invalid comparison between Unknown and I4
		//IL_0497: Invalid comparison between Unknown and I4
		//IL_04a9: Invalid comparison between Unknown and I4
		//IL_04bb: Invalid comparison between Unknown and I4
		//IL_04ce: Invalid comparison between Unknown and I4
		if (Capturing != 0)
		{
			if (Input.GetKeyDown((KeyCode)27))
			{
				Capturing = 0;
				return;
			}
			KeyCode[] array = keys;
			for (int i = 0; i < array.Length; i++)
			{
				KeyCode val = array[i];
				if ((int)val <= 0 || (int)val >= 323 || val == MenuInput.Key || !Input.GetKeyDown(val))
				{
					continue;
				}
				if (Capturing == 1)
				{
					if (val == Pvp)
					{
						continue;
					}
					Local8Mod.Settings.RescueKey = ((object)(*(KeyCode*)(&val))/*cast due to .constrained prefix*/).ToString();
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
							if ((int)val >= 97 && (int)val <= 122)
							{
								i = val - 61;
							}
							else if ((int)val >= 48 && (int)val <= 57)
							{
								i = val - 22;
							}
							else if ((int)val >= 256 && (int)val <= 265)
							{
								i = val - 169;
							}
							else if ((int)val >= 282 && (int)val <= 293)
							{
								i = val - 268;
							}
							else if ((int)val >= 294 && (int)val <= 296)
							{
								i = val - 188;
							}
							else if ((int)val == 8)
							{
								i = 65;
							}
							else if ((int)val == 9)
							{
								i = 66;
							}
							else if ((int)val == 13)
							{
								i = 72;
							}
							else if ((int)val == 32)
							{
								i = 76;
							}
							else if ((int)val == 44)
							{
								i = 73;
							}
							else if ((int)val == 45)
							{
								i = 63;
							}
							else if ((int)val == 46)
							{
								i = 74;
							}
							else if ((int)val == 47)
							{
								i = 75;
							}
							else if ((int)val == 59)
							{
								i = 70;
							}
							else if ((int)val == 61)
							{
								i = 64;
							}
							else if ((int)val == 91)
							{
								i = 67;
							}
							else if ((int)val == 92)
							{
								i = 69;
							}
							else if ((int)val == 93)
							{
								i = 68;
							}
							else if ((int)val == 96)
							{
								i = 62;
							}
							else if ((int)val == 127)
							{
								i = 78;
							}
							else if ((int)val == 266)
							{
								i = 103;
							}
							else if ((int)val == 267)
							{
								i = 98;
							}
							else if ((int)val == 268)
							{
								i = 99;
							}
							else if ((int)val == 269)
							{
								i = 100;
							}
							else if ((int)val == 270)
							{
								i = 101;
							}
							else if ((int)val == 271)
							{
								i = 102;
							}
							else if ((int)val == 272)
							{
								i = 105;
							}
							else if ((int)val == 273)
							{
								i = 85;
							}
							else if ((int)val == 274)
							{
								i = 86;
							}
							else if ((int)val == 275)
							{
								i = 84;
							}
							else if ((int)val == 276)
							{
								i = 83;
							}
							else if ((int)val == 277)
							{
								i = 77;
							}
							else if ((int)val == 278)
							{
								i = 79;
							}
							else if ((int)val == 279)
							{
								i = 80;
							}
							else if ((int)val == 280)
							{
								i = 81;
							}
							else if ((int)val == 281)
							{
								i = 82;
							}
							else if ((int)val == 300)
							{
								i = 97;
							}
							else if ((int)val == 301)
							{
								i = 110;
							}
							else if ((int)val == 303)
							{
								i = 9;
							}
							else if ((int)val == 304)
							{
								i = 5;
							}
							else if ((int)val == 305)
							{
								i = 12;
							}
							else if ((int)val == 306)
							{
								i = 8;
							}
							else if ((int)val == 307)
							{
								i = 10;
							}
							else if ((int)val == 308)
							{
								i = 6;
							}
							else if ((int)val == 309)
							{
								i = 7;
							}
							else if ((int)val == 312)
							{
								i = 11;
							}
							else
							{
								if ((int)val != 313)
								{
									break;
								}
								i = 109;
							}
							PlayerAction obj;
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
																				goto IL_07a7;
																			}
																			obj = Plugin.Self.Session.Players[Hud.selected].Actions.paneRight;
																		}
																		else
																		{
																			obj = Plugin.Self.Session.Players[Hud.selected].Actions.quickMap;
																		}
																	}
																	else
																	{
																		obj = Plugin.Self.Session.Players[Hud.selected].Actions.superDash;
																	}
																}
																else
																{
																	obj = Plugin.Self.Session.Players[Hud.selected].Actions.dreamNail;
																}
															}
															else
															{
																obj = Plugin.Self.Session.Players[Hud.selected].Actions.focus;
															}
														}
														else
														{
															obj = Plugin.Self.Session.Players[Hud.selected].Actions.cast;
														}
													}
													else
													{
														obj = Plugin.Self.Session.Players[Hud.selected].Actions.dash;
													}
												}
												else
												{
													obj = Plugin.Self.Session.Players[Hud.selected].Actions.attack;
												}
											}
											else
											{
												obj = Plugin.Self.Session.Players[Hud.selected].Actions.jump;
											}
										}
										else
										{
											obj = Plugin.Self.Session.Players[Hud.selected].Actions.right;
										}
									}
									else
									{
										obj = Plugin.Self.Session.Players[Hud.selected].Actions.left;
									}
								}
								else
								{
									obj = Plugin.Self.Session.Players[Hud.selected].Actions.down;
								}
							}
							else
							{
								obj = Plugin.Self.Session.Players[Hud.selected].Actions.up;
							}
							obj.ClearBindings();
							obj.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)i }));
							Capturing = 0;
							break;
						}
						goto IL_07a7;
					}
					if (val == Rescue)
					{
						continue;
					}
					Local8Mod.Settings.PvpKey = ((object)(*(KeyCode*)(&val))/*cast due to .constrained prefix*/).ToString();
				}
				Capturing = 0;
				Plugin.Self.ExportSettings();
				break;
				IL_07a7:
				Capturing = 0;
				break;
			}
		}
		else if (s != null && s.Active && s.Gameplay && !Plugin.Self.Panel && !MenuInput.Waiting && Application.isFocused && Input.GetKeyDown(Pvp))
		{
			PvpMatch.Toggle();
		}
	}
}
