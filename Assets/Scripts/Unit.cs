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

    [HideInInspector]
    public Transform Trnsfrm;
    [HideInInspector]
    public Rigidbody2D Body;
    [HideInInspector]
    public Player Owner;

    public GameObject VisDat;

    public float MaxSpeed = 5;
    public float TurnSpeed = 10;
    public float Acceleration = 2;

    void Awake() {
        Trnsfrm = transform;
        Body = GetComponent<Rigidbody2D>();
        DesPos = transform.position;
    }
    void Start() {
        DesPos = transform.position;
    }

    public void fixCol( Color c ) {

        foreach(var mr in VisDat.GetComponentsInChildren<MeshRenderer>()) {
            foreach(Material mat in mr.materials) {
               // Debug.Log("fix?");
                if(mat.name == Sys.get().BaseColMat.name + " (Instance)" ) {
                  //  Debug.Log("fix!!!!!????");

                    mat.color = c;
                }
            }
        }
    }

    public void init(Player o) {

        Owner = o;
        fixCol(o.Col);

    }
    /* public void init(Player o) {  NOTE:  --- it appears   Server's local client also gets Rpc function called.. which is bit weird...it's kind of handy so will tolerate it
        if(isServer) Rpc_init(o.gameObject);  //always do network bit first, slightly better

        _int_init(o);
    } */
    [ClientRpc]
    public void Rpc_init(GameObject oo) { init(oo.GetComponent<Player>()); }

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
                float rotSpeed = (1.0f - Mathf.Abs(0.5f - Mathf.Pow(speed / MaxSpeed, 2))) * TurnSpeed;  
                ry = Mathf.LerpAngle(ry, Mathf.MoveTowardsAngle(ry, dy, 600 * Time.deltaTime), rotSpeed * Time.deltaTime);
                ///Trnsfrm.RotateAround(  
                //Trnsfrm.eulerAngles = new Vector3(0, 0, ry);
                Body.MoveRotation( ry );
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
        float acc = Acceleration;
        if( Mathf.Abs(desSpeed) < Mathf.Abs(speed) ) acc *= 3;
        speed = Mathf.Lerp(speed, desSpeed, acc * Time.deltaTime);
        //        var pos = Trnsfrm.position; pos.y = 0; Trnsfrm.position = pos;
        Body.velocity = Vector2.Lerp(fwd * speed, Body.velocity, 20 * Time.deltaTime);
    }

    void OnDrawGizmos() {
        Trnsfrm = transform;
        Gizmos.color = Color.black;
        Gizmos.DrawLine(Trnsfrm.position, DesPos);
    }
}
