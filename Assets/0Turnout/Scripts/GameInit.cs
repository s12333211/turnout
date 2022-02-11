using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        CreateDontDestroyObject();
    }

    private static void CreateDontDestroyObject()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("DontDestroy");
        if (prefabs.Length <= 0) {
            return;
        }
        foreach (GameObject prefab in prefabs) {
            GameObject ins = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(ins);
        }
    }
}
