using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PhysicsGuard {
 static readonly float[] since=new float[8];
 internal static void Reset(){for(int i=0;i<since.Length;i++)since[i]=0f;}
 internal static void Tick(CoopSession s){
  if(s==null||!s.Gameplay||!s.Active||GameManager.instance==null||GameManager.instance.isPaused||GameManager.instance.IsLoadingSceneTransition||TransitionVote.Pending||ScriptedParty.Active||CoopEnding.Active||Charms.NativeMenuOpen||PickupCard.Owner!=null||InteractionRouter.ActivePlayer!=null)return;
  foreach(PlayerSlot p in s.Players){
   if(p.Index<0||p.Index>=8)continue;
   HeroController h=p.Hero;
   if(!p.Ready||!p.Alive||p.InputBlocked||p.SpawnPending||p.ArenaTransfer||!h||h.cState.transitioning||h.cState.swimming||p.AcidAssistActive||h.inAcid||h.controlReqlinquished||!h.acceptingInput||EmergencyWarp.Active(p)||BenchSeats.Seated(p)){since[p.Index]=0f;continue;}
   Rigidbody2D body=h.GetComponent<Rigidbody2D>();
   if(!body||(!body.isKinematic&&body.simulated&&body.gravityScale>.01f)){since[p.Index]=0f;continue;}
   if(since[p.Index]==0f){since[p.Index]=Time.unscaledTime;continue;}
   if(Time.unscaledTime-since[p.Index]<1.4f)continue;
   since[p.Index]=0f;
   float gravity=h.DEFAULT_GRAVITY>0f?h.DEFAULT_GRAVITY:1f;
   body.simulated=true;body.isKinematic=false;body.gravityScale=gravity;
   Diagnostics.Write("PHYSICS restored gravity P"+(p.Index+1)+" at "+h.transform.position);
  }
 }
}
}
