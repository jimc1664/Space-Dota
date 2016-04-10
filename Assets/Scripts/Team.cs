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
    public LayerMask EnemyMask, AllyMask;

    public const int Team1i = 10, TeamC = 8;
    public int Layer = 0;

    //[SyncVar]
    public float Score = 0;

    public bool IsLocalTeam = false;
    public void start(int teamI) {
        IsLocalTeam = false;

        Layer = Team1i + teamI;
        AllyMask = ((1 << teamI) | (1 << teamI + TeamC)) << Team1i;
        EnemyMask = (((1 << TeamC * 2) - 1) << Team1i ) ^ AllyMask;    //playing with ma bits -- (hashtag) real programmerz
      
        ColPoolI = Random.Range( 0, ColorPool.Count );
    }

    public bool Rebuff = false;
    public void speedBuff(float dur, float af, float sf) {
        Glob_SpeedBoost = Time.time + dur;
        Glob_SpeedBoost_LDur = dur;
        Glob_SpeedBoost_Eff_MaxSpd = sf;
        Glob_SpeedBoost_Eff_Acc = af;
        Rebuff = true;
    }
    public void dmgBuff(float dur, float dmg, float rof) {
        Glob_DamageBoost = Time.time + dur;
        Glob_DamageBoost_LDur = dur;
        Glob_DamageBoost_Eff_RoF = rof;
        Glob_DamageBoost_Eff_Dmg = dmg;
    }
    public void healthBuff(float dur, float rep, float mit) {
        Glob_HealthBoost = Time.time + dur;
        Glob_HealthBoost_LDur = dur;
        // Glob_HealthBoost_Eff_Repair = rep;

        if(Members[0].isServer) {
            foreach(var u in GameObject.FindObjectsOfType<Unit>())
                if(u.Tm == this)
                    u.Health = Mathf.Min(u.Health + rep, 1);
        }
        Glob_HealthBoost_Eff_Mitigation = mit;
    }
    public float Glob_SpeedBoost = -999.0f, Glob_SpeedBoost_LDur = 1, Glob_SpeedBoost_Eff_MaxSpd = 2.0f, Glob_SpeedBoost_Eff_Acc = 2;
    public float Glob_DamageBoost = -999.0f, Glob_DamageBoost_LDur = 1, Glob_DamageBoost_Eff_RoF = 1.0f, Glob_DamageBoost_Eff_Dmg = 1;
    public float Glob_HealthBoost = -999.0f, Glob_HealthBoost_LDur = 1, Glob_HealthBoost_Eff_Repair = 0.0f, Glob_HealthBoost_Eff_Mitigation = 1;

    public void update() {
        if(Rebuff)
            Debug.Log("debuf");
        Rebuff = false;
        
        if(Glob_DamageBoost < Time.time) {
            Glob_DamageBoost_Eff_RoF = Glob_DamageBoost_Eff_Dmg = 1;
        }

        Glob_HealthBoost_Eff_Repair = 0;
        if(Glob_DamageBoost < Time.time) {
            Glob_HealthBoost_Eff_Mitigation = 1;
        }
    }
 }
