using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Targeting : NetBehaviour {

    [HideInInspector] public Unit U;
    public float Range = 4;
    public float Cycle = 0.5f;
    
    public List<Weapon> Weapons;

    [ClientRpc]
    public void Rpc_setTarget(GameObject tgo, byte ti ) {
   //     Debug.Log("_this " + this.GetInstanceID());

        if( ti >= (byte)U.WeaponMasterList.Count ) return;
        var trt = U.WeaponMasterList[ti];
        if(tgo != null) {
            trt.Target = tgo.GetComponent<Unit>();
            if(trt.Target != null)
                trt.getSubTarget();
        } else
            trt.Target = null;
    }

    public float calcEngageRange(Unit target) {
        float ret = float.MaxValue;
        foreach(var t in Weapons) {
            ret = Mathf.Min( ret, t.EngageRange );
        }
        if(ret == float.MaxValue) ret = U.RoughRadius;
        return ret;
    }

    void Awake() {
  // ??      if(!isServer) Destroy(this);
        U =  GetComponent<Unit>();
      
    }

    public bool Friendly = false;

    public LayerMask TargetMask;

    void OnEnable() {
        for(int i = Weapons.Count; i-- > 0;) {
            Weapons[i].MyInd = U.WeaponMasterList.Count;
            U.WeaponMasterList.Add(Weapons[i]);
            Weapons[i].Trgtn = this;
            Weapons[i].enabled = true;
        }

        TargetMask = Friendly ? U.Tm.AllyMask : U.Tm.EnemyMask;
    }

    [HideInInspector] public  float Timer;

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
       // if(U.Owner == null) return;
        if( (Time.time - Timer)  > Cycle) {
            Timer = Time.time;

            TargetList.Clear();
            var uk = U as Unit_Kinematic;
            if(uk != null && !Friendly && uk.Target  && (uk.Target.Trnsfrm.position - U.Trnsfrm.position).sqrMagnitude < Range*Range ) {
                TargetList.Add(0, uk.Target);
                Timer -= Cycle * 0.5f;
                return;
            }
            TargetMask = Friendly ? U.Tm.AllyMask : U.Tm.EnemyMask;
            var cols = Physics2D.OverlapCircleAll(U.Trnsfrm.position, Range, TargetMask );
            foreach(var c in cols) {
               // Debug.Log("target ?? " + c.name);
                Unit u;
                if(c.attachedRigidbody != null) {
                    u = c.attachedRigidbody.GetComponent<Unit>();
                } else {
                    u = c.GetComponentInParent<Unit>();
                }
                if( u == null ) continue;
                if(u == U) continue; //== this

                if( TargetList.ContainsValue( u ) ) continue; //inefficent...   todo better list (or better use of this one)
                float d = (U.Trnsfrm.position - u.Trnsfrm.position).sqrMagnitude;
              //  Debug.Log("target a ?? " + u.name);

                if(Friendly) {
                    if(u.Health >= 1.0f)
                        continue;
                    d *= (1.0f + u.Health *4.0f) ;
                }
               // Debug.Log("target b ?? " + u.name);
                TargetList.Add(d,u);

              //  Debug.Log("target list cnt  " +TargetList.Count + "  "+name);
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
