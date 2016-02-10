using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Ignore this script - for testing purposes only.....
/// </summary>

public class NavMeshGenTest : MonoBehaviour {

    public int DebugI = 0;
    public bool Inc = false;
    int Tri;

    public Transform Obj1, Obj2;
    void drawTri(Vector2 a, Vector2 b, Vector2 c) {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(c, b);
        Gizmos.DrawLine(a, c);
    }

    void drawCross( Vector2 p ) {
        Gizmos.DrawLine(p - Vector2.right, p + Vector2.right);
        Gizmos.DrawLine(p - Vector2.up, p + Vector2.up);
    }

    List<Vector2> Verts;

    class Node {
        public int[] Vi = new int[3];
        public Node[] Nbrs = new Node[3];
        public Vector2[] Edge = new Vector2[3];

        public Vector2 Centre;

        public SearchNode SN;

        public int a { get { return Vi[0]; } set { Vi[0] = value; } }
        public int b { get { return Vi[1]; } set { Vi[1] = value; } }
        public int c { get { return Vi[2]; } set { Vi[2] = value; } }

    };
    List<Node> Nodes;

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

    bool gen() {
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

        var gos = GameObject.FindGameObjectsWithTag("NavMesh");

        //List<int> blackList
        foreach(GameObject go in gos) {
            col = go.GetComponent<PolygonCollider2D>();
            trns = go.transform;

            List<int> ni = new List<int>(col.points.GetLength(0));

            for(int i = 0; i < col.points.GetLength(0); i++) {
                ni.Add(Verts.Count);
                Verts.Add(trns.TransformPoint(col.points[i]));
            }
            // for(int i = col.points.GetLength(0); i-- > 0; ) ni.Add(trns.TransformPoint(col.points[i]));

            islands.Add(ni);

        }

        foreach(List<int> ni in islands) {

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
                        if(ii == ni) continue;
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
                    //  Debug.DrawLine( a, b );
                    goto label_jumpout;
                label_breakContinue1: ;
                }
            }
        label_jumpout: ;
            //*/
        }

        islands = null;

        // Gizmos.color = new Color(0, 0, 1, 0.7f);
        for(int i = vi.Count, j = 0; i-- > 0; j = i) {
            Gizmos.DrawLine(Verts[vi[i]], Verts[vi[j]]);
        }

        // return;
        List<int> ovi = new List<int>(vi);

        Gizmos.color = new Color(0, 0, 1, 0.7f);
        for(int iter = 1; iter-- > 0; )
            for(int i = vi.Count, j = 0, k = 1; i-- > 0; k = j, j = i) {

                int ia = vi[i], ib = vi[j], ic = vi[k];
                Vector2 a = Verts[ia], b = Verts[ib], c = Verts[ic];
                //*
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

                    if(DebugI == Tri) {
                        Gizmos.color = new Color(1, 1, 0, 1.0f);
                        drawTri(a, b, c);
                    }
                    goto label_breakContinue2;
                }

                for(int l = Verts.Count; l-- > 0; ) {
                    if(ia == l || ib == l || ic == l) continue;
                    if(Util.pointInTriangle(Verts[l], a, b, c)) {
                        //*
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
                        if(DebugI == Tri) {
                            Gizmos.color = new Color(1, 0, 0, 0.7f);
                            Gizmos.DrawLine(c, a);
                            Gizmos.DrawLine(Verts[ovi[l]], Verts[ovi[m]]);//*/

                            Vector2 ret = Vector2.zero;
                            if(Util.lineLineIntersection(a, c, Verts[ovi[l]], Verts[ovi[m]], ref ret))
                                drawCross(ret);
                        }
                        goto label_breakContinue2;
                    }

                }

                vi.RemoveAt(j);

                if(Tri == DebugI) {
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
                }
                addNode(ia, ib, ic);
                if(vi.Count <= 2) { iter = 0; break; }

                i--;
                j = (i + 1) % vi.Count;
                k = (i + 2) % vi.Count;

                iter = 1;
                label_breakContinue2: ;

                if(Tri == DebugI) return false;
            }

        if(vi.Count > 2) {
            Gizmos.color = Color.red;
            for(int l = vi.Count, m = 0; l-- > 0; m = l) {
                Gizmos.DrawLine(Verts[vi[l]], Verts[vi[m]]);
            }

            return false;
        }
        return true;
    }


    Node findNode(Vector2 p) {
        foreach(Node n in Nodes) {

            if(Util.pointInTriangle(p, Verts[n.a], Verts[n.b], Verts[n.c])) return n; ;
        }
        return null;
    }

    public class DuplicateKeyComparer<K> :  IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return -result;  // (-) invert 
        }
    }
    class SearchNode {
        public Node N;
        public int NbrI;
        public Vector2 P;
        public float D;
        public SearchNode Prev;
    }

    class SmoothNode {
        public Vector2 P;
        public Vector2 E1, E2;
    }

    void OnDrawGizmos() {
        if(Inc) {
            DebugI++;
            Inc = false;
        }
        Tri = -1;

        if(!gen()) ;// return;
        Gizmos.color = Color.white;
        foreach( Node n in Nodes) {

            drawCross( n.Centre );
            for(int i = 3; i-- > 0; ) {
                var o = n.Nbrs[i];
                
                if(o != null) {
                    Gizmos.DrawLine( n.Centre, o.Centre );
                }
            }
        }

        if(Nodes.Count == 0 || !Obj1 || !Obj2) return;

        Gizmos.color = Color.red;
        Node a = findNode(Obj1.position), b = findNode(Obj2.position);

        if(a==null || b==null) return;

        SortedList<float, SearchNode> search = new SortedList<float, SearchNode>( new DuplicateKeyComparer<float>() );

        Vector2 desP = Obj2.position, strtP = Obj1.position;

        var cur = new SearchNode();
        cur.D = 0;
        cur.N = a;
        cur.Prev = null;
        cur.P = Obj1.position;
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
              //  Gizmos.DrawLine(cur.P, n.Edge[i] );

                var sn = new SearchNode();
                sn.P = n.Edge[i];
                sn.D = cur.D + (cur.P - sn.P).magnitude;
                sn.N = o;
                sn.Prev = cur;
                sn.N.SN = sn;
                sn.NbrI =i;

                var d = sn.D + (desP - sn.P).magnitude;
                if(d > cD) continue;

                if(o == b) {                                 
                    cD = d;
                    cP = sn;
                   // Gizmos.DrawLine(sn.P, desP);
                    continue;
                }
                search.Add( d, sn);
            }
            if(search.Count == 0) break;
            cur = search.Values[search.Count - 1];

            if(search.Keys[search.Count - 1] > cD) break;

            search.RemoveAt(search.Count - 1);
        }


        if(cP==null) return;


        List<SmoothNode> smooth  = new List<SmoothNode>();

        

        //Gizmos.DrawLine( desP, cP.P );

        SmoothNode smt = new SmoothNode();
        smt.P = desP;
        smooth.Add( smt );
        for(; ; ) {
            var n = cP.Prev;
            if(n != null) {
                //Gizmos.DrawLine( n.P, cP.P );

               // Gizmos.DrawLine(Verts[ n.N.Vi[ cP.NbrI ] ],Verts[ n.N.Vi[ (cP.NbrI+2)%3 ] ] );
                
                smt = new SmoothNode();
                smt.P = cP.P;
                smt.E1 =Verts[ n.N.Vi[ cP.NbrI ] ];
                smt.E2 = Verts[n.N.Vi[(cP.NbrI + 2) % 3]];// -smt.E1;
                smooth.Add( smt );

                cP = n;
            } else {
                //Gizmos.DrawLine( strtP, cP.P );

                smt = new SmoothNode();
                smt.P = strtP;
                smooth.Add( smt );
                break;
            }
        }

        Gizmos.color = Color.red;
        
        /*
        for(int iter =10; iter-->0;) {
           // break;
            for(int i = smooth.Count-1;--i > 0;) {
                smt = smooth[i];

                
                smt.P = (smt.P*0 + smooth[i+1].P+ smooth[i-1].P )*0.5f;

                //var op = smt.P;

               // Util.lineLineIntersection( smooth[i+1].P, smooth[i-1].P, smt.E1,  smt.E1 + smt.E2, ref smt.P );

                var vec = smt.P - smt.E1;
                var dt = Vector2.Dot( vec, smt.E2 ); dt /= smt.E2.sqrMagnitude;

               

                
                dt = Mathf.Clamp01( dt );
               // Gizmos.DrawLine(  smt.E1,  smt.E1 + smt.E2 );

               // Gizmos.DrawLine(smooth[i + 1].P, smooth[i - 1].P);
                smt.P =smt.E1 + smt.E2*dt;

               // Gizmos.DrawLine(smt.P,  op );
                break;
            }
            break;
        }

        Gizmos.color = Color.green;
        for(int i = smooth.Count-1;i-- > 0;) {
            Gizmos.DrawLine( smooth[i+1].P, smooth[i].P );
        } */


        Vector2 vec, fnlA, fnlB;

        Vector2 tPos = smooth[0].P,  cPos = smooth[smooth.Count-1].P;

        int CurNode = smooth.Count - 2;
        //  tPos = smooth[CurNode].P;
        fnlA = smooth[CurNode].E1;
        fnlB = smooth[CurNode].E2;
        float sgn = -1;


        for(int ci = CurNode - 1; ; ci--) {

            if(ci <= 0) {

                break;
            }


            //Vector2 tPos = smooth[CurNode].P;


            Vector2 fnlA2 = smooth[ci].E1;
            Vector2 fnlB2 = smooth[ci].E2;

            if((fnlA2 - fnlA).sqrMagnitude > (fnlB2 - fnlB).sqrMagnitude) {
                Debug.DrawLine(cPos, fnlA2, Color.black);

                sgn = Util.sign(fnlA2, cPos, fnlA);
                if(Util.sign(fnlA2, cPos, fnlA) > 0) {

                    if(Util.sign(fnlB, cPos, fnlA) < 0) {
                        tPos = fnlB;
                        break;
                    }
                    fnlA = fnlA2;
                } else {

                    // tPos = fnlA;
                    //   break;
                }


            } else {
                Debug.DrawLine(cPos, fnlB2, Color.black);
                sgn = Util.sign(fnlB, cPos, fnlB2);

                if(Util.sign(fnlB, cPos, fnlB2) > 0) {
                    if(Util.sign(fnlB, cPos, fnlA) < 0) {
                        //  tPos = fnlA;
                        //  break;
                    }
                    fnlB = fnlB2;
                } else {
                    //  tPos = fnlB;
                    //   break;
                }
            }

        }

                
        if(Util.sign(fnlB, cPos, tPos) < 0) tPos = fnlB;
        if(Util.sign(tPos, cPos, fnlA) < 0) tPos = fnlA;

        //   Debug.Log("sgn  " + sgn);
        Debug.DrawLine(cPos, fnlA, Color.green);
        Debug.DrawLine(cPos, fnlB, Color.red);
        Debug.DrawLine(cPos, tPos, Color.white);
    }

}
