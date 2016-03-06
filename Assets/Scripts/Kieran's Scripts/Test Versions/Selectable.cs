using UnityEngine;
using System.Collections;

public class Selectable : MonoBehaviour {


    //todo radius ??

    [HideInInspector]
    public bool selected = false;
    public bool Highlighted = false;
    bool lSelected = false;

    [HideInInspector]
    public Unit U;
    public Transform Projector;
    public Transform LocalUI;

    public Player getOwner() {
        var uk = GetComponent<Unit_Kinematic>();
        if(uk == null) return null;
        return uk.Owner;
    }
    
    void Awake() {
        U = GetComponent<Unit>();
    }

    
    void Update () {
        //NOTE!!!!   -- NetID..  assetId  for double click --maybe ??


       // if(U.Owner == null) return;
        bool proj = false, health = selected || Highlighted;
        if(selected) {
            var uk = GetComponent<Unit_Kinematic>();
            if( uk != null )
            proj = uk.Owner.isLocalPlayer;
        }

        if(health) 
            LocalUI.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Vector3.forward);
        if(proj)
            Projector.localEulerAngles += new Vector3(0, 0, 10.0f) * Time.deltaTime;
/*
        if ( Input.GetMouseButtonUp(0))  {
            Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
            camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);  //more efficent to transform rect to screen space  (not that it makes any real difference) 
            selected = BoxSelector.selection.Contains(camPos);  //todo radius
        }
        */
        if( Projector != null ) Projector.gameObject.SetActive(proj);
        LocalUI.gameObject.SetActive(health);

        if(lSelected != selected) {       
            lSelected = selected;
        } 
	}
}
