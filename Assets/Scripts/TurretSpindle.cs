using UnityEngine;
using System.Collections;

public class TurretSpindle : MonoBehaviour {

    public Transform DropPoint;

    void OnDrawGizmos() {

        if(Application.isPlaying) return;

        var p = DropPoint.position; p.Scale(DropPoint.forward);
        DropPoint.position -= p;
    }
}
