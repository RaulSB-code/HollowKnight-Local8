using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpGeoHud {
 static readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
 static GeoCounter counter;static float nextScan;
 internal static void Tick(){if(!Battle())Reset();}
 static bool Battle(){var r=Plugin.Self;var s=r==null?null:r.Session;var gm=GameManager.instance;
  return r!=null&&r.Enabled.Value&&s!=null&&s.Active&&gm&&gm.IsGameplayScene()&&!gm.IsLoadingSceneTransition&&(PvpArena.KeepsMusic||!PvpArena.Active&&PvpMatch.Running);
 }
 static void Hide(GameObject target){if(!target)return;foreach(var renderer in target.GetComponentsInChildren<Renderer>(true))if(renderer){if(!hidden.ContainsKey(renderer))hidden[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;}}
 internal static void BeforeCamera(Camera camera){
  if(!Battle()){Reset();return;}
  var gc=GameCameras.instance;if(!gc)return;var current=gc.geoCounter;
  if(current!=counter){Reset();counter=current;}
  if(!counter)return;
  if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.5f;
   Hide(counter.geoSprite);if(counter.geoTextMesh)Hide(counter.geoTextMesh.gameObject);if(counter.addTextMesh)Hide(counter.addTextMesh.gameObject);if(counter.subTextMesh)Hide(counter.subTextMesh.gameObject);
  }
  foreach(var pair in hidden)if(pair.Key)pair.Key.forceRenderingOff=true;
 }
 internal static void Reset(){foreach(var pair in hidden)if(pair.Key)pair.Key.forceRenderingOff=pair.Value;hidden.Clear();counter=null;nextScan=0;}
}
}
