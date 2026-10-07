using System;
using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class MaskDisplay {
 struct Measurement {internal int Width,Height;internal Rect Visible;internal bool Valid;}
 static readonly Dictionary<int,Measurement> measurements=new Dictionary<int,Measurement>();
 internal static void Clear(){measurements.Clear();}
 static Measurement Measure(Texture2D texture){
  int id=texture.GetInstanceID();Measurement m;
  if(measurements.TryGetValue(id,out m)&&m.Width==texture.width&&m.Height==texture.height)return m;
  m=new Measurement{Width=texture.width,Height=texture.height};
  try{
   var pixels=texture.GetPixels32();int left=m.Width,right=-1,bottom=m.Height,top=-1;
   if(pixels.Length==m.Width*m.Height)for(int y=0;y<m.Height;y++)for(int x=0;x<m.Width;x++){
    // Ignore effectively invisible alpha tails, retaining the skin's artwork.
    if(pixels[y*m.Width+x].a<=8)continue;
    if(x<left)left=x;if(x>right)right=x;if(y<bottom)bottom=y;if(y>top)top=y;
   }
   if(right>=left&&top>=bottom){m.Visible=new Rect(left/(float)m.Width,bottom/(float)m.Height,(right-left+1)/(float)m.Width,(top-bottom+1)/(float)m.Height);m.Valid=true;}
  }catch(Exception e){Diagnostics.Throttled("HUD empty mask alignment",e);}
  measurements[id]=m;return m;
 }
 // GUI ScaleToFit centres the texture canvas, which need not centre its art.
 // Recover the full mask's actual visible footprint in screen coordinates.
 internal static Rect Footprint(Rect cell,int width,int height,Rect visible){
  float scale=Mathf.Min(cell.width/width,cell.height/height),w=width*scale,h=height*scale;
  float x=cell.x+(cell.width-w)*.5f,y=cell.y+(cell.height-h)*.5f;
  return new Rect(x+visible.x*w,y+(1-visible.y-visible.height)*h,visible.width*w,visible.height*h);
 }
 internal static void Icon(Texture texture,Rect cell,Color tint,PlayerSlot player){
  var images=player==null?null:HudAssets.For(player);
  // Filled masks, blue masks and native break frames keep their original draw.
  if(images==null||!images.Empty||texture!=images.Empty||!images.Mask){Hud.Icon(texture,cell,tint);return;}
  var full=Measure(images.Mask);var empty=Measure(images.Empty);
  if(!full.Valid||!empty.Valid){Hud.Icon(texture,cell,tint);return;}
  Rect target=Footprint(cell,full.Width,full.Height,full.Visible);
  Color previous=GUI.color;
  try{GUI.color=tint;GUI.DrawTextureWithTexCoords(target,texture,empty.Visible,true);}
  finally{GUI.color=previous;}
 }
}
}
