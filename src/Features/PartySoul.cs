using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PartySoul {
 static bool installed;
 static readonly Dictionary<Fsm,int> kinds=new Dictionary<Fsm,int>();
 static int scene=-1;
 internal static bool IndividualDeathAudio {get{return isolated>0;}}
 [ThreadStatic] static int isolated;
 static CoopSession Session=>Plugin.Self==null?null:Plugin.Self.Session;
 internal static void Install(){if(installed)return;installed=true;
  On.PlayerData.StartSoulLimiter+=Start;On.PlayerData.EndSoulLimiter+=End;On.PlayerData.TakeGeo+=TakeGeo;
  On.HutongGames.PlayMaker.FsmState.OnEnter+=Enter;
  On.HutongGames.PlayMaker.Fsm.Update+=Update;On.HutongGames.PlayMaker.Fsm.FixedUpdate+=Fixed;On.HutongGames.PlayMaker.Fsm.LateUpdate+=Late;
  On.HutongGames.PlayMaker.Fsm.OnEnable+=Enable;On.HutongGames.PlayMaker.Fsm.Start+=StartFsm;
 }
 internal static void Uninstall(){if(!installed)return;installed=false;
  On.PlayerData.StartSoulLimiter-=Start;On.PlayerData.EndSoulLimiter-=End;On.PlayerData.TakeGeo-=TakeGeo;
  On.HutongGames.PlayMaker.FsmState.OnEnter-=Enter;
  On.HutongGames.PlayMaker.Fsm.Update-=Update;On.HutongGames.PlayMaker.Fsm.FixedUpdate-=Fixed;On.HutongGames.PlayMaker.Fsm.LateUpdate-=Late;
  On.HutongGames.PlayMaker.Fsm.OnEnable-=Enable;On.HutongGames.PlayMaker.Fsm.Start-=StartFsm;Reset();
 }
 internal static void Reset(){kinds.Clear();scene=-1;BenchSave.Reset();}
 internal static void Sync(CoopSession s){
  if(s==null||!s.Active||s.Data==null||PvpArena.SkipSoulSync())return;
  if(Local8Mod.Save.SharedSoulState==0)Local8Mod.Save.SharedSoulState=s.Data.soulLimited?2:1;
  bool limited=Local8Mod.Save.SharedSoulState==2;
  int max=BossSequenceController.BoundSoul?33:limited?66:99;
  foreach(var p in s.Players){p.Vitals.SoulLimited=limited;p.Vitals.MaxSoul=max;p.Vitals.Soul=Math.Min(p.Vitals.Soul,max);}
  s.Data.soulLimited=limited;s.Data.maxMP=max;s.Data.MPCharge=Math.Min(s.Data.MPCharge,max);
 }
 static void Start(On.PlayerData.orig_StartSoulLimiter orig,PlayerData d){
  var s=Session;if(s==null||!s.Active||d!=s.Data){orig(d);return;}
  if(isolated>0||(!s.TeamWipe&&!s.AllowVanillaDeath&&Local8Mod.Save.SharedSoulState!=2)){
   Diagnostics.Write("PARTY SOUL ignored individual death limiter");return;
  }
  orig(d);Local8Mod.Save.SharedSoulState=2;Sync(s);Diagnostics.Write("PARTY SOUL broken after team defeat");
 }
 static void End(On.PlayerData.orig_EndSoulLimiter orig,PlayerData d){
  var s=Session;if(s==null||!s.Active||d!=s.Data){orig(d);return;}
  if(isolated>0)return;
  orig(d);Local8Mod.Save.SharedSoulState=1;Sync(s);Diagnostics.Write("PARTY SOUL restored by native shade recovery");
 }
 static void TakeGeo(On.PlayerData.orig_TakeGeo orig,PlayerData d,int n){if(isolated==0)orig(d,n);}
 static bool Isolate(Fsm f){
  var s=Session;if(s==null||!s.Active||f==null||!f.GameObject)return false;
  int current=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;if(scene!=current){scene=current;kinds.Clear();}
  int kind;
  if(!kinds.TryGetValue(f,out kind)){
   var go=f.GameObject;
   if(go.GetComponentInParent<LocalShade>())kind=1;
   else foreach(var p in s.Players){if(!p.Hero||!p.Hero.heroDeathPrefab)continue;var death=p.Hero.heroDeathPrefab;if(go==death||go.transform.IsChildOf(death.transform)){kind=2;break;}}
   kinds[f]=kind;
  }
  return kind==1||(kind==2&&!s.TeamWipe&&!s.AllowVanillaDeath);
 }
 sealed class Scope:IDisposable {
  readonly CoopSession s;readonly PlayerData d;
  readonly int geo,pool,hp,mp,fire,quake,scream,special,state;
  readonly string room,zone;readonly float x,y;readonly Vector3 map;
  internal Scope(){s=Session;d=s.Data;geo=d.geo;pool=d.geoPool;room=d.shadeScene;zone=d.shadeMapZone;x=d.shadePositionX;y=d.shadePositionY;hp=d.shadeHealth;mp=d.shadeMP;fire=d.shadeFireballLevel;quake=d.shadeQuakeLevel;scream=d.shadeScreamLevel;special=d.shadeSpecialType;map=d.shadeMapPos;state=Local8Mod.Save.SharedSoulState;isolated++;}
  public void Dispose(){isolated--;d.geo=geo;d.geoPool=pool;d.shadeScene=room;d.shadeMapZone=zone;d.shadePositionX=x;d.shadePositionY=y;d.shadeHealth=hp;d.shadeMP=mp;d.shadeFireballLevel=fire;d.shadeQuakeLevel=quake;d.shadeScreamLevel=scream;d.shadeSpecialType=special;d.shadeMapPos=map;Local8Mod.Save.SharedSoulState=state;Sync(s);}
 }
 static IDisposable Guard(Fsm f){if(!Isolate(f))return null;Sync(Session);return new Scope();}
 static void Enter(On.HutongGames.PlayMaker.FsmState.orig_OnEnter orig,FsmState f){using(Guard(f.Fsm))orig(f);}
 static void Update(On.HutongGames.PlayMaker.Fsm.orig_Update orig,Fsm f){using(Guard(f))orig(f);}
 static void Fixed(On.HutongGames.PlayMaker.Fsm.orig_FixedUpdate orig,Fsm f){using(Guard(f))orig(f);}
 static void Late(On.HutongGames.PlayMaker.Fsm.orig_LateUpdate orig,Fsm f){using(Guard(f))orig(f);}
 static void Enable(On.HutongGames.PlayMaker.Fsm.orig_OnEnable orig,Fsm f){using(Guard(f))orig(f);}
 static void StartFsm(On.HutongGames.PlayMaker.Fsm.orig_Start orig,Fsm f){using(Guard(f))orig(f);}
}
}
