using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


public class Helio : Unit {

    void OnEnable() {
        base.OnEnable();


        CurHoverDistance = VisDat.transform.position.z;
    }
    static void update(Transform Trnsfrm, Rigidbody2D Body, Helio U, ref int SPi, ref bool PathActive, Vector2 drift ) {

        Vector2 Pos = Body.position, dVel = Vector2.zero;
        float Ang = Body.rotation;
        float AngVel = Body.angularVelocity;
        Vector2 Fwd = new Vector2(-Mathf.Sin(Ang * Mathf.Deg2Rad), Mathf.Cos(Ang * Mathf.Deg2Rad));
        Vector2 Vec = U.SmoothPath[SPi] - Pos;
        float Mag = Vec.magnitude;
        
        float acc = U.Acceleration;

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
                    if(Mag < U.RoughRadius * 0.75)
                        PathActive = false;
                    break;
                }
            }

            float rotMod = 0.5f + Mathf.Max( 0, 0.5f*Vector2.Dot(Vec / Mag, Fwd) );
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
            Vec *= rotMod;
            dVel = Vec;

            var dy = Mathf.Rad2Deg * Mathf.Atan2(-Vec.x, Vec.y);
            Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, dy, 720 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
        } else {
            /*if(U.Target != null) {
                Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, U.TargetAng, 1080 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
            } */
            acc *= 1.5f;
        }
        dVel += drift;
        Body.velocity = Vector2.Lerp(Body.velocity, dVel, U.Acceleration * Time.deltaTime);
    }

    public float DesHoverDistance = 1;
    public float CurHoverDistance = 0;
    public float HoverWobble = 1;

    float HeightCastTimer = 0;
    float HoverPseudoTimer = 0;
    void Update() {
        base.Update();

        if((Time.time - HeightCastTimer) > 0.2f) {
            HeightCastTimer = Time.time;
            var layerM = (1 << (Player.Team1i + Player.TeamC)) - 1;
            RaycastHit hit;
            

            if( Physics.SphereCast( Trnsfrm.position  +Vector3.forward*10+ (Vector3)(Body.velocity*0.5f),RoughRadius , Vector3.back, out hit, 20.0f, layerM ) ) {
                DesHoverDistance += ( ( hit.point.z+RoughRadius*0.5f +0.2f )- DesHoverDistance) * 0.5f;
            } else Debug.Log("miss??");

        }

        float hs = 5;
        if(CurHoverDistance > DesHoverDistance) hs *= 0.3f;

        CurHoverDistance += (DesHoverDistance - CurHoverDistance) * hs * Time.deltaTime;

        HoverPseudoTimer += Time.deltaTime;
        VisDat.transform.localPosition = new Vector3(0, 0, CurHoverDistance + wobble() );
    }
    float wobble() {
        return (Mathf.Sin(Mathf.Sin(HoverPseudoTimer * 5.0f) + HoverPseudoTimer) * 0.3f * +Mathf.Sin(HoverPseudoTimer * 10.0f) * 0.1f) * HoverWobble;
    }

    override protected bool desPos(Vector2 dp) {
        if(base.desPos(dp)) return true;
        if(FlyingHigh) {
            TargetNode = null;
            TargetP = dp;

            PathActive = true;
            SyncO.PathActive = true;
            return true;
        }
        return false;
    }

    bool FlyingHigh = true;  //option for helio's to stick to ground and not fly over cliffs - (to hide)  (todo)
    void FixedUpdate() {

        if(!FlyingHigh)
            updatePath();
        else {

            CurNode = NavMsh.findNode_Closest(SyncO.Body.position, CurNode );
 
            checkTarget();

            if(PathActive) {
                //if smothpath count > 1 ...etc

                SmoothPath[0] = TargetP;
            }
        }

        Vector2 drift = Random.onUnitSphere * wobble() *25.0f;
        update(SyncO.Trnsfrm, SyncO.Body, this, ref SyncO.SPi, ref SyncO.PathActive, drift );
        update(Trnsfrm, Body, this, ref SPi, ref PathActive, drift);

        fUpdate_ReSync();
    }
}
