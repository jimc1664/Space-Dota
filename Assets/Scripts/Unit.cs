using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Unit : NetworkBehaviour {

    public Vector3 DesPos;
    public Transform Trnsfrm;
    public float Speed = 5;
    void Awake() {
        Trnsfrm = transform;
        DesPos = transform.position;
    }

    [ServerCallback]
    void FixedUpdate() {

        var vec = DesPos - Trnsfrm.position;
        var mag = vec.magnitude;
        
        float desSpeed = 0;

        if(mag > 0.1) {
            var ry = Trnsfrm.eulerAngles.y;
            var dy = Mathf.Rad2Deg* Mathf.Atan2(vec.x, vec.z);
            Debug.Log(" ang = "+ ry+"  des "+ dy);
            ry = Mathf.LerpAngle( ry, Mathf.MoveTowardsAngle(ry, dy, 600*Time.deltaTime), 10 *Time.deltaTime );
            Trnsfrm.eulerAngles= new Vector3(0, ry, 0);
            desSpeed = Mathf.Clamp( Vector3.Dot( Trnsfrm.forward, vec ), 0, 4 );
        }
        float acc = 4;
        if( desSpeed < Speed ) acc *= 3;
        Speed = Mathf.Lerp( Speed, desSpeed, acc*Time.deltaTime );

//        var pos = Trnsfrm.position; pos.y = 0; Trnsfrm.position = pos;
        GetComponent<Rigidbody>().velocity = Trnsfrm.forward * Speed;
    }

    void OnDrawGizmos() {
        Trnsfrm = transform;
        Gizmos.color = Color.black;
        Gizmos.DrawLine(Trnsfrm.position, DesPos);
    }
}
