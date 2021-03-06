﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;


//todo.. requires unit... forgot sysntax and intellisense is pooped
public class UnitSpawn_Hlpr : NetBehaviour {


    [HideInInspector] public Transform Prev, Next;
    [HideInInspector] public float D;


    Unit_Kinematic U;
    void Awake() {
        U = GetComponent<Unit_Kinematic>();
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
        U.fixColliders();

        U.initSyncO();

        var j = gameObject.AddComponent<DistanceJoint2D>();  //disble inter collision by magic
        j.connectedBody = Car.Body;
        j.maxDistanceOnly = true;
        j.distance = 99999.0f;


        j = U.SyncO.gameObject.AddComponent<DistanceJoint2D>();  //disble inter collision by magic
        j.connectedBody = Car.SyncO.Body;
        j.maxDistanceOnly = true;
        j.distance = 99999.0f;       
    }

    float Timer = 0;

    Carrier Car;
    Carrier.SpawnPoint SP;
    [ClientRpc]
    public void Rpc_init(GameObject co, byte spI, float squids, float urSquids ) {
       
        Car = co.GetComponent<Carrier>();
        if(Car == null || (uint)spI >= (uint)Car.SpawnPoints.Count) {  //note: in network functions we are unduly careful
            Destroy(gameObject);
            Debug.LogError("Error in UnitSpawn_Hlpr::init  " + Car + "  ---- " + (int)spI);
            return;
        }

        Player o = Car.Owner;

        if(o.isLocalPlayer && !isServer) {
        //    o.Squids -= (float) U.SquidCost;
            o.Squids = squids;  //todo probably not correct place for these
            o.UnrefSquids = urSquids;
            o.Pop += U.PopCost;
        }

        U.init(o);

        SP = Car.SpawnPoints[spI];
        SP.CurSpwn = this;
        Prev = SP.FirstPoint;

        if(Prev.childCount > 0) Next = Prev.GetChild(0);
       // U.Trnsfrm.parent = c.Trnsfrm;


        Timer = 1;
        //enabled = true;
        // gameObject.SetActive(true);
        





    }
    bool Spawning = false;

    int Temp = 5;
    void Update() {

        if(!Spawning) {
            if(Timer <= 0) {  //created but not init-ed
                if((Timer -= Time.deltaTime) < -3) {
                    Destroy(gameObject);
                }
                return;
            }
            if(Prev == null) { //carrier died??

                Destroy(gameObject);
                return;
            }

            U.Trnsfrm.position = Prev.position;

            if(SP.IsOpen) {
                activate();
                Spawning = true; 
            }

        } else {
            if(Next != null) {
                float spd = U.MaxSpeed *0.8f;
                float dis = (Next.position - Prev.position).magnitude;

                D += spd * Time.deltaTime / dis;
                if(D > 1.0f) {
                    Prev = Next;
                    if(Prev.childCount > 0) {
                        Next = Prev.GetChild(0);
                        D -= 1;
                    } else {
                        Next = null;
                        //U.Trnsfrm.position = Prev.position;
                        U.SyncO.Body.MovePosition(Prev.position);
                        U.Body.MovePosition(U.SyncO.Body.position);

                    }
                }
            }
            if(Next != null) {
                var p = Vector3.Lerp(Prev.position, Next.position, D);
                //U.Trnsfrm.position = p;
                U.SyncO.Body.MovePosition(p);
                U.Body.MovePosition(U.SyncO.Body.position);
                var r = Quaternion.Lerp(Prev.rotation, Next.rotation, D).eulerAngles.z;
                U.SyncO.Body.MoveRotation(r);
                U.Body.MoveRotation(U.SyncO.Body.rotation);
            } else { // our carrier == kaboom-boom  or just done..
                if(U.enabled == false) {
                    Destroy(GetComponent<DistanceJoint2D>());
                    Destroy(U.SyncO.gameObject.GetComponent<DistanceJoint2D>());
                    U.enabled = true;
                 //   return;
                }
                U.SyncO.Trnsfrm.position = Prev.position;
                //if( Temp-- <= 0 )
                Destroy(this);
                U.SyncO.Body.WakeUp();
               // U.SyncO.Body.sleepMode = RigidbodySleepMode2D.StartAwake;
               U.Body.WakeUp();
             //   U.SyncO.Body.velocity = Prev.forward * U.MaxSpeed;

                Car.SyncO.Body.WakeUp();
                return;
            }
        }
    }


}
