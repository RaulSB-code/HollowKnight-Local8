using System;
using System.Collections.Generic;
using UnityEngine;
namespace KO.HollowKnight8 {
// Only the owned, small crystal emitters found by CrystalDashCoop use this.
// Restore their native settings when the shared charge ends; never edit assets.
internal static class CrystalParticleDensity {
 sealed class Original {internal int Max;internal float Time,Distance;}
 static readonly Dictionary<ParticleSystem,Original> originals=new Dictionary<ParticleSystem,Original>();
 internal static void Apply(ParticleSystem ps){
  if(!ps)return;Original original;
  if(!originals.TryGetValue(ps,out original)){var m=ps.main;var e=ps.emission;original=new Original{Max=m.maxParticles,Time=e.rateOverTimeMultiplier,Distance=e.rateOverDistanceMultiplier};originals[ps]=original;}
  var main=ps.main;main.maxParticles=Math.Max(1,Math.Min(32,(int)Math.Ceiling(original.Max*.65f)));
  var emission=ps.emission;emission.rateOverTimeMultiplier=original.Time*.65f;emission.rateOverDistanceMultiplier=original.Distance*.65f;
 }
 internal static void Restore(ParticleSystem ps){Original original;if(!ps||!originals.TryGetValue(ps,out original))return;
  var main=ps.main;main.maxParticles=original.Max;var e=ps.emission;e.rateOverTimeMultiplier=original.Time;e.rateOverDistanceMultiplier=original.Distance;originals.Remove(ps);
 }
 internal static void Reset(){foreach(var ps in new List<ParticleSystem>(originals.Keys))Restore(ps);originals.Clear();}
}
}
