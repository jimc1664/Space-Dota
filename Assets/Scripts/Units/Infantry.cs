using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Infantry : Unit {
    //TODO --- copied and pasted lots from turret!!  bad !! fix!!

    Animator Anim;

   // Targeting Trgtn;
    public Transform MuzzelPoint;
    public GameObject FireingAnim;


    public Unit FireTarget;
    [HideInInspector] public float TargetAng;
    bool JustFired = false;

    public float Range = 3, RoF = 1, Dmg = 500, AP = 0, Accuracy = 0.95f, InvTracking = 1;
    float RofTimer = -5;

    Transform SubTarget;
    Vector3 TargetOff;

    public void getSubTarget() {

        SubTarget = FireTarget.HitTargets[Random.Range(0, FireTarget.HitTargets.Count)];
        TargetOff = Random.insideUnitSphere;
    }
    void Awake() {
        base.Awake();
        Anim = VisDat.GetComponentInChildren<Animator>();
        
        Trgtn.enabled = false;
        
    }
    static void update(Transform Trnsfrm, Rigidbody2D Body, Infantry U, ref int SPi, ref bool PathActive) {

        Vector2 Pos = Body.position, Vel = Body.velocity;
        float Ang = Body.rotation;
        float AngVel = Body.angularVelocity;
        Vector2 Fwd = new Vector2(-Mathf.Sin(Ang * Mathf.Deg2Rad), Mathf.Cos(Ang * Mathf.Deg2Rad));
        Vector2 Vec = U.SmoothPath[SPi] - Pos;
        float Mag = Vec.magnitude;
        
        if(PathActive) {
            for(; ; ) {
                if(SPi < U.SmoothPath.Count - 1) {
                    var v2 = U.SmoothPath[SPi + 1] - Pos;
                    if(Vector2.Dot(Vec, v2) < -0.1f) {
                        SPi++;
                        Vec = v2;
                    } else {
                        Mag = Vec.magnitude;
                        break;
                    }
                } else {
                    Mag = Vec.magnitude;
                    if(Mag < U.RoughRadius *0.75 )
                        PathActive = false;
                    break;
                }
            }

            Mag *= 2; Vec *= 2;

            for(; ; ) {
                if(Mag > U.MaxSpeed) {

                    Vec = Vec * U.MaxSpeed / Mag;
                    break;
                }
                if(SPi >= U.SmoothPath.Count - 1) break;
                var v2 = U.SmoothPath[SPi + 1] - U.SmoothPath[SPi];
                var vn = Vec / Mag;
                float dt2 = Vector2.Dot(v2, vn);
                if(dt2 > 0) {
                    Mag += dt2;
                    Vec = vn * Mag;
                } else break;
            }
            Body.velocity = Vector2.Lerp(Body.velocity, Vec, U.Acceleration * Time.deltaTime);

            var dy = Mathf.Rad2Deg * Mathf.Atan2(-Vec.x, Vec.y);
            Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, dy, 1080 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
        } else {
            if(U.FireTarget != null) {
                Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, U.TargetAng, 1080 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
            }
            Body.velocity = Vector2.Lerp(Body.velocity, Vector2.zero, 1.5f *U.Acceleration * Time.deltaTime);
        }         
    }


    override protected float calcEngageRange(Unit target) {        
        return Range;
    }

    float AnimRandTimer = 1;


    [ClientRpc]
    public void Rpc_setTarget(GameObject tgo) {
        if(tgo != null) {
            FireTarget = tgo.GetComponent<Unit>();
            if(FireTarget != null)
               getSubTarget();
        } else
            FireTarget = null;
    }

    float FiredTimer = 0;
    void Update() {
        base.Update();

        float spd =  Body.velocity.magnitude / MaxSpeed;

        if(Trgtn.enabled) {
            if(isServer && (Time.time - RofTimer) > RoF / 2) {
                Unit nt = null;
                if(Trgtn.TargetList.Count > 0) {
                    nt = Trgtn.TargetList.Values[0];
                }
                if(nt != FireTarget) {
                    if(nt != null) {
                        Rpc_setTarget(nt.gameObject);

                    } else
                        Rpc_setTarget(null );
                }
                if(JustFired) {
                    if(FireTarget != null) getSubTarget();
                    JustFired = false;
                }
            }

        
            if( FireTarget != null ) {

                var tp = SubTarget.TransformPoint(TargetOff);

                Vector2 vec = tp - Trnsfrm.position;
                //vec = Trgtn.U.Trnsfrm.InverseTransformDirection(vec);

                TargetAng = Mathf.Rad2Deg * Mathf.Atan2(-vec.x, vec.y);
                if((Time.time - RofTimer) > RoF && (Trnsfrm.position - FireTarget.Trnsfrm.position).magnitude - FireTarget.RoughRadius < Range
                    && Vector2.Dot( Trnsfrm.up,  vec.normalized ) > 0.9f  ) {  //todo - neaten?

                    RofTimer = Time.time;
                    Instantiate(FireingAnim).GetComponent<CannonShell>().init(MuzzelPoint.position, tp );
                    JustFired = true;
                    FiredTimer = 0.2f + Time.deltaTime;
                    if(isServer) {
                        float acc = Accuracy - FireTarget.Dodge * InvTracking;  //todo - ensure doesn't go below 0... 
                        float roll = Random.Range(0.0f, 1.0f);
                        if(roll < acc)
                            FireTarget.damage(Dmg, AP);
                    }
                }  
            
            }
            if(spd > 0.2f) Trgtn.enabled = false;
        } else {
            if(spd < 0.1f || !PathActive) Trgtn.enabled = true;
        }

        if( FiredTimer > 0 ) {
            FiredTimer -= Time.deltaTime;
            Anim.SetFloat("FireEverything", FiredTimer );
        }

        //todo - pre cache controller id's 
        Anim.SetFloat("Speed",spd );
        if((AnimRandTimer -= Time.deltaTime) < 0) {
            Anim.SetFloat("Rando", Random.value);
        }

    }

    void FixedUpdate() {
        updatePath();

        update(SyncO.Trnsfrm, SyncO.Body, this, ref SyncO.SPi, ref SyncO.PathActive );
        update(Trnsfrm, Body, this, ref SPi, ref PathActive );


        fUpdate_ReSync();
    }
}
