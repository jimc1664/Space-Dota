using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// 
/// The working of this systemis properly documented!?! 
/// See Subsystem::AI https://docs.google.com/document/d/1XEvnsBw4qZ5qhLcU9MuaNjbGF6QDe_1SDpUKnjmqk3k/edit#heading=h.erzg9879h8e2
/// 
/// 
/// Ear clipping was done from memory but this is where I learnt it so should resemble
/// http://www.geometrictools.com/Documentation/TriangulationByEarClipping.pdf
/// 
/// A* from memory.. this looks like good site though - what I would use if I was to refine 
/// http://theory.stanford.edu/~amitp/GameProgramming/AStarComparison.html
/// 
/// Relies on numerous math helpers which I did not do from memory - see Util.cs for details
/// 
/// - Jim
/// </summary>

public class NavMesh : MonoBehaviour {


    // 'interface' 
    public bool ReGen, Constant, BuildOuter; 


    //? Old debug stuff
    public int DebugI = 0;
    public bool Inc = false, Dec = false;
   // int Tri;
   // public Transform Obj1, Obj2;
    void drawTri(Vector2 a, Vector2 b, Vector2 c) {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(c, b);
        Gizmos.DrawLine(a, c);
    }

    void drawCross( Vector2 p ) {
        Gizmos.DrawLine(p - Vector2.right, p + Vector2.right);
        Gizmos.DrawLine(p - Vector2.up, p + Vector2.up);
    }
    void debugDrawCross( Vector2 p, Color c ) {
        Debug.DrawLine(p - Vector2.right, p + Vector2.right, c);
        Debug.DrawLine(p - Vector2.up, p + Vector2.up, c);
    }

   // [SerializeField]
    List<Vector2> Verts = new List<Vector2>();


    //a POLYGON of the NavMesh
  //  [System.Serializable]
    public class Node {

        public Node(TNode tn) {
            var vc = tn.Count;
            Vi = new int[vc];
            Nbrs = new Node[vc];
            Edge = new Vector2[vc];
            Tris = new Tri[vc-2];


            for(int i = tn.Count; i-- > 0; ) {
                var e = tn[i];
                Vi[i] = e.Vi;
                //Nbrs[i] = e.Nbr;
                Edge[i] = e.Edge;
            }
        }

        public void linkTri(Tri t) {
            int i = 0;
            while(Tris[i] != null ) i++;
            Tris[i] = t;
        }

        public void linkNbrs(TNode tn) {
            for(int i = tn.Count; i-- > 0; ) {
                var e = tn[i];
                if( e.Nbr != null )
                    Nbrs[i] = e.Nbr.Nd;
            }
        }

        public int[] Vi;// = new int[3];
        public Node[] Nbrs;// = new Node[3];
        public Vector2[] Edge;// = new Vector2[3];  // todo -- clear optimisation
        public Tri[] Tris;
        //public Vector2 Centre;

        public SearchNode SN;

        /*
        public int a { get { return Vi[0]; } set { Vi[0] = value; } }
        public int b { get { return Vi[1]; } set { Vi[1] = value; } }
        public int c { get { return Vi[2]; } set { Vi[2] = value; } }
         */

    };
 //   [SerializeField]
    List<Node> Nodes = new List<Node>();


    public class TNode : List<EdgeRef> {
        public TNode() { }
        public TNode(TNode o) {
            AddRange(o);
        }
        public Node Nd;
    };
    public class Tri {
        public int[] Vi = new int[3];
        public Tri[] Nbrs = new Tri[3];
        public Vector2[] Edge = new Vector2[3];

        public Vector2 Centre;  //for debug only me thinks 

        public Node Nd;
        public TNode _TNode;
        public int a { get { return Vi[0]; } set { Vi[0] = value; } }
        public int b { get { return Vi[1]; } set { Vi[1] = value; } }
        public int c { get { return Vi[2]; } set { Vi[2] = value; } }
    };
    List<Tri> Tris = new List<Tri>();

    public struct EdgeRef {
        public Tri T;
        public int I;

        public int Vi { get { return T.Vi[I]; } set { }  }
        public Tri Nbr { get { return T.Nbrs[I]; } private set { } }
        public Vector2 Edge { get { return T.Edge[I]; } private set { } }
        public int NbrId { get { if(Nbr != null) return Nbr.GetHashCode(); else return 0; } private set { } }
    };


    void addTri(int a, int b, int c) {

        var t = new Tri();
        t.Vi[0] = a;
        t.Vi[1] = b;
        t.Vi[2] = c;

        t.Centre = (Verts[t.a] + Verts[t.b] + Verts[t.c]) * (1.0f / 3.0f);

        t.Edge[0] = (Verts[a] + Verts[c]) * 0.5f;
        t.Edge[1] = (Verts[b] + Verts[a]) * 0.5f;
        t.Edge[2] = (Verts[c] + Verts[b]) * 0.5f;

        for(int i = Tris.Count; i-- > 0; ) {
            var o = Tris[i];

            for(int j1 = 0, j2 = 2; j1 < 3; j2 = j1++) {
                int v1 = t.Vi[j1], v2 =t.Vi[j2];
                for(int k1 = 3, k2 = 0; k1-- > 0; k2 = k1) {
                    if(o.Vi[k1] != v1 || o.Vi[k2] != v2) continue;

                    t.Nbrs[j1] = o;
                    if(o.Nbrs[k2] != null) Debug.LogError("err");
                    o.Nbrs[k2] = t;

                    goto label_doubleBreak;
                }
            }

            label_doubleBreak: ;
        }


        Tris.Add(t);
    }


    //generates the navmesh  -   == false  means it failed somehow...  likely incorrect input - possible bug
    bool gen() {  

        //outer boundary
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();
        Transform trns = transform;
        /*for( int i = col.points.GetLength(0), j = 0; i-- > 0; j = i  ) {
            Gizmos.DrawLine(trns.TransformPoint( col.points[i]) , trns.TransformPoint( col.points[j]));
        }
        */

        Tris = new List<Tri>();
        Nodes = new List<Node>();
        Verts = new List<Vector2>(col.points.GetLength(0));
        List<int> vi = new List<int>(col.points.GetLength(0));

        for(int i = 0; i < col.points.GetLength(0); i++) {
            vi.Add(Verts.Count);
            Verts.Add(trns.TransformPoint(col.points[i]));
        }

        List<List<int>> islands = new List<List<int>>();


		//obstacles - subtract thse from outline poly gon - overlaps not handled
		var gos = GameObject.FindGameObjectsWithTag("NavMesh");

		foreach (GameObject go in gos) 	{
			foreach (var subCol in go.GetComponents<PolygonCollider2D>()) {
				trns = go.transform;

				List<int> ni = new List<int>(subCol.points.GetLength(0));

				for (int i = 0; i < subCol.points.GetLength(0); i++) {
					ni.Add(Verts.Count);
					Verts.Add(trns.TransformPoint(subCol.points[i]));
				}
				// for(int i = col.points.GetLength( 0); i-- > 0; ) ni.Add(trns.TransformPoint(col.points[i]));

				islands.Add(ni);
			}
		}


        //this loop runs through all the islands (obstacles) and adds them in keyhole fashion to the current polygon 
        //  - adds two edges to and from a vert on the island from an outer vert
        for( ; islands.Count > 0; ) {
            bool change = true;
            for(int islI = islands.Count; islI-- > 0; ) {
                List<int> ni = islands[islI];
                // Debug.Log("vi.c " + vi.Count);
                for(int i = vi.Count, pi = 0; i-- > 0; pi = i) {

                    int i2 = i - 1; if(i2 < 0) i2 += vi.Count;
                    int ai = vi[i];
                    Vector2 a = Verts[ai], c = Verts[vi[pi]], d = Verts[vi[i2]];
                    for(int j = ni.Count; j-- > 0; ) {
                        var bi = ni[j];
                        var b = Verts[bi];
                        if(Util.sign(b, a, d) < 0) continue;
                        if(Util.sign(c, a, b) < 0) continue;

                        for(int k = ni.Count, l = 0; k-- > 0; l = k) {
                            if(ni[l] == bi || ni[k] == bi) continue;
                            if(Util.lineLineIntersect(a, b, Verts[ni[l]], Verts[ni[k]]))
                                goto label_breakContinue1;
                        }

                        foreach(List<int> ii in islands) {
                            if(ii == ni) {
                                // Debug.Log("skip");
                                continue;
                            }
                            for(int k = ii.Count, l = 0; k-- > 0; l = k) {
                                if(Util.lineLineIntersect(a, b, Verts[ii[l]], Verts[ii[k]]))
                                    goto label_breakContinue1;
                            }
                        }

                        for(int k = vi.Count, l = 0; k-- > 0; l = k) {
                            // if(l == i || k == i) continue;
                            if(vi[l] == ai || vi[k] == ai) continue;
                            if(Util.lineLineIntersect(a, b, Verts[vi[l]], Verts[vi[k]]))
                                goto label_breakContinue1;
                        }

                        List<int> oi = vi;
                        vi = new List<int>(oi.Count + ni.Count + 2);

                        for(int l = 0; l <= i; l++) vi.Add(oi[l]);
                        for(int l = j; ; ) {
                            vi.Add(ni[l]);
                            if(l-- == 0) l = ni.Count - 1;

                            if(l == j) {
                                vi.Add(ni[l]);
                                break;
                            }
                        }
                        for(int l = i; ; ) {
                            vi.Add(oi[l]);
                            if(++l == oi.Count) break;
                            //  if( l == i ) break;
                        }

                        //  Debug.Log( "a  "+vi.Count+"  b "+(oi.Count+ni.Count+2));
                       // Debug.DrawLine(a, b, Color.black);


                        // I like to use goto's and label's as composite breaks / continues for nested loops 
                        //  aslong as you only jump forward and out there is no problems 
                        //  yes - i could do the same by flags or  function-ify-ing the inner loop - i think this is neater and eaier to read
                        goto label_jumpout;
                        label_breakContinue1: ;
                    }
                }
                //Debug.Log("fail");
                continue;
                label_jumpout: ;

                islands.RemoveAt(islI);
                change = true;
                //*/
            }
            if(change == false) {
                Debug.Log("Critical nav mesh gen Error!!");
                break;
            }
        }

        islands = null;

        // Gizmos.color = new Color(0, 0, 1, 0.7f);
        /*for(int i = vi.Count, j = 0; i-- > 0; j = i) {
            Gizmos.DrawLine(Verts[vi[i]], Verts[vi[j]]);
        } */

        List<int> ovi = new List<int>(vi);

        //Gizmos.color = new Color(0, 0, 1, 0.7f);

        for(int i = Verts.Count; --i > 0; ) {
            for(int j = i; j-- > 0; ) {
                var d = (Verts[i] - Verts[j]).sqrMagnitude;
                if(d < 0.5f) {

                    Debug.Log("d " + d);
                    debugDrawCross(Verts[i], Color.black);
                    Debug.DrawLine(Verts[i], Verts[j]);
                }
            }
        }


        //this is the actual ear clipping - we go round the edge and remove any ears we come across and add them to the NavMesh
        //  - when an ear is created it creates an internal edge - we keep track of this for easy adjacancy data 
        // - unoptimal  - not a priority
        for(int iter = 1; iter-- > 0; )
            for(int i = vi.Count, j = 0, k = 1; i-- > 0; k = j, j = i) {

                int ia = vi[i], ib = vi[j], ic = vi[k];
                Vector2 a = Verts[ia], b = Verts[ib], c = Verts[ic];
                /*
                if(++Tri == DebugI) {
                    Gizmos.color = new Color(0, 0, 0, 1.0f);
                    drawTri(a, b, c);
                    drawCross(b);


                   // Debug.Log(" strt  i " + i + "  j " + j + "  k " + k + " ia " + ia + " ib " + ib + " ic " + ic + "  tri " + Tri + "  iter " + iter);
                    string str = "cnt " + vi.Count + "  list";
                    for(int l = 0; l < vi.Count; l++)
                        str += "  " + vi[l];
                  //  Debug.Log(str);
                    // Tri--;
                } //*/
                if(Vector3.Cross(c - b, a - b).z > 0) {

                    /*if(DebugI == Tri) {
                        Gizmos.color = new Color(1, 1, 0, 1.0f);
                        drawTri(a, b, c);
                    }*/
                    goto label_breakContinue2;
                }

                for(int l = Verts.Count; l-- > 0; ) {
                    if(ia == l || ib == l || ic == l) continue;
                    if(Util.pointInTriangle(Verts[l], a, b, c)) {
                        /*
                        if(DebugI == Tri) {

                            // Debug.Log(" pit " + a + "  tri " + Tri);
                            // drawCross(a);
                            Gizmos.color = new Color(0, 1, 0, 0.7f);
                            Gizmos.DrawLine(Verts[l], a);
                            Gizmos.DrawLine(Verts[l], b);
                            Gizmos.DrawLine(Verts[l], c);
                            //  Gizmos.color = new Color(0, 1, 1, 0.7f);
                            // Gizmos.DrawLine( a,c );
                            // Gizmos.DrawLine( a,b );
                            // Gizmos.DrawLine( b,c );
                        }//*/
                        goto label_breakContinue2;
                    }
                }

                for(int l = ovi.Count, m = 0; l-- > 0; m = l) {
                    if(ovi[l] == ia || ovi[l] == ic || ovi[m] == ia || ovi[m] == ic) continue;

                    if(Util.lineLineIntersect(a, c, Verts[ovi[l]], Verts[ovi[m]])) {
                        //*  
                        /*if(DebugI == Tri) {
                            Gizmos.color = new Color(1, 0, 0, 0.7f);
                            Gizmos.DrawLine(c, a);
                            Gizmos.DrawLine(Verts[ovi[l]], Verts[ovi[m]]);//* /

                            Vector2 ret = Vector2.zero;
                            if(Util.lineLineIntersection(a, c, Verts[ovi[l]], Verts[ovi[m]], ref ret))
                                drawCross(ret);
                        }*/
                        goto label_breakContinue2;
                    }

                }

                vi.RemoveAt(j);

                /*if(Tri == DebugI) {
                    Gizmos.color = Color.red;
                    for(int l = vi.Count, m = 0; l-- > 0; m = l) {
                        Gizmos.DrawLine(Verts[vi[l]], Verts[vi[m]]);
                    }
                }
                if(Tri == DebugI - 1) {
                    Gizmos.color = Color.grey;
                    for(int l = vi.Count, m = 0; l-- > 0; m = l) {
                        Gizmos.DrawLine(Verts[vi[l]], Verts[vi[m]]);
                    }
                }
                if(DebugI < 0 || Tri == DebugI) {
                    Gizmos.color = new Color(0, 0, 1, 0.7f);
                    drawTri(a, b, c);
                }*/
                addTri(ia, ib, ic);
                if(vi.Count <= 2) { iter = 0; break; }

                i--;
                j = (i + 1) % vi.Count;
                k = (i + 2) % vi.Count;

                iter = 1;
                label_breakContinue2: ;

              //  if(Tri == DebugI) return false;
            }

        if(vi.Count > 2) {
            /*Gizmos.color = Color.red;
            for(int l = vi.Count, m = 0; l-- > 0; m = l) {
                Gizmos.DrawLine(Verts[vi[l]], Verts[vi[m]]);
            }*/

            return false;
        }

        SortedList<float, TNode> search = new SortedList<float, TNode>(new DuplicateKeyComparer<float>());
        List<TNode> tNodes = new List<TNode>();
        foreach(Tri t in Tris) {
            float key = -1;
            t._TNode = new TNode();
            for(int i = 3, j = 0, k = 1; i-- > 0; k = j, j = i) {
                float dt = Mathf.Abs( Vector2.Dot( (Verts[t.Vi[k]] - Verts[t.Vi[j]]).normalized, (Verts[ t.Vi[i]] - Verts[t.Vi[j]]).normalized) );
                key = Mathf.Max(key, dt);
                EdgeRef e = new EdgeRef(); e.T = t; e.I = i;
                t._TNode.Add(e);
               // Debug.Log("sign  " + Util.sign(Verts[t.Vi[i]],Verts[t.Vi[j]], Verts[t.Vi[k]]));
            }
            tNodes.Add(t._TNode);
            search.Add(key, t._TNode);
          //  Debug.Log( "key " +tNodes.GetEnumerator().Current.Key

        }



        for(int itCnt = 0; search.Count > 0; ) {
          
            var iter = search.GetEnumerator(); iter.MoveNext();
            var k = iter.Current.Key;
            var n = iter.Current.Value;
            search.RemoveAt(0);

            if( n.Count == 0 ) continue;

            for(int i = n.Count, j = 0; i-- > 0; j = i ) {
                var e = n[i];

                if(itCnt++ == DebugI) {
                    return false;
                }


                if(itCnt == DebugI) {
                    
                    foreach(var tn in tNodes) {

                        //  Debug.Log("n ");
                        //foreach(var r in n) Debug.Log(" >> " + r.Vi);
                        for(int i3 = tn.Count, j3 = 0, k3 = 1; i3-- > 0; k3 = j3, j3 = i3) {
                            var t = tn[i3].T;

                            for(int i2 = 3; i2-- > 0; ) {
                                Debug.DrawLine(Verts[t.Vi[i2]], Verts[t.Vi[(i2 + 1) % 3]], new Color(0.0f, 0.0f, 1, 0.1f));
                            }

                            debugDrawCross(t.Centre, Color.white);
                            var col2 = Color.red;
                            if(Util.sign(Verts[tn[i3].Vi], Verts[tn[j3].Vi], Verts[tn[k3].Vi]) < 0)
                                col2 = Color.yellow;
                            Debug.DrawLine(Verts[tn[i3].Vi], Verts[tn[j3].Vi], col2);
                        }
                    }

                    Vector2 centre = Vector2.zero;
                    foreach(var r in n) centre += Verts[r.Vi];
                    centre /= n.Count;
                    foreach(var r in n) Debug.DrawLine(Verts[r.Vi], centre, Color.green);

                    var mid = (Verts[e.Vi] + Verts[n[j].Vi])*0.5f;
                    Debug.DrawLine(mid, centre, Color.black);

                    Debug.Log("---------------------------------------");
                    Debug.Log("---------------------------------------");
                    Debug.Log(" key "+k+"  pre  " + i);
                    foreach(var r in n) Debug.Log(" >> vi " + r.Vi + "  t " + r.T.GetHashCode() + "  nbr " + r.NbrId);
                }

                if(e.Nbr == null) {
                    if(itCnt == DebugI) Debug.Log("e.Nbr == null");
                    continue;
                }
                var on = e.Nbr._TNode;
                if( on.Count == 0 ) {
                    if(itCnt == DebugI) Debug.Log("on.Count == 0");
                    Debug.LogError("on ==  n");
                    continue;  //todo this should not be needed...
                }
                if(on ==  n) {
                    if(itCnt == DebugI)  Debug.Log("on ==  n");
                    Debug.LogError( "on ==  n");
                   
                    continue; // ???
                }

                if(itCnt == DebugI) {
                    Debug.Log("  --  " );
                    foreach(var r in on) Debug.Log(" >> vi " + r.Vi + "  t " + r.T.GetHashCode() + "  nbr " + r.NbrId);

                    Vector2 centre = Vector2.zero, c2 = Vector2.zero;
                    foreach(var r in n) centre += Verts[r.Vi];
                    foreach(var r in on) c2 += Verts[r.Vi];
                    centre /= n.Count; c2 /= on.Count;
                    Debug.DrawLine(c2, centre, Color.black);
                }

                var oi = on.Count -1;
                for(; oi >= 0; oi--) {
                    if(on[oi].Nbr == e.T) goto label_jumpOut_findSuccess;
                }

                if(itCnt == DebugI) {
                    Debug.LogError("failed to match..");
                }
                continue;
               
                label_jumpOut_findSuccess: ;

                var oj = oi == on.Count-1 ? 0 : oi + 1;
                var ins = oi == 0 ? on.Count - 1 : oi - 1;

                if(itCnt == DebugI) {
                    Debug.Log("oi  " + oi + "oj  " + oj + "  ins " + ins + " ins.Vi " + on[ins].Vi );
                }

                if(n[i].Vi != on[oj].Vi || n[j].Vi != on[oi].Vi)
                    Debug.LogError("mismatch");

                /*if((uint)oj >= (uint)on.Count || (uint)j >= (uint)n.Count) {
                    Debug.Log("j   " + j + "  n.Count " + n.Count + "   oj " + oj + "  on.Count   "+ on.Count );
                    return false;
                }

                if((uint)n[j].I >= (uint)n[j].T.Vi.Length) {
                    Debug.Log("j   " + j + "  n[j].I " +n[j].I + "   n[j].T.Vi.Length   "+ n[j].T.Vi.Length );
                    return false;
                }
                if((uint)on[oj].I >= (uint)on[oj].T.Vi.Length) {
                    Debug.Log("oj   " + oj + "  on[oj].I " +on[oj].I + "   on[oj].T.Vi.Length   "+ on[oj].T.Vi.Length);
                    return false;
                } */
                    /*
                if(n[j].Vi 
                    != on[oj].Vi) {
                    oi = oj;
                    j = i == 0 ? n.Count-1 : i - 1;
                }  else
                    oi = oj == 0 ? on.Count - 1 : oj - 1; */

                //if(Util.sign(Verts[n[i].Vi], Verts[ e.T.Vi[oi]], Verts[n[j].Vi]) < 0) continue;
                int fi = i;
                TNode oldN = null;
                if(on.Count > 3) {
                    oldN = new TNode(n);
                    //int endI = (oi + 2) % on.Count;

                    if(itCnt == DebugI) {
                        Debug.Log("merge   ");
                        foreach(var r in on) Debug.Log(" >> " + r.Vi);
                        Debug.Log(" into ");
                        foreach(var r in n) Debug.Log(" >> " + r.Vi);
                    }
                    for(; ; ) {
                        n.Insert(j, on[ins]);
                        if( fi > j ) fi++;
                        if(ins == 0) ins = on.Count - 1;
                        else ins--;
                        if(ins == oj) break;
                    }
                } else {
                    n.Insert(j, on[ins]);
                    if(fi > j) fi++;                    
                }
               // Debug.Log("post ");
              //  foreach(var r in n) Debug.Log(" >> " + r.Vi);


                float key = -1; int ki = -1;
                for(int i2 = n.Count, j2 = 0, k2 = 1; i2-- > 0; k2  =j2, j2 = i2) {
                    float dt = Mathf.Abs(Vector2.Dot((Verts[n[k2].Vi] - Verts[n[j2].Vi]).normalized, (Verts[n[i2].Vi] - Verts[n[j2].Vi]).normalized));
                    if(dt > key) ki = i2;
                    key = Mathf.Max( key, dt );
                    if(Util.sign(Verts[n[i2].Vi], Verts[n[j2].Vi], Verts[n[k2].Vi]) < 0) {
                        if(on.Count > 3) {
                            n.Clear();
                            n.AddRange(oldN);
                            if(itCnt == DebugI) {
                                Debug.Log("FAIL   -- copy ");
                                foreach(var r in n) Debug.Log(" >> " + r.Vi);
                                Debug.Log("-- ");
                                foreach(var r in oldN) Debug.Log(" >> " + r.Vi);
                            }
                        } else {
                            if(itCnt == DebugI) {
                                Debug.Log("FAIL " + i2);
                                foreach(var r in n) Debug.Log(" >> " + r.Vi);       
                            };
                            n.RemoveAt(j);
                        }
                        goto label_breakContinue;
                    }
                }



                if(on.Count > 3) {
                    foreach(var r in on)
                        r.T._TNode = n;

                } else e.Nbr._TNode = n;

                n[fi] = on[oj];

                on.Clear();
                
                tNodes.Remove(on);
                search.Add( key, n );

                if(itCnt == DebugI) {
                    Debug.Log("  success   nk" + key);
                    foreach(var r in n) Debug.Log(" >> vi " + r.Vi + "  t " + r.T.GetHashCode() + "  nbr " + r.NbrId);

                    int i2 = ki, j2 = i2 - 1, k2 = j2 - 1;
                    if(j2 < 0) j2 += n.Count;
                    if(k2 < 0) k2 += n.Count;

                   // Debug.DrawLine(Verts[n[i2].Vi], Verts[n[i2].Vi], Color.black);
                }
                break;
                label_breakContinue: ;
            }
            
            
           // break;
        }

        foreach(var tn in tNodes) {
            tn.Nd = new Node(tn); 
            Nodes.Add( tn.Nd );
        }
        foreach(var t in Tris ) {
            t.Nd = t._TNode.Nd;
            t.Nd.linkTri(t);
        }
        foreach(var tn in tNodes) tn.Nd.linkNbrs(tn);

        foreach(var n in tNodes) {

            for(int i = n.Count, j = 0, k = 1; i-- > 0; k  =j, j = i ) {
                var t = n[i].T;

                for(int i2 = 3; i2-- > 0; ) {
                    Debug.DrawLine( Verts[t.Vi[i2]], Verts[t.Vi[(i2+1)%3]],  new Color(0.0f, 0.0f, 1, 0.1f) );
                }

                debugDrawCross(t.Centre, Color.white);
                var col2 = Color.red;
                if(Util.sign(Verts[n[i].Vi], Verts[n[j].Vi], Verts[n[k].Vi]) < 0)
                    col2 = Color.yellow;
                Debug.DrawLine(Verts[n[i].Vi], Verts[n[j].Vi], col2);
            }   


        }
        //Debug.Log("DONE");



        return true;
    }



    //search nodes for one constaining 'p'  - spatial sorting is obvious optimisation - not priority 
    public Node findNode(Vector2 p) {

        //todo - real version..
        foreach(Tri n in Tris ) {
            if(Util.pointInTriangle(p, Verts[n.a], Verts[n.b], Verts[n.c])) return n.Nd;
        }
        return null;
    }

    public Node findNode(Vector2 p, Node n ) {
        if(n != null) {
            foreach( var t in n.Tris )
                if(Util.pointInTriangle(p, Verts[t.a], Verts[t.b], Verts[t.c])) return n;

        }
        return findNode(p);
    }


    //  stuff for A* search
    public class DuplicateKeyComparer<K> :  IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return -result;  // (-) invert 
        }
    }
    public class SearchNode {
        public Node N;
        public int NbrI;
        public Vector2 P;
        public float D;
        public SearchNode Prev;
    }

    public class SmoothNode {
        public Vector2 P, P2;
        public Vector2 E1, E2;
        //public Node N;  todo..
    }


    public class Path {
        public List<SmoothNode> Smooth;
    }


    public Path getPath( Vector2 from, Vector2 to, Node toNode ) {

        if(Nodes.Count == 0 ) return null;

        Node a = findNode(from), b = toNode;
        //Debug.Log("a  " + a+"  b  "+b );
        if(a == null || b == null || a == b ) return null;

        SortedList<float, SearchNode> search = new SortedList<float, SearchNode>(new DuplicateKeyComparer<float>());

        var cur = new SearchNode();
        cur.D = 0;
        cur.N = a;
        cur.Prev = null;
        cur.P = from;
        cur.N.SN = cur;


        float cD = float.MaxValue;
        SearchNode cP = null;
        for(; ; ) {

            var n = cur.N;
            for(int i = n.Nbrs.Length; i-- > 0; ) {
                var o = n.Nbrs[i];
                if(o == null) continue;

                if(o.SN != null) {
                    // var d = cur.D + (cur.P - sn.P).magnitude;
                    continue;
                }
               // Gizmos.DrawLine(cur.P, n.Edge[i]);

                var sn = new SearchNode();
                sn.P = n.Edge[i];
                sn.D = cur.D + (cur.P - sn.P).magnitude;
                sn.N = o;
                sn.Prev = cur;
                sn.N.SN = sn;
                sn.NbrI = i;

                var d = sn.D + (to - sn.P).magnitude;
                if(d > cD) continue;

                if(o == b) {
                    cD = d;
                    cP = sn;
               //     Gizmos.DrawLine(sn.P, to);
                    continue;
                }
                search.Add(d, sn);
            }
            if(search.Count == 0) break;
            cur = search.Values[search.Count - 1];

            if(search.Keys[search.Count - 1] > cD) break;

            search.RemoveAt(search.Count - 1);
        }
        foreach( Node n in Nodes ) n.SN =null;

        if(cP == null) return null;

        Path ret = new Path();
        List<SmoothNode> smooth = ret.Smooth = new List<SmoothNode>();

      //  Gizmos.color = Color.green;

        //Gizmos.DrawLine( desP, cP.P );

        SmoothNode smt = new SmoothNode();
      //  smt.P2 = smt.P = to;
        //smooth.Add(smt);
        for(; ; ) {
            var n = cP.Prev;
            if(n != null) {
                //Gizmos.DrawLine( n.P, cP.P );

                // Gizmos.DrawLine(Verts[ n.N.Vi[ cP.NbrI ] ],Verts[ n.N.Vi[ (cP.NbrI+2)%3 ] ] );

                smt = new SmoothNode();
                smt.P2 = smt.P = cP.P;
                smt.E1 = Verts[n.N.Vi[cP.NbrI]];
                smt.E2 = Verts[n.N.Vi[(cP.NbrI + 1) % n.N.Nbrs.Length]];// -smt.E1;
                smooth.Add(smt);

                cP = n;
            } else {
                //Gizmos.DrawLine( strtP, cP.P );

              //  smt = new SmoothNode();
              //  smt.P2 = smt.P = from;
              //  smooth.Add(smt);
                break;
            }
        }
        return ret;
        for(int iter = 60; iter-- > 0; ) {

           /* for(int i = smooth.Count - 1; --i > 0; ) {
                smt = smooth[i];
                smt.P2 = (smooth[i + 1].P + smooth[i - 1].P) * 0.5f;

                var vec = smt.P2 - smt.E1;
                var dt = Vector2.Dot(vec, smt.E2); dt /= smt.E2.sqrMagnitude;
                dt = Mathf.Clamp01(dt);
                //  Gizmos.DrawLine(  smt.E1,  smt.E1 + smt.E2 );
                smt.P2 = smt.E1 + smt.E2 * dt;
            }
            for(int i = smooth.Count - 1; --i > 0; ) {
                smt = smooth[i];
                smt.P = (smooth[i + 1].P2 + smooth[i - 1].P2) * 0.5f;

                var vec = smt.P - smt.E1;
                var dt = Vector2.Dot(vec, smt.E2); dt /= smt.E2.sqrMagnitude;
                dt = Mathf.Clamp01(dt);
                //  Gizmos.DrawLine(  smt.E1,  smt.E1 + smt.E2 );
                smt.P = smt.E1 + smt.E2 * dt;
            }*/

            for(int i = smooth.Count - 1; --i > 0; ) {
                smt = smooth[i];
                smt.P = (smt.P*0.5f + smooth[i + 1].P + smooth[i - 1].P) * 0.4f;

                var vec = smt.P - smt.E1;
                var dt = Vector2.Dot(vec, smt.E2); dt /= smt.E2.sqrMagnitude;

                float edge = 0.25f / smt.E2.magnitude;
                dt = Mathf.Clamp(dt, edge, 1.0f-edge );
                //  Gizmos.DrawLine(  smt.E1,  smt.E1 + smt.E2 );
                smt.P = smt.E1 + smt.E2 * dt;
                break;
            }
            break;
        }
        return ret;
        /*
        for(int i = smooth.Count - 1; i-- > 0; ) {
            Gizmos.DrawLine(smooth[i + 1].P, smooth[i].P);
        } */
    }

    void Awake() {
        gen(); // ensure we have a NavMesh! just in case
    }


    public Transform T1, T2;

    //OnDrawGizmo is a convient callback for doing stuff in scene - since it - in particular the  debug stuff I draw in it can then be definately turned off 
    //   aware of ExceuteInEditMode - I like this.
    //   custom inspector is of course a more proper way - but if the interface is very simple (variables will do) the the extra class is pretty superfluous 
    void OnDrawGizmos() {

        if( !Application.isPlaying ) {

            if(Inc) {
                ReGen = true;
                DebugI++;
                Inc = false;
            } else if(Dec) {
                ReGen = true;
                DebugI--;
                Dec = false;
            }


            if(ReGen || Constant || Nodes.Count == 0  ) {
                ReGen = false;
                gen();
            }



            // 5 minute helper function to build inverted collider of the outline - for the collider that keeps you on the map
            if(BuildOuter) {
                BuildOuter = false;

                var go = (GameObject)Instantiate(this.gameObject);
                DestroyImmediate( go.GetComponent<NavMesh>() );
                go.transform.parent = transform.parent;

                PolygonCollider2D pc1 = GetComponent<PolygonCollider2D>(), pc2 = go.GetComponent<PolygonCollider2D>();
                pc2.points[0] = pc1.points[0];
                Vector2 tl = new Vector2(float.MaxValue, float.MaxValue), br = new Vector2(float.MinValue, float.MinValue); 
                float leftMost = float.MaxValue; int ci =0;

                var pnts = new Vector2[pc1.points.Length+4];

                for(int i = pc1.points.Length, j = 0; i-- > 0; j++) {
                    pnts[i] = pc1.points[j];
                  //  pnts[i] += new Vector2(1, 0);

                    float x = pnts[i].x;
                    if(x < leftMost) {
                        leftMost = x;
                        ci = i;
                    }
                    tl = Vector2.Min(tl, pnts[i]);
                    br = Vector2.Max(br, pnts[i]);
                }

                tl -= new Vector2(2, 2);
                br += new Vector2(2, 2);

                for(int i = pc1.points.Length; i-- > ci; ) pnts[i + 4] = pnts[i];

                
                pnts[ci] = new Vector2(tl.x, tl.y);
                pnts[ci + 1] = new Vector2(tl.x, br.y);
                pnts[ci + 2] = new Vector2(br.x, br.y);
                pnts[ci + 3] = new Vector2(br.x, tl.y);

               // pc2.CreatePrimitive(5);
                pc2.SetPath(0, pnts);
                pc2.enabled = true;
            }
        }

        foreach(Tri t in Tris) {
            //break;
            Gizmos.color = Color.white;
            drawCross(t.Centre);
            for(int i = 3; i-- > 0; ) {
                var o = t.Nbrs[i];
                Gizmos.color = new Color(0.5f, 0.5f, 1, 0.5f);
                Gizmos.DrawLine( Verts[ t.Vi[i] ], Verts[ t.Vi[(i+1)%3]] );
                if(o != null) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(t.Centre, o.Centre);
                }
            }
        }

        foreach(Node n in Nodes ) {
            for(int i = n.Nbrs.Length; i-- > 0; ) {

                Gizmos.color = new Color(0.0f, 1.0f, 0, 0.5f);
                Gizmos.DrawLine(Verts[n.Vi[i]], Verts[n.Vi[(i + 1) % n.Nbrs.Length]]);
            }
        }


        if(!Application.isPlaying && T1 != null && T2 != null) {

            var p = getPath(T1.position, T2.position, findNode( T2.position) );
            Gizmos.color = Color.white;
            Gizmos.DrawLine(T1.position, T2.position);
            if(p != null) {
                Debug.Log(" pc " + p.Smooth.Count);
                Vector2 lastPos = T1.position;
                for(int i = p.Smooth.Count; i-- > 0; ) {
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(lastPos, p.Smooth[i].P);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(lastPos, p.Smooth[i].E1);
                    Gizmos.color =  Color.blue;
                    Gizmos.DrawLine(lastPos, p.Smooth[i].E2 );

                    lastPos = p.Smooth[i].P;
                }

                Gizmos.color = Color.black;
                Gizmos.DrawLine(lastPos, T2.position );
            }
        }

    }

}
