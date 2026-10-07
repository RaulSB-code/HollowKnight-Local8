namespace KO.HollowKnight8 {
internal static class PvpHealthWinner {
 static readonly int[] health=new int[8];
 internal static int Normal(int[] sides,bool[] alive,int count){
  int winner=PvpRules.Outcome(sides,alive,count);
  if(winner!=PvpRules.Ongoing||!(PvpMatch.remaining<=0))return winner;
  for(int i=0;i<count;i++){
   var p=PvpMatch.roster[i].Player;
   health[i]=p.Vitals.Health+p.Vitals.Blue;
  }
  return PvpHealthRules.Timeout(sides,alive,health,count);
 }
}
}
