using UnityEngine;
using System.Collections;

public class AnimFeedback : MonoBehaviour {

    public int Flags = 0;

    public Animator Ctrl;
    void Awake() {
        Ctrl = GetComponent<Animator>();
    }

    public void setFlagA_On() { Flags |= 1; }
    public void setFlagA_Off() { Flags &= (-1 ^1 ); }

    public bool getFlag( int i ) {
        return (Flags & (1 << i) ) != 0 ;
    }
}
