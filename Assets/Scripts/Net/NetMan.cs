using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NetMan : NetworkManager {


    public override void OnServerConnect(NetworkConnection conn) { //someone connected to our game
        base.OnServerConnect(conn);
      //  Debug.Log(" OnServerConnect "+conn);
    }

    //sends scene data --- ie syncs clients scene
    class Msg_Scene : MessageBase {  //this is actually bit more complicated due hlapi  -- somethinsg are auto synced somethings not so much...
        //todo -- client should be dead and incapable until it gets this message...
        public Msg_Scene() { }
        public Msg_Scene(List<string> o) {
            Objs = o.ToArray();
        }
        public string[] Objs;
       // public List<string> Objs; 

    };
    const int Msg_SceneId = MsgType.Highest + 1;
    void recv(Msg_Scene scn) {

        foreach(string s in scn.Objs) {
            Debug.Log("recv " + s);
        }
    }

    public override void OnServerReady(NetworkConnection conn) {  //someone is ready
        base.OnServerReady(conn);
      //  Debug.Log(" OnServerReady " + conn);

        if(conn.hostId < 0) return; //Is actually server ... or wtf


        List<string> msg = new List<string>();
        var nbs = FindObjectsOfType<NetBehaviour>();   //things we need to sync --   todo - not sure how this list is built, could be horrendously slow - easy to maintain our own
        foreach(var nb in nbs) {
            Debug.Log(" nb " + nb.name);
            nb.appendForSync(msg);

        }
        conn.Send(Msg_SceneId, new Msg_Scene(msg ) );
    }

    T recvMsg<T>(NetworkMessage msg) where T : MessageBase, new() {
        T m = msg.ReadMessage<T>();
        Debug.Log("Client ::recvMsg " + typeof(T));
        return m;
    }
    public override void OnStartClient(NetworkClient c) {
        base.OnStartClient(c);
        c.RegisterHandler( Msg_SceneId, (NetworkMessage msg) => { recv(recvMsg<Msg_Scene>(msg) ); });

        Debug.Log(" OnStartClient " + c);
    }
    public override void OnStartHost() {
        base.OnStartHost();
      //  c.RegisterHandler( Msg_SceneId, (NetworkMessage msg) => { recv(recvMsg<Msg_Scene>(msg) ); });

        Debug.Log(" OnStartHost " );
    }   
    public override void OnClientConnect(NetworkConnection conn) {  //we connected to someones game
         base.OnClientConnect(conn);


         //Debug.Log(" OnClientConnect " + conn);
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
