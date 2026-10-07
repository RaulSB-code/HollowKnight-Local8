using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
// One native unsheath cue at each actor's real draw, not at the eventual
// group FINISHED event. Only that exact challenge's duplicate cue is muted.
internal static class ChallengeDrawAudio {
 static Fsm source;static AudioClip clip;static float gain=.65f;
 static readonly HashSet<PlayerSlot> played=new HashSet<PlayerSlot>();
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;
  On.HutongGames.PlayMaker.Actions.AudioPlay.OnEnter+=Audio;
  On.HutongGames.PlayMaker.Actions.AudioPlaySimple.OnEnter+=Simple;
  On.HutongGames.PlayMaker.Actions.AudioPlayerOneShotSingle.DoPlayRandomClip+=Single;
  On.HutongGames.PlayMaker.Actions.AudioPlayerOneShot.DoPlayRandomClip+=Multiple;
 }
 internal static void Uninstall(){if(!installed)return;installed=false;
  On.HutongGames.PlayMaker.Actions.AudioPlay.OnEnter-=Audio;
  On.HutongGames.PlayMaker.Actions.AudioPlaySimple.OnEnter-=Simple;
  On.HutongGames.PlayMaker.Actions.AudioPlayerOneShotSingle.DoPlayRandomClip-=Single;
  On.HutongGames.PlayMaker.Actions.AudioPlayerOneShot.DoPlayRandomClip-=Multiple;Reset();
 }
 static int Score(AudioClip candidate){
  if(!candidate||candidate.length<=.02f||candidate.length>3)return -1;
  string n=candidate.name.ToLowerInvariant();
  if(n.Contains("music")||n.Contains("roar")||n.Contains("sheath")&&!n.Contains("unsheath"))return -1;
  bool draw=n.Contains("unsheath")||n.Contains("draw")||n.Contains("nail out")||n.Contains("nail_out")||n.Contains("sword out");
  return draw?20+(n.Contains("nail")?10:0)+(n.Contains("hero")||n.Contains("knight")?5:0):n.Contains("challenge")?10:-1;
 }
 static IEnumerable<AudioClip> Clips(FsmStateAction action){
  var single=action as AudioPlayerOneShotSingle;if(single!=null){yield return single.audioClip==null?null:single.audioClip.Value as AudioClip;yield break;}
  var multiple=action as AudioPlayerOneShot;if(multiple!=null){if(multiple.audioClips!=null)foreach(var c in multiple.audioClips)yield return c;yield break;}
  var audio=action as AudioPlay;if(audio!=null){yield return audio.oneShotClip==null?null:audio.oneShotClip.Value as AudioClip;yield break;}
  var simple=action as AudioPlaySimple;if(simple!=null)yield return simple.oneShotClip==null?null:simple.oneShotClip.Value as AudioClip;
 }
 static FsmFloat Volume(FsmStateAction a)=>a is AudioPlayerOneShotSingle?((AudioPlayerOneShotSingle)a).volume:a is AudioPlayerOneShot?((AudioPlayerOneShot)a).volume:a is AudioPlay?((AudioPlay)a).volume:a is AudioPlaySimple?((AudioPlaySimple)a).volume:null;
 internal static void Begin(Fsm f,PlayerSlot owner){
  Reset();source=f;int best=-1;AudioClip unique=null;FsmFloat uniqueVolume=null;bool several=false;
  if(f!=null&&f.States!=null)foreach(var state in f.States){if(state==null||state.Actions==null)continue;
   foreach(var action in state.Actions)foreach(var c in Clips(action)){
    if(c&&c.length>.02f&&c.length<3&&c.name.IndexOf("music",StringComparison.OrdinalIgnoreCase)<0){if(unique&&unique!=c)several=true;else if(!unique){unique=c;uniqueVolume=Volume(action);}}
    int score=Score(c);if(score<0||score+100<=best)continue;
    best=score+100;clip=c;var v=Volume(action);gain=v==null||v.IsNone?.65f:Mathf.Clamp(v.Value,0,1);
   }
  }
  // The native challenge sometimes names its sole cue simply "Sword".
  // Prefer that actual serialized asset over a heuristic from other scenes.
  if(!clip&&unique&&!several){clip=unique;best=100;gain=uniqueVolume==null||uniqueVolume.IsNone?.65f:Mathf.Clamp(uniqueVolume.Value,0,1);}
  if(!clip)foreach(var c in Resources.FindObjectsOfTypeAll<AudioClip>()){int score=Score(c);if(score>best){best=score;clip=c;}}
  if(!clip)Diagnostics.Write("CHALLENGE no loaded native draw audio in "+(f==null?"none":f.Name));
  Draw(owner);
 }
 internal static void Guest(ChallengeSequence.Guest g){
  if(g!=null&&g.Played&&g.Animator&&g.Animator.CurrentClip!=null&&g.Animator.CurrentClip.name=="Challenge Start")Draw(g.Player);
 }
 static void Draw(PlayerSlot p){
  if(!clip||p==null||!p.Alive||!p.Ready||!p.Connected||!p.Hero||played.Contains(p))return;
  try{
   var cue=new GameObject("Local8 Nail Draw P"+(p.Index+1));cue.transform.position=p.Hero.transform.position;
   var audio=cue.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=gain*(GameManager.instance?Mathf.Clamp01(GameManager.instance.GetImplicitCinematicVolume()):1);
   audio.PlayOneShot(clip);Object.Destroy(cue,Mathf.Max(1,clip.length+.2f));played.Add(p);
   Diagnostics.Write("CHALLENGE draw sound P"+(p.Index+1)+" clip="+clip.name);
  }catch(Exception ex){Diagnostics.Throttled("CHALLENGE draw audio",ex);}
 }
 static bool Duplicate(FsmStateAction action){
  if(source==null||clip==null||played.Count==0||action.Fsm!=source)return false;
  foreach(var c in Clips(action))if(c==clip)return true;return false;
 }
 static void Native(FsmStateAction action,Action run){
  var volume=Volume(action);if(!Duplicate(action)||volume==null||volume.IsNone){run();return;}
  // Keep the native action, its delay, pooled audio object and FINISHED event.
  // One-shot gain zero removes just the duplicate voice, never global audio.
  float original=volume.Value;try{volume.Value=0;run();}finally{volume.Value=original;}
 }
 static void Audio(On.HutongGames.PlayMaker.Actions.AudioPlay.orig_OnEnter orig,AudioPlay a)=>Native(a,()=>orig(a));
 static void Simple(On.HutongGames.PlayMaker.Actions.AudioPlaySimple.orig_OnEnter orig,AudioPlaySimple a)=>Native(a,()=>orig(a));
 static void Single(On.HutongGames.PlayMaker.Actions.AudioPlayerOneShotSingle.orig_DoPlayRandomClip orig,AudioPlayerOneShotSingle a)=>Native(a,()=>orig(a));
 static void Multiple(On.HutongGames.PlayMaker.Actions.AudioPlayerOneShot.orig_DoPlayRandomClip orig,AudioPlayerOneShot a)=>Native(a,()=>orig(a));
 internal static void Reset(){source=null;clip=null;gain=.65f;played.Clear();}
}
}
