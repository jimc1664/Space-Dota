using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

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
    [HideInInspector]
    public Player Owner;

    public GameObject VisDat;

    public float MaxSpeed = 5;
    public float TurnSpeed = 10;
    public float Acceleration = 2;

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
        fixCol(o.Col);
    }
    /* public void init(Player o) {  NOTE:  --- it appears   Server's local client also gets Rpc function called.. which is bit weird...it's kind of handy so will tolerate it
        if(isServer) Rpc_init(o.gameObject);  //always do network bit first, slightly better

        _int_init(o);
    } */
    [ClientRpc]
    public void Rpc_init(GameObject oo) { init(oo.GetComponent<Player>()); }

   // [ServerCallback]
    void FixedUpdate() {

        Mv_Wheeled.update(Trnsfrm, Body, ref PathActive, this);
        Mv_Wheeled.update(SyncO.Trnsfrm, SyncO.Body, ref SyncO.PathActive, this);


        //if(isClient) {
            Body.MovePosition( Vector2.Lerp( Body.position, SyncO.Body.position, 10.0f *Time.deltaTime  )  );
            Body.velocity =  Vector2.Lerp( Body.velocity, SyncO.Body.velocity, 10.0f *Time.deltaTime );
            Body.MoveRotation( Mathf.LerpAngle( Body.rotation, SyncO.Body.rotation, 10.0f *Time.deltaTime  ) );

        //}
    }

    void OnDrawGizmos() {
       // Trnsfrm = transform;
        Gizmos.color = Color.black;
        if(SyncO != null) 
            Gizmos.DrawLine(Trnsfrm.position, SyncO.Trnsfrm.position);
    }



}
