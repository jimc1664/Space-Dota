using UnityEngine;
using System.Collections;

public class ALLAHUAKBAR : Weapon {


    float CountDown = 0;  //up.. 

    void Update() {

        if(Trgtn.isServer) {

            if((Time.time - RofTimer > RoF) ){
                if(Trgtn.TargetList.Count > 0) {
                    CountDown += Time.deltaTime;

                    if(CountDown > 2.0f && (Trgtn.TargetList.Values[0].Trnsfrm.position - Trnsfrm.position).sqrMagnitude < 0.75*0.75 ) {
                        // Trgtn.Rpc_setTarget(Trgtn.TargetList[0].gameObject, (byte)MyInd);
                        Trgtn.U.damage(999999999, 99999);
                    }

                } else {
                    CountDown -= Time.deltaTime;
                    if(CountDown < 0) CountDown = 0;
                }


            }
        }
    }

    void OnDisable() {
        if((Time.time - RofTimer > RoF) && Trgtn.U.Health <= 0 ) {
            var t = (Instantiate(FireingAnim) as GameObject).transform;
            t.position = Trgtn.U.Trnsfrm.position;

            if(Trgtn.isServer) {
                Trgtn.TargetList.Clear();
                var cols = Physics2D.OverlapCircleAll(Trgtn.U.Trnsfrm.position, Range, Trgtn.TargetMask);
                foreach(var c in cols) {
                    // Debug.Log("target ?? " + c.name);
                    Unit u;
                    if(c.attachedRigidbody != null) {
                        u = c.attachedRigidbody.GetComponent<Unit>();
                    } else {
                        u = c.GetComponentInParent<Unit>();
                    }
                    if(u == null) continue;
                    if(u == Trgtn.U) continue; //== this

                    if(Trgtn.TargetList.ContainsValue(u)) continue; //inefficent...   todo better list (or better use of this one)
                    float d = (Trgtn.U.Trnsfrm.position - u.Trnsfrm.position).magnitude;
                    //  Debug.Log("target a ?? " + u.name);


                    // Debug.Log("target b ?? " + u.name);
                    Trgtn.TargetList.Add(d, u);

                    var dmg = Dmg * 1.0f - 0.5f * d / Range;
                    u.damage(dmg, AP);

                    //  Debug.Log("target list cnt  " +TargetList.Count + "  "+name);
                }
            }
        }
    }
}
