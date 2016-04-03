//#define DRAW_NAV_LINES

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


public class Unit_Kinematic : Unit {

    [HideInInspector]
    public Rigidbody2D Body;
    //[HideInInspector]

        //todo - [don't serialize this please, bitch]
    public Player Owner;

    public int PopCost = 4;

    [HideInInspector]
    public bool PathActive = false;
    [HideInInspector]
    public int SPi;

    // [HideInInspector]
    //public bool PathActive = false;
    //two targeting modes..
    public Unit Target = null;
    float EngageRange = 0;
    [HideInInspector]
    public Vector2 TargetP;

    public float _MaxSpeed = 5;
    public float _Acceleration = 2;

    float MaxSpeed_Cached, Acceleration_Cached;
    public float MaxSpeed {
        get { return MaxSpeed_Cached; }
        private set { }
    }
    public float Acceleration {
        get { return Acceleration_Cached; }
        private set { Debug.LogError("err"); }
    }
    public enum Buffable {
        MaxSpeed, Acceleration
    };
    struct Buff {
        public float TimeEnd;
        public float Factor;
    }
    List<Buff> MaxSpeed_Buffs, Acceleration_Buffs;

    delegate void Buff_Dlg(float baseVal, ref float cacheVal, ref List<Buff> buffs);
    void forEach_Buffable(Buff_Dlg act) {
        act(_MaxSpeed, ref MaxSpeed_Cached, ref MaxSpeed_Buffs);
        act(_Acceleration, ref Acceleration_Cached, ref Acceleration_Buffs);
    }

    void buffable(Buffable e, Buff_Dlg act) {
        switch(e) {
            case Buffable.MaxSpeed: act(_MaxSpeed, ref MaxSpeed_Cached, ref MaxSpeed_Buffs); break;
            case Buffable.Acceleration: act(_Acceleration, ref Acceleration_Cached, ref Acceleration_Buffs); break;
        }
    }
    public void buff(Buffable e, float factor, float time) {
        float end = time + Time.time;
        buffable(e, (float baseVal, ref float cacheVal, ref List<Buff> buffs) => {
            if(buffs == null) buffs = new List<Buff>();

            Buff b = new Buff();
            b.Factor = factor;
            b.TimeEnd = end;
            buffs.Add(b);
            BuffsDirty = true;
        });

    }
    bool BuffsDirty = false;  //todo seperate dirty flags for each var -- bits
    float ReBuff = float.MaxValue;

    //todo -- diminishing returns on buffs   ((x+1)**(1-n) - 1)/(1-n) ??

    void rebuff() {
        ReBuff = float.MaxValue;
        forEach_Buffable((float baseVal, ref float cacheVal, ref List<Buff> buffs) => {
            cacheVal = baseVal;
            if(buffs == null) return;
            for(int i = buffs.Count; i-- > 0; ) {
                var b = buffs[i];
                if(b.TimeEnd <= Time.time) {
                    buffs.RemoveAt(i);
                    if(buffs.Count == 0) {
                        buffs = null;
                        return;
                    }
                    continue;
                }
                if(ReBuff > b.TimeEnd)
                    ReBuff = b.TimeEnd;
                cacheVal += baseVal * b.Factor;
            }
        });
        BuffsDirty = false;
    }

    public float MaxTurnSpeed = 180;

    //- maximum offset the network sync stuff will allow, it will clamp to this range or tele if your wayout(2x) somehow
    public float OffsetSyncMax = 0.5f;
    public NavMesh NavMsh;

    public NavMesh.Path Path;
    public int CurNodeI;
    public NavMesh.Node CurNode;

    public int MaxSmoothIter = -1;
    public float PathRadius = 1.0f;

    protected NavMesh.Node TargetNode;

       float LPathTime = -1, LSmoothTime = -1;
    Vector2 LTPos; NavMesh.Node LTNode;
    public List<Vector2> SmoothPath = new List<Vector2>();
    static List<Vector2> WorkingSmoothPath = new List<Vector2>();

    [HideInInspector]
    public Unit_SyncHelper SyncO;  //todo client only

    virtual protected bool desPos(Vector2 dp) {
        var n = NavMsh.findNode(dp, TargetNode); //err here 
        if(n == null) return false;
        TargetNode = n;
        TargetP = dp;
        Target = null;

        PathActive = true;
        SyncO.PathActive = true;
        return true;
    }

    [ClientRpc]
    public void Rpc_DesPos(Vector2 dp) { desPos(dp); }

      [ClientRpc]
    public void Rpc_attackUnit(GameObject trgt) {

        if(trgt == null) return;
        var tu = trgt.GetComponent<Unit>();
        if(tu == null) return;
        var tuk = tu as Unit_Kinematic;
        if(tuk == null) {
            var tb = tu as Unit_Structure;
            TargetP = tb.Site.DropPoint.position;
            TargetNode = tb.Site.Dp_Node;
        } else {
            TargetNode = tuk.CurNode;
            TargetP = tuk.Body.position;
        }
        Target = tu;
        

        EngageRange = calcEngageRange(Target);

        EngageRange += tu.RoughRadius * 0.75f;
        if(Trgtn != null)
            Trgtn.Timer = 0;

        PathActive = true;
        SyncO.PathActive = true;    
    }

      new protected void Awake() {
        base.Awake();
        Body = GetComponent<Rigidbody2D>();

        forEach_Buffable((float baseVal, ref float cacheVal, ref List<Buff> buffs) => {
            cacheVal = baseVal;
        });
    }
    new protected void Start() {
        base.Start();
    //    DesPos = transform.position;
        NavMsh = FindObjectOfType<NavMesh>();
        TargetNode = CurNode = NavMsh.findNode(Trnsfrm.position);
       // Rpc_DesPos(Body.position); //WRONG!!
    }


    public void init(Player o) {
        base.init(o.Tm);
        Owner = o;
       // Debug.Log(" init " + name + "    " + GetInstanceID() + "  o " + Owner);
        /*  if(!Owner.isLocalPlayer) {
              var s = GetComponent<Selectable>();
              if(s != null) {

                  var pm = s.Projector.GetComponent<Projector>().material;
                  var c = pm.color;
                  c.b = pm.color.r;
                  c.r = pm.color.b;
                  pm.color = c;
              }
          } */

        //todo
        fixCol(o.Col);
      
    }
    /* public void init(Player o) {  NOTE:  --- it appears   Server's local client also gets Rpc function called.. which is bit weird...it's kind of handy so will tolerate it
        if(isServer) Rpc_init(o.gameObject);  //always do network bit first, slightly better

        _int_init(o);
    } */


    [ClientRpc]
    public void Rpc_init(GameObject oo) {
        // if(isServer) return; //better way..
        init(oo.GetComponent<Player>());

    }

    public void initSyncO() {

        var go = new GameObject();
        go.name = name + "  syncO";
        SyncO = go.AddComponent<Unit_SyncHelper>();
        SyncO.Body = go.AddComponent<Rigidbody2D>(Body);
        SyncO.Body.mass *= 10;
        var rj = go.AddComponent<RelativeJoint2D>();
        rj.correctionScale = 0.05f;
        var dj = go.AddComponent<DistanceJoint2D>();
        dj.distance = 0.5f;
        dj.maxDistanceOnly = true;
        dj.connectedBody = rj.connectedBody = Body;

        for(int i = 0; i < Trnsfrm.childCount; i++) {
            var t = Trnsfrm.GetChild(i);
            var c = t.GetComponentInChildren<Collider2D>();
            if( c== null || c.isTrigger ) continue;

            var go2 = Instantiate(t.gameObject);
            go2.transform.parent = SyncO.Trnsfrm;
        }

        var l = 31; if((this as Helio) != null) l--;
        foreach(Transform t in SyncO.GetComponentInChildren<Transform>()) {
            t.gameObject.layer = l;
        }
    }
    new protected void OnEnable() {
        base.OnEnable();
        SmoothPath = new List<Vector2>();
        SmoothPath.Add(TargetP = Trnsfrm.position);

        if(SyncO == null) initSyncO();
    }

    new protected void OnDisable() {
        base.OnDisable();

        if(SyncO != null) Destroy(SyncO.gameObject);

        if( Owner != null && (Owner.isLocalPlayer || isServer) ) {
            Owner.Pop -= PopCost;
            Owner = null;
          //  Debug.Log("OnDisable  " + name);
        }
    }
   
    void FixedUpdate() {
        updatePath();

        //fUpdate_ReSync();
    }
    protected void fUpdate_ReSync() {

        //if(isClient) {  //todo -- leave for testing 
        float lerp = 10.0f * Time.deltaTime;
        float off = (Body.position - SyncO.Body.position).magnitude;
//lerp *= off / OffsetSyncMax;
        if(off > OffsetSyncMax) {
            if(off > OffsetSyncMax * 2) {
                lerp = 1;
                //MvmntController.CMBias = MvmntController_SO.CMBias;  //todo
                //MvmntController.CRBias = MvmntController_SO.CRBias;
                SPi = SyncO.SPi;
                PathActive = SyncO.PathActive;
            } else {
                lerp = Mathf.Max(lerp, 1.0f - OffsetSyncMax / off);
            }
        } else {
        //    
        }
        //  lerp = 1;

        // Debug.Log(lerp);
        Body.MovePosition(Vector2.Lerp(Body.position, SyncO.Body.position, lerp));
        Body.MoveRotation(Mathf.LerpAngle(Body.rotation, SyncO.Body.rotation, lerp));
        Body.velocity = Vector2.Lerp(Body.velocity, SyncO.Body.velocity, lerp);
        float angV = Mathf.Lerp(Body.angularVelocity, SyncO.Body.angularVelocity, lerp);
        //Debug.Log(" angV  " + angV);
        if(!float.IsNaN(angV)) angV = 0; //not sure why we need this... todo..?
        Body.angularVelocity = angV;
    }

    new protected void Update() {
        base.Update();
        if(BuffsDirty || ReBuff < Time.time)
            rebuff();


       // fUpdate_ReSync();
    }

        /*
    public bool RefreshLA = true;
   
    
    void localAvoidance() {

        LocalAvoidance_FB.reset();

        LocalAvoidance_FB.line( Trnsfrm.position, TargetP,  Color.red );
        Vector2 cp = Trnsfrm.position;
        var vec = TargetP - cp;

        LocalAvoidance_FB.sphere(cp + vec.normalized * 4, 1, Color.red);

    }
    GizmoFeedBack LocalAvoidance_FB = new GizmoFeedBack(); */

 
    protected void checkTarget() {
        if(Target != null) {
            Vector2 tp = Target.Trnsfrm.position;

            var vec = tp - SyncO.Body.position;
            var sm = vec.sqrMagnitude;

            if(sm <  Util.pow2( EngageRange * 0.9f )) {

                SyncO.PathActive = PathActive = false;

            } else if(sm > EngageRange * EngageRange )
                SyncO.PathActive = PathActive = true;

            if(PathActive) {
                var tk = Target as Unit_Kinematic;
                if(tk != null) {
                    TargetP = tp;
                    TargetNode = tk.CurNode;
                } else {
                    var tb = Target as Unit_Structure;
                    TargetP = tb.Site.DropPoint.position;
                    TargetNode = tb.Site.Dp_Node;
                }
            } else {
                TargetNode = CurNode;
                TargetP = SyncO.Body.position;
            }
        }
    }

    protected void updatePath() {
        if(NavMsh == null) return;
        var lCn =  CurNode;
        CurNode = NavMsh.findNode(SyncO.Body. position, CurNode);
        if(CurNode == null) {
            Debug.Log("no node..");
            Debug.DrawLine(Trnsfrm.position, Vector3.zero);
        }

        checkTarget();

        Vector2 biasOff = (Vector2)Trnsfrm.up * RoughRadius*2 + Body.velocity / Acceleration;
        //cPos = SyncO.Body.position
        Debug.DrawLine(SyncO.Body.position + biasOff, SyncO.Body.position, Color.black);

        if(!PathActive) return;

        Vector2 tPos = TargetP, cPos = SyncO.Body.position;

        Vector2 tPos2 = tPos;

        if(CurNode != TargetNode) {

            if(Path != null && LPathTime - Time.time < -0.2f) {
                if(LPathTime - Time.time < -2.0f) Path = null;  //recalc every so often
                else {
                    var sqDiff = (LTPos - tPos).sqrMagnitude;
                    // if target moved much - need to recalculate path    
                    if(sqDiff > 4.0f || (LTNode != TargetNode && sqDiff > 0.5f)) Path = null;
                }
            }

            if(Path != null && lCn != CurNode) {
                if(!fixNodeI(ref CurNodeI, CurNode)) {
                    ///not resolved!! -- we got shunted off to side most likely..   --- or possibly rewound but too far - (likely cos we just repathed)
                   // Debug.Log("SHUNTED!!");
                    Path = null;  //todo - we may be able to quick fix some of thse cases
                }
            }

            if(Path == null) {
                Path = NavMsh.getPath2(cPos, cPos+ biasOff, tPos, TargetNode);
                if(Path != null) {
                    LTPos = tPos;
                    LTNode = TargetNode;
                    LPathTime = Time.time; LSmoothTime = -1.0f;
                    CurNodeI = Path.Smooth.Count - 1;
                    if(CurNode != Path.Smooth[CurNodeI].N)
                        Debug.LogError("err");
                    // ValidPos = Trnsfrm.position;
                    //  Debug.Log("pc  " + Path.Smooth.Count + "  cn " + CurNode);
                } else {
                    CurNodeI = -1;
                    //      Debug.Log("path fail");
                }
            }

            
            ///funnel!
            //  basicly recursivlely look at edge we must travers to get to next node until we find a corner in our way - then move thataway
            //  no corner in the way means just go towards target
            if(Time.time - LSmoothTime > 0.5f) {  //todo - can check if we have diverged from path to affect resmooth
                var smthP = WorkingSmoothPath;
                smthP.Clear();
                if(Path != null) {


#if DRAW_NAV_LINES                 
                    Vector2 lastPos = cPos;
                    for(int i = CurNodeI; i-- > 0; ) {
                        Debug.DrawLine(lastPos, Path.Smooth[i].P, Color.white);
                        Debug.DrawLine(lastPos, Path.Smooth[i].E1, Color.grey);
                        Debug.DrawLine(lastPos, Path.Smooth[i].E2, Color.grey);

                        lastPos = Path.Smooth[i].P;
                    }
                    Debug.DrawLine(lastPos, tPos, Color.white);
#endif
                    // for(; CurNodeI >= 0; CurNodeI--) {  //path is backwards - because .... reasons

                    funnel(cPos, ref tPos, CurNodeI);

                    var lp = cPos;
                    var cp = tPos; var cni = CurNodeI; var n = CurNode;
                    float dis = (lp - cp).magnitude;
                    float lDis = 0;
                    float maxDis = 25;


                    for(int maxIter = 10; maxIter-- > 0; ) {
                        // Debug.Log("iter " + maxIter);
                        smthP.Add(cp);
                        var tp = TargetP;
                        if(dis > maxDis) {
                            //   Debug.Log("PASS dis " + maxIter);
                            var v = (cp - lp);
#if DRAW_NAV_LINES  
                            Debug.DrawLine(lp, lp + v * (maxDis - lDis) / v.magnitude, Color.black);
#endif
                            break;
                        }
                        if((tp - cp).sqrMagnitude < 0.5f) {
                            //  Debug.Log("PASS  " + maxIter);
#if DRAW_NAV_LINES  
                            Debug.DrawLine(lp, cp, Color.black);
#endif
                            break;
                        }
#if DRAW_NAV_LINES  
                        Debug.DrawLine(lp, cp, Color.black);
#endif
                        var ln = n;
                        n = NavMsh.findNode(cp, n);

                        if(ln != n) {
                            if(n == TargetNode) {
                                //   Debug.Log("PASS  " + maxIter);
#if DRAW_NAV_LINES  
                                Debug.DrawLine(cp, tp, Color.black);
#endif
                                smthP.Add(tp);
                                break;
                            }
                            if(!fixNodeI(ref cni, n)) {
                                //    Debug.Log("FAIL --- you shall not pass");
                                smthP.Add(tp);  //hope for the best
                                break;
                            }
                        }

                        funnel(cp, ref tp, cni);
                        float d = (cp - tp).magnitude;
                        //   Debug.Log("dis " + d);
                        float minStep = 0.75f;
                        if(d < minStep) {
                            tp = cp + (tp - cp) * minStep / d;
                            d = minStep;
                        }
                        lDis = dis;
                        dis += d;
                        //   break;
                        lp = cp;
                        cp = tp;
                    }

                } else {
                    smthP.Add(TargetP);
                    if(CurNode != TargetNode) //fallen off map somehow  ... used to happen when colliders dind match map and also current node wasn't clamped    -- try and move back to last valid position
                        //  tPos = ValidPos;
                        Debug.Log("Awk noes we appear to have fallen off the map");
                }

                if( SmoothPath.Count > 0 ) {
                    if(Vector2.Dot((Body.position - smthP[0]).normalized, (Body.position - SmoothPath[SyncO.SPi]).normalized) < 0.8f)
                        steerUpdate();
                } else steerUpdate();

                SmoothPath = new List<Vector2>(smthP); //todo 
                SPi = SyncO.SPi = 0;
            }           
        } else {
            Path = null;
            //bool dirty = false;
            if(SmoothPath.Count < 1) {
               // dirty = true;
                SmoothPath.Add(TargetP);
                steerUpdate();
            } else if( (SmoothPath[0]- TargetP).sqrMagnitude > 0.5f ) {
               // dirty = true;

                if( Vector2.Dot( (Body.position - TargetP).normalized, (Body.position - SmoothPath[0]).normalized ) < 0.8f )
                    steerUpdate();
                SmoothPath.Clear();
                SmoothPath.Add(TargetP);
            }
            SPi = SyncO.SPi = 0;
        }


       // Debug.DrawLine( Body.position, tPos, Color.white);
      //  Debug.DrawLine( tPos2, tPos, Color.white);
        //DesPos = tPos;
        //vec = tPos - cPos;

       // DesVec = vec;
    }
    protected virtual void steerUpdate() {  //todo -  move path finding up a level - generic - no virtual
 
    }

    void funnel(Vector2 cPos, ref Vector2 tPos, int ni ) {
        Vector2 cnrA, cnrB;
        // the arc defined between cnrA < cPos > cnrB  is the range of current valid directions 
        cnrA = smoothHelper(Path.Smooth[ni].E1, cPos, true);
        cnrB = smoothHelper(Path.Smooth[ni].E2, cPos, false);
#if DRAW_NAV_LINES  
        Debug.DrawLine(cPos, cnrA, Color.red);
        Debug.DrawLine(cPos, cnrB, Color.blue);
#endif
        var td = float.MaxValue;
        for(int ci = ni - 1; ci >= 0; ci--) {
            // only one of these will be different from current corners   -- this would be an obvious place to optimise (todo)
            ///!!! no longer true -- still place to optimise - vert ids
            Vector2 nCnrA = smoothHelper(Path.Smooth[ci].E1, cPos, true);
            Vector2 nCnrB = smoothHelper(Path.Smooth[ci].E2, cPos, false);


            //new corner may refine our current valid arc 
            //  it may refine it so far that the angle of the arc becomes 0 - ie a direction - cnrA . cnrB  would be  exactly the same direction  - if so then we are done here

            

            if((nCnrA - cnrA).sqrMagnitude > 0.01f) {
                float sgn = Util.sign(nCnrA, cPos, cnrA);
#if DRAW_NAV_LINES  
                Debug.DrawLine(nCnrA, cnrA, Color.magenta);
#endif
                if(Util.sign(nCnrA, cPos, cnrA) > 0) {
                    if(Util.sign(cnrB, cPos, nCnrA) < 0) {
                        tPos = cnrB;
                        td = (tPos - cPos).sqrMagnitude;
                        //  tPos2 = (Path.Smooth[ci].E1 + Path.Smooth[ci].E2) * 0.5f;
                        // else tPos2 = TargetP;
                        //  Debug.Log("breakb");
                        //break;
                    } else {
#if DRAW_NAV_LINES  
                        Debug.DrawLine(nCnrA, cnrA, Color.red);
                        Debug.DrawLine(cPos, (cnrA + nCnrA) * 0.5f, Color.red);
#endif
                        cnrA = nCnrA;
                    }
                }
            }
            if((nCnrB - cnrB).sqrMagnitude > 0.01f) {
                float sgn = Util.sign(cnrB, cPos, nCnrB);
#if DRAW_NAV_LINES  
                Debug.DrawLine(nCnrB, cnrB, Color.cyan);
#endif
                if(Util.sign(cnrB, cPos, nCnrB) > 0) {
                    if(Util.sign(nCnrB, cPos, cnrA) < 0) {
                        if((cPos - cnrA).sqrMagnitude < td) {
                            tPos = cnrA;
                            // tPos2 = (Path.Smooth[ci].E1 + Path.Smooth[ci].E2) * 0.5f;
                            // else tPos2 = TargetP;
                            td = 0;
                        }
                        //   Debug.Log("breaka");
                        //break;
                    } else {
#if DRAW_NAV_LINES  
                        Debug.DrawLine(nCnrB, cnrB, Color.blue);
                        Debug.DrawLine(cPos, (cnrB + nCnrB) * 0.5f, Color.blue);
#endif
                        cnrB = nCnrB;
                    }
                }
            }
            if(td != float.MaxValue) return;
        }

        //todo --maybe -- could move these to start and on assign new crnr???

        var otp = tPos;
        if( Util.sign(cnrB, cPos, tPos) < 0) {
            //if(CurNodeI > 0) tPos2 = (Path.Smooth[CurNodeI - 1].E1 + Path.Smooth[CurNodeI - 1].E2)*0.5f;
            //else tPos2 = TargetP;

            tPos = cnrB;
            td = (tPos - cPos).sqrMagnitude;
        //    Debug.Log("  a " + td );
        }
        if(Util.sign(otp, cPos, cnrA) < 0) {
            //if(CurNodeI > 0) tPos2 = (Path.Smooth[CurNodeI - 1].E1 + Path.Smooth[CurNodeI - 1].E2)*0.5f;
            //else tPos2 = TargetP;
          //  Debug.Log("  b " + (cPos - cnrA).sqrMagnitude);
           if((cPos - cnrA).sqrMagnitude < td) {
                tPos = cnrA;
                // tPos2 = (Path.Smooth[ci].E1 + Path.Smooth[ci].E2) * 0.5f;
                // else tPos2 = TargetP;
                td = 0;
            }
        }

    }
    bool fixNodeI( ref int cni, NavMesh.Node n ) {

        for(int i = cni; i-- > 0; ) {
            if(Path.Smooth[i].N == n) {
                //Debug.Log("fwd  " + i + "    old -| " + cni);
                cni = i;
               // goto label_CurNodeResolved;  //resloved - we are following path properly
                return true;
            }
        }
        for(int i = cni; ++i < Path.Smooth.Count; ) {
            if(Path.Smooth[i].N == n) {
               // Debug.Log("rewind  " + i + "    old -| " + cni);
                cni = i;
                return true;
              //  goto label_CurNodeResolved;  //resloved - we have somhw been bumped back... no matter rewind a bit...  (also rewind smoother)
            }
        }

        return false;
    }

///!  le copied and pasted from earlier thing i did 
    // adjusts the corner by pathing radius of the AI - ie go around corners not through them
    Vector2 smoothHelper(Vector2 edge, Vector2 at, bool flip) {  //todo think of better name...
        Vector2 b = edge, aToC = edge - at, vec = aToC;
        vec = Vector3.Cross(vec, Vector3.forward);  //lazy..
        vec.Normalize();
        if(flip) vec = -vec;
        
        b += vec * PathRadius;
#if DRAW_NAV_LINES  
        Debug.DrawLine(edge, b, Color.grey);
#endif
        for(int iter = 2; iter-- > 0; ) {
            var aToB = b - at;
            var sMag = aToB.sqrMagnitude;
            if(sMag < 0.001f) return b;
            var d = Vector2.Dot(aToB, aToC) / sMag;
            b = at + aToB * d;
            b = edge + (b - edge).normalized * PathRadius;
#if DRAW_NAV_LINES  
            Debug.DrawLine(edge, b, Color.yellow);
#endif
        }
        return b;
    }

}
