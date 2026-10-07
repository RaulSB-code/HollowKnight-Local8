using System;
namespace KO.HollowKnight8 {
// Serializable settings contain no actor/game references. A running match uses a copy.
public sealed class PvpArenaOptions {
 public int Arena, Masks, BlueMasks, Soul=99, SoulCapacity=99, Reserve;
 public int SpellLevel=-1, DashCooldown=-1, HazardMasks=1;
 public bool InfiniteSoul, InfiniteWings, InfiniteDash, AllAbilities, AllowHealing=true, AllowCharms=true;
 public int[] PlayerMasks=new int[8];
 public void Normalize(){Arena=Clamp(Arena,0,PvpArenaCatalog.Scenes.Length-1);Masks=Clamp(Masks,0,50);BlueMasks=Clamp(BlueMasks,0,50);SoulCapacity=Clamp(SoulCapacity,1,999);Soul=Clamp(Soul,0,SoulCapacity);Reserve=Clamp(Reserve,0,99);SpellLevel=Clamp(SpellLevel,-1,2);DashCooldown=Clamp(DashCooldown,-1,2000);HazardMasks=Clamp(HazardMasks,1,20);if(PlayerMasks==null||PlayerMasks.Length!=8)PlayerMasks=new int[8];for(int i=0;i<8;i++)PlayerMasks[i]=Clamp(PlayerMasks[i],0,50);}
 public int Health(int player,int normal){return PlayerMasks[player]>0?PlayerMasks[player]:Masks>0?Masks:Math.Max(1,normal);}
 public PvpArenaOptions Copy(){var c=(PvpArenaOptions)MemberwiseClone();c.PlayerMasks=(int[])PlayerMasks.Clone();return c;}
 static int Clamp(int n,int low,int high){return Math.Max(low,Math.Min(high,n));}
}
internal static class PvpArenaCatalog {
 internal static readonly string[] Scenes={"","GG_Hornet_1","GG_Hollow_Knight","GG_Gruz_Mother_V","GG_Vengefly_V","GG_Ghost_Markoth_V","GG_Ghost_Xero_V","GG_Ghost_No_Eyes_V","GG_Mantis_Lords_V","GG_Ghost_Gorb_V","GG_Ghost_Markoth"};
 internal static readonly string[] Keys={"here","flat","large","spikes","void","markoth","xero","noeyes","mantis","gorb","markothfloor"};
 internal static bool OwnedScene(string scene,string arena){return !string.IsNullOrEmpty(arena)&&scene==arena;}
 internal static int Winner(int[] sides,bool[] alive,int count){return PvpRules.Outcome(sides,alive,count);}
}
}
