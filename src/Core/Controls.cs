using System;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class Controls
{
	private static float nextVirtualProbe;

	private static bool nativeProbeFailed;

	private static bool nativeAddedByMod;

	private static int lastVirtualCount = -1;

	internal static int VirtualCount => Devices().Count(IsVJoy);

	internal static bool NativeActive
	{
		get
		{
			if (InputManager.IsSetup)
			{
				return InputManager.HasDeviceManager<NativeInputDeviceManager>();
			}
			return false;
		}
	}

	private static bool VirtualName(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			if (name.IndexOf("vjoy", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return name.IndexOf("virtual joystick", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			return true;
		}
		return false;
	}

	internal static bool IsVJoy(InputDevice d)
	{
		if (d is NativeInputDevice nativeInputDevice)
		{
			return VirtualName(nativeInputDevice.Info.name);
		}
		if (d is UnityInputDevice unityInputDevice)
		{
			return VirtualName(unityInputDevice.Meta);
		}
		return false;
	}

	private static bool UnknownVJoy(InputDevice d)
	{
		if (IsVJoy(d))
		{
			return d.IsUnknown;
		}
		return false;
	}

	private static int VirtualIndex(InputDevice d)
	{
		int num = 0;
		foreach (InputDevice item in Devices())
		{
			if (item == d)
			{
				break;
			}
			if (IsVJoy(item))
			{
				num++;
			}
		}
		return num + 1;
	}

	internal static string Label(InputDevice d)
	{
		if (!IsVJoy(d))
		{
			return d.Name;
		}
		return "vJoy " + VirtualIndex(d) + " (DirectInput)";
	}

	internal static void ProbeVirtual()
	{
		if (Time.unscaledTime < nextVirtualProbe)
		{
			return;
		}
		nextVirtualProbe = Time.unscaledTime + 2f;
		if (!InputManager.IsSetup || Application.platform != RuntimePlatform.WindowsPlayer)
		{
			return;
		}
		if (!NativeActive)
		{
			nativeAddedByMod = false;
		}
		if (!NativeActive && !nativeProbeFailed && VirtualCount == 0)
		{
			bool flag = !InputManager.HasDeviceManager<UnityInputDeviceManager>();
			bool flag2 = false;
			if (!flag && Time.unscaledTime > 5f)
			{
				try
				{
					flag2 = Input.GetJoystickNames().Any(VirtualName);
				}
				catch (Exception ex)
				{
					Diagnostics.Throttled("VJOY joystick probe", ex);
				}
			}
			if (flag || flag2)
			{
				EnableDirectInput(requested: false);
			}
		}
		int virtualCount = VirtualCount;
		if (virtualCount == lastVirtualCount)
		{
			return;
		}
		lastVirtualCount = virtualCount;
		Diagnostics.Write("VJOY detected=" + virtualCount + " native=" + NativeActive + " total=" + Devices().Count());
		foreach (InputDevice item in Devices().Where(IsVJoy))
		{
			NativeInputDevice nativeInputDevice = item as NativeInputDevice;
			UnityInputDevice unityInputDevice = item as UnityInputDevice;
			Diagnostics.Write("VJOY " + Label(item) + " raw=" + ((nativeInputDevice != null) ? nativeInputDevice.Info.name : unityInputDevice.Meta) + " buttons=" + item.NumUnknownButtons + " axes=" + item.NumUnknownAnalogs + " id=" + ((nativeInputDevice != null) ? nativeInputDevice.Info.location : unityInputDevice.JoystickId.ToString()));
		}
	}

	internal static void EnableDirectInput(bool requested)
	{
		if (!InputManager.IsSetup)
		{
			if (requested)
			{
				Plugin.Self.Notice("El sistema de mandos aun no esta listo.");
			}
			return;
		}
		if (NativeActive)
		{
			if (requested)
			{
				Plugin.Self.Notice("DirectInput activo. vJoy detectados: " + VirtualCount + ". Comprueba vJoy y su alimentador si faltan.");
			}
			return;
		}
		if (VirtualCount > 0)
		{
			if (requested)
			{
				Plugin.Self.Notice("vJoy ya esta detectado: " + VirtualCount + " dispositivo(s). Puedes unirlos desde F8.");
			}
			return;
		}
		try
		{
			List<string> list = new List<string>();
			if (!NativeInputDeviceManager.CheckPlatformSupport(list))
			{
				nativeProbeFailed = true;
				Diagnostics.Write("VJOY DirectInput no disponible: " + string.Join("; ", list.ToArray()));
				if (requested)
				{
					Plugin.Self.Notice("El lector DirectInput no esta disponible. Mira Local8-Logs.");
				}
				return;
			}
			InputManager.AddDeviceManager<NativeInputDeviceManager>();
			nativeAddedByMod = true;
			nativeProbeFailed = false;
			Diagnostics.Write("VJOY DirectInput activado: lector nativo InControl, sin reemplazar los controles del juego.");
			if (requested)
			{
				Plugin.Self.Notice("Buscando vJoy. Espera unos segundos y vuelve a abrir F8.");
			}
		}
		catch (Exception ex)
		{
			nativeProbeFailed = true;
			Diagnostics.Throttled("VJOY DirectInput", ex);
			if (requested)
			{
				Plugin.Self.Notice("No se pudo iniciar DirectInput. Mira Local8-Logs.");
			}
		}
	}

	internal static IEnumerable<InputDevice> Devices()
	{
		IEnumerable<InputDevice> enumerable = InputManager.Devices.Where((InputDevice d) => d != null && d.IsAttached && !d.Passive);
		if (nativeAddedByMod)
		{
			bool nativeVirtual = enumerable.Any((InputDevice d) => d is NativeInputDevice && IsVJoy(d));
			enumerable = enumerable.Where((InputDevice d) => (!(d is NativeInputDevice) || IsVJoy(d)) && (!nativeVirtual || !(d is UnityInputDevice) || !IsVJoy(d)));
		}
		return enumerable;
	}

	internal static bool JoinPressed(InputDevice d)
	{
		if (d != null)
		{
			if (!d.CommandWasPressed)
			{
				if (UnknownVJoy(d) && d.NumUnknownButtons >= 8)
				{
					return d.GetControl(InputControlType.Button7).WasPressed;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal static string Signature(HeroActions actions)
	{
		if (actions != null)
		{
			return string.Join(";", actions.Actions.Select((PlayerAction a) => a.Name + ":" + string.Join(",", a.Bindings.Select((BindingSource b) => b.BindingSourceType.ToString() + "=" + b.Name))));
		}
		return "";
	}

	internal static string Key(InputDevice device)
	{
		if (device == null || device == InputDevice.Null)
		{
			return "Keyboard";
		}
		if (IsVJoy(device))
		{
			NativeInputDevice nativeInputDevice = device as NativeInputDevice;
			UnityInputDevice unityInputDevice = device as UnityInputDevice;
			return "vjoy|" + ((nativeInputDevice != null && !string.IsNullOrEmpty(nativeInputDevice.Info.location)) ? ("location:" + nativeInputDevice.Info.location) : ((unityInputDevice != null) ? ("joystick:" + unityInputDevice.JoystickId) : ("ordinal:" + VirtualIndex(device))));
		}
		int num = 0;
		foreach (InputDevice item in Devices())
		{
			if (item == device)
			{
				break;
			}
			if (item.Name == device.Name)
			{
				num++;
			}
		}
		return device.GUID.ToString("N") + "|" + device.Name + "|" + num;
	}

	internal static InputDevice Find(string key)
	{
		if (string.IsNullOrEmpty(key) || key == "Keyboard")
		{
			return InputDevice.Null;
		}
		return Devices().FirstOrDefault((InputDevice d) => Key(d) == key);
	}

	internal static bool IsKeyboard(InputDevice device)
	{
		if (device != null)
		{
			return device == InputDevice.Null;
		}
		return true;
	}

	internal static void Bind(PlayerSlot slot, InputDevice device)
	{
		slot.Device = device ?? InputDevice.Null;
		slot.DeviceKey = Key(slot.Device);
		slot.DeviceName = ((slot.Device == InputDevice.Null) ? "Teclado" : Label(slot.Device));
		slot.Connected = slot.Device == InputDevice.Null || slot.Device.IsAttached;
		if (slot.Actions != null)
		{
			slot.Actions.Device = slot.Device;
		}
	}

	internal static void InitPrimary(PlayerSlot slot, HeroActions vanilla, InputDevice device)
	{
		slot.Actions = CreateFiltered(vanilla, IsKeyboard(device), device);
		slot.OwnsActions = true;
		Bind(slot, device);
	}

	internal static void InitSecondary(PlayerSlot slot, HeroActions vanilla, InputDevice device)
	{
		slot.Actions = CreateFiltered(vanilla, IsKeyboard(device), device);
		slot.OwnsActions = true;
		Bind(slot, device);
		if (device == InputDevice.Null)
		{
			int num = 0;
			if (slot.Index > 0 && IsKeyboard(Local8Runtime.Self.Session.Players[0].Device))
			{
				num++;
			}
			if (slot.Index > 1 && IsKeyboard(Local8Runtime.Self.Session.Players[1].Device))
			{
				num++;
			}
			if (slot.Index > 2 && IsKeyboard(Local8Runtime.Self.Session.Players[2].Device))
			{
				num++;
			}
			if (slot.Index > 3 && IsKeyboard(Local8Runtime.Self.Session.Players[3].Device))
			{
				num++;
			}
			if (slot.Index > 4 && IsKeyboard(Local8Runtime.Self.Session.Players[4].Device))
			{
				num++;
			}
			if (slot.Index > 5 && IsKeyboard(Local8Runtime.Self.Session.Players[5].Device))
			{
				num++;
			}
			if (slot.Index > 6 && IsKeyboard(Local8Runtime.Self.Session.Players[6].Device))
			{
				num++;
			}
			if (slot.Index > 7 && IsKeyboard(Local8Runtime.Self.Session.Players[7].Device))
			{
				num++;
			}
			switch (num)
			{
			case 0:
				slot.Actions.left.ClearBindings();
				slot.Actions.right.ClearBindings();
				slot.Actions.up.ClearBindings();
				slot.Actions.down.ClearBindings();
				slot.Actions.jump.ClearBindings();
				slot.Actions.attack.ClearBindings();
				slot.Actions.cast.ClearBindings();
				slot.Actions.focus.ClearBindings();
				slot.Actions.quickCast.ClearBindings();
				slot.Actions.dreamNail.ClearBindings();
				slot.Actions.dash.ClearBindings();
				slot.Actions.superDash.ClearBindings();
				slot.Actions.pause.ClearBindings();
				slot.Actions.openInventory.ClearBindings();
				slot.Actions.quickMap.ClearBindings();
				slot.Actions.menuSubmit.ClearBindings();
				slot.Actions.menuCancel.ClearBindings();
				slot.Actions.textSpeedup.ClearBindings();
				slot.Actions.skipCutscene.ClearBindings();
				slot.Actions.paneLeft.ClearBindings();
				slot.Actions.paneRight.ClearBindings();
				slot.Actions.left.AddBinding(new KeyBindingSource(InControl.Key.A));
				slot.Actions.right.AddBinding(new KeyBindingSource(InControl.Key.D));
				slot.Actions.up.AddBinding(new KeyBindingSource(InControl.Key.W));
				slot.Actions.down.AddBinding(new KeyBindingSource(InControl.Key.S));
				slot.Actions.jump.AddBinding(new KeyBindingSource(InControl.Key.Space));
				slot.Actions.attack.AddBinding(new KeyBindingSource(InControl.Key.Q));
				slot.Actions.cast.AddBinding(new KeyBindingSource(InControl.Key.C));
				slot.Actions.focus.AddBinding(new KeyBindingSource(InControl.Key.X));
				slot.Actions.quickCast.AddBinding(new KeyBindingSource(InControl.Key.Z));
				slot.Actions.dreamNail.AddBinding(new KeyBindingSource(InControl.Key.Key1));
				slot.Actions.dash.AddBinding(new KeyBindingSource(InControl.Key.E));
				slot.Actions.superDash.AddBinding(new KeyBindingSource(InControl.Key.Key2));
				slot.Actions.pause.AddBinding(new KeyBindingSource(InControl.Key.Escape));
				slot.Actions.openInventory.AddBinding(new KeyBindingSource(InControl.Key.Key1));
				slot.Actions.quickMap.AddBinding(new KeyBindingSource(InControl.Key.Key2));
				slot.Actions.menuSubmit.AddBinding(new KeyBindingSource(InControl.Key.Q));
				slot.Actions.menuCancel.AddBinding(new KeyBindingSource(InControl.Key.E));
				slot.Actions.textSpeedup.AddBinding(new KeyBindingSource(InControl.Key.Space));
				slot.Actions.skipCutscene.AddBinding(new KeyBindingSource(InControl.Key.Space));
				slot.Actions.paneLeft.AddBinding(new KeyBindingSource(InControl.Key.A));
				slot.Actions.paneRight.AddBinding(new KeyBindingSource(InControl.Key.Key4));
				break;
			case 1:
				slot.Actions.left.ClearBindings();
				slot.Actions.right.ClearBindings();
				slot.Actions.up.ClearBindings();
				slot.Actions.down.ClearBindings();
				slot.Actions.jump.ClearBindings();
				slot.Actions.attack.ClearBindings();
				slot.Actions.cast.ClearBindings();
				slot.Actions.focus.ClearBindings();
				slot.Actions.quickCast.ClearBindings();
				slot.Actions.dreamNail.ClearBindings();
				slot.Actions.dash.ClearBindings();
				slot.Actions.superDash.ClearBindings();
				slot.Actions.pause.ClearBindings();
				slot.Actions.openInventory.ClearBindings();
				slot.Actions.quickMap.ClearBindings();
				slot.Actions.menuSubmit.ClearBindings();
				slot.Actions.menuCancel.ClearBindings();
				slot.Actions.textSpeedup.ClearBindings();
				slot.Actions.skipCutscene.ClearBindings();
				slot.Actions.paneLeft.ClearBindings();
				slot.Actions.paneRight.ClearBindings();
				slot.Actions.left.AddBinding(new KeyBindingSource(InControl.Key.E));
				slot.Actions.right.AddBinding(new KeyBindingSource(InControl.Key.H));
				slot.Actions.up.AddBinding(new KeyBindingSource(InControl.Key.T));
				slot.Actions.down.AddBinding(new KeyBindingSource(InControl.Key.G));
				slot.Actions.jump.AddBinding(new KeyBindingSource(InControl.Key.V));
				slot.Actions.attack.AddBinding(new KeyBindingSource(InControl.Key.Y));
				slot.Actions.cast.AddBinding(new KeyBindingSource(InControl.Key.N));
				slot.Actions.focus.AddBinding(new KeyBindingSource(InControl.Key.Key7));
				slot.Actions.quickCast.AddBinding(new KeyBindingSource(InControl.Key.Key5));
				slot.Actions.dreamNail.AddBinding(new KeyBindingSource(InControl.Key.R));
				slot.Actions.dash.AddBinding(new KeyBindingSource(InControl.Key.B));
				slot.Actions.superDash.AddBinding(new KeyBindingSource(InControl.Key.Key3));
				slot.Actions.pause.AddBinding(new KeyBindingSource(InControl.Key.F9));
				slot.Actions.openInventory.AddBinding(new KeyBindingSource(InControl.Key.Key6));
				slot.Actions.quickMap.AddBinding(new KeyBindingSource(InControl.Key.Key4));
				slot.Actions.menuSubmit.AddBinding(new KeyBindingSource(InControl.Key.Key8));
				slot.Actions.menuCancel.AddBinding(new KeyBindingSource(InControl.Key.Key9));
				slot.Actions.textSpeedup.AddBinding(new KeyBindingSource(InControl.Key.V));
				slot.Actions.skipCutscene.AddBinding(new KeyBindingSource(InControl.Key.B));
				slot.Actions.paneLeft.AddBinding(new KeyBindingSource(InControl.Key.E));
				slot.Actions.paneRight.AddBinding(new KeyBindingSource(InControl.Key.Key6));
				break;
			case 2:
				slot.Actions.left.ClearBindings();
				slot.Actions.right.ClearBindings();
				slot.Actions.up.ClearBindings();
				slot.Actions.down.ClearBindings();
				slot.Actions.jump.ClearBindings();
				slot.Actions.attack.ClearBindings();
				slot.Actions.cast.ClearBindings();
				slot.Actions.focus.ClearBindings();
				slot.Actions.quickCast.ClearBindings();
				slot.Actions.dreamNail.ClearBindings();
				slot.Actions.dash.ClearBindings();
				slot.Actions.superDash.ClearBindings();
				slot.Actions.pause.ClearBindings();
				slot.Actions.openInventory.ClearBindings();
				slot.Actions.quickMap.ClearBindings();
				slot.Actions.menuSubmit.ClearBindings();
				slot.Actions.menuCancel.ClearBindings();
				slot.Actions.textSpeedup.ClearBindings();
				slot.Actions.skipCutscene.ClearBindings();
				slot.Actions.paneLeft.ClearBindings();
				slot.Actions.paneRight.ClearBindings();
				slot.Actions.left.AddBinding(new KeyBindingSource(InControl.Key.J));
				slot.Actions.right.AddBinding(new KeyBindingSource(InControl.Key.L));
				slot.Actions.up.AddBinding(new KeyBindingSource(InControl.Key.W));
				slot.Actions.down.AddBinding(new KeyBindingSource(InControl.Key.K));
				slot.Actions.jump.AddBinding(new KeyBindingSource(InControl.Key.M));
				slot.Actions.attack.AddBinding(new KeyBindingSource(InControl.Key.O));
				slot.Actions.cast.AddBinding(new KeyBindingSource(InControl.Key.Period));
				slot.Actions.focus.AddBinding(new KeyBindingSource(InControl.Key.U));
				slot.Actions.quickCast.AddBinding(new KeyBindingSource(InControl.Key.Key0));
				slot.Actions.dreamNail.AddBinding(new KeyBindingSource(InControl.Key.P));
				slot.Actions.dash.AddBinding(new KeyBindingSource(InControl.Key.Comma));
				slot.Actions.superDash.AddBinding(new KeyBindingSource(InControl.Key.Slash));
				slot.Actions.pause.AddBinding(new KeyBindingSource(InControl.Key.F10));
				slot.Actions.openInventory.AddBinding(new KeyBindingSource(InControl.Key.RightBracket));
				slot.Actions.quickMap.AddBinding(new KeyBindingSource(InControl.Key.Backslash));
				slot.Actions.menuSubmit.AddBinding(new KeyBindingSource(InControl.Key.Semicolon));
				slot.Actions.menuCancel.AddBinding(new KeyBindingSource(InControl.Key.Quote));
				slot.Actions.textSpeedup.AddBinding(new KeyBindingSource(InControl.Key.M));
				slot.Actions.skipCutscene.AddBinding(new KeyBindingSource(InControl.Key.Comma));
				slot.Actions.paneLeft.AddBinding(new KeyBindingSource(InControl.Key.J));
				slot.Actions.paneRight.AddBinding(new KeyBindingSource(InControl.Key.Key8));
				break;
			default:
				slot.Actions.left.ClearBindings();
				slot.Actions.right.ClearBindings();
				slot.Actions.up.ClearBindings();
				slot.Actions.down.ClearBindings();
				slot.Actions.jump.ClearBindings();
				slot.Actions.attack.ClearBindings();
				slot.Actions.cast.ClearBindings();
				slot.Actions.focus.ClearBindings();
				slot.Actions.quickCast.ClearBindings();
				slot.Actions.dreamNail.ClearBindings();
				slot.Actions.dash.ClearBindings();
				slot.Actions.superDash.ClearBindings();
				slot.Actions.pause.ClearBindings();
				slot.Actions.openInventory.ClearBindings();
				slot.Actions.quickMap.ClearBindings();
				slot.Actions.menuSubmit.ClearBindings();
				slot.Actions.menuCancel.ClearBindings();
				slot.Actions.textSpeedup.ClearBindings();
				slot.Actions.skipCutscene.ClearBindings();
				slot.Actions.paneLeft.ClearBindings();
				slot.Actions.paneRight.ClearBindings();
				slot.Actions.left.AddBinding(new KeyBindingSource(InControl.Key.Pad5));
				slot.Actions.right.AddBinding(new KeyBindingSource(InControl.Key.Pad7));
				slot.Actions.up.AddBinding(new KeyBindingSource(InControl.Key.Pad9));
				slot.Actions.down.AddBinding(new KeyBindingSource(InControl.Key.Pad6));
				slot.Actions.jump.AddBinding(new KeyBindingSource(InControl.Key.Pad1));
				slot.Actions.attack.AddBinding(new KeyBindingSource(InControl.Key.Pad8));
				slot.Actions.cast.AddBinding(new KeyBindingSource(InControl.Key.Pad2));
				slot.Actions.focus.AddBinding(new KeyBindingSource(InControl.Key.PadEnter));
				slot.Actions.quickCast.AddBinding(new KeyBindingSource(InControl.Key.PadMinus));
				slot.Actions.dreamNail.AddBinding(new KeyBindingSource(InControl.Key.Pad4));
				slot.Actions.dash.AddBinding(new KeyBindingSource(InControl.Key.Numlock));
				slot.Actions.superDash.AddBinding(new KeyBindingSource(InControl.Key.Pad3));
				slot.Actions.pause.AddBinding(new KeyBindingSource(InControl.Key.F11));
				slot.Actions.openInventory.AddBinding(new KeyBindingSource(InControl.Key.PadPlus));
				slot.Actions.quickMap.AddBinding(new KeyBindingSource(InControl.Key.PadMultiply));
				slot.Actions.menuSubmit.AddBinding(new KeyBindingSource(InControl.Key.PadPeriod));
				slot.Actions.menuCancel.AddBinding(new KeyBindingSource(InControl.Key.Clear));
				slot.Actions.textSpeedup.AddBinding(new KeyBindingSource(InControl.Key.PadPeriod));
				slot.Actions.skipCutscene.AddBinding(new KeyBindingSource(InControl.Key.Clear));
				slot.Actions.paneLeft.AddBinding(new KeyBindingSource(InControl.Key.Pad5));
				slot.Actions.paneRight.AddBinding(new KeyBindingSource(InControl.Key.PadPeriod));
				break;
			}
		}
		KeypadInput.Defaults(slot);
		KeyboardMemory.Restore(slot);
		KeypadInput.RepairMenus(slot);
	}

	internal static void RebindPrimary(PlayerSlot slot, HeroActions vanilla, InputDevice device)
	{
		if (slot.Index > 0)
		{
			if (!IsKeyboard(slot.Device) || slot.Actions == null)
			{
				Dispose(slot);
				InitSecondary(slot, vanilla, device);
			}
		}
		else
		{
			Dispose(slot);
			InitPrimary(slot, vanilla, device);
		}
	}

	private static HeroActions CreateFiltered(HeroActions source, bool keyboard, InputDevice selected)
	{
		HeroActions heroActions = new HeroActions();
		foreach (PlayerAction action in source.Actions)
		{
			PlayerAction playerActionByName = heroActions.GetPlayerActionByName(action.Name);
			if (playerActionByName == null)
			{
				continue;
			}
			foreach (BindingSource binding in action.Bindings)
			{
				BindingSource bindingSource = null;
				KeyBindingSource keyBindingSource = binding as KeyBindingSource;
				if (keyboard && keyBindingSource != null)
				{
					bindingSource = new KeyBindingSource(keyBindingSource.Control);
				}
				MouseBindingSource mouseBindingSource = binding as MouseBindingSource;
				if (keyboard && mouseBindingSource != null)
				{
					bindingSource = new MouseBindingSource(mouseBindingSource.Control);
				}
				DeviceBindingSource deviceBindingSource = binding as DeviceBindingSource;
				if (!keyboard && deviceBindingSource != null && (action != source.quickCast || (deviceBindingSource.Control != InputControlType.RightBumper && deviceBindingSource.Control != InputControlType.RightTrigger)) && (action != source.dash || deviceBindingSource.Control != InputControlType.RightBumper))
				{
					bindingSource = new DeviceBindingSource(deviceBindingSource.Control);
				}
				UnknownDeviceBindingSource unknownDeviceBindingSource = binding as UnknownDeviceBindingSource;
				if (!keyboard && unknownDeviceBindingSource != null)
				{
					bindingSource = new UnknownDeviceBindingSource(unknownDeviceBindingSource.Control);
				}
				if (bindingSource != null)
				{
					playerActionByName.AddBinding(bindingSource);
				}
			}
		}
		if (!keyboard)
		{
			if (UnknownVJoy(selected))
			{
				MapVirtual(heroActions, selected);
			}
			else
			{
				EnsureControllerFallback(heroActions);
				AddIfMissing(heroActions.dash, InputControlType.RightTrigger);
			}
		}
		if (keyboard)
		{
			if (heroActions.pause.Bindings.Count == 0)
			{
				heroActions.pause.AddBinding(new KeyBindingSource(InControl.Key.Escape));
			}
			if (heroActions.menuCancel.Bindings.Count == 0)
			{
				heroActions.menuCancel.AddBinding(new KeyBindingSource(InControl.Key.Escape));
			}
			if (heroActions.menuSubmit.Bindings.Count == 0)
			{
				heroActions.menuSubmit.AddBinding(new KeyBindingSource(InControl.Key.Return));
			}
		}
		if (heroActions.focus.Bindings.Count == 0)
		{
			foreach (BindingSource binding2 in heroActions.cast.Bindings)
			{
				KeyBindingSource keyBindingSource2 = binding2 as KeyBindingSource;
				MouseBindingSource mouseBindingSource2 = binding2 as MouseBindingSource;
				DeviceBindingSource deviceBindingSource2 = binding2 as DeviceBindingSource;
				if (keyBindingSource2 != null)
				{
					heroActions.focus.AddBinding(new KeyBindingSource(keyBindingSource2.Control));
				}
				else if (mouseBindingSource2 != null)
				{
					heroActions.focus.AddBinding(new MouseBindingSource(mouseBindingSource2.Control));
				}
				else if (deviceBindingSource2 != null)
				{
					heroActions.focus.AddBinding(new DeviceBindingSource(deviceBindingSource2.Control));
				}
			}
			return heroActions;
		}
		return heroActions;
	}

	private static void MapVirtual(HeroActions a, InputDevice d)
	{
		Axis(a.left, d, 0, InputRangeType.ZeroToMinusOne);
		Axis(a.right, d, 0, InputRangeType.ZeroToOne);
		Axis(a.up, d, 1, InputRangeType.ZeroToMinusOne);
		Axis(a.down, d, 1, InputRangeType.ZeroToOne);
		Button(a.jump, d, 1);
		Button(a.cast, d, 2);
		Button(a.focus, d, 2);
		Button(a.attack, d, 3);
		Button(a.dreamNail, d, 4);
		Button(a.quickMap, d, 5);
		Button(a.dash, d, 6);
		Button(a.superDash, d, 7);
		Button(a.pause, d, 8);
		Button(a.openInventory, d, 9);
		Button(a.quickCast, d, 10);
		Button(a.menuSubmit, d, 1);
		Button(a.menuCancel, d, 2);
		Button(a.textSpeedup, d, 1);
		Button(a.skipCutscene, d, 2);
		Diagnostics.Write("VJOY mapped " + Label(d) + " axes=" + d.NumUnknownAnalogs + " buttons=" + d.NumUnknownButtons);
	}

	private static void Axis(PlayerAction action, InputDevice d, int index, InputRangeType range)
	{
		action.ClearBindings();
		if (d.NumUnknownAnalogs > index)
		{
			action.AddBinding(new UnknownDeviceBindingSource(new UnknownDeviceControl((InputControlType)(400 + index), range)));
		}
	}

	private static void Button(PlayerAction action, InputDevice d, int number)
	{
		action.ClearBindings();
		if (d.NumUnknownButtons >= number)
		{
			action.AddBinding(new UnknownDeviceBindingSource(new UnknownDeviceControl((InputControlType)(499 + number), InputRangeType.ZeroToOne)));
		}
	}

	private static void EnsureControllerFallback(HeroActions a)
	{
		AddIfEmpty(a.left, InputControlType.LeftStickLeft, InputControlType.DPadLeft);
		AddIfEmpty(a.right, InputControlType.LeftStickRight, InputControlType.DPadRight);
		AddIfEmpty(a.up, InputControlType.LeftStickUp, InputControlType.DPadUp);
		AddIfEmpty(a.down, InputControlType.LeftStickDown, InputControlType.DPadDown);
		AddIfEmpty(a.jump, InputControlType.Action1);
		AddIfEmpty(a.attack, InputControlType.Action3);
		AddIfEmpty(a.dash, InputControlType.RightTrigger);
		AddIfEmpty(a.cast, InputControlType.Action2);
		AddIfEmpty(a.superDash, InputControlType.LeftTrigger);
		AddIfEmpty(a.dreamNail, InputControlType.Action4);
		AddIfEmpty(a.quickMap, InputControlType.LeftBumper);
		AddIfEmpty(a.openInventory, InputControlType.Select);
		AddIfEmpty(a.pause, InputControlType.Start);
		AddIfEmpty(a.menuSubmit, InputControlType.Action1);
		AddIfEmpty(a.menuCancel, InputControlType.Action2);
		AddIfEmpty(a.textSpeedup, InputControlType.Action1);
		AddIfEmpty(a.skipCutscene, InputControlType.Action2);
	}

	private static void AddIfEmpty(PlayerAction action, params InputControlType[] controls)
	{
		if (action.Bindings.Count == 0)
		{
			foreach (InputControlType control in controls)
			{
				action.AddBinding(new DeviceBindingSource(control));
			}
		}
	}

	private static void AddIfMissing(PlayerAction action, InputControlType control)
	{
		if (!action.Bindings.OfType<DeviceBindingSource>().Any((DeviceBindingSource b) => b.Control == control))
		{
			action.AddBinding(new DeviceBindingSource(control));
		}
	}

	internal static void Pump(PlayerSlot slot, bool block)
	{
		if (slot != null && slot.Actions != null)
		{
			slot.Connected = slot.Device == InputDevice.Null || slot.Device.IsAttached;
			slot.InputBlocked = block || slot.ArenaTransfer || !slot.Connected || !Application.isFocused;
			if (slot.InputBlocked)
			{
				slot.Actions.ClearInputState();
			}
		}
	}

	internal static void Dispose(PlayerSlot slot)
	{
		if (slot.Actions != null && slot.OwnsActions)
		{
			slot.Actions.Destroy();
		}
		slot.Actions = null;
		slot.OwnsActions = false;
	}
}
