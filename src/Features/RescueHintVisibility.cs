using UnityEngine;
namespace KO.HollowKnight8 {
internal static class RescueHintVisibility {
 static readonly RescueHintEpisode episode=new RescueHintEpisode();static float observed=-10;static CoopSession current;
 internal static void Reset(){current=null;observed=-10;episode.Offscreen=episode.Announced=false;episode.Since=episode.ClearSince=-1;episode.Next=0;}
 internal static void Observe(CoopSession s,Vector3 center,float half,float aspect){
  if(current!=s){Reset();current=s;}
  bool outside=false;int count=0;
  if(s!=null&&!TransitionVote.ApproachingExit(s))foreach(var p in s.Players){
   if(!CameraPresenceGuard.HintActor(p))continue;
   count++;var b=DuelGround.Body(p);
   outside|=DreamRescueRules.Offscreen(center.x,center.y,half*aspect,half,b.min.x,b.max.x,b.min.y,b.max.y);
  }
  observed=Time.unscaledTime;episode.Observe(count>1&&outside,observed);
  if(!episode.Offscreen)RescueHint.HideDistant();
  Request();
 }
 internal static void Request(){
  // Old camera distance/clipping calls also pass here. Only the final rendered
  // shot may authorize a contextual message, once per prolonged split.
  var s=Plugin.Self?Plugin.Self.Session:null;var gm=GameManager.instance;
  if(s!=current||s==null||!s.Active||!s.Gameplay||!gm||gm.isPaused||gm.IsLoadingSceneTransition||Plugin.Self.Panel||Charms.NativeMenuOpen||ScriptedParty.Active||CoopEnding.Active||TransitionVote.ApproachingExit(s)||
     Time.unscaledTime-observed>.3f||!episode.Due(Time.unscaledTime)||RescueHint.queued)return;
  episode.Announce(Time.unscaledTime);RescueHint.queued=RescueHint.distant=true;RescueHint.visibleTime=0;
  Diagnostics.Write("RESCUE hint confirmed offscreen player");
 }
 internal static void BeforeHint(CoopSession s){
  var gm=GameManager.instance;
  if(RescueHint.distant&&(s!=current||!episode.Offscreen||Time.unscaledTime-observed>.3f||!gm||gm.isPaused||gm.IsLoadingSceneTransition||TransitionVote.ApproachingExit(s)))RescueHint.HideDistant();
 }
}
}
