using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Sapper : Vehicle {

    public TurretCtorMenu Task;
    public int BuildSelection = -1;

    public BuildingSite TargetSite = null;


   // [ServerCallback]
    new void Update() {
        base.Update();

        float buildRange = RoughRadius * 2.5f;
        if( isServer && TargetSite != null) {

            if(TargetSite.Structure == null) {
                if(BuildSelection != -1 && ((Vector2)TargetSite.DropPoint.position - Body.position).sqrMagnitude < Util.pow2(buildRange)) {

                    TargetSite.create(BuildSelection, Tm.Layer - Team.Team1i, Owner.ColI);

                    Destroy(gameObject);  //todo animate destruction (construction)
                }
            } else if(BuildSelection == TargetSite.Structure.Ind && TargetSite.Structure.Tm == Tm ) {

                Build_Hlpr bh = TargetSite.Structure.GetComponent<Build_Hlpr>();
                if(bh != null && bh.Recv < bh.Cost && ((Vector2)TargetSite.DropPoint.position - Body.position).sqrMagnitude < Util.pow2(buildRange)) {

                    bh.Rpc_add();
                    Destroy(gameObject);  //todo animate destruction (construction)
                }

            }
        }
    }

}
