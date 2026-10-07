using System;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
internal static class DreamRescueArrival {
 sealed class Flight {internal EmergencyWarp.Trip Trip;internal PlayerSlot Target;internal HeroController Hero;internal Vector3 From;internal float Elapsed,Duration;}
 static readonly Flight[] flights=new Flight[8];
 static bool Eligible(PlayerSlot q,PlayerSlot self){return q!=null&&q!=self&&q.Hero&&q.Hero.gameObject.activeInHierarchy&&q.Ready&&q.Alive&&q.Connected&&!q.SpawnPending&&!q.ArenaTransfer&&!q.Hazard&&!EmergencyWarp.Active(q)&&!q.Hero.cState.transitioning&&!TransitionVote.Holding(q)&&!CoopEnding.HoldsActor(q);}
 internal static void Begin(CoopSession s,EmergencyWarp.Trip trip){
  var p=trip.Player;if(flights[p.Index]!=null)return;
  PlayerSlot target=null;float nearest=float.MaxValue;
  foreach(var q in s.Players)if(Eligible(q,p)){float distance=((Vector2)(q.Hero.transform.position-trip.Position)).sqrMagnitude;if(distance<nearest){nearest=distance;target=q;}}
  Vector3 to;
  if(target==null||!s.NearAlly(target,p,out to)){
   if(trip.Elapsed<6f)return;
   Diagnostics.Write("DREAM rescue cancelled P"+(p.Index+1)+" no safe nearby teammate");
   EmergencyWarp.cooldown[p.Index]=Time.unscaledTime+3f;DreamRescueFeedback.Denied(p);EmergencyWarp.Cancel(p);return;
  }
  flights[p.Index]=new Flight{Trip=trip,Target=target,Hero=target.Hero,From=trip.Position,Duration=DreamRescueRules.ArrivalTime(Vector2.Distance(trip.Position,to))};
  Diagnostics.Write("DREAM rescue essence flight P"+(p.Index+1)+" to P"+(target.Index+1));
 }
 internal static bool Advance(CoopSession s,EmergencyWarp.Trip trip){
  var p=trip.Player;var flight=flights[p.Index];if(flight==null)return false;
  if(flight.Trip!=trip){Forget(p);return false;}
  var gm=GameManager.instance;
  if(gm&&(gm.isPaused||gm.IsLoadingSceneTransition)){ActorRecovery.Freeze(p);EmergencyWarp.Hide(trip);return true;}
  Vector3 to;
  if(!Eligible(flight.Target,p)||flight.Target.Hero!=flight.Hero||!s.NearAlly(flight.Target,p,out to)){
   DreamRescueFeedback.Denied(p);EmergencyWarp.cooldown[p.Index]=Time.unscaledTime+3f;EmergencyWarp.Cancel(p);return true;
  }
  to.z=(float)CoopRules.PlayerDepth(p.Index);
  float elapsed=flight.Elapsed+=Mathf.Max(0,Time.unscaledDeltaTime),t=DreamRescueRules.ArrivalProgress(elapsed,flight.Duration);
  trip.Position=Vector3.Lerp(flight.From,to,t);trip.Hero.transform.position=trip.Position;
  if(trip.Effect)trip.Effect.transform.position=trip.Position;
  ActorRecovery.Freeze(p);EmergencyWarp.Hide(trip);
  if(elapsed<flight.Duration)return true;
  // Reappear only once the essence actually reaches the verified landing.
  var effect=trip.Effect;trip.Effect=null;trip.Hero.transform.position=to;
  EmergencyWarp.Restore(trip);EmergencyWarp.trips[p.Index]=null;flights[p.Index]=null;
  if(effect){foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);Object.Destroy(effect,.65f);}
  CameraPresenceGuard.EndRescue(p);p.SafePoint=to;p.HasSafePoint=s.IsSafe(to,p);p.SafeAt=Time.unscaledTime;p.Vitals.HazardPoint=to;s.Commit(p);
  Diagnostics.Write("DREAM rescue arrived P"+(p.Index+1)+" to P"+(flight.Target.Index+1)+" pos="+to);
  return true;
 }
 internal static void Forget(PlayerSlot p){if(p!=null&&p.Index>=0&&p.Index<8){flights[p.Index]=null;CameraPresenceGuard.EndRescue(p);}}
 internal static void Reset(){Array.Clear(flights,0,flights.Length);}
}
}
