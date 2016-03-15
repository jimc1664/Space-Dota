using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Carrier : Vehicle {

    [System.Serializable]
    public class SpawnData {
        public GameObject Fab;
        public KeyCode Key;
        public float ConstructTime = 1;  // delay before can build summit else 

    };
    public List<SpawnData> SpawnDat;


    [System.Serializable]
    public class SpawnPoint {
        public Transform FirstPoint;
        public UnitSpawn_Hlpr CurSpwn;

        public bool IsOpen, LastDes;
    }
    public List<SpawnPoint> SpawnPoints;

    public int getSpawnPointI(byte sdi) {
        int ret = -1;
        int start = Random.Range(0, SpawnPoints.Count);
        for(int i = 0; i < SpawnPoints.Count; i++ ) {
            int cr = (start + i) % SpawnPoints.Count;
            if(SpawnPoints[cr].CurSpwn == null) {
                ret = cr;
                break;
            }
        }
        return ret;
    }

    float BuildTimer = 0.5f; //initial delay
    AnimFeedback Anim_FB;

    public float MaxSquidCap = 300, SquidGenRate = 3, SquidRefineRate = 10, SquidRefineEff = 0.5f, SquidMineRate = 50;

    new void Start() {
        base.Start();

        Anim_FB = VisDat.GetComponent<AnimFeedback>();

        if( !isServer )
            foreach(var col in GetComponentsInChildren<CircleCollider2D>()) {
                if(col.isTrigger)
                    Destroy(col.gameObject);
            }
    }
   
    new void Update() {

        base.Update();

        for(int i = SpawnPoints.Count; i-- > 0; ) {
            var sp = SpawnPoints[i];
            if( Anim_FB) {

                bool desOpen = sp.CurSpwn != null;
                if( desOpen != sp.LastDes ) {
                    Anim_FB.Ctrl.SetFloat("Spwn" + (i + 1), desOpen ? 1 : -1);
                    var st = Anim_FB.Ctrl.GetCurrentAnimatorStateInfo(i);

                    if( st.normalizedTime < 0 )
                        Anim_FB.Ctrl.Play(st.fullPathHash, i, 0);
                    else if(st.normalizedTime > 0.3333f )
                        Anim_FB.Ctrl.Play(st.fullPathHash, i, 1 );
                   
                    sp.LastDes = desOpen;
                } 
                sp.IsOpen = Anim_FB.getFlag(i);
            } else 
                sp.IsOpen = true;

        }

        if(Owner == null || ( !Owner.isLocalPlayer && !isServer) ) return;

        if(isServer) {
            if(ResFields.Count > 0 && (Owner.UnrefSquids + Owner.Squids < MaxSquidCap + 0.001f)) {
                float r = SquidMineRate * Time.deltaTime, fullR = r;
                r = Mathf.Min(r, MaxSquidCap - (Owner.UnrefSquids + Owner.Squids));
                float or = r;

                int pick = Random.Range(0, ResFields.Count);   //careful cos we doing naughty things around networking

                var rp = ResFields[pick]; if(rp != null) rp.grab(ref r, fullR, Trnsfrm);
                if(r > 0) {
                    for(int i = ResFields.Count; i-- > 0; ) {
                        var res = ResFields[i];
                        if(res == null) {
                            ResFields.RemoveAt(i);
                            break;
                        }
                        if(i == pick) continue;
                        res.grab(ref r, fullR, Trnsfrm);
                        if(r == 0) break;
                    }
                }
                Owner.UnrefSquids += or - r;
            }
        
            if(Owner.UnrefSquids > 0) {
                var r = SquidRefineRate * Time.deltaTime;
                if( r < Owner.UnrefSquids ) 
                    Owner.UnrefSquids -= r;
                else {
                    r = Owner.UnrefSquids;
                    Owner.UnrefSquids = 0;
                }
                Owner.Squids += r * SquidRefineEff;
            }
            Owner.Squids += SquidGenRate * Time.deltaTime;
            if(Owner.Squids > MaxSquidCap) Owner.Squids = Owner.MaxSquids;
            if(Owner.Squids + Owner.UnrefSquids > MaxSquidCap) Owner.UnrefSquids = Owner.MaxSquids - Owner.Squids;
        }
        if(isServer) {
            Health += 4.0f*Time.deltaTime / MaxHealth;
            if(Health > 1) Health = 1;
        }

        if(!Owner.isLocalPlayer) return;
        BuildTimer -= Time.deltaTime;
      //  if( BuildTimer > 0 ) return;

        ManageInput();
    }


    void ManageInput() {
        for(int i = SpawnDat.Count; i-- > 0; ) {
            SpawnData sd = SpawnDat[i];
            if(Input.GetKeyUp(sd.Key)) {  //todo getkeyup should be reference to real ui
                spawnUnit(i);
            }

        }
    }
    public void spawnUnit(int i) {
        SpawnData sd = SpawnDat[i];
        Owner.Cmd_createFrom((byte)i, gameObject);  //more than 256 ?? nah
        BuildTimer = sd.ConstructTime;  //todo -- verify
        Debug.Log("Cmd_createFrom please??");
    }

    /*
    [Command]
    void Cmd_createFrom( byte i) {
        Debug.Log("Cmd_createFrom");
        GameObject c = (GameObject)Instantiate(SpawnDat[i].Fab, Vector3.zero, Quaternion.identity);


        NetworkServer.Spawn(c);

        //todo -- SpawnPoints.Count  >= 256 == err
        c.GetComponent<UnitSpawn_Hlpr>().Rpc_init(gameObject, (byte)Random.Range(0, SpawnPoints.Count));
    }  */

    List<ResourceField> ResFields = new List<ResourceField>();


    [Server]
    void OnTriggerEnter2D(Collider2D other) {
        var res = other.GetComponent<ResourceField>();
        if(res == null) {
          //  Debug.Log("triggered??  " + other.name);
        } else {
            Debug.Assert(!ResFields.Contains(res));
            res.Carriers++;
            ResFields.Add(res);
        }
    }

    [Server]
    void OnTriggerExit2D(Collider2D other) {
        var res = other.GetComponent<ResourceField>();
        if(res == null) {
          //  Debug.Log("triggered2??  " + other.name);
        } else {
            Debug.Assert(ResFields.Contains(res));
            ResFields.Remove(res);
            res.Carriers--;
        }
    }
    new void OnDisable() {
        base.OnDisable();
        foreach(var r in ResFields)
            if(r != null)
                r.Carriers--;
        ResFields.Clear();
    }

    [ClientRpc]
    new public void Rpc_init(GameObject oo) {
        // if(isServer) return; //better way..
        init(oo.GetComponent<Player>());

        Owner.Car = this;
        Owner.MaxSquids = MaxSquidCap;
        Owner.Squids = 100;


        if(Owner.isLocalPlayer) {
            var s = Sys.get();
            for(int i = s.Carriers.Count; i-- > 0; )
                    s.CarrierSpecUI[i].SetActive(false);

            Sys.get().CarrierSpecUI[Owner.CarrierSelection].SetActive(true);
          //  fucking shitty sync...

         
            for(int i = s.Carriers.Count; i-- > 0; )
                if(name ==  s.Carriers[i].name+"(Clone)" )
                    s.CarrierSpecUI[i].SetActive(true);


            var ct = Camera.main.transform;
            var p = ct.position; 
            p.x = transform.position.x;
            p.y = -7;
            ct.position = p;
            Sys.get().startGame();
        }
    }
}
