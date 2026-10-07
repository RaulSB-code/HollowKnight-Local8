using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;
namespace KO.HollowKnight8 {
internal static class RoleEffects {
 static bool installed,healing;
 internal const float LeechRadius=14f;
 internal static PlayerSlot Player(Component c){var s=RoleSystem.Session;return s==null||!s.Active?null:s.Resolve(c);}
 static bool Low(PlayerSlot p){return p!=null&&(PlayerContext.Current==p?RoleSystem.Session.Data.health:p.Vitals.Health)*2<=p.CurrentMaxHealth;}
 internal static void Install(){if(installed)return;installed=true;
  On.HealthManager.Hit+=Hit;On.HealthManager.ApplyExtraDamage+=ExtraDamage;
  On.HeroController.Update+=Update;
  On.HeroController.FixedUpdate+=Fixed;
  On.HeroController.StartMPDrain+=StartFocus;
  On.HeroController.DoAttack+=Attack;
  On.HeroController.HeroDash+=Dash;
  On.HeroController.Invulnerable+=Invulnerable;
  On.HeroController.TakeDamage+=Damage;
  On.HeroController.AddHealth+=Heal;
  On.PlayerData.AddHealth+=DataHeal;
  On.HutongGames.PlayMaker.FsmState.OnEnter+=StateEnter;
  RoleSummons.Install();
  On.HeroController.CharmUpdate+=CharmsChanged;
  On.PlayMakerFSM.Update+=FsmUpdate;
  On.InputHandler.Update+=InputUpdate;

 }
 internal static void Uninstall(){if(!installed)return;installed=false;
  On.HealthManager.Hit-=Hit;On.HealthManager.ApplyExtraDamage-=ExtraDamage;On.HeroController.Update-=Update;On.HeroController.FixedUpdate-=Fixed;On.HeroController.StartMPDrain-=StartFocus;On.HeroController.DoAttack-=Attack;On.HeroController.HeroDash-=Dash;On.HeroController.Invulnerable-=Invulnerable;On.HeroController.TakeDamage-=Damage;On.HeroController.AddHealth-=Heal;On.HeroController.CharmUpdate-=CharmsChanged;On.PlayMakerFSM.Update-=FsmUpdate;On.InputHandler.Update-=InputUpdate;On.PlayerData.AddHealth-=DataHeal;On.HutongGames.PlayMaker.FsmState.OnEnter-=StateEnter;RoleSummons.Uninstall();RoleAura.Release();
 }
 static void InputUpdate(On.InputHandler.orig_Update orig,InputHandler self){orig(self);}
 static void FsmUpdate(On.PlayMakerFSM.orig_Update orig,PlayMakerFSM self){orig(self);}
 static void Hit(On.HealthManager.orig_Hit orig,HealthManager enemy,HitInstance hit){var s=RoleSystem.Session;if(s==null||!s.Active){orig(enemy,hit);return;}PlayerSlot p;string kind;RoleSummons.Resolve(hit.Source,out p,out kind);if(p==null&&kind==null)p=PlayerContext.Current;RoleDefinition r=RoleSystem.Definition(p);
  float factor=kind!=null?r.Summons:(hit.AttackType==AttackTypes.Spell?r.Spell:(hit.AttackType==AttackTypes.Nail||hit.AttackType==AttackTypes.NailBeam?RoleCatalog.NailFactor(r,Low(p)):1f));
  // The existing co-op hook remains responsible for living-player damage scaling, exactly once.
  if(hit.DamageDealt>0)hit.DamageDealt=Mathf.Max(1,Mathf.RoundToInt(hit.DamageDealt*factor));int hp=enemy?enemy.hp:0;Vector3 position=enemy?enemy.transform.position:new Vector3();bool shade=enemy&&enemy.GetComponent<LocalShade>()!=null;orig(enemy,hit);
  if(!shade&&hp>0&&(!enemy||enemy.hp<hp))EnemyDamaged(s,position);
 }
 static void ExtraDamage(On.HealthManager.orig_ApplyExtraDamage orig,HealthManager enemy,int amount){
  var s=RoleSystem.Session;int hp=enemy?enemy.hp:0;Vector3 position=enemy?enemy.transform.position:new Vector3();bool shade=enemy&&enemy.GetComponent<LocalShade>()!=null;
  orig(enemy,amount);if(s!=null&&s.Active&&!shade&&hp>0&&(!enemy||enemy.hp<hp))EnemyDamaged(s,position);
 }
 static void EnemyDamaged(CoopSession s,Vector3 position){
  foreach(PlayerSlot mage in s.Players){
   if(!mage.Alive||!mage.Ready||!mage.Hero||!RoleSystem.Definition(mage).SoulLeech||Vector3.SqrMagnitude(mage.Hero.transform.position-position)>LeechRadius*LeechRadius)continue;
   RoleState st=RoleSystem.State(mage);int amount=st.SoulDrops.Gain(Time.time);if(amount==0)continue;
   // Write through the recipient's PlayerData context: the legacy AddMPCharge actor
   // forwarding used by world interactions must not redirect this passive award.
   using(PlayerContext.Enter(mage)){int before=s.Data.MPCharge+s.Data.MPReserve;s.Data.AddMPCharge(amount);mage.Capture(s.Data);
    int gained=s.Data.MPCharge+s.Data.MPReserve-before;if(gained>0)Diagnostics.Write("ROLE SOUL P"+(mage.Index+1)+" +"+gained);
   }
  }
 }
 // Apply constants only during the owner's vanilla update and always restore. No multipliers accumulate across frames.
 struct Movement {
  float run,runCharm,runCombo;
  internal Movement(HeroController h,float factor){run=h.RUN_SPEED;runCharm=h.RUN_SPEED_CH;runCombo=h.RUN_SPEED_CH_COMBO;h.RUN_SPEED*=factor;h.RUN_SPEED_CH*=factor;h.RUN_SPEED_CH_COMBO*=factor;}
  internal void Restore(HeroController h){h.RUN_SPEED=run;h.RUN_SPEED_CH=runCharm;h.RUN_SPEED_CH_COMBO=runCombo;}
 }
 static void Update(On.HeroController.orig_Update orig,HeroController h){if(RoleSystem.BlockHero(h))return;float factor=RoleSystem.Definition(Player(h)).Run;if(factor==1f){orig(h);return;}var fields=new Movement(h,factor);try{orig(h);}finally{fields.Restore(h);}}
 static void Fixed(On.HeroController.orig_FixedUpdate orig,HeroController h){if(RoleSystem.BlockHero(h))return;float factor=RoleSystem.Definition(Player(h)).Run;if(factor==1f){orig(h);return;}var fields=new Movement(h,factor);try{orig(h);}finally{fields.Restore(h);}}
 static void Attack(On.HeroController.orig_DoAttack orig,HeroController h){PlayerSlot p=Player(h);float speed=RoleCatalog.AttackFactor(RoleSystem.Definition(p),Low(p));if(speed==1f){orig(h);return;}float normal=h.ATTACK_COOLDOWN_TIME,charm=h.ATTACK_COOLDOWN_TIME_CH;try{h.ATTACK_COOLDOWN_TIME/=speed;h.ATTACK_COOLDOWN_TIME_CH/=speed;orig(h);}finally{h.ATTACK_COOLDOWN_TIME=normal;h.ATTACK_COOLDOWN_TIME_CH=charm;}}
 static void Dash(On.HeroController.orig_HeroDash orig,HeroController h){float factor=RoleSystem.Definition(Player(h)).DashRecovery;if(factor==1f){orig(h);return;}float normal=h.DASH_COOLDOWN,charm=h.DASH_COOLDOWN_CH;try{h.DASH_COOLDOWN*=factor;h.DASH_COOLDOWN_CH*=factor;orig(h);}finally{h.DASH_COOLDOWN=normal;h.DASH_COOLDOWN_CH=charm;}}
 static void StartFocus(On.HeroController.orig_StartMPDrain orig,HeroController h,float time){var p=Player(h);if(p!=null)RoleSystem.State(p).FocusCycle.Begin(Time.time);orig(h,time*RoleSystem.Definition(p).FocusTime);}
 static IEnumerator Invulnerable(On.HeroController.orig_Invulnerable orig,HeroController h,float duration){bool hitDuration=Mathf.Approximately(duration,h.INVUL_TIME)||Mathf.Approximately(duration,h.INVUL_TIME_STAL);return orig(h,duration*(hitDuration?RoleSystem.Definition(Player(h)).Invulnerability:1f));}
 static void Damage(On.HeroController.orig_TakeDamage orig,HeroController h,GameObject source,CollisionSide side,int amount,int hazard){
  if(RoleSystem.BlockHero(h))return;PlayerSlot p=Player(h);if(p==null||!RoleSystem.Definition(p).RiskOvercharm){orig(h,source,side,amount,hazard);return;}
  var s=RoleSystem.Session;bool saved=p.Charms.Overcharmed;
  // One roll per hit, shared by the original damage path and the co-op fatal-hit check.
  using(PlayerContext.Enter(p)){
   bool dataSaved=s.Data.overcharmed;bool twice=UnityEngine.Random.value<.1f;
   bool pvp=PvpCombat.Applying(p);int masks=PvpCombat.incomingMasks;
   int hp=s.Data.health+s.Data.healthBlue;
   p.Charms.Overcharmed=twice;s.Data.overcharmed=twice;
   // PvP has two downstream hooks that replace vanilla damage with incomingMasks.
   // Set that source once so both hooks agree and never multiply it a second time.
   if(pvp)PvpCombat.incomingMasks=masks*(twice?2:1);
   try{orig(h,source,side,amount,hazard);
    int after=s.Data.health+s.Data.healthBlue;
    if(after<hp||p.Down)Diagnostics.Write("ROLE SHAMAN P"+(p.Index+1)+" x2="+twice+" pvp="+pvp+" hp="+hp+"->"+after);
   }finally{p.Charms.Overcharmed=saved;s.Data.overcharmed=dataSaved;if(pvp)PvpCombat.incomingMasks=masks;}
  }
 }
 static bool FocusState(PlayerSlot p){return p!=null&&p.Hero&&p.Hero.spellControl&&RoleHealingRules.HealState(p.Hero.spellControl.ActiveStateName);}
 static void StateEnter(On.HutongGames.PlayMaker.FsmState.orig_OnEnter orig,FsmState state){
  var s=RoleSystem.Session;PlayerSlot p=s!=null&&s.Active?s.Resolve(state.Fsm):null;
  bool ownSpell=p!=null&&p.Hero&&p.Hero.spellControl&&state.Fsm==p.Hero.spellControl.Fsm;
  bool completed=ownSpell&&RoleHealingRules.HealState(state.Name);
  if(ownSpell&&state.Name=="Focus Start")RoleSystem.State(p).FocusCycle.Begin(Time.time);
  // Reserve on entry: vanilla may synchronously reach Inactive while executing
  // these actions, and it may set health directly instead of calling AddHealth.
  bool pulse=completed&&CanHeal(p)&&RoleSystem.State(p).FocusCycle.Complete(Time.time,true);
  // Reuse the original FSM/ownership hooks; never broadcast a heal event to other FSMs.
  using(RoleSummons.Enter(state.Fsm))orig(state);
  if(pulse)HealAllies(p);
  else if(ownSpell&&(state.Name=="Inactive"||state.Name=="Focus Cancel"))RoleSystem.State(p).FocusCycle.Cancel();
 }
 static void Heal(On.HeroController.orig_AddHealth orig,HeroController h,int amount){
  PlayerSlot p=Player(h);bool focus=FocusState(p);
  using(PlayerContext.Enter(p)){orig(h,amount);if(amount>0&&focus)CompleteFocus(p,false);}
 }
 static void DataHeal(On.PlayerData.orig_AddHealth orig,PlayerData data,int amount){
  // Vanilla focus may write PlayerData directly, without HeroController.AddHealth.
  PlayerSlot p=PlayerContext.Current;bool focus=FocusState(p);orig(data,amount);
  if(amount>0&&focus)CompleteFocus(p,false);
 }
 static void CompleteFocus(PlayerSlot p,bool nativeHealState){
  if(!CanHeal(p))return;
  var state=RoleSystem.State(p);if(!state.FocusCycle.Complete(Time.time,nativeHealState))return;
  HealAllies(p);
 }
 static bool CanHeal(PlayerSlot p){return !healing&&p!=null&&p.Hero&&p.Alive&&p.Ready&&!p.Reviving&&RoleSystem.Definition(p).HealingAura;}
 static void HealAllies(PlayerSlot p){
  if(!CanHeal(p))return;
  int count=0,outside=0,duel=0,unavailable=0,full=0;healing=true;
  try{
   var s=RoleSystem.Session;Vector3 center=RoleAura.Feet(p);
   foreach(PlayerSlot q in s.Players){
    if(q==p||!q.Hero||q.Hero.gameObject.scene!=p.Hero.gameObject.scene)continue;
    Vector3 delta=RoleAura.Feet(q)-center;
    // Recheck live data after entering the receiver context: slot snapshots may still
    // contain the pre-focus values while an outer FSM is executing.
    using(PlayerContext.Enter(q)){
     int hp=s.Data.health;
     if(PvpEnemy(p,q)){duel++;continue;}
     if(!q.Alive||!q.Ready||q.Hazard||q.ArenaTransfer||q.SpawnPending||hp<=0){unavailable++;continue;}
     if(hp>=q.CurrentMaxHealth){full++;continue;}
     if(!RoleHealingRules.Contains(delta.x,delta.y)){outside++;continue;}
     q.Hero.AddHealth(1);s.Data.health=Math.Min(s.Data.health,q.CurrentMaxHealth);q.Capture(s.Data);
     if(s.Data.health>hp){count++;RoleAura.Received(q);}
    }
   }
   RoleAura.Pulse(p);
   Diagnostics.Write("ROLE HEAL P"+(p.Index+1)+" allies="+count+" full="+full+" outside="+outside+" unavailable="+unavailable+" duel="+duel);
  }catch(Exception e){Diagnostics.Throttled("ROLE HEAL",e);}finally{healing=false;}
 }
 static bool PvpEnemy(PlayerSlot a,PlayerSlot b){return PvpMatch.Running && PvpCombat.Opponents(a,b);} // Casual friendly fire does not turn co-op companions into healing enemies.
 static void CharmsChanged(On.HeroController.orig_CharmUpdate orig,HeroController h){orig(h);PlayerSlot p=Player(h);if(p==null||RoleSystem.Definition(p).Masks==0)return;var s=RoleSystem.Session;using(PlayerContext.Enter(p)){Charms.UpdateMaximum(p,s.Data,false);p.Vitals.Write(s.Data);}}
 internal static void RefreshAura(PlayerSlot p){if(!RoleSystem.Definition(p).HealingAura)RoleAura.Remove(p);}
 internal static void Tick(CoopSession s){
  foreach(var p in s.Players){if(!p.Hero||!p.Ready)continue;var st=RoleSystem.State(p);int masks=RoleSystem.Definition(p).Masks;
   if(st.StatsHero!=p.Hero||st.AppliedMasks!=masks){
    // P1 can load a saved Guardian before co-op becomes active. Apply its capacity
    // once to every new actor, without requiring another role selection or healing it.
    using(PlayerContext.Enter(p)){Charms.UpdateMaximum(p,s.Data,false);p.Vitals.Write(s.Data);}
    st.StatsHero=p.Hero;st.AppliedMasks=masks;
   }
  }
  RoleAura.Tick(s);
 }
 internal static void ClearAuras(){RoleAura.Clear();}
}
}
