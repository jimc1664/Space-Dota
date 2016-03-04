using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour {
    public int Count = 5;
    public bool Inc = false;

    void OnDrawGizmos() {
        Vector3 sz = Vector3.one *0.3f;
        Color col = Color.blue;
        Gizmos.color = col;

        Vector2 cp = transform.position;
        Vector2 yAx = transform.up;
        Vector2 xAx = transform.right;
        float rad = 0.5f;

        if(Inc) {
            Count++;
            Inc = false;
        }

        int lm = 1 << LayerMask.NameToLayer("Map");
        int r = 0, c = 0, cm = 1, rm = 0;
        bool fr = false;
        for(int i = Count; ; ) {

            var off = c *xAx + r*yAx;
            var p = cp +off *rad;
            if( Physics2D.OverlapCircle( p, rad *0.8f, lm ) == null ) {
                i--;
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube( p, sz);
                if( i <= 0 ) break;
            } else {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(p, sz);
            }

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
        }
    }
}
