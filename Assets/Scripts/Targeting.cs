using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Targeting : NetBehaviour {

    [HideInInspector] public Unit U;
    public float Range = 4;
    public float Cycle = 0.5f;
    
    public List<Turret> Turrets;

    [ClientRpc]
    public void Rpc_setTarget(GameObject tgo, byte ti ) {
        if( ti >= (byte)Turrets.Count ) return;
        var trt = Turrets[ti];
        if(tgo != null) {
            trt.Target = tgo.GetComponent<Unit>();
            if(trt.Target != null)
                trt.getSubTarget();
        } else
            trt.Target = null;
    }


    void Awake() {
  // ??      if(!isServer) Destroy(this);
        U =  GetComponent<Unit>();

      
    }

    void OnEnable() {
        for(int i = Turrets.Count; i-- > 0; )
            Turrets[i].MyInd = i;
    }

    float Timer;

    public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return result;  // (-) invert 
        }
    }

    public SortedList<float, Unit> TargetList = new SortedList<float,Unit>( new DuplicateKeyComparer<float> () );
    //public List<Unit> TargetList;

    [ServerCallback]
    void Update() {
        if(U.Owner == null) return;
        if( (Time.time - Timer)  > Cycle) {
            Timer = Time.time;

            TargetList.Clear();
            var cols = Physics2D.OverlapCircleAll(U.Trnsfrm.position, Range, U.Owner.EnemyMask );
            foreach(var c in cols) {
                if( c.attachedRigidbody == null ) continue;
                var u = c.attachedRigidbody.GetComponent<Unit>();
                if( u == null ) continue;
                if(u == U) continue; //== this

                if( TargetList.ContainsValue( u ) ) continue; //inefficent...   todo better list (or better use of this one)
                float d = (U.Trnsfrm.position - u.Trnsfrm.position).sqrMagnitude;
                TargetList.Add(d,u);
            }
        }
    }

    void OnDrawGizmos() {
        var t = transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(t.position, Range);

        if(Timer > Cycle / 2) return;
        foreach(Unit u in TargetList.Values ) {
            if(u == null) continue;
            Gizmos.color = Color.Lerp(Gizmos.color, new Color (0, 0, 0, -1), 1.0f- Timer / (Cycle * 2));
            Gizmos.DrawLine(t.position, u.Trnsfrm.position);
            Gizmos.color = Color.yellow;
        }
    }
}
