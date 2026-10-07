using System;
namespace KO.HollowKnight8 {
// One pulse per completed focus. A cancelled charge or another source of health never pulses.
internal sealed class RoleFocusCycle {
 bool armed,sawStart;
 float started,lastPulse=-100f;
 internal void Begin(float now){armed=true;sawStart=true;started=now;}
 internal void Cancel(){armed=false;}
 internal bool Complete(float now,bool nativeHealState){
  if(now-lastPulse<.20f)return false;
  if(!armed&&(sawStart||!nativeHealState))return false;
  if(!nativeHealState&&(!armed||now-started>15f))return false;
  armed=false;sawStart=true;lastPulse=now;return true;
 }
}
internal static class RoleHealingRules {
 internal const float Radius=4.2f,Height=5.6f;
 internal static bool HealState(string state){
  if(string.IsNullOrEmpty(state))return false;
  return state.Equals("Focus Heal",StringComparison.Ordinal)||state.Equals("Focus Heal 2",StringComparison.Ordinal);
 }
 internal static bool Contains(float dx,float dy){
  if(dy<-.55f)return false;
  float y=Math.Max(0,dy)/Height,x=dx/Radius;
  return x*x+y*y<=1.00001f;
 }
 internal static bool CanReceive(bool alive,bool ready,bool opponent,bool unavailable,int hp,int max,float dx,float dy){
  return alive&&ready&&!opponent&&!unavailable&&hp>0&&hp<max&&Contains(dx,dy);
 }
}
}
