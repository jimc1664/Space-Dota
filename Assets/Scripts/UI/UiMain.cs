using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class UiMain : MonoBehaviour {


    public List<Text> AbilityTxtFields;
    public List<string> AbilityTxt;
    int AbilityFlags = 1;
    List<string> ActiveAbilities = new List<string>();

    void OnEnable() {

        //Sys.get().
    }

    void sub( float t, int i, ref bool dirty ) {
        bool active = t > Time.time;
        if(active == (((AbilityFlags >> i) & 1) != 0) ) return;
        AbilityFlags ^= 1 << i;

        dirty = true;
        if(active)
            ActiveAbilities.Add(AbilityTxt[i]);
        else
            ActiveAbilities.Remove(AbilityTxt[i]);
    }

    void Update () {
        bool dirty = false;
        sub(Sys.get().LocalTeam.Glob_SpeedBoost, 0, ref dirty);
        sub(Sys.get().LocalTeam.Glob_DamageBoost, 1, ref dirty);
        sub(Sys.get().LocalTeam.Glob_HealthBoost, 2, ref dirty);

        for(int i = AbilityTxtFields.Count; i-- > 0;) {
            if( ActiveAbilities.Count > i )
                AbilityTxtFields[i].text = ActiveAbilities[i];
            else
                AbilityTxtFields[i].text = (i == 0) ?  "<i>None</i>" : "";
        }
    }
}
