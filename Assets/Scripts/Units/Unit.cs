﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;


public class GizmoFeedBack {
    public struct Sphere {
        public Vector3 At;
        public float Radius;
        public Color Col;
    };
    public struct Line {
        public Vector3 P1, P2;
        public Color Col;
    };
    List<Sphere> Spheres = new List<Sphere>();
    List<Line> Lines = new List<Line>();

    public void sphere(Vector3 a, float r, Color c) {
        var s = new Sphere();
        s.At = a;
        s.Radius = r;
        s.Col = c;
        Spheres.Add(s);
    }
    public void line(Vector3 a, Vector3 b, Color c) {
        var s = new Line();
        s.P1 = a; s.P2 = b;
        s.Col = c;
        Lines.Add(s);
    }
    public void draw() {

        foreach(var s in Spheres) {
            Gizmos.color = s.Col;
            Gizmos.DrawWireSphere(s.At, s.Radius);
        }
        foreach(var s in Lines) {
            Gizmos.color = s.Col;
            Gizmos.DrawLine(s.P1, s.P2);
        }
    }
    public void reset() {
        Spheres.Clear();
        Lines.Clear();
    }

};
public class Unit : NetBehaviour {
  
    [HideInInspector]
    public Transform Trnsfrm;

    public float Vision = 10;
    public bool IsHighAsFuckPal = false;

    public GameObject VisDat;

    public List<Transform> HitTargets;  //todo - very optimisable..


    public Team Tm;

    public int SquidCost = 25;

    public float RoughRadius = 1;
   
    virtual protected float calcEngageRange( Unit target ) {
        if(Trgtn != null)
            return Trgtn.calcEngageRange(target);
        return RoughRadius * 1.5f;
    }
  
    protected Targeting Trgtn;
    protected void Awake() {
        Trnsfrm = transform;
        Trgtn = GetComponent<Targeting>();  //could be null...


    }
    protected void Start() {
       

    }

    public void fixColliders() {

        var l = Tm.Layer; if((this as Helio) != null) l += Team.TeamC;
        // Debug.Log("helio? " + l );
        foreach(var c in GetComponentsInChildren<Collider2D>()) {
            if(c.gameObject.layer == 0)
                c.gameObject.layer = l;

        }

        foreach(var c in GetComponentsInChildren<Collider>()) {
            c.gameObject.layer = l;
        } 
    }
    public void init(Team t) {

        Tm = t;

        if(Tm.IsLocalTeam && FowTest.get() != null )
            FowTest.get().register(this);

        fixColliders();
        foreach(var c in GetComponents<SpeedBoost>()) { //todo base class
            c.enabled = true;
        }
    
    }

    //public bool InVisionList;
    protected void OnEnable() {
        NetMan.UnitCount++;
        if(Trgtn != null) Trgtn.enabled = true;
    }

    protected void OnDisable() {
        NetMan.UnitCount--;
        if(Trgtn != null) Trgtn.enabled = false;
    }


    protected Color Col;
    public void fixCol( Color c ) {
        Col = c;
        fixCol_In(VisDat, c);
    }


    public static void fixCol_In( GameObject go,  Color c) {

        foreach(var mr in go.GetComponentsInChildren<Renderer>()) {
            foreach(Material mat in mr.materials) {
                // Debug.Log("fix?");
                if(mat.name == Sys.get().BaseColMat.name + " (Instance)") {
                    //  Debug.Log("fix!!!!!????");

                    mat.color = c;
                }
            }
        }
    }


    public float Dodge = 0.0f;
    public float MaxHealth = 1337.0f;
    public float Armor = 3;

    [SyncVar] //todo - lazy 
    protected float Health = 1;
    public bool Invinciblity = false;  //for very subtle cheating...


    public Slider HealthBar;
   // public Transform Canvas;

    protected void Update() {

        HealthBar.value = 1 - Health; //todo move to Slectable
    }
    public void damage(float dmg, float ap) {
        float effArmor = Mathf.Max(0, Armor - ap) * (1 + Random.Range(0.0f, 0.5f));

        float reduction = 0.025f+ 1.95f/ (1.0f + Mathf.Exp(  effArmor *0.5f ) );
        dmg *= reduction;

        Health -= (  dmg) / MaxHealth;

       // Debug.Log("dmg " + dmg + "  reduction = " + reduction + "  effArmor = " + effArmor + "  Health = " + (Health * MaxHealth));

        HealthBar.value = 1- Health;

        if(Invinciblity) Health = Mathf.Max(Health, 0.01f);

        if(Health < 0) {
            if(Trgtn != null) {

                foreach(var t in Trgtn.Turrets)
                    Destroy(t);
                Destroy(Trgtn);
            }
           // foreach(var t in HitTargets)
            //    Destroy(t.gameObject);
            foreach(var c in GetComponentsInChildren<Collider2D>()) {
                Destroy(c);
            }

            foreach(var c in VisDat.GetComponentsInChildren<Collider>()) {
                c.gameObject.layer = 0;
            }



            die();
          
            
        }
    }

    [System.Serializable]
    public class DeathComponent {
        public GameObject Go;
        public float Chance = 0.2f;
        public float Mass = 0.1f;

    };
    public List<DeathComponent> DeathParts;
    public GameObject DeathStuff;

    protected void die() {

        var sel = GetComponent<Selectable>();
        if( sel.Projector ) Destroy(sel.Projector.gameObject);
        Destroy(sel.LocalUI.gameObject);
        Destroy(sel);

        var dh = gameObject.AddComponent<Death_Hlpr>();

        dh.C = dh.C2 = Col;
        dh.C2 *= 0.1f;
        dh.C2.a = 1;

        float mass = 1;
        var vel = Vector2.zero;
        var angVel = 0.0f;

        var bdy = GetComponent<Rigidbody2D>();
        if(bdy) {
            mass = bdy.mass;
            vel = bdy.velocity;
            angVel = bdy.angularVelocity;
            DestroyImmediate(bdy);
        }
        foreach(var c in GetComponentsInChildren<MeshCollider>())
            if(!c.convex) {
                c.convex = true;
                Debug.Log("*ERR* -- fixing mesh collider..." + name);
            }

        //enabled = false;

        var rb = gameObject.AddComponent<Rigidbody>();

        Debug.Log(" vel " + vel);
        rb.mass = mass;
        rb.velocity = vel * 1.5f;
        rb.angularVelocity = new Vector3(0, 0, angVel);

        var off = Random.onUnitSphere * Random.Range(0.5f, 2.0f) * RoughRadius;
        off.Scale(new Vector3(1, 0.2f, 1));
        // rb.angularDrag =
        rb.AddForceAtPosition((Random.onUnitSphere + Vector3.forward) * Random.Range(0.5f, 1.0f) * 30.0f * (1.0f + mass * 0.5f), rb.position + off);
        rb.AddRelativeForce(off * (1.0f + mass * 0.5f));

        foreach(Collider c in VisDat.GetComponentsInChildren<Collider>())
            c.enabled = true;

        dh.Parts.Add(Trnsfrm);
        foreach(var dp in DeathParts) {
            if(Random.value > dp.Chance) continue;
            var rb2 = dp.Go.AddComponent<Rigidbody>();
            dh.Parts.Add(dp.Go.transform);
            rb2.mass = dp.Mass;
            rb2.velocity = rb.GetPointVelocity(dp.Go.transform.position);
            rb2.angularVelocity = rb.angularVelocity;


        }
        if(DeathStuff) {
            DeathStuff.SetActive(true);
            Unit.fixCol_In(DeathStuff, Col);
        }

        Destroy(this);
    }



    protected void OnDrawGizmos() {
        Trnsfrm = transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Trnsfrm.position, RoughRadius);
        return;
       /* Gizmos.color = Color.black;
        if(SyncO != null) 
            Gizmos.DrawLine(Trnsfrm.position, SyncO.Trnsfrm.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Trnsfrm.position, OffsetSyncMax);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Trnsfrm.position, RoughRadius);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(Trnsfrm.position, PathRadius); */
    }



}
