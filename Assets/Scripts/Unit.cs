using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GizmoFeedBack {
    public struct Sphere {
        public Vector3 At;
        public float Radius;
        public Color Col;
    };
    public struct Line {
        public Vector3 P1, P2;
        public Color Col;
    };
    List<Sphere> Spheres = new List<Sphere>();
    List<Line> Lines = new List<Line>();

    public void sphere(Vector3 a, float r, Color c) {
        var s = new Sphere();
        s.At = a;
        s.Radius = r;
        s.Col = c;
        Spheres.Add(s);
    }
    public void line(Vector3 a, Vector3 b, Color c) {
        var s = new Line();
        s.P1 = a; s.P2 = b;
        s.Col = c;
        Lines.Add(s);
    }
    public void draw() {

        foreach(var s in Spheres) {
            Gizmos.color = s.Col;
            Gizmos.DrawWireSphere(s.At, s.Radius);
        }
        foreach(var s in Lines) {
            Gizmos.color = s.Col;
            Gizmos.DrawLine(s.P1, s.P2);
        }
    }
    public void reset() {
        Spheres.Clear();
        Lines.Clear();
    }

};
public class Unit : NetBehaviour {


   
    [HideInInspector]
    public Transform Trnsfrm;
    [HideInInspector]
    public Rigidbody2D Body;
    //[HideInInspector]
    
    //todo - [don't serialize this please, bitch]
    public Player Owner;

    public GameObject VisDat;

    public float MaxSpeed = 5;
    public float TurnSpeed = 10;
    public float Acceleration = 2;
    public float Friction = 40;

    //- maximum offset the network sync stuff will allow, it will clamp to this range or tele if your wayout(2x) somehow
    public float OffsetSyncMax = 0.5f, RoughRadius = 1;


    public List<Transform> HitTargets;  //todo - very optimisable..


    NavMesh NavMsh;

    public NavMesh.Path Path;
    public int CurNodeI;
    NavMesh.Node CurNode;

    public int MaxSmoothIter = -1;
    public float PathRadius = 1.0f;

    protected NavMesh.Node TargetNode;

    [HideInInspector]
    public bool PathActive = false;
    //two targeting modes..
    // public CharMotor Target;
    [HideInInspector]
    public Vector2 DesPos, TargetP;



    [ClientRpc]
    public void Rpc_DesPos(Vector2 dp) {

        Debug.Log("Rpc_DesPos");
        //Target = null;
        var n = NavMsh.findNode(dp, TargetNode);
        if(n == null) return;
        TargetNode = n;
        TargetP = dp;
        PathActive = true;
        if(SyncO != null) SyncO.PathActive = true;
    }


    void Awake() {
        Trnsfrm = transform;
        Body = GetComponent<Rigidbody2D>();
    }
    void Start() {
    //    DesPos = transform.position;
        NavMsh = FindObjectOfType<NavMesh>();

       // Rpc_DesPos(Body.position); //WRONG!!
    }

    [HideInInspector]
    public Unit_SyncHelper SyncO;  //todo client only
    void OnEnable() {

        NetMan.UnitCount++;

        var go = new GameObject();
        go.name = name + "  syncO";
        SyncO = go.AddComponent<Unit_SyncHelper>();
        SyncO.Body = go.AddComponent<Rigidbody2D>(Body);
        SyncO.PathActive = PathActive;
        for( int i = 0; i < Trnsfrm.childCount; i++ ) {
            var t = Trnsfrm.GetChild(i);
            if(t.GetComponentInChildren<Collider2D>() == null) continue;
            var go2 = Instantiate(t.gameObject);
            go2.transform.parent =SyncO.Trnsfrm;
        }
        foreach(Transform t in SyncO.GetComponentInChildren<Transform>()) {
            t.gameObject.layer = 31;
        }
    }

    void OnDisable() {
        if(SyncO != null) Destroy(SyncO.gameObject);

        NetMan.UnitCount--;
    }


    public void fixCol( Color c ) {

        foreach(var mr in VisDat.GetComponentsInChildren<MeshRenderer>()) {
            foreach(Material mat in mr.materials) {
               // Debug.Log("fix?");
                if(mat.name == Sys.get().BaseColMat.name + " (Instance)" ) {
                  //  Debug.Log("fix!!!!!????");

                    mat.color = c;
                }
            }
        }
    }

    public void init(Player o) {
        Owner = o;
        if(!Owner.isLocalPlayer) {
            var s = GetComponent<Selectable>();
            if(s!=null) Destroy(s);
        }

        foreach(var c in GetComponentsInChildren<Collider2D>()) {
            c.gameObject.layer = o.Layer;
        }

        fixCol(o.Col);
    }
    /* public void init(Player o) {  NOTE:  --- it appears   Server's local client also gets Rpc function called.. which is bit weird...it's kind of handy so will tolerate it
        if(isServer) Rpc_init(o.gameObject);  //always do network bit first, slightly better

        _int_init(o);
    } */
    [ClientRpc]
    public void Rpc_init(GameObject oo) { init(oo.GetComponent<Player>()); }


    void FixedUpdate() {

        updatePath();
       // localAvoidance();


        //SyncO.PathActive = PathActive = true;
       // Mv_Wheeled.update(Trnsfrm, Body, ref PathActive, this);
     //   Debug.Log(" SyncO.PathActive " + SyncO.PathActive);
        Mv_Wheeled.update(SyncO.Trnsfrm, SyncO.Body, ref SyncO.PathActive, this, LocalAvoidance_FB );

        
        //if(isClient) {  //todo -- leave for testing 
        float lerp = 10.0f *Time.deltaTime ;
        float off = (Body.position - SyncO.Body.position).magnitude;


        if(off > OffsetSyncMax) {
            if(off > OffsetSyncMax * 2) {
                lerp = 1;
            } else {
                lerp = Mathf.Max( lerp, 1.0f  - OffsetSyncMax / off );
            }
        }
        lerp = 1;

       // Debug.Log(lerp);
        Body.MovePosition(Vector2.Lerp(Body.position, SyncO.Body.position, lerp));
        Body.MoveRotation(Mathf.LerpAngle(Body.rotation, SyncO.Body.rotation, lerp));
        Body.velocity = Vector2.Lerp(Body.velocity, SyncO.Body.velocity, lerp);
        float angV = Mathf.Lerp(Body.angularVelocity, SyncO.Body.angularVelocity, lerp);
        //Debug.Log(" angV  " + angV);
        if(!float.IsNaN(angV)) angV = 0; //not sure why we need this... todo..?
        Body.angularVelocity = angV;

    }

    public float Dodge = 0.0f;
    public float MaxHealth = 1337.0f;
    public float Armor = 3;

    [SyncVar] //todo - lazy 
    float Health = 1;
    public bool Invinciblity = false;  //for very subtle cheating...


    public Slider HealthBar;
    public Transform Canvas;

    protected void Update() {
        Canvas.rotation = Quaternion.LookRotation( -Camera.main.transform.forward, Vector3.forward);
        HealthBar.value = 1 - Health;
    }
    public void damage(float dmg, float ap) {
        float effArmor = Mathf.Max(0, Armor - ap) * (1 + Random.Range(0.0f, 0.5f));

        float reduction = 0.025f+ 1.95f/ (1.0f + Mathf.Exp(  effArmor *0.5f ) );
        dmg *= reduction;

        Health -= (  dmg) / MaxHealth;

       // Debug.Log("dmg " + dmg + "  reduction = " + reduction + "  effArmor = " + effArmor + "  Health = " + (Health * MaxHealth));

        HealthBar.value = 1- Health;

        if(Invinciblity) Health = Mathf.Max(Health, 0.01f);

        if(Health < 0) Destroy(gameObject);
    }

    public bool RefreshLA = true;
   
    void localAvoidance() {

        LocalAvoidance_FB.reset();

        LocalAvoidance_FB.line( Trnsfrm.position, TargetP,  Color.red );
        Vector2 cp = Trnsfrm.position;
        var vec = TargetP - cp;

        LocalAvoidance_FB.sphere(cp + vec.normalized * 4, 1, Color.red);

    }
    GizmoFeedBack LocalAvoidance_FB = new GizmoFeedBack();

    float LPathTime = -1, LSmoothTime = -1;
    Vector2 LTPos; NavMesh.Node LTNode;
    List<Vector2> SmoothPath = new List<Vector2>();
    int SPi;
    static List<Vector2> WorkingSmoothPath = new List<Vector2>();
    void updatePath() {
        if(NavMsh == null) return;
        var lCn =  CurNode;
        CurNode = NavMsh.findNode(Trnsfrm.position, CurNode);
        if(CurNode == null) {
            Debug.LogError("no node..");

        }
     /*   if(Target != null) {
            TargetP = Target.Trnsfrm.position;
            TargetNode = Target.CurNode;
        } */

        Vector2 tPos = TargetP, cPos = Body.position;

        Vector2 tPos2 = tPos;
        var smthP = WorkingSmoothPath;


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
                    Debug.Log("SHUNTED!!");
                    Path = null;  //todo - we may be able to quick fix some of thse cases
                }
            }

            if(Path == null) {
                Path = NavMsh.getPath(cPos, tPos, TargetNode);
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

            if(Path != null) {

                smthP.Clear();
                     
                Vector2 lastPos = cPos;
                for(int i = CurNodeI; i-- > 0; ) {
                    Debug.DrawLine(lastPos, Path.Smooth[i].P, Color.white);
                    Debug.DrawLine(lastPos, Path.Smooth[i].E1, Color.grey);
                    Debug.DrawLine(lastPos, Path.Smooth[i].E2, Color.grey);

                    lastPos = Path.Smooth[i].P;
                }
                Debug.DrawLine(lastPos, tPos, Color.white);

                // for(; CurNodeI >= 0; CurNodeI--) {  //path is backwards - because .... reasons

                funnel(cPos, ref tPos, CurNodeI);

                var lp = cPos;
                var cp = tPos; var cni = CurNodeI; var n = CurNode;
                float dis = (lp - cp).magnitude;
                float lDis = 0;
                float maxDis = 25;
                for(int maxIter = 10; maxIter-- > 0; ) {
                   // Debug.Log("iter " + maxIter);
                    var tp = TargetP;
                    if(dis > maxDis) {
                     //   Debug.Log("PASS dis " + maxIter);
                        var v = (cp - lp);

                        Debug.DrawLine(lp, lp + v * (maxDis - lDis) / v.magnitude, Color.black);
                        break;
                    }
                    if((tp - cp).sqrMagnitude < 0.5f) {
                      //  Debug.Log("PASS  " + maxIter);
                        Debug.DrawLine(lp, cp, Color.black);
                        break;
                    }
                    Debug.DrawLine(lp, cp, Color.black);
                    var ln = n;
                    n = NavMsh.findNode(cp, n);

                    if(ln != n) {
                        if(n == TargetNode) {
                         //   Debug.Log("PASS  " + maxIter);
                            Debug.DrawLine(cp, tp, Color.black);
                            break;
                        }
                        if(!fixNodeI(ref cni, n)) {
                        //    Debug.Log("FAIL --- you shall not pass");
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
                if(CurNode != TargetNode) //fallen off map somehow  ... used to happen when colliders dind match map and also current node wasn't clamped    -- try and move back to last valid position
                    //  tPos = ValidPos;
                    Debug.Log("Awk noes we appear to have fallen off the map");
            }
        } else {
            Path = null;
            bool dirty = false;
            if(SmoothPath.Count < 1) {
                dirty = true;
                SmoothPath.Add(TargetP);
            } else if( (SmoothPath[0]- TargetP).sqrMagnitude > 0.5f ) {
                dirty = true;
                SmoothPath[0] = TargetP;
                SPi = 0;
            }       
        }


       // Debug.DrawLine( Body.position, tPos, Color.white);
      //  Debug.DrawLine( tPos2, tPos, Color.white);
        DesPos = tPos;
        //vec = tPos - cPos;

       // DesVec = vec;
    }

    void funnel(Vector2 cPos, ref Vector2 tPos, int ni ) {
        Vector2 cnrA, cnrB;
        // the arc defined between cnrA < cPos > cnrB  is the range of current valid directions 
        cnrA = smoothHelper(Path.Smooth[ni].E1, cPos, true);
        cnrB = smoothHelper(Path.Smooth[ni].E2, cPos, false);
        Debug.DrawLine(cPos, cnrA, Color.red);
        Debug.DrawLine(cPos, cnrB, Color.blue);

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
                Debug.DrawLine(nCnrA, cnrA, Color.magenta);
                if(Util.sign(nCnrA, cPos, cnrA) > 0) {
                    if(Util.sign(cnrB, cPos, nCnrA) < 0) {
                        tPos = cnrB;
                        td = (tPos - cPos).sqrMagnitude;
                        //  tPos2 = (Path.Smooth[ci].E1 + Path.Smooth[ci].E2) * 0.5f;
                        // else tPos2 = TargetP;
                        //  Debug.Log("breakb");
                        //break;
                    } else {
                        Debug.DrawLine(nCnrA, cnrA, Color.red);
                        Debug.DrawLine(cPos, (cnrA + nCnrA) * 0.5f, Color.red);
                        cnrA = nCnrA;
                    }
                }
            }
            if((nCnrB - cnrB).sqrMagnitude > 0.01f) {
                float sgn = Util.sign(cnrB, cPos, nCnrB);
                Debug.DrawLine(nCnrB, cnrB, Color.cyan);
                if(Util.sign(cnrB, cPos, nCnrB) > 0) {
                    if(Util.sign(cnrB, cPos, nCnrA) < 0) {
                        if((cPos - cnrA).sqrMagnitude < td) {
                            tPos = cnrA;
                            // tPos2 = (Path.Smooth[ci].E1 + Path.Smooth[ci].E2) * 0.5f;
                            // else tPos2 = TargetP;
                            td = 0;
                        }
                        //   Debug.Log("breaka");
                        //break;
                    } else {
                        Debug.DrawLine(nCnrB, cnrB, Color.blue);
                        Debug.DrawLine(cPos, (cnrB + nCnrB) * 0.5f, Color.blue);
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
        Debug.DrawLine(edge, b, Color.grey);

        for(int iter = 2; iter-- > 0; ) {
            var aToB = b - at;
            var sMag = aToB.sqrMagnitude;
            if(sMag < 0.001f) return b;
            var d = Vector2.Dot(aToB, aToC) / sMag;
            b = at + aToB * d;
            b = edge + (b - edge).normalized * PathRadius;

            Debug.DrawLine(edge, b, Color.yellow);
        }
        return b;
    }


    void OnDrawGizmos() {
        Trnsfrm = transform;

        LocalAvoidance_FB.draw();


        return;
        Gizmos.color = Color.black;
        if(SyncO != null) 
            Gizmos.DrawLine(Trnsfrm.position, SyncO.Trnsfrm.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Trnsfrm.position, OffsetSyncMax);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Trnsfrm.position, RoughRadius);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(Trnsfrm.position, PathRadius);
    }



}
