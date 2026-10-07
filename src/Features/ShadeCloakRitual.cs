using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using GlobalEnums;
using Scenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
// The real collector keeps the native pickup, card and scene scripts. Other
// living actors follow cosmetically; no ability FSM or save flag is copied.
internal static class ShadeCloakRitual {
 sealed class Actor {
  internal PlayerSlot Player;internal HeroController Hero;internal Vector3 At;
  internal Rigidbody2D Body;internal bool Simulated,Kinematic;internal float Gravity;
  internal HeroAnimationController Animation;internal bool AnimationEnabled;
  internal tk2dSpriteAnimator Animator;internal string Pose;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip> Completed;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip,int> Triggered;
  internal readonly Dictionary<tk2dBaseSprite,Color> Sprites=new Dictionary<tk2dBaseSprite,Color>();
  internal readonly Dictionary<SpriteRenderer,Color> Skins=new Dictionary<SpriteRenderer,Color>();
  internal DarkEssenceFx Effect;
 }
 static readonly List<Actor> actors=new List<Actor>();
 static CoopSession session;static PlayerSlot collector;static HeroController primary;static Fsm source;static GameObject card;
 static DarkEssenceFx nativeEffect;
 static readonly Dictionary<Fsm,string> kinds=new Dictionary<Fsm,string>();
 sealed class Probe {internal FsmState[] States;internal float Next;}
 static readonly Dictionary<Fsm,Probe> unrecognized=new Dictionary<Fsm,Probe>();
 static readonly Dictionary<Fsm,PlayerSlot> touching=new Dictionary<Fsm,PlayerSlot>();
 static readonly HashSet<Fsm> touched=new HashSet<Fsm>(),reported=new HashSet<Fsm>();
 static readonly Dictionary<PlayerSlot,float> screaming=new Dictionary<PlayerSlot,float>();
 static bool active,cardSeen,nativeDone;static float elapsed,clearFor;static int scene;static string ritualKind;
 internal static bool Active=>active;
 static string Kind(Fsm f){
  if(f==null||!f.GameObject)return null;
  string kind;if(kinds.TryGetValue(f,out kind))return kind;
  // An actor's ordinary Spell Control can contain an upgrade branch too.
  // It becomes a ritual source only at the real level-change call below,
  // never merely because a heal/teleport elsewhere takes control.
  if(f.GameObject.GetComponentInParent<HeroController>())return null;
  Probe probe;if(unrecognized.TryGetValue(f,out probe)&&probe.States==f.States&&Time.unscaledTime<probe.Next)return null;
  if(f.Name=="Get Shadow Dash"&&Scenes.GetActiveScene().name=="Abyss_10")kind="Shade Cloak";
  else if(f.Name=="Scream Get"&&f.GameObject.name=="Scream 2 Get"&&Scenes.GetActiveScene().name=="Abyss_12")kind="Abyss Shriek";
  else if(f.States!=null)foreach(var state in f.States){if(state==null||state.Actions==null)continue;
   foreach(var action in state.Actions){
    kind=Acquisition(action);if(kind!=null)break;
   }if(kind!=null)break;
  }
  // States/actions may not yet be deserialized at the first trigger callback.
  // Cache positive identities only; a premature null must not poison the room.
  if(kind!=null){kinds[f]=kind;unrecognized.Remove(f);}
  else if(f.States!=null&&f.States.Length>0)unrecognized[f]=new Probe{States=f.States,Next=Time.unscaledTime+.5f};
  return kind;
 }
 static string Acquisition(FsmStateAction action){
  var flag=action as SetPlayerDataBool;
  if(flag!=null&&flag.boolName!=null&&flag.boolName.Value=="hasShadowDash"&&flag.value!=null&&flag.value.Value)return "Shade Cloak";
  var level=action as SetPlayerDataInt;
  return level!=null&&level.intName!=null&&level.value!=null&&level.value.Value==2?Spell(level.intName.Value):null;
 }
 static bool Acquired(CoopSession s,string kind)=>kind=="Shade Cloak"?s.Data.GetBool("hasShadowDash"):s.Data.GetInt(kind=="Shade Soul"?"fireballLevel":kind=="Descending Dark"?"quakeLevel":"screamLevel")>=2;
 static PlayerSlot TouchOwner(Fsm f,PlayerSlot resolved){
  PlayerSlot p;if(f!=null&&touching.TryGetValue(f,out p)&&p.Alive&&p.Ready&&p.Connected)return p;
  return WorldRouting.Resolve(f)??resolved;
 }
 // Observe the actual acquisition collider BEFORE its native event executes.
 // This is independent of the statue's FSM name or control action type.
 internal static PlayerSlot Contact(PlayerSlot p,Fsm f,bool exit){
  if(f==null||p==null)return p;
  ShadeShriekZone.Contact(p,f,exit);
  if(exit){PlayerSlot old;if(touching.TryGetValue(f,out old)&&old==p){touching.Remove(f);touched.Remove(f);}return p;}
  var s=Plugin.Self?Plugin.Self.Session:null;if(s==null||!s.Active||s.Players.Count<2)return p;
  string kind=Kind(f);
  if(kind==null||s==null||Acquired(s,kind))return p;
  if(!active)touching[f]=p;
  // Standing in the shriek zone is permission to cast, not acquisition.
  if(kind!="Abyss Shriek"&&!touched.Contains(f)&&Begin(f,p))touched.Add(f);
  return p;
 }
 static string Spell(string key)=>key=="fireballLevel"?"Shade Soul":key=="quakeLevel"?"Descending Dark":key=="screamLevel"?"Abyss Shriek":null;
 internal static void Observe(FsmState state,PlayerSlot resolved){
  if(state==null)return;
  ShadeShriekZone.Before(state,resolved);
  TrackScream(state,resolved);
  var s=Plugin.Self?Plugin.Self.Session:null;if(s==null||!s.Active||s.Players.Count<2)return;
  // These are the native acquisition branch, not the normal Scream Antic1/2
  // or Burst 1/2. Its animations run inside each actor's Spell Control and
  // do not necessarily use a clip called "Scream 2 Get".
  if(resolved!=null&&resolved.Hero&&resolved.Hero.spellControl&&state.Fsm==resolved.Hero.spellControl.Fsm&&Scenes.GetActiveScene().name=="Abyss_12"&&
     (state.Name=="SG Antic"||state.Name=="Scream Burst 3")){
   kinds[state.Fsm]="Abyss Shriek";Begin(state.Fsm,resolved);
  }
  string kind=Kind(state.Fsm);if(kind==null)return;
  resolved=TouchOwner(state.Fsm,resolved);
  if(reported.Add(state.Fsm))Diagnostics.Write("SHADE source "+kind+" object="+state.Fsm.GameObject.name+" fsm="+state.Fsm.Name+" state="+state.Name+" actor="+(resolved==null?"none":"P"+(resolved.Index+1)));
  // Infer entry from an actual control-taking action, never from idle/check
  // states just because the statue happens to be loaded in the room.
  if(state.Actions!=null)foreach(var action in state.Actions){var call=action as CallMethodProper;
   if(Acquisition(action)!=null||call!=null&&call.methodName!=null&&(call.methodName.Value=="RelinquishControl"||call.methodName.Value=="StopAnimationControl")){Begin(state.Fsm,resolved);break;}
  }
 }
 // Source polling is vanilla, but its hero/Spell Control references must be
 // evaluated for the actual upward caster rather than the ambient P1 singleton.
 static bool ScreamState(string name)=>name=="Scream Get?"||name=="SG Antic"||name=="Scream Burst 3"||name=="Scream Antic1"||name=="Scream Antic2"||name=="Scream"||name=="Scream 2"||name=="Scream Burst 1"||name=="Scream Burst 2";
 static void TrackScream(FsmState state,PlayerSlot p){
  var s=Plugin.Self?Plugin.Self.Session:null;
  if(s==null||!s.Active||s.Players.Count<2||Scenes.GetActiveScene().name!="Abyss_12"||s.Data.GetInt("screamLevel")>=2||p==null||!p.Hero||!p.Hero.spellControl||state.Fsm!=p.Hero.spellControl.Fsm)return;
  if(ScreamState(state.Name))screaming[p]=Time.unscaledTime;else screaming.Remove(p);
 }
 static PlayerSlot Caster(Fsm f){
  var s=Plugin.Self?Plugin.Self.Session:null;
  if(s==null||!s.Active||s.Players.Count<2||Scenes.GetActiveScene().name!="Abyss_12"||s.Data.GetInt("screamLevel")>=2||Kind(f)!="Abyss Shriek")return null;
  PlayerSlot best=null;float latest=-1;
  foreach(var pair in screaming){var p=pair.Key;
   if(pair.Value<latest||Time.unscaledTime-pair.Value>1.25f||!p.Alive||!p.Ready||!p.Connected||p.Hazard||!p.Hero||!p.Hero.spellControl||!ScreamState(p.Hero.spellControl.Fsm.ActiveStateName))continue;
   best=p;latest=pair.Value;
  }
  return best;
 }
 // Capture an actor-owned native event before Hooks resolves the world FSM.
 // Unaccepted broadcasts cannot steal a source or start a cinematic.
 internal static void BeforeEvent(Fsm f,FsmEvent evt,FsmEventData data){
  var s=Plugin.Self?Plugin.Self.Session:null;
  if(active||s==null||!s.Active||s.Players.Count<2||f==null||evt==null||Kind(f)==null||Acquired(s,Kind(f)))return;
  var transitions=f.ActiveState==null?null:f.ActiveState.Transitions;bool accepted=false;
  if(transitions!=null)foreach(var t in transitions)if(t!=null&&t.EventName==evt.Name){accepted=true;break;}
  if(!accepted)return;
  var p=data!=null&&data.SentByFsm!=null?s.Resolve(data.SentByFsm):PlayerContext.Current;
  if(p==null||!p.Alive||!p.Ready||!p.Connected)return;
  touching[f]=p;
 }
 // Observe the real acquisition clip at its first native Play call. This also
 // covers upgrade animations owned by the hero's Spell Control instead of the
 // world statue, without replaying or replacing native animation callbacks.
 internal static void AnimationStarting(tk2dSpriteAnimator animator,tk2dSpriteAnimationClip clip){
  if(!animator||clip==null)return;string kind=null;
  switch(clip.name){case "Scream 2 Get":kind="Abyss Shriek";break;case "Fireball 2 Get":kind="Shade Soul";break;case "Quake 2 Get":kind="Descending Dark";break;case "Shadow Dash Get":kind="Shade Cloak";break;}
  if(kind==null&&(clip.name.StartsWith("Collect Magical",StringComparison.Ordinal)||clip.name.EndsWith(" Get",StringComparison.Ordinal)))kind=Kind(WorldRouting.Current);
  if(kind==null)return;var s=Plugin.Self?Plugin.Self.Session:null;
  var hero=animator.GetComponentInParent<HeroController>();var p=s==null?null:s.Resolve(hero);
  // A real get clip is proof even when the native action already wrote level
  // two before playing it. The active transaction handles duplicate hooks.
  if(p==null||!p.Alive||!p.Ready||!p.Connected)return;
  var f=WorldRouting.Current;
  if(f==null||Kind(f)!=kind){f=hero.spellControl?hero.spellControl.Fsm:null;if(f==null)return;kinds[f]=kind;}
  Begin(f,p);
 }
 internal static void ObserveCall(Fsm f,PlayerSlot p,string method){
  if(method=="RelinquishControl"||method=="StopAnimationControl")Begin(f,p);
  if(active&&(f==source||Kind(f)==ritualKind)&&p==collector&&method=="RegainControl")nativeDone=true;
 }
 internal static void Card(Fsm f,GameObject created){
  if(!created)return;
  if(!active)Begin(f,PlayerContext.Current??PickupCard.Resolve(f)??InteractionRouter.Resolve(f)??WorldRouting.Resolve(f));
  if(active&&(f==source||Kind(f)==ritualKind)){card=created;cardSeen=true;clearFor=0;}
 }
 // Keep the world transaction assigned to the actor that actually entered,
 // even after followers freeze or another player touches the statue.
 internal static PlayerSlot Route(PlayerSlot routed,Fsm f){
  // Shriek's native hero branch and its world Ui Msg are two different FSMs.
  // Both belong to the same real caster for the duration of this transaction.
  if(active&&(f==source||ritualKind=="Abyss Shriek"&&Kind(f)==ritualKind)&&collector!=null&&collector.Hero==primary&&collector.Alive)return collector;
  var caster=Caster(f);if(caster!=null)return caster;
  PlayerSlot p;return f!=null&&touching.TryGetValue(f,out p)&&p.Alive&&p.Ready&&p.Connected?p:routed;
 }
 internal static void NativeAction(Fsm f,string key,int value){
  string kind=key=="hasShadowDash"&&value==1?"Shade Cloak":value==2?Spell(key):null;
  if(kind==null||f==null)return;
  var s=Plugin.Self?Plugin.Self.Session:null;if(s==null||Acquired(s,kind))return;
  // The native action itself proves acquisition, including hero-owned scripts.
  kinds[f]=kind;Begin(f,TouchOwner(f,PlayerContext.Current??PickupCard.Resolve(f)));
 }
 internal static void ControlTaken(HeroController hero){
  var s=Plugin.Self?Plugin.Self.Session:null;var f=WorldRouting.Current;
  if(!hero||s==null||f==null||Kind(f)==null)return;
  Begin(f,s.Resolve(hero));
 }
 internal static void BoolChanged(PlayerData data,string key,bool value){
  var s=Plugin.Self?Plugin.Self.Session:null;
  if(value&&key=="hasShadowDash"&&s!=null&&data==s.Data&&!data.GetBool(key))NativeAction(WorldRouting.Current,key,1);
 }
 internal static void LevelChanged(PlayerData data,string key,int value){
  var s=Plugin.Self?Plugin.Self.Session:null;var f=WorldRouting.Current;
  if(value!=2||Spell(key)==null||s==null||data!=s.Data||data.GetInt(key)>=2||f==null||InteractionRouter.IsUi(f))return;
  // Covers upgrades obtained by a scripted method rather than a serialized
  // SetPlayerDataInt action. Debug edits outside a native FSM do not start it.
  kinds[f]=Spell(key);Begin(f,PlayerContext.Current??PickupCard.Resolve(f)??WorldRouting.Resolve(f));
 }
 static bool Begin(Fsm f,PlayerSlot p){
  var s=Plugin.Self?Plugin.Self.Session:null;
  if(active||s==null||!s.Active||s.Players.Count<2||s.TeamWipe||PvpMatch.Running||CoopEnding.Active||p==null||!p.Alive||!p.Ready||!p.Connected||!s.Players.Contains(p)||Kind(f)==null)return false;
  var gm=GameManager.instance;if(!gm||gm.IsLoadingSceneTransition||!gm.HasFinishedEnteringScene)return false;
  session=s;collector=p;primary=p.Hero;source=f;scene=Scenes.GetActiveScene().handle;
  elapsed=clearFor=0;cardSeen=nativeDone=false;card=null;ritualKind=Kind(f);active=true;
  // Never stop or replay the collector's animator, body or coroutines.
  // The native control-taking action that called us executes next.
  foreach(var guest in s.Players)if(guest!=p&&guest.Alive&&guest.Ready&&guest.Connected&&!guest.Hazard&&!guest.Retiring&&!guest.ArenaTransfer&&!EmergencyWarp.Active(guest)){
   var a=new Actor{Player=guest,Hero=guest.Hero,At=guest.Hero.transform.position,Body=guest.Hero.GetComponent<Rigidbody2D>(),Animator=guest.Hero.GetComponent<tk2dSpriteAnimator>(),Animation=guest.Hero.GetComponent<HeroAnimationController>()};actors.Add(a);
   try{
    if(a.Body){a.Simulated=a.Body.simulated;a.Kinematic=a.Body.isKinematic;a.Gravity=a.Body.gravityScale;}
    a.AnimationEnabled=!a.Animation||a.Animation.controlEnabled;
    if(a.Animator){a.Completed=a.Animator.AnimationCompleted;a.Triggered=a.Animator.AnimationEventTriggered;a.Animator.AnimationCompleted=null;a.Animator.AnimationEventTriggered=null;
     foreach(string pose in guest.Index==0?new[]{"Idle"}:new[]{"Focus","Focus Loop","Focus Get","Idle"})if(a.Animator.GetClipByName(pose)!=null){a.Pose=pose;break;}}
    var sprite=a.Hero.GetComponent<tk2dBaseSprite>();if(sprite)a.Sprites[sprite]=sprite.color;
    var skin=guest.SkinRenderer as SpriteRenderer;if(skin)a.Skins[skin]=skin.color;
    using(PlayerContext.Enter(guest)){CrystalDashCoop.StopPlayer(guest);guest.Hero.RelinquishControl();guest.Hero.StopAnimationControl();}
    if(a.Body){a.Body.velocity=Vector2.zero;a.Body.simulated=false;}
    a.Effect=DarkEssenceFx.Create(guest);
   }catch(Exception ex){Diagnostics.Throttled("SHADE ritual guest P"+(guest.Index+1),ex);}
  }
  try{nativeEffect=DarkEssenceFx.Create(p);}catch(Exception ex){Diagnostics.Throttled("SHADE native essence",ex);}
  DarkEssenceFx.Cue(primary,false);
  Diagnostics.Write("SHADE ritual "+Kind(f)+" collector=P"+(p.Index+1)+" guests="+actors.Count+"; native actor owns progress and card");
  return true;
 }
 internal static bool Combined(bool native)=>native||active;
 internal static bool Holds(bool native,PlayerSlot p)=>native||active&&p!=null&&actors.Exists(a=>a.Player==p&&a.Hero&&p.Hero==a.Hero&&p.Alive&&p.Ready);
 internal static bool HoldsFsm(PlayerSlot p,Fsm f){
  // PoolSpawn tags the get-item UI with its owner too. Actor suspension must
  // not suspend that UI's jump/submit listeners or the world pickup itself.
  if(active&&(f==source||PickupCard.InputOwner(f)!=null||InteractionRouter.IsUi(f)))return false;
  return ScriptedParty.Holds(p);
 }
 internal static void Tick(CoopSession s){
  if(!active)return;var gm=GameManager.instance;
  if(s!=session||s==null||!s.Active||s.TeamWipe||Scenes.GetActiveScene().handle!=scene||!primary||collector==null||collector.Hero!=primary||!collector.Alive||!collector.Connected||gm&&gm.IsLoadingSceneTransition){
   bool local=s==session&&s!=null&&s.Active&&Scenes.GetActiveScene().handle==scene&&gm&&!gm.IsLoadingSceneTransition;
   End(local);return;
  }
  if(gm&&gm.isPaused)return;
  elapsed+=Mathf.Max(0,Time.unscaledDeltaTime);
  bool visible=cardSeen&&card&&card.activeInHierarchy;
  var ui=UIManager.instance;
  bool playable=gm&&s.Gameplay&&gm.HasFinishedEnteringScene&&ui&&ui.uiState==UIState.PLAYING&&!Plugin.Self.Panel;
  // A successful native wake can finish immediately after its fade. A missed
  // callback gets one short grace period after the card has really vanished.
  var nativeAnimation=primary.GetComponent<HeroAnimationController>();
  bool nativeAwake=!primary.controlReqlinquished&&primary.acceptingInput&&(!nativeAnimation||nativeAnimation.controlEnabled);
  if(!visible&&playable&&(cardSeen||nativeDone||elapsed>2&&nativeAwake))clearFor+=Time.unscaledDeltaTime;else clearFor=0;
  bool ended=elapsed>2&&clearFor>=(nativeDone||nativeAwake?.4f:2.5f);
  bool timeout=elapsed>=32;
  if(ended||timeout){Diagnostics.Write("SHADE ritual finished timeout="+timeout+" card="+visible+" nativeWake="+nativeDone);End(true);return;}
  float ritualStrength=Mathf.Clamp01(elapsed/.65f)*(1-Mathf.Clamp01(clearFor/.9f));
  if(nativeEffect!=null)nativeEffect.Tick(primary.transform.position,elapsed,ritualStrength);
  foreach(var a in actors){
   var p=a.Player;if(!a.Hero||p.Hero!=a.Hero||!p.Alive||!p.Ready||!p.Connected)continue;
   p.ProtectionUntil=Mathf.Max(p.ProtectionUntil,Time.time+.3f);
   a.Hero.transform.position=a.At;if(a.Body)a.Body.velocity=Vector2.zero;
   if(a.Animator&&a.Pose!=null&&(a.Animator.CurrentClip==null||a.Animator.CurrentClip.name!=a.Pose))a.Animator.Play(a.Pose);
   float strength=Mathf.Clamp01(elapsed/.8f)*(1-Mathf.Clamp01(clearFor/.9f));
   Tint(a,strength);
   if(a.Effect!=null)a.Effect.Tick(a.At,elapsed,strength);
  }
 }
 // Normal per-player colour runs in LateUpdate after the cinematic Tick.
 // Reapply only this ritual's body tint after those native/custom skin passes.
 internal static void VisualTick(){
  if(!active)return;
  float strength=Mathf.Clamp01(elapsed/.8f)*(1-Mathf.Clamp01(clearFor/.9f));
  foreach(var a in actors)if(a.Hero&&a.Player.Hero==a.Hero&&a.Player.Alive&&a.Player.Ready)Tint(a,strength);
 }
 static void Tint(Actor a,float strength){
  var p=a.Player;
   if(p.Index==0)return;
   Color shade=new Color(.04f,.035f,.055f,1);
   foreach(var pair in a.Sprites)if(pair.Key){Color c=Color.Lerp(pair.Value,shade,strength*.8f);c.a=pair.Value.a;pair.Key.color=c;}
   foreach(var pair in a.Skins)if(pair.Key){Color c=Color.Lerp(pair.Value,shade,strength*.8f);c.a=pair.Value.a;pair.Key.color=c;}
 }
 static void Restore(Actor a,bool wake){
  if(a.Effect!=null)a.Effect.Dispose();
  foreach(var pair in a.Sprites)if(pair.Key)pair.Key.color=pair.Value;
  foreach(var pair in a.Skins)if(pair.Key)pair.Key.color=pair.Value;
  if(a.Animator){a.Animator.AnimationCompleted=a.Completed;a.Animator.AnimationEventTriggered=a.Triggered;}
  var p=a.Player;if(!a.Hero||p.Hero!=a.Hero||!p.Alive||p.Hazard||p.Retiring)return;
  if(a.Body){a.Body.simulated=a.Simulated;a.Body.isKinematic=a.Kinematic;a.Body.gravityScale=a.Gravity;a.Body.velocity=Vector2.zero;}
  if(a.Animation)Reflect.Set(a.Animation,"<controlEnabled>k__BackingField",a.AnimationEnabled);
  if(wake&&p.Ready){using(PlayerContext.Enter(p)){ActorRecovery.Reset(p);CoopSession.RestoreLivingVisuals(p);}p.ProtectionUntil=Time.time+1f;}
 }
 static void End(bool wake){
  if(!active)return;active=false;
  foreach(var a in actors)try{Restore(a,wake);}catch(Exception ex){Diagnostics.Throttled("SHADE ritual release",ex);}actors.Clear();
  // Never dismiss the vanilla message or mutate its save flag. Recover the
  // native actor only after the message disappeared in this same scene.
  if(nativeEffect!=null){nativeEffect.Dispose();nativeEffect=null;}
  if(wake&&collector!=null&&collector.Hero==primary&&collector.Alive&&collector.Ready&&!(card&&card.activeInHierarchy)){
   var animation=primary.GetComponent<HeroAnimationController>();
   if(primary.controlReqlinquished||!primary.acceptingInput||animation&&!animation.controlEnabled)
    using(PlayerContext.Enter(collector)){ActorRecovery.Reset(collector);CoopSession.RestoreLivingVisuals(collector);}
   DarkEssenceFx.Cue(primary,true);
  }
  if(source!=null)kinds.Remove(source);
  DarkEssenceFx.Reset();session=null;collector=null;primary=null;source=null;ritualKind=null;card=null;cardSeen=nativeDone=false;elapsed=clearFor=0;
 }
 internal static void Reset(){End(false);kinds.Clear();unrecognized.Clear();touching.Clear();touched.Clear();reported.Clear();screaming.Clear();ShadeShriekZone.Reset();DarkEssenceFx.Reset();}
}
}
