using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class Team {
    //public string Name;
    //public Color Col;

    public List<Color> ColorPool;
    [HideInInspector]
    public int ColPoolI;
    public List<Player> Members;

    public Transform SpawnLoc;

    void Start() {

         ColPoolI = Random.Range( 0, ColorPool.Count );
    }
 }
