using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class ResourceField : NetBehaviour {


   // [SyncVar] ///lazy
    public float ResCnt = 500;   
    public float MaxRes = 500;

    int Orig;
    Transform Trnsfrm;
	void Start () {
        Trnsfrm = transform;
        Orig = transform.childCount;


        if( ResCnt == MaxRes ) return;
        int oSeed = Random.seed;

        Random.seed = (int)netId.Value;
        refresh();
        Random.seed = oSeed;

        if(!isServer) {
            Destroy(GetComponent<Collider2D>());
        }

	}
    public int Carriers = 0;

    [Server]
    public void grab( ref float r, float fullR, Transform car  ) {
        Debug.Assert(Carriers > 0);
            
        float g =  Mathf.Min( r, fullR / (float)Carriers );
        if(isServer) {
            if(g > ResCnt) {
                g = ResCnt;
                ResCnt = 0;
                refresh(car);
                Destroy(gameObject);
            } else {
                ResCnt -= g;
                refresh(car );
            }
        } else {
            g = Mathf.Min(g, ResCnt);
        }
        r -= g;
    }

    public GameObject ResourceBeam_Fab;

    [ClientRpc]
    void Rpc_harvest( int c,  GameObject car ) { //this is really bad
      //  Debug.Log("Rpc_harvest " + c + "     " + car);
        if(c >= Trnsfrm.childCount) return;
        var go = Trnsfrm.GetChild(c);

        if(car != null) {
            var la = (Instantiate(ResourceBeam_Fab, go.position, Quaternion.identity) as GameObject).GetComponent<LookAt>();
            la.At = car.transform;
            la.Offset = Vector3.forward * 0.3f;
            la.enabled = true;
        }
        Destroy(go.gameObject);
    }
    void refresh(Transform car = null ) {
        int nr = Mathf.CeilToInt( (ResCnt / MaxRes) * (float)Orig );
        int c = Trnsfrm.childCount;
        for(int iter = 100; nr < c; c-- ) {
            if(car != null) {
                Rpc_harvest(Random.Range(0, Trnsfrm.childCount), car.gameObject);
            } else {
                var go = Trnsfrm.GetChild(Random.Range(0, Trnsfrm.childCount));
                Destroy(go.gameObject);
            }

            if(iter-- < 0) {
                Debug.LogError("err  " + nr + "  Trnsfrm.childCount " + Trnsfrm.childCount);
                break;
            }
        }
    }


}
