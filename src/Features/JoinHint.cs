using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace KO.HollowKnight8 {
internal static class JoinHint {
 static bool shown;
 static float elapsed,nextSearch;
 static TextMeshProUGUI label;
 static TMP_FontAsset font;
 internal static void Reset(){if(label)UnityEngine.Object.Destroy(label.gameObject);label=null;font=null;shown=false;elapsed=nextSearch=0f;}
 static void Hide(){if(label)label.enabled=false;}
 static bool Create(){
  UIManager ui=UIManager.instance;
  if(!ui||!ui.UICanvas||!ui.UICanvas.gameObject.activeInHierarchy)return false;
  if(!font&&Time.unscaledTime>=nextSearch){nextSearch=Time.unscaledTime+1f;
   foreach(TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())if(f&&(f.name.IndexOf("Trajan",StringComparison.OrdinalIgnoreCase)>=0||f.name.IndexOf("Perpetua",StringComparison.OrdinalIgnoreCase)>=0)){font=f;break;}
   if(!font)foreach(TMP_Text t in Resources.FindObjectsOfTypeAll<TMP_Text>())if(t&&t.font&&t.gameObject.scene.IsValid()){font=t.font;break;}
  }
  if(!font)return false;
  if(!label){var go=new GameObject("Local8 Join Hint",typeof(RectTransform));go.transform.SetParent(ui.UICanvas.transform,false);
   label=go.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=25f;label.alignment=(TextAlignmentOptions)5;label.raycastTarget=false;label.enableWordWrapping=false;
   RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.05f,.12f);rect.anchorMax=new Vector2(.95f,.18f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
  return true;
 }
 internal static void Tick(Local8Runtime runtime){
  Hide();if(shown||runtime==null||!runtime.Enabled.Value||runtime.Panel)return;
  CoopSession session=runtime.Session;GameManager gm=GameManager.instance;
  if(session!=null&&session.Players.Count>1){shown=true;Hide();return;}
  if(session==null||gm==null||!session.Gameplay||!session.CanJoin||session.Primary==null||!session.Primary.Ready||session.Players.Count!=1||gm.isPaused||Charms.NativeMenuOpen||ScriptedParty.Active||PickupCard.Owner!=null||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null||UIManager.instance==null||(int)UIManager.instance.uiState!=4)return;
  if(!Create())return;
  elapsed+=Time.unscaledDeltaTime;
  if(elapsed>=15f){shown=true;Hide();return;}
  float opacity=Mathf.Clamp01(Mathf.Min(elapsed/.5f,(15f-elapsed)/2f));
  label.text=RoleText.Get("join.hud_start");label.color=new Color(.95f,.93f,.86f,opacity);label.enabled=true;
 }
}
}
