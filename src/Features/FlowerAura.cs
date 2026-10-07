using System;
using UnityEngine;
using UnitySceneManager=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
internal static class FlowerAura {
 static PlayerSlot carrier;static HeroController hero;static GameObject root;
 static ParticleSystem particles;static float nextSource,emit;
 internal static void Reset(){if(root){root.SetActive(false);UnityEngine.Object.Destroy(root);}carrier=null;hero=null;root=null;particles=null;emit=0;nextSource=0;}
 internal static void Tick(){
  try{
   var runtime=Plugin.Self;var s=runtime?runtime.Session:null;var gm=GameManager.instance;
   if(!runtime||!runtime.Enabled.Value||s==null||!s.Active){Reset();return;}
   // Use the existing quest's exact owner and intact-flower checks. Never infer
   // ownership from the shared PlayerData view of a different player's update.
   var p=FlowerRules.Carrier(s);
   if(p==null||!p.Hero||!p.Alive||!p.Connected){Reset();return;}
   // Knights live in Unity's persistent scene; the cosmetic belongs to the
   // active room, otherwise comparing its scene with the hero rebuilds it every frame.
   if(carrier!=p||hero!=p.Hero||(root&&root.scene!=UnitySceneManager.GetActiveScene())){Reset();carrier=p;hero=p.Hero;}
   bool visible=s.Gameplay&&gm&&!gm.IsLoadingSceneTransition&&gm.HasFinishedEnteringScene&&!gm.isPaused&&!runtime.Panel&&p.Ready&&!p.Hazard&&!p.SpawnPending&&!p.ArenaTransfer&&!TransitionVote.Holding(p)&&p.Hero.gameObject.activeInHierarchy&&!Charms.NativeMenuOpen;
   if(!visible){if(root)root.SetActive(false);if(particles)particles.Clear();emit=0;return;}
   if(!particles){if(Time.unscaledTime<nextSource)return;nextSource=Time.unscaledTime+2;
    var original=RoleAura.FindFocusParticles(hero);if(!original)return;
    root=new GameObject("Local8 Delicate Flower Soul Aura P"+(p.Index+1));root.layer=hero.gameObject.layer;
    // Only copy native particle materials/atlas. No cloned FSMs, lights,
    // colliders, audio or damage components can affect the quest or other players.
    particles=root.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=32;main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.1f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.10f,.22f);main.startColor=Color.white;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.gravityModifier=0;
    var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
    var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
    var source=original.GetComponent<ParticleSystemRenderer>();var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=source.sharedMaterials;renderer.renderMode=ParticleSystemRenderMode.Billboard;
    Renderer body=p.SkinRenderer?p.SkinRenderer:hero.GetComponent<Renderer>();if(body){renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+1;}
    var sheet=original.textureSheetAnimation;if(sheet.enabled){var copy=particles.textureSheetAnimation;copy.enabled=true;copy.mode=sheet.mode;copy.numTilesX=sheet.numTilesX;copy.numTilesY=sheet.numTilesY;copy.animation=sheet.animation;copy.frameOverTime=sheet.frameOverTime;copy.startFrame=sheet.startFrame;copy.cycleCount=sheet.cycleCount;}
   }
   root.transform.position=hero.transform.position+new Vector3(0,0,-.1f);root.SetActive(true);if(!particles.isPlaying)particles.Play();
   emit+=Mathf.Min(Time.deltaTime,.1f)*12;int count=Mathf.FloorToInt(emit);emit-=count;
   for(int i=0;i<count;i++){float angle=UnityEngine.Random.Range(0,Mathf.PI*2);var ep=new ParticleSystem.EmitParams();ep.position=new Vector3(Mathf.Cos(angle)*.8f,Mathf.Sin(angle)*.9f,0);ep.velocity=new Vector3(UnityEngine.Random.Range(-.04f,.04f),UnityEngine.Random.Range(.15f,.35f),0);particles.Emit(ep,1);}
  }catch(Exception e){Reset();nextSource=Time.unscaledTime+2;Diagnostics.Throttled("FLOWER aura",e);}
 }
}
}
