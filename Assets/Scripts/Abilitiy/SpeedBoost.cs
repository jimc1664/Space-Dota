using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SpeedBoost : NetBehaviour {

    public KeyCode Key;
    public float AccFactor, SpdFactor, Cooldown, Duration;
    public int Cost;
    Carrier Car;
    float Timer ;

	void OnEnable() {
        Car = GetComponent<Carrier>();
        //if(!Car.Owner.isLocalPlayer && !Car.isServer) Destroy(this);
        Timer = Time.time-Cooldown;
	}


    bool check() {
        return Mathf.FloorToInt(Car.Owner.Squids) > Cost && (Time.time - Timer) > Cooldown;
    }

    [Command]
    void Cmd_boost() {  //todo  -- if command fails - could be timing issues  -- should try repeatedly for few frames...
       // Debug.Log("Cmd_boost");
        if(Car == null || Car.Owner == null || !check()) return;

        Rpc_apply();  //todo - don't think this is called immediately...it needs to be (squid reduction)
    }

    [ClientRpc]
    void Rpc_apply() {
       // Debug.Log("Rpc_apply");
        Car.Owner.Squids -= Cost;
        Timer = Time.time;

        Car.buff(Unit.Buffable.MaxSpeed, SpdFactor, Duration);
        Car.buff(Unit.Buffable.Acceleration, AccFactor, Duration);
    }

    //[ClientCallback]

	void Update () {
        if(!Car.Owner.isLocalPlayer) return;
        //Debug.Log("Key  " + Input.GetKeyUp(Key) + "  ----  " + (Mathf.FloorToInt(Car.Owner.Squids) > Cost) + "  --  " + (Time.time - Timer)  );
        if(Input.GetKeyUp(Key) && check()) {
        //    Debug.Log("Cmd?");
            Cmd_boost();
        }
	}
}
