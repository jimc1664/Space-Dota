using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

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
        NetM = FindObjectOfType<NetMan>();
    }

    public Text UI;
    public Text SquidUI, Squid_Unref_UI, Squid_Cap_UI;
    public Text PopUI;

    public GameObject StartUI;
    public GameObject GameUI;

    public List<GameObject> Carriers;
    public List<GameObject> CarrierSpecUI;

    public GameObject TurretCtorDialog;

    public bool Started = false;

    NetMan NetM;
    void Update() {

         UI.text = " Unit Count : " + NetMan.UnitCount;

         if(NetM.WeIsHosting) {

             if(!Started) {
                 var players = FindObjectsOfType<Player>();
                 foreach(var p in players)
                     if((uint)p.CarrierSelection > (uint)Carriers.Count) return;


                 foreach(var p in players)
                     p.spawn();
                 return;
             }
         }
     }

     public void startGame() {
        if(Started) return;
        Started = true;

        GameUI.SetActive(true);
        StartUI.SetActive(false);



     }
    /*
    NetServer Server;
    NetClient Client;*/

#if UNITY_EDITOR

    void OnDrawGizmos() {

    }

#endif

}
