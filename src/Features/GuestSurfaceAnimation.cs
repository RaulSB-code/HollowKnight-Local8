using System;
using UnityEngine;
namespace KO.HollowKnight8 {
// Surface Water is a world FSM with singleton-hero side effects. Keep the
// existing isolated buoyancy and render the native surface clips locally.
internal static class GuestSurfaceAnimation {
 static readonly HeroController[] lastBody=new HeroController[8];
 static readonly string[] lastPose=new string[8];
 static tk2dSpriteAnimationClip OwnClip(tk2dSpriteAnimator animator,string name){
  var clip=animator.GetClipByName(name);
  return clip!=null&&clip.frames!=null&&clip.frames.Length>0?clip:null;
 }
 internal static bool Pose(PlayerSlot p){
  if(p==null||p.Index<=0||p.Index>=8||!p.AcidAssistActive||!p.Hero||!p.Alive||!p.Ready||!p.Connected||p.InputBlocked||p.SpawnPending||p.ArenaTransfer)return false;
  var r=Plugin.Self;var s=r?r.Session:null;var gm=GameManager.instance;
  if(s==null||!s.Active||!s.Gameplay||!gm||gm.isPaused||gm.IsLoadingSceneTransition)return false;
  var h=p.Hero;var c=h.cState;
  if(h.controlReqlinquished||Time.unscaledTime<p.WakeUntil||c.jumping||c.doubleJumping||c.dashing||c.superDashing||c.attacking||c.casting||c.recoiling||c.dead||c.transitioning||EmergencyWarp.Active(p)||TransitionVote.Holding(p)||CoopEnding.HoldsActor(p)||ScriptedParty.Holds(p)||ChallengeSequence.Holds(p)||BenchSeats.Seated(p))return false;
  var animator=h.GetComponent<tk2dSpriteAnimator>();if(!animator)return false;
  float direction=p.Actions!=null?p.Actions.right.Value-p.Actions.left.Value:0f;
  bool moving=Mathf.Abs(direction)>.1f;
  var clip=OwnClip(animator,moving?"Surface Swim":"Surface Idle")??OwnClip(animator,moving?"Surface Idle":"Surface Swim");
  // Legacy/custom libraries may supply a real Swim. Its existing native
  // animation route remains responsible; never copy P1's frames or atlas.
  if(clip==null&&OwnClip(animator,"Swim")!=null)return false;
  if(clip==null)clip=OwnClip(animator,"Idle");
  if(clip==null)return false;
  if(lastBody[p.Index]!=h||lastPose[p.Index]!=clip.name){
   lastBody[p.Index]=h;lastPose[p.Index]=clip.name;
   Diagnostics.Write("WATER own surface pose P"+(p.Index+1)+"="+clip.name);
  }
  // A stable loop keeps advancing instead of restarting on every frame.
  // Native Surface frames provide the submerged appearance at the existing
  // body-relative float height, in both ordinary water and safe acid.
  if(animator.CurrentClip!=clip)animator.Play(clip);
  return true;
 }
 internal static void Reset(){Array.Clear(lastBody,0,lastBody.Length);Array.Clear(lastPose,0,lastPose.Length);}
}
}
