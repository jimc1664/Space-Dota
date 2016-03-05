﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class HUDEventManager : Unit
{
    [SerializeField]
    Player carrier;

    [System.Serializable]
    public class SpawnData
    {
        public GameObject Fab;
        public KeyCode Key;
        public float ConstructTime = 1;  // delay before can build summit else 

    };
    public List<SpawnData> SpawnDat;

    public List<GameObject> SpawnPoints;

    public float BuildTimer = 0.5f; //initial delay

    void Update()
    {

        base.Update();
        if (Owner == null || !Owner.isLocalPlayer) return;

        BuildTimer -= Time.deltaTime;
        if (BuildTimer > 0) return;

    }

    [Command]
    void Cmd_createFrom(byte i)
    {
        Debug.Log("Cmd_createFrom");
        GameObject c = (GameObject)Instantiate(SpawnDat[i].Fab, Vector3.zero, Quaternion.identity);

        NetworkServer.Spawn(c);

        //todo -- SpawnPoints.Count  >= 256 == err
        c.GetComponent<UnitSpawn_Hlpr>().Rpc_init(carrier.gameObject, (byte)Random.Range(0, SpawnPoints.Count));
    }


    public void GenerateTank()
    {
        SpawnData sd = SpawnDat[0];
        //todo getkeyup should be reference to real ui
        Owner.Cmd_createFrom((byte)0, carrier.gameObject);  //more than 256 ?? nah
        BuildTimer = sd.ConstructTime;  //todo -- verify
        Debug.Log("Cmd_createFrom please??");
    }
}
