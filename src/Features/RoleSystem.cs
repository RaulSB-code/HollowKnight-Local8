using System;
using System.Collections.Generic;
using InControl;
using UnityEngine;
namespace KO.HollowKnight8 {
internal sealed class RoleState {
 internal PlayerSlot Player;
 internal string Participant;
 internal int Chosen, Cursor, LastCharm=1, Direction;
 internal bool PendingJoin, SubmitHeld, CancelHeld;
 internal float NextMove;
 internal readonly MageSoulRules SoulDrops=new MageSoulRules();
 internal HeroController StatsHero;
 internal int AppliedMasks=-1;
 internal readonly RoleFocusCycle FocusCycle=new RoleFocusCycle();
}
internal static class RoleSystem {
 internal static readonly RoleState[] States=new RoleState[8];
 internal static PlayerSlot Joining;
 internal static int LastMoved;
 static CoopSession owner;
 static readonly RoleSelectionMemory selections=new RoleSelectionMemory();
 static float oldScale=1f, openedAt, releaseAt;
 static bool paused, installed;
 internal static bool JoiningMenu { get {return Joining!=null;} }
 internal static bool SelectionActive {get{return JoiningMenu && Plugin.Self!=null && !Plugin.Self.Panel && GameManager.instance!=null && !GameManager.instance.isPaused;}}
 // Choosing a role only suspends its owner. Pausing the entire session left P3/P4
 // unable to use their own keyboard (including the numeric pad) while P2's
 // first-time role choice was waiting for input.
 internal static bool PauseSession(CoopSession s){return false;}
 internal static CoopSession Session {get {return Plugin.Self==null?null:Plugin.Self.Session;}}
 internal static RoleState State(PlayerSlot p) {
  if(p==null||p.Index<0||p.Index>=States.Length)return null;
  RoleState x=States[p.Index];
  if(x==null||x.Player!=p){EnsureSave();string id=Controls.IsKeyboard(p.Device)?"keyboard-slot:"+p.Index:Controls.Key(p.Device);int chosen;
   chosen=selections.Role(id,RoleCatalog.Index(Local8Mod.Save.RoleIds[p.Index]));
   x=new RoleState{Player=p,Participant=id,Chosen=chosen,PendingJoin=selections.NeedsSelection(id,Local8Mod.Save.RoleIds[p.Index])};x.Cursor=x.Chosen;States[p.Index]=x;
   if(p.Index==3&&Controls.IsKeyboard(p.Device)&&p.Actions!=null)
    Diagnostics.Write("KEYBOARD P4 move="+Binding(p.Actions.left)+"/"+Binding(p.Actions.right)+"/"+Binding(p.Actions.up)+"/"+Binding(p.Actions.down));}
  return x;
 }
 static string Binding(PlayerAction a){return a==null||a.Bindings.Count==0?"none":a.Bindings[0].Name;}
 internal static RoleDefinition Definition(PlayerSlot p) {
  return p==null||Session==null||!Session.Active?RoleCatalog.All[0]:RoleCatalog.All[State(p).Chosen];
 }
 static void EnsureSave(){if(Local8Mod.Save.RoleIds==null||Local8Mod.Save.RoleIds.Length!=8){var a=new string[8];if(Local8Mod.Save.RoleIds!=null)Array.Copy(Local8Mod.Save.RoleIds,a,Math.Min(8,Local8Mod.Save.RoleIds.Length));Local8Mod.Save.RoleIds=a;}}
 internal static void UnlockOvercharm(PlayerData data){if(data!=null&&!data.canOvercharm)data.canOvercharm=true;}
 internal static void Install(){if(installed)return;installed=true;KeypadInput.Install();RoleEffects.Install();PartySoul.Install();RumbleRouting.Install();BossMusicGuard.Install();SpellSafety.Install();JumpInputIsolation.Install();PvpArena.Install();Camera.onPreCull+=RoleUI.BeforeCamera;}
 internal static void Shutdown(){PvpArena.Shutdown();KeypadInput.Uninstall();ReleasePause();RoleUI.Release();JoinHint.Reset();SoulDisplay.Clear();PhysicsGuard.Reset();RoleEffects.Uninstall();PartySoul.Uninstall();RumbleRouting.Uninstall();BossMusicGuard.Uninstall();SpellSafety.Uninstall();JumpInputIsolation.Uninstall();CoopBath.Reset();Camera.onPreCull-=RoleUI.BeforeCamera;installed=false;Array.Clear(States,0,8);owner=null;Joining=null;selections.Clear();}
 internal static void Reset(){PartySoul.Reset();JoinHint.Reset();RumbleRouting.StopAll();PhysicsGuard.Reset();BossMusicGuard.Reset();CoopBath.Reset();ReleasePause();Joining=null;releaseAt=0;RoleUI.RestoreVanilla();RoleEffects.ClearAuras();Array.Clear(States,0,8);owner=null;selections.Clear();}
 static void ReleasePause(){if(paused){paused=false;GameManager gm=GameManager.instance;if(gm==null||!gm.isPaused){if(Time.timeScale==0)Time.timeScale=oldScale>0?oldScale:1f;}}}
 static bool Ready(PlayerSlot p){return p!=null&&p.Connected&&p.Ready&&p.Alive&&!p.InputBlocked&&!p.ArenaTransfer&&!p.SpawnPending;}
 internal static void Tick(Local8Runtime runtime) {
  try {
   CoopSession s=runtime.Session;
   if(s!=owner){Reset();owner=s;}
   JoinHint.Tick(runtime);CharmMenuExit.Tick();if(runtime.Enabled.Value){RespawnSlider.Normalize(runtime);if(s!=null&&s.Active&&s.Data!=null&&!s.Data.canOvercharm){s.Data.canOvercharm=true;Diagnostics.Write("CHARMS overcharm unlocked for coop");}PartySoul.Sync(s);PhysicsGuard.Tick(s);CoopBath.Tick(s);}
   if(!runtime.Enabled.Value||s==null||!s.Active||!s.Gameplay){ReleasePause();Joining=null;RoleAura.Hide();return;}
   GameManager gm=GameManager.instance;
   if(gm==null||gm.IsLoadingSceneTransition||!gm.HasFinishedEnteringScene){ReleasePause();Joining=null;RoleAura.Hide();return;}
   for(int i=0;i<8;i++)if(States[i]!=null&&!s.Players.Contains(States[i].Player)){if(Joining==States[i].Player)Joining=null;States[i]=null;}
   foreach(PlayerSlot p in s.Players){p.Connected=Controls.IsKeyboard(p.Device)||(p.Device!=null&&p.Device.IsAttached);State(p);}
   foreach(PlayerSlot p in s.Players)if(p.Index==3&&Controls.IsKeyboard(p.Device)&&p.Actions!=null){
    string key=Input.GetKeyDown(KeyCode.Keypad4)?"4":Input.GetKeyDown(KeyCode.Keypad6)?"6":Input.GetKeyDown(KeyCode.Keypad8)?"8":Input.GetKeyDown(KeyCode.Keypad5)?"5":null;
    if(key!=null)Diagnostics.Write("KEYBOARD P4 pad="+key+" action="+Direction(p)+" blocked="+p.InputBlocked+" selecting="+(Joining==p));
   }
   if(Joining!=null&&(!Joining.Connected||!Joining.Alive||!Joining.Ready)){Joining=null;ReleasePause();}
   if(PvpArena.Active){ReleasePause();Joining=null;RoleEffects.Tick(s);return;}
   // Never steal input from a native conversation, pause screen or inventory page.
   if(runtime.Panel||gm.isPaused||Charms.NativeMenuOpen||ScriptedParty.Active||CoopEnding.Active||PickupCard.Owner!=null||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null){ReleasePause();RoleAura.Hide();return;}
   if(Joining==null&&Time.unscaledTime>=releaseAt&&s.Players.Count>1&&gm.IsGameplayScene()&&UIManager.instance!=null&&(int)UIManager.instance.uiState==4){
    bool waitingSpawn=false;foreach(PlayerSlot p in s.Players)if(p.SpawnPending||!p.Ready)waitingSpawn=true;
    if(!waitingSpawn)foreach(PlayerSlot p in s.Players)if(State(p).PendingJoin&&Ready(p)&&p.Hero.CanInput()&&!p.Hero.cState.transitioning){Joining=p;openedAt=Time.unscaledTime;State(p).Cursor=State(p).Chosen;Prime(State(p));Diagnostics.Write("ROLE selection P"+(p.Index+1)+"; other players retain input");break;}
   }
   if(Joining!=null)JoinInput(Joining);
   else ReleasePause();
   RoleEffects.Tick(s);
  }catch(Exception e){Diagnostics.Throttled("ROLES",e);Joining=null;ReleasePause();}
 }
 internal static bool BlockHero(HeroController hero){return SelectionActive&&Joining!=null&&Joining.Hero==hero;}
 internal static void Prime(RoleState x){x.NextMove=Time.unscaledTime+.25f;x.Direction=0;x.SubmitHeld=true;x.CancelHeld=true;}
 internal static int Direction(PlayerSlot p){HeroActions a=p.Actions;if(a==null)return 0; // Already bound to this player's keyboard profile or device by Controls.
  return a.left.IsPressed?-1:a.right.IsPressed?1:a.up.IsPressed?2:a.down.IsPressed?3:0;
 }
 internal static bool Submit(PlayerSlot p){HeroActions a=p.Actions;return a!=null&&(int)Platform.Current.GetMenuAction(a.menuSubmit.IsPressed,a.menuCancel.IsPressed,a.jump.IsPressed,a.attack.IsPressed,a.cast.IsPressed)==1;}
 internal static bool Cancel(PlayerSlot p){HeroActions a=p.Actions;return a!=null&&(int)Platform.Current.GetMenuAction(a.menuSubmit.IsPressed,a.menuCancel.IsPressed,a.jump.IsPressed,a.attack.IsPressed,a.cast.IsPressed)==2;}
 internal static bool MoveDue(RoleState x,int d){bool result=d!=0&&(d!=x.Direction||Time.unscaledTime>=x.NextMove);if(result){x.NextMove=Time.unscaledTime+(d==x.Direction?.15f:.30f);}x.Direction=d;return result;}
 static void JoinInput(PlayerSlot p){RoleState x=State(p);if(Time.unscaledTime-openedAt<.25f)return;
  int d=Direction(p);if(MoveDue(x,d))x.Cursor=RoleCatalog.JoinNext(x.Cursor,d==-1?-1:d==1?1:0,d==2?1:d==3?-1:0);
  bool confirm=Submit(p),cancel=Cancel(p);
  if(confirm&&!x.SubmitHeld){Choose(p,x.Cursor,true);FinishJoin(x);}
  else if(cancel&&!x.CancelHeld){Choose(p,0,true);FinishJoin(x);}
  x.SubmitHeld=confirm;x.CancelHeld=cancel;
 }
 internal static void FinishJoin(RoleState x){x.PendingJoin=false;selections.Complete(x.Participant,x.Chosen);Joining=null;releaseAt=Time.unscaledTime+.2f;ReleasePause();}
 internal static bool Choose(PlayerSlot p,int index,bool joining){
  if(p==null||index<0||index>=RoleCatalog.All.Length||(!joining&&!Charms.CanEdit(p)))return false;
  RoleState x=State(p);selections.Change(x.Participant,index);if(x.Chosen==index){EnsureSave();Local8Mod.Save.RoleIds[p.Index]=RoleCatalog.All[index].Id;return true;}
  x.Chosen=index;EnsureSave();Local8Mod.Save.RoleIds[p.Index]=RoleCatalog.All[index].Id;
  CoopSession s=Session;
  if(s!=null&&p.Hero){using(PlayerContext.Enter(p)){Charms.UpdateMaximum(p,s.Data,false);p.Vitals.Write(s.Data);}}
  RoleEffects.RefreshAura(p);
  Diagnostics.Write("ROLE P"+(p.Index+1)+"="+RoleCatalog.All[index].Id);return true;
 }
 internal static int Maximum(int value,PlayerSlot p){return value+Definition(p).Masks;}
 internal static void NativeInput(CoopSession s){
  if(!Charms.wasNative){Charms.wasNative=true;for(int i=0;i<8;i++){if(Charms.NativeCursor[i]<1||Charms.NativeCursor[i]>40)Charms.NativeCursor[i]=Charms.FirstOwned(s.Data);}
   foreach(PlayerSlot p in s.Players){RoleState x=State(p);x.Cursor=x.Chosen;Prime(x);x.LastCharm=Charms.NativeCursor[p.Index];}}
  Charms.Freeze(s);
  foreach(PlayerSlot p in s.Players){if(!p.Alive||!p.Ready||!p.Connected||p.Actions==null)continue;RoleState x=State(p);int d=Direction(p);
   bool inRoles=Charms.NativeCursor[p.Index]>=100;
   if(MoveDue(x,d)){
    LastMoved=p.Index;
    if(inRoles){int next=RoleCatalog.VerticalNext(x.Cursor,d==-1?-1:d==1?1:0,d==2?1:d==3?-1:0);if(next<0){Charms.NativeCursor[p.Index]=x.LastCharm;inRoles=false;}else {x.Cursor=next;Charms.NativeCursor[p.Index]=100+next;}}
    else {int old=Charms.NativeCursor[p.Index],next=Charms.Neighbor(old,d==-1?-1:d==1?1:0,d==2?1:d==3?-1:0,s.Data);if(d==1&&next==old){x.LastCharm=old;x.Cursor=x.Chosen;Charms.NativeCursor[p.Index]=100+x.Cursor;inRoles=true;}else {Charms.NativeCursor[p.Index]=next;x.LastCharm=next;}}
   }
   bool submit=Submit(p);if(submit&&!x.SubmitHeld){if(inRoles)Choose(p,x.Cursor,false);else Charms.Toggle(p,Charms.NativeCursor[p.Index]);}
   x.SubmitHeld=submit;
  }
 }
}
}
