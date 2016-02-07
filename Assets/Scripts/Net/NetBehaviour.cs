using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NetBehaviour : NetworkBehaviour {

    public virtual void appendForSync( List<string> a ) {
        Debug.LogError("not implemented  " + name);
    }
}
