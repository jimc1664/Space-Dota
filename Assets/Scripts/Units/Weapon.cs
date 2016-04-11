using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

    public GameObject FireingAnim;

    protected Transform Trnsfrm;
    [HideInInspector]
    public Targeting Trgtn;

    public float Range = 3, RoF = 1, Dmg = 500, AP = 0;

    protected float RofTimer = -5;

    public int MyInd = 255;
    public Unit Target;

    // [HideInInspector]
    public float EngageRange;

    public virtual void getSubTarget() {


    }
   void Awake() {
        Trnsfrm = transform;
        // Trgtn = GetComponentInParent<Targeting>();
    }
    void OnEnable() {
        if(Trgtn == null) {
            enabled = false;
            return;
        }
        RofTimer = Time.time;
        EngageRange = Range - ((Vector2)Trnsfrm.position - (Vector2)Trgtn.U.Trnsfrm.position).magnitude * 1.1f;  //world space cos lazy
    }
    void OnDrawGizmos() {
        var t = transform;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(t.position, Range);

    }
}
