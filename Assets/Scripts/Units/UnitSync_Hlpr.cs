using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

public class Unit_SyncHelper : MonoBehaviour {



    [HideInInspector]
    public Transform Trnsfrm;
    [HideInInspector]
    public Rigidbody2D Body;

    [HideInInspector]
    public bool PathActive = false;
    [HideInInspector]
    public int SPi;

    void Awake() {

        Trnsfrm = transform;
       // Body = gameObject.AddComponent<Rigidbody2D>();

    }
}


public static class ExtensionMethods {

    //http://answers.unity3d.com/questions/530178/how-to-get-a-component-from-an-object-and-add-it-t.html
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
     {
         Type type = comp.GetType();
         if (type != other.GetType()) return null; // type mis-match
         BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
         PropertyInfo[] pinfos = type.GetProperties(flags);
         foreach (var pinfo in pinfos) {
             if (pinfo.CanWrite) {
                 try {
                     pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                 }
                 catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
             }
         }
         FieldInfo[] finfos = type.GetFields(flags);
         foreach (var finfo in finfos) {
             finfo.SetValue(comp, finfo.GetValue(other));
         }
         return comp as T;
     }

    public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }
}