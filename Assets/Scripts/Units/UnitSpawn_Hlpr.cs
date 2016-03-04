using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;


//todo.. requires unit... forgot sysntax and intellisense is pooped
public class UnitSpawn_Hlpr : NetBehaviour {


    [HideInInspector] public Transform Prev, Next;
    [HideInInspector] public float D;


    Unit U;
    void Awake() {
        U = GetComponent<Unit>();
        U.enabled = false;
        if(U.enabled == true) Debug.LogError("err");
        U.VisDat.SetActive(false);


        foreach( Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

      //  U.Body.
    //    enabled = false;
      //  gameObject.SetActive(false);
    }

    public void activate() {
        U.VisDat.SetActive(true);

        foreach(Collider2D c2 in GetComponentsInChildren<Collider2D>()) {
            c2.enabled = true;
        }

    }

    float Timer = 0;


    [ClientRpc]
    public void Rpc_init(GameObject co, byte spI) {
       
        var c = co.GetComponent<Carrier>();
        if(c == null || (uint)spI >= (uint)c.SpawnPoints.Count) {  //note: in network functions we are unduly careful
            Destroy(gameObject);
            Debug.LogError("Error in UnitSpawn_Hlpr::init  " + c + "  ---- " + (int)spI);
            return;
        }

        Player o = c.Owner;

        if(o.isLocalPlayer && !isServer) {
            o.Squids -= (float) U.SquidCost;
            o.Pop += U.PopCost;
        }
        activate();
        U.init(o);

        var sp = c.SpawnPoints[spI];
        Prev = sp.transform;

        if(Prev.childCount > 0) Next = Prev.GetChild(0);
       // U.Trnsfrm.parent = c.Trnsfrm;
        U.Trnsfrm.position = Prev.position;
        U.Trnsfrm.rotation = Prev.rotation;

        Timer = 1;
        //enabled = true;
        // gameObject.SetActive(true);
        

        var j = gameObject.AddComponent<DistanceJoint2D>();  //disble inter collision by magic
        j.connectedBody = c.Body;
        j.maxDistanceOnly = true;
        j.distance = 99999.0f;
    }

    void Update() {

        if(Timer <= 0) {  //created but not init-ed
            if((Timer -= Time.deltaTime) < -3) {
                Destroy( gameObject);               
            }
            return;
        }
        
        if( Next != null) {
            float spd = U.MaxSpeed / 2;
            float dis = (Next.position - Prev.position).magnitude;

            D += spd * Time.deltaTime / dis;
            if(D > 1.0f) {
                Prev = Next;
                if(Prev.childCount > 0) {
                    Next = Prev.GetChild(0);
                    D -= 1;
                } else {
                    Next = null;
                    U.Trnsfrm.position = Prev.position;
                }                
            } 
        }
        if(Next != null) {
            var p = Vector3.Lerp(Prev.position, Next.position, D);
            U.Body.MovePosition(p);
            U.Body.MoveRotation( Quaternion.Lerp(Prev.rotation, Next.rotation, D).eulerAngles.z );
        } else { // our carrier == kaboom-boom  or just done..
            U.enabled = true;
            Destroy(GetComponent<DistanceJoint2D>());
            Destroy(this);          
            return;
        } 

    }


}
