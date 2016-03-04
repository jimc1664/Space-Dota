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

    public void grab( ref float r, float fullR ) {
        Debug.Assert(Carriers > 0);
            
        float g =  Mathf.Min( r, fullR / (float)Carriers );
        if(isServer) {
            if(g > ResCnt) {
                g = ResCnt;
                Destroy(this);
            } else {
                ResCnt -= g;
                refresh();
            }
        } else {
            g = Mathf.Min(g, ResCnt);
        }
        r -= g;
    }

    void refresh() {
        int nr = Mathf.CeilToInt( (ResCnt / MaxRes) * (float)Orig );
        int c = Trnsfrm.childCount;
        for(int iter = 100; nr < c; c-- ) {
            Destroy(Trnsfrm.GetChild(Random.Range(0, Trnsfrm.childCount)).gameObject);
            if(iter-- < 0) {
                Debug.LogError("err  " + nr + "  Trnsfrm.childCount " + Trnsfrm.childCount);
                break;
            }
        }
    }


}
