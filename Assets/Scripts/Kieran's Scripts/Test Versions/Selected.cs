using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Selected : MonoBehaviour {

    [SerializeField]
    private bool selected = false;
    [SerializeField]
    private GameObject selectionGlow = null;
    private GameObject glow = null;

    private GameObject[] unitTypeArray;

    //Selection Variables
    bool selectAllEnabled = false;
    bool callOnce = false;

    // Update is called once per frame
    void Update()
    {
        DragClick();
        ParticleSelection();

        if (Input.GetKeyDown(KeyCode.LeftAlt))
            selectAllEnabled = !selectAllEnabled;

        if (selectAllEnabled)
        {
            Debug.Log("Enabled");
            SelectAllSpecificUnits();
            callOnce = true;
        }
        else if (!selectAllEnabled && callOnce)
        {
            Debug.Log("Disabled");
            DeselectAllSpecificUnits();
            callOnce = false;
        }
          
    }

    /// <summary>
    /// Manages the activation of particle effect system if unit is selected
    /// </summary>
    private void ParticleSelection()
    {
        if (selected && glow == null)
        {
            glow = (GameObject)GameObject.Instantiate(selectionGlow);    //Instance of the glow particle system is created & moved to the unit's location
            glow.transform.parent = transform;
            glow.transform.localPosition = new Vector3(0, -GetComponent<MeshFilter>().mesh.bounds.extents.y, 0);
        }
        else if (!selected && glow != null)
        {
            Destroy(glow);
            glow = null;
        }

    }


    public void DragClick()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 camPos = Camera.main.WorldToScreenPoint(transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
            camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);
            selected = BoxSelector.selection.Contains(camPos);
        }
    }

    public void OnMouseOver()
    {

        if(Input.GetMouseButton(0))
        {
            gameObject.GetComponent<Selected>().selected = true;
        }
    }


    public void SelectAllSpecificUnits()
    {
        unitTypeArray = GameObject.FindGameObjectsWithTag(gameObject.tag);
        Debug.Log(gameObject.tag);
        foreach (GameObject g in unitTypeArray)
        {
            g.GetComponent<Selected>().selected = true;
        }
    }

    public void DeselectAllSpecificUnits()
    {
        unitTypeArray = GameObject.FindGameObjectsWithTag(gameObject.tag);
        foreach (GameObject g in unitTypeArray)
        {
            g.GetComponent<Selected>().selected = false;
              g.GetComponent<Selected>().selected = false;
           
        }
    }

   
  
}
