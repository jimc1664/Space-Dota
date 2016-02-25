using UnityEngine;
using System.Collections;

public class ParticleRotation : MonoBehaviour {

    [SerializeField]
    private float rotSpeed = 0.3f;

	// Update is called once per frame
	void Update () {

        transform.eulerAngles += new Vector3(0, rotSpeed, 0);
	}
}
