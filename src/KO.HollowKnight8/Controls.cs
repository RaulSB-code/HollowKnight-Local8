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
		NativeInputDevice val = (NativeInputDevice)(object)((d is NativeInputDevice) ? d : null);
		if (val != null)
		{
			return VirtualName(val.Info.name);
		}
		UnityInputDevice val2 = (UnityInputDevice)(object)((d is UnityInputDevice) ? d : null);
		if (val2 != null)
		{
			return VirtualName(((InputDevice)val2).Meta);
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
		//IL_002a: Invalid comparison between Unknown and I4
		if (Time.unscaledTime < nextVirtualProbe)
		{
			return;
		}
		nextVirtualProbe = Time.unscaledTime + 2f;
		if (!InputManager.IsSetup || (int)Application.platform != 2)
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
			NativeInputDevice val = (NativeInputDevice)(object)((item is NativeInputDevice) ? item : null);
			UnityInputDevice val2 = (UnityInputDevice)(object)((item is UnityInputDevice) ? item : null);
			Diagnostics.Write("VJOY " + Label(item) + " raw=" + ((val != null) ? val.Info.name : ((InputDevice)val2).Meta) + " buttons=" + item.NumUnknownButtons + " axes=" + item.NumUnknownAnalogs + " id=" + ((val != null) ? val.Info.location : val2.JoystickId.ToString()));
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
			if (!NativeInputDeviceManager.CheckPlatformSupport((ICollection<string>)list))
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
					return ((OneAxisInputControl)d.GetControl((InputControlType)507)).WasPressed;
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
			return string.Join(";", ((PlayerActionSet)actions).Actions.Select((PlayerAction a) => a.Name + ":" + string.Join(",", a.Bindings.Select((BindingSource b) => ((object)b.BindingSourceType/*cast due to .constrained prefix*/).ToString() + "=" + b.Name))));
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
			NativeInputDevice val = (NativeInputDevice)(object)((device is NativeInputDevice) ? device : null);
			UnityInputDevice val2 = (UnityInputDevice)(object)((device is UnityInputDevice) ? device : null);
			return "vjoy|" + ((val != null && !string.IsNullOrEmpty(val.Info.location)) ? ("location:" + val.Info.location) : ((val2 != null) ? ("joystick:" + val2.JoystickId) : ("ordinal:" + VirtualIndex(device))));
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
		return Devices().FirstOrDefault((Func<InputDevice, bool>)((InputDevice d) => Key(d) == key));
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
			((PlayerActionSet)slot.Actions).Device = slot.Device;
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
				slot.Actions.left.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)36 }));
				slot.Actions.right.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)39 }));
				slot.Actions.up.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)58 }));
				slot.Actions.down.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)54 }));
				slot.Actions.jump.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)76 }));
				slot.Actions.attack.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)52 }));
				slot.Actions.cast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)38 }));
				slot.Actions.focus.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)59 }));
				slot.Actions.quickCast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)61 }));
				slot.Actions.dreamNail.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)27 }));
				slot.Actions.dash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)40 }));
				slot.Actions.superDash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)28 }));
				slot.Actions.pause.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)13 }));
				slot.Actions.openInventory.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)27 }));
				slot.Actions.quickMap.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)28 }));
				slot.Actions.menuSubmit.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)52 }));
				slot.Actions.menuCancel.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)40 }));
				slot.Actions.textSpeedup.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)76 }));
				slot.Actions.skipCutscene.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)76 }));
				slot.Actions.paneLeft.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)36 }));
				slot.Actions.paneRight.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)30 }));
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
				slot.Actions.left.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)40 }));
				slot.Actions.right.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)43 }));
				slot.Actions.up.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)55 }));
				slot.Actions.down.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)42 }));
				slot.Actions.jump.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)57 }));
				slot.Actions.attack.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)60 }));
				slot.Actions.cast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)49 }));
				slot.Actions.focus.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)33 }));
				slot.Actions.quickCast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)31 }));
				slot.Actions.dreamNail.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)53 }));
				slot.Actions.dash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)37 }));
				slot.Actions.superDash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)29 }));
				slot.Actions.pause.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)22 }));
				slot.Actions.openInventory.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)32 }));
				slot.Actions.quickMap.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)30 }));
				slot.Actions.menuSubmit.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)34 }));
				slot.Actions.menuCancel.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)35 }));
				slot.Actions.textSpeedup.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)57 }));
				slot.Actions.skipCutscene.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)37 }));
				slot.Actions.paneLeft.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)40 }));
				slot.Actions.paneRight.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)32 }));
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
				slot.Actions.left.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)45 }));
				slot.Actions.right.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)47 }));
				slot.Actions.up.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)58 }));
				slot.Actions.down.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)46 }));
				slot.Actions.jump.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)48 }));
				slot.Actions.attack.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)50 }));
				slot.Actions.cast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)74 }));
				slot.Actions.focus.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)56 }));
				slot.Actions.quickCast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)26 }));
				slot.Actions.dreamNail.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)51 }));
				slot.Actions.dash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)73 }));
				slot.Actions.superDash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)75 }));
				slot.Actions.pause.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)23 }));
				slot.Actions.openInventory.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)68 }));
				slot.Actions.quickMap.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)69 }));
				slot.Actions.menuSubmit.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)70 }));
				slot.Actions.menuCancel.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)71 }));
				slot.Actions.textSpeedup.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)48 }));
				slot.Actions.skipCutscene.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)73 }));
				slot.Actions.paneLeft.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)45 }));
				slot.Actions.paneRight.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)34 }));
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
				slot.Actions.left.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)92 }));
				slot.Actions.right.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)94 }));
				slot.Actions.up.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)96 }));
				slot.Actions.down.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)93 }));
				slot.Actions.jump.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)88 }));
				slot.Actions.attack.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)95 }));
				slot.Actions.cast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)89 }));
				slot.Actions.focus.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)102 }));
				slot.Actions.quickCast.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)100 }));
				slot.Actions.dreamNail.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)91 }));
				slot.Actions.dash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)97 }));
				slot.Actions.superDash.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)90 }));
				slot.Actions.pause.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)24 }));
				slot.Actions.openInventory.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)101 }));
				slot.Actions.quickMap.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)99 }));
				slot.Actions.menuSubmit.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)103 }));
				slot.Actions.menuCancel.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)104 }));
				slot.Actions.textSpeedup.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)103 }));
				slot.Actions.skipCutscene.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)104 }));
				slot.Actions.paneLeft.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)92 }));
				slot.Actions.paneRight.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)103 }));
				break;
			}
		}
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
		//IL_00ba: Invalid comparison between Unknown and I4
		//IL_00d9: Invalid comparison between Unknown and I4
		//IL_00c5: Invalid comparison between Unknown and I4
		HeroActions val = new HeroActions();
		foreach (PlayerAction action in ((PlayerActionSet)source).Actions)
		{
			PlayerAction playerActionByName = ((PlayerActionSet)val).GetPlayerActionByName(action.Name);
			if (playerActionByName == null)
			{
				continue;
			}
			foreach (BindingSource binding in action.Bindings)
			{
				BindingSource val2 = null;
				KeyBindingSource val3 = (KeyBindingSource)(object)((binding is KeyBindingSource) ? binding : null);
				if (keyboard && (BindingSource)(object)val3 != (BindingSource)null)
				{
					val2 = (BindingSource)new KeyBindingSource(val3.Control);
				}
				MouseBindingSource val4 = (MouseBindingSource)(object)((binding is MouseBindingSource) ? binding : null);
				if (keyboard && (BindingSource)(object)val4 != (BindingSource)null)
				{
					val2 = (BindingSource)new MouseBindingSource(val4.Control);
				}
				DeviceBindingSource val5 = (DeviceBindingSource)(object)((binding is DeviceBindingSource) ? binding : null);
				if (!keyboard && (BindingSource)(object)val5 != (BindingSource)null && (action != source.quickCast || ((int)val5.Control != 18 && (int)val5.Control != 16)) && (action != source.dash || (int)val5.Control != 18))
				{
					val2 = (BindingSource)new DeviceBindingSource(val5.Control);
				}
				UnknownDeviceBindingSource val6 = (UnknownDeviceBindingSource)(object)((binding is UnknownDeviceBindingSource) ? binding : null);
				if (!keyboard && (BindingSource)(object)val6 != (BindingSource)null)
				{
					val2 = (BindingSource)new UnknownDeviceBindingSource(val6.Control);
				}
				if (val2 != (BindingSource)null)
				{
					playerActionByName.AddBinding(val2);
				}
			}
		}
		if (!keyboard)
		{
			if (UnknownVJoy(selected))
			{
				MapVirtual(val, selected);
			}
			else
			{
				EnsureControllerFallback(val);
				AddIfMissing(val.dash, (InputControlType)16);
			}
		}
		if (keyboard)
		{
			if (val.pause.Bindings.Count == 0)
			{
				val.pause.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)13 }));
			}
			if (val.menuCancel.Bindings.Count == 0)
			{
				val.menuCancel.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)13 }));
			}
			if (val.menuSubmit.Bindings.Count == 0)
			{
				val.menuSubmit.AddBinding((BindingSource)new KeyBindingSource((Key[])(object)new Key[1] { (Key)72 }));
			}
		}
		if (val.focus.Bindings.Count == 0)
		{
			foreach (BindingSource binding2 in val.cast.Bindings)
			{
				KeyBindingSource val7 = (KeyBindingSource)(object)((binding2 is KeyBindingSource) ? binding2 : null);
				MouseBindingSource val8 = (MouseBindingSource)(object)((binding2 is MouseBindingSource) ? binding2 : null);
				DeviceBindingSource val9 = (DeviceBindingSource)(object)((binding2 is DeviceBindingSource) ? binding2 : null);
				if ((BindingSource)(object)val7 != (BindingSource)null)
				{
					val.focus.AddBinding((BindingSource)new KeyBindingSource(val7.Control));
				}
				else if ((BindingSource)(object)val8 != (BindingSource)null)
				{
					val.focus.AddBinding((BindingSource)new MouseBindingSource(val8.Control));
				}
				else if ((BindingSource)(object)val9 != (BindingSource)null)
				{
					val.focus.AddBinding((BindingSource)new DeviceBindingSource(val9.Control));
				}
			}
		}
		return val;
	}

	private static void MapVirtual(HeroActions a, InputDevice d)
	{
		Axis(a.left, d, 0, (InputRangeType)4);
		Axis(a.right, d, 0, (InputRangeType)3);
		Axis(a.up, d, 1, (InputRangeType)4);
		Axis(a.down, d, 1, (InputRangeType)3);
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
			action.AddBinding((BindingSource)new UnknownDeviceBindingSource(new UnknownDeviceControl((InputControlType)(400 + index), range)));
		}
	}

	private static void Button(PlayerAction action, InputDevice d, int number)
	{
		action.ClearBindings();
		if (d.NumUnknownButtons >= number)
		{
			action.AddBinding((BindingSource)new UnknownDeviceBindingSource(new UnknownDeviceControl((InputControlType)(499 + number), (InputRangeType)3)));
		}
	}

	private static void EnsureControllerFallback(HeroActions a)
	{
		AddIfEmpty(a.left, (InputControlType)3, (InputControlType)13);
		AddIfEmpty(a.right, (InputControlType)4, (InputControlType)14);
		AddIfEmpty(a.up, (InputControlType)1, (InputControlType)11);
		AddIfEmpty(a.down, (InputControlType)2, (InputControlType)12);
		AddIfEmpty(a.jump, (InputControlType)19);
		AddIfEmpty(a.attack, (InputControlType)21);
		AddIfEmpty(a.dash, (InputControlType)16);
		AddIfEmpty(a.cast, (InputControlType)20);
		AddIfEmpty(a.superDash, (InputControlType)15);
		AddIfEmpty(a.dreamNail, (InputControlType)22);
		AddIfEmpty(a.quickMap, (InputControlType)17);
		AddIfEmpty(a.openInventory, (InputControlType)102);
		AddIfEmpty(a.pause, (InputControlType)101);
		AddIfEmpty(a.menuSubmit, (InputControlType)19);
		AddIfEmpty(a.menuCancel, (InputControlType)20);
		AddIfEmpty(a.textSpeedup, (InputControlType)19);
		AddIfEmpty(a.skipCutscene, (InputControlType)20);
	}

	private static void AddIfEmpty(PlayerAction action, params InputControlType[] controls)
	{
		if (action.Bindings.Count == 0)
		{
			foreach (InputControlType val in controls)
			{
				action.AddBinding((BindingSource)new DeviceBindingSource(val));
			}
		}
	}

	private static void AddIfMissing(PlayerAction action, InputControlType control)
	{
		if (!action.Bindings.OfType<DeviceBindingSource>().Any((DeviceBindingSource b) => b.Control == control))
		{
			action.AddBinding((BindingSource)new DeviceBindingSource(control));
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
				((PlayerActionSet)slot.Actions).ClearInputState();
			}
		}
	}

	internal static void Dispose(PlayerSlot slot)
	{
		if (slot.Actions != null && slot.OwnsActions)
		{
			((PlayerActionSet)slot.Actions).Destroy();
		}
		slot.Actions = null;
		slot.OwnsActions = false;
	}
}
