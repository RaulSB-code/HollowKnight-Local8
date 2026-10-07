using UnityEngine;
namespace KO.HollowKnight8 {
internal static class PvpArenaUI {
 static string T(string key){return PvpArena.Text(key);}
 static GUIStyle Label {get{var s=new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleLeft};s.normal.textColor=new Color(.94f,.94f,.98f);return s;}}
 static GUIStyle Button {get{return new GUIStyle(GUI.skin.button){fontSize=14};}}
 internal static void Draw(Local8Runtime runtime,CoopSession session,float width,ref float row){
  var c=PvpArena.Options;row+=14;GUI.Label(new Rect(5,row,width-10,30),T("title"),new GUIStyle(Label){fontSize=22});row+=36;
  GUI.Label(new Rect(5,row,width-10,25),T("destination"),Label);row+=29;
  bool prior=GUI.enabled;GUI.enabled=prior&&!PvpMatch.Running;
  Toggle(T("charms"),ref c.AllowCharms,width,ref row);
  float gridHeight=Mathf.CeilToInt(PvpArenaCatalog.Keys.Length/2f)*34f;int picked=GUI.SelectionGrid(new Rect(5,row,width-10,gridHeight),c.Arena,Names(),2,Button);if(picked!=c.Arena){c.Arena=picked;PvpArena.SaveOptions();}row+=gridHeight+10;
  if(c.Arena>0){
   GUI.Label(new Rect(7,row,width-14,44),T("details"),new GUIStyle(Label){wordWrap=true,fontSize=12});row+=52;
   GUI.Label(new Rect(5,row,width-10,25),T("presets"),Label);row+=30;
   string[] presets={T("native"),T("twenty"),T("magic"),T("air")};
   for(int i=0;i<4;i++)if(GUI.Button(new Rect(5+i*(width-10)/4,row,(width-10)/4-4,31),presets[i],Button))Preset(c,i);row+=42;
   Number(T("masks"),ref c.Masks,0,50,width,ref row,true);
   Number(T("blue"),ref c.BlueMasks,0,50,width,ref row);
   Number(T("capacity"),ref c.SoulCapacity,1,999,width,ref row);
   Number(T("soul"),ref c.Soul,0,c.SoulCapacity,width,ref row);
   Number(T("reserve"),ref c.Reserve,0,99,width,ref row);
   Toggle(T("infinitesoul"),ref c.InfiniteSoul,width,ref row);
   Toggle(T("wings"),ref c.InfiniteWings,width,ref row);
   Toggle(T("dash"),ref c.InfiniteDash,width,ref row);
   Toggle(T("abilities"),ref c.AllAbilities,width,ref row);
   Toggle(T("healing"),ref c.AllowHealing,width,ref row);
   Number(T("cooldown"),ref c.DashCooldown,-1,2000,width,ref row,true);
   Number(T("hazard"),ref c.HazardMasks,1,20,width,ref row);
   GUI.Label(new Rect(5,row+4,width*.31f,29),T("spells"),Label);
   c.SpellLevel=GUI.SelectionGrid(new Rect(width*.32f,row,width*.67f,32),c.SpellLevel+1,new[]{T("native"),T("off"),"1","2"},4,Button)-1;row+=43;
   GUI.Label(new Rect(5,row,width-10,27),T("perplayer"),new GUIStyle(Label){fontSize=18});row+=34;
   foreach(var p in session.Players)Number("P"+(p.Index+1)+"  "+T("masks"),ref c.PlayerMasks[p.Index],0,50,width,ref row,true);
  }
  GUI.enabled=prior;
  if(PvpArena.Active&&GUI.Button(new Rect(5,row,width-10,36),T("return"),Button))PvpArena.Back();row+=44;
  if(c.Arena>0&&!PvpArena.Active&&GUI.Button(new Rect(5,row,width-10,36),T("enter"),Button))PvpArena.StartRequest();row+=44;
  c.Normalize();PvpArena.SaveOptions();
 }
 static string[] Names(){var a=new string[PvpArenaCatalog.Keys.Length];for(int i=0;i<a.Length;i++)a[i]=T(PvpArenaCatalog.Keys[i]);return a;}
 static void Toggle(string label,ref bool value,float width,ref float row){value=GUI.Toggle(new Rect(10,row,width-20,30),value,label,new GUIStyle(GUI.skin.toggle){fontSize=14});row+=34;}
 static void Number(string label,ref int n,int min,int max,float width,ref float row,bool native=false){GUI.Label(new Rect(8,row,width-20,25),label+": "+(native&&n==min?T("native"):n.ToString()),Label);row+=26;float v=GUI.HorizontalSlider(new Rect(18,row,width-130,20),n,min,max);n=Mathf.RoundToInt(v);if(GUI.Button(new Rect(width-104,row-6,45,27),"-",Button))n=Mathf.Max(min,n-1);if(GUI.Button(new Rect(width-54,row-6,45,27),"+",Button))n=Mathf.Min(max,n+1);row+=31;}
 static void Preset(PvpArenaOptions c,int i){c.Masks=i==1?20:0;c.BlueMasks=0;c.SoulCapacity=99;c.Soul=99;c.Reserve=0;c.InfiniteSoul=i==2;c.InfiniteWings=i==3;c.InfiniteDash=i==3;c.AllAbilities=i>1;c.SpellLevel=i==2?2:-1;c.AllowHealing=true;c.DashCooldown=-1;c.HazardMasks=1;for(int p=0;p<8;p++)c.PlayerMasks[p]=0;}
}
}
