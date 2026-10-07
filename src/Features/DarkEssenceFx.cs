using System;
using UnityEngine;
using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {
// The supplied dream rosette has real transparent pixels. Bind the complete
// texture explicitly: the native Warp In emitter uses sprite atlas UVs that
// cannot be reused with a replacement alpha shader (they drew black quads).
internal sealed class DarkEssenceFx:IDisposable {
 GameObject root;ParticleSystem[] particles;float emit;bool burst;
 readonly System.Collections.Generic.List<Material> materials=new System.Collections.Generic.List<Material>();
 static Texture2D artwork;static AudioClip sound;static GameObject cue;
 static Texture2D Artwork(){
  if(artwork)return artwork;
  using(var stream=typeof(DarkEssenceFx).Assembly.GetManifestResourceStream("Local8.Dark.Essence")){
   if(stream==null)return null;var bytes=new byte[stream.Length];int offset=0;
   while(offset<bytes.Length){int read=stream.Read(bytes,offset,bytes.Length-offset);if(read<=0)return null;offset+=read;}
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
   if(!ImageConversion.LoadImage(texture,bytes,true)){Object.Destroy(texture);return null;}
   texture.name="Local8 Transparent Shadow Essence";texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;artwork=texture;return artwork;
  }
 }
 internal static DarkEssenceFx Create(PlayerSlot p){
  if(p==null||p.Index==0||!p.Hero)return null;
  var texture=Artwork();var shader=Shader.Find("Sprites/Default")??Shader.Find("Particles/Alpha Blended");
  // Missing artwork/shader must never fall back to the default opaque quad.
  if(!texture||!shader||p==null||!p.Hero)return null;
  var fx=new DarkEssenceFx();
  try{
   fx.root=new GameObject("Local8 Shade Cloak Dark Essence P"+(p.Index+1));fx.root.transform.position=p.Hero.transform.position;fx.root.layer=p.Hero.gameObject.layer;
   var ps=fx.root.AddComponent<ParticleSystem>();fx.particles=new[]{ps};ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=48;main.simulationSpeed=1;main.startLifetime=.65f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.3f,.48f);main.startColor=new Color(0,0,0,.95f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
   var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
   var sheet=ps.textureSheetAnimation;sheet.enabled=false;
   var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
   gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.12f,0),new GradientAlphaKey(.82f,.16f),new GradientAlphaKey(.55f,.32f),new GradientAlphaKey(.9f,.48f),new GradientAlphaKey(.64f,.65f),new GradientAlphaKey(.82f,.78f),new GradientAlphaKey(0,1)});color.color=gradient;
   var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.12f));
   var r=ps.GetComponent<ParticleSystemRenderer>();
   var material=new Material(shader);fx.materials.Add(material);material.name="Local8 Alpha Shadow Essence";material.mainTexture=texture;
   if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",texture);
   if(material.HasProperty("_TintColor"))material.SetColor("_TintColor",Color.white);
   if(material.HasProperty("_Color"))material.SetColor("_Color",Color.white);
   r.sharedMaterial=material;r.renderMode=ParticleSystemRenderMode.Billboard;
   var body=p.SkinRenderer?p.SkinRenderer:p.Hero.GetComponent<Renderer>();if(body){r.sortingLayerID=body.sortingLayerID;r.sortingOrder=body.sortingOrder+2;}
   ps.Play();return fx;
  }catch{fx.Dispose();throw;}
 }
 internal void Tick(Vector3 at,float elapsed,float strength){
  if(!root||particles==null)return;root.transform.position=at;
  emit+=Mathf.Min(.05f,Time.deltaTime)*24f*strength;int count=Mathf.Min(3,Mathf.FloorToInt(emit));emit-=count;
  if(!burst&&strength>.15f){count+=12;burst=true;}
  for(int n=0;n<particles.Length&&n<3;n++){var ps=particles[n];if(!ps)continue;
   for(int i=0;i<count;i++){
    float angle=elapsed*2.3f+UnityEngine.Random.Range(0,Mathf.PI*2);float radius=UnityEngine.Random.Range(.95f,1.65f);
    var radial=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
    var ep=new ParticleSystem.EmitParams();ep.position=at+radial+new Vector3(0,.3f,-.2f);
    ep.startLifetime=.65f;ep.velocity=radial*(-1f/.65f);ep.startColor=new Color(0,0,0,.95f);ps.Emit(ep,1);
   }
  }
 }
 internal static void Cue(HeroController hero,bool complete){try{
  if(!sound)foreach(var clip in Resources.FindObjectsOfTypeAll<AudioClip>())if(clip){string n=clip.name.ToLowerInvariant();if(n.Contains("dream")&&(n.Contains("charge")||n.Contains("appear"))&&!n.Contains("fail")&&!n.Contains("music")){sound=clip;break;}}
  if(!sound||!hero)return;if(cue)Object.Destroy(cue);cue=new GameObject("Local8 Quiet Shade Essence Cue");cue.transform.position=hero.transform.position;
  var audio=cue.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=(complete?.045f:.035f)*Mathf.Clamp01(GameManager.instance?GameManager.instance.GetImplicitCinematicVolume():1f);audio.PlayOneShot(sound);Object.Destroy(cue,Mathf.Max(1,sound.length+.25f));
 }catch(Exception ex){Diagnostics.Throttled("SHADE ritual quiet cue",ex);}}
 public void Dispose(){if(root)Object.Destroy(root);foreach(var material in materials)if(material)Object.Destroy(material);materials.Clear();root=null;particles=null;}
 internal static void Reset(){sound=null;/* Finite one-shot cue is allowed to finish naturally. */cue=null;}
}
}
