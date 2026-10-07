using System;
using System.Reflection;
using GlobalEnums;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class LiquidPolish {
 static bool installed;
 static readonly PropertyInfo force=typeof(HeroController).GetProperty("ForceFootstepSound",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 static readonly FieldInfo forceField=typeof(HeroController).GetField("forceFootstepSound",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 static readonly object none=NoFootsteps();
 static object NoFootsteps(){var type=force!=null?force.PropertyType:forceField==null?null:forceField.FieldType;return type!=null&&type.IsEnum&&Enum.IsDefined(type,"None")?Enum.Parse(type,"None"):null;}
 // Ordinary water retains exactly its established height. Acid sits .2 lower.
 internal static float Inset(bool acid)=>acid?.38f:.18f;
 internal static void Install(){if(installed)return;installed=true;On.HeroAudioController.PlaySound+=Sound;}
 internal static void Uninstall(){if(!installed)return;installed=false;On.HeroAudioController.PlaySound-=Sound;}
 static bool Water(AudioSource source,HeroController h)=>source&&source.clip&&source.GetComponentInParent<HeroController>()==h&&(source.clip==h.footstepsRunWater||source.clip.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0);
 static bool Surface(HeroController h){var a=h.GetComponent<tk2dSpriteAnimator>();return a&&a.CurrentClip!=null&&a.CurrentClip.name!=null&&a.CurrentClip.name.StartsWith("Surface ",StringComparison.Ordinal);}
 static bool Stale(PlayerSlot p,HeroController h){
  var s=Plugin.Self?Plugin.Self.Session:null;var gm=GameManager.instance;
  if(p==null||!h||s==null||!s.Active||!s.Gameplay||!gm||gm.isPaused||gm.IsLoadingSceneTransition)return false;
  var c=h.cState;return p.AcidAssistActive||!p.Alive||!c.onGround||c.jumping||c.doubleJumping||c.dashing||c.superDashing||c.swimming||Surface(h);
 }
 static void ClearForce(HeroController h){
  // Later native builds force surface footsteps; older ones lack this field.
  // Never change native swimming/FSM state or global audio.
  if(none==null)return;
  if(force!=null&&force.CanWrite)force.SetValue(h,none,null);else if(forceField!=null)forceField.SetValue(h,none);
 }
 static void Stop(AudioSource source,HeroController h){if(Water(source,h)&&source.isPlaying)source.Stop();}
 internal static void AfterHero(HeroController h){
  var s=Plugin.Self?Plugin.Self.Session:null;var p=s==null?null:s.Resolve(h);
  if(!Stale(p,h)||!Water(h.footStepsRunAudioSource,h)&&!Water(h.footStepsWalkAudioSource,h))return;
  ClearForce(h);Stop(h.footStepsRunAudioSource,h);Stop(h.footStepsWalkAudioSource,h);
 }
 static void Sound(On.HeroAudioController.orig_PlaySound orig,HeroAudioController audio,HeroSounds sound){
  if(audio&&(sound==HeroSounds.FOOTSTEPS_RUN||sound==HeroSounds.FOOTSTEPS_WALK)){
   var h=audio.GetComponentInParent<HeroController>();var s=Plugin.Self?Plugin.Self.Session:null;var p=s==null||!h?null:s.Resolve(h);
   var source=sound==HeroSounds.FOOTSTEPS_RUN?audio.footStepsRun:audio.footStepsWalk;
   if(h&&Stale(p,h)&&Water(source,h)){ClearForce(h);Stop(source,h);return;}
  }
  orig(audio,sound);
 }
}
}
