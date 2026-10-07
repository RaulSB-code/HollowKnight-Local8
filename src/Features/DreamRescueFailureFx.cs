using System;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
internal static class DreamRescueFailureFx {
 internal static void Emit(PlayerSlot p){
  GameObject effect=null;
  try{
   effect=NativeDreamFx.Create(p,p.Hero.transform.position);if(!effect)return;
   bool emitted=false;
   foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>(true)){
    ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=ps.main;main.loop=false;main.maxParticles=6;main.startLifetime=.4f;main.startSize=.28f;main.startSpeed=0;main.simulationSpeed=1;main.simulationSpace=ParticleSystemSimulationSpace.World;
    var emission=ps.emission;emission.enabled=false;
    var velocity=ps.velocityOverLifetime;velocity.enabled=false;var force=ps.forceOverLifetime;force.enabled=false;var noise=ps.noise;noise.enabled=false;
    var color=Color.Lerp(Color.white,p.Color,.2f);color.a=.14f;main.startColor=color;
    var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
     new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.05f,.3f),new GradientAlphaKey(.8f,.45f),new GradientAlphaKey(.05f,.65f),new GradientAlphaKey(.5f,.78f),new GradientAlphaKey(0,1)});
    var lifetime=ps.colorOverLifetime;lifetime.enabled=true;lifetime.color=gradient;
    if(emitted)continue;emitted=true;ps.Play(false);
    for(int i=0;i<6;i++){float angle=i*Mathf.PI/3;var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
     ps.Emit(new ParticleSystem.EmitParams{position=p.Hero.transform.position+direction*.25f,velocity=direction*1.6f,startColor=color},1);
    }
   }
   Object.Destroy(effect,.5f);
  }catch(Exception ex){if(effect)Object.Destroy(effect);Diagnostics.Throttled("DREAM rescue denied particles",ex);}
 }
}
}
