using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Player : NetworkBehaviour {


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
        t.Members.Add(this);
        int colI = (t.ColPoolI++)%t.ColorPool.Count;
        Col = t.ColorPool[colI];

        Rpc_setup((byte)(uint)ti, (byte)(uint)colI );


        GameObject c = (GameObject)Instantiate(Cmmdr, Vector3.zero, Quaternion.identity );
        NetworkServer.Spawn(c);
        c.GetComponent<Unit>().fixCol(Col);
    }





    public Color Col;
  //  public Material Mat;
    public Team Tm;

     
    [ClientRpc]
    void Rpc_setup( byte teamI, byte colI ) {
        Tm = Sys.get().Teams[teamI];
        Col = Tm.ColorPool[colI];
    }

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
        if(Selected == null) return;
        if(Input.GetMouseButtonUp(1)) {
            RaycastHit hit;
            if(Physics.Raycast( Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, 1<<LayerMask.NameToLayer("Map"))) {

                Cmd_MoveUnit(Selected.gameObject, hit.point);
            }

        }

    }
 
    [Command]
    public void Cmd_MoveUnit(GameObject u, Vector3 p) {
        u.GetComponent<Unit>().DesPos = p;
        u.GetComponent<Unit>().PathActive = true;
        Debug.Log("move unit " + u.name + "  to - " + p);
    }

}
