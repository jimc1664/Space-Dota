using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class ResourceField : NetBehaviour {


    [SyncVar] ///lazy
    public float ResCnt = 500;   
    public float MaxRes = 500;

    int Orig;
    Transform Trnsfrm;
	void Start () {
        Trnsfrm = transform;
        Orig = transform.childCount;
        refresh();
	}
    public int Carriers = 0;

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

    void refresh(Transform car = null ) {
        int nr = Mathf.CeilToInt( (ResCnt / MaxRes) * (float)Orig );
        int c = Trnsfrm.childCount;
        for(int iter = 100; nr < c; c-- ) {
            var go = Trnsfrm.GetChild(Random.Range(0, Trnsfrm.childCount));
            if(car != null) {
                var la = (Instantiate(ResourceBeam_Fab, go.position, Quaternion.identity ) as GameObject ).GetComponent<LookAt>();
                la.At = car;
                la.Offset = Vector3.forward * 0.3f;
                la.enabled = true;
            }
            Destroy( go.gameObject );

            if(iter-- < 0) {
                Debug.LogError("err  " + nr + "  Trnsfrm.childCount " + Trnsfrm.childCount);
                break;
            }
        }
    }


}
