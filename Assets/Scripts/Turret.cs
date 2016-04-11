using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Networking;

public class Turret : Weapon {

    public Transform Barrel, MuzzelPoint;

    public float Accuracy = 0.95f, InvTracking = 1;
    public float TurretSpeed = 10;


    Transform SubTarget;
    Vector3 TargetOff;

    public override void getSubTarget() {

        SubTarget = Target.HitTargets[Random.Range(0, Target.HitTargets.Count)];
        TargetOff = Random.insideUnitSphere;
    }

    bool JustFired = false;

	void Update () {
        // if(Trgtn.Friendly)
        //       Debug.Log("target count =  " + Trgtn.TargetList.Count + "   " + Trgtn.name);

        float effRof = RoF * Trgtn.U.Tm.Glob_DamageBoost_Eff_RoF;
        if(Trgtn.isServer && (Time.time - RofTimer ) > effRof / 2 ) {



            Unit nt = null;
            if(Trgtn.TargetList.Count > 0) {
                nt = Trgtn.TargetList.Values[0];
            }
            if(nt != Target) {

                if(nt != null) {
               //     Debug.Log("trgtn " + Trgtn.GetInstanceID());
                    Trgtn.Rpc_setTarget(nt.gameObject, (byte)MyInd);
                    
                } else
                    Trgtn.Rpc_setTarget(null, (byte)MyInd);

            }
            if(JustFired) {
                if(Target != null) getSubTarget();
                JustFired = false;
            }
        }
        Vector3 vec = Vector3.up, tp = Vector3.zero;

        
        if( Target != null ) {

            tp = SubTarget.TransformPoint(TargetOff);

            vec = tp - Trnsfrm.position;
            vec = Trgtn.U.Trnsfrm.InverseTransformDirection(vec);
        }  

        float dz = Mathf.Rad2Deg * Mathf.Atan2(-vec.x, vec.y), rz = Trnsfrm.localRotation.eulerAngles.z;
        rz = Mathf.LerpAngle(rz, Mathf.MoveTowardsAngle(rz, dz, TurretSpeed * 600.0f * Time.deltaTime), 10 * Time.deltaTime);
        Trnsfrm.localRotation = Quaternion.Euler( new Vector3(0, 0, rz ) );


        float dx = Mathf.Rad2Deg * Mathf.Atan2(vec.z,  Mathf.Sqrt((vec.y * vec.y) + (vec.x * vec.x))), rx = Barrel.localRotation.eulerAngles.x;
        rx = Mathf.LerpAngle(rx, Mathf.MoveTowardsAngle(rx, dx, TurretSpeed *0.3f* 600.0f * Time.deltaTime), 10 * Time.deltaTime);
        Barrel.localRotation = Quaternion.Euler(new Vector3(rx, 0, 0));

        if( Target == null ) return;

     //   Debug.Log( "  a  "+ Mathf.DeltaAngle( rz,dz ) + "  b  "+ Mathf.DeltaAngle( rx,dx ) );
        float coneSqr = 25;

        //todo --- targeting improvement to get range to collider it hit
        //todo  vec.sqrMagnitude   -- for range - ---- but then need to pick smart sub target
        if((Time.time - RofTimer) > effRof &&  (Trnsfrm.position - Target.Trnsfrm.position).magnitude - Target.RoughRadius < Range
            && Util.pow2(Mathf.DeltaAngle(rz, dz)) < coneSqr && Util.pow2(Mathf.DeltaAngle(rx, dx)) < coneSqr) {  //todo - neaten?
            
            RofTimer = Time.time;
            var go = Instantiate(FireingAnim);
            if(Dmg < 0) {
                var t = go.transform;
                t.parent = MuzzelPoint;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                // t.localPosition = MuzzelPoint.position;
                // t.localRotation = MuzzelPoint.rotation;
                t.GetComponent<ParticleSystem>().startLifetime *= (Trnsfrm.position - Target.Trnsfrm.position).magnitude / 4.0f;
            } else {
                var cs = go.GetComponent<CannonShell>();
                cs.init(MuzzelPoint.position, tp);
            }
            JustFired = true;

            if(Trgtn.isServer) {
                float acc = Accuracy - Target.Dodge * InvTracking;  //todo - ensure doesn't go below 0... 
                float roll = Random.Range(0.0f,1.0f);
                if( roll < acc )
                    Target.damage(Dmg * Trgtn.U.Tm.Glob_DamageBoost_Eff_Dmg, AP);

             //   Debug.Log("rool  " + roll + "   acc " + acc);
            }
        }
    }


}
