using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Sapper : MonoBehaviour
    {

    public TurretCtorMenu Task;
    public int BuildSelection = -1;

    public BuildingSite TargetSite = null;

    [HideInInspector]
    public Unit_Kinematic U;
    void OnEnable() {
        U = GetComponent<Unit_Kinematic>(); 
    }
   // [ServerCallback]
    void Update() {
    //    base.Update();

        float buildRange = U.RoughRadius * 1.5f +1.2f;
        if(U.isServer && TargetSite != null) {

            if(TargetSite.Structure == null) {
                if(BuildSelection != -1 && ((Vector2)TargetSite.DropPoint.position - U.Body.position).sqrMagnitude < Util.pow2(buildRange)) {

                    TargetSite.create(BuildSelection, U.Tm.Layer - Team.Team1i, U.Owner.ColI);

                    Destroy(gameObject);  //todo animate destruction (construction)
                }
            } else if(BuildSelection == TargetSite.Structure.Ind && TargetSite.Structure.Tm == U.Tm ) {

                Build_Hlpr bh = TargetSite.Structure.GetComponent<Build_Hlpr>();
                if(bh != null && bh.Recv < bh.Cost && ((Vector2)TargetSite.DropPoint.position - U.Body.position).sqrMagnitude < Util.pow2(buildRange)) {

                    bh.Rpc_add();
                    Destroy(gameObject);  //todo animate destruction (construction)
                }

            }
        }
    }

}
