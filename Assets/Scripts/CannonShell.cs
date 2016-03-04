using UnityEngine;
using System.Collections;

public class CannonShell : MonoBehaviour {

    public GameObject MichealBay;
    public Transform Tail;
    public float Speed = 20;

    Transform Target;
    Vector3 Start, TargetP, TargetLO;
    float Delta = 0;

    Transform Trnsfrm;
    void Awake() {
        Trnsfrm = transform;
    }

    public void init(Vector3 s, Vector3 t) {
        TargetP = t;
        Trnsfrm.position = s;
        Trnsfrm.rotation = Quaternion.LookRotation(TargetP - Trnsfrm.position, Vector3.forward);
        Tail.localScale = new Vector3(1,  0.1f, 1);
    }
    void Update () {

        Vector3 vec = TargetP - Trnsfrm.position;
        float mag = vec.magnitude, spd = Speed * Time.deltaTime;
        if(spd >= mag) {
            if( MichealBay != null ) Instantiate(MichealBay, Trnsfrm.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        Trnsfrm.position += vec * spd / mag;
        Delta += spd;
        
        
        Tail.localScale = new Vector3(1, Delta * 0.8f, 1);

	}
}
