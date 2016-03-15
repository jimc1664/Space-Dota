using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Build_Hlpr : NetBehaviour {


    public float BuildTimer;
    public int Cost;

    public int Recv = 1;
    public GameObject Scaffolding;

    public Animator Anim;

    Unit_Structure U;


    void OnEnable() {
        //if(TmI == -1) gameObject.SetActive(false);
       // Debug.Log("enabled...");
        U = GetComponent<Unit_Structure>();
        Anim = GetComponent<Animator>();
    }
    [ClientRpc]
    public void Rpc_init( int tsI, int tmI, int ci, int fabI, int complete ) {

        var tp = Sys.get().Site[tsI];
        var t = transform;
        t.parent = tp.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        if(complete != 0) Recv = Cost;

        U.Site = tp;
        tp.Structure = U;
        U.Ind = fabI;
        
        Anim.enabled = true;
        var clip = Anim.runtimeAnimatorController.animationClips[0];
        Anim.speed = clip.length / BuildTimer;
        //clip.apparentSpeed = 

        //Anim.clip.sped
        //Anim.clip.length
        
        U.init(Sys.get().Teams[tmI]);
        U.fixCol( U.C = U.Tm.ColorPool[ci] );

        if(Recv >= Cost) Anim.SetTrigger("Finished");
    }
    [ClientRpc]
    public void Rpc_add() {
        Recv++;
        if(Recv >= Cost) Anim.SetTrigger("Finished");
    }
    bool Built = false;
    public void anim_built() {
        Built = true;
        U.VisDat2.SetActive(true);
    }
    public void anim_start() {
        if(!Built) return;
        U.enabled = true;
        Destroy(this);
        Destroy(Scaffolding);
        Destroy(Anim);
    }

    public void anim_builtFull() {
        Built = true;
        anim_start();
    }
}
