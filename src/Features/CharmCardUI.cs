using UnityEngine;
namespace KO.HollowKnight8 {
internal static class CharmCardUI {
 static void Box(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 static GUIStyle Style(float size,Color color,bool wrap=false){var s=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size),wordWrap=wrap,clipping=TextClipping.Clip,richText=false,alignment=TextAnchor.UpperLeft};s.normal.textColor=color;return s;}
 internal static float Header(Rect r){return Mathf.Clamp(r.height*.19f,15,25);}
 internal static float Icons(PlayerSlot p,Rect r){int columns=Mathf.Clamp(Mathf.FloorToInt((r.width-16)/36f),4,10);int rows=Mathf.Max(1,Mathf.CeilToInt((float)p.Charms.Order.Count/columns));return Mathf.Min(Mathf.Clamp(r.height*.34f,18,48),Mathf.Min((r.width-16)/columns,Mathf.Max(18,r.height-Header(r)-85)/rows));}
 static Rect Warning(Rect r){float height=Style(10,Color.white,true).CalcHeight(new GUIContent(UiLocalization.Get("charms.bench")),r.width-16);return new Rect(r.x+8,r.yMax-52-Mathf.Max(24,height),r.width-16,Mathf.Max(24,height));}
 internal static Rect DescriptionRect(PlayerSlot p,Rect r){int columns=Mathf.Clamp(Mathf.FloorToInt((r.width-16)/36f),4,10);int rows=Mathf.Max(1,Mathf.CeilToInt((float)p.Charms.Order.Count/columns));float y=r.y+Header(r)+9+rows*Icons(p,r);float end=Charms.CanEdit(p)?r.yMax-56:Warning(r).y-4;return new Rect(r.x+8,Mathf.Min(y,end),r.width-16,Mathf.Max(0,end-y));}
 internal static void Draw(PlayerSlot p,PlayerData data,Rect r){
  Box(r,new Color(.015f,.013f,.023f,.96f));Box(new Rect(r.x,r.y,3,r.height),p.Color);Box(new Rect(r.x,r.y,r.width,1),p.Color);
  GUI.Label(new Rect(r.x+8,r.y+2,36,Header(r)),"P"+(p.Index+1),Style(Mathf.Clamp(r.height*.17f,14,23),p.Color));
  int selected=Charms.NativeCursor[p.Index];
  if(selected>=1&&selected<=40&&data.GetBool("gotCharm_"+selected))GUI.Label(new Rect(r.x+45,r.y+5,r.width-53,Header(r)),Charms.Name(selected),new GUIStyle(Style(10,p.Color)){alignment=TextAnchor.MiddleRight});
  int[] ids=p.Charms.Ids();int columns=Mathf.Clamp(Mathf.FloorToInt((r.width-16)/36f),4,10);float icon=Icons(p,r);
  for(int i=0;i<ids.Length;i++){var at=new Rect(r.x+8+(i%columns)*icon,r.y+Header(r)+5+(i/columns)*icon,icon-2,icon-2);var t=Charms.Icon(ids[i]);if(t){Color old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(at,t,ScaleMode.ScaleToFit,true);GUI.color=old;}else GUI.Label(at,ids[i].ToString(),Style(10,p.Color));}
  if(selected>=1&&selected<=40&&data.GetBool("gotCharm_"+selected)&&r.height>108){Rect desc=DescriptionRect(p,r);if(desc.height>15)GUI.Label(desc,Charms.Description(selected),new GUIStyle(Style(Mathf.Clamp(r.height/24,9,12),new Color(.9f,.88f,.85f),true)){richText=true});}
  if(!Charms.CanEdit(p))GUI.Label(Warning(r),UiLocalization.Get("charms.bench"),Style(10,Color.Lerp(p.Color,Color.white,.5f),true));
  string notches=UiLocalization.Get("charms.notches")+"  "+p.Charms.Used+"/"+data.charmSlots+(p.Charms.Overcharmed?"  "+UiLocalization.Get("charms.overcharmed"):"");
  var notchStyle=Style(10,new Color(.9f,.9f,.89f),true);while(notchStyle.fontSize>8&&notchStyle.CalcHeight(new GUIContent(notches),r.width-16)>28)notchStyle.fontSize--;
  GUI.Label(new Rect(r.x+8,r.yMax-48,r.width-16,28),notches,notchStyle);
  int slots=Mathf.Clamp(data.charmSlots,0,11),count=Mathf.Max(slots,Mathf.Min(16,p.Charms.Used));float spacing=(r.width-16)/Mathf.Max(1,count),dot=Mathf.Clamp(spacing*.57f,4,13);
  for(int i=0;i<count;i++){bool used=i<p.Charms.Used,extra=i>=slots;Color c=extra?new Color(.85f,.48f,.96f,.95f):used?Color.Lerp(p.Color,Color.white,.55f):new Color(.66f,.67f,.74f,.9f);float x=r.x+8+i*spacing+(spacing-dot)*.5f,y=r.yMax-dot-5;Box(new Rect(x,y,dot,dot),c);Box(new Rect(x+dot*.3f,y+dot*.3f,dot*.4f,dot*.4f),used?Color.white:new Color(.22f,.23f,.28f,.9f));}
 }
}
}
