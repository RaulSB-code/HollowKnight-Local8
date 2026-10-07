using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpFamiliars {
 static readonly FieldInfo targetField=typeof(KnightHatchling).GetField("target",BindingFlags.Instance|BindingFlags.NonPublic);
 static bool installed,retargeting,rebinding;static float nextDiscovery;
 static readonly Dictionary<KnightHatchling,GameObject> borrowed=new Dictionary<KnightHatchling,GameObject>();
 static readonly HashSet<KnightHatchling> hatchlings=new HashSet<KnightHatchling>();
 static readonly List<KnightHatchling> expired=new List<KnightHatchling>();
 static readonly Dictionary<Component,GameObject> targets=new Dictionary<Component,GameObject>();
 static readonly Dictionary<int,float> nextSwing=new Dictionary<int,float>(),shieldBroken=new Dictionary<int,float>();
 static readonly Dictionary<int,PvpCombat.Attack> shieldOriginal=new Dictionary<int,PvpCombat.Attack>();
 static readonly Dictionary<int,PvpKind> kinds=new Dictionary<int,PvpKind>();
 internal static bool Duel {get{return Local8Mod.Settings.PvpMode==2&&PvpMatch.Running&&PvpCharms.Allowed;}}
 static bool Hunting {get{var s=PvpCombat.Session;var gm=GameManager.instance;return Duel&&Local8Mod.Settings.PvpCharmAttacks&&PvpMatch.CanFight&&PvpCombat.Enabled&&s!=null&&s.Active&&s.Gameplay&&!s.TeamWipe&&gm&&!gm.isPaused&&!Plugin.Self.Panel;}}
 static bool Owner(Component component,out PlayerSlot p){string kind;RoleSummons.Resolve(component?component.gameObject:null,out p,out kind);return p!=null&&kind!=null;}
 static bool Combatant(PlayerSlot p){return p!=null&&p.Hero&&p.Alive&&p.Ready&&p.Connected&&!p.InputBlocked&&!p.ArenaTransfer&&!p.Vitals.AtBench&&Time.time>=p.ProtectionUntil&&!p.Hero.cState.transitioning;}
 static bool Visible(Component detector,PlayerSlot owner,PlayerSlot p,Collider2D area){
  if(!Combatant(p)||!PvpCombat.Opponents(owner,p))return false;
  var at=p.Hero.transform.position;if((at-detector.transform.position).sqrMagnitude>144)return false;
  if(area&&area.enabled&&area.gameObject.activeInHierarchy&&!area.OverlapPoint(at))return false;
  return !Physics2D.Linecast(detector.transform.position,at,1<<8).collider;
 }
 static GameObject Target(Component detector,PlayerSlot owner){
  if(!Hunting||!detector||!Combatant(owner))return null;
  var s=PvpCombat.Session;var area=detector.GetComponent<Collider2D>();GameObject old;
  // Keep an acquired opponent while valid. A different hero ticking this frame
  // or two opponents crossing must not redirect a familiar toward its owner.
  if(targets.TryGetValue(detector,out old)&&old){var p=s.Resolve(old);if(Visible(detector,owner,p,area))return old;}
  GameObject result=null;float nearest=float.MaxValue;
  foreach(var p in s.Players){if(!Visible(detector,owner,p,area))continue;float d=(p.Hero.transform.position-detector.transform.position).sqrMagnitude;if(d>=nearest)continue;nearest=d;result=p.Hero.gameObject;}
  targets[detector]=result;return result;
 }
 static GameObject GrimmTarget(On.GrimmEnemyRange.orig_GetTarget orig,GrimmEnemyRange detector){PlayerSlot p;return Duel&&Owner(detector,out p)?Target(detector,p):orig(detector);}
 static bool GrimmRange(On.GrimmEnemyRange.orig_IsEnemyInRange orig,GrimmEnemyRange detector){PlayerSlot p;return Duel&&Owner(detector,out p)?Target(detector,p)!=null:orig(detector);}
 static GameObject WeaverTarget(On.WeaverlingEnemyList.orig_GetTarget orig,WeaverlingEnemyList detector){PlayerSlot p;return Duel&&Owner(detector,out p)?Target(detector,p):orig(detector);}
 static void Hatch(On.KnightHatchling.orig_FixedUpdate orig,KnightHatchling hatch){PlayerSlot p;if(!Duel||!Owner(hatch,out p)){orig(hatch);return;}
  hatchlings.Add(hatch);var state=hatch.CurrentState;
  if(state==KnightHatchling.State.Follow||state==KnightHatchling.State.Attack){var target=Target(hatch.enemyRange?hatch.enemyRange:hatch,p);
   // Native Attack disables the sensor. Keep chasing, but validate the actual
   // rival each fixed tick and never retain a world target from before PvP.
   Reflect.Set(hatch,"target",target);borrowed[hatch]=target;
   if(target&&state==KnightHatchling.State.Follow)Reflect.Set(hatch,"currentState",KnightHatchling.State.Attack);
  }
  using(PlayerContext.Enter(p))orig(hatch);
 }
 sealed class OwnedExplosion:IEnumerator,IDisposable {
  readonly PlayerSlot owner;readonly IEnumerator inner;
  internal OwnedExplosion(PlayerSlot p,IEnumerator r){owner=p;inner=r;}
  public object Current {get{var child=inner.Current as IEnumerator;return child==null?inner.Current:new OwnedExplosion(owner,child);}}
  public bool MoveNext(){if(owner==null||!owner.Hero)return false;using(PlayerContext.Enter(owner))using(RoleSummons.EnterOwned(owner,"hatchling"))return inner.MoveNext();}
  public void Reset(){throw new NotSupportedException();}
  public void Dispose(){using(PlayerContext.Enter(owner))using(RoleSummons.EnterOwned(owner,"hatchling")){var d=inner as IDisposable;if(d!=null)d.Dispose();}}
 }
 static IEnumerator Explode(On.KnightHatchling.orig_Explode orig,KnightHatchling hatch){PlayerSlot p;var inner=orig(hatch);return Duel&&Owner(hatch,out p)?new OwnedExplosion(p,inner):inner;}
 // Retarget/RebindObject normally map ALL references to a cloned hero. PvP
 // familiar enemy variables must keep their chosen rival, while Hero/Owner
 // references still use the existing native per-player mapping.
 static bool EnemyVariable(FsmGameObject v){if(v==null)return false;string n=(v.Name??"").ToLowerInvariant();return !n.Contains("hero")&&!n.Contains("owner")&&!n.Contains("player")&&(n.Contains("enemy")||n.Contains("target")||n.Contains("opponent"));}
 static void Remember(Dictionary<FsmGameObject,GameObject> saved,FsmGameObject v,PlayerSlot owner){if(!EnemyVariable(v)||!v.Value)return;var rival=PvpCombat.Session.Resolve(v.Value);if(rival!=null&&PvpCombat.Opponents(owner,rival))saved[v]=v.Value;}
 static void RestoreReferences(Dictionary<FsmGameObject,GameObject> saved){foreach(var pair in saved)pair.Key.Value=pair.Value;}
 internal static bool Retarget(Fsm fsm,PlayerSlot requested){PlayerSlot owner;if(retargeting||!Duel||fsm==null||!Owner(fsm.GameObject?fsm.GameObject.transform:null,out owner))return false;
  var saved=new Dictionary<FsmGameObject,GameObject>();if(fsm.Variables!=null)foreach(var v in fsm.Variables.GameObjectVariables)Remember(saved,v,owner);
  retargeting=true;try{Hooks.Retarget(fsm,owner);}finally{retargeting=false;RestoreReferences(saved);}return true;
 }
 internal static bool Rebind(object action,PlayerSlot requested){var a=action as FsmStateAction;PlayerSlot owner;if(rebinding||!Duel||a==null||a.Fsm==null||!Owner(a.Fsm.GameObject?a.Fsm.GameObject.transform:null,out owner))return false;
  var saved=new Dictionary<FsmGameObject,GameObject>();foreach(var f in Hooks.GetReferenceFields(action.GetType())){var v=f.GetValue(action) as FsmGameObject;if(v!=null)Remember(saved,v,owner);var target=f.GetValue(action) as FsmOwnerDefault;if(target!=null&&target.GameObject!=null)Remember(saved,target.GameObject,owner);}
  rebinding=true;try{Hooks.RebindObject(action,owner);}finally{rebinding=false;RestoreReferences(saved);}return true;
 }
 internal static PlayerSlot Canonical(GameObject root,PlayerSlot requested){if(!Duel||!root)return requested;PlayerSlot owner;string kind;RoleSummons.Resolve(root,out owner,out kind);if(kind!=null&&owner!=null){if(PvpCombat.Session.Resolve(root)!=owner){(root.GetComponent<OwnerTag>()??root.AddComponent<OwnerTag>()).Player=owner;PvpCombat.Session.RefreshOwnership(root,owner);}return owner;}return requested;}
 internal static bool TrackFsm(Fsm fsm){if(!Duel||fsm==null||!fsm.GameObject)return false;PlayerSlot owner;string kind;RoleSummons.Resolve(fsm.GameObject,out owner,out kind);if(kind==null)return false;
  // Bypass the old Current-before-Resolve attribution for every familiar FSM.
  if(owner!=null)PvpCombat.Track(fsm.GameObject,owner,false);return true;
 }
 internal static void Tracked(GameObject root,PlayerSlot requested,bool reset){if(!Duel||!root)return;var owner=Canonical(root,requested);if(owner==null)return;
  foreach(var f in root.GetComponentsInChildren<PlayMakerFSM>(true)){if(!f||f.FsmName!="Shield Hit")continue;PlayerSlot p;string kind;RoleSummons.Resolve(f.gameObject,out p,out kind);if(p!=owner||kind!="shield")continue;
   // Dreamshield's collider is controlled by Shield Hit, not damages_enemy.
   foreach(var col in f.GetComponents<Collider2D>()){int id=col.GetInstanceID();PvpCombat.Attack a;if(!shieldOriginal.ContainsKey(id)){PvpCombat.Attack previous;PvpCombat.tracked.TryGetValue(id,out previous);shieldOriginal[id]=previous;a=new PvpCombat.Attack();PvpCombat.tracked[id]=a;reset=true;}else if(!PvpCombat.tracked.TryGetValue(id,out a)){a=new PvpCombat.Attack();PvpCombat.tracked[id]=a;reset=true;}
    if(a.Owner!=owner)reset=true;a.Collider=col;a.Owner=owner;a.Fsm=f;a.Slash=null;a.Damage=null;if(!kinds.ContainsKey(id))kinds[id]=a.Kind;a.Kind=PvpKind.Charm;if(reset){a.Swing.Reset();a.WasActive=false;shieldBroken.Remove(id);}
   }
  }
 }
 internal static bool Active(bool vanilla,PvpCombat.Attack a){if(!Duel||a==null||!a.Collider)return vanilla;
  PlayerSlot owner;string kind;RoleSummons.Resolve(a.Collider.gameObject,out owner,out kind);if(kind==null)return vanilla;if(owner!=a.Owner)return false;
  if(!a.Collider.enabled||!a.Collider.gameObject.activeInHierarchy)return false;
  var hatch=a.Collider.GetComponentInParent<KnightHatchling>();if(hatch&&hatch.CurrentState!=KnightHatchling.State.Attack)return false;
  if(kind!="shield"||!a.Fsm||a.Fsm.FsmName!="Shield Hit")return vanilla;
  float until;if(shieldBroken.TryGetValue(a.Collider.GetInstanceID(),out until)&&Time.time<until)return false;
  string state=(a.Fsm.ActiveStateName??"").ToLowerInvariant();if(state.Contains("break")||state.Contains("reform")||state.Contains("disappear")||state.Contains("destroy")||state.Contains("init"))return false;
  var r=a.Collider.GetComponent<Renderer>();return !r||r.enabled;
 }
 internal static void Install(){if(installed)return;installed=true;On.GrimmEnemyRange.GetTarget+=GrimmTarget;On.GrimmEnemyRange.IsEnemyInRange+=GrimmRange;On.WeaverlingEnemyList.GetTarget+=WeaverTarget;On.KnightHatchling.FixedUpdate+=Hatch;On.KnightHatchling.Explode+=Explode;}
 internal static void Uninstall(){Reset();if(!installed)return;installed=false;On.GrimmEnemyRange.GetTarget-=GrimmTarget;On.GrimmEnemyRange.IsEnemyInRange-=GrimmRange;On.WeaverlingEnemyList.GetTarget-=WeaverTarget;On.KnightHatchling.FixedUpdate-=Hatch;On.KnightHatchling.Explode-=Explode;}
 internal static void Reset(){foreach(var kv in borrowed)if(kv.Key&&targetField!=null&&(GameObject)targetField.GetValue(kv.Key)==kv.Value){Reflect.Set(kv.Key,"target",null);if(kv.Key.CurrentState==KnightHatchling.State.Attack)Reflect.Set(kv.Key,"currentState",KnightHatchling.State.Follow);}borrowed.Clear();targets.Clear();hatchlings.Clear();shieldBroken.Clear();nextSwing.Clear();nextDiscovery=0;
  foreach(var a in PvpCombat.tracked.Values)if(a.Collider&&kinds.TryGetValue(a.Collider.GetInstanceID(),out var kind))a.Kind=kind;kinds.Clear();
  foreach(var pair in shieldOriginal){var a=pair.Value;if(a!=null&&a.Collider&&PvpCombat.Session!=null&&PvpCombat.Session.Resolve(a.Collider)==a.Owner)PvpCombat.tracked[pair.Key]=a;else PvpCombat.tracked.Remove(pair.Key);}shieldOriginal.Clear();
 }
 internal static void Round(){Reset();}
 static void NailHatchlings(){expired.Clear();foreach(var hatch in hatchlings){if(!hatch||!hatch.gameObject.activeInHierarchy){expired.Add(hatch);continue;}if(hatch.CurrentState!=KnightHatchling.State.Follow&&hatch.CurrentState!=KnightHatchling.State.Attack)continue;
   PlayerSlot owner;if(!Owner(hatch,out owner)||!Combatant(owner))continue;var body=hatch.GetComponent<Collider2D>();if(!body||!body.enabled)continue;
   foreach(var a in PvpCombat.tracked.Values){if(!a.Collider||!a.Active||a.Swing.Parried||(a.Kind!=PvpKind.Nail&&a.Kind!=PvpKind.Art)||!Combatant(a.Owner)||!PvpCombat.Opponents(a.Owner,owner)||PvpCombat.Session.Resolve(a.Collider)!=a.Owner||!PvpCombat.Overlap(a.Collider,body))continue;
    if(Physics2D.Linecast(a.Collider.transform.position,hatch.transform.position,1<<8).collider)continue;
    using(PlayerContext.Enter(owner))hatch.FsmHitLanded();Diagnostics.Write("PVP HATCH destroyed by P"+(a.Owner.Index+1)+" owner=P"+(owner.Index+1));break;
   }
  }foreach(var h in expired){hatchlings.Remove(h);borrowed.Remove(h);}
 }
 internal static void BeforeCombat(){
  if(Local8Mod.Settings.PvpMode!=2||!PvpMatch.Running)return;
  if(Duel&&Time.time>=nextDiscovery){nextDiscovery=Time.time+.75f;
   // A shield can predate the duel and therefore have no spawn event during it.
   foreach(var tag in UnityEngine.Object.FindObjectsOfType<OwnerTag>()){if(!tag||!tag.gameObject.activeInHierarchy)continue;PlayerSlot p;string kind;RoleSummons.Resolve(tag.gameObject,out p,out kind);if(p!=null&&kind=="shield")Tracked(tag.gameObject,p,false);}
  }
  foreach(var a in PvpCombat.tracked.Values){if(!a.Collider||a.Owner==null)continue;PlayerSlot owner;string kind;RoleSummons.Resolve(a.Collider.gameObject,out owner,out kind);if(owner==null||kind==null)continue;
   // Explicit spawn owner is authoritative even for detached pooled attacks.
   a.Owner=Canonical(a.Collider.gameObject,owner);if(!PvpCharms.Allowed){a.Swing.Parried=true;a.WasActive=a.Active;continue;}
   int id=a.Collider.GetInstanceID();if(!kinds.ContainsKey(id))kinds[id]=a.Kind;a.Kind=PvpKind.Charm;
   if(Hunting&&Time.time>=a.Owner.ProtectionUntil){float next;if(!nextSwing.TryGetValue(id,out next)||Time.time>=next){a.Swing.Reset();nextSwing[id]=Time.time+1f;}}
  }
  if(Hunting)NailHatchlings();
 }
 internal static void Hit(Collider2D source,PlayerSlot owner){if(!Duel||!source||owner==null)return;string kind;PlayerSlot attributed;RoleSummons.Resolve(source.gameObject,out attributed,out kind);if(attributed!=owner||kind==null)return;
  using(PlayerContext.Enter(owner)){var ball=source.GetComponentInParent<GrimmballControl>();if(ball){ball.DoHit();return;}var hatch=source.GetComponentInParent<KnightHatchling>();if(hatch){if(hatch.CurrentState==KnightHatchling.State.Attack)hatch.FsmHitLanded();return;}
   foreach(var fsm in source.GetComponents<PlayMakerFSM>()){if(kind=="shield"&&fsm.FsmName=="Shield Hit"){
     shieldBroken[source.GetInstanceID()]=Time.time+2f;
     // Enter the native break/reform sequence, including its sound and effect.
     var state=fsm.Fsm.GetState("Break");if(state!=null)fsm.SetState("Break");
    }else if(fsm.FsmName=="damages_enemy")fsm.SendEvent("HIT LANDED");}
  }
 }
}
}
