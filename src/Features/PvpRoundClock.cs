using System;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpRoundClock {
 internal static void Migrate(){var c=Local8Mod.Settings;if(c.RoundTimerInitialized)return;if(c.DuelSeconds==180)c.DuelSeconds=0;c.RoundTimerInitialized=true;}
 // Infinity survives the existing subtraction and timeout comparison.
 internal static float Duration(int seconds){return RoundTimeRules.Duration(seconds);}
 internal static string Format(float seconds){return RoundTimeRules.Format(seconds);}
 internal static bool Visible(float seconds){return RoundTimeRules.Visible(seconds);}
 internal static void Number(string label,ref int value,int min,int max,int step,float width,ref float row){
  Migrate();var style=new GUIStyle(GUI.skin.label){fontSize=14};style.normal.textColor=Color.white;
  GUI.Label(new Rect(8,row+5,width-170,28),label+": "+(value<=0?UiLocalization.Get("pvp.unlimited"):value.ToString()),style);
  var button=new GUIStyle(GUI.skin.button){fontSize=14};
  if(GUI.Button(new Rect(width-158,row,72,30),"-",button))value=Mathf.Max(0,value-30);
  if(GUI.Button(new Rect(width-80,row,72,30),"+",button))value=Mathf.Min(600,value+30);
  row+=39;
 }
 internal static string Banner(string original){
  if(PvpArena.Active)return PvpArena.Banner(original);
  int state=PvpMatch.state,round=PvpMatch.round;float left=PvpMatch.remaining;
  if(Local8Mod.Settings.PvpMode==0)return "";
  if(Local8Mod.Settings.PvpMode==1)return UiLocalization.Get("pvp.friendly");
  if(state==0)return UiLocalization.Get("pvp.duel")+": "+ActionKeys.PvpLabel+" / F8 > PvP";
  if(state==4)return PvpMatch.Result;
  if(state==3)return PvpMatch.Result+"  |  "+UiLocalization.Get("pvp.next_round")+Mathf.Max(1,Mathf.CeilToInt(left));
  return UiLocalization.Get("pvp.round")+" "+round+(state==1?"  -  "+Mathf.Max(1,Mathf.CeilToInt(left)):"");
 }
 internal static void Draw(Local8Runtime runtime){
  if(runtime==null||runtime.Panel||runtime.Session==null||!runtime.Session.Active||!runtime.Session.Gameplay||Charms.NativeMenuOpen||RoleSystem.JoiningMenu||!GameManager.instance||GameManager.instance.isPaused||GameManager.instance.IsLoadingSceneTransition)return;
  float remaining=PvpArena.Active?PvpArena.ClockSeconds:(PvpMatch.state==2?PvpMatch.remaining:float.NaN);
  if(!Visible(remaining))return;
  Matrix4x4 old=GUI.matrix;Color color=GUI.color;int depth=GUI.depth;
  try{float scale=Screen.height/900f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));GUI.depth=-10000;RoleUI.ClockText(new Rect(Screen.width/scale-200,35,165,50),Format(remaining));}
  finally{GUI.matrix=old;GUI.color=color;GUI.depth=depth;}
 }
}
}
