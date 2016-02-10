using UnityEngine;
using System.Collections;


/// <summary>
/// -Utility functions, math used by pathfinding.  
/// The actual math behind functions comes from internet, for reasons of safety, speed and sense.
/// </summary>
public class Util  {

    //http://stackoverflow.com/questions/2049582/how-to-determine-a-point-in-a-triangle
    //todo - barycentric based - faster?
    public static float sign(Vector2 p1, Vector2 p2, Vector2 p3) {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
     }
    public static bool pointInTriangle (Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3) {
        bool b1, b2, b3;

        b1 = sign(pt, v1, v2) < 0.0f;
        b2 = sign(pt, v2, v3) < 0.0f;
        b3 = sign(pt, v3, v1) < 0.0f;

        return ((b1 == b2) && (b2 == b3));
    }

    //http://community.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry2
    public static bool lineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 ret) {        
        // line :=  aX + bY = c
        float a1 = p2.y - p1.y;
        float b1 = p1.x - p2.x;

        float a2 = p4.y - p3.y;
        float b2 = p3.x - p4.x;

        float crs = a1 * b2 - a2 * b1;
        if(crs <= 0.00001f) return false;

        float c1 = a1 * p1.x + b1 * p1.y;
        float c2 = a2 * p3.x + b2 * p3.y;

        float invCrs = 1.0f /crs;

        ret =  new Vector2( (b2 * c1 - b1 * c2) * crs, (a1 * c2 - a2 * c1) * crs  );
        return true;
    }

    //le math http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/comment-page-1/
    //and props to Daniel Prihodko for finding my stupid typo
    public static bool lineLineIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {

        var a = p2 - p1;
        var b = p4 - p3;
        var c = p1 - p3;

        var delta = b.y * a.x - b.x * a.y;

        if( Mathf.Abs(delta) <= 0.00001f) return false;   /////todo?  - does not handle overlapping parallel..
        delta = 1.0f/delta;
        float ua = (b.x * c.y - b.y * c.x) *delta;
        float ub = (a.x * c.y - a.y * c.x) *delta;

        return ua >= 0 && ub >= 0 && ua <= 1 && ub <= 1;
    }

    public static T cast<T>(object o) {
        return (T)o;
    }

}
