using System;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
internal static class DreamRescueFeedback {
 static readonly float[] held=new float[8];static readonly bool[] reported=new bool[8];
 static AudioClip fail;static AudioSource native;static float nativeVolume=1,nextSound,nextSearch;static GameObject cue;
 internal static void Before(CoopSession s,PlayerSlot[] live){
  var gm=GameManager.instance;var ui=UIManager.instance;var r=Plugin.Self;
  bool gameplay=s!=null&&s.Active&&s.Gameplay&&gm&&!gm.isPaused&&!gm.IsLoadingSceneTransition&&ui&&ui.uiState==GlobalEnums.UIState.PLAYING&&r&&!r.Panel&&!Charms.NativeMenuOpen&&!ScriptedParty.Active&&PickupCard.Owner==null&&InteractionRouter.ActivePlayer==null&&ShopMenuRouting.Buyer==null&&!StagMenuRouting.HasOwner;
  for(int i=0;i<8;i++){var p=s==null?null:s.Players.Find(x=>x.Index==i);bool key=p!=null&&ActionKeys.Held(p);
   if(!key){held[i]=0;reported[i]=false;continue;}
   if(!gameplay||p==null||!p.Hero||!p.Alive||!p.Ready||!p.Connected||p.ArenaTransfer||TransitionVote.Holding(p)||EmergencyWarp.Active(p)){held[i]=0;reported[i]=true;continue;}
   if(!reported[i])held[i]+=Mathf.Max(0,Time.unscaledDeltaTime);
  }
 }
 internal static void After(CoopSession s,PlayerSlot[] live){if(s==null||!s.Active)return;foreach(var p in s.Players){int i=p.Index;if(i<0||i>=8||reported[i]||held[i]<.35f)continue;reported[i]=true;if(!EmergencyWarp.Active(p))Denied(p);}}
 internal static void Denied(PlayerSlot p){
  if(p==null||!p.Hero||Time.unscaledTime<nextSound)return;
  nextSound=Time.unscaledTime+1.5f;DreamRescueFailureFx.Emit(p);
  try{
   if(!fail&&Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+2;Find(p);}
   if(!fail)return;
   nextSound=Time.unscaledTime+Mathf.Max(1.5f,fail.length);
   if(cue)Object.Destroy(cue);cue=new GameObject("Local8 Dream Gate Fail");var audio=cue.AddComponent<AudioSource>();
   audio.playOnAwake=false;audio.spatialBlend=0;
   // The gate clip itself is loud. Cap the absolute gain as well as reducing
   // the native action gain; a resource-only fallback must not bypass SFX mute.
   if(native)audio.outputAudioMixerGroup=native.outputAudioMixerGroup;
   float settings=audio.outputAudioMixerGroup?1f:(GameManager.instance?GameManager.instance.GetImplicitCinematicVolume():1f);
   audio.volume=Mathf.Min(.1f,Mathf.Clamp01(nativeVolume)*.15f)*Mathf.Clamp01(settings);
   audio.PlayOneShot(fail);Object.Destroy(cue,Mathf.Max(1f,fail.length+.25f));
  }catch(Exception e){Diagnostics.Throttled("DREAM rescue failure sound",e);}
 }
 static void Find(PlayerSlot p){
  native=null;nativeVolume=1;
  // Read the actual native gate-failure action/clip, without entering the
  // Dream Nail FSM or generating a synthetic approximation of its sound.
  foreach(var fsm in p.Hero.GetComponentsInChildren<PlayMakerFSM>(true)){if(fsm.FsmName!="Dream Nail")continue;
   foreach(var state in fsm.FsmStates){if(state==null||state.Actions==null)continue;foreach(var action in state.Actions){var play=action as AudioPlay;if(play==null||play.oneShotClip==null)continue;var clip=play.oneShotClip.Value as AudioClip;if(!clip||!CrystalDashRules.FailClip(clip.name))continue;
    fail=clip;GameObject go=fsm.Fsm.GetOwnerDefaultTarget(play.gameObject);native=go?go.GetComponent<AudioSource>():null;nativeVolume=play.volume!=null&&!play.volume.IsNone?play.volume.Value:(native?native.volume:1f);return;
   }}
  }
  foreach(var clip in Resources.FindObjectsOfTypeAll<AudioClip>())if(clip&&CrystalDashRules.FailClip(clip.name)){fail=clip;return;}
 }
 internal static void Reset(){for(int i=0;i<8;i++){held[i]=0;reported[i]=false;}if(cue)Object.Destroy(cue);cue=null;fail=null;native=null;nativeVolume=1;nextSound=nextSearch=0;}
}
}
