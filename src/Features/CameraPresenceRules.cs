using System;
namespace KO.HollowKnight8 {
// Geometry only: the native camera still owns its lens, damping and room shot.
internal static class CameraPresenceRules {
 internal static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
 internal static bool Contains(float cx,float cy,float hx,float hy,float minX,float maxX,float minY,float maxY){
  return minX>=cx-hx&&maxX<=cx+hx&&minY>=cy-hy&&maxY<=cy+hy;
 }
 internal static float Contain(float center,float half,float low,float high,float roomLow,float roomHigh){
  float lo=high-half+.02f,hi=low+half-.02f;
  if(lo>hi)return (low+high)*.5f;
  float roomLo=roomLow+half,roomHi=roomHigh-half;
  if(roomLo<=roomHi){float a=Math.Max(lo,roomLo),b=Math.Min(hi,roomHi);if(a<=b){lo=a;hi=b;}}
  return Math.Max(lo,Math.Min(hi,center));
 }
 internal static bool ChangeLeader(float next,float previous,float elapsed){return next>previous+2f&&elapsed>=.8f;}
 // Slow only outward motion in the edge band. An already offscreen origin
 // may return at full speed; it is never snapped or allowed farther out.
 internal static float Leash(float from,float delta,float low,float high){
  if(!Finite(from)||!Finite(delta)||!Finite(low)||!Finite(high)||low>=high)return from;
  if(delta==0)return from;
  if(from<low)return delta<0?from:Math.Min(from+delta,high);
  if(from>high)return delta>0?from:Math.Max(from+delta,low);
  float edge=delta<0?from-low:high-from;
  float band=Math.Max(2f,Math.Min(5f,(high-low)*.22f));
  float ratio=Math.Max(0f,Math.Min(1f,edge/band));
  float factor=.03f+.97f*ratio*ratio;
  return Math.Max(low,Math.Min(high,from+delta*factor));
 }
}
}
