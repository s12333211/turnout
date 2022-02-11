using FluffyUnderware.Curvy;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageManager))]
public class StageManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var stageManager = target as StageManager;
        if (Application.isPlaying == false)
        {
            GUILayout.Space(10);
            if (GUILayout.Button("ステージ" + stageManager.GetStageNumber() + "に保存"))
            {
                stageManager.SaveStage();
            }
            GUILayout.Space(10);
            // ステージの選択
            Rect position = EditorGUILayout.GetControlRect(false, 2 * EditorGUIUtility.singleLineHeight);
            position.height *= 0.5f;
            var stageNumber = (int)EditorGUI.Slider(position, "ステージ選択", stageManager.StageNumberNow, stageManager.StageNumberMin, stageManager.StageNumberMax);
            stageManager.SetStageNumber(stageNumber);
            // ステージ選択のサブラベル
            position.y += position.height;
            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth + 54; //54 seems to be the width of the slider's float field
            GUIStyle style = GUI.skin.label;
            style.alignment = TextAnchor.UpperLeft; EditorGUI.LabelField(position, stageManager.StageNumberMin.ToString(), style);
            style.alignment = TextAnchor.UpperRight; EditorGUI.LabelField(position, stageManager.StageNumberMax.ToString(), style);
            GUILayout.Space(10);
            if (GUILayout.Button("ステージ" + stageManager.GetStageNumber() + "を読み込み"))
            {
                stageManager.LoadStage();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("ステージ設定をクリア"))
            {
                stageManager.ClearStage();
            }
        }
    }
}
