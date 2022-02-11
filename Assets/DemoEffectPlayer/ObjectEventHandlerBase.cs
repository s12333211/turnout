using UnityEngine;

public abstract class ObjectEventHandlerBase : MonoBehaviour
{
    public System.Action<ObjectEvent> onEnter = delegate { };
    public System.Action<ObjectEvent> onStay = delegate { };
    public System.Action<ObjectEvent> onExit = delegate { };
}

public interface ObjectEvent
{
    GameObject GetGameObject();
    Transform GetTransform();
    Bounds GetBounds();
    void AddForce(Vector3 force, ForceMode forceMode);
}

public class ColliderEvent : ObjectEvent
{
    private readonly Collider collider;
    public ColliderEvent(Collider collider) => this.collider = collider;
    public GameObject GetGameObject() => collider.gameObject;
    public Transform GetTransform() => collider.transform;
    public Bounds GetBounds() => collider.bounds;
    public void AddForce(Vector3 force, ForceMode forceMode)
    {
        if (collider.attachedRigidbody != null)
        {
            collider.attachedRigidbody.constraints = RigidbodyConstraints.None;
            collider.attachedRigidbody.AddForce(force, forceMode);
        }
    }
}

public class Collider2DEvent : ObjectEvent
{
    private readonly Collider2D collider;
    public Collider2DEvent(Collider2D collider) => this.collider = collider;
    public GameObject GetGameObject() => collider.gameObject;
    public Transform GetTransform() => collider.transform;
    public Bounds GetBounds() => collider.bounds;
    public void AddForce(Vector3 force, ForceMode forceMode)
    {
        if (collider.attachedRigidbody != null)
        {
            collider.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
            switch (forceMode)
            {
                case ForceMode.Force:
                    collider.attachedRigidbody.AddForce(force, ForceMode2D.Force);
                    break;
                case ForceMode.Acceleration:
                    collider.attachedRigidbody.AddForce(force * collider.attachedRigidbody.mass, ForceMode2D.Force);
                    break;
                case ForceMode.Impulse:
                    collider.attachedRigidbody.AddForce(force, ForceMode2D.Impulse);
                    break;
                case ForceMode.VelocityChange:
                    collider.attachedRigidbody.AddForce(force * collider.attachedRigidbody.mass, ForceMode2D.Impulse);
                    break;
            }
        }
    }
}