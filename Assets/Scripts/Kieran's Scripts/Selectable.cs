using UnityEngine;
using System.Collections;

public class Selectable : MonoBehaviour {


    //todo radius ??

    [HideInInspector]
    public bool selected = false;
    bool lSelected = false;

    [HideInInspector]
    public Unit U;

    void Awake() {
        U = GetComponent<Unit>();

    }

    void Update () {

        if(U.Owner == null) return;

        if ( Input.GetMouseButtonUp(0))
        {
            Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
            camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);  //more efficent to transform rect to screen space  (not that it makes any real difference) 
            selected = BoxSelector.selection.Contains(camPos);  //todo radius
        }


        if(lSelected != selected) {
            if(selected)  //todo real selection ui
                //     GetComponent<Renderer>().material.color = Color.red;
                U.fixCol(Color.green);
            else
                U.fixCol(U.Owner.Col);

            lSelected = selected;
        } 
	}
}
