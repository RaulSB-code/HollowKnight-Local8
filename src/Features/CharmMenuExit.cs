using InControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KO.HollowKnight8 {
internal static class CharmMenuExit {
 static float until;
 static bool pending;
 // The inventory pane can still be active and inside the viewport during a
 // transition to another UI.  Its position alone must not own menu input.
 internal static bool NativeActive(bool visible) {
  if(!visible)return false;
  var gm=GameManager.instance;
  if(gm&&(gm.isPaused||gm.IsLoadingSceneTransition))return false;
  var interacting=InteractionRouter.ActivePlayer;
  if(ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||interacting!=null&&!interacting.Vitals.AtBench||PickupCard.Owner!=null)return false;
  if(gm&&gm.inventoryFSM){
   var inventory=gm.inventoryFSM;
   if(!inventory.isActiveAndEnabled||!inventory.gameObject.activeInHierarchy)return false;
   string state=inventory.ActiveStateName;
   if(string.Equals(state,"Closed",System.StringComparison.OrdinalIgnoreCase)||string.Equals(state,"Close",System.StringComparison.OrdinalIgnoreCase)||string.Equals(state,"Inactive",System.StringComparison.OrdinalIgnoreCase))return false;
  }
  EventSystem events=EventSystem.current;
  GameObject selected=events==null?null:events.currentSelectedGameObject;
  GameObject pane=Charms.NativePane;
  return selected==null||pane==null||selected.transform==pane.transform||selected.transform.IsChildOf(pane.transform);
 }
 internal static void OnExit() {pending=true;until=Time.unscaledTime+20f;Tick();}
 internal static void Tick() {
  if(!pending)return;
  if(Time.unscaledTime>until){pending=false;return;}
  CoopSession session=Plugin.Self==null?null:Plugin.Self.Session;
  if(session==null||!session.Active||session.Primary==null||session.Primary.Actions==null||Charms.NativeMenuOpen||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null)return;
  HeroActions actions=session.Primary.Actions;
  bool rebound=false;
  foreach(InControlInputModule module in Object.FindObjectsOfType<InControlInputModule>()) {
   if(module==null)continue;
   rebound=true;
   module.MoveAction=actions.moveVector;
   module.SubmitAction=actions.menuSubmit;
   module.CancelAction=actions.menuCancel;
  }
  foreach(HollowKnightInputModule hk in Object.FindObjectsOfType<HollowKnightInputModule>()) {
   if(hk==null)continue;
   rebound=true;
   hk.MoveAction=actions.moveVector;
   hk.SubmitAction=actions.menuSubmit;
   hk.CancelAction=actions.menuCancel;
   hk.JumpAction=actions.jump;
   hk.AttackAction=actions.attack;
   hk.CastAction=actions.cast;
  }
  if(rebound)pending=false;
 }
}
}
