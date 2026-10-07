using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityScenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
// A local elimination must not permanently replace the arena's colour grade
// or leave the save owner's full-screen death fade over a continuing round.
internal static class ArenaVisualRecovery {
 sealed class Grade {
  internal SceneColorManager Manager;
  internal Color AmbientA,AmbientB,HeroA,HeroB,Ambient;
  internal float IntensityA,IntensityB,SaturationA,SaturationB,Factor,Intensity;
  internal AnimationCurve RedA,GreenA,BlueA,RedB,GreenB,BlueB;
  internal void Capture(SceneColorManager m){Manager=m;AmbientA=m.AmbientColorA;AmbientB=m.AmbientColorB;HeroA=m.HeroLightColorA;HeroB=m.HeroLightColorB;IntensityA=m.AmbientIntensityA;IntensityB=m.AmbientIntensityB;SaturationA=m.SaturationA;SaturationB=m.SaturationB;Factor=m.Factor;RedA=m.RedA;GreenA=m.GreenA;BlueA=m.BlueA;RedB=m.RedB;GreenB=m.GreenB;BlueB=m.BlueB;Ambient=RenderSettings.ambientLight;Intensity=RenderSettings.ambientIntensity;}
  internal void Restore(){var m=Manager;if(!m)return;bool changed=!m.AmbientColorA.Equals(AmbientA)||!m.AmbientColorB.Equals(AmbientB)||!m.HeroLightColorA.Equals(HeroA)||!m.HeroLightColorB.Equals(HeroB)||m.AmbientIntensityA!=IntensityA||m.AmbientIntensityB!=IntensityB||m.SaturationA!=SaturationA||m.SaturationB!=SaturationB||m.Factor!=Factor||m.RedA!=RedA||m.GreenA!=GreenA||m.BlueA!=BlueA||m.RedB!=RedB||m.GreenB!=GreenB||m.BlueB!=BlueB;m.AmbientColorA=AmbientA;m.AmbientColorB=AmbientB;m.HeroLightColorA=HeroA;m.HeroLightColorB=HeroB;m.AmbientIntensityA=IntensityA;m.AmbientIntensityB=IntensityB;m.SaturationA=SaturationA;m.SaturationB=SaturationB;m.Factor=Factor;m.RedA=RedA;m.GreenA=GreenA;m.BlueA=BlueA;m.RedB=RedB;m.GreenB=GreenB;m.BlueB=BlueB;if(changed)m.UpdateScript(true);RenderSettings.ambientLight=Ambient;RenderSettings.ambientIntensity=Intensity;}
 }
 static Grade grade;static string room;static float captureAt,repairAt,until,next;static bool armed;
 internal static void Reset(){grade=null;room=null;captureAt=repairAt=until=next=0;armed=false;}
 internal static void Downed(PlayerSlot p){if(!PvpArena.KeepsMusic||p==null||!p.Down)return;armed=true;repairAt=Time.unscaledTime+.6f;until=Time.unscaledTime+3.5f;next=0;}
 internal static void RoundStarted(){if(armed){repairAt=Time.unscaledTime+.2f;until=Time.unscaledTime+3.5f;next=0;}}
 internal static void BeforeCamera(Camera camera){var gc=GameCameras.instance;if(!gc||(camera!=gc.mainCamera&&camera!=gc.hudCamera))return;try{TickCore(true);}catch(Exception e){Reset();Diagnostics.Throttled("PVP VISUAL render recovery",e);}}
 internal static void Tick(){
  try{TickCore();}catch(Exception e){Reset();Diagnostics.Throttled("PVP VISUAL recovery",e);}
 }
 static void TickCore(bool rendering=false){
  var gm=GameManager.instance;var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(!PvpArena.KeepsMusic||!gm||s==null||!s.Active||gm.IsLoadingSceneTransition||!gm.HasFinishedEnteringScene){Reset();return;}
  string current=UnityScenes.GetActiveScene().name;var gc=GameCameras.instance;
  if(room!=current){Reset();room=current;captureAt=Time.unscaledTime+.25f;}
  if(!gc||!gc.sceneColorManager||gm.isPaused)return;
  if(grade==null){if(armed||Time.unscaledTime<captureAt)return;grade=new Grade();grade.Capture(gc.sceneColorManager);return;}
  if(!armed||Time.unscaledTime<repairAt||(!rendering&&Time.unscaledTime<next))return;
  // A pause must not exhaust the watchdog before its first repair.
  bool final=Time.unscaledTime>until;next=Time.unscaledTime+.15f;
  grade.Restore();
  // The composite reads the real hero gradient, so restore its colour without
  // enabling cloned masks/lights or changing the multiplayer light shader.
  foreach(var p in s.Players)if(p.Hero&&p.Hero.heroLight)p.Hero.heroLight.color=Color.Lerp(grade.HeroA,grade.HeroB,grade.Factor);
  var fade=gc.cameraFadeFSM;string state=fade?fade.ActiveStateName:null;
  if(fade&&state!="Normal"&&fade.Fsm.GetState("Normal")!=null){fade.SetState("Normal");Diagnostics.Write("PVP VISUAL recovered death fade room="+room+" previous="+state);}
  // This boss-free room is static. Its lingering native death grade must not
  // reappear after the watchdog or after a late update, until the match ends.
  if(final&&room!="GG_Hollow_Knight")armed=false;
 }
}
internal static class ArenaReturnRecovery {
 static CoopSession session;static string room;static float until,next;
 static readonly HashSet<FsmState> resumed=new HashSet<FsmState>();
 internal static void Begin(CoopSession s){session=s;room=UnityScenes.GetActiveScene().name;until=Time.unscaledTime+3;next=0;resumed.Clear();CharmMenuExit.OnExit();Tick();}
 internal static void Reset(){session=null;room=null;resumed.Clear();until=next=0;}
 internal static void Tick(){
  try{TickCore();}catch(Exception e){Reset();Diagnostics.Throttled("PVP RETURN recovery",e);}
 }
 static void TickCore(){
  if(session==null)return;var gm=GameManager.instance;
  if(!gm||Plugin.Self==null||Plugin.Self.Session!=session||!session.Active||UnityScenes.GetActiveScene().name!=room||PvpArena.Active||Time.unscaledTime>until){Reset();return;}
  if(gm.IsLoadingSceneTransition||!gm.HasFinishedEnteringScene||!session.Gameplay||gm.isPaused||Time.unscaledTime<next)return;
  next=Time.unscaledTime+.25f;
  // The trip was requested in free gameplay. Its completed native entry, not
  // a saved temporary menu flag, now owns pausing again.
  if(PickupCard.Owner==null&&InteractionRouter.ActivePlayer==null&&ShopMenuRouting.Buyer==null&&!StagMenuRouting.HasOwner&&!ScriptedParty.Active&&!CoopEnding.Active&&!Charms.NativeMenuOpen)session.Data.disablePause=false;
  foreach(var f in UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>()){
   if(!f||!f.enabled||!f.gameObject.activeInHierarchy||f.gameObject.scene.name!=room||f.Fsm==null)continue;
   var state=f.Fsm.ActiveState;if(state==null||state.Actions==null||resumed.Contains(state))continue;
   foreach(var action in state.Actions){
    var wait=action as WaitForFinishedEnteringScene;if(wait==null)continue;
    // Replay only this native completed-entry action, not an ENTER/UNLOCK
    // broadcast or a hard-coded door state. Keys and statue prerequisites stay native.
    resumed.Add(state);wait.OnEnter();Diagnostics.Write("PVP RETURN resumed native arrival object="+f.gameObject.name+" fsm="+f.Fsm.Name);break;
   }
  }
 }
}
}
