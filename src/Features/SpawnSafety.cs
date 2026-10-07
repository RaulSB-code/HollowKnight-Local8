using UnityEngine;
namespace KO.HollowKnight8 {
internal static class SpawnSafety {
 static readonly Collider2D[] overlaps=new Collider2D[128];
 static readonly RaycastHit2D[] casts=new RaycastHit2D[128];
 const int SolidLayers=(1<<8)|(1<<25);
 static bool Solid(Collider2D c){return c&&c.enabled&&!c.isTrigger&&(c.gameObject.layer==8||c.gameObject.layer==25)&&!c.GetComponentInParent<HeroController>();}
 static Vector2 Clearance(Vector3 size){return new Vector2(Mathf.Max(.1f,size.x+.10f),Mathf.Max(.1f,size.y-.04f));}
 // The native check shrinks width by .14, admitting a thin wall at the body's
 // edge. Keep a horizontal margin while allowing the feet to touch the floor.
 internal static bool Footprint(bool normal,PlayerSlot p,Vector3 at,Vector3 offset,Vector3 size,bool arena){
  if(!normal)return false;
  int count=Physics2D.OverlapBoxNonAlloc(at+offset,Clearance(size),0f,overlaps,SolidLayers);
  if(count==overlaps.Length)return false;
  for(int i=0;i<count;i++)if(Solid(overlaps[i]))return false;
  return true;
 }
 internal static bool ClearAt(PlayerSlot p,Vector3 at){
  if(p==null||!p.Hero)return false;
  var b=DuelGround.Body(p);string why;
  return DuelGround.Clear(p,at,b.center-p.Hero.transform.position,b.size,out why);
 }
 internal static bool Path(PlayerSlot p,Vector3 from,Vector3 to){
  if(p==null||!p.Hero||!ClearAt(p,to))return false;
  var b=DuelGround.Body(p);Vector3 offset=b.center-p.Hero.transform.position;
  if(!Footprint(true,p,from,offset,b.size,false))return false;
  Vector2 delta=to-from;float distance=delta.magnitude;if(distance<.01f)return true;
  int count=Physics2D.BoxCastNonAlloc(from+offset,Clearance(b.size),0f,delta/distance,casts,distance,SolidLayers);
  if(count==casts.Length)return false;
  for(int i=0;i<count;i++)if(Solid(casts[i].collider))return false;
  return true;
 }
 // Both revival and manual rescue use this same bounded search. A free point
 // across a wall is not a point alongside the ally: sweep the body to it too.
 internal static bool NearAlly(CoopSession s,PlayerSlot ally,PlayerSlot arriving,out Vector3 result){
  result=Vector3.zero;
  if(s==null||ally==null||!ally.Hero||arriving==null||!arriving.Hero)return false;
  Physics2D.SyncTransforms();Vector3 origin=ally.Hero.transform.position;
  var b=DuelGround.Body(arriving);Vector3 offset=b.center-arriving.Hero.transform.position;
  if(!s.InRoom(origin)||!ArenaGather.Allows(origin)||!Footprint(true,arriving,origin,offset,b.size,false))return false;
  float foot=b.size.y*.5f-offset.y,step=Mathf.Max(.8f,b.size.x+.16f);
  int preferred=(arriving.Index&1)==0?1:-1;
  for(int i=1;i<=4;i++)for(int side=0;side<2;side++){
   float x=origin.x+i*step*(side==0?preferred:-preferred);
   foreach(var hit in Physics2D.RaycastAll(new Vector2(x,origin.y+1.5f),Vector2.down,5f,1<<8)){
    if(!hit.collider||hit.collider.isTrigger||hit.normal.y<.6f)continue;
    var at=new Vector3(x,hit.point.y+foot+.06f,origin.z);
    if(!PartyRules.Near(at.x-origin.x,at.y-origin.y)||!s.InRoom(at)||!ArenaGather.Allows(at)||!s.IsSafe(at,arriving)||!Path(arriving,origin,at))continue;
    result=at;return true;
   }
  }
  // An airborne ally or a narrow ledge can safely share the same clear point.
  if(ClearAt(arriving,origin)){result=origin;return true;}return false;
 }
 // Recheck immediately before health/control/collider restoration. Saved
 // dream checkpoints and moving gates can bypass earlier destination checks.
 internal static bool Recovery(CoopSession s,PlayerSlot p,ref Vector3 at){
  Physics2D.SyncTransforms();
  if(s.InRoom(at)&&ClearAt(p,at))return true;
  var ally=s.LongestLiving(p);Vector3 near;
  if(ally!=null&&NearAlly(s,ally,p,out near)){at=near;Diagnostics.Write("SPAWN wall fallback P"+(p.Index+1)+" -> "+at);return true;}
  p.HazardUntil=Time.time+.35f;
  Diagnostics.Throttled("SPAWN waiting for clear body P"+(p.Index+1),new System.InvalidOperationException("Destination intersects terrain"));
  return false;
 }
}
}
