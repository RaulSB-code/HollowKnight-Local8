using System;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
// Native artwork in a fresh emitter: no cloned boss FSMs, flashes, lights,
// colliders, damage components or global audio snapshots.
internal sealed class EndingEssenceFx:IDisposable {
 GameObject root;ParticleSystem particles;readonly ParticleSystem.Particle[] motes=new ParticleSystem.Particle[96];float emit;
 static AudioClip sound;static GameObject cue;static ParticleSystem source;
 internal static EndingEssenceFx Create(PlayerSlot p){
  if(!source){int best=39;foreach(var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>()){
   if(!ps||!ps.GetComponent<ParticleSystemRenderer>()||!ps.GetComponent<ParticleSystemRenderer>().sharedMaterial)continue;
   int score=NativeDreamFx.Score(ps);if(score>best){best=score;source=ps;}
  }}
  var native=source?source:RoleAura.FindFocusParticles(p.Hero);if(!native)return null;
  var fx=new EndingEssenceFx();
  try{
   fx.root=new GameObject("Local8 Ending Dream Essence P"+(p.Index+1));fx.root.layer=p.Hero.gameObject.layer;
   fx.particles=fx.root.AddComponent<ParticleSystem>();fx.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=fx.particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=96;main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,1.8f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.09f,.18f);main.startColor=Color.white;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
   var emission=fx.particles.emission;emission.enabled=false;var shape=fx.particles.shape;shape.enabled=false;
   var col=fx.particles.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.12f),new GradientAlphaKey(.65f,.8f),new GradientAlphaKey(0,1)});col.color=gradient;
   var renderer=fx.particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=native.GetComponent<ParticleSystemRenderer>().sharedMaterials;renderer.renderMode=ParticleSystemRenderMode.Billboard;
   Renderer body=p.SkinRenderer?p.SkinRenderer:p.Hero.GetComponent<Renderer>();if(body){renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+1;}
   var sheet=native.textureSheetAnimation;if(sheet.enabled){var copy=fx.particles.textureSheetAnimation;copy.enabled=true;copy.mode=sheet.mode;copy.numTilesX=sheet.numTilesX;copy.numTilesY=sheet.numTilesY;copy.animation=sheet.animation;copy.frameOverTime=sheet.frameOverTime;copy.startFrame=sheet.startFrame;copy.cycleCount=sheet.cycleCount;}
   fx.particles.Play();return fx;
  }catch{fx.Dispose();throw;}
 }
 internal void Tick(Vector3 from,Vector3 target,float elapsed,float duration){
  if(!particles)return;
  float dt=Mathf.Min(Mathf.Max(0,Time.deltaTime),.1f);
  if(elapsed>=0&&elapsed<duration+.1f){emit+=dt*(elapsed>duration*.65f?24f:14f);int n=Mathf.Min(4,Mathf.FloorToInt(emit));emit-=n;
   for(int i=0;i<n;i++){var ep=new ParticleSystem.EmitParams();ep.position=from+new Vector3(UnityEngine.Random.Range(-.3f,.3f),UnityEngine.Random.Range(-.3f,.5f),-.15f);ep.velocity=Vector3.zero;particles.Emit(ep,1);}}
  int count=particles.GetParticles(motes);
  for(int i=0;i<count;i++){
   var mote=motes[i];Vector3 delta=target-mote.position;
   // Remaining lifetime sets arrival speed even when guests stand far away.
   float travel=Mathf.Max(.08f,mote.remainingLifetime-.12f);
   float step=Mathf.Clamp01(dt/travel);Vector3 pos=Vector3.Lerp(mote.position,target,step);
   float curl=Mathf.Sin((mote.startLifetime-mote.remainingLifetime)*8f+i*.7f)*.12f*dt;
   pos+=new Vector3(-delta.y,delta.x,0).normalized*curl;
   mote.position=pos;mote.velocity=Vector3.zero;
   if(delta.sqrMagnitude<.04f)mote.remainingLifetime=Mathf.Min(.12f,mote.remainingLifetime);
   motes[i]=mote;
  }
  particles.SetParticles(motes,count);
 }
 internal static void Cue(HeroController primary,bool climax){
  try{
   if(!sound){foreach(var clip in Resources.FindObjectsOfTypeAll<AudioClip>()){
    if(!clip)continue;string n=clip.name.ToLowerInvariant();
    if((n.Contains("dream")&&(n.Contains("appear")||n.Contains("warp")||n.Contains("charge")))&&!n.Contains("fail")&&!n.Contains("music")){sound=clip;break;}
   }}
   if(!sound||!primary)return;
   if(cue)Object.Destroy(cue);cue=new GameObject("Local8 Ending Quiet Dream Cue");cue.transform.position=primary.transform.position;
   var audio=cue.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
   audio.volume=(climax?.055f:.04f)*Mathf.Clamp01(GameManager.instance?GameManager.instance.GetImplicitCinematicVolume():1f);
   audio.PlayOneShot(sound);Object.Destroy(cue,Mathf.Max(1,sound.length+.25f));
  }catch(Exception e){Diagnostics.Throttled("ENDING quiet cue",e);}
 }
 public void Dispose(){if(root)Object.Destroy(root);root=null;particles=null;}
 internal static void Reset(){if(cue)Object.Destroy(cue);cue=null;sound=null;source=null;}
}
}
