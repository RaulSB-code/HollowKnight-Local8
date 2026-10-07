using UnityEngine;
namespace KO.HollowKnight8 {
internal static class JumpInputIsolation {
 static readonly System.Reflection.FieldInfo inputField=Reflect.Field(typeof(HeroController),"inputHandler");
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;On.HeroController.LookForInput+=Input;On.HeroController.LookForQueueInput+=Queue;On.HeroController.JumpReleased+=Released;}
 internal static void Uninstall(){if(!installed)return;installed=false;On.HeroController.LookForInput-=Input;On.HeroController.LookForQueueInput-=Queue;On.HeroController.JumpReleased-=Released;}
 static PlayerSlot Owner(HeroController hero){var s=RoleSystem.Session;return s!=null&&s.Active?s.Resolve(hero):null;}
 static void Input(On.HeroController.orig_LookForInput orig,HeroController hero){
  var p=Owner(hero);using(PlayerContext.Enter(p)){
   var input=inputField.GetValue(hero) as InputHandler;var old=input?input.inputActions:null;
   try{if(input&&p!=null&&p.Actions!=null)input.inputActions=p.Actions;orig(hero);}finally{if(input)input.inputActions=old;}
  }
 }
 static void Queue(On.HeroController.orig_LookForQueueInput orig,HeroController hero){
  var p=Owner(hero);using(PlayerContext.Enter(p)){
   var input=inputField.GetValue(hero) as InputHandler;var old=input?input.inputActions:null;
   try{if(input&&p!=null&&p.Actions!=null)input.inputActions=p.Actions;orig(hero);}finally{if(input)input.inputActions=old;}
  }
 }
 static void Released(On.HeroController.orig_JumpReleased orig,HeroController hero){
  var p=Owner(hero);
  // Do not alter CancelJump: ceilings, maximum height, damage and scripted
  // movement retain their vanilla cancellation paths.
  if(p!=null&&p.Alive&&p.Ready&&!p.InputBlocked&&!p.Hazard&&!p.ArenaTransfer&&!hero.controlReqlinquished&&hero.cState.jumping&&!hero.cState.recoiling&&p.Actions!=null&&p.Actions.jump.IsPressed)return;
  using(PlayerContext.Enter(p)){orig(hero);}
 }
}
}
