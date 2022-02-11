using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CurvyGlobalManager))]
public class CurvyGlobalManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var curvyGlobalManager = target as CurvyGlobalManager;
        if (Application.isPlaying == false)
        {
            if (GUILayout.Button("分岐点にSwitch追加、Follow-Up初期化"))
            {
                foreach (var connection in curvyGlobalManager.Connections)
                {
                    // 分岐点にConnectionSwitch追加
                    ConnectionSwitch connectionSwitch = connection.GetComponent<ConnectionSwitch>();
                    if (connectionSwitch == null)
                    {
                        Undo.RecordObject(connection, "分岐点設定");
                        connectionSwitch = ObjectFactory.AddComponent<ConnectionSwitch>(connection.gameObject);
                    }
                    // Follow-Up初期化とコントロールポイントの位置同期
                    foreach (var controlPoint in connection.ControlPointsList)
                    {
                        Undo.RecordObject(controlPoint, "分岐点設定");
                        controlPoint.SetFollowUp(null);
                        controlPoint.SetPosition(connection.ControlPointsList[0].transform.position);   ///位置は少しずれがあっても<see cref="CurvyConnection.AutoSetFollowUp"/>が効かない
                    }
                }
            }
        }
    }
}
