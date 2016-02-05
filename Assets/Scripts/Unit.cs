using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Unit : NetworkBehaviour {

    [SyncVar(hook = "Hook_DesPos")]
    public Vector2 DesPos;

    void Hook_DesPos( Vector2 dp ) {
        DesPos = dp;
        PathActive = true;
    }

    [HideInInspector]
    public bool PathActive = false;

    public Transform Trnsfrm;
    public float MaxSpeed = 5;

    void Awake() {
        Trnsfrm = transform;
        DesPos = transform.position;
    }
    void Start() {
        DesPos = transform.position;
    }

    public void fixCol( Color c ) {
        if(isServer) Rpc_fixCol(c);
        foreach(var mr in GetComponentsInChildren<MeshRenderer>()) {
            foreach(Material mat in mr.materials) {
               // Debug.Log("fix?");
                if(mat.name == Sys.get().BaseColMat.name + " (Instance)" ) {
                  //  Debug.Log("fix!!!!!????");

                    mat.color = c;
                }
            }
        }
    }

    [ClientRpc]
    void Rpc_fixCol( Color c ) { fixCol(c); }
   // [ServerCallback]
    void FixedUpdate() {

        Vector2 fwd = Trnsfrm.up;
        float desSpeed = 0, speed = Vector2.Dot( GetComponent<Rigidbody2D>().velocity, fwd );

        if(PathActive) {
            var vec = DesPos - (Vector2)Trnsfrm.position;
            var mag = vec.magnitude;

            if(mag > 0.1) {
                var ry = Trnsfrm.eulerAngles.z;
                var dy = Mathf.Rad2Deg * Mathf.Atan2(-vec.x, vec.y);
                //Debug.Log(" ang = " + ry + "  des " + dy);
                float rotSpeed = (  1.0f- Mathf.Abs( 0.5f - Mathf.Pow(speed / MaxSpeed, 2)  ) )  * 10;  
                ry = Mathf.LerpAngle(ry, Mathf.MoveTowardsAngle(ry, dy, 600 * Time.deltaTime), rotSpeed * Time.deltaTime);
                ///Trnsfrm.RotateAround(  
                Trnsfrm.eulerAngles = new Vector3(0, 0, ry);
                fwd = Trnsfrm.up;
                desSpeed = Mathf.Clamp(Vector3.Dot( fwd, vec), -MaxSpeed /2 , MaxSpeed );

                if(desSpeed > -MaxSpeed / 6) {
                    if(desSpeed < MaxSpeed / 2)
                        desSpeed = Mathf.Max(Mathf.Min(mag, MaxSpeed / 2), desSpeed);
                } else {

                    if(desSpeed < - MaxSpeed / 6 - speed ) {
                        if(desSpeed > -MaxSpeed / 3)
                            desSpeed = Mathf.Min(-Mathf.Min(mag, MaxSpeed / 3), desSpeed);
                    } else
                        desSpeed = Mathf.Max(Mathf.Min(mag, MaxSpeed / 2), -desSpeed);
                }
                //Debug.Log("ms  " + desSpeed  +   "  --- " + ( (-MaxSpeed / 6 - speed) ) );
              //  if(desSpeed > -MaxSpeed / 4 && desSpeed < 0 && speed > desSpeed / 4)
              //      desSpeed = -desSpeed;
            } else PathActive = false;
        }
        float acc = 2;
        if( Mathf.Abs(desSpeed) < Mathf.Abs(speed) ) acc *= 3;
        speed = Mathf.Lerp(speed, desSpeed, acc * Time.deltaTime);
        //        var pos = Trnsfrm.position; pos.y = 0; Trnsfrm.position = pos;
        GetComponent<Rigidbody2D>().velocity = fwd * speed;
    }

    void OnDrawGizmos() {
        Trnsfrm = transform;
        Gizmos.color = Color.black;
        Gizmos.DrawLine(Trnsfrm.position, DesPos);
    }
}
