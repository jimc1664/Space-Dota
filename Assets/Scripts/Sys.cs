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

    //Reference to Minimap Cam
    [SerializeField ]
    GameObject minimap;
    //Reference to netmanager hud script
    [SerializeField]
    GameObject netManager;
    NetworkManagerHUD netHUD;


    public Text UI;
    public Text SquidUI, Squid_Unref_UI, Squid_Cap_UI;
    public Text PopUI;

    public GameObject StartUI;
    public GameObject GameUI;

    public List<GameObject> Carriers;
    public List<GameObject> CarrierSpecUI;

    public GameObject TurretCtorDialog;


    [System.Serializable]
    public class BuildingSite_Dat {
        public List<GameObject> Options;
        public GameObject Menu;
    }
    public List<BuildingSite_Dat> SiteDat;

    public List<BuildingSite> Site;


    public bool Started = false;

    NetMan NetM;

    void Start() {

        for(int i = Teams.Count; i-- > 0; )
            Teams[i].start(i);

        netHUD = netManager.GetComponent<NetworkManagerHUD>();

    }
    public float TimeRem = 10*60;

    void Update() {

        if(Started) {
            int s1 = Mathf.FloorToInt(Teams[0].Score);
            int s2 = Mathf.FloorToInt(Teams[1].Score);
            TimeRem -= Time.deltaTime;
            UI.text ="Blue:  "+s2+"   Red: "+s1+"   Time Remaing "+TimeRem;

            int max = 5000;
            if(s1 > max || s2 > max || TimeRem < 0) {
                Time.timeScale = 0;
                if(s1 > s2)
                    UI.text ="Red Wins (woo)";
                else
                    UI.text ="Blue Wins (woo)";
            }

        } else UI.text = ""; // Unit Count : " + NetMan.UnitCount;

         if(NetM.WeIsHosting) {

             if(!Started) {
                 var players = FindObjectsOfType<Player>();
                 foreach(var p in players)
                     if((uint)p.CarrierSelection > (uint)Carriers.Count) return;


                 foreach(var p in players)
                     p.spawn();

                 foreach(var t in FindObjectsOfType<BuildingSite>()) {
                     if(t.InitSel != -1) {
                         t.create(t.InitSel, t.Ti, 0, 1 );
                     }
                 }
                 return;
             }
         }
     }

     public void startGame() {
        if (Started)
            return;
       
        Started = true;
        GameUI.SetActive(true);
        StartUI.SetActive(false);
        minimap.SetActive(true);
        netHUD.enabled = false;
       
    }
    /*
    NetServer Server;
    NetClient Client;*/

#if UNITY_EDITOR

    void OnDrawGizmos() {

    }

#endif

}
