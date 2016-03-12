using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Death_Hlpr : MonoBehaviour {


    //public Rigidbody Body;

    [HideInInspector]
    public List<Transform> Parts = new List<Transform>();


    public Color C, C2;
    float T;

	void Start () {
	
	}

    float s1 = 2.5f, s2 = 6;
	void FixedUpdate () {


        if(T < s1) {
            float t = T / s1;

            t *= t;
            foreach(var trn in Parts) {
                var b = trn.GetComponent<Rigidbody>();
                b.velocity -= b.velocity * t;
                b.angularVelocity -= b.angularVelocity * t;
            }
        } 
	}

    bool Stage2 = false;
    void Update() {
        T += Time.deltaTime;
        
        if( !Stage2) {
            Stage2 = T > s1;
            if(Stage2) {
                foreach(var trn in Parts)
                    Destroy( trn.GetComponent<Rigidbody>() );
            } else {
                float t = T / s1;
                var c = Color.Lerp(C, C2, t);
                Unit.fixCol_In(Parts[0].gameObject, c);
            }
        } else {
            foreach(var trn in Parts)
                trn.position += Vector3.back * Time.deltaTime * 0.05f;

            if(T > s2 && Parts[0].position.z < 1 )
                Destroy(gameObject);
        }            
    }


}
