using UnityEngine;
namespace KO.HollowKnight8 {
internal static class KeyboardWarning {
 internal static void Draw(float width,ref float row){
  string message=UiLocalization.Get("keyboard.rollover");
  var style=new GUIStyle(Hud.small){fontSize=13,wordWrap=true,richText=false};
  style.normal.textColor=new Color(.94f,.83f,.62f);
  float available=Mathf.Max(80,width-10);
  float height=Mathf.Max(34,style.CalcHeight(new GUIContent(message),available)+4);
  GUI.Label(new Rect(5,row,available,height),message,style);
  row+=height+10;
 }
}
}
