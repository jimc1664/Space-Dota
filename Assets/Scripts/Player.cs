    using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Player : NetBehaviour {


    //[SyncVar]  ///lazy!
    public Carrier Car;

    /*
    static Player Singleton;
    public static Player get() {
#if UNITY_EDITOR
        if(Singleton == null) Singleton = FindObjectOfType<Player>();
#endif //UNITY_EDITOR
        return Singleton;
    } */


    void Awake() {
      //  if(Singleton != null && Singleton != this) Debug.LogError("Singleton violation");
      //  Singleton = this;

        
    }

    [ServerCallback]
    void Start() {


        Sys s = Sys.get();

        int ti = Random.Range(0, s.Teams.Count );
        for(int i = 0; i < s.Teams.Count; i++)
            if(s.Teams[i].Members.Count < s.Teams[ti].Members.Count)
                ti = i;
        //ti  = 1;
        Team t = s.Teams[ti];
        int colI = (t.ColPoolI++)%t.ColorPool.Count;
        Col = t.ColorPool[colI];

        Rpc_init((byte)(uint)ti, (byte)(uint)colI );

       
        return;


    }

    public void spawn() {
        var s = Sys.get();
        s.startGame();

        //  Debug.Log("wtf");
        Vector3 sp = Tm.SpawnLoc.position + (Vector3)Random.insideUnitCircle.normalized; sp.z = 0;
        GameObject c = (GameObject)Instantiate( s.Carriers[CarrierSelection], sp, Tm.SpawnLoc.rotation);
        //   c.GetComponent<Carrier>().init(this);
        NetworkServer.SpawnWithClientAuthority( c, connectionToClient );
        c.GetComponent<Carrier>().Rpc_init(this.gameObject);
    }

    public int ColI;
    public Color Col;
  //  public Material Mat;
    public Team Tm;

    
    public LayerMask SelectionMask;  //AllyMask


    //todo - sync these properly every now and then
    public int Pop, MaxPop = 80;
    public float MaxSquids = 0;
    [SyncVar]  //fuck it
    public float Squids = 50, UnrefSquids = 0;


   
    public void init(byte teamI, byte colI) {
        Tm = Sys.get().Teams[teamI];
        Col = Tm.ColorPool[ColI=colI];
        Tm.Members.Add(this);


        SelectionMask = (((1 << Team.TeamC * 2) - 1) << Team.Team1i) | 1; 

        if(isLocalPlayer) {
            Tm.IsLocalTeam = true;
            var sui = Sys.get().StartUI;
            sui.SetActive(true);
            sui.GetComponentInChildren<UnityEngine.UI.Image>().color = Col;
        }
    }
     
    [ClientRpc]
    void Rpc_init(byte teamI, byte colI) { init(teamI, colI); }

    [SyncVar]
    public int CarrierSelection = -1;
    [Command]
    void Cmd_setCarrierSelection(int cs) {
        if(Sys.get().Started == false) 
            CarrierSelection = cs; 
    }

    Selectable Highlighted = null;
    void Update() {



        var sys = Sys.get();


        if(!sys.Started) {
            if(!isLocalPlayer) return;
           // if( CarrierSelection!= -1 ) //move this
          //      sys.CarrierSpecUI[CarrierSelection].SetActive(true);

            for(int i = 0; i < sys.Carriers.Count; i++) {
                if(Input.GetKeyUp(KeyCode.Alpha1 + i)) {
                    Cmd_setCarrierSelection(i);
                }
            }
            return;
        }


        if (!isLocalPlayer)  return;


        Camera.main.GetComponent<BoxSelector>().CheckCamera(); //messy

        sys.SquidUI.text = "" + Mathf.FloorToInt(Squids);
        sys.Squid_Unref_UI.text = "" + Mathf.FloorToInt(UnrefSquids);
        sys.Squid_Cap_UI.text = "" + ( MaxSquids -Mathf.FloorToInt(UnrefSquids) - Mathf.FloorToInt(Squids));

        sys.PopUI.text = "" + Pop +" / "+MaxPop;

        RaycastHit hit;
        Selectable nHl = null;
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, SelectionMask )) {
          
            nHl = hit.collider.gameObject.GetComponentInParent<Selectable>();
         //   Debug.Log(" hit " + hit.collider.name + "   " + nHl);
        }

        if(nHl != Highlighted ) {
            if(Highlighted != null ) Highlighted.Highlighted = false;
            if(nHl != null) nHl.Highlighted = true;
            Highlighted = nHl;
        }

        if(Input.GetMouseButtonUp(0)) {
            
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool bs = Mathf.Abs(BoxSelector.selection.width * BoxSelector.selection.height) > 8;
            foreach(var s in FindObjectsOfType<Selectable>()) {
                if(s.selected && (!shift || s.getOwner() != this)) s.selected = false;

                if(bs && s.getOwner() == this) {
                    Vector3 camPos = Camera.main.WorldToScreenPoint(s.U.VisDat.transform.position); //Transposes 3D Vector into 2D Screen Space (Unproject)
                    camPos.y = BoxSelector.ScreenToRectSpace(camPos.y);  //more efficent to transform rect to screen space  (not that it makes any real difference) 
                    s.selected |= BoxSelector.selection.Contains(camPos);  //todo radius
                }
            }

            if(!bs && Highlighted != null) {
                if(Highlighted.getOwner() == this || !shift) //todo --- or have nothing selected
                    Highlighted.selected = !Highlighted.selected;
            }
            BoxSelector.selection = new Rect(Input.mousePosition.x, BoxSelector.ScreenToRectSpace(Input.mousePosition.y), 0, 0);
        }

        //if(Selected == null) return;
        if(Input.GetMouseButtonUp(1)) {
            //RaycastHit hit;
            bool commanded = tryAttack_Command()
                || trySapperTask_Command()
                || tryMove_Command();
        }

    }

    bool tryMove_Command() {
        RaycastHit hit;
        int lm = 1 << LayerMask.NameToLayer("Map");
        if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, lm )) {
            //Debug.Log("move? " + hit.collider.gameObject + "    " + hit.point);
            //todo -- this is unbelievably wrong

            Vector2 cp = hit.point;
            var n = FindObjectOfType<NavMesh>().findNode(cp);

            Vector2 yAx = Vector2.up;
            Vector2 xAx = Vector2.right;
            float rad = 1.1f;

            int r = 0, c = 0, cm = 1, rm = 0;
            bool fr = false;

            Selectable cs = null;
            if(Car != null && n != null) {
                cs = Car.GetComponent<Selectable>();
                if(cs.selected == true) {
                    c = 2; cm = 2; rm = 1;
                    Cmd_moveUnit(Car.gameObject, cp);
                }
            }

            int maxAttempt = 50;
            //                List<Selectable> selection = new List<Selectable>();
            foreach(var s in FindObjectsOfType<Selectable>()) {
                if(!s.selected || s.getOwner() != this) continue;

                bool isHeli = (s.U as Helio) != null;
                if(n == null && !isHeli) continue;
                if(cs == s) continue;

                maxAttempt += 10;
                for(; maxAttempt-- > 0; ) {
                    var off = c * xAx + r * yAx;
                    var p = cp + off * rad;

                    if(c > 0) c = -c;
                    else if(-c == cm) {
                        if(r > 0) {
                            if(fr) c = 0;
                            else c = -c;
                            r = -r;
                        } else if(-r == rm) {
                            if(rm >= cm) {
                                c = ++cm;
                                r = 0;
                                fr = false;
                            } else {
                                r = ++rm;
                                c = 0;
                                fr = true;
                            }
                        } else {
                            r = -r + 1;
                            c = -c;
                        }
                    } else c = -c + 1;

                    if(isHeli || Physics2D.OverlapCircle(p, rad * 0.8f, lm) == null) {
                        clearSapTask(s);
                        Cmd_moveUnit(s.gameObject, p);
                        break;
                    }
                }

            }
            return true;
        }
        return false;
    }
    bool tryAttack_Command() {

        if(Highlighted != null && (Tm.EnemyMask.value & (1 << Highlighted.U.Tm.Layer)) != 0) {
              
            foreach(var s in FindObjectsOfType<Selectable>()) {
                if(s.selected && s.getOwner() == this)
                    Cmd_attackUnit(s.gameObject, Highlighted.gameObject );
            }
            return true;
        }
        return false;
    }
    bool trySapperTask_Command() {

        bool sapper = false;
        foreach( var s in FindObjectsOfType<Sapper>() ) {///todo - i hate this...
            if(s.Owner == this && s.GetComponent<Selectable>().selected) {
                sapper = true;
                break;
            }
        }
        if(!sapper ) return false;
         RaycastHit hit;
        if( !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, float.MaxValue, 1 << LayerMask.NameToLayer("TurretSpindle") )) return false;
        var ts = hit.collider.GetComponentInParent<BuildingSite>();
        if(ts ==null ) return false;
        TurretCtorMenu task = null;
        int bs = -1;
        if( ts.Structure == null ) {        
            var dp = ts.DropPoint;
            var p = dp.position;
            var sys = Sys.get();
            var t = Instantiate(sys.TurretCtorDialog).transform;
            t.parent = ts.transform;
            t.localPosition = Vector3.zero;
            task = t.GetComponent<TurretCtorMenu>();
            task.Spindle = ts;
        } else {
            var bh = ts.Structure.GetComponent<Build_Hlpr>();

            if(bh == null || bh.Cost <= bh.Recv )  return false;

            bs = ts.Structure.Ind;
        }
       // Debug.Log("1111111111");
        foreach(var s in FindObjectsOfType<Selectable>()) {
            if(!s.selected || s.getOwner() != this) continue;
            var sap = s.U as Sapper;
            if(sap == null) continue;
            sap.Task = task;
            Cmd_taskSapper( s.gameObject, ts.Index, bs );
        }
        return true;
    }

    void clearSapTask( Selectable s ) {
        var sap = s.U as Sapper;
        if( sap != null ) {
            sap.Task = null;
            sap.BuildSelection = -1;
            sap.TargetSite = null;
        }
    }

    [Command]
    public void Cmd_moveUnit(GameObject uo, Vector2 p) {
        if(uo == null) return;
        var u = uo.GetComponent<Unit_Kinematic>();
        if(u == null) return;
        u.Rpc_DesPos(p);
        //  Debug.Log("move unit " + u.name + "  to - " + p);
    }

    [Command]
    public void Cmd_taskSapper(GameObject uo, int tsi, int bs ) {
        if(uo == null) return;
        var sap = uo.GetComponent<Sapper>();
        if(sap == null || (uint)tsi >= (uint)Sys.get().Site.Count ) return;

        sap.BuildSelection = bs;
        sap.TargetSite = Sys.get().Site[tsi];
        sap.Rpc_DesPos(sap.TargetSite.DropPoint.position);
    }

    [Command]
    public void Cmd_attackUnit(GameObject uo, GameObject trgt ) {
        if(uo == null || trgt == null ) return;
        var u = uo.GetComponent<Unit_Kinematic>();
        if(u == null || trgt.GetComponent<Unit>() == null) return;
        u.Rpc_attackUnit(trgt);
    }


    [Command]
    public void Cmd_createFrom(byte i, GameObject muhCarrier ) {
        if(muhCarrier == null) return;  // it died mayhaps?
        var c = muhCarrier.GetComponent<Carrier>();
        if(c== null || c.Owner != this) return; //todo - zomg cheater .. ban-hammer  or possibly error..
      //  Debug.Log("Cmd_createFrom");
        var sd = c.SpawnDat[i];
        var ud = sd.Fab.GetComponent<Unit_Kinematic>();
        if(ud.PopCost + Pop > MaxPop) return; //todo...feed back 
        if(ud.SquidCost > Mathf.FloorToInt(Squids) ) return; //todo...feed back 

        int spi = c.getSpawnPointI(i);
        if(spi < 0) return;

        GameObject go = (GameObject)Instantiate(sd.Fab, Vector3.zero, Quaternion.identity);

        NetworkServer.Spawn(go);
        //todo -- SpawnPoints.Count  >= 256 == err
        Squids -= (float)ud.SquidCost;
        Pop += ud.PopCost;

        var ush = go.GetComponent<UnitSpawn_Hlpr>();
        c.SpawnPoints[spi].CurSpwn = ush;
        ush.Rpc_init(muhCarrier, (byte)spi, Squids, UnrefSquids  );
    }
}
