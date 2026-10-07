using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace KO.HollowKnight8 {
// Definitions and translated resources are independent of UI/input. Save stable IDs, not list indices.
internal sealed class RoleDefinition {
 internal readonly string Id;
 internal float Nail=1f, Spell=1f, Summons=1f, AttackSpeed=1f, DashRecovery=1f, Run=1f, FocusTime=1f, Invulnerability=1f;
 internal int Masks;
 internal bool SoulLeech, HealingAura, RiskOvercharm, LowHealthRage;
 internal RoleDefinition(string id) { Id=id; }
 internal string Name { get { return RoleText.Get(Id+".name"); } }
 internal string Description { get { return RoleText.Get(Id+".desc"); } }
}
internal static class RoleCatalog {
 internal static readonly RoleDefinition[] All = {
  new RoleDefinition("none"),
  new RoleDefinition("mage") {Nail=.9f, Spell=1.1f, SoulLeech=true},
  new RoleDefinition("warrior") {Nail=1.1f, Spell=.9f, Invulnerability=1.15f},
  new RoleDefinition("healer") {FocusTime=1.15f, HealingAura=true},
  new RoleDefinition("shaman") {RiskOvercharm=true},
  new RoleDefinition("explorer") {Nail=.9f, AttackSpeed=1.1f, DashRecovery=.85f},
  new RoleDefinition("guardian") {Masks=1, Invulnerability=1.15f, Run=.9f, DashRecovery=1.10f},
  new RoleDefinition("berserker") {FocusTime=1.35f, LowHealthRage=true},
  new RoleDefinition("summoner") {Nail=.9f, Spell=.9f, Summons=1.3f}
 };
 internal static int Index(string id) { for(int i=0;i<All.Length;i++) if(All[i].Id==id) return i; return 0; }
 internal static bool Known(string id) {return !string.IsNullOrEmpty(id)&&All[Index(id)].Id==id;}
 internal static float NailFactor(RoleDefinition r,bool low) {return r.Nail*(r.LowHealthRage&&low?1.2f:1f);}
 internal static float AttackFactor(RoleDefinition r,bool low) {return r.AttackSpeed*(r.LowHealthRage&&low?1.1f:1f);}
 // Two columns, matching the supplied vertical artwork (five left, four right).
 internal static int VerticalNext(int id,int dx,int dy) {
  int split=(All.Length+1)/2, col=id<split?0:1, row=col==0?id:id-split, count=col==0?split:All.Length-split;
  if(dy!=0) return (col==0?0:split)+Math.Max(0,Math.Min(count-1,row-dy));
  if(dx<0&&col==0)return -1;
  if(dx>0&&col==1)return id;
  if(dx!=0){int other=col==0?All.Length-split:split;int r=count<=1?0:(int)Math.Round((double)row*(other-1)/(count-1));return (col==0?split:0)+r;}
  return id;
 }
 internal static int JoinNext(int id,int dx,int dy) {
  int split=(All.Length+1)/2, row=id<split?0:1, col=row==0?id:id-split, count=row==0?split:All.Length-split;
  if(dx!=0)return (row==0?0:split)+Math.Max(0,Math.Min(count-1,col+dx));
  if(dy>0&&row==1)return Math.Min(split-1,col);
  if(dy<0&&row==0)return split+Math.Min(All.Length-split-1,col);
  return id;
 }
}
internal static class RoleText {
 static readonly string[] Languages={"ES","EN","FR","DE","IT","PT","RU","ZH","JA","KO"};
 static readonly Dictionary<string,string[]> Texts=Load();
 internal static string Language {get {try{return global::Language.Language.CurrentLanguage().ToString();}catch{return "EN";}}}
 static Dictionary<string,string[]> Load(){var d=new Dictionary<string,string[]>();foreach(string resource in new[]{"Local8.Roles.Text","Local8.UI.Text"})using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){
 if(stream==null)continue;using(var reader=new StreamReader(stream)){string line;while((line=reader.ReadLine())!=null){if(line.Length==0||line[0]=='#')continue;var cells=line.Split('\t');if(cells.Length!=11)continue;var v=new string[10];Array.Copy(cells,1,v,0,10);d[cells[0]]=v;}}}return d;}
 internal static bool Has(string key){return Texts.ContainsKey(key);}
 internal static string Get(string key){string[] v;if(!Texts.TryGetValue(key,out v))return key;int i=Array.IndexOf(Languages,Language);return v[i<0?1:i];}
}
}
