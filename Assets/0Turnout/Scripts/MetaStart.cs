using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine;

public class MetaStart : CurvyMetadataBase
{
    public MovementDirection movementDirection = MovementDirection.Forward;

    private CurvySplineSegment GetTargetControlPoint()
    {
        if (movementDirection == MovementDirection.Forward)
        {
            return ControlPoint.Spline.GetNextControlPoint(ControlPoint);
        }
        else
        {
            return ControlPoint.Spline.GetPreviousControlPoint(ControlPoint);
        }
    }

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
        gUIStyle.normal.textColor = new Color(0f, 1f, 0f, 1f);
        UnityEditor.Handles.Label(transform.position, "起点", gUIStyle);

        // 進む方向
        if (ControlPoint != null)
        {
            var targetControlPoist = GetTargetControlPoint();
            if (targetControlPoist != null)
            {
                Vector3 vector = (targetControlPoist.transform.position - transform.position).normalized;
                Gizmos.color = new Color(0f, 0.1f, 1f, 0.9f);
                Gizmos.DrawRay(transform.position + Vector3.up, vector * 8);
                Gizmos.DrawRay(transform.position + Vector3.up + vector * 8, Quaternion.Euler(0, 135, 0) * vector * 2);
                Gizmos.DrawRay(transform.position + Vector3.up + vector * 8, Quaternion.Euler(0, -135, 0) * vector * 2);
            }
        }
    }
#endif
}