using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VineteTile : MonoBehaviour
{
    [SerializeField] private VineteObj vinete;

    public void Grow(System.Action callback) {
        int growth = vinete.GetGrowth();
        vinete.Grow(growth + 1, () => {
            callback();
        });
    }

    public void SetGrowth(int growth) {
        vinete.SetGrowth(growth);
    }
}
