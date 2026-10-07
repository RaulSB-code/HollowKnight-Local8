using System;
namespace KO.HollowKnight8 {
internal static class PvpHealthRules {
 // Remaining masks, including blue masks. Teams pool their living members'
 // health; free-for-all sides contain one player. Equal totals are a draw.
 internal static int Timeout(int[] sides,bool[] alive,int[] health,int count){
  int winner=0;long best=0;bool tied=false;
  for(int i=0;i<count;i++){
   if(!alive[i])continue;
   bool seen=false;for(int j=0;j<i;j++)if(alive[j]&&sides[j]==sides[i]){seen=true;break;}
   if(seen)continue;
   long total=0;for(int j=i;j<count;j++)if(alive[j]&&sides[j]==sides[i])total+=Math.Max(0,health[j]);
   if(total>best){best=total;winner=sides[i];tied=false;}else if(total==best)tied=true;
  }
  return tied?0:winner;
 }
}
}
