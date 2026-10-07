using System;
using System.Reflection;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class GodhomeCameraTuning {
 static readonly FieldInfo activeLock=typeof(CameraController).GetField("currentLockArea",BindingFlags.Instance|BindingFlags.NonPublic);
 // Tighten compact, fixed arenas using their width, not their tall ceiling
 // trigger. Large/vertical fights retain alpha155's framing and edge safety.
 internal static float InitialHalf(float selected,float vanilla,CameraController controller){
  float normal=Math.Max(Math.Min(selected,vanilla),selected*.90f);
  if(!controller||controller.mode!=CameraController.CameraMode.LOCKED)return normal;
  var area=activeLock==null?null:activeLock.GetValue(controller) as CameraLockArea;
  var zone=area?area.GetComponent<Collider2D>():null;if(!zone)return normal;
  var b=zone.bounds;float x=controller.xLockMax-controller.xLockMin,y=controller.yLockMax-controller.yLockMin;
  if(b.size.x<12||b.size.x>24||b.size.y<6||b.size.y>26||x<0||x>2||y<0||y>2)return normal;
  var camera=controller.cam??Camera.main;float aspect=camera?camera.aspect:16f/9f;
  return Math.Min(normal,Math.Max(vanilla,(b.size.x+1.5f)/(2f*Math.Max(.1f,aspect))));
 }
}
}
