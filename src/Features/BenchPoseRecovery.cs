using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
namespace KO.HollowKnight8 {
// Native seats remain native. Correct only a retained sitting pose after
// an actor has physically left that seat, without resetting gameplay FSMs.
internal static class BenchPoseRecovery {
 sealed class Seat {internal HeroController Hero;internal Vector3 At;}
 static readonly Dictionary<PlayerSlot,Seat> native=new Dictionary<PlayerSlot,Seat>();
 static readonly List<PlayerSlot> stale=new List<PlayerSlot>();
 internal static void Observe(Fsm f,PlayerSlot p){
  if(p==null||!p.Hero||!BenchSeats.Seated(p)||BenchSeats.Custom(p)||!InteractionRouter.IsBench(f))return;
  Seat seat;if(!native.TryGetValue(p,out seat)||seat.Hero!=p.Hero)native[p]=new Seat{Hero=p.Hero,At=p.Hero.transform.position};
 }
 internal static void Released(PlayerSlot p){if(p!=null)native.Remove(p);}
 internal static void Downed(PlayerSlot p){
  if(p==null||!p.Down)return;Released(p);p.Vitals.AtBench=false;
  var s=Plugin.Self?Plugin.Self.Session:null;if(s!=null&&s.Data!=null){using(PlayerContext.Enter(p))s.Data.atBench=false;}
 }
 static bool Sitting(tk2dSpriteAnimationClip c){
  if(c==null||string.IsNullOrEmpty(c.name))return false;
  return c.name.Equals("Sit",StringComparison.OrdinalIgnoreCase)||c.name.Equals("Sit Idle",StringComparison.OrdinalIgnoreCase)||c.name.Equals("Sit Loop",StringComparison.OrdinalIgnoreCase);
 }
 internal static void Tick(){
  var r=Plugin.Self;var s=r?r.Session:null;var gm=GameManager.instance;
  if(s==null||!s.Active||!s.Gameplay||!gm||gm.isPaused||gm.IsLoadingSceneTransition||r.Panel||Charms.NativeMenuOpen||ScriptedParty.Active||CoopEnding.Active||PickupCard.Owner!=null||InteractionRouter.ActivePlayer!=null||ShopMenuRouting.MenuVisible||StagMenuRouting.HasOwner)return;
  stale.Clear();foreach(var kv in native)if(!kv.Key.Hero||kv.Key.Hero!=kv.Value.Hero||!s.Players.Contains(kv.Key)||!BenchSeats.Seated(kv.Key))stale.Add(kv.Key);
  foreach(var p in stale)native.Remove(p);stale.Clear();
  foreach(var p in s.Players){
   var h=p.Hero;if(!h||!p.Ready||!p.Connected||!p.Alive||p.Down||p.Hazard||p.Retiring||p.InputBlocked||p.SpawnPending||p.ArenaTransfer||h.cState.transitioning||h.cState.dead||h.IsDreamReturning||Time.unscaledTime<p.WakeUntil||BenchSeats.Custom(p)||EmergencyWarp.Active(p)||TransitionVote.Holding(p)||ChallengeSequence.Holds(p))continue;
   var animator=h.GetComponent<tk2dSpriteAnimator>();if(!animator||!Sitting(animator.CurrentClip))continue;
   Seat seat;bool seated=BenchSeats.Seated(p);
   if(seated){
    // Actual seat distance is separate from the larger charm-change radius.
    // Preserve native rest, wake and Get Off while the actor is on its bench.
    if(!native.TryGetValue(p,out seat)){native[p]=new Seat{Hero=h,At=h.transform.position};continue;}
    Vector3 at=h.transform.position;if(Mathf.Abs(at.x-seat.At.x)<=2.5f&&Mathf.Abs(at.y-seat.At.y)<=1.6f)continue;
   }else if(p.Vitals.AtBench||h.controlReqlinquished||!h.acceptingInput)continue;
   using(PlayerContext.Enter(p)){
    if(seated)BenchSeats.Leave(p);
    p.Vitals.AtBench=false;if(s.Data!=null)s.Data.atBench=false;
    // Repair animation only. Movement, gravity, jump/dash, invulnerability,
    // inventory, native dialogue and scene coroutines keep their own state.
    h.StartAnimationControl();
    if(animator.GetClipByName("Idle")!=null)animator.Play("Idle");
   }
   Released(p);Diagnostics.Write("BENCH stale sitting pose cleared P"+(p.Index+1));
  }
 }
 internal static void Reset(){native.Clear();stale.Clear();}
}
}
