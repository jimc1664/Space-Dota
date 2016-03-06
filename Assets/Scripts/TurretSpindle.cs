using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
public class TurretSpindle : MonoBehaviour {

    public Transform DropPoint;
    public NavMesh.Node Dp_Node;
    public Unit_Structure Structure;

    void OnDrawGizmos() {

        if(Application.isPlaying) return;

      //  var p = DropPoint.position; p.Scale(DropPoint.forward);
        //DropPoint.position -= p;
    }

    public int InitSel = -1, Ti =0;
    public int Index = -1;

    public void Start() {
        var nm = FindObjectOfType<NavMesh>();
        Dp_Node = nm.findNode(DropPoint.position);
        Index = Sys.get().Spindles.IndexOf(this);
        if(Dp_Node == null || Index == -1 ) {
            Debug.Log("err  "+name +"    "+DropPoint.position );
           // Destroy(gameObject);
        }
    }
    
    public void create( int  sel, int ti, int ci, int complete = 0 ) {

        GameObject go = (GameObject)Instantiate(Sys.get().TurretOptions[sel]);
   

        var bh = go.GetComponent<Build_Hlpr>();
        NetworkServer.Spawn(go);
        bh.Rpc_init(Index, ti, ci, sel, complete );

    }
}
