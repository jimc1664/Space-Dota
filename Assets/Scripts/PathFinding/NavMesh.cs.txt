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
   // public int DebugI = 0;
  //  public bool Inc = false;
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

    List<Vector2> Verts = new List<Vector2>();


    //a trinagle of the NavMesh
    public class Node {
        public int[] Vi = new int[3];
        public Node[] Nbrs = new Node[3];
        public Vector2[] Edge = new Vector2[3];

        public Vector2 Centre;

        public SearchNode SN;

        public int a { get { return Vi[0]; } set { Vi[0] = value; } }
        public int b { get { return Vi[1]; } set { Vi[1] = value; } }
        public int c { get { return Vi[2]; } set { Vi[2] = value; } }

    };
    List<Node> Nodes = new List<Node>();

    void addNode(int a, int b, int c) {

        var n = new Node();
        n.Vi[0] = a;
        n.Vi[1] = b;
        n.Vi[2] = c;

        n.Centre = (Verts[n.a] + Verts[n.b] + Verts[n.c]) * (1.0f / 3.0f);

        n.Edge[0] = (Verts[a] + Verts[c]) * 0.5f;
        n.Edge[1] = (Verts[b] + Verts[a]) * 0.5f;
        n.Edge[2] = (Verts[c] + Verts[b]) * 0.5f;

        for(int i = Nodes.Count; i-- > 0; ) {
            var o = Nodes[i];

            for(int j1 = 0, j2 = 2; j1 < 3; j2 = j1++) {
                int v1 = n.Vi[j1], v2 =n.Vi[j2];
                for(int k1 = 3, k2 = 0; k1-- > 0; k2 = k1) {
                    if(o.Vi[k1] != v1 || o.Vi[k2] != v2) continue;

                    n.Nbrs[j1] = o;
                    if(o.Nbrs[k2] != null) Debug.LogError("err");
                    o.Nbrs[k2] = n;

                    goto label_doubleBreak;
                }
            }

            label_doubleBreak: ;
        }
        
        
        Nodes.Add(n);
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

		foreach (GameObject go in gos)
		{
			foreach (var subCol in go.GetComponents<PolygonCollider2D>())
			{
				trns = go.transform;

				List<int> ni = new List<int>(subCol.points.GetLength(0));

				for (int i = 0; i < subCol.points.GetLength(0); i++)
				{
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
                        Debug.DrawLine(a, b, Color.black);


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

        Gizmos.color = new Color(0, 0, 1, 0.7f);


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
                addNode(ia, ib, ic);
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
        return true;
    }



    //search nodes for one constaining 'p'  - spatial sorting is obvious optimisation - not priority 
    public Node findNode(Vector2 p) {
        foreach(Node n in Nodes) {
            if(Util.pointInTriangle(p, Verts[n.a], Verts[n.b], Verts[n.c])) return n;
        }
        return null;
    }

    public Node findNode(Vector2 p, Node n ) {
        if(n != null && Util.pointInTriangle(p, Verts[n.a], Verts[n.b], Verts[n.c])) return n;        
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
            for(int i = 3; i-- > 0; ) {
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
        smt.P2 = smt.P = to;
        smooth.Add(smt);
        for(; ; ) {
            var n = cP.Prev;
            if(n != null) {
                //Gizmos.DrawLine( n.P, cP.P );

                // Gizmos.DrawLine(Verts[ n.N.Vi[ cP.NbrI ] ],Verts[ n.N.Vi[ (cP.NbrI+2)%3 ] ] );

                smt = new SmoothNode();
                smt.P2 = smt.P = cP.P;
                smt.E1 = Verts[n.N.Vi[cP.NbrI]];
                smt.E2 = Verts[n.N.Vi[(cP.NbrI + 2) % 3]];// -smt.E1;
                smooth.Add(smt);

                cP = n;
            } else {
                //Gizmos.DrawLine( strtP, cP.P );

                smt = new SmoothNode();
                smt.P2 = smt.P = from;
                smooth.Add(smt);
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


    //OnDrawGizmo is a convient callback for doing stuff in scene - since it - in particular the  debug stuff I draw in it can then be definately turned off 
    //   aware of ExceuteInEditMode - I like this.
    //   custom inspector is of course a more proper way - but if the interface is very simple (variables will do) the the extra class is pretty superfluous 
    void OnDrawGizmos() {

        if( !Application.isPlaying ) {
            if(Nodes.Count == 0 || ReGen || Constant) {
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
  
        foreach(Node n in Nodes) {
            Gizmos.color = Color.white;
            drawCross(n.Centre);
            for(int i = 3; i-- > 0; ) {
                var o = n.Nbrs[i];
                Gizmos.color = new Color(0, 0, 1, 0.5f);
                Gizmos.DrawLine( Verts[ n.Vi[i] ], Verts[ n.Vi[(i+1)%3]] );
                if(o != null) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(n.Centre, o.Centre);
                }
            }
        }        
        return;
    }

}
