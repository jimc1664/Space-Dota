using UnityEngine;
using System.Collections;

public class Vehicle : Unit_Kinematic {



    public Mv_Wheeled MvmntController_SO = new Mv_Wheeled(), MvmntController = new Mv_Wheeled();

    public float MaxTurnAccel = 90;
    public float TurnDeAccelMod = 1.5f;
    public float Friction = 0.8f;

    public float SteerStep = 0.7f;
    float SteerTimer = 0;
    const float SteerDelay = 1.5f;
    bool SteerUpdate = false;

   // public bool IsSapper = false;

    void FixedUpdate() {

        updatePath();
        // localAvoidance();

        //SyncO.PathActive = PathActive = true;
        // Mv_Wheeled.update(Trnsfrm, Body, ref PathActive, this);
        //   Debug.Log(" SyncO.PathActive " + SyncO.PathActive);

        

        bool steerCheck = SyncO.PathActive && ((Time.time - SteerTimer) > SteerDelay) && (SteerUpdate || SyncO.Body.velocity.sqrMagnitude < 0.25f) && (MaxSpeed / _MaxSpeed  < 1.5f) ;
        MvmntController_SO.update(SyncO.Trnsfrm, SyncO.Body, steerCheck, this, ref SyncO.SPi, ref SyncO.PathActive, Steering_FB);
        if(steerCheck) {
            SteerUpdate = false;
            SteerTimer = Time.time;
            MvmntController.CMBias = MvmntController_SO.CMBias;
            MvmntController.CRBias = MvmntController_SO.CRBias;
        }
        MvmntController.update(Trnsfrm, Body, false, this, ref SPi, ref PathActive, null);



    }

    GizmoFeedBack Steering_FB = new GizmoFeedBack();

    protected override void steerUpdate() {  //todo -  move path finding up a level - generic - no virtual
        SteerUpdate = true;
        SteerTimer = Mathf.Min(SteerTimer, Time.time - SteerDelay / 3);
    }

    new protected void OnDrawGizmos() {
        base.OnDrawGizmos();

        Steering_FB.draw();
    }
}
