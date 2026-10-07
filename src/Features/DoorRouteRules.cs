using System;
namespace KO.HollowKnight8 {
internal static class DoorRouteRules {
 // These are gate identifiers, not a whitelist of rooms. Modded room names remain valid.
 internal static bool GateName(string value){
  if(string.IsNullOrEmpty(value))return false;
  foreach(string prefix in new[]{"bot","top","left","right","door"}){
   if(!value.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||value.Length==prefix.Length)continue;
   bool digits=true;for(int i=prefix.Length;i<value.Length;i++)if(value[i]<'0'||value[i]>'9'){digits=false;break;}
   if(digits)return true;
  }
  return false;
 }
 internal static bool Resolve(string scene,string entry,string nativeScene,string nativeEntry,bool ambiguous,Func<string,bool> canLoad,out string resultScene,out string resultEntry){
  resultScene=null;resultEntry=null;if(ambiguous)return false;
  if(!string.IsNullOrEmpty(nativeScene)&&!string.IsNullOrEmpty(nativeEntry)){
   // The door FSM is the route the vanilla interaction actually executes.
   if(!GateName(nativeScene)||canLoad(nativeScene)){resultScene=nativeScene;resultEntry=nativeEntry;return true;}
  }
  if(string.IsNullOrEmpty(scene)||string.IsNullOrEmpty(entry))return false;
  if(!GateName(scene)||canLoad(scene)){resultScene=scene;resultEntry=entry;return true;}
  // Repair a reversed pair only with positive evidence that the other value is a scene.
  if(canLoad(entry)){resultScene=entry;resultEntry=scene;return true;}
  return false;
 }
}
}
