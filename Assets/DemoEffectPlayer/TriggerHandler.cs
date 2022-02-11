using UnityEngine;

public class TriggerHandler : ObjectEventHandlerBase
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider[] colliders = this.GetComponents<Collider>();
        Collider2D[] collider2Ds = this.GetComponents<Collider2D>();
        if (colliders.Length == 0 && collider2Ds.Length == 0)
            Debug.LogAssertion("TriggerHandler needs Collider.", this);
        // Collider of this object does not need to set isTrigger flag.
    }
#endif

    void OnTriggerEnter(Collider other)
    {
        onEnter(new ColliderEvent(other));
    }

    void OnTriggerStay(Collider other)
    {
        onStay(new ColliderEvent(other));
    }

    void OnTriggerExit(Collider other)
    {
        onExit(new ColliderEvent(other));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        onEnter(new Collider2DEvent(other));
    }

    void OnTriggerStay2D(Collider2D other)
    {
        onStay(new Collider2DEvent(other));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        onExit(new Collider2DEvent(other));
    }
}