using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class SoulDisplay {
 static readonly Dictionary<int,Texture2D> broken=new Dictionary<int,Texture2D>();
 internal static int Capacity(Vitals v){return v.SoulLimited?99:Mathf.Max(1,v.MaxSoul);}
 internal static void Clear(){foreach(var tex in broken.Values)if(tex)Object.Destroy(tex);broken.Clear();}
 static Texture2D Cut(Texture source){
  int id=source.GetInstanceID();Texture2D result;if(broken.TryGetValue(id,out result)&&result)return result;
  int w=source.width,h=source.height;
  if(w<2||h<2||w>2048||h>2048)return null;
  RenderTexture rt=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32);
  RenderTexture previous=RenderTexture.active;
  try{
   Graphics.Blit(source,rt);RenderTexture.active=rt;
   result=new Texture2D(w,h,TextureFormat.ARGB32,false);
   result.ReadPixels(new Rect(0,0,w,h),0,0,false);
   Color32[] pixels=result.GetPixels32();
   // One continuous alpha cut, with a few shallow teeth across the broken lip.
   for(int x=0;x<w;x++){
    float tooth=Mathf.Abs((x%Mathf.Max(3,w/8))/(float)Mathf.Max(3,w/8)*2f-1f);
    int lip=Mathf.RoundToInt(h*(.66f+.055f*tooth));
    for(int y=lip;y<h;y++)pixels[y*w+x].a=0;
   }
   result.SetPixels32(pixels);result.Apply(false,true);
   result.filterMode=source.filterMode;result.wrapMode=TextureWrapMode.Clamp;
   broken[id]=result;return result;
  }catch(System.Exception e){Diagnostics.Throttled("SOUL CUT",e);if(result)Object.Destroy(result);return null;}
  finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
 }
 internal static void Icon(Texture texture,Rect rect,Color tint,PlayerSlot p){
  if(!p.Vitals.SoulLimited||!texture){Hud.Icon(texture,rect,tint);return;}
  Texture2D cut=Cut(texture);Hud.Icon(cut?cut:texture,rect,tint);
 }
}
}
