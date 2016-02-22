using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mv_Wheeled  {

    public static void update( Transform Trnsfrm, Rigidbody2D Body, ref bool PathActive, Unit u ) {

        Vector2 fwd = Trnsfrm.up;
        float desSpeed = 0, speed = Vector2.Dot( Body.velocity, fwd);

        if(PathActive) {
            var vec = u.DesPos - (Vector2)Trnsfrm.position;
            var mag = vec.magnitude;
            vec *= 1000.0f;
           // if(mag > 0.01) {
                var ry = Trnsfrm.eulerAngles.z;
                var dy = Mathf.Rad2Deg * Mathf.Atan2(-vec.x, vec.y);
                //Debug.Log(" ang = " + ry + "  des " + dy);
                float rotSpeed = (1.0f - Mathf.Abs(0.5f - Mathf.Pow(speed / u.MaxSpeed, 2))) * u.TurnSpeed;
                ry = Mathf.LerpAngle(ry, Mathf.MoveTowardsAngle(ry, dy, 600 * Time.deltaTime), rotSpeed * Time.deltaTime);
                ///Trnsfrm.RotateAround(  
                //Trnsfrm.eulerAngles = new Vector3(0, 0, ry);
                Body.MoveRotation(ry);
                fwd = Trnsfrm.up;
                desSpeed = Mathf.Clamp(Vector3.Dot(fwd, vec), -u.MaxSpeed / 2, u.MaxSpeed);

                if(desSpeed > -u.MaxSpeed / 6) {
                    if(desSpeed < u.MaxSpeed / 2)
                        desSpeed = Mathf.Max(Mathf.Min(mag, u.MaxSpeed / 2), desSpeed);
                } else {

                    if(desSpeed < -u.MaxSpeed / 6 - speed) {
                        if(desSpeed > -u.MaxSpeed / 3)
                            desSpeed = Mathf.Min(-Mathf.Min(mag, u.MaxSpeed / 3), desSpeed);
                    } else
                        desSpeed = Mathf.Max(Mathf.Min(mag, u.MaxSpeed / 2), -desSpeed);
                }
                //Debug.Log("ms  " + desSpeed  +   "  --- " + ( (-MaxSpeed / 6 - speed) ) );
                //  if(desSpeed > -MaxSpeed / 4 && desSpeed < 0 && speed > desSpeed / 4)
                //      desSpeed = -desSpeed;
          //  } else PathActive = false;
        }
        float acc = u.Acceleration;
        if(Mathf.Abs(desSpeed) < Mathf.Abs(speed)) acc *= 3;
        speed = Mathf.Lerp(speed, desSpeed, acc * Time.deltaTime);
        //        var pos = Trnsfrm.position; pos.y = 0; Trnsfrm.position = pos;
        Body.velocity = Vector2.Lerp(Body.velocity, fwd * speed, u.Friction * Time.deltaTime);
    }

    static void drawNode(Vector2 p, Vector2 d, Color c, GizmoFeedBack gizmo, float r = 1 ) {

        gizmo.sphere(p,r, c );

        gizmo.line(p, p + d * 1.5f, Color.black);
    }
    public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return result;  // (-) invert 
        }
    }
    class LANode {
        public Vector2 Pos, Dir;
        public float Speed, Ang, AngVel;
        public int Depth;
        public LANode Prev;
    };

    static void step(ref Vector2 p0, ref float a0, ref float av0, float s0, float s1, float avAdd, float step ) {
        //float s = spd[si];
        //float avAdd = angV[ai];

        var effSpd = (s0 + s1) * 0.5f;

        var av1 = av0 + avAdd;

        var a1 = (a0 + (av0 + av1) * 0.5f * step);
        var ta = (a1 + a0) * 0.5f;
        Vector2 td = new Vector2(-Mathf.Sin(ta * Mathf.Deg2Rad), Mathf.Cos(ta * Mathf.Deg2Rad));
        //Vector2 d = new Vector2(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad));

       // Vector2 p = n1.Pos + td * effSpd * step;

        p0 += td * effSpd * step;
        av0 = av1;
        a0 = a1;
    }

    static LANode Cur;
    static float Timer = 0;
    public static void update(Transform Trnsfrm, Rigidbody2D Body, ref bool PathActive, Unit u, GizmoFeedBack gizmo) {
     //   Time.timeScale = 0.1f;
        if(!PathActive) return;
        //Vector2 fwd = Trnsfrm.up;
        //float desSpeed = 0, speed = Vector2.Dot(Body.velocity, fwd);

        float avAcc = 25;
        if((Timer -= Time.deltaTime) < -0.5f) u.RefreshLA = true;
        if(u.RefreshLA) {
            Timer = 0;
            gizmo.reset();
            var n1 = new LANode();  var first = n1;
            n1.Ang = Body.rotation;
            n1.Dir = new Vector2(-Mathf.Sin(n1.Ang * Mathf.Deg2Rad), Mathf.Cos(n1.Ang * Mathf.Deg2Rad));
            n1.Pos = Body.position;
            n1.Speed = Vector2.Dot(Body.velocity, n1.Dir);
            n1.AngVel = Body.angularVelocity;
            n1.Depth = 0;

            float[] spd = new float[3];
            float[] angV = new float[3];
            float tStep = 1.0f;

            float r = 1.1f;
            Color col = Color.red;
            SortedList<float, LANode> search = new SortedList<float, LANode>(new DuplicateKeyComparer<float>());


            float bestD = float.MaxValue;
            LANode best = null;

            for(int maxIter = 1000; maxIter-- > 0; ) {

                spd[0] = n1.Speed;
                float acc = u.Acceleration * tStep;
                float dec = acc * 3;
                if(Mathf.Abs(n1.Speed) > Mathf.Abs(n1.Speed + dec))
                    spd[1] = n1.Speed + dec;
                else
                    spd[1] = n1.Speed + acc;
                if(Mathf.Abs(n1.Speed) > Mathf.Abs(n1.Speed - dec))
                    spd[2] = n1.Speed - dec;
                else
                    spd[2] = n1.Speed - acc;


                angV[0] = 0;
                angV[1] = +avAcc *tStep;
                angV[2] = -avAcc *tStep;

                //  Debug.Log("i " + i + "  step " + step + "  spd[1] " + spd[1] + "  angV[1] " + angV[1]);

                for(int si = 3; si-- > 0; )
                    for(int ai = 3; ai-- > 0; ) {
                        // int si = 1; ai = 1;
                        float s = spd[si];
                        float avAdd = angV[ai];
                        /*
                        var effSpd = (n1.Speed + s) * 0.5f;
                        var av = n1.AngVel + avAdd;

                        var a = (n1.Ang +(n1.AngVel+ av)*0.5f*step  ); 
                        var ta = (a + n1.Ang) * 0.5f;
                        Vector2 td = new Vector2(-Mathf.Sin(ta* Mathf.Deg2Rad), Mathf.Cos(ta* Mathf.Deg2Rad));
                        Vector2 d = new Vector2(-Mathf.Sin(a* Mathf.Deg2Rad), Mathf.Cos(a* Mathf.Deg2Rad));

                        Vector2 p = n1.Pos + td   *effSpd*step;
                        */
                        Vector2 p = n1.Pos;
                        float a = n1.Ang, av = n1.AngVel;

                        //ef Vector2 p0, ref float a0, ref float av0, ref float s0, float s1, float avAdd, float step ) {
                        step(ref p, ref a, ref av, n1.Speed, s, avAdd, tStep);

                        Vector2 d = new Vector2(-Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad));

                        drawNode(p, d, col, gizmo, 0.18f);
                        var vec = u.TargetP - p;

                        float dis = Mathf.Abs((u.TargetP - p).magnitude - Vector2.Dot(vec.normalized, d) * s)   + Mathf.Abs(av*0.025f);


                        var nn = new LANode();
                        nn.Dir = d;
                        nn.Pos = p;
                        nn.Speed = s;
                        nn.Ang = a;
                        nn.AngVel = av;
                        nn.Depth = n1.Depth + 1;
                        nn.Prev = n1;
                       

                        if(dis < bestD) {
                            best = nn;
                            bestD = dis;

                            if(dis < 1.0f) goto label_doubleBreak;
                        }
                        if(n1.Depth < 3) {
                            dis += 0.1f * nn.Depth;
                            search.Add(dis, nn);
                        }
                    }

                


                if(search.Count == 0) break;
                if(search.Keys[0] > (bestD + 1) * 1.1f) break;
                n1 = search.Values[0];
                search.RemoveAt(0);
            }
            label_doubleBreak:;
            for(; ;) {
                
                drawNode(best.Pos, best.Dir, Color.green, gizmo, 0.2f);
                if(best.Prev == first) {
                    Cur = best;
                    break;
                }
                best = best.Prev;
            }
            u.RefreshLA = false;
        }
     //   return;
        var pos = Body.position;
        var ang = Body.rotation;
        var dir = new Vector2(-Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad));
        var spd0 = Vector2.Dot(Body.velocity, dir);
        var angVel = Body.angularVelocity;


        float accel = u.Acceleration * Time.deltaTime;

        if( Mathf.Abs( Cur.Speed ) < Mathf.Abs(spd0 ) ) accel *= 3;
        float spd1 = Mathf.Clamp(Cur.Speed, spd0 - accel, spd0 + accel);

        float angVelAdd = Mathf.Clamp(Cur.AngVel - angVel,  - avAcc * Time.deltaTime,  avAcc * Time.deltaTime);

        step(ref pos, ref ang, ref angVel, spd0, spd1, angVelAdd, Time.deltaTime);
      //  Debug.Log("spd0  " + spd0 + "   spd1  " + spd1);
        Body.angularVelocity = angVel;
        //Body.angularVelocity = 10;
        //Body.MoveRotation(ang);        
        Body.velocity = dir * spd1;
    }
}
