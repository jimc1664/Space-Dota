using UnityEngine;
using System.Collections;

public class Selected : MonoBehaviour {

    public bool selected = false;

	// Update is called once per frame
	void Update () {

        if (GetComponent<Renderer>().isVisible && Input.GetMouseButtonUp(0))
        {
            Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
            camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);
            selected = BoxSelector.selection.Contains(camPos);
        }

        if (selected)
            GetComponent<Renderer>().material.color = Color.red;
        else
            GetComponent<Renderer>().material.color = Color.white;
	}
}
