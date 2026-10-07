using System;
using System.Reflection;
using UnitySceneManager=UnityEngine.SceneManagement.SceneManager;
using On;
using UnityEngine;
using UnityEngine.Audio;

namespace KO.HollowKnight8 {
internal static class BossMusicGuard {
 static bool installed;
 static HealthManager lastBoss;
 static float lastAlive;
 static float nextLog;
 // Death FSMs can stop the six music sources or change an unnamed mixer
 // snapshot without going through the cue/snapshot hooks. Remember only these
 // sources and repair them briefly after a death/result, never on room return.
 static readonly FieldInfo sourceField=typeof(AudioManager).GetField("musicSources",BindingFlags.Instance|BindingFlags.NonPublic);
 sealed class Playing {internal AudioSource Source;internal AudioClip Clip;internal int Sample;internal float Volume,LastPlaying;}
 static Playing[] playing;
 static AudioManager tracked;
 static MusicCue trackedCue;
 static string trackedRoom;
 static int livingMask=-1;
 static float repairUntil,nextSnapshot,combatCheck;
 static bool combat;
 internal static void RoundEvent(){repairUntil=Time.unscaledTime+5;nextSnapshot=0;}
 static void ForgetPlayback(){playing=null;tracked=null;trackedCue=null;trackedRoom=null;livingMask=-1;repairUntil=nextSnapshot=combatCheck=0;combat=false;}
 internal static void Tick(){try{TickPlayback();}catch(Exception e){ForgetPlayback();Diagnostics.Throttled("MUSIC continuity",e);}}
 static void TickPlayback(){
  var gm=GameManager.instance;var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(!gm||s==null||!s.Active||gm.IsLoadingSceneTransition||!gm.IsGameplayScene()){ForgetPlayback();return;}
  if(gm.isPaused||!Application.isFocused)return;
  var audio=gm.AudioManager;var cue=audio?audio.CurrentMusicCue:null;string room=UnitySceneManager.GetActiveScene().name;
  if(!audio||!cue||Silent(cue,gm)){ForgetPlayback();return;}
  if(tracked!=audio||trackedCue!=cue||trackedRoom!=room){
   float pending=tracked==null?repairUntil:0;ForgetPlayback();tracked=audio;trackedCue=cue;trackedRoom=room;repairUntil=pending;
   var sources=sourceField==null?null:sourceField.GetValue(audio) as AudioSource[];
   if(sources!=null){playing=new Playing[sources.Length];for(int i=0;i<sources.Length;i++)playing[i]=new Playing{Source=sources[i]};}
  }
  int mask=0;foreach(var p in s.Players)if(p!=null&&p.Alive&&p.Ready&&p.Index>=0&&p.Index<8)mask|=1<<p.Index;
  bool death=livingMask>=0&&(livingMask&~mask)!=0;livingMask=mask;
  if(death){RoundEvent();combatCheck=0;}
  // Cache the encounter query, especially FindObjectsOfType in Godhome.
  if(Time.unscaledTime>=combatCheck){combat=CombatAlive(s);combatCheck=Time.unscaledTime+.25f;}
  bool protect=combat&&!(s.TeamWipe||s.AllowVanillaDeath)||PvpArena.KeepsMusic;
  bool repaired=false,hasPlayback=false;
  if(playing!=null)foreach(var saved in playing){
   var source=saved.Source;if(!source||!source.isActiveAndEnabled)continue;
   if(protect&&Time.unscaledTime<repairUntil&&saved.Clip&&Time.unscaledTime-saved.LastPlaying<=5){
    // Never replace a different phase/scene clip installed by the game.
    if(!source.isPlaying&&(!source.clip||source.clip==saved.Clip)){
     source.clip=saved.Clip;source.timeSamples=Math.Max(0,Math.Min(saved.Sample,saved.Clip.samples-1));source.volume=saved.Volume;source.Play();repaired=true;
    }else if(source.clip==saved.Clip&&source.isPlaying&&source.volume==0&&saved.Volume>0){source.volume=saved.Volume;repaired=true;}
   }
   if(source.isPlaying&&source.clip){hasPlayback=true;saved.Clip=source.clip;saved.Sample=source.timeSamples;if(source.volume>0)saved.Volume=source.volume;saved.LastPlaying=Time.unscaledTime;}
  }
  // Result/countdown deaths sometimes mute only the mixer; the cue and its
  // sources remain playing. Reassert this cue's own snapshot during that window.
  if(protect&&hasPlayback&&cue.Snapshot&&Time.unscaledTime<repairUntil&&Time.unscaledTime>=nextSnapshot&&(PvpArena.KeepsMusic||repaired)){
   cue.Snapshot.TransitionTo(.15f);nextSnapshot=Time.unscaledTime+.6f;
  }
  if(repaired)Log("resumed after death",cue,cue);
 }

 internal static void Install(){if(installed)return;installed=true;On.AudioManager.ApplyMusicSnapshot+=Snapshot;On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter+=SnapshotAction;}
 internal static void Uninstall(){if(!installed)return;installed=false;On.AudioManager.ApplyMusicSnapshot-=Snapshot;On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter-=SnapshotAction;Reset();}
 internal static void Reset(){lastBoss=null;lastAlive=0;nextLog=0;ForgetPlayback();}

 static bool NearParty(HealthManager boss,CoopSession s){
  if(!boss||s==null)return false;
  Vector3 where=boss.transform.position;
  foreach(PlayerSlot p in s.Players)if(p!=null&&p.Alive&&p.Ready&&p.Hero&&Vector2.Distance(p.Hero.transform.position,where)<95f)return true;
  return false;
 }
 static bool Living(HealthManager boss,CoopSession s){return boss&&boss.gameObject.activeInHierarchy&&boss.hp>0&&!boss.GetIsDead()&&NearParty(boss,s);}
 static bool CombatAlive(CoopSession s){
  if(s==null||!s.Active||GameManager.instance==null||!GameManager.instance.IsGameplayScene()||GameManager.instance.IsLoadingSceneTransition)return false;
  // PvP has no live boss, and a round can briefly have no survivors. Keep the
  // arena cue through results/countdowns, until the return trip actually starts.
  if(PvpArena.KeepsMusic)return true;
  if(s.TeamWipe||s.AllowVanillaDeath)return false;
  // Native death-prefab actions are global even when only one co-op hero dies.
  if(PartySoul.IndividualDeathAudio){foreach(var p in s.Players)if(p!=null&&p.Alive&&p.Ready&&p.Hero)return true;}
  foreach(HealthManager boss in ArenaGather.engagedBosses)if(Living(boss,s)){lastBoss=boss;lastAlive=Time.unscaledTime;return true;}
  HealthManager target=ArenaGather.encounterTarget;
  if(Living(target,s)&&ArenaGather.Boss(target)){lastBoss=target;lastAlive=Time.unscaledTime;return true;}
  // Godhome can open a boss room without an arena gather or a player-owned hit.
  if(BossSceneController.IsBossScene)foreach(HealthManager boss in UnityEngine.Object.FindObjectsOfType<HealthManager>())
   if(Living(boss,s)&&!boss.GetComponent<LocalShade>()){lastBoss=boss;lastAlive=Time.unscaledTime;return true;}
  // A boss may briefly deactivate between phases; never protect a dead boss.
  return lastBoss&&lastBoss.hp>0&&!lastBoss.GetIsDead()&&NearParty(lastBoss,s)&&Time.unscaledTime-lastAlive<3f;
 }
 static bool Silent(MusicCue cue,GameManager game){
  if(cue==game.noMusicCue)return true;
  if(!cue||!string.IsNullOrEmpty(cue.OriginalMusicEventName))return false;
  try{for(int i=0;i<6;i++){MusicCue.MusicChannelInfo channel=cue.GetChannelInfo((MusicChannels)i);if(channel!=null&&channel.Clip)return false;}}
  catch(NullReferenceException){return false;}
  return true;
 }
 static void Log(string kind,UnityEngine.Object current,UnityEngine.Object next){
  if(Time.unscaledTime<nextLog)return;nextLog=Time.unscaledTime+0.7f;
  Diagnostics.Write("MUSIC kept " +kind+" current="+(current?current.name:"none")+" requested="+(next?next.name:"none")+" boss="+(lastBoss?lastBoss.name:"none"));
 }
 internal static void Cue(On.AudioManager.orig_ApplyMusicCue orig,AudioManager audio,MusicCue cue,float delay,float transition,bool snapshot){
  CoopSession s=Plugin.Self==null?null:Plugin.Self.Session;
  GameManager game=GameManager.instance;
  MusicCue current=audio.CurrentMusicCue;
  if(game!=null&&current&&current!=game.noMusicCue&&cue!=current&&CombatAlive(s)){
   MusicCue requested=cue;
   if(cue&&s!=null&&s.Data!=null)requested=cue.ResolveAlternatives(s.Data);
   if(Silent(requested,game)){Log("boss cue",current,requested);return;}
   Log("boss transition",current,requested);
  }
  orig(audio,cue,delay,transition,snapshot);
 }
 static bool RejectSnapshot(AudioMixerSnapshot next,AudioManager audio){
  GameManager game=GameManager.instance;if(!game||!next||!audio)return false;
  MusicCue current=audio.CurrentMusicCue;
  AudioMixerSnapshot silence=game.noMusicCue?game.noMusicCue.Snapshot:null;
  if((next==silence||next==game.noMusicSnapshot)&&current&&current!=game.noMusicCue&&current.Snapshot!=next&&CombatAlive(Plugin.Self==null?null:Plugin.Self.Session)){
   Log("combat snapshot",current,next);return true;
  }
  return false;
 }
 static void Snapshot(On.AudioManager.orig_ApplyMusicSnapshot orig,AudioManager audio,AudioMixerSnapshot next,float delay,float transition){if(!RejectSnapshot(next,audio))orig(audio,next,delay,transition);}
 // Death FSMs can bypass AudioManager and transition the mixer directly.
 // Finish the action even when skipping its mute so the death animation proceeds.
 static void SnapshotAction(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig,HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot action){
  GameManager game=GameManager.instance;
  if(action.snapshot!=null&&game&&RejectSnapshot(action.snapshot.Value as AudioMixerSnapshot,game.AudioManager)){action.Finish();return;}
  orig(action);
 }
}
}
