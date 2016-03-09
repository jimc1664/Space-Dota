using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FowTest : MonoBehaviour {


    public List<Transform> Eyes;
    public float Rad = 2;
    public float Step = 0.2f;
    public float D1 = 1, D2 = 5;

    public Vector2 Tl = -new Vector2(30, 30), Size = new Vector2(60, 60);

    struct Cell {
        public byte Col;
        public float D, Dm;
    };
    Cell[] Map;


    public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
        public int Compare(K x, K y) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return -result;  // (-) invert 
        }
    }
    class SearchNode {
        //public float D;
        public int I, X, Y;
        public int L_Di;
        public float Dm = 1;
        public int Dir;
        public int Adj;
        public int Fails;
   };

    public float InitDm = 0.2f;
    public int MaxFail = 4;
    public bool Regen = false;


    bool sub(SortedList<float, SearchNode> search, int i, int x, int y, float nk, int di, int adj ) {
        if( Map[i].D < nk) {
            Map[i].D = nk;
            if(nk > Step) {
                SearchNode sn1 = new SearchNode();
                sn1.I = i;
                sn1.X = x; sn1.Y = y;
                sn1.L_Di = di;
                // sn1.Dm = nDm;
                sn1.Dir = 1 << di;
                sn1.Adj = adj;
                search.Add(nk, sn1);
            }
            return true;
        }
        return false;
    }
    public int MaxIter = 1000;
    public float TypRange = 12;

    public Transform FowContainer;
    public GameObject Prefab;

    void gen() {

        Vector3 sz = Vector3.one * 0.8f * Rad;
        Color col = Color.blue;
        Gizmos.color = col;

        /*Vector2 cp = transform.position;
        Vector2 yAx = transform.up;
        Vector2 xAx = transform.right;*/
        Vector2 cp = Tl;
        Vector2 yAx = Vector2.up;
        Vector2 xAx = Vector2.right;

        int lm = 1 << LayerMask.NameToLayer("Map");

        int dx = Mathf.CeilToInt(Size.x / Rad);
        int dy = Mathf.CeilToInt(Size.y / Rad);

        Map = new Cell[dx * dy];
        for(int x = dx; x-- > 0; )
            for(int y = dy; y-- > 0; ) {
                int i = x + y * dx;
                var off = (float)x * xAx + (float)y * yAx;
                var p = cp + off * Rad;
                if(Physics2D.OverlapCircle(p, Rad * 0.3f, lm) == null) {
                    Map[i].Col = 0;
                } else {
                    Map[i].Col = 1;
                }
                Map[i].D = 0;
                Map[i].Dm = 99;
            }


        SortedList<float, SearchNode> search = new SortedList<float, SearchNode>(new DuplicateKeyComparer<float>());

        foreach(Transform eye in Eyes) {
            if(!eye.gameObject.activeInHierarchy) continue;
            var ep = ((Vector2)eye.position - Tl) / Rad;

            int ex = Mathf.RoundToInt(ep.x);
            int ey = Mathf.RoundToInt(ep.y);

            float range = TypRange;
            int i = ex + ey * dx;
            if(Map[i].D > range) continue;
            Map[i].D = range;
            SearchNode sn = new SearchNode();
            sn.I = i;
            sn.X = ex; sn.Y = ey;
            sn.L_Di = 8;
            sn.Dm = InitDm;
            sn.Fails = MaxFail;
            sn.Dir = -1;
            search.Add(range, sn);
        }
        //unchecked {  //just so i can    (uint)-1
        int[,] da = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 }, { 1, 1 }, };

        float[,] dm = new float[9, 8];

        int[ ]  adj= {
            (1<<0) | (1<<4) | (1<<6), 
            (1<<1) | (1<<5) | (1<<7), 
            (1<<2) | (1<<5) | (1<<6), 
            (1<<3) | (1<<4) | (1<<7), 

            (1<<4) | (1<<0) | (1<<3), 
            (1<<5) | (1<<1) | (1<<2), 
            (1<<6) | (1<<0) | (1<<2), 
            (1<<7) | (1<<1) | (1<<3), 
        };
        for(int i1 = 8; i1-- > 0; ) {

            for(int i2 = 8; i2-- > 0; ) {
                Vector2 v1 = new Vector2(da[i1, 0], da[i1, 1]).normalized,
                    v2 = new Vector2(da[i2, 0], da[i2, 1]).normalized;

                float dt = Vector2.Dot(v1, v2);  //-1 -> 1
                //dt = (dt - 1) * -0.5f;   //0 ->  1
                // dt = dt *dt;
                //dt = D1 + dt * (D2 - D1);

                if(dt > 0.95f) {
                    dt = 1;
                } else dt = D2;

                dm[i1, i2] = dt;

              //  Debug.Log(" dt   " + dt + " v1   " + v1 + " v2   " + v2 + " x   " + (da[i2, 0]) + " y   " + (da[i2, 1]));
            }
            dm[8, i1] = 1;
        }

        for(int maxI = MaxIter; maxI-- > 0; ) {
            var iter = search.GetEnumerator();
            if(!iter.MoveNext()) break;
            var pair = iter.Current;
            var key = pair.Key;
            var cur = pair.Value;
            // if(key > bestD * 1.2f) break;
            search.RemoveAt(0);
            //if(key != Map[cur.I].D)
            //    continue;

            // Debug.Log("s  " + cur.X +"   y " + cur.Y+"   k " + key);

            for(int di = 8; di-- > 0; ) {
                if((cur.Dir & (1 << di)) == 0) continue;
                int x = cur.X + da[di, 0];
                int y = cur.Y + da[di, 1];
                if((uint)x >= (uint)dx || (uint)y >= (uint)dy) continue;
                int i = x + y * dx;

                if(Map[i].Col != 0) continue;

                float dm2 = 1;
                if(di >= 4)
                    dm2 = 1.42f;

                float nDm = dm[cur.L_Di, di] *cur.Dm;
                float nk = key - (Rad) * dm2  * Mathf.Max( nDm, 1.0f );
                int fails = cur.Fails;
                //Debug.Log("    n  " + x +"   y " + y);
                if(Map[i].D < nk) {
                    Map[i].D = nk;
                    if(nk < (Rad) * Mathf.Max(nDm, 1.0f)) continue;
                   
                } else {
                    if( fails-- <- 0 ) continue;
                    if(nk < (Rad) * Mathf.Max(nDm, 1.0f)) continue;

                    float nnk1 = Map[i].D - (Rad) * Mathf.Max(Map[i].Dm * D2 * D2, 1.0f);
                    float nnk2 = nk - (Rad) * Mathf.Max(nDm * D1, 1.0f);
                    if(nnk1 > nnk2) continue;

                  //  float nDm = dm[cur.L_Di, di] *cur.Dm;
                  //  
                   // if( Map[i].D< nnk ) continue
                }

                
                Map[i].Dm = Mathf.Min(Map[i].Dm, nDm);
                                            
                SearchNode sn1 = new SearchNode();
                sn1.I = i;
                sn1.X = x; sn1.Y = y;
                sn1.L_Di = di;
                sn1.Dm = nDm;
                sn1.Fails = fails;
                sn1.Dir = adj[di];
                search.Add(nk, sn1);
                
            }
        }

        if(FowContainer == null) return;

        DestroyImmediate(FowContainer.gameObject);

        FowContainer = new GameObject().transform;
        FowContainer.name = "_FowContainer";



        for(int x = dx; x-- > 0; )
            for(int y = dy; y-- > 0; ) {
                int i = x + y * dx;
                if(Map[i].D > 0) {
                    var off = (float)x * xAx + (float)y * yAx;
                    var p = cp + off * Rad;

                    var go = (GameObject)Instantiate( Prefab);
                    var t = go.transform;
                    t.parent = FowContainer;
                    go.layer = 29;
                    t.position = p;
                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>() ;

                    var cl = Color.white;
                    cl *= Mathf.Min(Map[i].D/15, 1) ;
                    cl.a = 1;
                    sr.color = cl;
            
                } 
        
            }


    
            


        // }
    }
    void OnDrawGizmos() {

        if(Map == null || Regen) {
            gen();
            Regen = false;
        }

        Vector3 sz = Vector3.one * 0.8f * Rad;
        Color col = Color.blue;
        Gizmos.color = col;

        /*Vector2 cp = transform.position;
        Vector2 yAx = transform.up;
        Vector2 xAx = transform.right;*/
        Vector2 cp = Tl;
        Vector2 yAx = Vector2.up;
        Vector2 xAx = Vector2.right;

        int lm = 1 << LayerMask.NameToLayer("Map");

        int dx = Mathf.CeilToInt(Size.x / Rad);
        int dy = Mathf.CeilToInt(Size.y / Rad);
        for(int x = dx; x-- > 0; )
            for(int y = dy; y-- > 0; ) {
                int i = x + y * dx;
                var off = (float)x * xAx + (float)y * yAx;
                var p = cp + off * Rad;
                if(Map[i].D > 0) {
                    Gizmos.color = new Color(0, Map[i].D / TypRange, 0);
                } else if(Map[i].Col == 0) {
                    Gizmos.color = Color.blue;
                } else {
                    Gizmos.color = Color.red;
                }
                Gizmos.DrawWireCube(p, sz);
            }


    }
}
