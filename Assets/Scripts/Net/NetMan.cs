using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class NetMan : NetworkManager {


    public override void OnServerConnect(NetworkConnection conn) { //someone connected to our game
        base.OnServerConnect(conn);
        //  Debug.Log(" OnServerConnect "+conn);
    }
    public override void OnServerDisconnect(NetworkConnection conn) { //someone dis-connected to our game


        //!!!   do not destroy player object --- incase rejoins... ???

        //base.OnServerDisconnect(conn);
        //  Debug.Log(" OnServerConnect "+conn);
    }

    interface IMsg {
        short getId();
    };

    
    
    class Msg_SyncDat : MessageBase, IMsg {  
        public Msg_SyncDat() { }
  
        public class UnitDat {  ///todo  - class??
                                ///
            public UnitDat() {
            }
            public UnitDat(Unit u) {
                Uo = u.gameObject;
                Pos = u.Body.position;
                Ang = u.Body.rotation;
                Vel = u.Body.velocity;
            }
            public GameObject Uo; //todo  short id

            // todo -- not floats..
            public Vector2 Pos;
            public float Ang;
            public Vector2 Vel;


            
        };
        public UnitDat[] Objs;

        public int Frame;

        public const int Id = MsgType.Highest + 1;
        public short getId() { return Id; }
    };

    int SynDat_Frame = 0;
    void recv(Msg_SyncDat msg ) {
      //  Debug.Log("recv syncdat !!");
        if( msg.Frame < SynDat_Frame  && msg.Frame+100000 > SynDat_Frame ) return;

        //if(!WeIsHosting )
        SynDat_Frame = msg.Frame;

        foreach(var ud in msg.Objs) {
            if(ud.Uo == null) continue; //possible cos we send this unreliable
            var us = ud.Uo.GetComponent<Unit>().SyncO;
            if( us == null ) continue;
            us.Body.position = ud.Pos;
            us.Body.rotation = ud.Ang;
            us.Body.velocity = ud.Vel;
        }
    }

    class Msg_Player : MessageBase, IMsg {
        public Msg_Player() { }
        public Msg_Player( Player p ) {
            Po = p.gameObject;
            Team = (byte)Sys.get().Teams.IndexOf(p.Tm);
            ColorI = (byte)p.Tm.ColorPool.IndexOf(p.Col);            
        }
        public GameObject Po;
        public byte Team, ColorI;

        public const int Id = MsgType.Highest + 2;
        public short getId() { return Id; }
    };
    void recv(Msg_Player m) {
        Player p = m.Po.GetComponent<Player>();
        p.init(m.Team, m.ColorI);
    }

    class Msg_Unit : MessageBase, IMsg {
        public Msg_Unit() { }
        public Msg_Unit( Unit u ) {
            Uo =u.gameObject;
            Debug.Log(" uu " + u.GetInstanceID() +"  name"+u.name + "  o " + u.Owner);
            OwnerObj = u.Owner.gameObject;
            Ang = u.Body.rotation;
            Pathing = u.SyncO.PathActive ? (byte)1 : (byte)0;
            DesPos = u.TargetP;

            Vel = u.Body.velocity;
            AngVel = u.Body.angularVelocity;

        }
        public GameObject Uo;
        public GameObject OwnerObj;
        public float Ang;
        public byte Pathing;
        public Vector2 DesPos;

        public Vector2 Vel;
        public float AngVel;

        public const int Id = Msg_Player.Id+1;
        public short getId() { return Id; }
    };
    void recv(Msg_Unit m) {
        Unit u = m.Uo.GetComponent<Unit>();
        u.init( m.OwnerObj.GetComponent<Player>() );
        u.Body.MoveRotation( m.Ang );
        u.Body.velocity = m.Vel;
        u.Body.angularVelocity = m.AngVel;
        u.PathActive = u.SyncO.PathActive = m.Pathing != 0;
        u.TargetP = m.DesPos;

        var sp = u.GetComponent<UnitSpawn_Hlpr>();
        if(sp != null) {
            sp.activate();
            u.enabled = true;
            Destroy(sp);
        }
    }


    class Msg_UnitSpawn_Hlpr : MessageBase, IMsg {
        public Msg_UnitSpawn_Hlpr() { }
        public Msg_UnitSpawn_Hlpr( UnitSpawn_Hlpr u ) {
            Uo = u.gameObject;

        }
        public GameObject Uo;
       // public byte Team, ColorI;

        public const int Id = MsgType.Highest + 2;
        public short getId() { return Id; }
    };
    void recv(Msg_UnitSpawn_Hlpr m) {
        UnitSpawn_Hlpr p = m.Uo.GetComponent<UnitSpawn_Hlpr>();
   //     p.init(m.Team, m.ColorI);
    }

    public static int UnitCount = 0;

    public override void OnServerReady(NetworkConnection conn) {  //someone is ready
        base.OnServerReady(conn);
      //  Debug.Log(" OnServerReady " + conn);

        if(conn.hostId < 0) return; //Is actually server ... or wtf

        /*
        List<string> msg = new List<string>();
        var nbs = FindObjectsOfType<NetBehaviour>();   //things we need to sync --   todo - not sure how this list is built, could be horrendously slow - easy to maintain our own
        foreach(var nb in nbs) {
            Debug.Log(" nb " + nb.name);
            nb.appendForSync(msg);

        }
        conn.Send(Msg_Scene.Id, new Msg_Scene(msg));
        */

        foreach(var p in FindObjectsOfType<Player>()) {
            conn.Send(Msg_Player.Id, new Msg_Player(p));
        }
        foreach(var u in FindObjectsOfType<Unit>()) {
            conn.Send(Msg_Unit.Id, new Msg_Unit(u));
        }
    }

    T recvMsg<T>(NetworkMessage msg) where T : MessageBase, new() {
        T m = msg.ReadMessage<T>();
   //     Debug.Log("Client ::recvMsg " + typeof(T));
        return m;    
    }

    //this may do unnesscary wrapping for each message - may optimise away may not...??
    void regMsgHandle<T>(NetworkClient c, System.Action<T> recv ) where T : MessageBase, IMsg, new() {  c.RegisterHandler((new T()).getId(), (NetworkMessage msg) => {   recv( recvMsg<T>(msg) );    });  }


    public override void OnStartClient(NetworkClient c) {
        base.OnStartClient(c);
        regMsgHandle<Msg_SyncDat>(c, recv);
        regMsgHandle<Msg_Player>(c, recv);
        regMsgHandle<Msg_Unit>(c, recv);
        Debug.Log(" OnStartClient " + c);
    }

    [HideInInspector]
    public bool WeIsHosting = false;
    public override void OnStartHost() {
        Debug.Log(" OnStartHost " );
        //reset stuff...

        var sys = Sys.get();
        foreach(var t in sys.Teams)
            t.Members.Clear();

        base.OnStartHost();

        WeIsHosting = true;

        UnitCount = 0;
    }
    public override void OnStopHost() {

        base.OnStopHost();

        WeIsHosting = false;
    }
    public override void OnClientConnect(NetworkConnection conn) {  //we connected to someones game
         base.OnClientConnect(conn);


         //Debug.Log(" OnClientConnect " + conn);
    }


    float SyncTimer = 0.0f;
  //  [ServerCallback]  sort of
    void Update() {
        if(!WeIsHosting) return;



        float rate = 0.25f;
        if((SyncTimer += Time.deltaTime) > rate) {
            SyncTimer -= rate;


            //FIRE EVERYTHING   -- todo not fireing everything
            var us = FindObjectsOfType<Unit>();
            var msg = new Msg_SyncDat();
            msg.Frame = SynDat_Frame++;
            msg.Objs = new Msg_SyncDat.UnitDat[us.GetLength(0)];
            for( int i = us.GetLength(0); i-- > 0; ) {
                msg.Objs[i] = new Msg_SyncDat.UnitDat(us[i]);
            }
            //todo compress..

            foreach(var conn in NetworkServer.connections) {
                if(conn == null) continue; ///todo investigate why i need this
                if( conn.hostId >= 0 )
                    conn.SendUnreliable(Msg_SyncDat.Id, msg);
            }
        }
    }

   /*// called when a client disconnects
    public virtual void OnServerDisconnect(NetworkConnection conn)  {
        NetworkServer.DestroyPlayersForConnection(conn);
    }

    // called when a client is ready
    public virtual void OnServerReady(NetworkConnection conn)
    {
        NetworkServer.SetClientReady(conn);
    }

    // called when a new player is added for a client
    public virtual void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        var player = (GameObject)GameObject.Instantiate(playerPrefab, playerSpawnPos, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }

    
    // called when a player is removed for a client
    public virtual void OnServerRemovePlayer(NetworkConnection conn, short playerControllerId)
    {
        PlayerController player;
        if (conn.GetPlayer(playerControllerId, out player))
        {
            if (player.NetworkIdentity != null && player.NetworkIdentity.gameObject != null)
                NetworkServer.Destroy(player.NetworkIdentity.gameObject);
        }
    } */

}
