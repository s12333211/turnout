using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [field: SerializeField] public List<Stage> Stages { get; private set; } = new List<Stage>();
    [SerializeField] private Transform mapPlane = null;
    [SerializeField] private Transform ignoreParentTransform = null;
    [SerializeField] private Transform startingStationTransform = null;
    [SerializeField] private Transform terminalStationTransform = null;
    [SerializeField] private GameObject goallinePrefab = null;
    [SerializeField] private GameObject coinPrefab = null;
    public int StageNumberNow { get; private set; }
    public int StageNumberMin { get; private set; }
    public int StageNumberMax { get; private set; }
    public int StageNumberLength { get; private set; }
    public Stage StageNow { get; private set; }
    private GameObject goallineObject;
    private List<ConnectionSwitch> enabledConnectionSwitches = new List<ConnectionSwitch>();
    private Dictionary<CurvyConnection, CurvySplineSegment[]> connectionConnectedControlPointsDict = new Dictionary<CurvyConnection, CurvySplineSegment[]>();

    private void OnValidate()
    {
        CheckStageRange();
        RemoveInvalidateObjects();
    }

    public void Awake()
    {
        foreach (var connection in CurvyGlobalManager.Instance.Connections)
        {
            connectionConnectedControlPointsDict.Add(connection, connection.ControlPointsList.ToArray());
        }
        CheckStageRange();
    }

    public void InitConnection()
    {
        enabledConnectionSwitches.Clear();
        foreach (var connection in CurvyGlobalManager.Instance.Connections)
        {
            // 削除したコントロールポイントを戻す
            foreach (var connectedControlPoint in connectionConnectedControlPointsDict[connection])
            {
                if (connectedControlPoint.Connection == null)
                    connection.AddControlPoints(connectedControlPoint);
            }
            // 使わないコントロールポイントを削除
            for (int i = connection.ControlPointsList.Count - 1; i >= 0; i--)
            {
                if (connection.ControlPointsList[i] == null || connection.ControlPointsList[i].enabled == false || connection.ControlPointsList[i].gameObject.activeInHierarchy == false)
                {
                    connection.RemoveControlPoint(connection.ControlPointsList[i], true);
                }
            }
            // Follow-Up自動設定
            var connectionSwitch = connection.GetComponent<ConnectionSwitch>();
            if (connectionSwitch != null)
            {
                if (connectionSwitch.enabled == true)
                    enabledConnectionSwitches.Add(connectionSwitch);
                connectionSwitch.InitSwitch();
                if (connectionSwitch.AvailableDirection.Count > 0)
                {
                    foreach (var controlPoint in connection.ControlPointsList)
                    {
                        if (controlPoint.Spline && controlPoint.Spline.CanControlPointHaveFollowUp(controlPoint))
                        {
                            foreach (var directionPair in connectionSwitch.AvailableDirection)
                            {
                                PathDirection targetDirection = default;
                                if (controlPoint == directionPair.fromDirection.controlPoint)
                                {
                                    foreach (var toDirection in directionPair.toDirections)
                                    {
                                        if (toDirection != directionPair.fromDirection)
                                        {
                                            targetDirection = toDirection;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var toDirection in directionPair.toDirections)
                                    {
                                        if (controlPoint == toDirection.controlPoint)
                                        {
                                            targetDirection = directionPair.fromDirection.Reverse();
                                            break;
                                        }
                                    }
                                }
                                if (targetDirection.controlPoint != null)
                                {
                                    var heading = targetDirection.movementDirection == MovementDirection.Forward ? ConnectionHeadingEnum.Plus : ConnectionHeadingEnum.Minus;
                                    controlPoint.SetFollowUp(targetDirection.controlPoint, heading);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {   // 残るパスは分岐にならないので、Curvyの自動設定に
                    connection.AutoSetFollowUp();
                }
            }
        }
    }

    public void UpdateAllEnabledSwitches()
    {
        foreach (var connectionSwitch in enabledConnectionSwitches)
        {
            connectionSwitch.UpdateSwitch();
        }
    }

    public void LoadStage()
    {
        GetStage();
        var allTransforms = FindObjectsOfTypeAllInScene<Transform>();
        foreach (var tf in allTransforms)
        {
            if (IsIgnored(tf))
                continue;
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(tf.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
            if (StageNow.inactiveGameObjects.Contains(tf.gameObject))
                tf.gameObject.SetActive(false);
            else
                tf.gameObject.SetActive(true);
        }
        var connectionSwitches = CurvyGlobalManager.Instance.GetComponentsInChildren<ConnectionSwitch>();
        foreach (var connectionSwitch in connectionSwitches)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(connectionSwitch, "ステージ" + GetStageNumber() + "読み込み");
#endif
            if (StageNow.enabledSwitchs.Contains(connectionSwitch))
                connectionSwitch.enabled = true;
            else
                connectionSwitch.enabled = false;
        }
        var metaTraps = FindObjectsOfTypeAllInScene<MetaTrap>();
        foreach (var metaTrap in metaTraps)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(metaTrap, "ステージ" + GetStageNumber() + "読み込み");
#endif
            if (StageNow.enabledTraps.Contains(metaTrap))
                metaTrap.enabled = true;
            else
                metaTrap.enabled = false;
            metaTrap.InitTrap();
        }
        var metaStart = FindObjectOfTypeAllInScene<MetaStart>();
        if (metaStart == null || metaStart.ControlPoint != StageNow.startPoint)
        {
            if (metaStart != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(metaStart?.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
                DestoryObject(metaStart);
            }
            if (StageNow.startPoint != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(StageNow.startPoint.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
                metaStart = StageNow.startPoint.gameObject.AddComponent<MetaStart>();
            }
        }
        if (metaStart != null)
            metaStart.movementDirection = StageNow.movementDirection;
        var metaEnd = FindObjectOfTypeAllInScene<MetaEnd>();
        if (metaEnd == null || metaEnd.ControlPoint != StageNow.endPoint)
        {
            if (metaEnd != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(metaEnd?.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
                DestoryObject(metaEnd);
            }
            if (StageNow.endPoint != null)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(StageNow.endPoint.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
                StageNow.endPoint.gameObject.AddComponent<MetaEnd>();
                // ゴールライン生成
                if (Application.isPlaying)
                {
                    if (goallineObject == null)
                        goallineObject = Instantiate(goallinePrefab, StageNow.endPoint.transform);
                    if (goallineObject != null)
                    {
                        goallineObject.transform.SetParent(StageNow.endPoint.transform);
                        goallineObject.transform.SetPositionAndRotation(StageNow.endPoint.transform.position, StageNow.endPoint.GetOrientationFast(0, false, Space.World));
                    }
                }
            }
        }
        RemoveMetaCoins("ステージ" + GetStageNumber() + "読み込み");
        AddMetaCoins(StageNow.coinPoints, "ステージ" + GetStageNumber() + "読み込み");
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(startingStationTransform.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
        startingStationTransform?.SetPositionAndRotation(StageNow.startingStationPosition, StageNow.startingStationRotation);
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(terminalStationTransform.gameObject, "ステージ" + GetStageNumber() + "読み込み");
#endif
        terminalStationTransform?.SetPositionAndRotation(StageNow.terminalStationPosition, StageNow.terminalStationRotation);
        LoadMapPlaneMaterial();
    }

    public void SaveStage()
    {
        GetStage();
        var allTransforms = FindObjectsOfTypeAllInScene<Transform>();
        StageNow.inactiveGameObjects.Clear();
        foreach (var tf in allTransforms)
        {
            if (IsIgnored(tf))
                continue;
            if (tf.gameObject.activeSelf == false)
                StageNow.inactiveGameObjects.Add(tf.gameObject);
        }
        var connectionSwitches = CurvyGlobalManager.Instance.GetComponentsInChildren<ConnectionSwitch>();
        StageNow.enabledSwitchs.Clear();
        foreach (var connectionSwitch in connectionSwitches)
        {
            if (connectionSwitch.enabled == true)
                StageNow.enabledSwitchs.Add(connectionSwitch);
        }
        var metaTraps = FindObjectsOfTypeAllInScene<MetaTrap>();
        StageNow.enabledTraps.Clear();
        foreach (var metaTrap in metaTraps)
        {
            if (metaTrap.enabled == true)
                StageNow.enabledTraps.Add(metaTrap);
        }
        var metaStart = FindObjectOfType<MetaStart>();
        if (metaStart != null)
            SetStart(metaStart);
        var metaEnd = FindObjectOfType<MetaEnd>();
        if (metaEnd != null)
            SetEnd(metaEnd);
        var metaCoins = FindObjectsOfTypeAllInScene<MetaCoin>();
        SetCoins(metaCoins);
        if (startingStationTransform != null)
        {
            StageNow.startingStationPosition = startingStationTransform.position;
            StageNow.startingStationRotation = startingStationTransform.rotation;
        }
        if (terminalStationTransform != null)
        {
            StageNow.terminalStationPosition = terminalStationTransform.position;
            StageNow.terminalStationRotation = terminalStationTransform.rotation;
        }
    }

    public void ClearStage()
    {
        var allTransforms = FindObjectsOfTypeAllInScene<Transform>();
        foreach (var tf in allTransforms)
        {
            if (IsIgnored(tf))
                continue;
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(tf.gameObject, "ステージ設定をクリア");
#endif
            tf.gameObject.SetActive(true);
        }
        var connectionSwitches = CurvyGlobalManager.Instance.GetComponentsInChildren<ConnectionSwitch>();
        foreach (var connectionSwitch in connectionSwitches)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(connectionSwitch, "ステージ設定をクリア");
#endif
            connectionSwitch.enabled = false;
        }
        var allMetaTraps = FindObjectsOfTypeAllInScene<MetaTrap>();
        foreach (var metaTrap in allMetaTraps)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(metaTrap, "ステージ設定をクリア");
#endif
            metaTrap.enabled = false;
        }
        var metaStart = FindObjectOfTypeAllInScene<MetaStart>();
        if (metaStart != null)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(metaStart, "ステージ設定をクリア");
#endif
            DestoryObject(metaStart);
        }
        var metaEnd = FindObjectOfTypeAllInScene<MetaEnd>();
        if (metaEnd != null)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(metaEnd, "ステージ設定をクリア");
#endif
            DestoryObject(metaEnd);
        }
        RemoveMetaCoins("ステージ設定をクリア");
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(startingStationTransform.gameObject, "ステージ設定をクリア");
#endif
        startingStationTransform?.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(terminalStationTransform.gameObject, "ステージ設定をクリア");
#endif
        terminalStationTransform?.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetStart(MetaStart metaStart)
    {
        if (metaStart == null)
            return;
        if (StageNow.startPoint != null && StageNow.startPoint != metaStart.ControlPoint)
        {
            var oldMetaStart = StageNow.startPoint.GetComponent<MetaStart>();
            if (oldMetaStart != null)
                DestoryObject(oldMetaStart);
        }
        StageNow.startPoint = metaStart.ControlPoint;
        StageNow.movementDirection = metaStart.movementDirection;
    }
    public void SetStart(CurvySplineSegment controlPoint)
    {
        if (controlPoint == null)
            return;
        var metaStart = controlPoint.GetComponent<MetaStart>();
        if (metaStart == null)
            metaStart = controlPoint.gameObject.AddComponent<MetaStart>();
        SetStart(metaStart);
    }

    public void SetEnd(MetaEnd metaEnd)
    {
        if (metaEnd == null)
            return;
        if (StageNow.endPoint != null && StageNow.endPoint != metaEnd.ControlPoint)
        {
            var oldMetaEnd = StageNow.endPoint.GetComponent<MetaEnd>();
            if (oldMetaEnd != null)
                DestoryObject(oldMetaEnd);
        }
        StageNow.endPoint = metaEnd.ControlPoint;
    }
    public void SetEnd(CurvySplineSegment controlPoint)
    {
        if (controlPoint == null)
            return;
        var metaEnd = controlPoint.GetComponent<MetaEnd>();
        if (metaEnd == null)
            metaEnd = controlPoint.gameObject.AddComponent<MetaEnd>();
        SetEnd(metaEnd);
    }

    public void SetCoins(List<MetaCoin> metaCoins)
    {
        if (metaCoins == null || metaCoins.Count == 0)
            return;
        var controlPoints = new List<CurvySplineSegment>();
        for (int i = metaCoins.Count - 1; i >= 0; i--)
        {
            if (metaCoins[i] == null)
                metaCoins.RemoveAt(i);
            else
                controlPoints.Add(metaCoins[i].ControlPoint);
        }
        SetCoins(controlPoints);
    }

    public void SetCoins(List<CurvySplineSegment> controlPoints)
    {
        if (controlPoints == null || controlPoints.Count == 0)
            return;
        for (int i = controlPoints.Count - 1; i >= 0; i--)
        {
            if (controlPoints[i] == null)
                controlPoints.RemoveAt(i);
        }
        RemoveMetaCoins("");
        StageNow.coinPoints = controlPoints;
        AddMetaCoins(StageNow.coinPoints, "");
    }

    private void RemoveMetaCoins(string recordName)
    {
        var metaCoins = FindObjectsOfTypeAllInScene<MetaCoin>();
        foreach (var metaCoin in metaCoins)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(metaCoin, recordName);
#endif
            DestoryObject(metaCoin);
        }
    }
    private void AddMetaCoins(List<CurvySplineSegment> coinPoints, string recordName)
    {
        foreach (var controlPoint in coinPoints)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(controlPoint.gameObject, recordName);
#endif
            if (controlPoint != null)
            {
                var metaCoin = controlPoint.gameObject.AddComponent<MetaCoin>();
                metaCoin.coinPrefab = coinPrefab;
            }
        }
    }

    private void LoadMapPlaneMaterial()
    {
        if (mapPlane == null || StageNow.mapMaterial == null)
            return;
        var renderers = mapPlane.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterial = StageNow.mapMaterial;
        }
    }

    private void RemoveInvalidateObjects()
    {
        foreach (var stage in Stages)
        {
            for (int i = stage.inactiveGameObjects.Count - 1; i >= 0; i--)
            {
                if (stage.inactiveGameObjects[i] == null)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(this, "無効なステージ設定を削除");
#endif
                    stage.inactiveGameObjects.RemoveAt(i);
                }
            }
            for (int i = stage.enabledTraps.Count - 1; i >= 0; i--)
            {
                if (stage.enabledTraps[i] == null)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(this, "無効なステージ設定を削除");
#endif
                    stage.enabledTraps.RemoveAt(i);
                }
            }
        }
    }

    public bool SetStageNumber(int stageNumber)
    {
        foreach (var stage in Stages)
        {
            if (stage.stageNumber == stageNumber)
            {
                StageNumberNow = stageNumber;
                GetStage();
                return true;
            }
        }
        return false;
    }

    public void GetStage()
    {
        if (StageNow?.stageNumber == StageNumberNow)
            return;
        StageNow = null;
        foreach (var stage in Stages)
        {
            if (stage.stageNumber == StageNumberNow)
                this.StageNow = stage;
        }
    }

    public string GetStageNumber()
    {
        if (StageNow == null || StageNow.stageNumber != StageNumberNow)
            GetStage();
        if (StageNow != null)
            return StageNow.stageNumber.ToString();
        else
            return StageNumberNow + "(存在しない)";
    }

    public void CheckStageRange()
    {
        StageNumberMin = int.MaxValue;
        StageNumberMax = int.MinValue;
        foreach (var stage in Stages)
        {
            if (StageNumberMax < stage.stageNumber)
                StageNumberMax = stage.stageNumber;
            if (StageNumberMin > stage.stageNumber)
                StageNumberMin = stage.stageNumber;
        }
        StageNumberLength = StageNumberMax - StageNumberMin + 1;
    }

    public bool IsIgnored(Transform tr)
    {
        if (tr.parent == null)
            return tr == ignoreParentTransform;
        else
            return IsIgnored(tr.parent);
    }

    private void DestoryObject<T>(T t) where T : UnityEngine.Object
    {
        if (Application.isPlaying)
            Destroy(t);
        else
            DestroyImmediate(t);
    }

    /// <summary>
    /// シーンのコンポーネント全部取得、非アクティブも含めて
    /// GameObjectは取得できないので、代わりにTransformを取得してgameObjectをアクセスする
    /// </summary>
    public static List<T> FindObjectsOfTypeAllInScene<T>() where T : UnityEngine.Object
    {
        List<T> results = new List<T>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                var allGameObjects = scene.GetRootGameObjects();
                foreach (var go in allGameObjects)
                {
                    results.AddRange(go.GetComponentsInChildren<T>(true));
                }
                break;
            }
        }
        return results;
    }

    /// <summary>
    /// シーンのコンポーネント一つ取得、非アクティブも含めて
    /// GameObjectは取得できないので、代わりにTransformを取得してgameObjectをアクセスする
    /// </summary>
    public static T FindObjectOfTypeAllInScene<T>() where T : UnityEngine.Object
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                var allGameObjects = scene.GetRootGameObjects();
                foreach (var go in allGameObjects)
                {
                    T t = go.GetComponentInChildren<T>(true);
                    if (t != null)
                        return t;
                }
                break;
            }
        }
        return null;
    }
}

[Serializable]
public class Stage : ISerializationCallbackReceiver
{
    [HideInInspector, SerializeField] private string stageString = "";  //UnityのInspectorで要素名として表示するための変数
    public int stageNumber;
    public List<GameObject> inactiveGameObjects;
    public List<ConnectionSwitch> enabledSwitchs;
    public List<MetaTrap> enabledTraps;
    public List<CurvySplineSegment> coinPoints;
    public CurvySplineSegment startPoint;
    public MovementDirection movementDirection;
    public CurvySplineSegment endPoint;
    public Vector3 startingStationPosition;
    public Quaternion startingStationRotation;
    public Vector3 terminalStationPosition;
    public Quaternion terminalStationRotation;
    [Header("このステージの地形のマテリアル")]
    public Material mapMaterial;

    public void OnBeforeSerialize()
    {
        stageString = stageNumber.ToString();
    }
    public void OnAfterDeserialize() { }
}