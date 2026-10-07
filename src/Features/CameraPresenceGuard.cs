using System;
using UnityEngine;
using UnityScenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
internal static class CameraPresenceGuard {
 struct View {
  internal Vector3 Center;internal float Half,Aspect;internal bool Valid;
  internal float Left {get{return Center.x-Half*Aspect+.35f;}}
  internal float Right {get{return Center.x+Half*Aspect-.35f;}}
  internal float Bottom {get{return Center.y-Half+.35f;}}
  internal float Top {get{return Center.y+Half-.35f;}}
 }
 static CoopSession current;static int scene=-1;
 static Vector3 entry;static bool entryReady;
 static PlayerSlot focus;static float focusAt,nextLog,smoothUntil;
 static View view;
 static readonly View[] leashes=new View[8];
 static readonly HeroController[] owners=new HeroController[8];
 internal static void Reset(){current=null;scene=-1;focus=null;view=default(View);entryReady=false;focusAt=nextLog=smoothUntil=0;Array.Clear(leashes,0,leashes.Length);Array.Clear(owners,0,owners.Length);RescueHintVisibility.Reset();}
 static void Scope(CoopSession s){int handle=UnityScenes.GetActiveScene().handle;if(current==s&&scene==handle)return;Reset();current=s;scene=handle;}
 // An essence cloud remains a camera participant. Its movement is leashed to
 // the rendered view, and the final shot is damped during rescue/arrival.
 internal static bool Framing(PlayerSlot p){return p!=null&&p.Alive;}
 internal static bool HintActor(PlayerSlot p){return p!=null&&p.Hero&&p.Hero.gameObject.activeInHierarchy&&p.Alive&&p.Ready&&p.Connected&&
  !p.SpawnPending&&!p.ArenaTransfer&&!p.Hazard&&!p.Hero.cState.transitioning&&!EmergencyWarp.Active(p)&&!TransitionVote.Holding(p)&&!CoopEnding.HoldsActor(p);}
 static Bounds Body(PlayerSlot p){var b=DuelGround.Body(p);b.SetMinMax(b.min+new Vector3(-.35f,-.4f,0),b.max+new Vector3(.35f,.85f,0));return b;}
 static bool Contains(View v,Bounds b){return CameraPresenceRules.Contains(v.Center.x,v.Center.y,v.Half*v.Aspect,v.Half,b.min.x,b.max.x,b.min.y,b.max.y);}
 static float Progress(PlayerSlot p){var at=p.Hero.transform.position;float x=at.x-entry.x,y=at.y-entry.y;return Mathf.Sqrt(x*x+y*y);}
 static bool Usable(View v){return v.Valid&&CameraPresenceRules.Finite(v.Center.x)&&CameraPresenceRules.Finite(v.Center.y)&&
  CameraPresenceRules.Finite(v.Half)&&CameraPresenceRules.Finite(v.Aspect)&&v.Half>1f&&v.Aspect>.1f;}
 internal static void After(CameraRig rig,CameraController ctrl,CoopSession s){
  if(rig==null||!rig.initialized||!rig.cam||!ctrl||s==null||!s.Active||!s.Gameplay||s.TeamWipe)return;
  Scope(s);var gm=GameManager.instance;
  if(!gm||gm.isPaused||gm.IsLoadingSceneTransition||ScriptedParty.Active||CoopEnding.Active||
     ctrl.mode==CameraController.CameraMode.FROZEN||ctrl.mode==CameraController.CameraMode.FADEOUT||ctrl.mode==CameraController.CameraMode.PANNING||
     ctrl.sceneWidth<=0||ctrl.sceneHeight<=0)return;
  var proposed=new View{Center=ctrl.transform.position,Half=rig.currentHalf,Aspect=rig.cam.aspect,Valid=true};
  if(!Usable(proposed))return;
  if(!entryReady&&s.HasArrival&&CameraPresenceRules.Finite(s.Arrival.x)&&CameraPresenceRules.Finite(s.Arrival.y)){entry=s.Arrival;entryReady=true;}
  if(!entryReady&&!view.Valid)entry=rig.nativePosition;
  PlayerSlot leader=null;float farthest=-1;int count=0;bool anyVisible=false,anyWarp=false;
  float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
  foreach(var p in s.Players){
   if(EmergencyWarp.Active(p))anyWarp=true;
   if(!HintActor(p))continue;
   var b=Body(p);if(!CameraPresenceRules.Finite(b.min.x)||!CameraPresenceRules.Finite(b.max.x)||!CameraPresenceRules.Finite(b.min.y)||!CameraPresenceRules.Finite(b.max.y))continue;
   count++;anyVisible|=Contains(proposed,b);minX=Mathf.Min(minX,b.min.x);maxX=Mathf.Max(maxX,b.max.x);minY=Mathf.Min(minY,b.min.y);maxY=Mathf.Max(maxY,b.max.y);
   float progress=Progress(p);if(progress>farthest+.001f){farthest=progress;leader=p;}
  }
  if(anyWarp)smoothUntil=Time.unscaledTime+.9f;
  if(Usable(view)&&(anyWarp||Time.unscaledTime<smoothUntil)){
   proposed=SmoothRescueFrame(proposed,rig,ctrl,s);
   view=proposed;RescueHintVisibility.Observe(s,view.Center,view.Half,view.Aspect);return;
  }
  if(count==0){view=proposed;RescueHintVisibility.Observe(s,view.Center,view.Half,view.Aspect);return;}
  bool split=maxX-minX>2f*proposed.Half*proposed.Aspect||maxY-minY>2f*proposed.Half;
  if(s.Players.Count>1&&(split||!anyVisible)){
   if(HintActor(focus)&&leader!=focus&&!CameraPresenceRules.ChangeLeader(farthest,Progress(focus),Time.unscaledTime-focusAt))leader=focus;
   if(focus!=leader){focus=leader;focusAt=Time.unscaledTime;nextLog=0;}
   var b=Body(leader);Vector3 fixedAt=proposed.Center;
   fixedAt.x=CameraPresenceRules.Contain(fixedAt.x,proposed.Half*proposed.Aspect,b.min.x,b.max.x,.5f,ctrl.sceneWidth-.5f);
   fixedAt.y=CameraPresenceRules.Contain(fixedAt.y,proposed.Half,b.min.y,b.max.y,.25f,ctrl.sceneHeight-.25f);
   if(Mathf.Abs(fixedAt.x-proposed.Center.x)>.001f)rig.velocity.x=0;
   if(Mathf.Abs(fixedAt.y-proposed.Center.y)>.001f)rig.velocity.y=0;
   rig.position=fixedAt;ctrl.transform.position=fixedAt;proposed.Center=fixedAt;
   if(Time.unscaledTime>=nextLog){nextLog=Time.unscaledTime+10f;Diagnostics.Write("CAMERA presence P"+(leader.Index+1)+" entry="+entry+" progress="+Progress(leader)+" split="+split+" pos="+fixedAt);}
  }else focus=null;
  view=proposed;RescueHintVisibility.Observe(s,view.Center,view.Half,view.Aspect);
 }
 static View SmoothRescueFrame(View proposed,CameraRig rig,CameraController ctrl,CoopSession s){
  float dt=Time.unscaledDeltaTime;
  // Clouds still participate in the original group framing. Their suggested
  // pan is limited around a real knight before damping, rather than letting a
  // remote cloud leave every physical body outside the rendered rectangle.
  PlayerSlot anchor=null,forward=null;float farthest=-1;
  foreach(var p in s.Players)if(HintActor(p)){
   float distance=Progress(p);if(distance>farthest){farthest=distance;forward=p;}
   if(anchor==null&&Contains(view,Body(p)))anchor=p;
  }
  if(s.Players.Contains(focus)&&HintActor(focus))anchor=focus;
  if(anchor==null)anchor=forward;
  if(anchor!=null&&ctrl.mode!=CameraController.CameraMode.LOCKED){
   var body=Body(anchor);float x=(body.min.x+body.max.x)*.5f,y=(body.min.y+body.max.y)*.5f;
   float xRoom=proposed.Half*proposed.Aspect*.28f,yRoom=proposed.Half*.24f;
   proposed.Center.x=Mathf.Clamp(proposed.Center.x,x-xRoom,x+xRoom);
   proposed.Center.y=Mathf.Clamp(proposed.Center.y,y-yRoom,y+yRoom);
  }
  proposed.Center.x=DreamRescueRules.Follow(view.Center.x,proposed.Center.x,dt,35f);
  proposed.Center.y=DreamRescueRules.Follow(view.Center.y,proposed.Center.y,dt,35f);
  proposed.Half=DreamRescueRules.Follow(view.Half,proposed.Half,dt,Mathf.Max(6f,view.Half*1.2f));
  proposed.Half=Mathf.Max(proposed.Half,view.Half-35f*Mathf.Min(.05f,Mathf.Max(0,dt))/Mathf.Max(1f,proposed.Aspect));
  if(anchor!=null){
   var b=Body(anchor);
   // This applies even when the real actor already left the previous shot.
   // Ordinary movement stays damped; a genuine sudden relocation must regain
   // visibility immediately rather than spend seconds looking at empty space.
   proposed.Center.x=CameraPresenceRules.Contain(proposed.Center.x,proposed.Half*proposed.Aspect,b.min.x,b.max.x,.5f,ctrl.sceneWidth-.5f);
   proposed.Center.y=CameraPresenceRules.Contain(proposed.Center.y,proposed.Half,b.min.y,b.max.y,.25f,ctrl.sceneHeight-.25f);
   if(focus!=anchor){focus=anchor;focusAt=Time.unscaledTime;}
  }
  rig.position=proposed.Center;rig.velocity=Vector3.zero;rig.zoomVelocity=0;rig.currentHalf=proposed.Half;rig.SetLens(proposed.Half);ctrl.transform.position=proposed.Center;
  return proposed;
 }
 internal static void BeginRescue(CoopSession s,PlayerSlot p){
  if(s==null||p==null||p.Index<0||p.Index>=8||!EmergencyWarp.Active(p))return;
  Scope(s);var captured=view;
  var rig=s.Camera;var gm=GameManager.instance;
  if(!Usable(captured)&&rig!=null&&rig.initialized&&rig.cam&&gm&&gm.cameraCtrl)
   captured=new View{Center=gm.cameraCtrl.transform.position,Half=rig.currentHalf,Aspect=rig.cam.aspect,Valid=true};
  owners[p.Index]=p.Hero;leashes[p.Index]=captured;if(Usable(captured))view=captured;smoothUntil=Time.unscaledTime+.9f;
  if(Usable(captured))Diagnostics.Write("DREAM rescue camera leash P"+(p.Index+1)+" x="+captured.Left+".."+captured.Right+" y="+captured.Bottom+".."+captured.Top);
 }
 internal static Vector3 RescueMove(Vector3 from,Vector3 delta,PlayerSlot p){
  if(p==null||p.Index<0||p.Index>=8||owners[p.Index]!=p.Hero||!Usable(leashes[p.Index]))return from+delta;
  var v=Usable(view)?view:leashes[p.Index];return new Vector3(CameraPresenceRules.Leash(from.x,delta.x,v.Left,v.Right),CameraPresenceRules.Leash(from.y,delta.y,v.Bottom,v.Top),from.z+delta.z);
 }
 internal static void EndRescue(PlayerSlot p){if(p==null||p.Index<0||p.Index>=8||owners[p.Index]!=p.Hero)return;owners[p.Index]=null;leashes[p.Index]=default(View);smoothUntil=Time.unscaledTime+.9f;}
}
}
