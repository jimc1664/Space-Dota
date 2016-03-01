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
   
    
    void Awake() {
        U = GetComponent<Unit>();
    }

    void Update () {

        if(U.Owner == null) return;

        if(selected || Highlighted) 
            LocalUI.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Vector3.forward);
        if(selected && U.Owner.isLocalPlayer )
            Projector.localEulerAngles += new Vector3(0, 0, 10.0f) * Time.deltaTime;
/*
        if ( Input.GetMouseButtonUp(0))  {
            Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
            camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);  //more efficent to transform rect to screen space  (not that it makes any real difference) 
            selected = BoxSelector.selection.Contains(camPos);  //todo radius
        }
        */
        Projector.gameObject.SetActive(selected && U.Owner.isLocalPlayer);
        LocalUI.gameObject.SetActive(selected || Highlighted );

        if(lSelected != selected) {       
            lSelected = selected;
        } 
	}
}
