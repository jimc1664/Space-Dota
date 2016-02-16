using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


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

        Rpc_DesPos(Body.position); //WRONG!!
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
        SyncO.PathActive = PathActive = true;
        Mv_Wheeled.update(Trnsfrm, Body, ref PathActive, this);
        Mv_Wheeled.update(SyncO.Trnsfrm, SyncO.Body, ref SyncO.PathActive, this);

        
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

    int LastPath = -1;
    Vector2 LTPos;
    void updatePath() {
        if(NavMsh == null) return;
        CurNode = NavMsh.findNode(Trnsfrm.position, CurNode);

     /*   if(Target != null) {
            TargetP = Target.Trnsfrm.position;
            TargetNode = Target.CurNode;
        } */

        Vector2 tPos = TargetP, cPos = Body.position;
        //Debug.DrawLine(tPos, cPos); 

        // if target moved much - need to recalculate path

        if(Path != null && LastPath - Time.frameCount < -10) {
            // Debug.Log(" LastPath -Time.frameCount " + (LastPath - Time.frameCount) );

            if((LTPos - tPos).sqrMagnitude > 0.25f || LastPath - Time.frameCount < -180) Path = null;

        }

        if(Path == null) {
            Path = NavMsh.getPath(cPos, tPos, TargetNode);

            if(Path != null) {
                LTPos = tPos;
                LastPath = Time.frameCount;
                CurNodeI = Path.Smooth.Count - 1;
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
        Vector2 vec, cnrA, cnrB;
        if(Path != null) {
            Vector2 lastPos = cPos;



            for(int i = CurNodeI; i-- > 0; ) {
                Debug.DrawLine(lastPos, Path.Smooth[i].P, Color.black);
                Debug.DrawLine(lastPos, Path.Smooth[i].E1, Color.grey);
                Debug.DrawLine(lastPos, Path.Smooth[i].E2, Color.grey);

                lastPos = Path.Smooth[i].P;
            }
            Debug.DrawLine(lastPos, tPos, Color.black );
            


            for(; CurNodeI >= 0; CurNodeI--) {  //path is backwards - because .... reasons
                //  tPos = Path.Smooth[CurNode].P;                
                ///
               
                if(Util.sign(Path.Smooth[CurNodeI].E2, cPos, Path.Smooth[CurNodeI].E1) < 0) {
                    // if  so  then we passed through an edge - advance our place along the path
                    //  -done awkard way because fo how things were refactored - not neatening because it won't be more efficent - and might somehow implode

                    //CurNode = Path.Smooth[CurNodeI].;///todo 

                    continue;
                }

                // the arc defined between cnrA < cPos > cnrB  is the range of current valid directions 
                cnrA = smoothHelper(Path.Smooth[CurNodeI].E1, cPos, true);
                cnrB = smoothHelper(Path.Smooth[CurNodeI].E2, cPos, false);
                Debug.DrawLine(cPos, cnrA, Color.red);
                Debug.DrawLine(cPos, cnrB, Color.blue);
               
                float sgn = -1;
                //  int msi = MaxSmoothIter;
                for(int ci = CurNodeI - 1; ; ci--) {

                    if(ci < 0) break;  //reached end of path

                    // only one of these will be different from current corners   -- this would be an obvious place to optimise (todo)
                    ///!!! no longer true -- still place to optimise - vert ids

                    Vector2 nCnrA = smoothHelper(Path.Smooth[ci].E1, cPos, true);
                    Vector2 nCnrB = smoothHelper(Path.Smooth[ci].E2, cPos, false);

                    ///!!!!!!BIG TODO ---BUG!! 
                    /// the order thse are done in matters..    by closest new corner

                    //new corner may refine our current valid arc 
                    //  it may refine it so far that the angle of the arc becomes 0 - ie a direction - cnrA . cnrB  would be  exactly the same direction  - if so then we are done here
                    if((nCnrA - cnrA).sqrMagnitude > 0.01f ) {
                        sgn = Util.sign(nCnrA, cPos, cnrA);
                        Debug.DrawLine(nCnrA, cnrA, Color.magenta);
                        if(Util.sign(nCnrA, cPos, cnrA) > 0) {
                            if(Util.sign(cnrB, cPos, nCnrA) < 0) {
                                tPos = cnrB;
                                  Debug.Log("breakb");
                                break;
                            }
                            Debug.DrawLine(nCnrA, cnrA, Color.red);
                            Debug.DrawLine(cPos, (cnrA + nCnrA) * 0.5f, Color.red);
                            cnrA = nCnrA;
                        }
                    }
                    if((nCnrB - cnrB).sqrMagnitude > 0.01f ) {
                        sgn = Util.sign(cnrB, cPos, nCnrB);
                        Debug.DrawLine(nCnrB, cnrB, Color.cyan);
                        if(Util.sign(cnrB, cPos, nCnrB) > 0) {
                            if(Util.sign(cnrB, cPos, nCnrA) < 0) {
                                tPos = cnrA;
                                   Debug.Log("breaka");
                                break;
                            }
                            Debug.DrawLine(nCnrB, cnrB, Color.blue);
                            Debug.DrawLine(cPos, (cnrB + nCnrB) * 0.5f, Color.blue);
                            cnrB = nCnrB;
                        }
                    }
                    // if( msi-- == 0 ) break;
                }

                if(Util.sign(cnrB, cPos, tPos) < 0) tPos = cnrB;
                if(Util.sign(tPos, cPos, cnrA) < 0) tPos = cnrA;

                /*//   Debug.Log("sgn  " + sgn);
                Debug.DrawLine(cPos, fnlA, Color.green);
                Debug.DrawLine(cPos, fnlB, Color.red);
                Debug.DrawLine(cPos, tPos, Color.white); */
                break;
            }

        } else {
            if(NavMsh.findNode(cPos) != TargetNode) //fallen off map somehow  ... used to happen when colliders dind match map and also current node wasn't clamped    -- try and move back to last valid position
                //  tPos = ValidPos;
                Debug.Log("Awk noes we appear to have fallen off the map");
        }

        Debug.DrawLine( Body.position, tPos, Color.white);
        DesPos = tPos;
        //vec = tPos - cPos;

       // DesVec = vec;
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
