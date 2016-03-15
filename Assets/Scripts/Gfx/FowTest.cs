using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;

//[ExecuteInEditMode]
public class FowTest : MonoBehaviour {

    public void register(Unit u) {
        Eyes.Add(u);
    }


    public bool amVisible(Transform t, float rad, float check = 0.1f ) {
        //rad = (rad + 1.0f) * 0.33f;


        int Dx = Mathf.CeilToInt(Size.x / Rad);
        int Dy = Mathf.CeilToInt(Size.y / Rad);


        rad /= Rad;
        var ep = ((Vector2)t.position - Tl) / Rad;

        Vector2 ec1 = new Vector2(Mathf.Floor(ep.x - rad), Mathf.Floor(ep.y - rad));
        Vector2 ec2 = new Vector2(Mathf.Ceil(ep.x + rad), Mathf.Ceil(ep.y + rad));


        int e1x = Mathf.Max( (int)ec1.x, 0 );
        int e1y = Mathf.Max( (int)ec1.y, 0 );
        int e2x = Mathf.Min( (int)ec2.x, Dx-1 );
        int e2y = Mathf.Min((int)ec2.y, Dy - 1); 


  
        for(int x = e1x; x <= e2x; x++ ) {
            for(int y = e1y; y <= e2y; y++ ) {
                int i = x + y * Dx;
                if(Map[i].Dh > check ) {
                   // Debug.Log("x   " + x + "  y " + y + " d " + Map[i].Dh);
                    return true;
                }


            }

        }

        return false;
    }

    public bool amVisible2(Transform t, float rad) {
        rad = (rad + 1.0f) * 0.33f;


        var ep = ((Vector2)t.position - Tl) / Rad;

        Vector2 ec = new Vector2(Mathf.Floor(ep.x), Mathf.Floor(ep.y));

        int Dx = Mathf.CeilToInt(Size.x / Rad);
        int Dy = Mathf.CeilToInt(Size.y / Rad);

        int e1x = (int)ec.x;
        int e1y = (int)ec.y;

        var em = ep - ec;

        float val = 0;
        float xm = 1 - em.x;
        for(int x = 2; x-- > 0; ) {
            int ex = e1x + x;
            if((uint)ex >= Dx) continue;
            float ym = 1 - em.y;
            for(int y = 2; y-- > 0; ) {
                int ey = e1y + y;
                if((uint)ey >= Dy) continue;
                int i = ex + ey * Dx;

                val += Map[i].Dh * xm * ym;
            }
               
            ym = 1 - ym;
        }
        xm = 1 - xm;

        return val > rad;
    }


    public Camera Cam;
    public List<Unit> Eyes;

    public float Rad = 2;
    public float Step = 0.2f;
    public float D1 = 1, D2 = 5;

    public Vector2 Tl = -new Vector2(30, 30), Size = new Vector2(60, 60);

    struct Cell {
       // public float Col;
        public float D, Dm, Dh;
    };
    Cell[] Map;

    float[] Map_Col;

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
        //  public int Adj;
    };

    class SearchNode_DBias  :  SearchNode {
        //public float D;

        public float Dm = 1;
        //  public int Adj;
        public int Fails;
    };


    public float InitDm = 0.2f;
    public int MaxFail = 4;
    public bool Regen = false;



    public int MaxIter = 1000;
    public float TypRange = 12, MaxCol = 4;

    public Transform FowContainer;
    public GameObject Prefab;

    class ThreadTask {

        volatile bool _IsDone = false;
        public bool IsDone { get { return _IsDone; } private set { } }
        System.Threading.Thread Hndl = null;
        
        
        public void start() {
            Hndl = new System.Threading.Thread( func );
            Hndl.Start();
        }

        public float Rad;

        public Cell[] Map, OldMap;

        public int Dx, Dy;
        float D2, MaxCol; int MaxIter;
        float InitDm; int MaxFail;

        float[] Map_Col;
        Vector2 Tl;
        public void init(ref float[] _Map_Col, ref Cell[] oldMap, Vector2 tl, Vector2 sz, float rad, List<Unit> Eyes, float initDm, int maxFail, float MaxCol, float d2, int maxIter) {

            Tl = tl;
            InitDm = initDm; MaxFail = maxFail;
            Rad = rad;
            D2 = d2; 
            MaxIter = maxIter;

            Vector2 yAx = Vector2.up;
            Vector2 xAx = Vector2.right;

            Dx = Mathf.CeilToInt(sz.x / Rad);
            Dy = Mathf.CeilToInt(sz.y / Rad);

            int lm = 1 << LayerMask.NameToLayer("Map");


            Map = new Cell[Dx * Dy];
            if(oldMap == null) 
                oldMap = new Cell[Map.Length];
                      
            OldMap = oldMap;

            if(_Map_Col == null || _Map_Col.Length != Map.Length) {
                Map_Col = _Map_Col = new float[Map.Length];
                for(int x = Dx; x-- > 0; )
                    for(int y = Dy; y-- > 0; ) {
                        int i = x + y * Dx;
                        var off = (float)x * xAx + (float)y * yAx;
                        var p = tl + off * Rad;

                        float rt = 1.0f;
                        if(Physics2D.OverlapCircle(p, Rad * 0.5f * rt, lm) == null) {
                            Map_Col[i] = 1;
                        } else {
                            //   Map[i].Col = 2;
                            rt = 0.1f;
                            if(Physics2D.OverlapCircle(p, Rad * 0.5f * rt, lm) != null) {
                                Map_Col[i] = 2;
                            } else {
                                float high = 1.0f, low = rt;
                                for(int iter = 4; iter-- > 0; ) {
                                    rt = (low + high) * 0.5f;
                                    if(Physics2D.OverlapCircle(p, Rad * 0.5f * rt, lm) == null) {
                                        low = rt;
                                    } else
                                        high = rt;

                                }
                                Map_Col[i] = 2.0f - low;
                            }
                        }
                        // Map[i].D = 0;
                        //  Map[i].Dm = 99;
                    }
            } else
                Map_Col = _Map_Col;

            if( Entries == null ) 
                 Entries = new List<Entry>();
            else 
                Entries.Clear();

            Entry e = new Entry();
            for(int ei = Eyes.Count; ei-- > 0; ) {
                Unit eye = Eyes[ei];
                if(eye == null) {
                    Eyes.RemoveAt(ei);
                    continue;
                }

                if(!eye.gameObject.activeInHierarchy) continue;

                e.P = eye.Trnsfrm.position;
                e.Rad = eye.Vision;
                e.High = eye.IsHighAsFuckPal;
                Entries.Add(e);

            }
        }

        struct Entry {
            public Vector2 P;
            public bool High;
            public float Rad;
        }
        static List<Entry> Entries;

        public void gen( ) {

            //Vector3 sz = Vector3.one * 0.8f * Rad;
            ///Color col = Color.blue;
            //Gizmos.color = col;

            /*Vector2 cp = transform.position;
            Vector2 yAx = transform.up;
            Vector2 xAx = transform.right;*/
           // Vector2 cp = Tl;
            //Vector2 yAx = Vector2.up;
            //Vector2 xAx = Vector2.right;

            SortedList<float, SearchNode_DBias> Search = new SortedList<float, SearchNode_DBias>(new DuplicateKeyComparer<float>());
            SortedList<float, SearchNode> Search2 = new SortedList<float, SearchNode>(new DuplicateKeyComparer<float>());

            foreach(Entry e in Entries) {
                var ep = ((Vector2)e.P - Tl) / Rad;

                Vector2 ec = new Vector2(Mathf.Floor(ep.x), Mathf.Floor(ep.y));

                int e1x = (int)ec.x;
                int e1y = (int)ec.y;

                var em = ep - ec;

                float xm = 1 - em.x;
                for(int x = 2; x-- > 0; ) {

                    int ex = e1x + x;
                    if((uint)ex >= Dx) continue;

                    float ym = 1 - em.y;
                    for(int y = 2; y-- > 0; ) {
                        
                        int ey = e1y + y;
                        if((uint)ey >= Dy) continue;
                        float range = e.Rad;
                        int i = ex + ey * Dx;
                        //if(Map[i].D > range) continue;
                        range -= new Vector2(xm, ym).magnitude * Rad;

                        if( ! e.High ) {
                            Map[i].D = range;
                            for(int di = 8; di-- > 0; ) {
                                var sn = new SearchNode_DBias();
                                sn.I = i;
                                sn.X = ex; sn.Y = ey;

                                sn.Dm = InitDm;
                                sn.Fails = MaxFail;
                                sn.L_Di = di;
                                //sn.Dir = -1;
                                Search.Add(range, sn);
                            }
                        } else {
                            
                            Map[i].Dh = range;
                            for(int di = 4; di-- > 0; ) {
                                var sn = new SearchNode();
                                sn.I = i;
                                sn.X = ex; sn.Y = ey;

                                sn.L_Di = di;
                                Search2.Add(range, sn);
                            }
                        }
                        ym = 1 - ym;
                    }
                    xm = 1 - xm;
                }
            }
            
            //unchecked {  //just so i can    (uint)-1
            int[,] da = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 }, { 1, 1 }, };

            int[,] adj2 = {
                {0, 4, 6}, 
                {1, 5, 7}, 
                {2, 5, 6}, 
                {3, 4, 7}, 

                {4, 0, 3}, 
                {5, 1, 2}, 
                {6, 0, 2}, 
                {7, 1, 3}, 
            };
            for(int maxI = MaxIter; maxI-- > 0; ) {
                var iter = Search2.GetEnumerator();
                if(!iter.MoveNext()) break;
                var pair = iter.Current;
                var key = pair.Key;
                var cur = pair.Value;
                // if(key > bestD * 1.2f) break;
                Search2.RemoveAt(0);
                if(key != Map[cur.I].Dh)
                    continue;

                // Debug.Log("s  " + cur.X +"   y " + cur.Y+"   k " + key);

                for(int ai = 3; ai-- > 0; ) {
                    //for(int di = 8; di-- > 0; ) {
                    //  if((cur.Dir & (1 << di)) == 0) continue;
                    int di = adj2[cur.L_Di, ai];
                    int x = cur.X + da[di, 0];
                    int y = cur.Y + da[di, 1];
                    if((uint)x >= (uint)Dx || (uint)y >= (uint)Dy) continue;
                    int i = x + y * Dx;
                    //todo -- no x y  --- rely on map for no wrapping

                    float dm2 = 1;
                    if(di >= 4)
                        dm2 = 1.42f;

                    float nk = key - (Rad) * dm2;

                    //  if(nk > OldMap[i].D)
                    //     nk += (OldMap[i].D - nk) * 0.3f;

                    
                    //Debug.Log("    n  " + x +"   y " + y);
                    if(Map[i].Dh < nk) {
                        Map[i].Dh = nk;

                    } else {
                        continue;
                    }
                    var sn1 = new SearchNode();
                    sn1.I = i;
                    sn1.X = x; sn1.Y = y;
                    sn1.L_Di = di;
                    Search2.Add(nk, sn1);
                }
            }

            for(int maxI = MaxIter; maxI-- > 0; ) {
                var iter = Search.GetEnumerator();
                if(!iter.MoveNext()) break;
                var pair = iter.Current;
                var key = pair.Key;
                var cur = pair.Value;
                // if(key > bestD * 1.2f) break;
                Search.RemoveAt(0);
                //if(key != Map[cur.I].D)
                //    continue;

                // Debug.Log("s  " + cur.X +"   y " + cur.Y+"   k " + key);

                for( int ai = 3; ai-- >0;) {
                //for(int di = 8; di-- > 0; ) {
                  //  if((cur.Dir & (1 << di)) == 0) continue;
                    int di = adj2[cur.L_Di, ai];
                    int x = cur.X + da[di, 0];
                    int y = cur.Y + da[di, 1];
                    if((uint)x >= (uint)Dx || (uint)y >= (uint)Dy) continue;
                    int i = x + y * Dx;
                    //todo -- no x y  --- rely on map for no wrapping


                    if(Map_Col[i] >= 2) continue;

                    float dm2 = 1;
                    if(di >= 4)
                        dm2 = 1.42f;

                    float nDm = (( ai == 0 ) ? 1 : D2) * cur.Dm; 

                    float nk = key - (Rad) * dm2 * Mathf.Max(nDm, 1.0f) * Map_Col[i];
                    if(Map[i].Dh > nk) continue;
                  //  if(nk > OldMap[i].D)
                   //     nk += (OldMap[i].D - nk) * 0.3f;

                    int fails = cur.Fails;
                    //Debug.Log("    n  " + x +"   y " + y);
                    if(Map[i].D < nk) {
                        Map[i].D = nk;
                        if(nk < (Rad) * Mathf.Max(nDm, 1.0f)) continue;
                   
                    } else {
                        if( fails-- <- 0 ) continue;
                        if(nk < (Rad) * Mathf.Max(nDm, 1.0f)) continue;

                        float nnk1 = Map[i].D - (Rad) * Mathf.Max(Map[i].Dm * D2 * D2, 1.0f) ;
                        float nnk2 = nk - (Rad) * Mathf.Max(nDm , 1.0f);
                        if(nnk1 > nnk2) continue;

                      //  float nDm = dm[cur.L_Di, di] *cur.Dm;
                      //  
                       // if( Map[i].D< nnk ) continue
                    }

                
                    Map[i].Dm = Mathf.Min(Map[i].Dm, nDm);

                    var sn1 = new SearchNode_DBias();
                    sn1.I = i;
                    sn1.X = x; sn1.Y = y;
                    sn1.L_Di = di;
                    sn1.Dm = nDm;
                    sn1.Fails = fails;
                    //sn1.Dir = di;
                    Search.Add(nk, sn1);
                
                }
            }

            for(int x = Dx; x-- > 0; )
                for(int y = Dy; y-- > 0; ) {
                    int i = x + y * Dx;
                   // Map[i].Dh = Mathf.Max( Map[i].Dh, Map[i].D ) ;
                    Map[i].D += (OldMap[i].D - Map[i].D) * 0.6f;
                    Map[i].Dh += (OldMap[i].Dh - Map[i].Dh) * 0.6f;
                    Map[i].Dh = Mathf.Max(Map[i].D, Map[i].Dh);
                }
        }

        void func() {
            gen();
            _IsDone = true;
        }
    };
    ThreadTask Pending = null;
    void _finish(  ) {

        // var oCont = FowContainer;
        //if(FowContainer != null) return;
        //DestroyImmediate(FowContainer.gameObject);

        if(FowContainer == null) {
            FowContainer = new GameObject().transform;
            FowContainer.name = "_FowContainer";
        }
            
        Vector2 yAx = Vector2.up;
        Vector2 xAx = Vector2.right;

        int sInd = 0;

        for(int x = Pending.Dx; x-- > 0; )
            for(int y = Pending.Dy; y-- > 0; ) {
                int i = x + y * Pending.Dx;
                if(Map[i].D > 0 || Map[i].Dh > 0) {
                    var off = (float)x * xAx + (float)y * yAx;
                    var p = Tl + off * Rad;

                    Transform t;
                    int si = sInd;
                    if(SprPool.Count > sInd++   ) {
                        t = SprPool[si];
                        if(t == null) {
                            var go = (GameObject)Instantiate(Prefab);
                            t = SprPool[si] =go.transform;
                            t.parent = FowContainer;  
                            go.layer = 29;
                        }
                    } else {
                        var go = (GameObject)Instantiate(Prefab);
                        go.layer = 29;
                        t = go.transform;
                        t.parent = FowContainer;  
                        SprPool.Add( t);
                    }
                                    
                    t.position = p;
                    SpriteRenderer sr = t.GetComponent<SpriteRenderer>() ;

                    float rscl = 15;
                    sr.color = new Color(  Mathf.Min(Map[i].D/rscl, 1),  Mathf.Min( Map[i].Dh/rscl, 1), 0, 1);
            
                } 
        
            }

        for(int i = sInd; i < SprPool.Count; i++)
            if(SprPool[i] != null) 
                DestroyImmediate( SprPool[i].gameObject );
        //  if( oCont != null )
        //      DestroyImmediate(oCont.gameObject);
        
    // }
    }

    void gen() {
        Pending = new ThreadTask();

        Pending.init(ref Map_Col, ref Map, Tl, Size, Rad, Eyes, InitDm, MaxFail, MaxCol, D2, MaxIter);
        Pending.gen();
        Map = Pending.Map;
        _finish();
    }

    List<Transform> SprPool = new List<Transform>();

    float LastGen = -1;
    public float UpdateFreq = 0.5f;
    public bool ConstantUpdate = true;
    void Update() {

        float time; 
#if UNITY_EDITOR
        time = (float)EditorApplication.timeSinceStartup;
#else
        time = Time.time;
#endif
        Cam.enabled = false;

        if(Pending == null) {

            if((time - LastGen) < UpdateFreq) return;
            if(Map == null || Regen || ConstantUpdate) {
                Pending = new ThreadTask();
                Pending.init(ref Map_Col, ref Map, Tl, Size, Rad, Eyes, InitDm, MaxFail, MaxCol, D2, MaxIter);
                Pending.start();               
            }
        } else if( Pending.IsDone ) {
            Map = Pending.Map;
            _finish();
            Pending = null;
            LastGen = time;
            Regen = false;
            Cam.enabled = true;
        }
    }
    

    void OnDrawGizmos() {
        return;
        if( (Map == null || Regen) && Pending == null) {
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
                } else if(Map_Col[i] == 1) {
                    Gizmos.color = Color.blue;
                } else {
                    Gizmos.color = new Color(2 - Map_Col[i], 0, 0);
                }
                Gizmos.DrawWireCube(p, sz);
            }


    }

    static FowTest Singleton;
    public static FowTest get() {
#if UNITY_EDITOR
        if(Singleton == null) Singleton = FindObjectOfType<FowTest>();
#endif //UNITY_EDITOR
        return Singleton;
    }
    void Awake() {
        if(Singleton != null && Singleton != this) Debug.LogError("Singleton violation");
        Singleton = this;
        Eyes.Clear();
    }
}
