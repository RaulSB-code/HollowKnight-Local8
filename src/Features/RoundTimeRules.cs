using System;
namespace KO.HollowKnight8 {
internal static class RoundTimeRules {
 internal static float Duration(int seconds){return seconds<=0?float.PositiveInfinity:seconds;}
 internal static bool Visible(float seconds){return !float.IsInfinity(seconds)&&!float.IsNaN(seconds)&&seconds>=0;}
 internal static string Format(float seconds){int s=Math.Max(0,(int)Math.Ceiling(seconds));return (s/60)+":"+(s%60).ToString("00");}
}
}
