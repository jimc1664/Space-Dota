using UnityEngine;
using System.Collections;

public class BoxSelector : MonoBehaviour {

    public Texture2D selectionHighLight = null;
    public static Rect selection = new Rect(0, 0, 0, 0);
    private Vector3 startClick = -Vector3.one;

	
	// Update is called once per frame
	void Update () {

        CheckCamera();
	
	}

    private void CheckCamera()
    {
        if (Input.GetMouseButtonDown(0))

            startClick = Input.mousePosition;
        else if (Input.GetMouseButtonUp(0))
        {
            if (selection.width < 0)
            {
                selection.x += selection.width;
                selection.width = -selection.width;
            }
            if (selection.height < 0)
            {
                selection.y += selection.height;
                selection.height = -selection.height;
            }

            startClick = -Vector3.one;
        }

        if (Input.GetMouseButton(0))
            selection = new Rect(startClick.x, ScreenToRectSpace(startClick.y), Input.mousePosition.x - startClick.x, ScreenToRectSpace(Input.mousePosition.y) - ScreenToRectSpace(startClick.y));
    }

    private void OnGUI()
    {
        if (startClick != -Vector3.one)
        {
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.DrawTexture(selection, selectionHighLight);
        }
    }

    public static float ScreenToRectSpace(float y)
    {
        return Screen.height - y;
    }
}
