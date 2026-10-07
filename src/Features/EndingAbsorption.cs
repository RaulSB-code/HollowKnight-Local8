using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using SceneApi=UnityEngine.SceneManagement.SceneManager;

namespace KO.HollowKnight8 {
// Cosmetic followers only. The primary's native FSM and scene loader remain
// the sole owners of the ending; no participant count can delay either one.
internal static class EndingAbsorption {
 internal const float FadeSeconds=4.8f, SafetySeconds=12f;
 sealed class Flash {
  internal Renderer Renderer;internal MaterialPropertyBlock Original=new MaterialPropertyBlock(),Working=new MaterialPropertyBlock();
 }
 sealed class Actor {
  internal PlayerSlot Player;internal HeroController Hero;
  internal tk2dSpriteAnimator Animator;internal tk2dSpriteAnimationClip Focus;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip> Completed;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip,int> Triggered;
  internal HeroAnimationController Animation;internal bool Controlled;
  internal Rigidbody2D Body;internal bool Simulated,Kinematic;internal float Gravity,Angular;
  internal Vector2 Velocity;internal Vector3 Position;internal float Delay;
  internal readonly Dictionary<tk2dBaseSprite,Color> Sprites=new Dictionary<tk2dBaseSprite,Color>();
  internal readonly Dictionary<SpriteRenderer,Color> Skins=new Dictionary<SpriteRenderer,Color>();
  internal readonly List<Flash> Flashes=new List<Flash>();
  internal EndingEssenceFx Stream;
  internal bool Released;
 }
 static readonly List<Actor> actors=new List<Actor>();
 static CoopSession session;static PlayerSlot primary;static HeroController target;
 static bool active,climax,handoff;static float elapsed,armedUntil;static int sceneHandle;
 internal static bool Active=>active;
 static bool InRoom()=>GameManager.instance&&GameManager.instance.sceneName=="Room_Final_Boss_Core";
 static bool SameBody(Actor a)=>a.Player!=null&&a.Hero&&a.Player.Hero==a.Hero&&session!=null&&session.Players.Contains(a.Player);
 static bool Valid(Actor a)=>!a.Released&&SameBody(a)&&a.Player.Alive&&a.Player.Ready&&a.Player.Connected;
 internal static bool Holds(PlayerSlot p)=>active&&p!=null&&p!=primary&&actors.Exists(a=>a.Player==p&&Valid(a));
 internal static bool Blocks(PlayerSlot p,Fsm f)=>Holds(p);
 internal static bool NeedsFocus(PlayerSlot p)=>false;
 internal static void Observe(FsmState state){
  if(!InRoom()||state==null||state.Fsm==null)return;
  var s=Plugin.Self?Plugin.Self.Session:null;if(s==null||!s.Active)return;
  string path=(state.Fsm.Name+"/"+(state.Fsm.GameObject?state.Fsm.GameObject.name:"")+"/"+state.Name).ToLowerInvariant();
  if(path.Contains("corpse/boss corpse/set knight focus")){armedUntil=Time.time+30f;return;}
  var p=s.Primary;if(p==null||!p.Hero||!p.Hero.spellControl||state.Fsm!=p.Hero.spellControl.Fsm)return;
  // Focus Start D is the native absorption entry. Some skins/mods reuse
  // Focus Start instead: allow that only after the exact corpse instruction.
  if(state.Name!="Focus Start D"&&!(Time.time<armedUntil&&state.Name=="Focus Start"))return;
  if(active)return;
  Start(s,p);
 }
 static void Start(CoopSession s,PlayerSlot p){
  session=s;primary=p;target=p.Hero;elapsed=0;climax=handoff=false;sceneHandle=SceneApi.GetActiveScene().handle;
  foreach(var player in s.Players){
   if(player==p||player.Index==0||!player.Ready||!player.Connected||!player.Alive||!player.Hero||player.Hazard||player.Down||player.Retiring)continue;
   var a=new Actor{Player=player,Hero=player.Hero,Position=player.Hero.transform.position,Delay=actors.Count*.08f};
   // Add before acquiring resources so a failed cosmetic setup is reversible.
   actors.Add(a);
   try{
    a.Animator=a.Hero.GetComponent<tk2dSpriteAnimator>();
    if(a.Animator){
     a.Completed=a.Animator.AnimationCompleted;a.Triggered=a.Animator.AnimationEventTriggered;
     a.Animator.AnimationCompleted=null;a.Animator.AnimationEventTriggered=null;
     foreach(string name in new[]{"Focus","Focus Loop","Focus Start"}){
      var clip=a.Animator.GetClipByName(name);if(clip!=null&&clip.frames!=null&&clip.frames.Length>0){a.Focus=clip;break;}
     }
    }
    a.Animation=a.Hero.GetComponent<HeroAnimationController>();
    if(a.Animation){a.Controlled=a.Animation.controlEnabled;Reflect.Set(a.Animation,"<controlEnabled>k__BackingField",false);}
    a.Body=a.Hero.GetComponent<Rigidbody2D>();
    if(a.Body){a.Simulated=a.Body.simulated;a.Kinematic=a.Body.isKinematic;a.Gravity=a.Body.gravityScale;a.Velocity=a.Body.velocity;a.Angular=a.Body.angularVelocity;
     a.Body.velocity=Vector2.zero;a.Body.angularVelocity=0;a.Body.gravityScale=0;a.Body.isKinematic=true;a.Body.simulated=false;}
    var body=a.Hero.GetComponent<tk2dBaseSprite>();if(body)a.Sprites[body]=body.color;
    var skin=player.SkinRenderer;
    if(skin){var sprite=skin.GetComponent<tk2dBaseSprite>();if(sprite)a.Sprites[sprite]=sprite.color;var sr=skin as SpriteRenderer;if(sr)a.Skins[sr]=sr.color;}
    foreach(var sprite in a.Hero.GetComponentsInChildren<SpriteRenderer>(true)){
     // Custom Knight body sprites may be children; exclude effect renderers.
     string n=sprite.name.ToLowerInvariant();if(sprite==skin||n.Contains("knight")||n.Contains("skin")||n=="sprite")a.Skins[sprite]=sprite.color;
    }
    AddFlash(a,a.Hero.GetComponent<Renderer>());AddFlash(a,skin);
    foreach(var sprite in a.Sprites.Keys)AddFlash(a,sprite.GetComponent<Renderer>());
    foreach(var sprite in a.Skins.Keys)AddFlash(a,sprite);
    if(a.Animator&&a.Focus!=null)a.Animator.Play(a.Focus);
    a.Stream=EndingEssenceFx.Create(player);
   }catch(Exception e){Diagnostics.Throttled("ENDING follower P"+(player.Index+1),e);}
  }
  active=actors.Count>0;
  if(!active){session=null;primary=null;target=null;return;}
  EndingEssenceFx.Cue(target,false);
  Diagnostics.Write("ENDING essence followers="+actors.Count+" native scene load remains immediate");
 }
 static void AddFlash(Actor a,Renderer renderer){
  if(!renderer||!renderer.sharedMaterial||!renderer.sharedMaterial.HasProperty("_FlashAmount")||a.Flashes.Exists(f=>f.Renderer==renderer))return;
  var flash=new Flash{Renderer=renderer};renderer.GetPropertyBlock(flash.Original);a.Flashes.Add(flash);
 }
 internal static void Tick(CoopSession s){
  if(!active)return;
  var gm=GameManager.instance;
  int currentScene=SceneApi.GetActiveScene().handle;
  if(s!=session||s==null||!s.Active||s.TeamWipe||!gm||(currentScene!=0&&currentScene!=sceneHandle)){Reset();return;}
  // GameManager clears its room name before the old scene finishes fading.
  // Keep the visual hold until actual scene replacement/SceneChanged Reset.
  if(gm.IsLoadingSceneTransition||string.IsNullOrEmpty(gm.sceneName)){
   if(!handoff){handoff=true;elapsed=Mathf.Max(elapsed,FadeSeconds+.56f);foreach(var a in actors)if(a.Stream!=null){a.Stream.Dispose();a.Stream=null;}Diagnostics.Write("ENDING native fade handoff; followers remain invisible");}
   Visual(s);return;
  }
  if(!InRoom()||!target||primary==null||primary.Hero!=target){Reset();return;}
  if(gm.isPaused)return;
  elapsed+=Mathf.Max(0,Time.deltaTime);
  if(elapsed>=SafetySeconds&&!climax&&!handoff){Diagnostics.Write("ENDING incomplete cosmetic timeout; follower controls restored");Reset();return;}
  for(int i=actors.Count-1;i>=0;i--){var a=actors[i];if(!Valid(a)){
    if(climax||handoff){if(!a.Released)Release(a,false);continue;}
    Release(a);actors.RemoveAt(i);continue;
   }
   // Freeze only each captured guest. Do not relocate P1, heal, spend soul,
   // enter a special guest FSM, or emit shared animation completion events.
   a.Hero.transform.position=a.Position;
   if(a.Animation)Reflect.Set(a.Animation,"<controlEnabled>k__BackingField",false);
   if(a.Animator&&a.Focus!=null&&(a.Animator.CurrentClip!=a.Focus||!a.Animator.Playing))a.Animator.Play(a.Focus);
   if(a.Stream!=null)a.Stream.Tick(a.Hero.transform.position+Vector3.up*.4f,target.transform.position+Vector3.up*.5f,elapsed-a.Delay,FadeSeconds);
  }
  if(!climax&&elapsed>=FadeSeconds+.56f){climax=true;EndingEssenceFx.Cue(target,true);}
  // Disappearing/disconnecting guests never create a scene-load condition.
 }
 internal static Color Dissolve(Color original,float seconds,float delay){
  float t=Mathf.Clamp01((seconds-delay)/FadeSeconds);float white=Mathf.Clamp01(t*1.6f);
  var c=Color.Lerp(original,Color.white,white);
  float fade=Mathf.Clamp01((t-.22f)/.78f);fade=fade*fade*(3f-2f*fade);
  c.a=original.a*(1f-fade);return c;
 }
 internal static void Visual(CoopSession s){
  if(!active||s!=session)return;
  foreach(var a in actors){if(!SameBody(a))continue;
   foreach(var pair in a.Sprites)if(pair.Key)pair.Key.color=Dissolve(pair.Value,elapsed,a.Delay);
   foreach(var pair in a.Skins)if(pair.Key)pair.Key.color=Dissolve(pair.Value,elapsed,a.Delay);
   // Use the game's body flash shader to whiten the artwork itself, without
   // changing shared materials or flashing the background/other players.
   foreach(var flash in a.Flashes)if(flash.Renderer){flash.Renderer.GetPropertyBlock(flash.Working);flash.Working.SetColor("_FlashColor",Color.white);flash.Working.SetFloat("_FlashAmount",Mathf.Clamp01((elapsed-a.Delay)/FadeSeconds*1.6f));flash.Renderer.SetPropertyBlock(flash.Working);}
  }
 }
 static void Release(Actor a,bool restoreVisuals=true){
  if(a.Stream!=null)a.Stream.Dispose();
  // Restore the original object, never a newly spawned replacement body.
  if(restoreVisuals){
   foreach(var pair in a.Sprites)if(pair.Key)pair.Key.color=pair.Value;
   foreach(var pair in a.Skins)if(pair.Key)pair.Key.color=pair.Value;
   foreach(var flash in a.Flashes)if(flash.Renderer)flash.Renderer.SetPropertyBlock(flash.Original);
  }
  if(a.Released)return;
  a.Released=true;
  if(a.Animator){a.Animator.AnimationCompleted=a.Completed;a.Animator.AnimationEventTriggered=a.Triggered;}
  bool deathOwnsBody=a.Player!=null&&a.Player.Hero==a.Hero&&(!a.Player.Alive||a.Player.Down||a.Player.Hazard||a.Player.Retiring);
  if(a.Animation&&!deathOwnsBody)Reflect.Set(a.Animation,"<controlEnabled>k__BackingField",a.Controlled);
  if(a.Body&&!deathOwnsBody){a.Body.simulated=a.Simulated;a.Body.isKinematic=a.Kinematic;a.Body.gravityScale=a.Gravity;
   // Death/loading owns its new motion; only a cancelled live follower gets
   // the motion it had before this strictly visual sequence.
   if(a.Player!=null&&a.Player.Hero==a.Hero&&a.Player.Alive){a.Body.velocity=a.Velocity;a.Body.angularVelocity=a.Angular;}}
 }
 internal static void Reset(){
  active=false;
  foreach(var a in actors)try{Release(a);}catch(Exception e){Diagnostics.Throttled("ENDING restore",e);}
  actors.Clear();session=null;primary=null;target=null;elapsed=armedUntil=0;sceneHandle=0;climax=handoff=false;
  CoopEnding.observed.Clear();EndingEssenceFx.Reset();
 }
}
}
