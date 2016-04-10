using UnityEngine;
using UnityEngine.Networking;


public class Ability : NetBehaviour {


    public KeyCode Key;
    public int Cost;
    public float Cooldown, Duration;
    protected Carrier Car;
    float Timer;

    void OnEnable() {
        Car = GetComponent<Carrier>();
        //if(!Car.Owner.isLocalPlayer && !Car.isServer) Destroy(this);
        Timer = Time.time - Cooldown;
    }


    protected virtual bool check() {
        return Mathf.FloorToInt(Car.Owner.Squids) > Cost && (Time.time - Timer) > Cooldown;
    }
    protected virtual void apply() {
        Debug.LogError("Ability::apply  unimplemented.. ");
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

        apply();
    }

    //[ClientCallback]

    void Update() {
        if(!Car.Owner.isLocalPlayer) return;
        //Debug.Log("Key  " + Input.GetKeyUp(Key) + "  ----  " + (Mathf.FloorToInt(Car.Owner.Squids) > Cost) + "  --  " + (Time.time - Timer)  );
        if(Input.GetKeyUp(Key) && check()) {
           // Debug.Log("Cmd?");
            Cmd_boost();
        }
    }
}
