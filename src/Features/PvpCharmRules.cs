using System;
namespace KO.HollowKnight8 {
internal static class PvpCharmRules {
 internal static int Lifeblood(bool allowed,bool heart,bool core){return allowed?((heart?2:0)+(core?4:0)):0;}
 internal static int Joni(bool allowed,bool equipped,int health){return allowed&&equipped?Math.Max(1,(int)(health*1.4f)):0;}
 internal static int Natural(bool allowed,bool heart,bool broken,int baseHealth,int roleMasks){return Math.Max(1,baseHealth+roleMasks+(allowed&&heart&&!broken?2:0));}
}
}
