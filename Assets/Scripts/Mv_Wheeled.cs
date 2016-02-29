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
                float rotSpeed = (1.0f - Mathf.Abs(0.5f - Mathf.Pow(speed / u.MaxSpeed, 2))) * u.MaxTurnSpeed;
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

    struct NewtonRaphson_Helper {

        float Drag, MaxSpeed, Accel, V0, P0;

        public NewtonRaphson_Helper(float maxSpeed, float maxAccel, float accel, float v0, float p0) {
            Drag = maxAccel / maxSpeed;
            MaxSpeed = maxSpeed;
            V0 = v0;
            P0 = p0;
            Accel = accel;
            K2 = (-Drag * v0 + accel) / (Drag * Drag);
            K1 = p0 - K2;
        }
        public float solve(int iter = 4) {
            float t = P0 / MaxSpeed;
            for(; iter-- > 0; ) {  //note - no early exit -... todo make compiler unroll...
                float pt = K1 + K2 / Mathf.Exp(Drag * t) + Accel * t / Drag;
                float vt = K2 * Mathf.Exp(-Drag * t) * (-Drag) + Accel / Drag;

                t = t - pt / vt;// / dvt;
            }
            return Mathf.Abs(t);
        }
        public float vel(float t) {
            return K2 * Mathf.Exp(-Drag * t) * (-Drag) + Accel / Drag;
        }
        public float pos(float t) {
            return K1 + K2 / Mathf.Exp(Drag * t) + Accel * t / Drag;
        }

        float K1, K2;
    };
   struct NewtonRaphson_Helper2 {

        float Drag, MaxSpeed, Accel, V0;

        public NewtonRaphson_Helper2(float maxSpeed, float maxAccel, float accel, float v0, float p0) {
            Drag = maxAccel / maxSpeed;
            MaxSpeed = maxSpeed;
            V0 = v0;
            Accel = accel;
            K2 = (-Drag * v0 + accel) / (Drag * Drag);
        }
       /* public float solve(int iter = 4) {
            float t = DesSpeed / Accel;
            for(; iter-- > 0; ) {  //note - no early exit -... todo make compiler unroll...
                float vt = K2 * Mathf.Exp(-Drag * t) * (-Drag) + Accel / Drag;
                float at = -vt * Drag - Accel;
                t = t - (vt-DesSpeed) /at;// / dvt;
            }
            return Mathf.Abs(t);
        } */
        public float vel(float t) {
            return K2 * Mathf.Exp(-Drag * t) * (-Drag) + Accel / Drag;
        }

        float K2;
    };

    static bool active = false;


    struct State {

        public float Ang, AngVel, Mag;
        public Vector2 Pos, Vel, Vec, Fwd;
        Unit U;
        public float FullTurn;
        public State( Unit u, Vector2 p, Vector2 vel, float a, float av ) {
            Pos = p; Vel = vel;
            Ang = a;
            AngVel = av;
            U = u;
            FullTurn = 0;
            Fwd = new Vector2(-Mathf.Sin(Ang * Mathf.Deg2Rad), Mathf.Cos(Ang * Mathf.Deg2Rad));

            Vec = U.SmoothPath[U.SPi] - Pos;
            Mag = Vec.magnitude;
        }

        public void step( float t, ref float movBias, ref float rotBias, ref int spi ) {

            var oAng = Ang;
            Vector2 oFwd = Fwd;
            float desSpeed = 0, dt, oSpeed = Vector2.Dot(Vel, oFwd);

            //var mAng = Mathf.LerpAngle(Ang, oAng, 0.5f);
            // Vector2 mFwd = new Vector2(-Mathf.Sin(mAng * Mathf.Deg2Rad), Mathf.Cos(mAng * Mathf.Deg2Rad)) 


            for(;;) {

                dt = Vector3.Dot(oFwd, Vec);
                desSpeed = Mathf.Clamp(dt, -U.MaxSpeed / 2, U.MaxSpeed); ;//

                if(dt != desSpeed ) break;  //we clamped.. so full speed.. 
                
                if( Mag < U.RoughRadius *0.75 ) {
                    if( spi < U.SmoothPath.Count-1 ) {
                        spi++;
                        Vec = U.SmoothPath[spi] - Pos;
                        Mag = Vec.magnitude;
                    }  else {
                        //pathActive = false;
                        break;
                    }
                } else {
                    if( spi >= U.SmoothPath.Count-1 ) break;
                    var v2 = U.SmoothPath[spi + 1] - U.SmoothPath[spi];
                    var vn = Vec / Mag;
                    float dt2 = Vector2.Dot(v2, vn);
                    if(dt2 > 0) {

                        
                        Mag += dt2;
                        Vec = vn * Mag;
                    } else break;
                }
                
            }

            if(movBias == -1 &&  dt/Mag > 0.6f ) movBias = 0;

            if( movBias == 1 || desSpeed > -U.MaxSpeed / 6 && movBias != -1 ) {
                if(desSpeed < U.MaxSpeed / 2)
                    desSpeed = Mathf.Max(Mathf.Min(Mag, U.MaxSpeed / 2), desSpeed);   //todo - add magic no. mul to mag (tweak)
            } else {
                if(desSpeed < -U.MaxSpeed / 6 - oSpeed  || movBias == -1 ) {
                    if(desSpeed > -U.MaxSpeed / 3)
                        desSpeed = Mathf.Min(-Mathf.Min(Mag, U.MaxSpeed / 3), desSpeed);
                } else
                    desSpeed = Mathf.Max(Mathf.Min(Mag, U.MaxSpeed / 2), -desSpeed);
            }
  

            float nSpeed, acc = U.Acceleration;
            if(desSpeed > oSpeed) {
                if(Mathf.Abs(desSpeed) < Mathf.Abs(oSpeed)) acc *= 1.5f;
                var posEq = new NewtonRaphson_Helper2(U.MaxSpeed, U.Acceleration, acc, oSpeed, 0);
                nSpeed = posEq.vel(t);
                if(nSpeed > desSpeed) nSpeed = desSpeed;
            } else {
                if(Mathf.Abs(desSpeed) < Mathf.Abs(oSpeed)) acc *= 1.5f;
                else acc *= 0.5f;

                var posEq = new NewtonRaphson_Helper2(U.MaxSpeed, U.Acceleration, -acc, oSpeed, 0);
                nSpeed = posEq.vel(t);
                if(nSpeed < desSpeed) nSpeed = desSpeed;
            }

            var dy = Mathf.Rad2Deg * Mathf.Atan2(-Vec.x, Vec.y);

            float rotMod = (1.0f - Mathf.Abs(0.5f - Mathf.Pow( (oSpeed+nSpeed)*0.5f / U.MaxSpeed, 2)));// *u.TurnSpeed;
           // rotMod = 1;
            float rotMaxSpeed = rotMod * U.MaxTurnSpeed;
            float rotMaxAccel = rotMod * U.MaxTurnAccel;
            float rotMaxDecel = rotMaxAccel * U.TurnDeAccelMod;

            float angDiff = Mathf.DeltaAngle(Ang, dy);

            FullTurn = 0;
            if( Mathf.Abs(angDiff + AngVel * t) < rotMaxAccel * 0.5f *t *0.25f ) {
                AngVel = 0;
                Ang = dy;
            } else {
                float rotDecel = rotMaxDecel;
                if(AngVel < 0) rotDecel = -rotDecel;
                float timeToStopRot = AngVel / rotDecel;

                //float drag = rotMaxAccel / rotMaxSpeed;

                bool rbFlag = false;
                float accAdd = rotMaxAccel;
                if(angDiff < 0) accAdd = -accAdd;
                if(rotBias != 0) {
                    if(rbFlag = (accAdd > 0 != rotBias > 0) )
                        accAdd = -accAdd;
                    else rotBias = 0;
                }
                var rotEq = new NewtonRaphson_Helper(rotMaxSpeed, rotMaxAccel, accAdd, AngVel, -angDiff);
                float timeToDesRot = rotEq.solve();

                if(rotBias != 0) {
                    if(rbFlag) timeToDesRot = Mathf.Max(timeToDesRot, t);
                   
                    //var est = Mathf.Abs(-angDiff  / rotMaxSpeed);
                   // if( float.IsNaN( timeToDesRot) ||  timeToDesRot / est > 5 )
                    //    timeToDesRot = est;
                 //   Debug.Log("timeToStopRot  " + timeToStopRot + "   timeToDesRot  " + timeToDesRot + "   angDiff  " + angDiff
                 //       + "   AngVel  " + AngVel + "   nav  " + rotEq.vel(t) + " ||| " + rotBias + "  f " + rbFlag);
                }
               
                var oAv = AngVel;
                if(timeToDesRot > timeToStopRot) {
                    var rs = t;

                    if(timeToDesRot < t) {  //hacky but not as bad as it looks
                        AngVel = 0;
                        Ang = dy;
                    } else {
                        if( rbFlag )  FullTurn = Mathf.Sign(accAdd);
                        AngVel = rotEq.vel(t);
                        Ang += (AngVel + oAv) * 0.5f * t;
                    }

                } else {
                    if(timeToStopRot > t) {
                        AngVel -= rotDecel * t;
                        Ang += (AngVel + oAv) * 0.5f * t;
                    } else {
                        AngVel = 0;
                        Ang = dy;
                    }
                }
            }
            
            Fwd = new Vector2(-Mathf.Sin(Ang * Mathf.Deg2Rad), Mathf.Cos(Ang * Mathf.Deg2Rad));//, mFwd = (oFwd + (nFwd-oFwd)*0.5f).normalized;
            // float acc = u.Acceleration;
           
            //var tVel = Vel;
           // tVel -= fwd * Vector2.Dot(Vel, ofwd);
            var oVel = Vel;
            Vel = Fwd * nSpeed;
            Pos += (Vel + oVel) * 0.5f * t;

            Vec = U.SmoothPath[spi] - Pos;
            Mag = Vec.magnitude;
        }
    
    };
    static float CMBias = 0, CRBias = 0;
    static List<Vector2> SubPath = new List<Vector2>();



    public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return result;  // (-) invert 
        }
    }
    class SteerNode {
        public State St;
        public SteerNode Prev;
        public int Spi, Iter;

        public float CurMBias, CurRBias;
        public float MBias, RBias;
    };
    public static void update(Transform Trnsfrm, Rigidbody2D Body, ref bool PathActive, Unit u, GizmoFeedBack gizmo) {

        //if(!PathActive) return;
        if(PathActive) {
            active = true;
        }
        if(!active) return;
        State st1 = new State( u, Trnsfrm.position, Body.velocity, Body.rotation, Body.angularVelocity );


        if(PathActive) {
            gizmo.reset();
            //gizmo.line(st1.Pos, u.SmoothPath[u.SmoothPath.Count-1], Color.white);

            Color[] cols = { Color.red, Color.blue, Color.green, Color.yellow };
            float[,] bias = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
            int dirs = 4;

           // SubPath = new List<Vector2>();
/*            float  dt = Vector2.Dot(st1.Vec, st1.Fwd) / st1.Mag;
            if(dt > -0.05f) {
                dirs = 2;
                bias[0, 0] = bias[1, 0] = 0;
            } */
            CMBias = CRBias = 0;
            int bestI = -1; float bestD = -1; 

            int mapLm = 1 << LayerMask.NameToLayer("Map");

            for(int i = dirs; i-- >0; ) {
       
                State st = st1;
                Vector2 lp = st.Pos;
                int spi = u.SPi;
                float mBias = bias[i, 0], rBias = bias[i, 1];
                float tMb = mBias, tRb = rBias;
                for(int maxIter = 30, iter =maxIter ; ; ) {
                    var col = cols[i];
                    //  float step = 1.0f;
                    st.step(0.7f, ref mBias, ref rBias, ref spi );
                    gizmo.line(st.Pos, lp, col );

                    col.a = 0.3f;
                    gizmo.sphere(st.Pos, u.RoughRadius, col);

                    col = col * 0.25f;
                    col.a = 1;
                    if(st.FullTurn != 0) {

                        gizmo.line(st.Pos, st.Pos + (Vector2)Vector3.Cross((Vector3)st.Fwd, Vector3.back).normalized * st.FullTurn, col);
                    } else {

                        if(maxIter - iter < 2) {
                            tRb = 0;
                            rBias = 0;
                        }
                        iter = Mathf.Min(iter, 3);
                    }

                    var hit = Physics2D.OverlapCircle(st.Pos, u.RoughRadius, mapLm );
                    if(hit == null) {

                    } else {
                        gizmo.sphere(st.Pos, u.RoughRadius, col );
                       /* var cc = hit as CircleCollider2D;
                        if(cc != null) {

                            var tan = (Vector2)Vector3.Cross((Vector3)st.Fwd/st.Mag, Vector3.back).normalized;
                            Vector2 cp = cc.transform.position;
                            var cr = cc.radius;
                            tan *= (cr + u.RoughRadius)*1.1f;
                            gizmo.sphere(cp + tan, u.RoughRadius, Color.green);
                            gizmo.sphere(cp - tan, u.RoughRadius, Color.green);
                        } */
                        break;
                    }


                    if(  spi >= u.SmoothPath.Count-1 && st.Mag < 2.5f && Vector2.Dot(st.Vec, st.Fwd) / st.Mag > 0.8f) {
                        if(iter > bestI || ( iter == bestI && st.Mag < bestD ) ) {
                            bestI = iter;
                            CMBias = tMb;
                            CRBias = tRb;
                            bestD = st.Mag;
                        }
                        break;
                    }
                  
                    if((iter--) < 0) break;
   
                    lp = st.Pos;

                }               
            }

   
        }
        Debug.DrawLine(Trnsfrm.position, u.SmoothPath[u.SPi], Color.white );
        st1.step(Time.deltaTime, ref CMBias, ref CRBias, ref u.SPi );

        Body.velocity = st1.Vel;
        Body.angularVelocity = st1.AngVel;

        PathActive = false;
    }

    static void drawNode(Vector2 p, Vector2 d, Color c, GizmoFeedBack gizmo, float r = 1 ) {

        gizmo.sphere(p,r, c );

        gizmo.line(p, p + d * 1.5f, Color.black);
    }
 /*   public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
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

    public static void update2(Transform Trnsfrm, Rigidbody2D Body, ref bool PathActive, Unit u, GizmoFeedBack gizmo) {
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
                        * /
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
    } */
}
