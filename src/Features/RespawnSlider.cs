using UnityEngine;
namespace KO.HollowKnight8 {
internal static class RespawnSlider {
 internal static void Draw(Local8Runtime runtime,float width,ref float row){
  if(runtime==null||runtime.RespawnSeconds==null)return;
  float current=Mathf.Clamp(runtime.RespawnSeconds.Value,10f,60f);
  int seconds=Mathf.RoundToInt(current);
  var style=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleLeft};style.normal.textColor=new Color(.93f,.94f,.98f);
  GUI.Label(new Rect(8f,row-57f,width-12f,24f),RoleText.Get("respawn.duration")+": "+seconds+" "+RoleText.Get("respawn.seconds"),style);
  float next=GUI.HorizontalSlider(new Rect(16f,row-30f,width-42f,24f),current,10f,60f);
  int selected=Mathf.RoundToInt(next);
  if(selected!=seconds)runtime.RespawnSeconds.Value=selected;
 }
 internal static void Normalize(Local8Runtime runtime){if(runtime!=null&&runtime.RespawnSeconds!=null)runtime.RespawnSeconds.Value=Mathf.Clamp(runtime.RespawnSeconds.Value,10f,60f);}
}
}
