using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IEnumerableExt
{
    public static IEnumerable<T> SingleItemAsEnumerable<T>(this T item)
    {
        yield return item;
    }
}
