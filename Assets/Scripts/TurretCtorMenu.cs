using UnityEngine;
using System.Collections;

public class TurretCtorMenu : MonoBehaviour {

    Transform Trnsfrm;
    public TurretSpindle Spindle;

    static TurretCtorMenu InTheEndThereCanBeOnlyOne;
    void Awake() {
        if(InTheEndThereCanBeOnlyOne != null)
            Destroy(InTheEndThereCanBeOnlyOne.gameObject);
        InTheEndThereCanBeOnlyOne = this;
        Trnsfrm = transform;
    }
    public void click( int i ) {
        foreach(var s in FindObjectsOfType<Sapper>())
            if(s.Task == this && s.Owner.isLocalPlayer ) {
                s.Owner.Cmd_taskSapper(s.gameObject, Spindle.Index, i);
                s.Task = null;
            }

        
        Destroy(gameObject);
    }

    void Update() {
        Trnsfrm.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.forward);
    }

}
