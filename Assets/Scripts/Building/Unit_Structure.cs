using UnityEngine;
using System.Collections;

public class Unit_Structure : Unit {

    public int Ind;
    public BuildingSite Site;
    public GameObject VisDat2;
    public Color C;

    new protected void OnEnable() {
        base.OnEnable();
        fixCol_In(Site.gameObject, C);
       // Debug.Log("fix col?? " + C);
        IsHighAsFuckPal = true;
    }
    new protected void OnDisable() {
        base.OnDisable();

        //if(Spindle.Structure == this)
        //    Spindle.Structure = null;
    }

    public float Regen = 1;

    new protected void Update() {
        base.Update();
        Health += Regen * Time.deltaTime / MaxHealth;
        if(Health > 1) Health = 1;

        if( GetComponent<Build_Hlpr>() == null && name == "Node(Clone)"  && Tm != null)
            Tm.Score += Time.deltaTime;
    }
}
