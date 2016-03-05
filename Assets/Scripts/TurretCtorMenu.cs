using UnityEngine;
using System.Collections;

public class TurretCtorMenu : MonoBehaviour {

    Transform Trnsfrm;
    void Awake() {
        Trnsfrm = transform;
    }
    public void click( int i ) {
        Debug.Log(" aa " + i);
    }

    void Update() {
        Trnsfrm.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.forward);
    }

}
