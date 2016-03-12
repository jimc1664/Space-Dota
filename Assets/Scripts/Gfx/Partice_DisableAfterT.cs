using UnityEngine;
using System.Collections;

public class Partice_DisableAfterT : MonoBehaviour {

    public float T = 1;
	// Update is called once per frame
	void Update () {
        if((T -= Time.deltaTime) < 0) {
            foreach(var ps in GetComponentsInChildren<ParticleSystem>()) {
                ps.Stop(false);
            }
        }
	}
}
