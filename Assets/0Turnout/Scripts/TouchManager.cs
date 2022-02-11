using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;
using System;

public class TouchManager : MonoBehaviour
{
    public Dictionary<int, TouchData> TouchDataDict { get; private set; } = new Dictionary<int, TouchData>();
    [Header("GUI以外開始したタッチを記録する")]
    public bool doGetTouchNotBeganInGUI = true;
    [Header("GUIで開始したタッチを記録する")]
    public bool doGetTouchBeganInGUI = false;
    [Header("------------------------------", order = 0), Space(-10, order = 1)]
    [Header("タップ操作を有効する", order = 3)]
    public bool enableTapControl = true;
    [Header("ダブルタップ操作を有効する")]
    public bool enableDoubleTapControl = false;
    [Header("長押し操作を有効する")]
    public bool enableLongPressControl = false;
    [Header("ドラッグ操作を有効する")]
    public bool enableDragControl = true;
    [Header("ピンチ操作を有効する")]
    public bool enablePinchControl = false;
    [Header("------------------------------", order = 0), Space(-10, order = 1)]
    [Header("タップ操作は距離短いのドラッグ操作でも送信する", order = 3), Tooltip("ついてにドラッグ操作の初期距離閾値をなくす")]
    public bool isTapModeTouchFireWhenShortDrag = false;
    [Header("ドラッグ操作のタッチ開始の情報は引きずれる")]
    public bool isDragModeTouchDragTouchBegan = false;
    [Header("ドラッグ操作は二つ同時にできる")]
    public bool isTwoDragModeTouchesSimultaneously = false;
    [Header("マウス操作を有効する")]
    public bool enableMouse = true;


    private Subject<(IObservable<Unit>, TouchData)> subjectTouchBegan = new Subject<(IObservable<Unit>, TouchData)>();
    public IObservable<(IObservable<Unit>, TouchData)> OnTouchBegan() => subjectTouchBegan;
    private Subject<TouchData> subjectTouchEnd = new Subject<TouchData>();
    public IObservable<TouchData> OnTouchEnd() => subjectTouchEnd;
    private Subject<Unit> subjectClearTouchData = new Subject<Unit>();
    public IObservable<Unit> OnClearTouchData() => subjectClearTouchData;

    private Subject<TouchData> subjectTap = new Subject<TouchData>();
    public IObservable<TouchData> OnTap() => subjectTap;
    private Subject<TouchData> subjectDoubleTap = new Subject<TouchData>();
    public IObservable<TouchData> OnDoubleTap() => subjectDoubleTap;

    private Subject<TouchData> subjectOnLongPressStart = new Subject<TouchData>();
    public IObservable<TouchData> OnLongPressStart() => subjectOnLongPressStart;
    private Subject<TouchData> subjectOnLongPress = new Subject<TouchData>();
    public IObservable<TouchData> OnLongPress() => subjectOnLongPress;
    private Subject<TouchData> subjectOnLongPressEnd = new Subject<TouchData>();
    public IObservable<TouchData> OnLongPressEnd() => subjectOnLongPressEnd;

    private Subject<TouchData> subjectDragStart = new Subject<TouchData>();
    public IObservable<TouchData> OnDragStart() => subjectDragStart;
    private Subject<TouchData> subjectDrag = new Subject<TouchData>();
    public IObservable<TouchData> OnDrag() => subjectDrag;
    private Subject<TouchData> subjectDragEnd = new Subject<TouchData>();
    public IObservable<TouchData> OnDragEnd() => subjectDragEnd;

    private Subject<(TouchData, TouchData)> subjectPinch = new Subject<(TouchData, TouchData)>();
    public IObservable<(TouchData, TouchData)> OnPinch() => subjectPinch;

    private static int screenWidth = 0;
    private static int screenHeight = 0;
    public static Vector2 viewportToScreenRatio = default;
    public static Vector2 ViewportToScreenRatio             //Viewport座標を画面の比率に合わせる用
    {
        get
        {
            if (Screen.width != screenWidth || Screen.height != screenHeight)
            {
                viewportToScreenRatio = new Vector2(Screen.width, Screen.height).normalized * 2;
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }
            return viewportToScreenRatio;
        }
    }

    public const float shortLatency = 0.33f;
    public const float longLatency = 0.66f;
    public const float shortDistance = 0.03f;
    public const float longDistance = 0.08f;
    private const float dragTouchBeganDistance = 0.12f;     //longDistanceより長い必要があります
    //#if UNITY_EDITOR
    //    //テスト用
    //    private List<TouchData> TouchDataHistory = new List<TouchData>();
    //    private List<IObservable<Unit>> HotTouchHistory = new List<IObservable<Unit>>();
    //#endif

    private void Start()
    {
        // タッチ情報ストリームの初期化
        var touchStream = this.UpdateAsObservable().Where(_ => Input.touchCount > 0).SelectMany(_ => Input.touches);

        // マウス情報をタッチ情報に変換
        if (enableMouse == true)
        {
            Input.simulateMouseWithTouches = false;
            var mouseStream = this.UpdateAsObservable().SelectMany(_ =>
            {
                List<Touch> mouseToTouches = new List<Touch>();
                for (int i = 0; i <= 2; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {   // マウスボタン押下時
                        Touch touch = new Touch();
                        touch.position = Input.mousePosition;
                        touch.phase = TouchPhase.Began;
                        touch.fingerId = -i - 1;    //マウス左ボタンは-1、右ボタンは-2、中ボタンは-3
                        mouseToTouches.Add(touch);
                    }
                    else if (Input.GetMouseButton(i) && TouchDataDict.TryGetValue(-i - 1, out TouchData touchData) && touchData.touchNow.Value.position == (Vector2)Input.mousePosition)
                    {   // マウスボタン押下中、移動しない
                        Touch touch = new Touch();
                        touch.position = Input.mousePosition;
                        touch.phase = TouchPhase.Stationary;
                        touch.fingerId = -i - 1;    //マウス左ボタンは-1、右ボタンは-2、中ボタンは-3
                        mouseToTouches.Add(touch);
                    }
                    else if (Input.GetMouseButton(i))
                    {   // マウスボタン押下中、移動する
                        Touch touch = new Touch();
                        touch.position = Input.mousePosition;
                        touch.phase = TouchPhase.Moved;
                        touch.fingerId = -i - 1;    //マウス左ボタンは-1、右ボタンは-2、中ボタンは-3
                        mouseToTouches.Add(touch);
                    }
                    else if (Input.GetMouseButtonUp(i))
                    {   // マウスボタンを離す時
                        Touch touch = new Touch();
                        touch.position = Input.mousePosition;
                        touch.phase = TouchPhase.Ended;
                        touch.fingerId = -i - 1;    //マウス左ボタンは-1、右ボタンは-2、中ボタンは-3
                        mouseToTouches.Add(touch);
                    }
                }
                return mouseToTouches;
            });
            // タッチ情報とマウス情報を合成
            touchStream = touchStream.Merge(mouseStream);
        }

        // タッチ情報シェア
        touchStream = touchStream.Share();
        // タッチ開始の情報管理
        var touchBeganStream = touchStream.Where(touch => touch.phase == TouchPhase.Began).Timestamp().Subscribe(touch =>
        {
            // 設定によって一部のタッチを排除
            bool beganInGUI = EventSystem.current?.IsPointerOverGameObject(touch.Value.fingerId) ?? false;
            if (beganInGUI == true)
            {
                if (!doGetTouchBeganInGUI)          //GUIから開始したタッチ情報
                    return;
            }
            else
            {
                if (!doGetTouchNotBeganInGUI)       //GUI以外開始したタッチ情報、或いはEventSystemがない場合
                    return;
            }
            //Debug.Log("Touch began!");
            // 新しいタッチ情報を設定
            TouchData newTouchData = new TouchData();
            newTouchData.beganInGUI = beganInGUI;   //GUIから開始したフラグ
            if (TouchDataDict.ContainsKey(touch.Value.fingerId))    //まだ削除してないタッチ情報があるの場合
            {
                // 最後の位置を時間付けて更新
                TouchDataDict[touch.Value.fingerId].SetTouchEnd(new Timestamped<Touch>(TouchDataDict[touch.Value.fingerId].touchNow.Value, DateTimeOffset.Now));
                // 終了処理
                RemoveTouchDataAtEndOfUpdate(TouchDataDict[touch.Value.fingerId]);
            }
            TouchDataDict[touch.Value.fingerId] = newTouchData;
            // 操作をプレイヤーコマンドに変換用
            var hotTouch = newTouchData.ObserveEveryValueChanged(x => x.touchNow.Timestamp).AsUnitObservable().Share();
            //#if UNITY_EDITOR
            //            //テスト用
            //            TouchDataHistory.Add(newTouchData);
            //            HotTouchHistory.Add(hotTouch);
            //#endif

            // タップ操作
            if (enableTapControl)
            {
                hotTouch.Where(_ => newTouchData.touchPhase == TouchPhase.Ended && (newTouchData.touchMode == TouchMode.Tap || (isTapModeTouchFireWhenShortDrag == true && newTouchData.touchMode == TouchMode.Drag)) && newTouchData.GetMoveVector().sqrMagnitude <= shortDistance * shortDistance && newTouchData.GetTimeElapsed() <= shortLatency).First()
                .Subscribe(_ =>
                {
                    subjectTap.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
            }

            // 長押し
            if (enableLongPressControl)
            {
                // 長押し操作中
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.LongPress && newTouchData.touchPhase != TouchPhase.Ended)
                .Subscribe(_ =>
                {
                    subjectOnLongPress.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
                // 長押し終了
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.LongPress && newTouchData.touchPhase == TouchPhase.Ended).First()
                .Subscribe(_ =>
                {
                    subjectOnLongPressEnd.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
                // 長押し操作モードに
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.Tap && newTouchData.touchPhase != TouchPhase.Ended && newTouchData.GetMoveVector().sqrMagnitude <= shortDistance * shortDistance && newTouchData.GetTimeElapsed() > longLatency).First()
                .Subscribe(_ =>
                {
                    newTouchData.touchMode = TouchMode.LongPress;
                    subjectOnLongPressStart.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
            }

            // 一本指操作
            if (enableDragControl)
            {
                // 一本指操作モード操作中
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.Drag && newTouchData.touchPhase != TouchPhase.Ended)
                .Subscribe(touchData =>
                {
                    // タッチ開始の情報を引きずる
                    if (isDragModeTouchDragTouchBegan)
                        newTouchData.DragTouchBegan(dragTouchBeganDistance);
                    // イベント送信
                    subjectDrag.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
                // 一本指操作モード終了
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.Drag && newTouchData.touchPhase == TouchPhase.Ended).First()
                .Subscribe(touchData =>
                {
                    // イベント送信
                    subjectDragEnd.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
                // 一本指操作モードに
                hotTouch.Where(_ => newTouchData.touchMode == TouchMode.Tap && newTouchData.touchPhase == TouchPhase.Moved && (isTapModeTouchFireWhenShortDrag == true || newTouchData.GetMoveVector().sqrMagnitude > shortDistance * shortDistance) && !DoDragModeTouchesReachLimit()).First()
                .Subscribe(_ =>
                {
                    newTouchData.touchMode = TouchMode.Drag;
                    // イベント送信
                    subjectDragStart.OnNext(newTouchData);
                }).AddTo(newTouchData.disposable);
            }

            // 二本指操作
            if (enablePinchControl)
            {
                // 二本指操作モードに
                hotTouch.Where(_ => Input.touchCount >= 2 && newTouchData.touchMode == TouchMode.Tap && newTouchData.touchPhase == TouchPhase.Began)
                .Select(_ => PairableTouchForPinchMode(newTouchData)).Where(touchDateTheOther => touchDateTheOther != null).First()
                .Subscribe(touchDateTheOther =>
                {
                    //Debug.Log("Touch pairing for pinch!");
                    newTouchData.SetPairedPinchTouch(touchDateTheOther);
                    touchDateTheOther.SetPairedPinchTouch(newTouchData);
                }).AddTo(newTouchData.disposable);
                // 二本指タッチ操作中
                hotTouch.Where(_ => Input.touchCount >= 2 && newTouchData.touchMode == TouchMode.Pinch && newTouchData.touchPhase == TouchPhase.Moved && newTouchData.pairedPinchTouchData?.touchPhase != TouchPhase.Ended)
                .Subscribe(_ =>
                {
                    subjectPinch.OnNext((newTouchData, newTouchData.pairedPinchTouchData));
                }).AddTo(newTouchData.disposable);
            }

            // イベントを送信
            subjectTouchBegan.OnNext((hotTouch, newTouchData));
            // タッチ開始の情報を設定し、イベントをトリガーする
            newTouchData.SetTouchBegan(touch);
        }).AddTo(gameObject);

        // タッチ中の情報管理
        var touchingStream = touchStream.Where(touch => touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary).Timestamp().Subscribe(touch =>
        {
            if (TouchDataDict.ContainsKey(touch.Value.fingerId))
            {
                // タッチ中の情報を設定し、イベントをトリガーする
                TouchDataDict[touch.Value.fingerId].SetTouching(touch);
            }
        }).AddTo(gameObject);

        // タッチ終了の情報管理
        var touchEndedStream = touchStream.Where(touch => touch.phase == TouchPhase.Ended).Timestamp().Subscribe(touch =>
        {
            //Debug.Log("Touch ended!");
            if (TouchDataDict.ContainsKey(touch.Value.fingerId))
            {
                // タッチ終了の情報を設定し、イベントをトリガーする
                TouchDataDict[touch.Value.fingerId].SetTouchEnd(touch);
                // 終了処理
                RemoveTouchDataAtEndOfUpdate(TouchDataDict[touch.Value.fingerId]);
            }
        }).AddTo(gameObject);

        // タッチの終了が来ないまま終わる場合の処理、2フレーム間更新しないとタッチ終了処理してから削除
        var touchMightEndedStream = this.UpdateAsObservable().SelectMany(_ => TouchDataDict).Where(touchDataKeyed => DateTime.Now - touchDataKeyed.Value.touchNow.Timestamp > TimeSpan.FromSeconds(Time.deltaTime * 2)).Subscribe(touchDataKeyed =>
        {
            //Debug.Log("Touch might ended!");
            // タッチ終了の情報は最後の位置を時間付けて設定
            touchDataKeyed.Value.SetTouchEnd(new Timestamped<Touch>(touchDataKeyed.Value.touchNow.Value, DateTimeOffset.Now));
            // 終了処理
            RemoveTouchDataAtEndOfUpdate(touchDataKeyed.Value);
        }).AddTo(gameObject);

        // ダブルタップ操作
        OnTouchEnd().Where(_ => enableDoubleTapControl == true).Buffer(2, 1)
            .Where(touchDatas => touchDatas[1].touchNow.Timestamp - touchDatas[0].touchNow.Timestamp < TimeSpan.FromSeconds(shortLatency)
            && touchDatas[0].GetTimeElapsed() < shortLatency && touchDatas[0].GetMoveVector().sqrMagnitude <= shortDistance * shortDistance
            && touchDatas[1].GetTimeElapsed() < shortLatency && touchDatas[1].GetMoveVector().sqrMagnitude <= shortDistance * shortDistance).Subscribe(touchDatas =>
        {
            subjectDoubleTap.OnNext(touchDatas[1]);
        }).AddTo(gameObject);
    }

    private TouchData PairableTouchForPinchMode(TouchData touchDataOne)     //二本指操作のもう一つのタッチを確認
    {
        TouchData touchDataTheOther = null;
        foreach (var touchDataKeyed in TouchDataDict)
        {
            if (touchDataKeyed.Value == touchDataOne)
                continue;
            // 既にピンチモードになったタッチを確認
            if (touchDataKeyed.Value.touchMode == TouchMode.Pinch && touchDataKeyed.Value.touchPhase != TouchPhase.Ended)
            {
                // ペアなしのピンチモードタッチがある場合、ペアリングする
                if (touchDataKeyed.Value.pairedPinchTouchData == null || touchDataKeyed.Value.pairedPinchTouchData.touchPhase == TouchPhase.Ended)
                    return touchDataKeyed.Value;
                // ペアしたピンチモードタッチ既にあり、ペアリングしない
                else
                    return null;
            }
            // 開始時間の差が短いのもう一つのタッチを確認
            else if (touchDataKeyed.Value.touchMode == TouchMode.Tap && touchDataKeyed.Value.touchPhase != TouchPhase.Ended && touchDataOne.touchBegan.Timestamp - touchDataKeyed.Value.touchBegan.Timestamp < TimeSpan.FromSeconds(shortLatency))
            {
                touchDataTheOther = touchDataKeyed.Value;
            }
        }
        return touchDataTheOther;
    }

    public bool DoDragModeTouchesReachLimit()                               //一本指のタッチはまだ使えるかを確認
    {
        int counter = 0;
        foreach (var touchDataKeyed in TouchDataDict)
        {
            if (touchDataKeyed.Value.touchPhase != TouchPhase.Ended && touchDataKeyed.Value.touchMode == TouchMode.Drag)
            {
                counter++;
                if (counter >= (isTwoDragModeTouchesSimultaneously ? 2 : 1))    //設定によって二つまで存在できます
                    return true;
            }
        }
        return false;
    }

    public void ClearTouchData()                                            //全てのタッチ情報を終了させる
    {
        // イベントを送信
        subjectClearTouchData.OnNext(default);
        // 全てのタッチ情報を終了させる
        foreach (var touchData in TouchDataDict)
        {
            // タッチ終了の情報は最後の位置を時間付けて設定
            touchData.Value.SetTouchEnd(new Timestamped<Touch>(touchData.Value.touchNow.Value, DateTimeOffset.Now));
            // 終了処理
            RemoveTouchDataAtEndOfUpdate(touchData.Value);
        }
        TouchDataDict.Clear();
    }

    private void RemoveTouchDataAtEndOfUpdate(TouchData touchData)          //Update終了後、このタッチ情報を終了処理する
    {
        Observable.TimerFrame(0, FrameCountType.EndOfFrame).First().Subscribe(_ =>
        {
            // 購読を停止
            touchData.disposable.Dispose();
            // まだ管理辞書にいる場合は削除
            if (TouchDataDict.ContainsKey(touchData.fingerId) && TouchDataDict[touchData.fingerId] == touchData)
                TouchDataDict.Remove(touchData.fingerId);
            // イベントを送信
            subjectTouchEnd.OnNext(touchData);
        }).AddTo(gameObject);
    }

    private void OnDrawGizmosSelected()                                     //タッチの開始位置と現在位置のデバッグ情報
    {
        foreach (var touchData in TouchDataDict.Values)
        {
            Gizmos.color = new Color(1, 1, 1, 0.3f);
            Gizmos.DrawSphere(Camera.main.ScreenToWorldPoint((Vector3)touchData.touchNow.Value.position + Vector3.forward), 0.01f);
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawSphere(Camera.main.ScreenToWorldPoint((Vector3)touchData.touchBegan.Value.position + Vector3.forward), 0.01f);
        }
    }

    private void OnDestroy()
    {
        ClearTouchData();
        subjectTouchBegan.OnCompleted();
        subjectTouchEnd.OnCompleted();
        subjectClearTouchData.OnCompleted();
        subjectTap.OnCompleted();
        subjectDoubleTap.OnCompleted();
        subjectOnLongPressStart.OnCompleted();
        subjectOnLongPress.OnCompleted();
        subjectOnLongPressEnd.OnCompleted();
        subjectDragStart.OnCompleted();
        subjectDrag.OnCompleted();
        subjectDragEnd.OnCompleted();
        subjectPinch.OnCompleted();
    }
}

[System.Serializable]
public class TouchData
{
    public Timestamped<Touch> touchBegan;
    public Timestamped<Touch> touchPrev;
    public Timestamped<Touch> touchNow;
    public int fingerId;
    public bool beganInGUI = false;
    public TouchPhase touchPhase;
    public TouchMode touchMode = TouchMode.Tap;
    public TouchData pairedPinchTouchData;
    public CompositeDisposable disposable = new CompositeDisposable();

    public void SetTouchBegan(Timestamped<Touch> touchBegan)                //タッチ開始の状態更新用
    {
        fingerId = touchBegan.Value.fingerId;
        touchPhase = TouchPhase.Began;
        this.touchBegan = touchBegan;
        touchPrev = touchBegan;
        touchNow = touchBegan;
    }
    public void SetTouching(Timestamped<Touch> touch)                       //タッチ中の状態更新用
    {
        touchPhase = touch.Value.phase;
        touchPrev = touchNow;
        touchNow = touch;
    }
    public void SetTouchEnd(Timestamped<Touch> touchEnded)                  //タッチ終了の状態更新用
    {
        touchPhase = TouchPhase.Ended;
        touchPrev = touchNow;
        touchNow = touchEnded;
    }

    public Vector2 GetMoveVector()                                          //移動ベクトル取得
    {
        return (touchNow.Value.ViewportPosition() - touchBegan.Value.ViewportPosition()) * TouchManager.ViewportToScreenRatio;
    }
    public void DragTouchBegan(float dragDistance)                          //タッチ開始の情報を引きずる
    {
        var vector = GetMoveVector();
        if (vector.sqrMagnitude > dragDistance * dragDistance)
        {
            Touch draggedTouchBegan = touchBegan.Value;
            draggedTouchBegan.position = touchNow.Value.position - (Vector2)Camera.main.ViewportToScreenPoint(vector.normalized / TouchManager.ViewportToScreenRatio) * dragDistance;
            touchBegan = new Timestamped<Touch>(draggedTouchBegan, touchBegan.Timestamp);
        }
    }
    public float GetTimeElapsed()                                           //時間経過取得
    {
        return (float)(touchNow.Timestamp - touchBegan.Timestamp).TotalSeconds;
    }
    public void SetPairedPinchTouch(TouchData touchData)
    {
        pairedPinchTouchData = touchData;
        touchMode = TouchMode.Pinch;
    }
}

// タッチ情報のViewport位置取得用
namespace UnityEngine
{
    public static class TouchExtensions
    {
        public static Vector2 ViewportPosition(this Touch touch)
        {
            return Camera.main.ScreenToViewportPoint(touch.position);
        }
    }
}

public enum TouchMode
{
    Tap,
    LongPress,
    Drag,
    Pinch,
}