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
    public LayerMask EnemyMask;  //AllyMask

    public const int Team1i = 10, TeamC = 8;
    public int Layer = 0;

    //[SyncVar]
    public float Score = 0;

    public bool IsLocalTeam = false;
    public void start(int teamI) {
        IsLocalTeam = false;

        Layer = Team1i + teamI;
        EnemyMask = (((1 << TeamC * 2) - 1) ^ ((1 << teamI) | (1 << teamI + TeamC))) << Team1i;    //playing with ma bits -- (hashtag) real programmerz
        ColPoolI = Random.Range( 0, ColorPool.Count );
    }
 }
