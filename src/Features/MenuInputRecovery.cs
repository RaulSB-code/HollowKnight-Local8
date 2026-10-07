using System.Collections.Generic;
using InControl;
using UnityEngine;

namespace KO.HollowKnight8 {
// Only repair references to the charm menu's private, neutral input view.
// A shop or another mod's legitimate bindings are never replaced by this guard.
internal static class MenuInputRecovery {
 static readonly HashSet<PlayerAction> neutral=new HashSet<PlayerAction>();
 static readonly HashSet<PlayerTwoAxisAction> vectors=new HashSet<PlayerTwoAxisAction>();
 static readonly HashSet<HeroActions> views=new HashSet<HeroActions>();
 static CoopSession owner;static float nextScan;
 internal static void Capture(){
  var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(owner!=s){Reset();owner=s;}
  var view=CharmNativeUi.view;if(view==null)return;
  views.Add(view);neutral.Add(view.menuSubmit);vectors.Add(view.moveVector);
 }
 static PlayerAction Repair(PlayerAction value,PlayerAction real){return value!=null&&neutral.Contains(value)?real:value;}
 static PlayerTwoAxisAction Repair(PlayerTwoAxisAction value,PlayerTwoAxisAction real){return value!=null&&vectors.Contains(value)?real:value;}
 internal static void Tick(){
  var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(s!=owner){Reset();owner=s;}
  if(s==null||!s.Active||s.Primary==null||s.Primary.Actions==null||views.Count==0||Time.unscaledTime<nextScan)return;
  nextScan=Time.unscaledTime+.25f;
  if(Charms.NativeMenuOpen||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null)return;
  var a=s.Primary.Actions;var gm=GameManager.instance;
  if(gm&&gm.inputHandler&&views.Contains(gm.inputHandler.inputActions))gm.inputHandler.inputActions=a;
  foreach(var module in Object.FindObjectsOfType<InControlInputModule>())if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
  }
  foreach(var module in Object.FindObjectsOfType<HollowKnightInputModule>())if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
   module.JumpAction=Repair(module.JumpAction,a.jump);module.AttackAction=Repair(module.AttackAction,a.attack);module.CastAction=Repair(module.CastAction,a.cast);
  }
 }
 internal static void Reset(){neutral.Clear();vectors.Clear();views.Clear();owner=null;nextScan=0;}
}
}
