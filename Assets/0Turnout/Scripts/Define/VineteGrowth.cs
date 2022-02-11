using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MasterData/VineteGrowth")]
public class VineteGrowth : ScriptableObject
{
    [Header("必要コイン")]
    public int needCoin;

    [Header("成長する場所")]
    public Vector2Int location = new Vector2Int();
}
