using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Scenes=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
// Scream Get? reads a local Spell Control bool, set by a world trigger that
// originally targets just the singleton knight. Re-evaluate that exact native
// trigger for the casting body, before its BoolTest runs. Never synthesize an
// upgrade, cast, event, position or an ability flag.
internal static class ShadeShriekZone {
 sealed class Zone {
  internal Fsm Fsm;internal readonly List<Collider2D> Shapes=new List<Collider2D>();
  internal readonly HashSet<PlayerSlot> Contacts=new HashSet<PlayerSlot>();internal bool ContactKnown;
 }
 static readonly Dictionary<Fsm,Zone> zones=new Dictionary<Fsm,Zone>();
 static int scene=-1;static float nextScan;static bool warned;
 static bool Enabled(){var s=Plugin.Self?Plugin.Self.Session:null;return s!=null&&s.Active&&s.Players.Count>1&&Scenes.GetActiveScene().name=="Abyss_12";}
 static void Scope(){int handle=Scenes.GetActiveScene().handle;if(scene==handle)return;Reset();scene=handle;}
 static bool ZoneWrite(FsmStateAction action){
  var set=action as SetFsmBool;
  if(set!=null&&set.variableName!=null&&set.variableName.Value=="Scream 2 Zone"&&set.setValue!=null&&set.setValue.Value)return true;
  var local=action as SetBoolValue;
  return local!=null&&local.boolVariable!=null&&local.boolVariable.Name=="Scream 2 Zone"&&local.boolValue!=null&&local.boolValue.Value;
 }
 static Zone Register(Fsm f){
  if(f==null||!f.GameObject||f.GameObject.GetComponentInParent<HeroController>())return null;
  Zone z;if(zones.TryGetValue(f,out z))return z;
  bool native=false;if(f.States!=null)foreach(var state in f.States){if(state==null||state.Actions==null)continue;foreach(var a in state.Actions)if(ZoneWrite(a)){native=true;break;}if(native)break;}
  if(!native)return null;
  z=new Zone{Fsm=f};zones[f]=z;
  foreach(var c in f.GameObject.GetComponentsInChildren<Collider2D>(true))if(c&&c.isTrigger&&!c.GetComponentInParent<HeroController>())z.Shapes.Add(c);
  if(z.Shapes.Count==0&&f.GameObject.transform.parent)foreach(var c in f.GameObject.transform.parent.GetComponents<Collider2D>())if(c&&c.isTrigger)z.Shapes.Add(c);
  Diagnostics.Write("SHADE shriek zone object="+f.GameObject.name+" fsm="+f.Name+" colliders="+z.Shapes.Count);
  return z;
 }
 internal static void Action(FsmStateAction a){if(a==null||!Enabled()||!ZoneWrite(a))return;Scope();Register(a.Fsm);}
 internal static void Contact(PlayerSlot p,Fsm f,bool exit){
  if(p==null||!Enabled())return;Scope();var z=Register(f);if(z==null)return;
  z.ContactKnown=true;if(exit)z.Contacts.Remove(p);else z.Contacts.Add(p);
 }
 internal static void Before(FsmState state,PlayerSlot p){
  if(state==null||!Enabled())return;Scope();
  if(p==null||!p.Hero||!p.Hero.spellControl||state.Fsm!=p.Hero.spellControl.Fsm){Register(state.Fsm);return;}
  if(state.Name!="Scream Get?")return;
  if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.5f;foreach(var f in UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>())if(f)Register(f.Fsm);}
  bool known=zones.Count>0,inside=false;
  foreach(var z in zones.Values){
   if(!z.Fsm.GameObject||!z.Fsm.GameObject.activeInHierarchy)continue;
   bool geometry=false;
   foreach(var c in z.Shapes)if(c&&c.enabled&&c.gameObject.activeInHierarchy){known=geometry=true;if(c.OverlapPoint(p.Hero.transform.position))inside=true;}
   if(!geometry&&z.Shapes.Count==0&&z.ContactKnown)inside|=z.Contacts.Contains(p);
  }
  if(!known){if(!warned){warned=true;Diagnostics.Write("SHADE shriek zone unavailable; native condition retained");}return;}
  var flag=state.Fsm.Variables.FindFsmBool("Scream 2 Zone");
  if(flag!=null){
   flag.Value=inside;
   // A cloned action can retain the serialized reference from P1 even when
   // the clone's variable table is separate. Bind this one local BoolTest to
   // the caster's own variable; no other actor's zone bool is written.
   if(state.Actions!=null)foreach(var a in state.Actions){var test=a as BoolTest;if(test!=null&&test.boolVariable!=null&&test.boolVariable.Name=="Scream 2 Zone")test.boolVariable=flag;}
   Diagnostics.Write("SHADE shriek decision P"+(p.Index+1)+" inside="+inside);
  }
 }
 internal static void Reset(){zones.Clear();scene=-1;nextScan=0;warned=false;}
}
}
