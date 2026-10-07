using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager=UnityEngine.SceneManagement.SceneManager;
namespace KO.HollowKnight8 {
internal static class PvpArena {
 enum Stage {None,Loading,Arranging,Countdown,Fighting,Result,Returning}
 sealed class Before {
  internal PlayerSlot Player;internal Vitals Vitals;internal Vector3 Position,Safe,Previous;
  internal bool HasSafe,HasPrevious,Facing;internal float Life;
  internal Vector3 Spawn;
 }
 static readonly List<Before> roster=new List<Before>();
 static readonly FieldInfo[] vitalFields=typeof(Vitals).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 static readonly FieldInfo combatLock=typeof(CameraController).GetField("currentLockArea",BindingFlags.Instance|BindingFlags.NonPublic);
 static readonly Dictionary<FieldInfo,object> world=new Dictionary<FieldInfo,object>();
 static readonly HashSet<int> bossControllers=new HashSet<int>();
 static readonly Dictionary<int,int> wins=new Dictionary<int,int>();
 static readonly int[] sides=new int[8],health=new int[8];static readonly bool[] alive=new bool[8];
 static Stage stage;static PvpArenaOptions options,rules;static CoopSession session;
 static string sourceRoom,arenaRoom,configSource,entryRoom;static float deadline,remaining;static int round,bestOf,oldMode,lastFrame=-1;
 static bool installed,ownTransition,finishing,returnSent;static Vector3 entrance;
 internal static PvpArenaOptions Options {get{string data=Local8Mod.Settings.PvpArenaConfig;if(options==null||data!=configSource){configSource=data;try{options=string.IsNullOrEmpty(data)?new PvpArenaOptions():JsonConvert.DeserializeObject<PvpArenaOptions>(data);}catch{options=new PvpArenaOptions();}if(options==null)options=new PvpArenaOptions();options.Normalize();}return options;}}
 internal static void SaveOptions(){if(options==null)return;options.Normalize();configSource=JsonConvert.SerializeObject(options);Local8Mod.Settings.PvpArenaConfig=configSource;}
 internal static float ClockSeconds {get{return stage==Stage.Fighting?remaining:float.NaN;}}
 internal static bool KeepsMusic {get{return Custom;}}
 // Arrival notifications belong to the destination, even while PvP still
 // owns the return trip. Outbound travel remains blocked by Transition/Exit.
 internal static bool BlocksWorldInteraction(bool blocked,Fsm f){return blocked&&!(stage==Stage.Returning&&f!=null&&f.GameObject&&f.GameObject.scene.name==sourceRoom);}
 internal static bool Active {get{return stage!=Stage.None;}}
 static bool Custom {get{return stage==Stage.Arranging||stage==Stage.Countdown||stage==Stage.Fighting||stage==Stage.Result;}}
 internal static bool IsRunning(bool normal){return Active||normal;}
 internal static bool CanFight(bool normal){return Active?stage==Stage.Fighting:normal;}
 internal static bool BlocksInput(bool normal){return Active?stage!=Stage.Fighting:normal;}
 internal static string Banner(string normal){return !Active?normal:stage==Stage.Loading||stage==Stage.Arranging?Text("loading"):stage==Stage.Returning?Text("returning"):stage==Stage.Result?result:Text("round")+" "+round+(stage==Stage.Countdown?"  |  "+Mathf.Max(1,Mathf.CeilToInt(remaining)):"");}
 static string result="";
 internal static string Announcement(string normal){return !Active?normal:stage==Stage.Countdown?Mathf.Max(1,Mathf.CeilToInt(remaining)).ToString():stage==Stage.Result?result:"";}
 internal static string Text(string key){return RoleText.Get("arena."+key);}
 static CoopSession Current {get{return Plugin.Self==null?null:Plugin.Self.Session;}}
 internal static void Install(){if(installed)return;installed=true;
  UnitySceneManager.sceneLoaded+=Loaded;On.GameManager.FindEntryPoint+=FindEntry;On.GameManager.EnterHero+=EnterHero;
  On.BossSceneController.Awake+=BossAwake;On.BossSceneController.Start+=BossStart;On.BossSceneController.Update+=BossUpdate;On.BossSceneController.OnDestroy+=BossDestroy;
  On.HealthManager.Awake+=EnemyAwake;
  On.HutongGames.PlayMaker.Fsm.OnEnable+=FsmEnable;On.HutongGames.PlayMaker.Fsm.Start+=FsmStart;On.HutongGames.PlayMaker.Fsm.Update+=FsmTick;On.HutongGames.PlayMaker.Fsm.FixedUpdate+=FsmFixed;On.HutongGames.PlayMaker.Fsm.LateUpdate+=FsmLate;
  On.GameManager.SaveGame+=Save;On.GameManager.SaveGame_Action1+=SaveCallback;On.GameManager.SaveGame_int_Action1+=SaveSlot;
  On.HeroController.Update+=HeroUpdate;On.PlayerData.GetBool+=DataBool;On.PlayerData.GetInt+=DataInt;On.HeroController.CanFocus+=Focus;On.HeroController.HeroDash+=Dash;On.HeroController.TakeDamage+=Damage;On.PlayerData.AddMPCharge+=AddSoul;On.PlayerData.TakeMP+=TakeSoul;On.HeroController.CanOpenInventory+=Inventory;On.GameManager.SaveLevelState+=SaveLevel;
 }
 internal static void Shutdown(){if(Active)LeaveSession();SaveOptions();if(!installed)return;installed=false;
  UnitySceneManager.sceneLoaded-=Loaded;On.GameManager.FindEntryPoint-=FindEntry;On.GameManager.EnterHero-=EnterHero;On.BossSceneController.Awake-=BossAwake;On.BossSceneController.Start-=BossStart;On.BossSceneController.Update-=BossUpdate;On.BossSceneController.OnDestroy-=BossDestroy;On.HealthManager.Awake-=EnemyAwake;On.HutongGames.PlayMaker.Fsm.OnEnable-=FsmEnable;On.HutongGames.PlayMaker.Fsm.Start-=FsmStart;On.HutongGames.PlayMaker.Fsm.Update-=FsmTick;On.HutongGames.PlayMaker.Fsm.FixedUpdate-=FsmFixed;On.HutongGames.PlayMaker.Fsm.LateUpdate-=FsmLate;On.GameManager.SaveGame-=Save;On.GameManager.SaveGame_Action1-=SaveCallback;On.GameManager.SaveGame_int_Action1-=SaveSlot;On.HeroController.Update-=HeroUpdate;On.PlayerData.GetBool-=DataBool;On.PlayerData.GetInt-=DataInt;On.HeroController.CanFocus-=Focus;On.HeroController.HeroDash-=Dash;On.HeroController.TakeDamage-=Damage;On.PlayerData.AddMPCharge-=AddSoul;On.PlayerData.TakeMP-=TakeSoul;On.HeroController.CanOpenInventory-=Inventory;On.GameManager.SaveLevelState-=SaveLevel;bossControllers.Clear();}
 static bool ArenaObject(GameObject go){return Active&&go&&go.scene.name==arenaRoom;}
 static bool PlayerObject(GameObject go){return go&&(go.GetComponentInParent<HeroController>()||go.GetComponentInParent<OwnerTag>());}
 static bool BattleFsm(Fsm f){if(f==null||!ArenaObject(f.GameObject)||PlayerObject(f.GameObject))return false;string path=f.Name;for(Transform t=f.GameObject.transform;t!=null;t=t.parent)path+="/"+t.name;path=path.ToLowerInvariant();return f.GameObject.GetComponentInParent<HealthManager>()||path.Contains("boss holder")||(f.GameObject.name+" "+f.Name).ToLowerInvariant().Contains("battle")||path.Contains("boss control")||path.Contains("dream return")||path.Contains("gg scene transition")||path.Contains("end scene");}
 static void FsmTick(On.HutongGames.PlayMaker.Fsm.orig_Update orig,Fsm f){if(!BattleFsm(f))orig(f);}
 static void FsmFixed(On.HutongGames.PlayMaker.Fsm.orig_FixedUpdate orig,Fsm f){if(!BattleFsm(f))orig(f);}
 static void FsmLate(On.HutongGames.PlayMaker.Fsm.orig_LateUpdate orig,Fsm f){if(!BattleFsm(f))orig(f);}
 static void FsmEnable(On.HutongGames.PlayMaker.Fsm.orig_OnEnable orig,Fsm f){if(!BattleFsm(f))orig(f);}
 static void FsmStart(On.HutongGames.PlayMaker.Fsm.orig_Start orig,Fsm f){if(!BattleFsm(f))orig(f);}
 internal static bool SuppressFsm(PlayMakerFSM f){return f&&BattleFsm(f.Fsm);}
 static void EnemyAwake(On.HealthManager.orig_Awake orig,HealthManager enemy){if(ArenaObject(enemy.gameObject)&&!PlayerObject(enemy.gameObject)){enemy.gameObject.SetActive(false);return;}orig(enemy);}
 static void BossAwake(On.BossSceneController.orig_Awake orig,BossSceneController boss){if(!ArenaObject(boss.gameObject)){orig(boss);return;}bossControllers.Add(boss.GetInstanceID());BossSceneController.Instance=boss;boss.BossLevel=arenaRoom.EndsWith("_V",StringComparison.Ordinal)?1:0;Reflect.Set(boss,"endedScene",true);Reflect.Set(boss,"restoreBindingsOnDestroy",false);boss.CanTransition=false;boss.doTransitionIn=false;boss.doTransitionOut=false;Reflect.Set(boss,"<HasTransitionedIn>k__BackingField",true);Reflect.Set(boss,"<BossHealthLookup>k__BackingField",new Dictionary<HealthManager,BossSceneController.BossHealthDetails>());}
 static IEnumerator Empty(){yield break;}
 static IEnumerator BossStart(On.BossSceneController.orig_Start orig,BossSceneController boss){return bossControllers.Contains(boss.GetInstanceID())?Empty():orig(boss);}
 static void BossUpdate(On.BossSceneController.orig_Update orig,BossSceneController boss){if(!bossControllers.Contains(boss.GetInstanceID()))orig(boss);}
 static void BossDestroy(On.BossSceneController.orig_OnDestroy orig,BossSceneController boss){if(bossControllers.Remove(boss.GetInstanceID())){if(BossSceneController.Instance==boss)BossSceneController.Instance=null;}else orig(boss);}
 static void Save(On.GameManager.orig_SaveGame orig,GameManager gm){if(!Active)orig(gm);else Diagnostics.Write("PVP ARENA suppressed temporary save");}
 static void SaveCallback(On.GameManager.orig_SaveGame_Action1 orig,GameManager gm,Action<bool> done){if(!Active)orig(gm,done);else {if(done!=null)done(true);}}
 static void SaveSlot(On.GameManager.orig_SaveGame_int_Action1 orig,GameManager gm,int slot,Action<bool> done){if(!Active)orig(gm,slot,done);else {if(done!=null)done(true);}}
 internal static bool StartRequest(){if(Active){Back();return true;}var config=Options;if(config.Arena==0)return false;var s=Current;var gm=GameManager.instance;
  if(s==null||!s.Active||!s.Gameplay||s.Players.Count<2||!gm||gm.isPaused||BossSequenceController.IsInSequence||BossSceneController.Instance||ArenaGather.EngagedBossAlive||CoopEnding.Active||ScriptedParty.Active||PickupCard.Owner!=null||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null||Charms.NativeMenuOpen||RoleSystem.JoiningMenu||TransitionVote.Pending){Issue("notready");return true;}
  foreach(var p in s.Players){string why;if(!p.Connected||!p.Ready||!p.Alive||p.Hero.controlReqlinquished||!DuelGround.Safe(p,p.Hero.transform.position,DuelGround.Body(p).center-p.Hero.transform.position,DuelGround.Body(p).size,out why)){Issue("notready");return true;}}
  int n=0;foreach(var p in s.Players){sides[n]=PvpRules.Side(p.Index,PvpCombat.Team(p));alive[n++]=true;}if(PvpRules.Outcome(sides,alive,n)!=PvpRules.Ongoing){Issue("teams");return true;}
  string target=PvpArenaCatalog.Scenes[config.Arena];if(!Application.CanStreamedLevelBeLoaded(target)){Issue("unavailable");return true;}
  // Save ordinary adventure before entering. Arena-only saves are suppressed.
  PvpMatch.Stop(true);gm.SaveGame();session=s;sourceRoom=UnitySceneManager.GetActiveScene().name;arenaRoom=target;rules=config.Copy();oldMode=Local8Mod.Settings.PvpMode;
  roster.Clear();world.Clear();wins.Clear();round=0;bestOf=PvpRules.SeriesLength(Local8Mod.Settings.DuelBestOf);
  foreach(var field in typeof(PlayerData).GetFields(BindingFlags.Public|BindingFlags.Instance)){if(field.Name=="disablePause"||field.Name=="atBench"||field.Name=="isInvincible")continue;if(field.FieldType.IsValueType||field.FieldType==typeof(string)||typeof(IList).IsAssignableFrom(field.FieldType)||typeof(IDictionary).IsAssignableFrom(field.FieldType))world[field]=CopyValue(field.GetValue(s.Data));}
  foreach(var p in s.Players){var v=CopyVitals(p.Vitals);roster.Add(new Before{Player=p,Vitals=v,Position=p.Hero.transform.position,Safe=p.SafePoint,Previous=p.PreviousSafePoint,HasSafe=p.HasSafePoint,HasPrevious=p.HasPreviousSafePoint,Facing=p.Hero.cState.facingRight,Life=p.LifeStartedAt});}
  PvpCharms.Begin(s);Local8Mod.Settings.PvpMode=2;PvpMatch.StartIssue="";stage=Stage.Loading;deadline=Time.unscaledTime+25;EmergencyWarp.CancelAll();TransitionVote.Reset();Plugin.Self.SetPanel(false);Diagnostics.Write("PVP ARENA travel "+sourceRoom+" -> "+arenaRoom+" players="+roster.Count);
  try{Travel(arenaRoom);}catch(Exception e){Diagnostics.Throttled("PVP ARENA entry",e);RestoreAll(false);Clear();Issue("failed");}return true;
 }
 static object CopyValue(object value){if(value is Array)return ((Array)value).Clone();if(value is IList){var list=(IList)Activator.CreateInstance(value.GetType());foreach(var item in (IList)value)list.Add(item);return list;}if(value is IDictionary){var map=(IDictionary)Activator.CreateInstance(value.GetType());foreach(DictionaryEntry item in (IDictionary)value)map[item.Key]=item.Value;return map;}return value;}
 static Vitals CopyVitals(Vitals from){var v=new Vitals();CopyVitals(from,v);return v;}
 static void CopyVitals(Vitals from,Vitals to){foreach(var f in vitalFields)f.SetValue(to,f.GetValue(from));}
 static void Issue(string key){PvpMatch.StartIssue=Text(key);if(Plugin.Self)Plugin.Self.SetPanel(true);Diagnostics.Write("PVP ARENA "+Text(key));}
 static void Travel(string room){var gm=GameManager.instance;entryRoom=room;RemoveEntry();if(room==arenaRoom)entrance=Vector3.zero;ownTransition=true;try{gm.BeginSceneTransition(new GameManager.SceneLoadInfo{SceneName=room,EntryGateName=EntryName,EntryDelay=0,Visualization=GameManager.SceneLoadVisualizations.Default,PreventCameraFadeOut=false,WaitForSceneTransitionCameraFade=true});}finally{ownTransition=false;}}
 internal static bool Transition(On.GameManager.orig_BeginSceneTransition orig,GameManager gm,GameManager.SceneLoadInfo info){if(!Active)return false;if(ownTransition&&info!=null&&info.SceneName==(stage==Stage.Returning?sourceRoom:arenaRoom)){TransitionVote.Reset();EmergencyWarp.Reset();InteractionRouter.CloseForTransition();session.PrepareTransition(session.Primary);using(PlayerContext.Enter(session.Primary))orig(gm,info);}else if(stage==Stage.Fighting)Forfeit(PlayerContext.Current??InteractionRouter.ActivePlayer);return true;}
 internal static bool ChangeMode(int mode){if(!Active)return false;oldMode=mode;Back();return true;}
 internal static bool StopRequest(bool restore){if(!Active)return false;Back();return true;}
 internal static bool SceneChange(){return Active;}
 internal static bool SessionTick(CoopSession s){return Active;}
 internal static bool Exit(PlayerSlot p){if(!Active)return false;if(!ownTransition&&stage==Stage.Fighting)Forfeit(p);return !ownTransition;}
 static void Forfeit(PlayerSlot p){if(p!=null&&p.Ready&&!p.Down&&session!=null){session.Down(p,false);PvpCombat.Down(p);}}
 static void Loaded(Scene scene,LoadSceneMode mode){if(!Active||(scene.name!=sourceRoom&&scene.name!=arenaRoom))return;
  try{var gm=GameManager.instance;if(!gm)return;
   if(scene.name==sourceRoom)entrance=roster[0].Position;
   else {var boss=BossSceneController.Instance;entrance=boss&&boss.heroSpawn?boss.heroSpawn.position:Vector3.zero;foreach(var root in scene.GetRootGameObjects()){foreach(var enemy in root.GetComponentsInChildren<HealthManager>(true))if(!PlayerObject(enemy.gameObject))enemy.gameObject.SetActive(false);foreach(var f in root.GetComponentsInChildren<PlayMakerFSM>(true))if(BattleFsm(f.Fsm))f.enabled=false;}if(entrance==Vector3.zero){foreach(var gate in TransitionPoint.TransitionPoints)if(gate&&gate.gameObject.scene==scene){entrance=gate.transform.position;break;}}}
   MakeEntry(scene,entrance);
   Diagnostics.Write("PVP ARENA loaded="+scene.name+" entrance="+entrance);
  }catch(Exception e){Diagnostics.Throttled("PVP ARENA loaded",e);}
 }
 internal static void Update(Local8Runtime runtime){if(!Active)return;if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
  try{var gm=GameManager.instance;if(runtime==null||runtime.Session!=session||!runtime.Enabled.Value||!gm||gm.IsMenuScene()||gm.IsTitleScreenScene()){LeaveSession();return;}
   string room=UnitySceneManager.GetActiveScene().name;
   if(stage==Stage.Returning&&!returnSent&&!gm.IsLoadingSceneTransition&&room!=sourceRoom){Travel(sourceRoom);returnSent=true;deadline=Time.unscaledTime+25;return;}
   if(stage==Stage.Loading||stage==Stage.Returning){if(room==(stage==Stage.Returning?sourceRoom:arenaRoom)&&session.Gameplay&&gm.HasFinishedEnteringScene&&!gm.IsLoadingSceneTransition&&AllReady(stage==Stage.Returning)){if(stage==Stage.Returning){var returning=session;RestoreAll(true);gm.FadeSceneIn();Clear();ArenaReturnRecovery.Begin(returning);Diagnostics.Write("PVP ARENA returned; original stats and positions restored");}else{stage=Stage.Arranging;gm.FadeSceneIn();Arrange();}return;}if(Time.unscaledTime>deadline){if(stage==Stage.Loading)Back();else {RestoreAll(false);Clear();Issue("failed");}}return;}
   if(room!=arenaRoom){RestoreAll(false);Clear();return;}
   if(!SameRoster()){Back();return;}if(gm.isPaused||runtime.Panel||!session.Gameplay)return;
   foreach(var entry in roster){var p=entry.Player;OverrideMaximum(p,session.Data,false);if(rules.InfiniteSoul&&p.Alive){p.Vitals.Soul=rules.SoulCapacity;p.Vitals.Reserve=rules.Reserve;session.Commit(p);}}
   remaining-=Time.deltaTime;
   if(stage==Stage.Countdown){foreach(var entry in roster)entry.Player.ProtectionUntil=Time.time+.15f;if(remaining<=0){stage=Stage.Fighting;remaining=RoundTimeRules.Duration(Local8Mod.Settings.DuelSeconds);PvpCombat.Invalidate();foreach(var e in roster){e.Player.ProtectionUntil=0;e.Player.Hero.cState.invulnerable=false;}}}
   else if(stage==Stage.Fighting){for(int i=0;i<roster.Count;i++){health[i]=roster[i].Player.Vitals.Health+roster[i].Player.Vitals.Blue;alive[i]=!roster[i].Player.Down&&health[i]>0;}int winner=PvpRules.Outcome(sides,alive,roster.Count);if(winner==PvpRules.Ongoing&&remaining<=0)winner=PvpHealthRules.Timeout(sides,alive,health,roster.Count);if(winner!=PvpRules.Ongoing)EndRound(winner);}
   else if(stage==Stage.Result&&remaining<=0){if(finishing)Back();else NextRound();}
  }catch(Exception e){Diagnostics.Throttled("PVP ARENA",e);Back();}
 }
 static bool AllReady(bool returning){foreach(var e in roster){if(!session.Players.Contains(e.Player)){if(returning)continue;return false;}if(!e.Player.Ready||!e.Player.Hero||e.Player.SpawnPending)return false;}return true;}
 static bool SameRoster(){if(session.Players.Count!=roster.Count)return false;for(int i=0;i<roster.Count;i++){var p=roster[i].Player;if(!session.Players.Contains(p)||!p.Connected||!p.Hero||PvpRules.Side(p.Index,PvpCombat.Team(p))!=sides[i])return false;}return true;}
 static void Arrange(){
  var points=FindSpawns();if(points.Count<roster.Count){Diagnostics.Write("PVP ARENA insufficient safe landings "+points.Count);Back();Issue("landings");return;}
  for(int i=0;i<roster.Count;i++)roster[i].Spawn=points[Mathf.RoundToInt(i*(points.Count-1f)/Mathf.Max(1,roster.Count-1))];
  NextRound();
 }
 // Search only the playable height around the authored hero spawn. A floor
 // below a void/spike arena is terrain too, but is not a combat landing.
 static List<Vector3> FindSpawns(){var points=new List<Vector3>();var p=roster[0].Player;var body=DuelGround.Body(p);Vector3 offset=body.center-p.Hero.transform.position;
  bool mantis=arenaRoom.StartsWith("GG_Mantis_Lords",StringComparison.Ordinal),markothFloor=arenaRoom=="GG_Ghost_Markoth";
  var area=FindArea();var zone=area?area.GetComponent<Collider2D>():null;
  // Its entrance stands beside the battle floor; legal terrain there is not
  // part of the arena. Never let an absent battle lock admit that corridor.
  if(markothFloor&&!zone)return points;
  float center=markothFloor?MarkothCenter(area):mantis?CombatCenterX():entrance.x;
  float left=Mathf.Max(1,center-20),right=center+20,top=entrance.y+8,bottom=entrance.y-5;
  var camera=GameCameras.instance?GameCameras.instance.cameraController:null;
  if(camera&&camera.sceneWidth>2){right=Mathf.Min(right,camera.sceneWidth-1);top=Mathf.Min(top,camera.sceneHeight-1);}
  if(zone){if(mantis){left=zone.bounds.min.x+1;right=zone.bounds.max.x-1;}else if(markothFloor){float inset=Mathf.Max(2.5f,body.size.x*.5f+.75f);left=Mathf.Max(center-8,zone.bounds.min.x+inset-offset.x);right=Mathf.Min(center+8,zone.bounds.max.x-inset-offset.x);}else{left=Mathf.Max(left,zone.bounds.min.x+1);right=Mathf.Min(right,zone.bounds.max.x-1);}top=Mathf.Min(top,zone.bounds.max.y-.25f);if(!markothFloor)bottom=Mathf.Max(bottom,zone.bounds.min.y+.1f);}
  for(float x=left;x<=right;x+=Mathf.Max(.8f,body.size.x+.12f)){
   Vector3 best=Vector3.zero;float score=float.MaxValue;
   foreach(var hit in Physics2D.RaycastAll(new Vector2(x,top),Vector2.down,Mathf.Max(0,top-bottom),1<<8)){
    if(!hit.collider||hit.collider.isTrigger||hit.normal.y<.6f||hit.point.y<bottom||hit.point.y>top)continue;
    var at=new Vector3(x,hit.point.y+body.size.y*.5f-offset.y+.05f,p.Hero.transform.position.z);string why;
    float distance=Mathf.Abs(at.y-entrance.y);if(distance<score&&DuelGround.Safe(p,at,offset,body.size,out why)&&(!markothFloor||SpawnSafety.ClearAt(p,at))){best=at;score=distance;}
   }
   if(score<float.MaxValue)points.Add(best);
  }
  if(arenaRoom=="GG_Mantis_Lords"||arenaRoom=="GG_Mantis_Lords_V")points=CentralIsland(points,CombatCenterX());
  if(markothFloor){Diagnostics.Write("PVP ARENA Markoth combat center="+center+" interior="+left+".."+right+" safe="+points.Count);}
  return points;
 }
 // Mantis side ledges are safe terrain, but lie outside the spike-separated
 // combat island. Choose the contiguous landing group nearest its boss centre.
 static List<Vector3> CentralIsland(List<Vector3> points,float center){
  var best=new List<Vector3>();var group=new List<Vector3>();float score=float.MaxValue;
  for(int i=0;i<=points.Count;i++){
   if(i==points.Count||(group.Count>0&&points[i].x-group[group.Count-1].x>2f)){
    if(group.Count>0){float d=Mathf.Abs((group[0].x+group[group.Count-1].x)*.5f-center);if(d<score){score=d;best=new List<Vector3>(group);}}group.Clear();
   }
   if(i<points.Count)group.Add(points[i]);
  }
  return best.Count>=roster.Count?best:new List<Vector3>();
 }
 static float CombatCenterX(){
  // The fixed combat lock is authoritative; inactive boss health holders can
  // have staging positions unrelated to the platform where combat takes place.
  var camera=GameCameras.instance?GameCameras.instance.cameraController:null;
  if(camera&&camera.mode==CameraController.CameraMode.LOCKED&&Mathf.Abs(camera.xLockMin-entrance.x)<80&&camera.xLockMax-camera.xLockMin<3)return (camera.xLockMin+camera.xLockMax)*.5f;
  var boss=BossSceneController.Instance;float sum=0;int count=0;
  if(boss&&boss.bosses!=null)foreach(var b in boss.bosses)if(b&&b.gameObject.scene.name==arenaRoom&&Mathf.Abs(b.transform.position.x-entrance.x)<80){sum+=b.transform.position.x;count++;}
  if(count>0)return sum/count;
  CameraLockArea nearest=null;float score=float.MaxValue;
  foreach(var area in UnityEngine.Object.FindObjectsOfType<CameraLockArea>()){
   var c=area.GetComponent<Collider2D>();if(area.gameObject.scene.name!=arenaRoom||!c||c.bounds.size.x<6||c.bounds.size.x>100)continue;
   float d=(c.bounds.center-entrance).sqrMagnitude;if(d<score){score=d;nearest=area;}
  }
  return nearest?nearest.GetComponent<Collider2D>().bounds.center.x:entrance.x;
 }

 // Only prevalidated arena placements and the saved return snapshot can bypass
 // the ordinary respawn heuristic. Never change normal hazard recovery.
 static PlayerSlot placing;static Vector3 placingAt;
 internal static bool PlacementSafe(bool normal,Vector3 at,PlayerSlot player){return normal||(Active&&player==placing&&at==placingAt&&SpawnSafety.ClearAt(player,at));}
 static void Place(PlayerSlot player,Vector3 at){var before=placing;var beforeAt=placingAt;placing=player;placingAt=at;try{session.Recover(player,true,at);if(player.Hero){if(player.Hero.cState.facingRight)player.Hero.FaceRight();else player.Hero.FaceLeft();}}finally{placing=before;placingAt=beforeAt;}}
 static void NextRound(){PvpFamiliars.Round();BossMusicGuard.RoundEvent();ArenaVisualRecovery.RoundStarted();round++;stage=Stage.Countdown;remaining=3;result="";PvpCombat.Reset(false);
  foreach(var e in roster){var p=e.Player;Revival.End(p);Place(p,e.Spawn);OverrideMaximum(p,session.Data,false);p.Vitals.Health=p.Vitals.MaxHealth;p.Vitals.Blue=rules.BlueMasks+p.Vitals.Joni+PvpCharms.Lifeblood(p);p.Vitals.DamagedBlue=false;p.Vitals.Soul=rules.Soul;p.Vitals.Reserve=rules.Reserve;p.Vitals.Invincible=false;p.Vitals.DisablePause=false;p.ProtectionUntil=Time.time+3;p.Vitals.AtBench=false;p.SafePoint=e.Spawn;p.PreviousSafePoint=e.Spawn;p.HasSafePoint=p.HasPreviousSafePoint=true;p.Vitals.HazardPoint=e.Spawn;session.Commit(p);}
  Diagnostics.Write("PVP ARENA round="+round+" room="+arenaRoom);
 }
 static void EndRound(int winner){BossMusicGuard.RoundEvent();int count=0;if(winner!=0){wins.TryGetValue(winner,out count);wins[winner]=++count;for(int i=0;i<roster.Count;i++)if(sides[i]==winner)PvpCombat.Wins[roster[i].Player.Index]++;}finishing=winner!=0&&PvpRules.SeriesComplete(bestOf,count);result=winner==0?Text("draw"):(winner>0?Text("team")+" "+winner:"P"+(-winner))+" "+Text(finishing?"winsmatch":"winsround");stage=Stage.Result;remaining=finishing?5:4;PvpCombat.Invalidate();}
 internal static void Back(){if(!Active||stage==Stage.Returning)return;finishing=true;stage=Stage.Returning;returnSent=false;result="";deadline=Time.unscaledTime+25;EmergencyWarp.CancelAll();PvpCombat.Reset(false);
  try{foreach(var e in roster){var p=e.Player;if(p.Hero&&p.Down)Place(p,e.Spawn);CopyVitals(e.Vitals,p.Vitals);p.Down=false;p.Hazard=false;session.Commit(p);}RestoreWorld();if(UnitySceneManager.GetActiveScene().name==sourceRoom&&!GameManager.instance.IsLoadingSceneTransition){RestoreAll(true);Clear();return;}if(!GameManager.instance.IsLoadingSceneTransition){Travel(sourceRoom);returnSent=true;}else deadline=Time.unscaledTime+35;}
  catch(Exception e){Diagnostics.Throttled("PVP ARENA return",e);RestoreAll(false);Clear();Issue("failed");}
 }
 // Continue an emergency return on the persistent GameManager if the mod/session is removed.
 static void LeaveSession(){var gm=GameManager.instance;string room=sourceRoom;Vector3 at=roster.Count>0?roster[0].Position:Vector3.zero;RestoreAll(false);Clear();if(gm&&!gm.IsMenuScene()&&!gm.IsTitleScreenScene()&&UnitySceneManager.GetActiveScene().name!=room)gm.StartCoroutine(ExitWithoutSession(gm,room,at));}
 static IEnumerator ExitWithoutSession(GameManager gm,string room,Vector3 at){float end=Time.unscaledTime+30;while(gm&&gm.IsLoadingSceneTransition&&Time.unscaledTime<end)yield return null;if(!gm||gm.IsMenuScene()||gm.IsTitleScreenScene()||gm.IsLoadingSceneTransition)yield break;
  UnityEngine.Events.UnityAction<Scene,LoadSceneMode> loaded=(scene,mode)=>{if(scene.name==room)MakeEntry(scene,at);};UnitySceneManager.sceneLoaded+=loaded;
  try{Reflect.Call(gm,"orig_BeginSceneTransition",new GameManager.SceneLoadInfo{SceneName=room,EntryGateName=EntryName,EntryDelay=0,Visualization=GameManager.SceneLoadVisualizations.Default,WaitForSceneTransitionCameraFade=true});end=Time.unscaledTime+30;while(gm&&!gm.IsMenuScene()&&(gm.IsLoadingSceneTransition||UnitySceneManager.GetActiveScene().name!=room)&&Time.unscaledTime<end)yield return null;if(gm&&UnitySceneManager.GetActiveScene().name==room)gm.FadeSceneIn();}
  finally{UnitySceneManager.sceneLoaded-=loaded;RemoveEntry();}}
 static void RestoreWorld(){if(session==null||session.Data==null)return;foreach(var item in world)item.Key.SetValue(session.Data,CopyValue(item.Value));}
 static void RestoreAll(bool positions){PvpCharms.End();RestoreWorld();if(session==null)return;foreach(var e in roster){var p=e.Player;if(!session.Players.Contains(p))continue;try{if(positions&&p.Hero)Place(p,e.Position);CopyVitals(e.Vitals,p.Vitals);p.Down=p.Hazard=false;p.HasSafePoint=e.HasSafe;p.HasPreviousSafePoint=e.HasPrevious;p.SafePoint=e.Safe;p.PreviousSafePoint=e.Previous;p.LifeStartedAt=e.Life;p.FarSince=-1;p.ProtectionUntil=0;p.InputBlocked=false;if(p.Hero){if(e.Facing)p.Hero.FaceRight();else p.Hero.FaceLeft();p.Hero.cState.invulnerable=false;}session.Commit(p);}catch(Exception ex){Diagnostics.Throttled("PVP ARENA restore P"+(p.Index+1),ex);}}Local8Mod.Settings.PvpMode=oldMode;}
 static void Clear(){RemoveEntry();placing=null;returnSent=false;stage=Stage.None;rules=null;session=null;roster.Clear();world.Clear();wins.Clear();sourceRoom=arenaRoom=entryRoom=null;finishing=false;ownTransition=false;PvpCombat.Reset(false);}
 internal static int Maximum(int normal,PlayerSlot p){if(!Custom||p==null)return normal;foreach(var e in roster)if(e.Player==p)return rules.Health(p.Index,e.Vitals.MaxHealth);return normal;}
 internal static bool OverrideMaximum(PlayerSlot p,PlayerData d,bool heal){if(!Custom||p==null)return false;foreach(var e in roster)if(e.Player==p){int maximum=rules.Health(p.Index,PvpCharms.NaturalHealth(p,e.Vitals.MaxHealth));p.Vitals.Joni=PvpCharms.Joni(p,maximum);p.Vitals.MaxHealth=p.Vitals.Joni>0?1:maximum;p.Vitals.MaxSoul=rules.SoulCapacity;p.Vitals.SoulLimited=false;p.Vitals.FocusCost=33;p.Vitals.Health=heal?p.Vitals.MaxHealth:Math.Min(p.Vitals.Health,p.Vitals.MaxHealth);p.Vitals.Soul=Math.Min(p.Vitals.Soul,rules.SoulCapacity);session.Commit(p);return true;}return false;}
 internal static bool SkipSoulSync(){return Custom;}
 static bool OurPlayer(){return Custom&&session!=null&&(PlayerContext.Current==null||session.Players.Contains(PlayerContext.Current));}
 static bool DataBool(On.PlayerData.orig_GetBool orig,PlayerData d,string key){if(OurPlayer()){if((rules.AllAbilities||rules.InfiniteWings)&&key=="hasDoubleJump")return true;if((rules.AllAbilities||rules.InfiniteDash)&&key=="canDash")return true;if(rules.AllAbilities&&(key=="hasDash"||key=="hasShadowDash"||key=="hasWalljump"||key=="hasSuperDash"||key=="hasAcidArmour"))return true;}return orig(d,key);}
 static int DataInt(On.PlayerData.orig_GetInt orig,PlayerData d,string key){if(OurPlayer()&&rules.SpellLevel>=0&&(key=="fireballLevel"||key=="quakeLevel"||key=="screamLevel"))return rules.SpellLevel;return orig(d,key);}
 static void HeroUpdate(On.HeroController.orig_Update orig,HeroController h){if(!Custom){orig(h);return;}PlayerSlot p=session.Resolve(h);if(p==null){orig(h);return;}using(PlayerContext.Enter(p)){var d=session.Data;bool wings=d.hasDoubleJump,dash=d.canDash,hasDash=d.hasDash,shadow=d.hasShadowDash,wall=d.hasWalljump,super=d.hasSuperDash,acid=d.hasAcidArmour;int fire=d.fireballLevel,quake=d.quakeLevel,scream=d.screamLevel;try{if(rules.InfiniteWings||rules.AllAbilities)d.hasDoubleJump=true;if(rules.InfiniteDash||rules.AllAbilities){d.canDash=true;d.hasDash=true;}if(rules.AllAbilities){d.hasShadowDash=true;d.hasWalljump=true;d.hasSuperDash=true;d.hasAcidArmour=true;}if(rules.SpellLevel>=0)d.fireballLevel=d.quakeLevel=d.screamLevel=rules.SpellLevel;if(p.Alive&&!p.InputBlocked){if(rules.InfiniteWings&&!h.cState.doubleJumping)Reflect.Set(h,"doubleJumped",false);if(rules.InfiniteDash&&!h.cState.dashing){Reflect.Set(h,"airDashed",false);if(rules.DashCooldown<0)Reflect.Set(h,"dashCooldownTimer",0f);}}orig(h);}finally{d.hasDoubleJump=wings;d.canDash=dash;d.hasDash=hasDash;d.hasShadowDash=shadow;d.hasWalljump=wall;d.hasSuperDash=super;d.hasAcidArmour=acid;d.fireballLevel=fire;d.quakeLevel=quake;d.screamLevel=scream;}}}
 static bool AddSoul(On.PlayerData.orig_AddMPCharge orig,PlayerData d,int amount){if(!OurPlayer()||d!=session.Data)return orig(d,amount);d.maxMP=rules.SoulCapacity;int before=d.MPCharge+d.MPReserve;int gain=Math.Max(0,amount),fill=Math.Min(gain,Math.Max(0,rules.SoulCapacity-d.MPCharge));d.MPCharge+=fill;d.MPReserve=Math.Min(rules.Reserve,d.MPReserve+gain-fill);return before!=d.MPCharge+d.MPReserve;}
 static void TakeSoul(On.PlayerData.orig_TakeMP orig,PlayerData d,int amount){if(OurPlayer()&&rules.InfiniteSoul&&d==session.Data){d.MPCharge=rules.SoulCapacity;return;}orig(d,amount);}
 static bool Inventory(On.HeroController.orig_CanOpenInventory orig,HeroController h){return !Active&&orig(h);}
 static void SaveLevel(On.GameManager.orig_SaveLevelState orig,GameManager gm){if(!Active||UnitySceneManager.GetActiveScene().name!=arenaRoom)orig(gm);}
 static bool Focus(On.HeroController.orig_CanFocus orig,HeroController h){return !(Custom&&!rules.AllowHealing&&session.Resolve(h)!=null)&&orig(h);}
 static void Dash(On.HeroController.orig_HeroDash orig,HeroController h){orig(h);if(Custom&&rules.DashCooldown>=0&&session.Resolve(h)!=null)Reflect.Set(h,"dashCooldownTimer",rules.DashCooldown*.001f);}
 static void Damage(On.HeroController.orig_TakeDamage orig,HeroController h,GameObject source,CollisionSide side,int masks,int hazard){if(!Custom||session.Resolve(h)==null){orig(h,source,side,masks,hazard);return;}if(stage!=Stage.Fighting)return;if(hazard>1)masks=rules.HazardMasks;var boss=BossSceneController.Instance;int level=boss?boss.BossLevel:0;try{if(boss)boss.BossLevel=0;orig(h,source,side,masks,hazard);}finally{if(boss)boss.BossLevel=level;}}

 // CameraRig remains the sole camera owner, including its Godhome framing
 // and scene/lens restoration. PvP must not capture an already enlarged lens.
 static CameraLockArea MarkothFloorArea(){
  var controller=GameCameras.instance?GameCameras.instance.cameraController:null;
  var active=controller&&combatLock!=null?combatLock.GetValue(controller) as CameraLockArea:null;
  if(MarkothAreaValid(active))return active;
  CameraLockArea best=null;float score=float.MaxValue,center=CombatCenterX();
  foreach(var a in UnityEngine.Object.FindObjectsOfType<CameraLockArea>()){
   if(!MarkothAreaValid(a))continue;var b=a.GetComponent<Collider2D>().bounds;
   float candidate=Mathf.Abs(b.center.x-center)+Mathf.Abs(b.center.y-entrance.y)*.1f;
   if(a.maxPriority)candidate-=1000;
   if(a.cameraXMin>0&&a.cameraXMax>=a.cameraXMin&&a.cameraXMax-a.cameraXMin<=3)candidate-=100;
   if(candidate<score){score=candidate;best=a;}
  }
  return best;
 }
 static bool MarkothAreaValid(CameraLockArea a){if(!a||a.gameObject.scene.name!=arenaRoom)return false;var c=a.GetComponent<Collider2D>();return c&&c.bounds.size.x>=15&&c.bounds.size.x<=85&&c.bounds.size.y>=6&&c.bounds.size.y<=46;}
 static float MarkothCenter(CameraLockArea area){
  // The actual log's trigger spans x=4.7..46.9, including black side scenery.
  // Its authored camera centres (21.08..29.83) and native entry are interior.
  float min=area.cameraXMin,max=area.cameraXMax;
  if(min>0&&max>=min&&max-min<=20)return (min+max)*.5f;
  var c=GameCameras.instance?GameCameras.instance.cameraController:null;
  if(c&&c.mode==CameraController.CameraMode.LOCKED&&c.xLockMin>0&&c.xLockMax>=c.xLockMin&&c.xLockMax-c.xLockMin<=20)return (c.xLockMin+c.xLockMax)*.5f;
  return area.GetComponent<Collider2D>().bounds.center.x;
 }
 static CameraLockArea FindArea(){if(arenaRoom=="GG_Ghost_Markoth")return MarkothFloorArea();CameraLockArea best=null;float distance=float.MaxValue;var reference=new Vector3(arenaRoom.StartsWith("GG_Mantis_Lords",StringComparison.Ordinal)?CombatCenterX():entrance.x,entrance.y,entrance.z);foreach(var a in UnityEngine.Object.FindObjectsOfType<CameraLockArea>()){
  if(a.gameObject.scene.name!=arenaRoom)continue;var c=a.GetComponent<Collider2D>();if(!c||c.bounds.size.x<6||c.bounds.size.x>100)continue;
  var bounds=c.bounds;if(reference.x<bounds.min.x-2||reference.x>bounds.max.x+2||reference.y<bounds.min.y-2||reference.y>bounds.max.y+2)continue;
  float d=(bounds.center-reference).sqrMagnitude;if(d<distance){best=a;distance=d;}}
  return best;
 }
 const string EntryName="door_local8_pvp_entry";static TransitionPoint entry;
 static void RemoveEntry(){if(entry){TransitionPoint.TransitionPoints.Remove(entry);UnityEngine.Object.Destroy(entry.gameObject);}entry=null;}
 static void MakeEntry(Scene scene,Vector3 at){if(entry&&entry.gameObject.scene==scene){entry.transform.position=at;return;}RemoveEntry();var go=new GameObject(EntryName);UnitySceneManager.MoveGameObjectToScene(go,scene);go.transform.position=at;entry=go.AddComponent<TransitionPoint>();entry.isADoor=true;entry.dontWalkOutOfDoor=true;entry.nonHazardGate=true;entry.entryDelay=0;}
 static bool EnsureEntry(Scene scene,out Vector3 at){at=Vector3.zero;if(!scene.IsValid()||!scene.isLoaded)return false;
  if(scene.name==sourceRoom&&roster.Count>0)at=roster[0].Position;
  else {
   var boss=BossSceneController.Instance;if(boss&&boss.gameObject.scene==scene&&boss.heroSpawn)at=boss.heroSpawn.position;
   if(at==Vector3.zero)foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<BossSceneController>(true))if(b.heroSpawn){at=b.heroSpawn.position;break;}
   if(at==Vector3.zero)foreach(var gate in TransitionPoint.TransitionPoints)if(gate&&gate.gameObject.scene==scene&&gate.name!=EntryName){at=gate.transform.position;break;}
   if(at==Vector3.zero&&scene.name==arenaRoom)at=entrance;
   if(at==Vector3.zero)return false;
  }
  MakeEntry(scene,at);return true;
 }
 static Vector2? FindEntry(On.GameManager.orig_FindEntryPoint orig,GameManager gm,string name,Scene filter){
  if(Active&&name==EntryName){Vector3 at;var scene=filter.IsValid()?filter:UnitySceneManager.GetSceneByName(entryRoom);if(scene.name==entryRoom&&EnsureEntry(scene,out at))return new Vector2(at.x,at.y);}
  return orig(gm,name,filter);
 }
 static void EnterHero(On.GameManager.orig_EnterHero orig,GameManager gm,bool additive){if(Active&&!string.IsNullOrEmpty(entryRoom)){Vector3 at;EnsureEntry(UnitySceneManager.GetSceneByName(entryRoom),out at);}orig(gm,additive);}
}
}
