using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


public class Helio : Unit {

    void OnEnable() {
        base.OnEnable();
        SmoothPath = new List<Vector2>();
        SmoothPath.Add(TargetP = Trnsfrm.position);


        CurHoverDistance = VisDat.transform.position.z;
    }
    static void update(Transform Trnsfrm, Rigidbody2D Body, Helio U, ref int SPi, ref bool PathActive) {

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
            Body.velocity = Vector2.Lerp(Body.velocity, Vec, U.Acceleration * Time.deltaTime);

            var dy = Mathf.Rad2Deg * Mathf.Atan2(-Vec.x, Vec.y);
            Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, dy, 720 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
        } else {
            /*if(U.Target != null) {
                Body.MoveRotation(Mathf.LerpAngle(Ang, Mathf.MoveTowardsAngle(Ang, U.TargetAng, 1080 * Time.deltaTime), U.MaxTurnSpeed * Time.deltaTime));
            } */
            Body.velocity = Vector2.Lerp(Body.velocity, Vector2.zero, 1.5f * U.Acceleration * Time.deltaTime);
        }
    }

    public float DesHoverDistance = 1;
    public float CurHoverDistance = 0;


    float HeightCastTimer = 0;
    float HoverPseudoTimer = 0;
    void Update() {
        base.Update();

        if((Time.time - HeightCastTimer) > 0.2f) {
            HeightCastTimer = Time.time;
            var layerM = (1 << (Player.Team1i + Player.TeamC)) - 1;
            RaycastHit hit;
            

            if( Physics.SphereCast( Trnsfrm.position  +Vector3.forward*10+ (Vector3)(Body.velocity*0.5f),RoughRadius , Vector3.back, out hit, 20.0f, layerM ) ) {
                DesHoverDistance += ( ( hit.point.z+RoughRadius*2 )- DesHoverDistance) * 0.5f;
            } else Debug.Log("miss??");


        }

        
        CurHoverDistance += (DesHoverDistance - CurHoverDistance) * 5 * Time.deltaTime;

        HoverPseudoTimer += Time.deltaTime;
        VisDat.transform.localPosition = new Vector3(0, 0, CurHoverDistance + Mathf.Sin(Mathf.Sin(HoverPseudoTimer * 5.0f) + HoverPseudoTimer) * 0.3f * +Mathf.Sin(HoverPseudoTimer * 10.0f) * 0.1f);
    }

    bool FlyingHigh = false;  //option for helio's to stick to ground and not fly over cliffs - (to hide)  (todo)
    void FixedUpdate() {

        if(FlyingHigh)
            updatePath();
        else if( PathActive ) {
            //if smothpath count > 1 ...etc
            SmoothPath[0] = TargetP; 
        }

        update(SyncO.Trnsfrm, SyncO.Body, this, ref SyncO.SPi, ref SyncO.PathActive);
        update(Trnsfrm, Body, this, ref SPi, ref PathActive);

        fUpdate_ReSync();
    }
}
