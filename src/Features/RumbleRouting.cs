using InControl;
using UnityEngine;
namespace KO.HollowKnight8 {
// Vanilla mixes every hero's emissions into one stream and follows InputManager.ActiveDevice.
// Keep a mixer per slot, then send each stream to that slot's assigned controller.
internal static class RumbleRouting {
 static readonly GamepadVibrationMixer[] mixers=new GamepadVibrationMixer[8];
 static readonly InputDevice[] devices=new InputDevice[8];
 static readonly bool[] vibrating=new bool[8];
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;On.VibrationManager.PlayVibrationClipOneShot+=Play;On.PlatformVibrationHelper.UpdateVibration+=Update;On.VibrationManager.StopAllVibration+=StopAllHook;On.VibrationManager.StopAllVibrationsWithTag+=StopTagHook;}
 internal static void Uninstall(){if(!installed)return;On.VibrationManager.PlayVibrationClipOneShot-=Play;On.PlatformVibrationHelper.UpdateVibration-=Update;On.VibrationManager.StopAllVibration-=StopAllHook;On.VibrationManager.StopAllVibrationsWithTag-=StopTagHook;installed=false;StopAll();}
 static VibrationEmission Play(On.VibrationManager.orig_PlayVibrationClipOneShot orig,VibrationData data,System.Nullable<VibrationTarget> target,bool loop,string tag){
  if(VibrationManager.IsMuted)return orig(data,target,loop,tag);
  VibrationMixer mixer=Mixer();if(mixer==null||mixer==VibrationManager.GetMixer())return orig(data,target,loop,tag);
  return mixer.PlayEmission(data,target??new VibrationTarget((VibrationMotors)3),loop,tag);
 }
 static void Update(On.PlatformVibrationHelper.orig_UpdateVibration orig,PlatformVibrationHelper helper){orig(helper);Tick();}
 static void StopAllHook(On.VibrationManager.orig_StopAllVibration orig){orig();StopAll();}
 static void StopTagHook(On.VibrationManager.orig_StopAllVibrationsWithTag orig,string tag){orig(tag);StopTag(tag);}
 internal static VibrationMixer Mixer(){
  Local8Runtime runtime=Plugin.Self;
  CoopSession s=runtime==null?null:runtime.Session;
  PlayerSlot p=PlayerContext.Current;
  if(s==null||!s.Active||p==null||p.Index<0||p.Index>=8||!p.Connected||!p.Ready||p.Device==null||p.Device==InputDevice.Null||!p.Device.IsAttached)return VibrationManager.GetMixer();
  if(mixers[p.Index]==null)mixers[p.Index]=new GamepadVibrationMixer(GamepadVibrationMixer.PlatformAdjustments.None);
  if(devices[p.Index]!=p.Device){Stop(p.Index);devices[p.Index]=p.Device;}
  return mixers[p.Index];
 }
 internal static void Tick(){
  Local8Runtime runtime=Plugin.Self;CoopSession s=runtime==null?null:runtime.Session;
  for(int i=0;i<8;i++){
   GamepadVibrationMixer mixer=mixers[i];if(mixer==null)continue;
   PlayerSlot p=s==null?null:s.Players.Find(x=>x.Index==i);
   if(s==null||!s.Active||p==null||!p.Connected||p.Device==null||p.Device==InputDevice.Null||!p.Device.IsAttached||devices[i]!=p.Device){Stop(i);continue;}
   mixer.Update(Time.deltaTime);
   var values=mixer.CurrentValues;
   bool active=values.Small>.001f||values.Large>.001f;
   if(active){devices[i].Vibrate(values.Small,values.Large);vibrating[i]=true;}
   else if(vibrating[i]){devices[i].StopVibration();vibrating[i]=false;}
  }
 }
 static void Stop(int i){if(devices[i]!=null&&vibrating[i])devices[i].StopVibration();vibrating[i]=false;if(mixers[i]!=null)mixers[i].StopAllEmissions();devices[i]=null;}
 internal static void StopAll(){for(int i=0;i<8;i++)Stop(i);}
 internal static void StopTag(string tag){for(int i=0;i<8;i++)if(mixers[i]!=null)mixers[i].StopAllEmissionsWithTag(tag);}
}
}
