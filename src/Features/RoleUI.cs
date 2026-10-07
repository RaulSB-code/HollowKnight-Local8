using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class RoleUI {
 static Texture2D horizontal,vertical,frame,verticalFrame;
 static Font nativeFont;
 static string fontLanguage;
 static float nextFont;
 static readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
 static GameObject hiddenPane;
 static float nextHide;
 static readonly Rect[] HorizontalUV={
  new Rect(20,128,303,395), new Rect(413,128,279,395), new Rect(803,128,300,395),new Rect(1208,128,295,395),new Rect(1603,128,300,395),
  new Rect(145,547,350,370),new Rect(584,547,331,370),new Rect(1000,547,335,370),new Rect(1423,547,334,370)};
 static readonly Rect[] VerticalUV={
  new Rect(240,100,200,185),new Rect(240,368,200,185),new Rect(230,630,214,165),new Rect(240,870,200,185),new Rect(240,1118,200,190),
  new Rect(650,150,240,182),new Rect(658,450,220,183),new Rect(666,730,205,184),new Rect(644,1030,244,180)};
 static Texture2D Load(string resource){using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){if(s==null)return null;byte[] a=new byte[s.Length];int offset=0;while(offset<a.Length){int n=s.Read(a,offset,a.Length-offset);if(n<=0)throw new EndOfStreamException(resource);offset+=n;}var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,a,true);t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;return t;}}
 static void Assets(){if(horizontal==null)horizontal=Load("Local8.Roles.Horizontal");if(vertical==null)vertical=Load("Local8.Roles.Vertical");if(frame==null)frame=Load("Local8.Roles.Frame");if(verticalFrame==null)verticalFrame=Load("Local8.Roles.VerticalFrame");}
 static void Font(){string lang=RoleText.Language;if(fontLanguage==lang&&nativeFont!=null)return;if(Time.unscaledTime<nextFont&&fontLanguage==lang)return;nextFont=Time.unscaledTime+3; fontLanguage=lang;
  var fonts=Resources.FindObjectsOfTypeAll<Font>();int best=-1;Font selected=null;string sample=RoleText.Get("title");
  foreach(Font f in fonts){if(f==null)continue;int score=0;string name=f.name.ToLowerInvariant();if(name.Contains("perpetua"))score+=20;if(name.Contains("trajan"))score+=15;int missing=0;foreach(char c in sample)if(!char.IsWhiteSpace(c)&&!f.HasCharacter(c))missing++;if(missing>0)score-=missing*20;if(score>best){best=score;selected=f;}}
  nativeFont=selected;
 }
 static GUIStyle Style(float size,Color color,TextAnchor align=TextAnchor.MiddleCenter,bool wrap=false){Font();var s=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size),alignment=align,wordWrap=wrap,richText=false,clipping=TextClipping.Clip,fontStyle=FontStyle.Normal};if(nativeFont!=null)s.font=nativeFont;s.normal.textColor=color;return s;}
 internal static void ClockText(Rect r,string value){Text(new Rect(r.x+1,r.y+1,r.width,r.height),value,36,Color.black,TextAnchor.MiddleRight);Text(r,value,36,Color.white,TextAnchor.MiddleRight);}
 static void Text(Rect r,string value,float size,Color color,TextAnchor align=TextAnchor.MiddleCenter,bool wrap=false){GUI.Label(r,value,Style(size,color,align,wrap));}
 static void Box(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 static Rect UV(Texture2D texture,Rect pixels){return new Rect(pixels.x/texture.width,1f-pixels.yMax/texture.height,pixels.width/texture.width,pixels.height/texture.height);}
 static void Art(Rect r,Texture2D texture,Rect crop,Color c,bool preserve=true){if(texture==null)return;if(preserve){float ratio=crop.width/crop.height;float w=Mathf.Min(r.width,r.height*ratio);float h=w/ratio;r=new Rect(r.center.x-w/2,r.center.y-h/2,w,h);}Color old=GUI.color;GUI.color=c;GUI.DrawTextureWithTexCoords(r,texture,UV(texture,crop),true);GUI.color=old;}
 static void Selection(Rect r,Color color,int overlap,bool compact=false){
  if(compact){
   // Draw the complete new horizontal frame, keeping both central ornaments intact.
   // Nest shared selections inside the cell instead of crossing adjacent rows/columns.
   r=new Rect(r.x,r.y-r.height*.25f,r.width,r.height*1.5f);
   float inset=Mathf.Min(overlap,7)*.55f;
   Art(new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2),verticalFrame,new Rect(0,0,verticalFrame.width,verticalFrame.height),color,false);return;
  }
  float expand=Mathf.Min(overlap,3)*2f;r=new Rect(r.x-expand,r.y-expand,r.width+expand*2,r.height+expand*2);
  // Nine-slice the supplied frame: preserve the ornament without stretching it over icons or text.
  float edge=Mathf.Min(24f,r.height*.15f),sx=337,sy=13,sw=1250,sh=1067,cut=240;
  for(int row=0;row<3;row++)for(int col=0;col<3;col++){if(row==1&&col==1)continue;
   float x=col==0?r.x:col==1?r.x+edge:r.xMax-edge,y=row==0?r.y:row==1?r.y+edge:r.yMax-edge;
   float w=col==1?r.width-edge*2:edge,h=row==1?r.height-edge*2:edge;
   float cx=col==0?sx:col==1?sx+cut:sx+sw-cut,cy=row==0?sy:row==1?sy+cut:sy+sh-cut;
   Art(new Rect(x,y,w,h),frame,new Rect(cx,cy,col==1?sw-cut*2:cut,row==1?sh-cut*2:cut),color,false);
  }
 }
 static void Badge(Rect r,PlayerSlot p,bool chosen){Text(r,"P"+(p.Index+1)+(chosen?" •":""),12,p.Color);}
 static string Controls(PlayerSlot p){var a=p.Actions;string left="",ok="",back="";try{if(a!=null){if(a.left.Bindings.Count>0)left=a.left.Bindings[0].Name;if(a.menuSubmit.Bindings.Count>0)ok=a.menuSubmit.Bindings[0].Name;if(a.menuCancel.Bindings.Count>0)back=a.menuCancel.Bindings[0].Name;}}catch{}if(left.Length==0)left="← ↑ ↓ →";if(ok.Length==0)ok="A";if(back.Length==0)back="B";return left+"  —  "+RoleText.Get("navigate")+"     "+ok+"  —  "+RoleText.Get("confirm")+"     "+back+"  —  "+RoleText.Get("continue");}
 internal static void Draw(Local8Runtime runtime){if(!RoleSystem.JoiningMenu||runtime.Panel||GameManager.instance==null||GameManager.instance.isPaused)return;PlayerSlot p=RoleSystem.Joining;RoleState st=RoleSystem.State(p);Assets();
  Matrix4x4 old=GUI.matrix;Color oc=GUI.color;int depth=GUI.depth;try {GUI.depth=-12000;float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float w=Screen.width/scale,h=900;Box(new Rect(0,0,w,h),new Color(.012f,.016f,.028f,.78f));
  Text(new Rect(w*.08f,32,w*.84f,49),RoleText.Get("title"),34,Color.white);
  Text(new Rect(w*.12f,86,w*.76f,30),"P"+(p.Index+1),24,p.Color);
  Rect content=new Rect(w*.075f,132,w*.85f,495);
  int split=(RoleCatalog.All.Length+1)/2;
  for(int i=0;i<RoleCatalog.All.Length;i++){int row=i<split?0:1,col=row==0?i:i-split,count=row==0?split:RoleCatalog.All.Length-split;float cellW=Mathf.Min(content.width/split,255);float gap=12;float start=content.center.x-(count*cellW)/2;Rect cell=new Rect(start+col*cellW+gap/2,content.y+row*245,cellW-gap,226);
   if(i==st.Cursor)Selection(new Rect(cell.x-9,cell.y-8,cell.width+18,cell.height+16),p.Color,0);
   if(i<HorizontalUV.Length)Art(new Rect(cell.x+20,cell.y+10,cell.width-40,166),horizontal,HorizontalUV[i],Color.white);
   Text(new Rect(cell.x,cell.y+179,cell.width,30),RoleCatalog.All[i].Name,20,i==st.Cursor?p.Color:Color.white);
   if(i==st.Chosen)Text(new Rect(cell.x,cell.y+207,cell.width,16),RoleText.Get("selected"),11,p.Color);
  }
  RoleDefinition r=RoleCatalog.All[st.Cursor];Text(new Rect(w*.1f,645,w*.8f,36),r.Name,25,p.Color);
  Text(new Rect(w*.16f,685,w*.68f,92),r.Description,19,new Color(.89f,.9f,.94f),TextAnchor.UpperCenter,true);
  Text(new Rect(w*.08f,800,w*.84f,26),Controls(p),15,Color.white);
  Text(new Rect(w*.08f,838,w*.84f,37),RoleText.Get("hint"),13,new Color(.65f,.69f,.76f),TextAnchor.MiddleCenter,true);
  }finally{GUI.matrix=old;GUI.color=oc;GUI.depth=depth;}
 }
 internal static void DrawNative(CoopSession s,float w,float h,float scale){if(!Charms.NativeMenuOpen||s==null)return;Assets();
  Rect panel=new Rect(w*.610f,h*.192f,w*.317f,h*.637f);
  Box(panel,new Color(.012f,.01f,.018f,.98f));Text(new Rect(panel.x,panel.y+4,panel.width,28),RoleText.Get("roles"),23,Color.white);
  float gridY=panel.y+39,gridH=panel.height-185,cellW=(panel.width-24)/2,cellH=gridH/5;
  for(int i=0;i<RoleCatalog.All.Length;i++){int split=(RoleCatalog.All.Length+1)/2,col=i<split?0:1,row=col==0?i:i-split;int count=col==0?split:RoleCatalog.All.Length-split;
   float rowY=gridY+row*gridH/count;float height=gridH/count;Rect cell=new Rect(panel.x+12+col*cellW,rowY,cellW,height);
   if(i<VerticalUV.Length)Art(new Rect(cell.x+cellW*.12f,cell.y+height*.25f,cellW*.27f,height*.48f),vertical,VerticalUV[i],Color.white);
   Rect nameRect=new Rect(cell.x+cellW*.40f,cell.y+height*.19f,cellW*.48f,height*.26f);
   string name=RoleCatalog.All[i].Name;float nameSize=Mathf.Clamp(cellW/12,11,16);
   while(nameSize>9&&Style(nameSize,Color.white,TextAnchor.MiddleCenter,true).CalcHeight(new GUIContent(name),nameRect.width)>nameRect.height)nameSize--;
   Text(nameRect,name,nameSize,Color.white,TextAnchor.MiddleCenter,true);
   int owners=0,overlaps=0;foreach(PlayerSlot p in s.Players){RoleState st=RoleSystem.State(p);if(st.Chosen==i){int column=owners%4,rr=owners/4;Text(new Rect(cell.x+cellW*.42f+column*cellW*.11f,cell.y+height*.42f+rr*13,cellW*.11f,17),"P"+(p.Index+1),14,p.Color);owners++;}}
   foreach(PlayerSlot p in s.Players)if(Charms.NativeCursor[p.Index]==100+i){Selection(new Rect(cell.x+2,cell.y+1,cell.width-4,cell.height-2),p.Color,overlaps++,true);Text(new Rect(cell.x+14+(overlaps-1)*22,cell.y-3,25,19),"P"+(p.Index+1),14,p.Color);}
  }
  PlayerSlot described=null;foreach(PlayerSlot candidate in s.Players)if(candidate.Index==RoleSystem.LastMoved)described=candidate;
  if(described!=null){RoleState state=RoleSystem.State(described);int id=Charms.NativeCursor[described.Index]>=100?state.Cursor:state.Chosen;Text(new Rect(panel.x+12,panel.yMax-136,panel.width-24,24),"P"+(described.Index+1)+"  ·  "+RoleCatalog.All[id].Name,14,described.Color);Text(new Rect(panel.x+14,panel.yMax-106,panel.width-28,71),RoleCatalog.All[id].Description,12,new Color(.87f,.88f,.91f),TextAnchor.UpperLeft,true);}
  Text(new Rect(panel.x+7,panel.yMax-29,panel.width-14,24),RoleText.Get("return_charms"),12,new Color(.72f,.73f,.8f),TextAnchor.MiddleCenter,true);
 }
 // Keep equipped charms and notches visible; replace only the description/name while this player browses roles.
 internal static void CardOverlay(PlayerSlot p,PlayerData data,Rect r){RoleState st=RoleSystem.State(p);bool browsing=Charms.NativeCursor[p.Index]>=100;
  if(!browsing)return;
  RoleDefinition role=RoleCatalog.All[st.Cursor];float header=Mathf.Clamp(r.height*.19f,15,25);
  Box(new Rect(r.x+37,r.y+2,r.width-42,header+1),new Color(.015f,.013f,.023f,1));Text(new Rect(r.x+40,r.y+3,r.width-48,header),role.Name,13,p.Color,TextAnchor.MiddleLeft);
  if(r.height>108){Rect desc=CharmCardUI.DescriptionRect(p,r);if(desc.height>15){Box(desc,new Color(.015f,.013f,.023f,1));Text(desc,role.Description,Mathf.Clamp(r.height/24,9,12),new Color(.92f,.91f,.95f),TextAnchor.UpperLeft,true);}}

 }
 internal static void BeforeCamera(Camera camera){if(!Charms.NativeMenuOpen){RestoreVanilla();return;}var gc=GameCameras.instance;if(gc==null||camera!=gc.hudCamera)return;GameObject pane=Charms.NativePane;if(!pane)return;
  if(pane!=hiddenPane){RestoreVanilla();hiddenPane=pane;nextHide=0;}
  if(Time.unscaledTime>=nextHide){nextHide=Time.unscaledTime+.5f;foreach(Renderer renderer in pane.GetComponentsInChildren<Renderer>(true)){if(renderer==null)continue;Vector3 v=camera.WorldToViewportPoint(renderer.bounds.center);Vector3 max=camera.WorldToViewportPoint(renderer.bounds.max);Vector3 min=camera.WorldToViewportPoint(renderer.bounds.min);
   // Spatially restrict to the vanilla right-hand description, preserving borders, grid and pane navigation.
   if(v.z>0&&v.x>.60f&&v.x<.94f&&v.y>.15f&&v.y<.83f&&Mathf.Abs(max.x-min.x)<.38f){if(!hidden.ContainsKey(renderer))hidden[renderer]=renderer.forceRenderingOff;}}}
  foreach(var item in hidden)if(item.Key)item.Key.forceRenderingOff=true;
 }
 internal static void RestoreVanilla(){foreach(var item in hidden)if(item.Key)item.Key.forceRenderingOff=item.Value;hidden.Clear();hiddenPane=null;}
 internal static void Release(){RestoreVanilla();if(horizontal)UnityEngine.Object.Destroy(horizontal);if(vertical)UnityEngine.Object.Destroy(vertical);if(frame)UnityEngine.Object.Destroy(frame);if(verticalFrame)UnityEngine.Object.Destroy(verticalFrame);horizontal=vertical=frame=verticalFrame=null;nativeFont=null;}
}
}
