using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//using System;

public class Sys : MonoBehaviour {

    public List<Team> Teams = new List<Team>();

   // public GameObject HealthBarFab, CanvasObj;


    public Material BaseColMat;
    //public int ActiveTeams = 2;
   // public int TeamInteractMask = 0;

  //  public GameObject PlayerFab;
  //  public GameObject CommanderFab;

    static Sys Singleton;
    public static Sys get() {
#if UNITY_EDITOR
        if(Singleton == null) Singleton = FindObjectOfType<Sys>();
#endif //UNITY_EDITOR
        return Singleton;
    }
    void Awake() {
        if(Singleton != null && Singleton != this) Debug.LogError("Singleton violation");
        Singleton = this;

    }

     public Text UI;

     void Update() {

         UI.text = " Unit Count : " + NetMan.UnitCount;

     }
    /*
    NetServer Server;
    NetClient Client;*/

#if UNITY_EDITOR

    void OnDrawGizmos() {

    }

#endif

}
