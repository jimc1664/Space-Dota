using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Player : NetBehaviour {


    [SyncVar]  ///lazy!
    public GameObject Cmmdr;

    public Unit Selected;

    /*
    static Player Singleton;
    public static Player get() {
#if UNITY_EDITOR
        if(Singleton == null) Singleton = FindObjectOfType<Player>();
#endif //UNITY_EDITOR
        return Singleton;
    } */


    void Awake() {
      //  if(Singleton != null && Singleton != this) Debug.LogError("Singleton violation");
      //  Singleton = this;
    }

    [ServerCallback]
    void Start() {

        Sys s = Sys.get();


        int ti = Random.Range(0, s.Teams.Count );
        for(int i = 0; i < s.Teams.Count; i++)
            if(s.Teams[i].Members.Count < s.Teams[ti].Members.Count)
                ti = i;

        Team t = s.Teams[ti];
        int colI = (t.ColPoolI++)%t.ColorPool.Count;
        Col = t.ColorPool[colI];

        Rpc_init((byte)(uint)ti, (byte)(uint)colI );


        GameObject c = (GameObject)Instantiate(Cmmdr, Vector3.zero, Quaternion.identity );
        NetworkServer.Spawn(c);
        c.GetComponent<Carrier>().Rpc_init( this.gameObject );
    }





    public Color Col;
  //  public Material Mat;
    public Team Tm;

    public void init(byte teamI, byte colI) {
        Tm = Sys.get().Teams[teamI];
        Col = Tm.ColorPool[colI];
        Tm.Members.Add(this);
    }
     
    [ClientRpc]
    void Rpc_init(byte teamI, byte colI) { init(teamI, colI); }

    void Update() {
        if (!isLocalPlayer)  return;

        if(Input.GetMouseButtonUp(0)) {
            Selected = null;
            RaycastHit hit;
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, -1 )) {
                Debug.Log(" hit "+hit.collider.name);
                Selected = hit.collider.gameObject.GetComponentInParent<Unit>();
            }

        }
        //if(Selected == null) return;
        if(Input.GetMouseButtonUp(1)) {
            RaycastHit hit;
            if(Physics.Raycast( Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, 1<<LayerMask.NameToLayer("Map"))) {

                if(Selected) {
                    var s = Selected.GetComponent<Selectable>();
                    if( s != null && s.selected == false )
                        Cmd_moveUnit(Selected.gameObject, hit.point);
                }

                //todo -- this is unbelievably wrong
                foreach(var s in FindObjectsOfType<Selectable>()) {
                    if( s.selected && s.U.Owner == this )
                        Cmd_moveUnit(s.gameObject, hit.point);
                }
            }

        }

    }
 
    [Command]
    public void Cmd_moveUnit(GameObject u, Vector3 p) {
        u.GetComponent<Unit>().DesPos = p;
        u.GetComponent<Unit>().PathActive = true;
        Debug.Log("move unit " + u.name + "  to - " + p);
    }


    [Command]
    public void Cmd_createFrom(byte i, GameObject muhCarrier ) {
        if(muhCarrier == null) return;  // it died mayhaps?
        var c = muhCarrier.GetComponent<Carrier>();
        if(c== null || c.Owner != this) return; //todo - zomg cheater .. ban-hammer  or possibly error..
        Debug.Log("Cmd_createFrom");
        GameObject go = (GameObject)Instantiate(c.SpawnDat[i].Fab, Vector3.zero, Quaternion.identity);

        NetworkServer.Spawn(go);
        //todo -- SpawnPoints.Count  >= 256 == err
        go.GetComponent<UnitSpawn_Hlpr>().Rpc_init(muhCarrier, (byte)Random.Range(0, c.SpawnPoints.Count));
    }
}
