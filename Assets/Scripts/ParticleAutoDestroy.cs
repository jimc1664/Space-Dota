using UnityEngine;
using System.Collections;

public class ParticleAutoDestroy : MonoBehaviour {

    ParticleSystem Ps;

    void Start() {
        Ps = GetComponentInChildren<ParticleSystem>();
    }

    void Update() {

        if(Ps == null) {
            Destroy(gameObject);
            return;
        }
        if( !Ps.IsAlive()) {
            Destroy(Ps.gameObject);
            Ps = GetComponentInChildren<ParticleSystem>();
        }
    }
    
}
