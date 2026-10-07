using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace KO.HollowKnight8 {
internal static class CoopBath {
 sealed class Region {internal Collider2D Collider;internal int Amount=1;internal float Interval=.1f;}
 static readonly List<Region> regions=new List<Region>();
 static readonly float[] nextGain=new float[8];
 static readonly PlayerSlot[] occupants=new PlayerSlot[8];
 static int scene=-1,mask;
 static float nextScan;
 internal static void Reset(){regions.Clear();System.Array.Clear(nextGain,0,8);System.Array.Clear(occupants,0,8);scene=-1;mask=0;nextScan=0;}
 static bool Active(CoopSession s){var gm=GameManager.instance;return s!=null&&s.Active&&s.Gameplay&&gm&&!gm.isPaused&&!gm.IsLoadingSceneTransition&&gm.HasFinishedEnteringScene&&Time.timeScale>0;}
 static bool Named(string name){name=name.ToLowerInvariant();return name=="spa"||name.StartsWith("spa ")||name.StartsWith("spa_")||name.Contains("hot spring")||name.Contains("hotspring")||name.Contains("bath");}
 static void Add(Collider2D c,int amount,float interval){if(!c||!c.isTrigger||c.GetComponentInParent<HeroController>()||c.GetComponentInParent<HealthManager>()||c.GetComponentInParent<DamageHero>())return;
  foreach(var r in regions)if(r.Collider==c){if(amount>0){r.Amount=amount;r.Interval=interval;}return;}
  regions.Add(new Region{Collider=c,Amount=amount>0?amount:1,Interval=interval});
 }
 static void Register(Fsm f,int amount){if(f==null||!f.GameObject||RoleSystem.Session.Resolve(f)!=null)return;
  float interval=.1f;
  // Use the native refill state's delay when available, including modded baths.
  if(f.States!=null)foreach(var state in f.States){bool refill=false;if(state.Actions==null)continue;foreach(var a in state.Actions){var call=a as CallMethodProper;if(call!=null&&call.methodName!=null&&call.methodName.Value=="TryAddMPChargeSpa")refill=true;}
   if(refill)foreach(var a in state.Actions){var wait=a as Wait;if(wait!=null&&wait.time!=null&&wait.time.Value>=.02f&&wait.time.Value<=1f)interval=wait.time.Value;}}
  var cs=f.GameObject.GetComponentsInChildren<Collider2D>(true);bool found=false;foreach(var c in cs)if(c.isTrigger){Add(c,amount,interval);found=true;}
  if(!found&&f.GameObject.transform.parent)foreach(var c in f.GameObject.transform.parent.GetComponents<Collider2D>())Add(c,amount,interval);
 }
 static void Scan(){
  int nowScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;if(scene!=nowScene){Reset();scene=nowScene;}
  if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+3f;
  regions.RemoveAll(r=>!r.Collider);
  foreach(var c in Object.FindObjectsOfType<Collider2D>())if(c.isTrigger&&(Named(c.name)||(c.transform.parent&&Named(c.transform.parent.name))))Add(c,0,.1f);
  // Do not depend on a remembered enter event: native FSMs may have gone idle
  // when the first bather filled up, or that bather may already have left.
  foreach(var f in Object.FindObjectsOfType<PlayMakerFSM>()){
   if(f.Fsm==null||f.Fsm.States==null||RoleSystem.Session.Resolve(f)!=null)continue;
   bool refill=false;foreach(var state in f.Fsm.States){if(state.Actions==null)continue;foreach(var a in state.Actions){var call=a as CallMethodProper;if(call!=null&&call.methodName!=null&&call.methodName.Value=="TryAddMPChargeSpa"){refill=true;break;}}if(refill)break;}
   if(refill)Register(f.Fsm,0);
  }
 }
 static Region Inside(PlayerSlot p){
  if(p==null||!p.Alive||!p.Ready||p.SpawnPending||p.ArenaTransfer||!p.Hero||!p.Hero.gameObject.activeInHierarchy)return null;
  Vector2 pos=p.Hero.transform.position;Collider2D hero=p.Hero.GetComponent<Collider2D>();
  foreach(var r in regions){var c=r.Collider;if(!c||!c.enabled||!c.gameObject.activeInHierarchy)continue;
   // The hero's feet may overlap shallow water while its pivot is above it.
   // Collider2D queries ignore the unrelated scene Z used by sprites.
   if(c.OverlapPoint(pos))return r;
   if(hero&&hero.enabled){var d=c.Distance(hero);if(d.isOverlapped)return r;}
  }return null;
 }
 internal static bool Refill(On.HeroController.orig_TryAddMPChargeSpa orig,HeroController hero,int amount){
  var s=RoleSystem.Session;if(s==null||!s.Active)return orig(hero,amount);
  if(!Active(s))return false;
  Scan();Register(WorldRouting.Current,Mathf.Max(1,amount));
  return Pump(s);
 }
 internal static void Tick(CoopSession s){if(!Active(s))return;Scan();Pump(s);}
 static bool Pump(CoopSession s){
  bool needs=false;int currentMask=0;
  foreach(var p in s.Players){if(p.Index<0||p.Index>=8)continue;int i=p.Index;var region=Inside(p);
   if(region==null){occupants[i]=null;nextGain[i]=0;continue;}
   currentMask|=1<<i;if(occupants[i]!=p){occupants[i]=p;nextGain[i]=Time.time;}
   using(PlayerContext.Enter(p)){
    var data=s.Data;bool room=data.MPCharge<data.maxMP||(!BossSequenceController.BoundSoul&&data.MPReserve<data.MPReserveMax);needs|=room;
    if(!room||Time.time<nextGain[i])continue;
    nextGain[i]=Time.time+region.Interval;
    data.AddMPCharge(region.Amount);p.Capture(data);
   }
  }
  if(mask!=currentMask){mask=currentMask;Diagnostics.Write("SPA occupants="+mask+" regions="+regions.Count+" concurrent refill");}
  return needs;
 }
}
}
