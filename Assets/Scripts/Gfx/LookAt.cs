using UnityEngine;
using System.Collections;

public class LookAt : MonoBehaviour {

    public Transform At;
    public Vector3 Offset = Vector3.zero;

    Transform Trnsfrm;
    void Awake() {
        Trnsfrm = transform;
    }
    void OnEnable() {
        Update();
    }
	void Update () {

        Trnsfrm.LookAt(At.position + Offset, Vector3.forward);
        Debug.DrawLine(Trnsfrm.position, At.position + Offset);
	}
}
