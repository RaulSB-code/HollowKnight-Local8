using System.Collections.Generic;
namespace KO.HollowKnight8 {
// Survives actor replacement and temporary disconnection; cleared only when the game session ends.
internal sealed class RoleSelectionMemory {
 readonly Dictionary<string,int> roles=new Dictionary<string,int>();
 readonly HashSet<string> prompted=new HashSet<string>();
 internal bool NeedsSelection(string player,string savedId=null){return !prompted.Contains(player)&&!RoleCatalog.Known(savedId);}
 internal int Role(string player,int saved){int value;return roles.TryGetValue(player,out value)?value:saved;}
 internal void Change(string player,int role){roles[player]=role;prompted.Add(player);}
 internal void Complete(string player,int role){Change(player,role);prompted.Add(player);}
 internal void Clear(){roles.Clear();prompted.Clear();}
}
}
