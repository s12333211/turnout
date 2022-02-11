using FluffyUnderware.Curvy;
using UnityEngine;

public class MetaEnd : CurvyMetadataBase
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isActiveAndEnabled == false)
            return;
        Gizmos.color = new Color(0.4f, 0.4f, 1f, 0.5f);
        Gizmos.DrawSphere(transform.position, 5f);
        GUIStyle gUIStyle = new GUIStyle();
        // テキストサイズ
        float zoom = Camera.current.orthographic == true ? Camera.current.orthographicSize : Vector3.Distance(Camera.current.transform.position, transform.position) / 2;
        gUIStyle.fontSize = Mathf.FloorToInt(512 / zoom);
        // 中央に寄せるための設定
        gUIStyle.fixedWidth = 1;
        gUIStyle.fixedHeight = 1;
        gUIStyle.alignment = TextAnchor.MiddleCenter;
        // テキスト色
        gUIStyle.normal.textColor = new Color(1f, 0f, 0f, 1f);
        UnityEditor.Handles.Label(transform.position, "終点", gUIStyle);
    }
#endif
}