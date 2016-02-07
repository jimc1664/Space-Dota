using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Carrier : Unit {

    public GameObject Unit1;
    public GameObject Unit2;

    public List<GameObject> SpawnPoints;

    float BuildTimer = 1;
    void Update() {

			BuildTimer -= Time.deltaTime;
			if( BuildTimer > 0 ) return;
			if(Input.GetKeyUp( KeyCode.Alpha1 )) {
				createFrom(Unit1);
				BuildTimer = 0.5f;
			}
			if(Input.GetKeyUp(KeyCode.Alpha2)) {
				BuildTimer = 1.0f;
			}

       
    }

    void createFrom(GameObject fab) {


        GameObject c = (GameObject)Instantiate(fab, Vector3.zero, Quaternion.identity);


        NetworkServer.Spawn(c);

        //todo -- SpawnPoints.Count  >= 256 == err
        c.GetComponent<UnitSpawn_Hlpr>().Rpc_init( gameObject, (byte)Random.Range(0, SpawnPoints.Count));


    }


}
