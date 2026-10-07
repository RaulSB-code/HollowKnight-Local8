using System;
namespace KO.HollowKnight8 {
internal static class CrystalDashRules {
 internal const float Radius=4.5f,LaunchGrace=.75f;
 internal static int Multiplier(int participants){return Math.Max(1,Math.Min(4,participants));}
 internal static double Factor(int participants){switch(Multiplier(participants)){case 2:return 3.5;case 3:return 4.75;case 4:return 5.5;default:return 1;}}
 internal static int Damage(int damage,int participants){return damage<=0?damage:(int)Math.Min(int.MaxValue,Math.Floor(damage*Factor(participants)+.5));}
 internal static string Name(string name){return (name??"").Replace(" ","").Replace("_","").Replace("-","").ToLowerInvariant();}
 internal static bool Charging(string state){var n=Name(state);return n.Contains("charg")&&!Stopped(state)&&n!="dashstart";}
 internal static bool Stopped(string state){var n=Name(state);return n=="inactive"||n=="idle"||n=="init"||n=="land"||n.Contains("cancel")||n.Contains("stop")||n.Contains("hitwall")||n.Contains("enddash")||n=="regaincontrol";}
 internal static bool Launch(string state){return Name(state)=="dashstart";}
 internal static bool Crystal(string name){var n=Name(name);return n.Contains("superdash")||n.Contains("crystaldash")||n.StartsWith("sdcrys")||n.StartsWith("sdcharge")||n.StartsWith("sdtrail")||n.StartsWith("sdburst")||n.StartsWith("sdfx");}
 // Colour only actual crystal artwork. Broad Superdash/Burst ancestors also
 // contain full-screen flash/light renderers and must never be tinted.
 internal static bool CrystalArt(string name){var n=Name(name);return n.StartsWith("sdcrys")||n=="crystals"||n=="crystal"||n.StartsWith("sdcrystal");}
 internal static bool Trail(string name){var n=Name(name);return n.StartsWith("sdtrail")||n.StartsWith("superdashtrail")||n=="superdashburst"||n=="sdburst"||n=="sdparticles"||n=="superdashparticles";}
 internal static bool DashEvent(string name){var n=Name(name);return n=="heroctrlentersuperdash"||n=="entersuperdash"||n.StartsWith("sdcharge")||n.StartsWith("sdcancel")||n.StartsWith("sddash")||n.StartsWith("sdstop")||n.StartsWith("superdash")||n.StartsWith("crystaldash");}
 internal static bool FailClip(string name){return Name(name).Contains("dreamgatefail");}
}
}
