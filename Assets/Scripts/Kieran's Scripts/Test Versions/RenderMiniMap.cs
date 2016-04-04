using UnityEngine;
using System.Collections;

public class RenderMiniMap : MonoBehaviour {

    Camera miniMap;
    static Texture2D renderedTexture;

    // Use this for initialization
    void Start () {

        miniMap = GameObject.Find("Sys/UI/Mini_Map").GetComponent<Camera>();
        renderedTexture = new Texture2D(miniMap.pixelWidth, miniMap.pixelHeight);
	}
	
	// Update is called once per frame
    void OnPostRender()
    { 

        renderedTexture.ReadPixels(new Rect(0, 0, 500, 500), 1,1);

        renderedTexture.Apply();
	
	}
}
