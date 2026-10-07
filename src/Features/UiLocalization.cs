namespace KO.HollowKnight8 {
internal static class UiLocalization {
 internal static string Get(string key){return RoleText.Get("ui."+key);}
 internal static string After(string translated,string key,string sheet,string fallback){
  return sheet=="LOCAL8"&&key!=null&&RoleText.Has("ui."+key)?Get(key):translated;
 }
}
}
