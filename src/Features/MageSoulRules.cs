using System;
namespace KO.HollowKnight8 {
// A drop is exactly 1/32 of the normal 99-point vessel, not one MP point.
// Carry the fractional remainder, and admit at most two events in any second.
internal sealed class MageSoulRules {
 float older=-100f,newer=-100f;
 int remainder;
 internal int Gain(float now){
  if(now<older)older=newer=-100f;
  if(now-older<1f)return 0;
  older=newer;newer=now;
  remainder+=99;int gain=remainder/32;remainder%=32;return gain;
 }
}
}
