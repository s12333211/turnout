using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHandler : ObjectEventHandlerBase
{
    [Header("UIへのマウスイベントは無視する")]
    public bool ignoreClickOnGUI = true;
    private Collider[] colliders = null;
    private Collider2D[] collider2Ds = null;
    private bool buttonDown = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider[] colliders = this.GetComponents<Collider>();
        Collider2D[] collider2Ds = this.GetComponents<Collider2D>();
        if (colliders.Length == 0 && collider2Ds.Length == 0)
            Debug.LogAssertion("MouseHandler needs Collider.", this);
    }
#endif

    void Awake()
    {
        colliders = GetComponents<Collider>();
        collider2Ds = GetComponents<Collider2D>();
    }

    void Update()
    {
        // UIへのマウスイベントは無視する設定の処理
        if (ignoreClickOnGUI && EventSystem.current?.IsPointerOverGameObject(-1) == true)
        {
            // 既に押しっぱなしなら離す処理
            if (buttonDown == true)
            {
                buttonDown = false;
                foreach (var collider in colliders)
                {
                    onExit(new ColliderEvent(collider));
                }
                foreach (var collider2D in collider2Ds)
                {
                    onExit(new Collider2DEvent(collider2D));
                }
            }
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            buttonDown = true;
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (var collider in colliders)
            {
                if (collider.Raycast(mouseRay, out var hit, Mathf.Infinity))
                    onEnter(new ColliderEvent(collider));
            }
            foreach (var collider2D in collider2Ds)
            {
                if (collider2D.OverlapPoint(mouseRay.origin + mouseRay.direction * (collider2D.bounds.center.z - mouseRay.origin.z) / mouseRay.direction.z))
                {
                    onEnter(new Collider2DEvent(collider2D));
                    break;
                }
            }

        }
        else if (Input.GetMouseButton(0) && buttonDown == true)
        {
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (var collider in colliders)
            {
                if (collider.Raycast(mouseRay, out var hit, Mathf.Infinity))
                    onStay(new ColliderEvent(collider));
            }
            foreach (var collider2D in collider2Ds)
            {
                if (collider2D.OverlapPoint(mouseRay.origin + mouseRay.direction * (collider2D.bounds.center.z - mouseRay.origin.z) / mouseRay.direction.z))
                {
                    onStay(new Collider2DEvent(collider2D));
                    break;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0) && buttonDown == true)
        {
            buttonDown = false;
            foreach (var collider in colliders)
            {
                onExit(new ColliderEvent(collider));
            }
            foreach (var collider2D in collider2Ds)
            {
                onExit(new Collider2DEvent(collider2D));
            }
        }
    }
}