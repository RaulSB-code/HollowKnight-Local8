using System;
using System.Reflection;
namespace KO.HollowKnight8 {
internal static class KeyboardMemory {
 static int capturing,selected;
 static readonly MethodInfo saveSettings=typeof(Modding.Mod).GetMethod("SaveGlobalSettings",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
 static void Ensure(){
  var settings=Local8Mod.Settings;
  if(settings.ExtraKeyboardBindings==null||settings.ExtraKeyboardBindings.Length!=8){
   var bindings=new string[8];if(settings.ExtraKeyboardBindings!=null)Array.Copy(settings.ExtraKeyboardBindings,bindings,Math.Min(8,settings.ExtraKeyboardBindings.Length));settings.ExtraKeyboardBindings=bindings;
  }
 }
 internal static void Restore(PlayerSlot p){
  if(p==null||p.Index<=0||p.Index>=8||p.Actions==null||!Controls.IsKeyboard(p.Device))return;
  try{Ensure();string data=Local8Mod.Settings.ExtraKeyboardBindings[p.Index];if(string.IsNullOrEmpty(data))return;
   p.Actions.Load(data);Diagnostics.Write("KEYBOARD restored P"+(p.Index+1));
  }catch(Exception e){Diagnostics.Throttled("KEYBOARD restore",e);}
 }
 internal static void BeforeCapture(){capturing=ActionKeys.Capturing;selected=Hud.selected;}
 internal static void AfterCapture(CoopSession s){
  if(capturing<10||capturing>22||ActionKeys.Capturing!=0||s==null)return;
  try{
   PlayerSlot p=null;foreach(var candidate in s.Players)if(candidate.Index==selected){p=candidate;break;}
   if(p==null||p.Index<=0||p.Index>=8||p.Actions==null||!Controls.IsKeyboard(p.Device))return;
   Ensure();string data=p.Actions.Save();if(data==Local8Mod.Settings.ExtraKeyboardBindings[p.Index])return;
   Local8Mod.Settings.ExtraKeyboardBindings[p.Index]=data;
   if(Local8Mod.Instance!=null){if(saveSettings==null)throw new MissingMethodException("Mod.SaveGlobalSettings");saveSettings.Invoke(Local8Mod.Instance,null);}
   Diagnostics.Write("KEYBOARD saved P"+(p.Index+1));
  }catch(Exception e){Diagnostics.Throttled("KEYBOARD save",e);}
 }
}
}
