using UnityEngine;

public class CollisionAndTriggerHandler : ObjectEventHandlerBase
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider[] colliders = this.GetComponents<Collider>();
        Collider2D[] collider2Ds = this.GetComponents<Collider2D>();
        if (colliders.Length == 0 && collider2Ds.Length == 0)
            Debug.LogAssertion("CollisionHandler needs Collider.", this);
        else
        {
            bool isNonTriggerExists = false;
            if (colliders.Length > 0)
            {
                foreach (Collider collider in colliders)
                {
                    isNonTriggerExists = !collider.isTrigger;
                    if (isNonTriggerExists)
                    {
                        break;
                    }
                }
            }
            else if (collider2Ds.Length > 0)
            {
                foreach (Collider2D collider in collider2Ds)
                {
                    isNonTriggerExists = !collider.isTrigger;
                    if (isNonTriggerExists)
                    {
                        break;
                    }
                }
            }
            if (!isNonTriggerExists)
            {
                Debug.LogAssertion("At least one Collider needs to NOT set isTrigger flag.", this);
            }
        }
    }
#endif

    void OnCollisionEnter(Collision collision)
    {
        onEnter(new ColliderEvent(collision.collider));
    }

    void OnCollisionStay(Collision collision)
    {
        onStay(new ColliderEvent(collision.collider));
    }

    void OnCollisionExit(Collision collision)
    {
        onExit(new ColliderEvent(collision.collider));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        onEnter(new Collider2DEvent(collision.collider));
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        onStay(new Collider2DEvent(collision.collider));
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        onExit(new Collider2DEvent(collision.collider));
    }

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