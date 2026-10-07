using System;
namespace KO.HollowKnight8 {
internal static class DreamRescueRules {
 internal static float ArrivalTime(float distance){return Math.Max(.12f,Math.Min(.45f,distance/55f));}
 internal static float ArrivalProgress(float elapsed,float duration){float t=Math.Max(0,Math.Min(1,elapsed/duration));return t*t*(3-2*t);}
 internal static float Follow(float from,float to,float dt,float speed){
  if(!CameraPresenceRules.Finite(from)||!CameraPresenceRules.Finite(to))return from;
  dt=Math.Max(0,Math.Min(.05f,dt));float change=(to-from)*(1-(float)Math.Exp(-dt/.28f));float limit=speed*dt;
  return from+Math.Max(-limit,Math.Min(limit,change));
 }
 internal static bool Offscreen(float cx,float cy,float hx,float hy,float minX,float maxX,float minY,float maxY){
  // A partly visible knight is still on screen. A small outward margin avoids
  // messages caused by an animated foot/head clipping the viewport edge.
  return maxX<cx-hx-.6f||minX>cx+hx+.6f||maxY<cy-hy-.6f||minY>cy+hy+.6f;
 }
}
internal sealed class RescueHintEpisode {
 internal bool Offscreen,Announced;internal float Since=-1,ClearSince=-1,Next;
 internal void Observe(bool outside,float now){
  Offscreen=outside;
  if(outside){ClearSince=-1;if(Since<0)Since=now;}
  else{Since=-1;if(ClearSince<0)ClearSince=now;if(now-ClearSince>=3f)Announced=false;}
 }
 internal bool Due(float now){return Offscreen&&!Announced&&Since>=0&&now-Since>=1.25f&&now>=Next;}
 internal void Announce(float now){Announced=true;Next=now+75f;}
}
}
