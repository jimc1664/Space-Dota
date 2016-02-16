using UnityEngine;
using System.Collections;

public class Selected : MonoBehaviour {

    public bool selected = false;
    private bool selectedByClick = false;

    [SerializeField]
    int counter = 0;

    // Update is called once per frame
    void Update () {

        if (GetComponent<Renderer>().isVisible && Input.GetMouseButton(0))
        {
            if (!selectedByClick)
            {
                Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
                camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);
                selected = BoxSelector.selection.Contains(camPos);
            }
            
        }

        if (selected)
            GetComponentInChildren<Projector>().enabled = true;
        else
            GetComponentInChildren<Projector>().enabled = false;

        if (counter == 2)
        {
            selected = false;
            selectedByClick = false;
            counter = 0;
        }
    }

    private void OnMouseDown()
    {
        counter++;
        selected = true;
        selectedByClick = true;
    }

    private void OnMouseUp()
    {
        if (selectedByClick)
            selected = true;
    }
}
