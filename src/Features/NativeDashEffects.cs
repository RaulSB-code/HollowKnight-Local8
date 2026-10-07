using System;
using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
// Native trail renderers can survive a cancelled dash when a death/transition
// skips their own FSM exit. Hide just those renderers and clear their particles;
// leave the knight, physics, screen flashes and unrelated effects untouched.
internal static class NativeDashEffects {
 sealed class Fx {
  internal PlayerSlot Player;internal HeroController Hero;internal bool Active,Flight;
  internal readonly Dictionary<Renderer,bool> Hidden=new Dictionary<Renderer,bool>();
  internal readonly Dictionary<ParticleSystem,bool> Stopped=new Dictionary<ParticleSystem,bool>();
  internal readonly List<GameObject> External=new List<GameObject>();
  internal readonly List<Renderer> ResumedRenderers=new List<Renderer>();
  internal readonly List<ParticleSystem> ResumedParticles=new List<ParticleSystem>();
 }
 static readonly Dictionary<HeroController,Fx> effects=new Dictionary<HeroController,Fx>();
 static readonly List<HeroController> expired=new List<HeroController>();
 static bool Visual(Transform t,Transform hero){
  bool visual=false;
  for(;t&&t!=hero;t=t.parent){string n=CrystalDashRules.Name(t.name);if(n.Contains("light")||n.Contains("flash")||n.Contains("screen")||n.Contains("mask")||n.Contains("fade"))return false;if(CrystalDashRules.Trail(t.name)||CrystalDashRules.CrystalArt(t.name))visual=true;}
  return visual;
 }
 static Fx Get(PlayerSlot p){Fx fx;if(!effects.TryGetValue(p.Hero,out fx))effects[p.Hero]=fx=new Fx{Player=p,Hero=p.Hero};return fx;}
 internal static void Spawned(GameObject obj){
  var s=Plugin.Self?Plugin.Self.Session:null;if(!obj||s==null||!s.Active||!(CrystalDashRules.Trail(obj.name)||CrystalDashRules.CrystalArt(obj.name)))return;
  var p=s.Resolve(obj);if(p==null||!p.Hero||obj==p.Hero.gameObject)return;
  var fx=Get(p);if(!fx.External.Contains(obj))fx.External.Add(obj);
  // A delayed native action may spawn its pooled trail after death/travel
  // already cancelled the flight. Do not let that late spawn restore it.
  if(!p.Alive||!p.Ready||p.Hero.cState.dead||p.ArenaTransfer||TransitionVote.Holding(p))Stop(p);
 }
 internal static void Stop(PlayerSlot p){
  if(p==null||!p.Hero)return;
  var fx=Get(p);fx.Active=fx.Flight=false;
  Hide(fx,p.Hero.gameObject);
  var s=Plugin.Self?Plugin.Self.Session:null;
  foreach(var obj in fx.External)if(obj&&s!=null&&s.Resolve(obj)==p)Hide(fx,obj);
 }
 static void Hide(Fx fx,GameObject obj){
  foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))if(renderer&&Visual(renderer.transform,fx.Hero.transform)){
   if(!fx.Hidden.ContainsKey(renderer))fx.Hidden[renderer]=renderer.enabled;
   renderer.enabled=false;
  }
  foreach(var ps in obj.GetComponentsInChildren<ParticleSystem>(true))if(ps&&Visual(ps.transform,fx.Hero.transform)){
   if(!fx.Stopped.ContainsKey(ps))fx.Stopped[ps]=ps.isPlaying||ps.main.playOnAwake||ps.emission.enabled;
   // Stop each identified emitter only. Its unrelated child effects retain
   // their own FSM and particles, including excluded lights and screen flashes.
   ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
  }
 }
 static bool Crystal(Transform t,Transform hero){for(;t&&t!=hero;t=t.parent)if(CrystalDashRules.CrystalArt(t.name))return true;return false;}
 static void Rearm(PlayerSlot p,bool flight){
  if(p==null||!p.Hero)return;var fx=Get(p);fx.Active=true;fx.Flight=flight;
  var remove=fx.ResumedRenderers;remove.Clear();
  foreach(var pair in fx.Hidden){if(!pair.Key){remove.Add(pair.Key);continue;}
   var s=Plugin.Self?Plugin.Self.Session:null;if(s!=null&&s.Resolve(pair.Key.gameObject)!=p){remove.Add(pair.Key);continue;}
   if(flight||Crystal(pair.Key.transform,p.Hero.transform)){pair.Key.enabled=pair.Value;remove.Add(pair.Key);}
  }
  foreach(var renderer in remove)fx.Hidden.Remove(renderer);
  remove.Clear();var resumed=fx.ResumedParticles;resumed.Clear();var session=Plugin.Self?Plugin.Self.Session:null;
  foreach(var pair in fx.Stopped){var ps=pair.Key;
   if(!ps||session!=null&&session.Resolve(ps.gameObject)!=p){resumed.Add(ps);continue;}
   if(!flight&&!Crystal(ps.transform,p.Hero.transform)||!ps.gameObject.activeInHierarchy)continue;
   // Enabling native emission does not restart a system stopped by cleanup.
   // Resume just that emitter, preserving its native emission/color/timing.
   // Inactive prefabs remain pending until the native activation action runs.
   if(!ps.isPlaying&&(pair.Value||ps.main.playOnAwake||ps.emission.enabled)){ps.Clear(false);ps.Play(false);}
   if(ps.isPlaying)resumed.Add(ps);
  }
  foreach(var ps in resumed)fx.Stopped.Remove(ps);
  resumed.Clear();
 }
 internal static void Charge(PlayerSlot p)=>Rearm(p,false);
 internal static void Flight(PlayerSlot p)=>Rearm(p,true);
 internal static void Tick(CoopSession s){
  foreach(var pair in effects){var fx=pair.Value;
   if(!fx.Hero||fx.Player==null||fx.Player.Hero!=fx.Hero||s==null||!s.Players.Contains(fx.Player)){expired.Add(pair.Key);continue;}
   bool invalid=!fx.Player.Alive||!fx.Player.Ready||fx.Player.Hero.cState.dead||fx.Player.ArenaTransfer||TransitionVote.Holding(fx.Player);
   if(invalid){bool resumed=fx.Active;foreach(var r in fx.Hidden.Keys)if(r&&r.enabled){resumed=true;break;}if(!resumed)foreach(var ps in fx.Stopped.Keys)if(ps&&ps.isPlaying){resumed=true;break;}if(resumed)Stop(fx.Player);}
   else if(fx.Active&&fx.Stopped.Count>0)Rearm(fx.Player,fx.Flight);
  }
  foreach(var h in expired)effects.Remove(h);expired.Clear();
 }
 internal static void Clear(){
  foreach(var fx in effects.Values)if(fx.Hero&&fx.Player!=null&&fx.Player.Hero==fx.Hero){
   Stop(fx.Player);
   if(fx.Player.Alive&&!fx.Hero.cState.dead)Rearm(fx.Player,true);
  }
  effects.Clear();expired.Clear();
 }
}
}
