using System;
using System.Runtime.InteropServices;
using InControl;
using UnityEngine;
namespace KO.HollowKnight8 {
// InControl's normal binding/action pipeline still owns pressed/released states.
// Read physical keypad keys before it, instead of aliasing them to P1's arrows.
internal static class KeypadInput {
 static bool installed,windows,focused,physicalReturn;static int frame=-1;static IntPtr hook;
 static readonly bool[] physical=new bool[18],held=new bool[18],pressed=new bool[18];
 static readonly bool[] navigation=new bool[11],observedNavigation=new bool[11];
 static readonly KeyCode[] unity={KeyCode.Keypad0,KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5,KeyCode.Keypad6,KeyCode.Keypad7,KeyCode.Keypad8,KeyCode.Keypad9,KeyCode.Numlock,KeyCode.KeypadDivide,KeyCode.KeypadMultiply,KeyCode.KeypadMinus,KeyCode.KeypadPlus,KeyCode.KeypadEnter,KeyCode.KeypadPeriod,KeyCode.KeypadEquals};
 static readonly Key[] keys={Key.Pad0,Key.Pad1,Key.Pad2,Key.Pad3,Key.Pad4,Key.Pad5,Key.Pad6,Key.Pad7,Key.Pad8,Key.Pad9,Key.Numlock,Key.PadDivide,Key.PadMultiply,Key.PadMinus,Key.PadPlus,Key.PadEnter,Key.PadPeriod,Key.PadEquals};
 static readonly Key[] navKeys={Key.Insert,Key.End,Key.DownArrow,Key.PageDown,Key.LeftArrow,Key.Clear,Key.RightArrow,Key.Home,Key.UpArrow,Key.PageUp,Key.Delete};
 static readonly KeyCode[] navUnity={KeyCode.Insert,KeyCode.End,KeyCode.DownArrow,KeyCode.PageDown,KeyCode.LeftArrow,KeyCode.Clear,KeyCode.RightArrow,KeyCode.Home,KeyCode.UpArrow,KeyCode.PageUp,KeyCode.Delete};
 static readonly int[] navVk={0x2D,0x23,0x28,0x22,0x25,0x0C,0x27,0x24,0x26,0x21,0x2E};
 static readonly int[] virtualKeys={0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x90,0x6F,0x6A,0x6D,0x6B,0,0x6E,0};
 static readonly NativeCallback callback=NativeEvent;
 delegate IntPtr NativeCallback(int code,IntPtr message,IntPtr data);
 [StructLayout(LayoutKind.Sequential)] struct KeyboardEvent {internal uint VirtualKey,ScanCode,Flags,Time;internal UIntPtr Extra;}
 [DllImport("user32.dll",EntryPoint="SetWindowsHookExW")] static extern IntPtr SetHook(int type,NativeCallback proc,IntPtr module,uint thread);
 [DllImport("user32.dll",EntryPoint="CallNextHookEx")] static extern IntPtr NextHook(IntPtr handle,int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll",EntryPoint="UnhookWindowsHookEx")] static extern bool RemoveHook(IntPtr handle);
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("kernel32.dll",EntryPoint="GetModuleHandleW",CharSet=CharSet.Unicode)] static extern IntPtr ModuleHandle(string name);
 internal static void Install(){if(installed)return;installed=true;
  windows=Application.platform==RuntimePlatform.WindowsPlayer||Application.platform==RuntimePlatform.WindowsEditor;
  focused=Application.isFocused;
  if(windows)try{hook=SetHook(13,callback,ModuleHandle(null),0);Diagnostics.Write("KEYPAD native reader="+(hook!=IntPtr.Zero?"physical":"polling fallback"));}catch(Exception e){windows=false;Diagnostics.Write("KEYPAD Unity fallback "+e.GetType().Name);}
  On.InControl.UnityKeyboardProvider.GetKeyIsPressed+=Read;
  On.InControl.UnityKeyboardProvider.AnyKeyIsPressed+=Any;
  On.InControl.UnityKeyboardProvider.Update+=Update;
 }
 internal static void Defaults(PlayerSlot p){
  if(p==null||p.Index<3||p.Actions==null||!Controls.IsKeyboard(p.Device))return;
  // The older numeric constants were offset: Pad5 was left, NumLock was dash,
  // and Clear was cancel. Restore an explicit keypad layout before saved binds.
  var a=p.Actions;Bind(a.left,Key.Pad4);Bind(a.right,Key.Pad6);Bind(a.up,Key.Pad8);Bind(a.down,Key.Pad5);
  Bind(a.jump,Key.Pad1);Bind(a.attack,Key.Pad7);Bind(a.cast,Key.Pad2);Bind(a.focus,Key.PadEnter);Bind(a.quickCast,Key.PadMinus);
  Bind(a.dreamNail,Key.Pad3);Bind(a.dash,Key.Pad9);Bind(a.superDash,Key.Pad0);Bind(a.pause,Key.F11);Bind(a.openInventory,Key.PadPlus);Bind(a.quickMap,Key.PadMultiply);
  Bind(a.menuSubmit,Key.PadPeriod);Bind(a.menuCancel,Key.PadDivide);Bind(a.textSpeedup,Key.PadPeriod);Bind(a.skipCutscene,Key.PadDivide);Bind(a.paneLeft,Key.Pad4);Bind(a.paneRight,Key.PadPeriod);
 }
 internal static void RepairMenus(PlayerSlot p){
  if(p==null||p.Index<3||p.Actions==null||!Controls.IsKeyboard(p.Device))return;
  foreach(var action in new[]{p.Actions.menuCancel,p.Actions.skipCutscene})if(action.Bindings.Count==1&&action.Bindings[0] is KeyBindingSource source&&source.Control.IncludeCount==1&&source.Control.ExcludeCount==0&&source.Control.GetInclude(0)==Key.Clear)Bind(action,Key.PadDivide);
 }
 static void Bind(PlayerAction action,Key key){action.ClearBindings();action.AddBinding(new KeyBindingSource(key));}
 internal static void Uninstall(){if(!installed)return;installed=false;On.InControl.UnityKeyboardProvider.GetKeyIsPressed-=Read;On.InControl.UnityKeyboardProvider.AnyKeyIsPressed-=Any;On.InControl.UnityKeyboardProvider.Update-=Update;if(hook!=IntPtr.Zero){RemoveHook(hook);hook=IntPtr.Zero;}Clear();}
 static void Clear(){physicalReturn=false;Array.Clear(physical,0,18);Array.Clear(held,0,18);Array.Clear(pressed,0,18);Array.Clear(navigation,0,11);Array.Clear(observedNavigation,0,11);frame=-1;}
 // Scan codes stay the same with NumLock off; extended navigation keys belong
 // to the main keyboard. Keypad Enter is likewise distinct from Return.
 internal static int PadFromScan(int scan,bool extended){if(extended)return scan==0x35?11:scan==0x1C?15:scan==0x45?10:-1;switch(scan){case 0x52:return 0;case 0x4F:return 1;case 0x50:return 2;case 0x51:return 3;case 0x4B:return 4;case 0x4C:return 5;case 0x4D:return 6;case 0x47:return 7;case 0x48:return 8;case 0x49:return 9;case 0x45:return 10;case 0x37:return 12;case 0x4A:return 13;case 0x4E:return 14;case 0x53:return 16;case 0x59:return 17;default:return -1;}}
 internal static void PhysicalEvent(int scan,bool extended,int vk,bool down){if(scan==0x1C&&!extended){physicalReturn=down;return;}int pad=PadFromScan(scan,extended);if(pad>=0){physical[pad]=down;return;}for(int i=0;i<navVk.Length;i++)if(vk==navVk[i]&&extended){navigation[i]=down;observedNavigation[i]=true;return;}}
 static IntPtr NativeEvent(int code,IntPtr message,IntPtr data){
  if(code>=0&&focused)try{int msg=message.ToInt32();if(msg==0x100||msg==0x104||msg==0x101||msg==0x105){var e=(KeyboardEvent)Marshal.PtrToStructure(data,typeof(KeyboardEvent));PhysicalEvent((int)e.ScanCode,(e.Flags&1)!=0,(int)e.VirtualKey,msg==0x100||msg==0x104);}}catch{} // Always pass the event on, even if a driver reports an unusual packet.
  return NextHook(hook,code,message,data);
 }
 static void Sample(){if(frame==Time.frameCount)return;frame=Time.frameCount;bool now=Application.isFocused;
  if(!now){Clear();focused=false;frame=Time.frameCount;return;}if(!focused){Clear();focused=true;frame=Time.frameCount;}
  for(int i=0;i<held.Length;i++){bool state=physical[i]||Input.GetKey(unity[i]);if(windows&&virtualKeys[i]!=0)state|=(GetAsyncKeyState(virtualKeys[i])&0x8000)!=0;pressed[i]=state&&!held[i];held[i]=state;}
 }
 static void Update(On.InControl.UnityKeyboardProvider.orig_Update orig,UnityKeyboardProvider provider){Sample();orig(provider);}
 static bool Read(On.InControl.UnityKeyboardProvider.orig_GetKeyIsPressed orig,UnityKeyboardProvider provider,Key key){Sample();if(!focused)return false;
  int pad=Array.IndexOf(keys,key);if(pad>=0)return held[pad];
  int nav=Array.IndexOf(navKeys,key);if(nav>=0){if(key==Key.Clear)return held[5]||orig(provider,key);if(hook!=IntPtr.Zero&&observedNavigation[nav])return navigation[nav];if(held[nav==10?16:nav])return false;}
  // Unity can report keypad Enter as Return. A true Return press must still work.
  if(key==Key.Return&&held[15]&&hook!=IntPtr.Zero)return physicalReturn;
  return orig(provider,key);
 }
 static bool Any(On.InControl.UnityKeyboardProvider.orig_AnyKeyIsPressed orig,UnityKeyboardProvider provider){Sample();if(!focused)return false;foreach(bool state in held)if(state)return true;return orig(provider);}
 internal static bool Held(KeyCode key){Sample();if(!focused)return false;int i=Array.IndexOf(unity,key);if(i>=0)return held[i];int nav=Array.IndexOf(navUnity,key);if(nav>=0){if(hook!=IntPtr.Zero&&observedNavigation[nav])return navigation[nav];if(held[nav==10?16:nav])return false;}if(key==KeyCode.Return&&held[15]&&hook!=IntPtr.Zero)return physicalReturn;return Input.GetKey(key);}
 internal static bool Pressed(KeyCode key){Sample();if(!focused)return false;int i=Array.IndexOf(unity,key);return i>=0?pressed[i]:Held(key)&&Input.GetKeyDown(key);}
}
}
