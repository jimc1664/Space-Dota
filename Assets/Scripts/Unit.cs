using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


public class Unit : NetBehaviour {

    [SyncVar(hook = "Hook_DesPos")]
    public Vector2 DesPos;

    void Hook_DesPos( Vector2 dp ) {
        DesPos = dp;
        PathActive = true;
        if( SyncO != null ) SyncO.PathActive = true;
    }

    [HideInInspector]
    public bool PathActive = false;

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

    //- maximum offset the network sync stuff will allow, it will clamp to this range or tele if your wayout(2x) somehow
    public float OffsetSyncMax = 0.5f, RoughRadius = 1;


    public List<Transform> HitTargets;  //todo - very optimisable..

    void Awake() {
        Trnsfrm = transform;
        Body = GetComponent<Rigidbody2D>();
   //     DesPos = transform.position;
    }
    void Start() {
    //    DesPos = transform.position;
    }

    [HideInInspector]
    public Unit_SyncHelper SyncO;  //todo client only
    void OnEnable() {

       

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
    }

    /*
    public override void OnStartClient() {
       // Debug.Log("OnStartClient  "+name);
    } */

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


        //}
    }

    public float Dodge = 0.0f;
    public float MaxHealth = 1337.0f;
    public float Armor = 3;

    float Health = 1;
    public Slider HealthBar;
    public Transform Canvas;

    protected void Update() {

        Canvas.rotation = Quaternion.LookRotation( -Camera.main.transform.forward, Vector3.forward);
        
    }
    public void damage(float dmg, float ap) {
        float effArmor = Mathf.Max(0, Armor - ap) * (1 + Random.Range(0.0f, 0.5f));

        float reduction = 0.025f+ 1.95f/ (1.0f + Mathf.Exp(  effArmor *0.5f ) );
        dmg *= reduction;

        Health -= (  dmg) / MaxHealth;

        Debug.Log("dmg " + dmg + "  reduction = " + reduction + "  effArmor = " + effArmor + "  Health = " + (Health * MaxHealth));

        HealthBar.value = 1- Health;
    }

    void OnDrawGizmos() {
        Trnsfrm = transform;
        Gizmos.color = Color.black;
        if(SyncO != null) 
            Gizmos.DrawLine(Trnsfrm.position, SyncO.Trnsfrm.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Trnsfrm.position, OffsetSyncMax);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Trnsfrm.position, RoughRadius );
    }



}
