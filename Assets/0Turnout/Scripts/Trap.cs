using FluffyUnderware.Curvy.Controllers;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Trap : MonoBehaviour
{
    [field: SerializeField, Header("汽車の上へ速度")] public float onTriggerUpVelocity { get; private set; } = 0;

    private void OnTriggerEnter(Collider other)
    {
        var splineController = other.GetComponent<SplineController>();
        if (splineController != null)
        {
            var train = splineController.transform.parent.GetComponent<Train>();
            if (train != null)
                train.Derail(onTriggerUpVelocity);
        }
    }
}