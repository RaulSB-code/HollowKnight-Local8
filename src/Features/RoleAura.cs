using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
namespace KO.HollowKnight8 {
internal sealed class RoleAura {
 internal HeroController Hero;
 internal GameObject Root;
 SpriteRenderer dome;
 ParticleSystem particles;
 float visible,emit,nextSource,pulseUntil;
 static Texture2D texture;
 static Sprite sprite;
 static readonly Dictionary<HeroController,RoleAura> all=new Dictionary<HeroController,RoleAura>();
 static readonly List<HeroController> expired=new List<HeroController>();
 static void Asset(){if(sprite)return;
  using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Local8.Roles.Dome")){
   if(stream==null)return;byte[] bytes=new byte[stream.Length];int read=0;while(read<bytes.Length){int n=stream.Read(bytes,read,bytes.Length-read);if(n<=0)throw new EndOfStreamException();read+=n;}
   texture=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(texture,bytes,true);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
   // Cupula2: align its drawn floor with the knight's feet.
   sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.12f),texture.width/(RoleHealingRules.Radius*2f/.85f));
  }
 }
 internal static Vector3 Feet(PlayerSlot p){
  var h=p.Hero;Vector3 v=h.transform.position;var col=h.GetComponent<BoxCollider2D>();
  v.y=col?col.bounds.min.y:v.y-.85f;v.z=h.transform.position.z-.08f;return v;
 }
 static RoleAura Get(PlayerSlot p){RoleAura a;if(all.TryGetValue(p.Hero,out a)&&a.Root)return a;
  Asset();a=new RoleAura{Hero=p.Hero};a.Root=new GameObject("Local8 Healer Dome P"+(p.Index+1));a.Root.layer=p.Hero.gameObject.layer;
  a.dome=a.Root.AddComponent<SpriteRenderer>();a.dome.sprite=sprite;
  Renderer body=p.SkinRenderer?p.SkinRenderer:p.Hero.GetComponent<Renderer>();if(body){a.dome.sortingLayerID=body.sortingLayerID;a.dome.sortingOrder=body.sortingOrder+1;}
  all[p.Hero]=a;return a;
 }
 internal static ParticleSystem FindFocusParticles(HeroController hero){
  if(!hero||!hero.spellControl)return null;
  foreach(var state in hero.spellControl.FsmStates){if(state.Name!="Focus"&&state.Name!="Focus Start")continue;
   foreach(var action in state.Actions){var rate=action as SetParticleEmissionRate;if(rate==null)continue;
    var obj=hero.spellControl.Fsm.GetOwnerDefaultTarget(rate.gameObject);if(!obj)continue;
    var ps=obj.GetComponent<ParticleSystem>();if(ps&&ps.GetComponent<ParticleSystemRenderer>()&&ps.GetComponent<ParticleSystemRenderer>().sharedMaterial)return ps;
   }
  }
  // Same player's native focus dust, including renamed/skinned prefabs.
  foreach(var ps in hero.GetComponentsInChildren<ParticleSystem>(true)){
   string name=ps.name.ToLowerInvariant();if((name.Contains("focus")||name=="dust l"||name=="dust r")&&ps.GetComponent<ParticleSystemRenderer>()&&ps.GetComponent<ParticleSystemRenderer>().sharedMaterial)return ps;
  }
  return null;
 }
 void MakeParticles(){if(particles||Time.unscaledTime<nextSource)return;nextSource=Time.unscaledTime+2f;
  var original=FindFocusParticles(Hero);if(!original)return;
  var go=new GameObject("Native Focus Soul Motes");go.layer=Root.layer;go.transform.SetParent(Root.transform,false);
  particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=96;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.85f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.07f,.15f);main.startColor=Color.white;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
  var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.enabled=false;
  var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
  var source=original.GetComponent<ParticleSystemRenderer>();var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterials=source.sharedMaterials;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sortingLayerID=dome.sortingLayerID;renderer.sortingOrder=dome.sortingOrder+1;
  var sheet=original.textureSheetAnimation;if(sheet.enabled){var copy=particles.textureSheetAnimation;copy.enabled=true;copy.mode=sheet.mode;copy.numTilesX=sheet.numTilesX;copy.numTilesY=sheet.numTilesY;copy.animation=sheet.animation;copy.frameOverTime=sheet.frameOverTime;copy.startFrame=sheet.startFrame;copy.cycleCount=sheet.cycleCount;}
  particles.Play();
 }
 void Emit(int count){if(!particles)return;
  for(int i=0;i<count;i++){float angle=UnityEngine.Random.Range(.03f,Mathf.PI-.03f);var ep=new ParticleSystem.EmitParams();ep.position=Root.transform.position+new Vector3(Mathf.Cos(angle)*RoleHealingRules.Radius,Mathf.Sin(angle)*RoleHealingRules.Height,-.02f);ep.velocity=new Vector3(UnityEngine.Random.Range(-.12f,.12f),UnityEngine.Random.Range(.12f,.40f),0);particles.Emit(ep,1);}
 }
 void Draw(PlayerSlot p,bool active){float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);visible=Mathf.MoveTowards(visible,active?1f:0f,dt*7f);
  Root.transform.position=Feet(p);dome.enabled=visible>0f;float flash=Time.time<pulseUntil?.17f:0f;dome.color=new Color(1,1,1,visible*(.34f+flash+.035f*Mathf.Sin(Time.time*3)));
  if(!active){emit=0;return;}MakeParticles();emit+=Time.deltaTime*22f;int count=Mathf.FloorToInt(emit);emit-=count;Emit(Mathf.Min(count,4));
 }
 internal static void Tick(CoopSession s){
  foreach(var kv in all)if(!kv.Key||!kv.Value.Root||!s.Players.Exists(p=>p.Hero==kv.Key))expired.Add(kv.Key);
  foreach(var h in expired){RoleAura a=all[h];if(a.Root)UnityEngine.Object.Destroy(a.Root);all.Remove(h);}expired.Clear();
  foreach(var p in s.Players){if(!p.Hero)continue;bool active=p.Alive&&p.Ready&&!p.InputBlocked&&!p.Hazard&&!p.Reviving&&!p.ArenaTransfer&&!RoleSystem.JoiningMenu&&!Charms.NativeMenuOpen&&RoleSystem.Definition(p).HealingAura&&p.Hero.cState.focusing;
   RoleAura a;all.TryGetValue(p.Hero,out a);if(active){a=Get(p);}if(a!=null&&a.Root)a.Draw(p,active);
  }
 }
 internal static void Pulse(PlayerSlot p){var a=Get(p);a.Root.transform.position=Feet(p);a.pulseUntil=Time.time+.35f;a.MakeParticles();a.Emit(16);}
 internal static void Received(PlayerSlot p){
  p.HealFlashUntil=Time.unscaledTime+.35f;
  // Native shader flash whitens the actual body, including Custom Knight renderers.
  // It has a finite duration; never use the repeating white-stay effect.
  var flash=p.Hero.GetComponent<SpriteFlash>();if(flash)flash.flashFocusHeal();
 }
 internal static void Remove(PlayerSlot p){if(p==null||!p.Hero)return;RoleAura a;if(all.TryGetValue(p.Hero,out a)){if(a.Root)UnityEngine.Object.Destroy(a.Root);all.Remove(p.Hero);}}
 internal static void Hide(){foreach(var a in all.Values){if(a.dome)a.dome.enabled=false;if(a.particles)a.particles.Clear();a.visible=0;}}
 internal static void Clear(){foreach(var a in all.Values)if(a.Root)UnityEngine.Object.Destroy(a.Root);all.Clear();expired.Clear();}
 internal static void Release(){Clear();if(sprite)UnityEngine.Object.Destroy(sprite);if(texture)UnityEngine.Object.Destroy(texture);sprite=null;texture=null;}
}
}
