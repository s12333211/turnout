using UnityEngine;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System.Collections.Generic;

[RequireComponent(typeof(CurvyConnection))]
public class ConnectionSwitch : MonoBehaviour
{
    private Train train;
    [SerializeField] private Switch switchPrefab = null;
    private Switch switchObject;
    public PathDirection FromDirectionNow { get; private set; }     //汽車が来る方向
    private int fromDirectionIndex = 0;
    public PathDirection ToDirectionNow { get; private set; }       //分岐先の方向
    private int toDirectionIndex = 0;
    public List<(PathDirection fromDirection, List<PathDirection> toDirections)> AvailableDirection { get; private set; } = new List<(PathDirection, List<PathDirection>)>();
    public CurvyConnection Connection { get; private set; }
    private CurvyConnection lastestReachedConnection;
    public bool IsUsable { get; private set; } = true;

    const float switchAngleLimit = 30f;

    protected void Awake()
    {
        Connection = GetComponent<CurvyConnection>();
        train = FindObjectOfType<Train>();
    }

    private void Start() { }    //enabled状態を表示させるために

    public void InitSwitch()
    {
        FromDirectionNow = new PathDirection(null, MovementDirection.Forward);
        ToDirectionNow = new PathDirection(null, MovementDirection.Forward);
        lastestReachedConnection = null;
        RefreshAvailableDirection();
        if (enabled == true && AvailableDirection.Count > 0)
        {
            train?.Locomotive.OnControlPointReached.AddListenerOnce(OnCPReached);
            if (switchObject == null)
                switchObject = Instantiate(switchPrefab, transform);
        }
        else
        {
            train?.Locomotive.OnControlPointReached.RemoveListener(OnCPReached);
            if (switchObject != null)
            {
                Destroy(switchObject.gameObject);
                switchObject = null;
            }
        }
    }

    public void OnCPReached(CurvySplineMoveEventArgs e)
    {
        // ConntectedControlPointを確認
        if (e.ControlPoint.Connection != null)
        {
            // スプライン切り替えた場合(同じConnectionのもう一つのポイントでのイベント)、その後スイッチの更新を行う
            if (lastestReachedConnection == e.ControlPoint.Connection)
            {
                UpdateSwitch();
            }
            // スプライン切り替えしない場合
            else
            {
                lastestReachedConnection = e.ControlPoint.Connection;
                // スイッチの更新を行う    //スプライン切り替える場合は更新しない、切り替えた後に更新
                var connectionSwitch = e.ControlPoint.Connection.GetComponent<ConnectionSwitch>();
                if (connectionSwitch != null && connectionSwitch.ToDirectionNow.controlPoint == e.ControlPoint)
                    UpdateSwitch();
            }
        }
    }

    public void UseSwitch()
    {
        if (IsUsable == false)
            return;
        if (AvailableDirection.Count > 0 && FromDirectionNow.controlPoint != null)
        {
            toDirectionIndex++;
            if (toDirectionIndex >= AvailableDirection[fromDirectionIndex].toDirections.Count)
                toDirectionIndex = 0;
            ToDirectionNow = AvailableDirection[fromDirectionIndex].toDirections[toDirectionIndex];
        }
        else
        {
            InitDirection();
        }
        ShowDirection();
    }

    public void UpdateSwitch()
    {
        if (IsUsable == false)
        {
            IsUsable = true;
        }
        InitDirection();
        ShowDirection();
    }

    private void InitDirection()
    {
        if (AvailableDirection.Count > 0)
        {
            // 汽車の進む方向と合わない場合 
            if (!train.IsReachableWithoutSwitch(FromDirectionNow))
            {
                var newFromDirectionIndex = -1;
                // 汽車が来る方向を確認
                if (AvailableDirection.Count >= 2)
                {
                    for (int i = 0; i < AvailableDirection.Count; i++)
                    {
                        if (train.IsReachableWithoutSwitch(AvailableDirection[i].fromDirection))
                        {
                            newFromDirectionIndex = i;
                            break;
                        }
                    }
                }
                else
                    newFromDirectionIndex = 0;
                if (newFromDirectionIndex != -1)
                    fromDirectionIndex = newFromDirectionIndex;
                // 分岐先はデフォルトランダム方向
                toDirectionIndex = Random.Range(0, AvailableDirection[fromDirectionIndex].toDirections.Count);
                // 今の分岐先方向と同じ方向があればその方向を使う
                for (int i = 0; i < AvailableDirection[fromDirectionIndex].toDirections.Count; i++)
                {
                    if (ToDirectionNow == AvailableDirection[fromDirectionIndex].toDirections[i])
                    {
                        toDirectionIndex = i;
                        break;
                    }
                }
                FromDirectionNow = AvailableDirection[fromDirectionIndex].fromDirection;
            }
            ToDirectionNow = AvailableDirection[fromDirectionIndex].toDirections[toDirectionIndex];
        }
        else
        {
            FromDirectionNow = new PathDirection(null, MovementDirection.Forward);
            ToDirectionNow = new PathDirection(null, MovementDirection.Forward);
        }
    }

    public void SetSwitchUnusableUntilNextConnectedControlPoint()
    {
        IsUsable = false;
    }

    public void SetFocus(bool state)
    {
        switchObject.SetFocus(state);
    }

    public void ShowDirection()
    {
        if (ToDirectionNow.controlPoint != null)
        {
            //var controlPoints = new List<CurvySplineSegment>();
            //controlPoints.Add(ToDirectionNow.controlPoint);
            //var nextPathDirection = ToDirectionNow.GetNextPathDirection();
            //controlPoints.Add(nextPathDirection.controlPoint);
            //var next2PathDirection = nextPathDirection.GetNextPathDirection();
            //controlPoints.Add(next2PathDirection.controlPoint);
            switchObject.SetDirection(ToDirectionNow);
        }
    }

    public void RefreshAvailableDirection()
    {
        if (Connection == null)
            Connection = GetComponent<CurvyConnection>();
        if (Connection == null)
            return;
        // 全ての線路の方向を集める
        List<(PathDirection direction, Quaternion orientation)> candidates = new List<(PathDirection, Quaternion)>();
        foreach (var connectedControlPoint in Connection.ControlPointsList)
        {
            if (connectedControlPoint.Spline.Closed == true || connectedControlPoint.IsLastControlPoint == false)
            {
                candidates.Add((new PathDirection(connectedControlPoint, MovementDirection.Forward), connectedControlPoint.GetOrientationFast(0, false)));
            }
            if (connectedControlPoint.Spline.Closed == true || connectedControlPoint.IsFirstControlPoint == false)
            {
                candidates.Add((new PathDirection(connectedControlPoint, MovementDirection.Backward), connectedControlPoint.GetOrientationFast(0, true)));
            }
        }
        // 分岐がある線路を確認
        AvailableDirection.Clear();
        for (int i = 0; i < candidates.Count; i++)
        {
            List<PathDirection> pathDirections = new List<PathDirection>();
            for (int j = 0; j < candidates.Count; j++)
            {
                if (i == j)
                    continue;
                // 線路の方向変更一定角度内の場合、分岐できることを記録
                if (Mathf.Abs(Quaternion.Angle(candidates[i].orientation, candidates[j].orientation) - 180f) < switchAngleLimit)
                {
                    pathDirections.Add(candidates[j].direction);
                }
            }
            // 2分岐以上の場合
            if (pathDirections.Count >= 2)
            {
                AvailableDirection.Add((candidates[i].direction.Reverse(), pathDirections));
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isActiveAndEnabled == false)
            return;
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.5f);
        Gizmos.DrawSphere(transform.position, 3f);
        GUIStyle gUIStyle = new GUIStyle();
        // テキストサイズ
        float zoom = Camera.current.orthographic == true ? Camera.current.orthographicSize : Vector3.Distance(Camera.current.transform.position, transform.position) / 2;
        gUIStyle.fontSize = Mathf.FloorToInt(512 / zoom);
        // 中央に寄せるための設定
        gUIStyle.fixedWidth = 1;
        gUIStyle.fixedHeight = 1;
        gUIStyle.alignment = TextAnchor.MiddleCenter;
        // テキスト色
        gUIStyle.normal.textColor = new Color(0f, 1f, 0f, 0.8f);
        UnityEditor.Handles.Label(transform.position, "分岐", gUIStyle);
    }
#endif
}