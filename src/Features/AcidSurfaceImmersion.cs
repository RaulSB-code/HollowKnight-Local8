using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Bounds=UnityEngine.Bounds;
namespace KO.HollowKnight8 {
// Read surface geometry/authoring only. Never run the singleton liquid FSM.
internal static class AcidSurfaceImmersion {
 sealed class Plane {internal Transform Source;internal float Offset;}
 sealed class Reference {internal HeroController Hero;internal float Sample,Since,Offset,Applied,At;internal bool Ready,HasApplied;}
 static float scanStamp=float.NaN;
 static readonly Dictionary<int,Plane> planes=new Dictionary<int,Plane>();
 static readonly Dictionary<int,string> reported=new Dictionary<int,string>();
 static readonly Dictionary<int,Reference> references=new Dictionary<int,Reference>();
 static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
 internal static void Reset(){scanStamp=float.NaN;planes.Clear();reported.Clear();references.Clear();}
 internal static bool SamePool(Bounds surface,Bounds acid){
  float width=surface.size.x;
  float overlap=Mathf.Min(surface.max.x,acid.max.x)-Mathf.Max(surface.min.x,acid.min.x);
  return width>0&&overlap>=width*.75f&&Mathf.Abs(surface.max.y-acid.max.y)<=1.25f&&
   surface.min.y<=acid.max.y+.35f&&acid.min.y<=surface.max.y+.35f;
 }
 internal static void Classify(List<AcidSwimming.Surface> pools,float stamp){
  if(stamp==scanStamp)return;scanStamp=stamp;planes.Clear();
  // Snapshot actual acid bodies before marking the neutral surface detectors;
  // otherwise a neighboring water detector could propagate the classification.
  var acid=new List<Bounds>();
  foreach(var p in pools)if(p.Acid&&p.Collider&&p.Collider.enabled&&p.Collider.gameObject.activeInHierarchy)acid.Add(p.Collider.bounds);
  for(int i=0;i<pools.Count;i++){
   var p=pools[i];if(p.Acid||!p.Collider)continue;
   foreach(var b in acid)if(SamePool(p.Collider.bounds,b)){p.Acid=true;pools[i]=p;break;}
  }
 }
 static bool Variable(FsmFloat a,FsmFloat b)=>a!=null&&b!=null&&!a.IsNone&&!b.IsNone&&
  (ReferenceEquals(a,b)||a.UseVariable&&b.UseVariable&&!string.IsNullOrEmpty(a.Name)&&a.Name==b.Name);
 static GameObject Target(FsmOwnerDefault owner,GameObject go)=>owner==null?null:owner.OwnerOption==OwnerDefaultOption.UseOwner?go:owner.GameObject==null?null:owner.GameObject.Value;
 static bool Hero(GameObject go){
  var s=Plugin.Self?Plugin.Self.Session:null;var h=go?go.GetComponentInParent<HeroController>():null;
  var p=s==null||!h?null:s.Resolve(h);return p!=null&&p.Index==0;
 }
 static Plane Authored(Collider2D pool){
  for(var t=pool.transform;t;t=t.parent){
   if(t.GetComponent<HeroController>())break;
   foreach(var component in t.gameObject.GetComponents<PlayMakerFSM>()){
    if(!component||!AcidSwimming.WorldSurface(component.Fsm)||component.Fsm.States==null)continue;
    var actions=new List<FsmStateAction>();
    foreach(var state in component.Fsm.States)if(state.Actions!=null)foreach(var a in state.Actions)if(a!=null&&a.Enabled)actions.Add(a);
    foreach(var a in actions){
     var set=a as SetPosition;
     if(set==null||!set.everyFrame||set.space!=Space.World||set.y==null||set.y.IsNone||!Hero(Target(set.gameObject,component.gameObject)))continue;
     Transform origin=null;float offset=0;bool addKnown=false,invalid=false;
     foreach(var writer in actions){
      var get=writer as GetPosition;
      if(get!=null&&Variable(get.y,set.y)){
       var go=Target(get.gameObject,component.gameObject);
       var shape=go?go.GetComponent<Collider2D>():null;
       if(get.space!=Space.World||!go||go.GetComponentInParent<HeroController>()||
          !AcidSwimming.WorldSurface(go)||!shape||!SamePool(pool.bounds,shape.bounds)||origin){invalid=true;break;}
       origin=go.transform;
      }
      var add=writer as FloatAdd;
      if(add!=null&&Variable(add.floatVariable,set.y)){
       if(add.perSecond||add.everyFrame||add.add==null||add.add.IsNone||add.add.UseVariable||!Finite(add.add.Value)||Mathf.Abs(add.add.Value)>2||addKnown){invalid=true;break;}
       offset=add.add.Value;addKnown=true;
      }
      // Other arithmetic/assignments make the static expression ambiguous.
      var assign=writer as SetFloatValue;var op=writer as FloatOperator;
      if(assign!=null&&Variable(assign.floatVariable,set.y)||op!=null&&Variable(op.storeResult,set.y)){invalid=true;break;}
     }
     if(!invalid&&origin)return new Plane{Source=origin,Offset=offset};
    }
    // Surface controllers live on the detector or its immediate parent.
   }
   if(t!=pool.transform)break;
  }
  return null;
 }
 static bool NativeReference(Collider2D pool,float top,float fallback,out float target){
  target=0;int id=pool.GetInstanceID();Reference reference;references.TryGetValue(id,out reference);
  var s=Plugin.Self?Plugin.Self.Session:null;var gm=GameManager.instance;var p=s==null?null:s.Primary;
  var h=p==null?null:p.Hero;var c=h?h.cState:null;var body=h?h.GetComponent<Rigidbody2D>():null;
  var animator=h?h.GetComponent<tk2dSpriteAnimator>():null;
  var b=pool.bounds;var at=h?h.transform.position:new Vector3();float offset=at.y-top;
  bool valid=s!=null&&s.Active&&s.Gameplay&&gm&&!gm.isPaused&&!gm.IsLoadingSceneTransition&&
   p!=null&&p.Index==0&&p.Alive&&p.Ready&&p.Connected&&!p.SpawnPending&&!p.ArenaTransfer&&
   !EmergencyWarp.Active(p)&&!TransitionVote.Holding(p)&&!CoopEnding.HoldsActor(p)&&!ScriptedParty.Holds(p)&&!ChallengeSequence.Holds(p)&&!BenchSeats.Seated(p)&&
   h&&!h.controlReqlinquished&&c.swimming&&!c.dead&&!c.transitioning&&!c.jumping&&!c.doubleJumping&&
   !c.dashing&&!c.superDashing&&!c.recoiling&&!c.casting&&body&&Mathf.Abs(body.velocity.y)<.25f&&
   animator&&animator.CurrentClip!=null&&animator.CurrentClip.name!=null&&
   animator.CurrentClip.name.StartsWith("Surface ",StringComparison.Ordinal)&&
   at.x>=b.min.x+.15f&&at.x<=b.max.x-.15f&&Finite(offset)&&offset>=-.8f&&offset<=1.1f;
  if(valid){
   if(reference==null){reference=new Reference();references[id]=reference;}
   if(reference.Hero!=h||Mathf.Abs(reference.Sample-offset)>.035f){reference.Hero=h;reference.Sample=offset;reference.Since=Time.unscaledTime;}
   else if(Time.unscaledTime-reference.Since>=.2f){reference.Offset=offset;reference.Ready=true;}
  }else if(reference!=null)reference.Hero=null;
  // Read only a proven, steady native swim in this exact pool. Store a relative
  // offset so P1 can leave, die or move to another pool without dragging guests.
  if(reference==null||!reference.Ready)return false;
  // Keep the existing acquisition/retention band. A sudden lower target can
  // eject a guest from it, so resolve the calibration at a bounded rate once
  // per rendered time, independently of guest count and repeated Inset calls.
  if(!reference.HasApplied){reference.Applied=fallback-top;reference.At=Time.unscaledTime;reference.HasApplied=true;}
  float elapsed=Mathf.Clamp(Time.unscaledTime-reference.At,0,.1f);reference.At=Time.unscaledTime;
  reference.Applied=Mathf.MoveTowards(reference.Applied,reference.Offset,elapsed*.8f);
  target=top+reference.Applied;return true;
 }
 internal static float Inset(bool acid,Collider2D pool,float feet){
  if(!acid||!pool)return .18f; // Ordinary water remains instruction-equivalent.
  float top=pool.bounds.max.y;
  // Surface clips already hide the lower body. Adding its complete foot
  // offset again left the knight suspended above the liquid (1.39 in the log).
  float target=top+.60f;string source="surface-pivot";
  int id=pool.GetInstanceID();Plane plane;
  if(!planes.TryGetValue(id,out plane)){plane=Authored(pool);planes[id]=plane;}
  if(plane!=null&&plane.Source){
   float native=plane.Source.position.y+plane.Offset;
   if(Finite(native)&&native>=top-.8f&&native<=top+1.1f){target=native;source="native-authoring";}
  }
  float observed;if(NativeReference(pool,top,target,out observed)){target=observed;source="native-reference";}
  string before;
  if(!reported.TryGetValue(id,out before)||before!=source){reported[id]=source;Diagnostics.Write("WATER acid plane "+pool.name+" source="+source+" top="+top+" target="+target+" feet="+feet);}
  return top+feet-target;
 }
}
}
