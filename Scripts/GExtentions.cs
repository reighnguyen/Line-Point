using System;
using System.Collections.Generic;

public static class GExtentions
{
    public static void RemoveRange<T>(this List<T> list, IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
        {
            list.Remove(enumerator.Current);
        }
    }

    public static void Log<T>(this List<T> list, char separator)
    {
        string str = "";
        for (int i = 0; i < list.Count; i++)
        {
            str += (list[i].ToString() + separator);
        }
        UnityEngine.Debug.Log(str);
    }

    public static void Log<T>(this List<T> list, string label, char separator)
    {
        string str = "";
        for (int i = 0; i < list.Count; i++)
        {
            str += (list[i].ToString() + separator);
        }
        UnityEngine.Debug.Log(string.Format("{0} {1}", label, str));
    }

    public static bool Contains<T>(this List<T> list, Func<T,bool> precise)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if(precise(list[i]))
            {
                return true;
            }
        }
        return false;
    }
}
