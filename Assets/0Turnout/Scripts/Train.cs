using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class Train : ConnectedControlPointsSelector
{
    [Header("子のSplineControllerを順番通り繋いで、起点に置く", order = 0)]
    [Header("速度", order = 1)]
    public float maxSpeed = 30;
    [SerializeField] private float maxSpeedBase = 30;
    [SerializeField] private float connectionDistance = 1;
    [SerializeField] private float accelerateRate = 0.4f;   //最大速度まで加速の加速度倍率、1は 1 * maxSpeed m/s²
    [SerializeField] private float goalBrakeDistance = 20;  //ゴール時今の速度からブレーキし始め、止まるまでの距離
    [SerializeField] private bool addJoint = true;
    [SerializeField] private AudioSource trainRunAudioSource = null;
    [SerializeField] private float trainRunSEPitchShift = 0.01f;
    [SerializeField] private AudioSource trainStartAudioSource = null;
    [SerializeField] private ParticleSystem smokeParticleSystem = null;
    [SerializeField] private ParticleSystem getCoinParticleSystem = null;
    [SerializeField] private AudioSource getCoinAudioSource = null;
    [SerializeField] private ObjectEventHandlerBase locomotiveCollisionHandler = null;
    private List<SplineController> splineControllers = new List<SplineController>();   //車輌のスプラインコントローラー（前から順番)
    private Collider colliderFirst = null;
    private TrainState trainState;
    private bool isPausing = false;
    private Dictionary<SplineController, float> splineControllerSpeedDict = new Dictionary<SplineController, float>();
    private Dictionary<SplineController, Rigidbody> splineControllerRigidbodyDict = new Dictionary<SplineController, Rigidbody>();
    private List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
    private CurvySpline splineNow;

    private Subject<Unit> subjectChangeSpline = new Subject<Unit>();
    public IObservable<Unit> OnChangeSpline() => subjectChangeSpline;

    private Subject<Unit> subjectSwitchUsed = new Subject<Unit>();
    public IObservable<Unit> OnSwitchUsed() => subjectSwitchUsed;

    private Subject<Unit> subjectDerail = new Subject<Unit>();
    public IObservable<Unit> OnDerail() => subjectDerail;

    private Subject<Unit> subjectReachEnd = new Subject<Unit>();
    public IObservable<Unit> OnReachEnd() => subjectReachEnd;

    private Subject<Unit> subjectGetCoin = new Subject<Unit>();
    public IObservable<Unit> OnGetCoin() => subjectGetCoin;

    private float brakePower;
    private float acceleratePower;
    private float timeScale;
    const float compressRatio = 0.25f;
    const float springUpOffset = 0.5f;
    const float trainRunPitchBase = 0.4f;
    const float smokeStopTimeAfterDerailed = 5;
    const int connectionSearchLoopLimit = 10;
    const string waterTag = "Water";
    const string coinTag = "Coin";

    public SplineController Locomotive => splineControllers[0];

    private void Awake()
    {
        var childSplineControllers = GetComponentsInChildren<SplineController>();
        splineControllers.AddRange(childSplineControllers);
        foreach (var splineController in childSplineControllers)
        {
            splineControllerRigidbodyDict.Add(splineController, splineController.GetComponent<Rigidbody>());
        }
        colliderFirst = Locomotive.GetComponent<Collider>();
        Locomotive.OnControlPointReached.AddListenerOnce(OnControlPointReached);
        // 汽車のイベント
        locomotiveCollisionHandler.onEnter += objectEvent =>
        {
            // 着水時煙は止まるように
            if (objectEvent.GetGameObject().CompareTag(waterTag))
            {
                smokeParticleSystem.Stop();
            }
            // コイン取得時の処理
            else if (objectEvent.GetGameObject().CompareTag(coinTag))
            {
                subjectGetCoin.OnNext(default);
                getCoinParticleSystem.transform.position = objectEvent.GetGameObject().transform.position;
                getCoinParticleSystem.Play();
                getCoinAudioSource.Play();
                Destroy(objectEvent.GetGameObject());
            }
        };
    }

    public void Init()
    {
        trainState = TrainState.Stop;
        timeScale = 1;
        acceleratePower = maxSpeed * accelerateRate;
        brakePower = Mathf.Pow(maxSpeed, 2) / (2 * goalBrakeDistance);  //デフォルト値、ブレーキ時改めて計算する
        // 起点に移動
        var metaStart = FindObjectOfType<MetaStart>();
        if (metaStart != null)
        {
            splineNow = metaStart.Spline;
            float position = metaStart.Spline.TFToDistance(metaStart.ControlPoint.TF);
            Bounds boundsBackward = default;
            for (int i = splineControllers.Count - 1; i >= 0; i--)
            {
                if (splineControllers[i] != null)
                {
                    splineControllers[i].enabled = true;
                    splineControllers[i].Speed = 0;                             //速度リセット
                    splineControllers[i].UpdateIn = CurvyUpdateMethod.FixedUpdate;
                    splineControllers[i].Spline = metaStart.Spline;
                    splineControllers[i].MovementDirection = metaStart.movementDirection;
                    splineControllerRigidbodyDict[splineControllers[i]].useGravity = false;
                    // アニメーション再生
                    var animator = splineControllers[i].GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.enabled = true;
                    }
                    // ローカルサイズ取得
                    var cargoWagon = splineControllers[i].GetComponent<CargoWagon>();
                    Bounds localBounds;
                    if (cargoWagon != null)
                    {
                        localBounds = cargoWagon.WagonSizeBounds;
                        cargoWagon.Init();  //貨物初期化
                    }
                    else
                        localBounds = splineControllers[i].GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                    // 最後の車輌以外、車輌の半分の長さ前へ移動
                    if (localBounds != null && i != splineControllers.Count - 1)
                        if (metaStart.movementDirection == MovementDirection.Forward)
                        {
                            position += localBounds.extents.z;
                        }
                        else
                        {
                            position -= localBounds.extents.z;
                        }
                    splineControllers[i].AbsolutePosition = position;
                    // 車輌の半分の長さ車両間の長さ前へ移動
                    if (localBounds != null)
                        if (metaStart.movementDirection == MovementDirection.Forward)
                        {
                            position += localBounds.extents.z;
                            position += connectionDistance * compressRatio;
                        }
                        else
                        {
                            position -= localBounds.extents.z;
                            position -= connectionDistance * compressRatio;
                        }

                    // ジョイント生成
                    if (addJoint == true && i < splineControllers.Count - 1)
                    {
                        ConfigurableJoint joint = splineControllers[i].gameObject.GetComponent<ConfigurableJoint>();
                        if (joint == null)
                        {
                            joint = splineControllers[i].gameObject.AddComponent<ConfigurableJoint>();
                            joints.Add(joint);
                        }
                        joint.linearLimit = new SoftJointLimit()
                        {
                            limit = connectionDistance * 1.1f,  //僅かに余裕を持たせる。しないと走行中は常に引っ張ることがある
                            contactDistance = connectionDistance * compressRatio,
                        };
                        joint.xMotion = ConfigurableJointMotion.Limited;
                        joint.yMotion = ConfigurableJointMotion.Limited;
                        joint.zMotion = ConfigurableJointMotion.Limited;
                        joint.anchor = new Vector3(0, springUpOffset, -localBounds.extents.z);
                        joint.autoConfigureConnectedAnchor = false;
                        joint.connectedBody = splineControllerRigidbodyDict[splineControllers[i + 1]];
                        joint.connectedAnchor = new Vector3(0, springUpOffset, boundsBackward.extents.z);
                    }
                    boundsBackward = localBounds;
                }
                else
                {
                    splineControllers.RemoveAt(i);
                }
            }
        }
        Observable.NextFrame().Subscribe(_ =>
        {
            smokeParticleSystem.Play();
        });
    }

    private void FixedUpdate()
    {
        if (trainState == TrainState.Accelerate)
        {
            bool isAllStart = true;
            Bounds localBounds;
            Bounds localBoundsForward = Locomotive.GetComponent<MeshFilter>().sharedMesh.bounds;
            Locomotive.Speed = Mathf.MoveTowards(Locomotive.Speed, maxSpeed, acceleratePower * Time.deltaTime);
            UpdateTrainRunAudio();  //走行音ピッチフェードイン
            for (int i = 1; i < splineControllers.Count; i++)
            {
                var cargoWagon = splineControllers[i].GetComponent<CargoWagon>();
                if (cargoWagon != null)
                    localBounds = cargoWagon.WagonSizeBounds;
                else
                    localBounds = splineControllers[i].GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                //Vector3 position = splineControllers[i].transform.position + splineControllers[i].transform.forward * localBounds.extents.z;
                //Vector3 positionForward = splineControllers[i - 1].transform.position + splineControllers[i - 1].transform.forward * localBoundsForward.extents.z;
                //float distanceBetween = Vector3.Distance(position, positionForward);
                //float distanceLimit = connectionDistance;
                float distanceBetween = GetDistanceBetweenSplineController(splineControllers[i], splineControllers[i - 1]);
                float distanceLimit = localBounds.extents.z + connectionDistance + localBoundsForward.extents.z;
                if (distanceBetween > distanceLimit)
                {
                    splineControllers[i].Speed = Locomotive.Speed;
                    if (splineControllers[i].MovementDirection == MovementDirection.Forward)
                        splineControllers[i].AbsolutePosition += distanceBetween - distanceLimit;
                    else
                        splineControllers[i].AbsolutePosition -= distanceBetween - distanceLimit;
                }
                if (Mathf.Abs(splineControllers[i].Speed) != maxSpeed)
                    isAllStart = false;
                localBoundsForward = localBounds;
            }
            if (isAllStart)
            {
                trainState = TrainState.Run;
                UpdateTrainRunAudio();
            }
        }
        else if (trainState == TrainState.Brake)
        {
            bool isAllEnd = true;
            Bounds localBounds;
            Bounds localBoundsForward = Locomotive.GetComponent<MeshFilter>().sharedMesh.bounds;
            Locomotive.Speed = Mathf.MoveTowards(Locomotive.Speed, 0, brakePower * Time.deltaTime);
            UpdateTrainRunAudio();  //走行音ピッチフェードアウト
            for (int i = 1; i < splineControllers.Count; i++)
            {
                var cargoWagon = splineControllers[i].GetComponent<CargoWagon>();
                if (cargoWagon != null)
                    localBounds = cargoWagon.WagonSizeBounds;
                else
                    localBounds = splineControllers[i].GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                if (splineControllers[i].Speed != 0)
                {
                    float distanceBetween = GetDistanceBetweenSplineController(splineControllers[i], splineControllers[i - 1]);
                    float distanceLimit = localBounds.extents.z + connectionDistance * compressRatio + localBoundsForward.extents.z;
                    if (distanceBetween < distanceLimit)
                    {
                        splineControllers[i].Speed = Locomotive.Speed;
                    }
                    if (splineControllers[i].Speed != 0)
                        isAllEnd = false;
                }
                localBoundsForward = localBounds;
            }
            if (isAllEnd)
            {
                trainState = TrainState.Stop;
                UpdateTrainRunAudio();
            }
        }
    }

    public void Play()
    {
        trainState = TrainState.Accelerate;
        // 初速
        Locomotive.Speed = Time.fixedDeltaTime * acceleratePower;
        trainRunAudioSource.Play();
        UpdateTrainRunAudio();
        trainStartAudioSource.Play();
    }

    public void Pause()
    {
        if (isPausing)
            return;
        isPausing = true;
        trainRunAudioSource.Pause();
        foreach (var splineController in splineControllers)
        {
            splineControllerSpeedDict.Add(splineController, splineController.Speed);
            splineController.Speed = 0;
        }
    }

    public void Resume()
    {
        if (!isPausing)
            return;
        isPausing = false;
        trainRunAudioSource.UnPause();
        foreach (var splineController in splineControllers)
        {
            splineController.Speed = splineControllerSpeedDict[splineController];
        }
        splineControllerSpeedDict.Clear();
    }

    public void Derail(float upVelocityPower)
    {
        if (trainState == TrainState.Derail)
            return;
        trainState = TrainState.Derail;
        subjectDerail.OnNext(default);

        trainRunAudioSource.Stop();
        foreach (var splineController in splineControllers)
        {
            // 走行しない
            splineController.enabled = false;
            // SplineControllerの速度をRigidbodyに変換
            var velocity = splineController.transform.forward * maxSpeed + Vector3.up * upVelocityPower;
            var rigidbody = splineControllerRigidbodyDict[splineController];
            if (rigidbody != null)
            {
                rigidbody.useGravity = true;
                rigidbody.velocity = velocity;
                //rigidbody.AddForce(splineController.transform.forward * maxSpeed, ForceMode.VelocityChange);
                //rigidbody.AddForce(Vector3.up * upVelocityPower, ForceMode.VelocityChange);
            }
            // 貨物にも速度を与える
            var cargoWagon = splineController.GetComponent<CargoWagon>();
            if (cargoWagon != null)
                cargoWagon.Depart(velocity);
            // アニメーション再生停止
            var animator = splineController.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
            // 煙は数秒後秒後止まる
            float timer = 0;
            this.UpdateAsObservable().TakeWhile(_ => trainState == TrainState.Derail).TakeWhile(_ => timer < smokeStopTimeAfterDerailed).Subscribe(_ =>
            {
                timer += Time.deltaTime;
            }, () =>
            {
                smokeParticleSystem.Stop();
            });
        }
    }

    public void SlowMotion(float timeScale)
    {
        this.timeScale = timeScale;
        UpdateTrainRunAudio();
    }

    private void UpdateTrainRunAudio()
    {
        if (Locomotive.Speed > 0)
            trainRunAudioSource.pitch = trainRunPitchBase + (Locomotive.Speed / maxSpeed * (1 + (maxSpeed - maxSpeedBase) * trainRunSEPitchShift - trainRunPitchBase)) * timeScale;
        else
            trainRunAudioSource.pitch = 0;
    }

    private void OnControlPointReached(CurvySplineMoveEventArgs e)
    {
        if (splineNow != e.Spline)
        {
            splineNow = e.Spline;
            subjectChangeSpline.OnNext(default);
        }
        if (e.ControlPoint.GetMetadata<MetaEnd>())
        {
            trainState = TrainState.Brake;
            brakePower = Mathf.Pow(Locomotive.Speed, 2) / (2 * goalBrakeDistance);
            subjectReachEnd.OnNext(default);
            // アニメーション再生停止
            foreach (var splineController in splineControllers)
            {
                var animator = splineController.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        subjectChangeSpline.OnCompleted();
        subjectSwitchUsed.OnCompleted();
        subjectDerail.OnCompleted();
        subjectReachEnd.OnCompleted();
        subjectGetCoin.OnCompleted();
    }

    /// <summary>
    /// SplineControllerのカスタム分岐方向変更機能に対応用
    /// このコンポーネントをConnections handlingのカスタムタイプのCustomSelectorに設定
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="connection"></param>
    /// <param name="currentControlPoint"></param>
    /// <returns></returns>
    public override CurvySplineSegment SelectConnectedControlPoint(SplineController caller, CurvyConnection connection, CurvySplineSegment currentControlPoint)
    {
        // ConnectionSwitchhありの分岐点処理
        var connectionSwitch = connection.GetComponent<ConnectionSwitch>();
        if (connectionSwitch != null)
        {
            // 汽車の処理
            if (caller == Locomotive)
            {
                if (connectionSwitch.FromDirectionNow.controlPoint == currentControlPoint && connectionSwitch.FromDirectionNow.movementDirection == caller.MovementDirection)
                {
                    connectionSwitch.SetSwitchUnusableUntilNextConnectedControlPoint();
                    caller.MovementDirection = connectionSwitch.ToDirectionNow.movementDirection;
                    if (caller == Locomotive)
                    {
                        Observable.NextFrame().Subscribe(_ =>
                        {
                            subjectSwitchUsed.OnNext(default);
                        }).AddTo(gameObject);
                    }
                    return connectionSwitch.ToDirectionNow.controlPoint;
                }
            }
            // 後車は前車の方向に追う
            else
            {
                for (int i = 1; i < splineControllers.Count; i++)
                {
                    if (splineControllers[i] == caller)
                    {
                        // 車両自身いる線路を確認
                        PathDirection pathDirectionThisCarriage = new PathDirection(caller, currentControlPoint);
                        foreach (var pathDirectionTuple in connectionSwitch.AvailableDirection)
                        {
                            if (pathDirectionTuple.fromDirection == pathDirectionThisCarriage)
                            {
                                // 分岐先に前方の車輌に届けるかを確認
                                PathDirection pathDirectionFrontCarriage = new PathDirection(splineControllers[i - 1]);
                                foreach (var toDirection in pathDirectionTuple.toDirections)
                                {
                                    if (pathDirectionFrontCarriage.CouldBeReachedWithoutSwitch(toDirection, 3)) //3つControlPoint内見つけないと諦める
                                    {
                                        caller.MovementDirection = toDirection.movementDirection;
                                        return toDirection.controlPoint;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
        // ConnectionSwitch無しの分岐点処理
        if (currentControlPoint.FollowUp != null)
        {
            switch (currentControlPoint.FollowUpHeading.ResolveAuto(currentControlPoint.FollowUp))
            {
                case ConnectionHeadingEnum.Minus:
                    caller.MovementDirection = MovementDirection.Backward;
                    break;
                case ConnectionHeadingEnum.Sharp:
                    break;
                case ConnectionHeadingEnum.Plus:
                    caller.MovementDirection = MovementDirection.Forward;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
            return currentControlPoint.FollowUp;
        }
        return currentControlPoint;
    }

    private float GetDistanceBetweenSplineController(SplineController splineControllerFrom, SplineController splineControllerTo)
    {
        float distance = 0;
        if (splineControllerFrom.Spline == splineControllerTo.Spline)
        {
            distance = (splineControllerFrom.MovementDirection == MovementDirection.Forward ? 1 : -1) * (splineControllerTo.AbsolutePosition - splineControllerFrom.AbsolutePosition);
        }
        else
        {   // この方法はFromから進んでToへ移動する場合のみ距離計算できる、逆に設定すると計算不能に
            PathDirection pathDirectionFrom = new PathDirection(splineControllerFrom);
            float position = splineControllerFrom.AbsolutePosition;
            for (int loopCount = 0; pathDirectionFrom.controlPoint.Spline != splineControllerTo.Spline; loopCount++)
            {
                if (loopCount >= connectionSearchLoopLimit || pathDirectionFrom.controlPoint == null)
                {
                    Debug.LogError("車両間の距離計算不能！");
                    return -1;
                }
                var connection = pathDirectionFrom.GetNextConnection(true, true, 3);      //次のパスの繋がりまで進む
                distance += (pathDirectionFrom.movementDirection == MovementDirection.Forward ? 1 : -1) * (pathDirectionFrom.controlPoint.Distance - position);
                if (pathDirectionFrom.controlPoint.FollowUp != null)
                    pathDirectionFrom = pathDirectionFrom.GetNextPathDirection();   //パス乗り換え
                position = pathDirectionFrom.controlPoint.Distance;
            }
            distance += (splineControllerTo.MovementDirection == MovementDirection.Forward ? 1 : -1) * (splineControllerTo.AbsolutePosition - position);
        }
        if (distance > 0)
            return distance;
        Debug.LogError("車両間の距離がマイナス値！");
        return -1;
    }

    public bool IsReachableWithoutSwitch(PathDirection pathDirection)
    {
        if (pathDirection.controlPoint == null)
            return false;
        return pathDirection.CouldBeReachedWithoutSwitch(new PathDirection(Locomotive));
    }

    public enum TrainState
    {
        Stop,
        Run,
        Accelerate,
        Brake,
        Derail,
    }
}