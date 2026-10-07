using System;
using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpCharms {
 sealed class Before {internal PlayerSlot Player;internal CharmLoadout Charms;internal int Maximum,Joni;}
 static readonly List<Before> before=new List<Before>();static CoopSession session;static bool allowed;
 internal static bool Active {get{return session!=null;}}
 internal static bool Allowed {get{return Active&&allowed;}}
 static CharmLoadout Copy(CharmLoadout source){var c=new CharmLoadout();Array.Copy(source.Equipped,c.Equipped,40);c.Order=new List<int>(source.Order);c.Used=source.Used;c.Overcharmed=source.Overcharmed;return c;}
 internal static void Begin(CoopSession s){
  End();if(s==null||!s.Active)return;session=s;allowed=PvpArena.Options.AllowCharms;
  foreach(var p in s.Players)before.Add(new Before{Player=p,Charms=Copy(p.Charms),Maximum=p.Vitals.MaxHealth,Joni=p.Vitals.Joni});
  if(!allowed)foreach(var p in s.Players){p.Charms=new CharmLoadout();try{Refresh(p);}catch(Exception ex){Diagnostics.Throttled("PVP charms disable",ex);}}
 }
 static void Refresh(PlayerSlot p){if(session==null||!p.Hero)return;using(PlayerContext.Enter(p)){p.Hero.CharmUpdate();p.Capture(session.Data);}SummonRouting.Refresh(p);}
 internal static void End(){
  if(session==null)return;var s=session;
  // Restore equipment before the original match restores its vital snapshots.
  foreach(var e in before){var p=e.Player;if(!s.Players.Contains(p))continue;p.Charms=Copy(e.Charms);int health=p.Vitals.Health,blue=p.Vitals.Blue,soul=p.Vitals.Soul,reserve=p.Vitals.Reserve;
   try{Refresh(p);}catch(Exception ex){Diagnostics.Throttled("PVP charms restore",ex);}
   p.Vitals.MaxHealth=e.Maximum;p.Vitals.Joni=e.Joni;p.Vitals.Health=health;p.Vitals.Blue=blue;p.Vitals.Soul=soul;p.Vitals.Reserve=reserve;s.Commit(p);
  }
  session=null;before.Clear();allowed=false;PvpFamiliars.Reset();
 }
 internal static int NaturalHealth(PlayerSlot p,int fallback){if(!Active||p==null)return fallback;return PvpCharmRules.Natural(allowed,p.Charms.Equipped[22],session.Data.brokenCharm_23,session.Data.maxHealthBase,RoleSystem.Definition(p).Masks);}
 internal static int Lifeblood(PlayerSlot p){return p==null?0:PvpCharmRules.Lifeblood(Allowed,p.Charms.Equipped[7],p.Charms.Equipped[8]);}
 internal static int Joni(PlayerSlot p,int health){return p==null?0:PvpCharmRules.Joni(Allowed,p.Charms.Equipped[26],health);}
 internal static void NormalRound(CoopSession s){if(!Active||PvpArena.Active||s!=session)return;
  foreach(var e in PvpMatch.roster){var p=e.Player;p.Vitals.Blue=allowed?Math.Max(e.Blue,p.Vitals.Joni+Lifeblood(p)):0;p.Vitals.DamagedBlue=false;s.Commit(p);}
  PvpFamiliars.Round();
 }
 internal static bool Save(CoopSession s){if(s!=session||!Active)return false;if(Local8Mod.Save.Charms==null||Local8Mod.Save.Charms.Length!=8)Local8Mod.Save.Charms=new int[8][];foreach(var e in before)Local8Mod.Save.Charms[e.Player.Index]=e.Charms.Ids();return true;}
 // Vanilla serialization and the existing SaveScope must see actual adventure
 // equipment, never the temporary empty loadout used by a no-charms duel.
 sealed class SaveScope:IDisposable {
  readonly CoopSession owner;readonly CharmLoadout data;readonly Dictionary<PlayerSlot,CharmLoadout> temporary=new Dictionary<PlayerSlot,CharmLoadout>();
  internal SaveScope(){owner=session;if(owner==null)return;data=new CharmLoadout();data.Read(owner.Data);foreach(var e in before){temporary[e.Player]=e.Player.Charms;e.Player.Charms=Copy(e.Charms);}owner.Primary.Charms.Write(owner.Data);}
  public void Dispose(){if(owner==null||session!=owner)return;foreach(var item in temporary)if(owner.Players.Contains(item.Key))item.Key.Charms=item.Value;data.Write(owner.Data);}
 }
 static bool installed;
 internal static void Install(){if(installed)return;installed=true;On.GameManager.SaveGame+=SaveGame;On.GameManager.SaveGame_Action1+=SaveCallback;On.GameManager.SaveGame_int_Action1+=SaveSlot;PvpFamiliars.Install();}
 internal static void Uninstall(){End();if(!installed)return;installed=false;On.GameManager.SaveGame-=SaveGame;On.GameManager.SaveGame_Action1-=SaveCallback;On.GameManager.SaveGame_int_Action1-=SaveSlot;PvpFamiliars.Uninstall();}
 static void SaveGame(On.GameManager.orig_SaveGame orig,GameManager gm){using(new SaveScope())orig(gm);}
 static void SaveCallback(On.GameManager.orig_SaveGame_Action1 orig,GameManager gm,Action<bool> done){using(new SaveScope())orig(gm,done);}
 static void SaveSlot(On.GameManager.orig_SaveGame_int_Action1 orig,GameManager gm,int slot,Action<bool> done){using(new SaveScope())orig(gm,slot,done);}
}
}
