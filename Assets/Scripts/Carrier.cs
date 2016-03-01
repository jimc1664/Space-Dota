using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Carrier : Unit {

    [System.Serializable]
    public class SpawnData {
        public GameObject Fab;
        public KeyCode Key;
        public float ConstructTime = 1;  // delay before can build summit else 

    };
    public List<SpawnData> SpawnDat;

    public List<GameObject> SpawnPoints;

    float BuildTimer = 0.5f; //initial delay



    void Update() {

        base.Update();
        if(Owner == null || !Owner.isLocalPlayer) return;

        BuildTimer -= Time.deltaTime;
        if( BuildTimer > 0 ) return;

        for(int i = SpawnDat.Count; i-- > 0;  ) {
            SpawnData sd = SpawnDat[i];
            if(Input.GetKeyUp(sd.Key)) {  //todo getkeyup should be reference to real ui
                Owner.Cmd_createFrom((byte) i, gameObject );  //more than 256 ?? nah
                BuildTimer = sd.ConstructTime;  //todo -- verify
                Debug.Log("Cmd_createFrom please??");
            }
        }
    }

    [Command]
    void Cmd_createFrom( byte i) {
        Debug.Log("Cmd_createFrom");
        GameObject c = (GameObject)Instantiate(SpawnDat[i].Fab, Vector3.zero, Quaternion.identity);


        NetworkServer.Spawn(c);

        //todo -- SpawnPoints.Count  >= 256 == err
        c.GetComponent<UnitSpawn_Hlpr>().Rpc_init(gameObject, (byte)Random.Range(0, SpawnPoints.Count));
    }




}
