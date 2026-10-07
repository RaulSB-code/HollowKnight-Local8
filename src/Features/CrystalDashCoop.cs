using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class CrystalDashCoop {
 sealed class Charge {
  internal PlayerSlot Player;internal HeroController Hero;internal bool Charging,Flying;
  internal int Count=1,Launched=1,LaunchCount=1;internal float LaunchAt,NextVisualScan;internal bool HitLogged;
  internal readonly float[] Linked={-100,-100,-100,-100,-100,-100,-100,-100};
  internal GameObject TrailRoot;internal ParticleSystem Trail;internal float NextSource,Emit;internal Vector3 LastPoint;internal bool HasPoint;
  internal readonly List<GameObject> External=new List<GameObject>();
  internal readonly Dictionary<SpriteRenderer,Color> FlatSprites=new Dictionary<SpriteRenderer,Color>();
  internal readonly Dictionary<tk2dBaseSprite,Color> Sprites=new Dictionary<tk2dBaseSprite,Color>();
  internal readonly Dictionary<ParticleSystem,ParticleSystem.MinMaxGradient> Particles=new Dictionary<ParticleSystem,ParticleSystem.MinMaxGradient>();
 }
 static readonly Charge[] charges=new Charge[8];static bool installed;
 static CoopSession Session {get {var r=Plugin.Self;return r?r.Session:null;}}
 internal static void Install(){if(installed)return;installed=true;On.HutongGames.PlayMaker.Fsm.ProcessEvent+=Event;On.HutongGames.PlayMaker.FsmState.OnEnter+=State;On.HealthManager.Hit+=Hit;}
 internal static void Uninstall(){if(installed){installed=false;On.HutongGames.PlayMaker.Fsm.ProcessEvent-=Event;On.HutongGames.PlayMaker.FsmState.OnEnter-=State;On.HealthManager.Hit-=Hit;}Reset();NativeDashEffects.Clear();}
 static bool Eligible(PlayerSlot p){return p!=null&&p.Index>=0&&p.Index<8&&p.Hero&&p.Alive&&p.Ready&&p.Connected&&!p.Hero.cState.dead&&!p.InputBlocked&&!p.ArenaTransfer&&!p.Reviving&&!p.Vitals.AtBench&&!TransitionVote.Holding(p)&&!EmergencyWarp.Active(p);}
 static Charge For(PlayerSlot p){var c=charges[p.Index];if(c!=null&&(c.Player!=p||c.Hero!=p.Hero)){Restore(c);c=null;}if(c==null)charges[p.Index]=c=new Charge{Player=p,Hero=p.Hero};return c;}
 // Source FSM identity survives a broadcast; proximity/current controller does not.
 internal static bool BlockEvent(Fsm target,FsmEvent evt,FsmEventData data){
  var s=Session;if(s==null||!s.Active||target==null||evt==null)return false;
  var receiver=s.Resolve(target);if(receiver==null)return false;
  Fsm sender=data==null?null:data.SentByFsm;var actor=(sender==null?null:s.Resolve(sender))??PlayerContext.Current;
  bool dash=sender!=null&&(sender.Name=="Superdash"||(sender.GameObject&&CrystalDashRules.Crystal(sender.GameObject.name)));
  if(!dash)dash=CrystalDashRules.DashEvent(evt.Name);
  return dash&&actor!=null&&receiver!=actor;
 }
 static void Event(On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent orig,Fsm target,FsmEvent evt,FsmEventData data){if(!BlockEvent(target,evt,data))orig(target,evt,data);}
 static void State(On.HutongGames.PlayMaker.FsmState.orig_OnEnter orig,FsmState state){
  var s=Session;var p=s!=null&&s.Active&&state!=null?s.Resolve(state.Fsm):null;
  if(Eligible(p)&&p.Hero.superDash&&state.Fsm==p.Hero.superDash.Fsm){
   var c=For(p);
   if(CrystalDashRules.Charging(state.Name)){if(!c.Charging&&!c.Flying)ClearLinks(c);c.Charging=true;NativeDashEffects.Charge(p);Refresh();}
   else if(CrystalDashRules.Launch(state.Name)){Refresh();c.LaunchCount=c.Count;c.Launched=CrystalDashRules.Multiplier(c.Count);c.Charging=false;c.Flying=true;c.HitLogged=false;c.LaunchAt=Time.time;NativeDashEffects.Flight(p);Diagnostics.Write("CRYSTAL DASH P"+(p.Index+1)+" linked="+c.Count+" damage=x"+CrystalDashRules.Factor(c.Launched));}
   else if(CrystalDashRules.Stopped(state.Name)){c.Charging=false;if(!c.Flying){c.Count=1;Restore(c);}}
  }
  orig(state);
  if(Eligible(p)&&p.Hero.superDash&&state.Fsm==p.Hero.superDash.Fsm){
   // OnEnter may synchronously advance the FSM. A returning Inactive/Regain
   // Control callback must not hide the new flight that started inside it.
   string current=p.Hero.superDash.ActiveStateName;
   if(CrystalDashRules.Stopped(current)&&!p.Hero.cState.superDashing)NativeDashEffects.Stop(p);
   else if(p.Hero.cState.superDashing)NativeDashEffects.Flight(p);
   else if(CrystalDashRules.Charging(current))NativeDashEffects.Charge(p);
  }
 }
 static void ClearLinks(Charge c){for(int i=0;i<8;i++)c.Linked[i]=-100;c.Count=c.Launched=c.LaunchCount=1;c.HitLogged=false;}
 static bool Allies(PlayerSlot a,PlayerSlot b){return Local8Mod.Settings.PvpMode!=2||!PvpMatch.Running||!PvpCombat.Opponents(a,b);}
 static bool Valid(Charge c){return c!=null&&c.Hero&&c.Player!=null&&c.Hero==c.Player.Hero&&Eligible(c.Player);}
 static bool Nearby(Charge a,Charge b){Vector2 from=a.Hero.transform.position,to=b.Hero.transform.position;return (to-from).sqrMagnitude<=CrystalDashRules.Radius*CrystalDashRules.Radius&&!Physics2D.Linecast(from,to,1<<8).collider;}
 static void Refresh(){
  for(int i=0;i<8;i++){var c=charges[i];if(!Valid(c)||!c.Charging)continue;int count=1;
   for(int j=0;j<8;j++){var other=charges[j];if(j==i||!Valid(other)||!Allies(c.Player,other.Player))continue;
    if(other.Charging&&Nearby(c,other)){c.Linked[j]=Time.time;count++;}
    else if(other.Flying&&Time.time-other.LaunchAt<=CrystalDashRules.LaunchGrace&&Time.time-c.Linked[j]<=CrystalDashRules.LaunchGrace)count++;
   }c.Count=count;
  }
 }
 internal static void Tick(){
  var s=Session;var gm=GameManager.instance;if(s==null||!s.Active){Reset();return;}
  if(gm&&gm.isPaused)return;
  if(!s.Gameplay||!gm||gm.IsLoadingSceneTransition){Reset();return;}
  for(int i=0;i<8;i++){var p=s.Players.Find(x=>x.Index==i);var c=charges[i];if(!Eligible(p)){if(c!=null){Restore(c);charges[i]=null;}continue;}
   c=For(p);string state=p.Hero.superDash?p.Hero.superDash.ActiveStateName:"";bool flying=p.Hero.cState.superDashing;
   // Latch from charge entry to launch/cancellation. The native FSM has
   // intermediate direction/charged states, not just Ground/Wall Charge.
   bool charging=!flying&&!CrystalDashRules.Stopped(state)&&(c.Charging||CrystalDashRules.Charging(state));
   if(charging&&!c.Charging&&!c.Flying){ClearLinks(c);NativeDashEffects.Charge(p);}
   if(flying&&!c.Flying){Refresh();c.LaunchCount=c.Count;c.Launched=CrystalDashRules.Multiplier(c.Count);c.LaunchAt=Time.time;NativeDashEffects.Flight(p);}
   if(!charging&&!flying&&(c.Flying||c.Charging)){Restore(c);ClearLinks(c);NativeDashEffects.Stop(p);}
   c.Charging=charging;c.Flying=flying;if(!charging&&!flying){Restore(c);c.Count=1;}
  }
  NativeDashEffects.Tick(s);Refresh();
  foreach(var c in charges)if(c!=null&&(c.Charging||c.Flying)){int n=c.Flying?c.LaunchCount:c.Count;if(n>1)Tint(c,n);else Restore(c);}
 }
 static Color TintColor(int count){if(count==2)return new Color(.12f,1f,1f,1f);if(count==3)return new Color(.95f,.25f,1f,1f);if(count==4)return new Color(1f,.65f,.08f,1f);return Color.HSVToRGB(Mathf.Repeat(Time.time*.18f+count*.1f,1f),.55f,1f);}
 internal static void Spawned(GameObject obj){
  var s=Session;if(!obj||s==null||!s.Active||!(CrystalDashRules.CrystalArt(obj.name)||CrystalDashRules.Trail(obj.name)))return;
  var p=s.Resolve(obj);if(!Eligible(p)||obj==p.Hero.gameObject)return;
  var c=For(p);c.External.RemoveAll(x=>!x);
  if(!c.External.Contains(obj)&&c.External.Count<32)c.External.Add(obj);
  c.NextVisualScan=0;
 }
 static IEnumerable<GameObject> Artwork(Charge c){
  yield return c.Hero.gameObject;
  var s=Session;
  foreach(var obj in c.External)if(obj&&s!=null&&s.Resolve(obj)==c.Player)yield return obj;
 }
 static void Tint(Charge c,int count){
  Color tint=TintColor(count);if(Time.time>=c.NextVisualScan){c.NextVisualScan=Time.time+.25f;var body=c.Hero.GetComponent<tk2dBaseSprite>();
  foreach(var root in Artwork(c))foreach(var sprite in root.GetComponentsInChildren<tk2dBaseSprite>(true)){if(!sprite||sprite==body||!CrystalVisual(sprite.transform,c.Hero.transform)||!Small(sprite.GetComponent<Renderer>()))continue;
   if(!c.Sprites.ContainsKey(sprite))c.Sprites[sprite]=sprite.color;
  }
  foreach(var root in Artwork(c))foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)){if(!sprite||!CrystalVisual(sprite.transform,c.Hero.transform)||!Small(sprite))continue;if(!c.FlatSprites.ContainsKey(sprite))c.FlatSprites[sprite]=sprite.color;}
  foreach(var root in Artwork(c))foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){if(!ps||!CrystalVisual(ps.transform,c.Hero.transform)||!Small(ps.GetComponent<Renderer>()))continue;var main=ps.main;
   if(!c.Particles.ContainsKey(ps))c.Particles[ps]=main.startColor;
   CrystalParticleDensity.Apply(ps);
  }
  }
  foreach(var pair in c.Sprites)if(pair.Key){var original=pair.Value;pair.Key.color=new Color(tint.r,tint.g,tint.b,original.a);}
  foreach(var pair in c.FlatSprites)if(pair.Key)pair.Key.color=new Color(tint.r,tint.g,tint.b,pair.Value.a);
  foreach(var pair in c.Particles)if(pair.Key){var main=pair.Key.main;main.startColor=tint;}
  Trail(c,tint,count);
 }
 static bool Small(Renderer r)=>!r||(r.bounds.size.x<=6f&&r.bounds.size.y<=6f);
 static bool CrystalVisual(Transform t,Transform hero){
  string name=CrystalDashRules.Name(t.name);if(name.Contains("light")||name.Contains("flash")||name.Contains("screen")||name.Contains("mask")||name.Contains("fade")||name.Contains("burst"))return false;
  for(;t&&t!=hero;t=t.parent)if(CrystalDashRules.CrystalArt(t.name))return true;return false;
 }
 static bool NativeParticleSource(Transform t,Transform hero){for(;t&&t!=hero;t=t.parent)if(CrystalDashRules.Trail(t.name)||CrystalDashRules.CrystalArt(t.name))return true;return false;}
 // Reuse the game's particle atlas/material without copying its FSMs, damage,
 // lights or audio. One bounded cosmetic emitter per linked player.
 static void Trail(Charge c,Color tint,int count){
  if(!c.Trail&&Time.time>=c.NextSource){c.NextSource=Time.time+1f;ParticleSystem original=RoleAura.FindFocusParticles(c.Hero);
   if(!original)foreach(var ps in c.Hero.GetComponentsInChildren<ParticleSystem>(true)){var r=ps.GetComponent<ParticleSystemRenderer>();if(r&&r.sharedMaterial&&NativeParticleSource(ps.transform,c.Hero.transform)){original=ps;break;}}
   if(!original)return;
   c.TrailRoot=new GameObject("Local8 Linked Crystal Motes P"+(c.Player.Index+1));c.TrailRoot.layer=c.Hero.gameObject.layer;c.Trail=c.TrailRoot.AddComponent<ParticleSystem>();c.Trail.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=c.Trail.main;main.playOnAwake=false;main.loop=true;main.maxParticles=40;main.startLifetime=new ParticleSystem.MinMaxCurve(.2f,.4f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.12f);main.startColor=tint;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
   var emission=c.Trail.emission;emission.enabled=false;var shape=c.Trail.shape;shape.enabled=false;
   var color=c.Trail.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.55f,0),new GradientAlphaKey(.3f,.4f),new GradientAlphaKey(0,1)});color.color=gradient;
   var source=original.GetComponent<ParticleSystemRenderer>();var renderer=c.Trail.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=source.sharedMaterials;renderer.renderMode=ParticleSystemRenderMode.Billboard;
   Renderer body=c.Hero.GetComponent<Renderer>();if(body){renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+1;}
   var sheet=original.textureSheetAnimation;if(sheet.enabled){var copy=c.Trail.textureSheetAnimation;copy.enabled=true;copy.mode=sheet.mode;copy.numTilesX=sheet.numTilesX;copy.numTilesY=sheet.numTilesY;copy.animation=sheet.animation;copy.frameOverTime=sheet.frameOverTime;copy.startFrame=sheet.startFrame;copy.cycleCount=sheet.cycleCount;}
   c.Trail.Play();c.Emit=1f;
  }
  if(!c.Trail)return;var point=c.Hero.transform.position+new Vector3(0,.15f,-.1f);c.TrailRoot.transform.position=point;
  if(!c.HasPoint||(point-c.LastPoint).sqrMagnitude>144)c.LastPoint=point;
  c.Emit+=Mathf.Min(Time.deltaTime,.05f)*(c.Flying?Mathf.Min(48,22+count*4):7);int n=Mathf.Min(4,Mathf.FloorToInt(c.Emit));c.Emit-=n;
  for(int i=0;i<n;i++){var ep=new ParticleSystem.EmitParams();ep.position=Vector3.Lerp(c.LastPoint,point,(i+1f)/n)+new Vector3(UnityEngine.Random.Range(-.35f,.35f),UnityEngine.Random.Range(-.35f,.35f),0);ep.velocity=new Vector3(UnityEngine.Random.Range(-.5f,.5f),UnityEngine.Random.Range(-.25f,.65f),0);ep.startColor=tint;c.Trail.Emit(ep,1);}
  c.LastPoint=point;c.HasPoint=true;
 }
 static void Restore(Charge c){if(c.TrailRoot){c.TrailRoot.SetActive(false);UnityEngine.Object.Destroy(c.TrailRoot);}c.TrailRoot=null;c.Trail=null;c.Emit=0;c.HasPoint=false;c.NextSource=0;foreach(var pair in c.FlatSprites)if(pair.Key)pair.Key.color=pair.Value;c.FlatSprites.Clear();foreach(var pair in c.Sprites)if(pair.Key)pair.Key.color=pair.Value;foreach(var pair in c.Particles)if(pair.Key){var main=pair.Key.main;main.startColor=pair.Value;CrystalParticleDensity.Restore(pair.Key);}c.Sprites.Clear();c.Particles.Clear();c.NextVisualScan=0;}
 internal static int Multiplier(GameObject source){
  var s=Session;if(s==null||!s.Active||!source)return 1;var p=s.Resolve(source);if(!Eligible(p)||!p.Hero.cState.superDashing)return 1;
  var c=charges[p.Index];if(c==null||c.Player!=p||c.Hero!=p.Hero||!c.Flying)return 1;
  // Only the native crystal-dash hitbox receives the bonus, never a nail,
  // spell, familiar or sharp-shadow hit occurring during the same flight.
  for(var t=source.transform;t&&t!=p.Hero.transform;t=t.parent)if(CrystalDashRules.Crystal(t.name))return c.Launched;
  return 1;
 }
 static void Hit(On.HealthManager.orig_Hit orig,HealthManager enemy,HitInstance hit){
  int n=Multiplier(hit.Source);var s=Session;var p=s!=null&&hit.Source?s.Resolve(hit.Source):null;
  // Some native dash actions report the knight root rather than its child
  // hitbox. Generic root contacts during this exact flight are also dash hits.
  if(n==1&&Eligible(p)&&hit.Source==p.Hero.gameObject&&hit.AttackType==AttackTypes.Generic&&p.Hero.cState.superDashing){var c=charges[p.Index];if(Valid(c)&&c.Flying)n=c.Launched;}
  int before=hit.DamageDealt;hit.DamageDealt=CrystalDashRules.Damage(before,n);
  if(n>1&&p!=null&&charges[p.Index]!=null&&!charges[p.Index].HitLogged){charges[p.Index].HitLogged=true;Diagnostics.Write("CRYSTAL HIT P"+(p.Index+1)+" base="+before+" multiplied="+hit.DamageDealt+" x"+CrystalDashRules.Factor(n));}
  orig(enemy,hit);
 }
 internal static PvpCombat.Contact PvpDamage(PvpCombat.Contact contact){if(contact.Attack!=null&&contact.Attack.Collider)contact.Damage=CrystalDashRules.Damage(contact.Damage,Multiplier(contact.Attack.Collider.gameObject));return contact;}
 internal static int Participants(PlayerSlot p){if(p==null||p.Index<0||p.Index>=8)return 1;var c=charges[p.Index];return c!=null&&c.Player==p&&c.Hero==p.Hero&&c.Flying?c.LaunchCount:1;}
 internal static void ContinueFlight(PlayerSlot p,int count){
  if(!Eligible(p)||!p.Hero.cState.superDashing)return;
  var c=For(p);c.Charging=false;c.Flying=true;c.Count=c.LaunchCount=Math.Max(1,count);c.Launched=CrystalDashRules.Multiplier(count);c.LaunchAt=Time.time;c.HitLogged=false;NativeDashEffects.Flight(p);
 }
 internal static void StopPlayer(PlayerSlot p){
  CrystalDashTransit.Cancel(p);
  if(p==null||!p.Hero)return;
  try{if(p.Hero.cState.superDashing||p.Hero.superDash&&CrystalDashRules.Charging(p.Hero.superDash.ActiveStateName))using(PlayerContext.Enter(p)){p.Hero.CancelSuperDash();p.Hero.cState.superDashing=false;}}
  catch(Exception e){Diagnostics.Throttled("CRYSTAL cancel P"+(p.Index+1),e);}
  NativeDashEffects.Stop(p);if(p.Index>=0&&p.Index<8&&charges[p.Index]!=null){Restore(charges[p.Index]);charges[p.Index]=null;}
 }
 internal static void Reset(){for(int i=0;i<8;i++){if(charges[i]!=null){NativeDashEffects.Stop(charges[i].Player);Restore(charges[i]);}charges[i]=null;}CrystalParticleDensity.Reset();}
}
}
