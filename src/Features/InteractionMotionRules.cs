using System;
namespace KO.HollowKnight8 {
internal static class InteractionMotionRules {
 // Use the actual library duration; malformed or exotic skin clips cannot
 // keep an interaction locked indefinitely.
 internal static float Duration(int frames,float fps){return frames<=0||fps<=0||float.IsNaN(fps)||float.IsInfinity(fps)?.33f:Math.Max(.08f,Math.Min(3f,frames/fps));}
 internal static float Progress(float elapsed,float duration){return Math.Max(0,Math.Min(1,elapsed/Math.Max(.08f,duration)));}
 internal static float Ease(float t){return t*t*(3-2*t);}
 internal static float Hop(float t){return 4*t*(1-t)*.24f;}
}
}
