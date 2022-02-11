using FluffyUnderware.Curvy;
using FluffyUnderware.CurvyEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CurvySplineSegment))]
public class CurvySplineSegmentEditorAddOn : CurvySplineSegmentEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var controlPoint = target as CurvySplineSegment;
        if (Application.isPlaying == false)
        {
            if (controlPoint.Connection != null)
            {
                var connectionSwitch = controlPoint.Connection.GetComponent<ConnectionSwitch>();
                if (connectionSwitch != null)
                {
                    if (connectionSwitch.enabled == true && GUILayout.Button("分岐のスイッチを無効化"))
                    {
                        Undo.RecordObject(connectionSwitch, "分岐のスイッチを無効化");
                        connectionSwitch.enabled = false;
                    }
                    else if (connectionSwitch.enabled == false && GUILayout.Button("分岐のスイッチを有効化"))
                    {
                        Undo.RecordObject(connectionSwitch, "分岐のスイッチを有効化");
                        connectionSwitch.enabled = true;
                    }
                }
            }
        }
    }
}
