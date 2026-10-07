using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
namespace KO.HollowKnight8 {
// Carries the existing per-player owner across detached and pooled familiar projectiles.
// Does not spawn, share, destroy, or change the charms of any familiar.
internal sealed class RoleSummonTag:MonoBehaviour {internal PlayerSlot Owner;internal string Kind;}
internal static class RoleSummons {
 [ThreadStatic] static PlayerSlot spawningOwner;
 [ThreadStatic] static string spawningKind;
 internal struct Scope:IDisposable {
  PlayerSlot old;string kind;
  internal Scope(Fsm fsm){old=spawningOwner;kind=spawningKind;spawningOwner=null;spawningKind=null;
   var s=RoleSystem.Session;if(s==null||!s.Active||fsm==null)return;
   // Almost every world/enemy FSM is unowned. Use the existing per-frame cache
   // before searching any hierarchy, important in large Godhome rooms.
   if(s.Resolve(fsm)==null)return;
   string k;PlayerSlot p;Resolve(fsm.GameObject,out p,out k);
   if(k!=null&&p!=null){spawningOwner=p;spawningKind=k;}
  }
  internal Scope(PlayerSlot owner,string suppliedKind){old=spawningOwner;kind=spawningKind;spawningOwner=owner;spawningKind=suppliedKind;}
  public void Dispose(){spawningOwner=old;spawningKind=kind;}
 }
 internal static Scope EnterOwned(PlayerSlot owner,string kind){return new Scope(owner,kind);}
 internal static Scope Enter(Fsm fsm){return new Scope(fsm);}
 internal static void Resolve(GameObject source,out PlayerSlot owner,out string kind){
  var s=RoleSystem.Session;owner=null;kind=null;if(s==null||!s.Active||!source)return;
  // Prefer explicit live spawn attribution over proximity or the currently ticking hero.
  var tag=source.GetComponentInParent<RoleSummonTag>();
  if(tag&&tag.Owner!=null&&!tag.Owner.Retiring&&tag.Kind!=null){owner=tag.Owner;kind=tag.Kind;return;}
  kind=SummonRouting.DamageKind(source);owner=s.Resolve(source);
  if(owner==null&&kind!=null&&spawningOwner!=null){owner=spawningOwner;kind=spawningKind;}
 }
 internal static void Install(){
  On.HutongGames.PlayMaker.Fsm.Update+=Update;
  On.HutongGames.PlayMaker.Fsm.FixedUpdate+=Fixed;
  On.HutongGames.PlayMaker.Fsm.LateUpdate+=Late;
  On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion+=Spawn;
  On.HutongGames.PlayMaker.Actions.CreateObject.OnEnter+=Create;
 }
 internal static void Uninstall(){
  On.HutongGames.PlayMaker.Fsm.Update-=Update;
  On.HutongGames.PlayMaker.Fsm.FixedUpdate-=Fixed;
  On.HutongGames.PlayMaker.Fsm.LateUpdate-=Late;
  On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion-=Spawn;
  On.HutongGames.PlayMaker.Actions.CreateObject.OnEnter-=Create;
  spawningOwner=null;spawningKind=null;
 }
 static void Update(On.HutongGames.PlayMaker.Fsm.orig_Update orig,Fsm f){using(Enter(f))orig(f);}
 static void Fixed(On.HutongGames.PlayMaker.Fsm.orig_FixedUpdate orig,Fsm f){using(Enter(f))orig(f);}
 static void Late(On.HutongGames.PlayMaker.Fsm.orig_LateUpdate orig,Fsm f){using(Enter(f))orig(f);}
 static GameObject Spawn(On.ObjectPool.orig_Spawn_GameObject_Transform_Vector3_Quaternion orig,GameObject prefab,Transform parent,Vector3 position,Quaternion rotation){
  var owner=spawningOwner;string kind=spawningKind;
  GameObject result=orig(prefab,parent,position,rotation);Tag(result,owner,kind);return result;
 }
 static void Create(On.HutongGames.PlayMaker.Actions.CreateObject.orig_OnEnter orig,CreateObject action){
  using(Enter(action.Fsm)){var owner=spawningOwner;string kind=spawningKind;orig(action);
   if(action.storeObject!=null)Tag(action.storeObject.Value,owner,kind);
  }
 }
 static void Tag(GameObject obj,PlayerSlot owner,string kind){
  if(!obj)return;var tag=obj.GetComponent<RoleSummonTag>();
  // Pool reuse must remove old role attribution even when a normal spell reuses the object.
  if(tag){tag.Owner=null;tag.Kind=null;}
  var s=RoleSystem.Session;
  if(s==null||!s.Active||owner==null||owner.Retiring||kind==null||obj.GetComponent<HeroController>()||obj.GetComponentInChildren<HealthManager>(true))return;
  if(!obj.GetComponentInChildren<DamageEnemies>(true)&&SummonRouting.DamageKind(obj)==null)return;
  if(!tag)tag=obj.AddComponent<RoleSummonTag>();tag.Owner=owner;tag.Kind=kind;
  (obj.GetComponent<OwnerTag>()??obj.AddComponent<OwnerTag>()).Player=owner;
  s.RefreshOwnership(obj,owner);
  PvpCombat.Track(obj,owner,true);
 }
}
}
