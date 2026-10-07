using System;
using System.Collections.Generic;
namespace KO.HollowKnight8 {
internal static class BenchSave {
 static readonly HashSet<PlayerSlot> resting=new HashSet<PlayerSlot>();
 internal static void Leave(PlayerSlot p){if(p!=null)resting.Remove(p);}
 internal static void Reset(){resting.Clear();}
 // Called only after the existing BenchRest validation and health/charms commit.
 internal static void Rest(CoopSession s,HeroController actor){
  var p=s.Resolve(actor);if(p==null||!resting.Add(p))return;
  try{if(GameManager.instance){GameManager.instance.SaveGame();Diagnostics.Write("BENCH SAVE requested P"+(p.Index+1));}}
  catch(Exception e){resting.Remove(p);Diagnostics.Throttled("BENCH SAVE",e);}
 }
}
}
