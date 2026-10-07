using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
namespace KO.HollowKnight8 {
// The casting FSM is authoritative. Proximity and the currently active controller
// are not reliable ownership signals when several players cast in the same frame.
internal static class SpellSafety {
 [ThreadStatic] static PlayerSlot casting;
 [ThreadStatic] static float direction;
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;On.HutongGames.PlayMaker.Fsm.OnEnable+=Enable;}
 internal static void Uninstall(){if(!installed)return;installed=false;On.HutongGames.PlayMaker.Fsm.OnEnable-=Enable;casting=null;}
 static PlayerSlot Caster(Fsm f){var s=RoleSystem.Session;return s!=null&&s.Active&&f!=null&&f.Name=="Spell Control"?s.Resolve(f):null;}
 internal static void Pool(On.HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool.orig_OnEnter orig,SpawnObjectFromGlobalPool action){
  PlayerSlot p=Caster(action.Fsm);if(p==null){orig(action);return;}
  var previous=casting;float oldDirection=direction;casting=p;direction=p.Hero.cState.facingRight?1f:-1f;
  try{using(PlayerContext.Enter(p)){orig(action);if(action.storeObject!=null)Born(action.storeObject.Value,p,direction);}}
  finally{casting=previous;direction=oldDirection;}
 }
 internal static void Create(On.HutongGames.PlayMaker.Actions.CreateObject.orig_OnEnter orig,CreateObject action){
  PlayerSlot p=Caster(action.Fsm);if(p==null){orig(action);return;}
  var previous=casting;float oldDirection=direction;casting=p;direction=p.Hero.cState.facingRight?1f:-1f;
  try{using(PlayerContext.Enter(p)){orig(action);if(action.storeObject!=null)Born(action.storeObject.Value,p,direction);}}
  finally{casting=previous;direction=oldDirection;}
 }
 internal static bool Soul(GameObject obj){if(!obj||obj.GetComponentInParent<HeroController>()||obj.GetComponentInParent<HealthManager>())return false;string n=obj.name.ToLowerInvariant();return n.Contains("fireball")||n.Contains("vengeful");}
 static void Stamp(GameObject obj,PlayerSlot p){
  (obj.GetComponent<OwnerTag>()??obj.AddComponent<OwnerTag>()).Player=p;
  RoleSystem.Session.RefreshOwnership(obj,p);Hooks.RebindReferences(obj,p);
 }
 static void Enable(On.HutongGames.PlayMaker.Fsm.orig_OnEnable orig,Fsm f){
  // Pool activation runs before Spawn returns. Remove the previous borrower's
  // owner/cache before vanilla OnEnable can read HeroController.instance.
  if(casting!=null&&f!=null&&f.GameObject){GameObject root=f.GameObject;for(Transform t=root.transform;t!=null;t=t.parent)if(Soul(t.gameObject))root=t.gameObject;
   if(Soul(root)){Stamp(root,casting);using(PlayerContext.Enter(casting)){orig(f);}return;}}
  orig(f);
 }
 static void Born(GameObject obj,PlayerSlot p,float facing){
  if(!obj||p==null||!p.Hero)return;
  Hooks.SpellBorn(obj,p);
  if(!Soul(obj))return;
  Stamp(obj,p);
  (obj.GetComponent<SoulProjectileFlight>()??obj.AddComponent<SoulProjectileFlight>()).Arm(p,facing);
 }
 internal static void Spawned(GameObject obj){
  var s=RoleSystem.Session;if(!obj||s==null||!s.Active||obj.GetComponent<HeroController>())return;
  bool familiar=SummonRouting.Familiar(obj),damage=obj.GetComponentInChildren<DamageEnemies>(true);
  PlayerSlot p=casting??PlayerContext.Current;
  // Use proximity only when there is no explicit actor context at all.
  if(p==null&&damage&&!familiar&&!PlayerContext.TargetingEnemy){float nearest=9f;foreach(var q in s.Players)if(q.Alive&&q.Ready&&q.Hero){float d=(q.Hero.transform.position-obj.transform.position).sqrMagnitude;if(d<nearest){nearest=d;p=q;}}}
  if((casting==null&&PlayerContext.TargetingEnemy)||p==null||obj.GetComponentInChildren<HealthManager>(true)||(obj.GetComponentInChildren<DamageHero>(true)&&!damage)){s.RefreshOwnership(obj,null);return;}
  Stamp(obj,p);PvpCombat.Track(obj,p,true);
  if(Soul(obj)&&damage)Born(obj,p,casting==p?direction:(p.Hero.cState.facingRight?1f:-1f));
 }
}
internal sealed class SoulProjectileFlight:MonoBehaviour {
 Rigidbody2D body;
 float facing,expires;
 bool armed;
 internal void Arm(PlayerSlot p,float dir){body=GetComponent<Rigidbody2D>();facing=dir;expires=Time.time+3.5f;armed=true;Correct();}
 void OnDisable(){armed=false;}
 void FixedUpdate(){Check();}
 void LateUpdate(){Check();}
 void Check(){if(!armed)return;if(Time.time>=expires){armed=false;gameObject.SetActive(false);return;}Correct();}
 void Correct(){
  if(body){Vector2 v=body.velocity;if(Mathf.Abs(v.x)>.1f&&Mathf.Sign(v.x)!=facing)body.velocity=new Vector2(Mathf.Abs(v.x)*facing,v.y);}
  Vector3 scale=transform.localScale;if(Mathf.Abs(scale.x)>.01f&&Mathf.Sign(scale.x)!=facing){scale.x=Mathf.Abs(scale.x)*facing;transform.localScale=scale;}
 }
}
}
