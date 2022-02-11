using Cinemachine;
using DG.Tweening.Plugins.Core.PathCore;
using Facebook.Unity;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Train train = null;
    [Header("列車の基本の速度")]
    [SerializeField] private float baseSpeed = 30;
    [Header("全ステージクリアする毎に、列車の速度の増える量")]
    [SerializeField] private float speedIncreasePerAllStages = 5f;
    [Header("スローモーションになる列車が分岐までの距離")]
    [SerializeField] private float slowmotionDistance = 5;
    [Header("スローモーションの速度倍率")]
    [SerializeField] private float slowmotionTimeScale = 0.2f;
    [SerializeField] private StageManager stageManager = null;
    [SerializeField] private TouchManager touchManager = null;
    [SerializeField] private SoundManager soundManager = null;
    [SerializeField] private CinemachineVirtualCamera mainCamera = null;
    [SerializeField] private CinemachineVirtualCamera loadingCamera = null;
    [SerializeField] private CinemachineVirtualCamera resultCamera = null;
    [SerializeField] private Animator resultCameraAniamtor = null;
    [SerializeField] private CanvasGroup loadingUICanvasGroup = null;
    [SerializeField] private CanvasGroup startUICanvasGroup = null;
    [SerializeField] private GameObject settingUI = null;
    [SerializeField] private GameObject retryUI = null;
    [SerializeField] private GameObject resultUI = null;
    [SerializeField] private DispNumText resultEarnCoinUI = null;
    [SerializeField] private Slider volumeMasterSlider = null;
    [SerializeField] private Slider volumeBGMSlider = null;
    [SerializeField] private Slider volumeSESlider = null;
    [SerializeField] private Text volumeBGMOnText = null;
    [SerializeField] private Text volumeBGMOffText = null;
    [SerializeField] private Text volumeSEOnText = null;
    [SerializeField] private Text volumeSEOffText = null;
    [SerializeField] private Text levelText = null;
    [SerializeField] private Text coinText = null;
    [SerializeField] private Animator coinAnimator = null;
    [SerializeField] private GameObject resultTrainEffectPrefab = null;
    [SerializeField] private AudioClip switchChangeSE = null;
    [SerializeField] private List<PlayableDirector> praiseTimelineList = new List<PlayableDirector>();
    [SerializeField] private Slider progressBarSlider = null;
    [SerializeField] private GameObject progressBarSwitchIconPrefab = null;

    private GamePhase gamePhase = GamePhase.Retry;
    private ConnectionSwitch connectionSwitchNow = null;
    private int stageNumber;
    [NonSerialized] private int[] stageNumberShuffleList = null;
    private int coin;
    private int coinFromStage;
    private GameObject resultTrainEffect;
    private List<(CurvySplineSegment controlPoint, float distance)> controlPointDistanceList = new List<(CurvySplineSegment controlPoint, float distance)>();
    private float totalDistance = 0;
    private List<GameObject> progressBarSwitchIconList = new List<GameObject>();

    const int segmentSearchTimesLimit = 128;
    const int connectionSearchTimesLimit = 16;
    const float titleDisplayTime = 0.5f;
    const float titleFadeoutTime = 1f;

    //private void Awake()
    //{
    //}

    /* 初期化順番
     * CurvyのAwake()
     * ->CurvyのStart()
     * ->stageManager.LoadStage()
     * ->stageManager.InitConnection()
     * ->train.Init()
     * ->ConnectionSwitchのStart()
     * ->this.FocusNextSwitch();
     */

    private void Start()
    {
        // サードパーティ初期化
        initThirdParty();
        // サブスクリプション
        // 画面タップで分岐点操作
        touchManager.OnTouchBegan().Subscribe(_ =>
        {
            if (gamePhase == GamePhase.WaitStart)
            {
                StartGame();
            }
            else
            {
                if (connectionSwitchNow == null)
                {
                    FocusNextSwitch();
                }
                if (connectionSwitchNow != null)
                {
                    connectionSwitchNow.UseSwitch();
                    SoundManager.PlaySE(switchChangeSE);
                }
            }
        }).AddTo(this);
        // 分岐点通った直後に次に分岐を見つける
        int lastIndex = -1;
        train.OnSwitchUsed().Subscribe(_ =>
        {
            var isSuccess = FocusNextSwitch();
            if (isSuccess)
            {
                // 正しい方向へ移動する場合、讃えるメッセージを表示
                int index = UnityEngine.Random.Range(0, praiseTimelineList.Count);
                for (int loopLimit = 0; loopLimit < 10 && index == lastIndex; loopLimit++)
                {
                    index = UnityEngine.Random.Range(0, praiseTimelineList.Count);
                }
                var timeline = praiseTimelineList[index];
                timeline.gameObject.SetActive(true);
                timeline.Play();
                lastIndex = index;
            }
        });
        // 讃えるメッセージ再生後隠すように
        foreach (var timeline in praiseTimelineList)
        {
            timeline.stopped += thisTimeline =>
            {
                thisTimeline.gameObject.SetActive(false);
            };
        }
        // 列車がコインを取得の場合
        train.OnGetCoin().Subscribe(_ =>
        {
            EarnCoin();
        });
        // 列車が脱線の場合
        train.OnDerail().Subscribe(_ =>
        {
            gamePhase = GamePhase.Retry;
            retryUI.SetActive(true);
        });
        // 列車が終点に到着の場合
        train.OnReachEnd().Subscribe(_ =>
        {
            gamePhase = GamePhase.Result;
            // ステージ一周クリアした後ステージをシャッフルする
            if (stageNumber % stageManager.StageNumberLength == 0)
            {
                ShuffleStage();
            }
            ShowResultEarnCoin();
            stageNumber++;
            SaveGame();
            resultCameraAniamtor.enabled = true;
            resultCameraAniamtor.Play(resultCameraAniamtor.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0.0f);   //巻き戻し
            resultCamera.enabled = true;
            resultTrainEffect = Instantiate(resultTrainEffectPrefab, train.Locomotive.transform);
            resultUI.SetActive(true);
        });
        // ゲームの初期化
        InitGame();
    }

    private void FixedUpdate()
    {
        if (gamePhase != GamePhase.Gaming)
        {
            Time.timeScale = 1;
            return;
        }
        // 進路の確認
        bool switchForwardExistMetaTrap = connectionSwitchNow?.ToDirectionNow.GetNextMetadata<MetaTrap>(out _, segmentSearchTimesLimit / 8) != null;
        float timeScale = 1;
        PathDirection pathDirection = new PathDirection(train.Locomotive);
        float distanceToConnection = (pathDirection.movementDirection == MovementDirection.Forward ? 1 : -1) * (pathDirection.controlPoint.Distance - train.Locomotive.AbsolutePosition);
        float position = pathDirection.controlPoint.Distance;
        for (int loopCount = 0; pathDirection.controlPoint != null && loopCount <= connectionSearchTimesLimit; loopCount++)
        {
            // 今のパスはMetaTrapがある場合、進路更新しない
            var metaTrap = pathDirection.GetNextMetadata<MetaTrap>(out _, segmentSearchTimesLimit / 8);
            if (metaTrap?.enabled == true)
            {
                break;
            }
            // 終点の確認
            var metaEnd = pathDirection.GetNextMetadata<MetaEnd>(out float distanceToEnd, segmentSearchTimesLimit / 4);
            if (metaEnd != null)
            {   // 進捗ゲージの列車位置を更新
                float distanceFromStart = totalDistance - distanceToConnection - distanceToEnd;
                progressBarSlider.value = distanceFromStart / totalDistance;
                break;
            }
            var connection = pathDirection.GetNextConnection(true, loopCount == 0);      //次のパスの繋がりまで進む、初回だけ現在位置のconnectionも取得
            distanceToConnection += (pathDirection.movementDirection == MovementDirection.Forward ? 1 : -1) * (pathDirection.controlPoint.Distance - position);
            // 分岐の進む方向にMetaTrapが存在する場合、距離を確認してスローモーションに
            if (switchForwardExistMetaTrap && distanceToConnection < slowmotionDistance && connectionSwitchNow?.Connection == pathDirection.controlPoint.Connection)
            {
                timeScale = slowmotionTimeScale;
            }
            // 分岐ありの場合
            var connectionSwitch = connection?.GetComponent<ConnectionSwitch>();
            if (connectionSwitch?.enabled == true)
            {   // 進捗ゲージの列車位置を更新
                float distanceFromStart = 0;
                foreach (var controlPointDistanceTuple in controlPointDistanceList)
                {
                    distanceFromStart += controlPointDistanceTuple.distance;
                    if (controlPointDistanceTuple.controlPoint == pathDirection.controlPoint)
                    {
                        distanceFromStart -= distanceToConnection;
                        progressBarSlider.value = distanceFromStart / totalDistance;
                        break;
                    }
                }
                break;
            }
            // 分岐なしの場合
            else if (pathDirection.controlPoint.FollowUp != null)
                pathDirection = pathDirection.GetNextPathDirection();       //パス乗り換え
            // 現在の位置を記録
            position = pathDirection.controlPoint.Distance;
        }
        if (Time.timeScale != timeScale)
        {
            Time.timeScale = timeScale;
            train.SlowMotion(timeScale);
        }
    }


    public void InitGame()
    {
        // ステージ1と以降は３ステージごとに、リザルトの後にタウン画面へ遷移
        if (gamePhase == GamePhase.Result && stageNumber % 3 == 2)
        {
            // タウンの進捗を確認
            int progress = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);
            int checkProgress = progress + 1;
            string mstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", checkProgress);
            VineteGrowth vineteGrowth = Resources.Load<VineteGrowth>(mstKey);
            // 必要コインが足りる場合
            if (vineteGrowth != null && vineteGrowth.needCoin <= coin)
            {
                ShowTownScene();
                return;
            }
        }
        if (gamePhase == GamePhase.Loading)
            return;
        gamePhase = GamePhase.Loading;
        // ローディング画面
        ShowLoading();
        // ゲーム初期化
        connectionSwitchNow = null;
        resultCameraAniamtor.enabled = false;
        resultCamera.enabled = false;
        if (resultTrainEffect != null)
            Destroy(resultTrainEffect);
        coinFromStage = 0;
        // UI初期化
        loadingUICanvasGroup.alpha = 1;
        loadingUICanvasGroup.gameObject.SetActive(true);
        startUICanvasGroup.alpha = 0;
        startUICanvasGroup.gameObject.SetActive(false);
        settingUI.SetActive(false);
        retryUI.SetActive(false);
        resultUI.SetActive(false);
        // セーブ読み込み
        stageNumber = PlayerPrefs.GetInt(GameDefine.StageNumberKey, 1);
        LoadStageShuffle();
        coin = PlayerPrefs.GetInt(GameDefine.CoinKey, 0);
        levelText.text = stageNumber.ToString();
        UpdateCoin();
        // ステージ初期化
        bool existStage = false;
        while (existStage == false)
        {
            int stageNumberToLoad = stageNumber;
            if (stageNumberToLoad < stageManager.StageNumberMin || stageNumberToLoad > stageManager.StageNumberMax)
            {
                stageNumberToLoad = stageNumberShuffleList[stageNumber % stageManager.StageNumberLength];
            }
            existStage = stageManager.SetStageNumber(stageNumberToLoad);
            // ステージナンバー再生成
            if (!existStage)
            {
                ShuffleStage();
            }
        }
        stageManager.LoadStage();
        stageManager.InitConnection();
        train.maxSpeed = baseSpeed + speedIncreasePerAllStages * GetTrainSpeedLevel();
        train.Init();
        stageManager.UpdateAllEnabledSwitches();
        // 次のフレームの初期化処理
        Observable.NextFrame().Subscribe(_ =>
        {
            InitProgressBar();
            FocusNextSwitch();
            gamePhase = GamePhase.WaitStart;
        });
    }

    public void ShowLoading()
    {
        loadingUICanvasGroup.alpha = 1;
        loadingUICanvasGroup.gameObject.SetActive(true);
        loadingCamera.enabled = true;
        float timer = 0;
        this.UpdateAsObservable().TakeWhile(_ => timer < titleDisplayTime || gamePhase == GamePhase.Loading).Subscribe(_ =>
        {
            timer += Time.deltaTime;
        }, () =>
        {
            timer = 0;
            ShowStart();
            this.UpdateAsObservable().TakeWhile(_ => timer < titleFadeoutTime).Subscribe(_ =>
            {
                timer += Time.deltaTime;
                loadingUICanvasGroup.alpha = 1 - timer / titleFadeoutTime;
                if (timer >= titleFadeoutTime / 2)
                    loadingCamera.enabled = false;
            }, () =>
            {
                loadingUICanvasGroup.gameObject.SetActive(false);
            });
        });
    }

    public void ShowStart()
    {
        startUICanvasGroup.alpha = 0;
        startUICanvasGroup.blocksRaycasts = false;
        startUICanvasGroup.gameObject.SetActive(true);
        float timer = 0;
        this.UpdateAsObservable().TakeWhile(_ => timer < titleDisplayTime).Subscribe(_ =>
        {
            timer += Time.deltaTime;
            startUICanvasGroup.alpha = timer / titleFadeoutTime;
        }, () =>
        {
            startUICanvasGroup.alpha = 1;
            startUICanvasGroup.blocksRaycasts = true;
        });
    }

    public void ShowSetting()
    {
        volumeMasterSlider.value = SoundManager.VolumeMaster;
        volumeBGMSlider.value = SoundManager.VolumeBGM;
        volumeSESlider.value = SoundManager.VolumeSE;
        SetBGMVolumeButtonLable();
        SetSEVolumeButtonLable();
        settingUI.SetActive(true);
    }

    public void CloseSetting()
    {
        settingUI.SetActive(false);
    }

    public void StartGame()
    {
        if (gamePhase != GamePhase.WaitStart)
            return;
        gamePhase = GamePhase.Gaming;
        train.Play();
        startUICanvasGroup.alpha = 1;
        startUICanvasGroup.blocksRaycasts = false;
        startUICanvasGroup.gameObject.SetActive(true);
        float timer = 0;
        this.UpdateAsObservable().TakeWhile(_ => timer < titleDisplayTime).Subscribe(_ =>
        {
            timer += Time.deltaTime;
            startUICanvasGroup.alpha = 1 - timer / titleFadeoutTime;
        }, () =>
        {
            startUICanvasGroup.alpha = 0;
            startUICanvasGroup.gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// 次の分岐を見つけてその分岐を操作できるに
    /// </summary>
    /// <returns>true:次の分岐や終点はある false:失敗になる</returns>
    public bool FocusNextSwitch()
    {
        PathDirection pathDirectionNow = new PathDirection(train.Locomotive);
        if (connectionSwitchNow != null)
            connectionSwitchNow.SetFocus(false);
        connectionSwitchNow = null;
        int i = 0;
        while (connectionSwitchNow == null && pathDirectionNow.controlPoint != null)
        {
            if (pathDirectionNow.controlPoint.Connection != null)
            {
                // 分岐の方向を確認
                var connectionSwitch = pathDirectionNow.controlPoint.Connection.GetComponent<ConnectionSwitch>();
                if (connectionSwitch?.FromDirectionNow == pathDirectionNow)
                {
                    connectionSwitchNow = connectionSwitch;
                    connectionSwitchNow.SetFocus(true);
                    return true;
                }
            }
            var metaEnd = pathDirectionNow.controlPoint.GetMetadata<MetaEnd>();
            if (metaEnd != null)
            {
                return true;
            }
            var metaTrap = pathDirectionNow.controlPoint.GetMetadata<MetaTrap>();
            if (metaTrap != null && metaTrap.enabled == true)
            {
                return false;
            }
            // 検索回数制限
            if (++i >= segmentSearchTimesLimit)
            {
                Debug.LogWarning("次の分岐の検索回数制限到達!");
                break;
            }
            // 次のポイントを確認
            pathDirectionNow = pathDirectionNow.GetNextPathDirection();
        }
        return false;
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt(GameDefine.StageNumberKey, stageNumber);
        PlayerPrefs.SetInt(GameDefine.CoinKey, coin);
        PlayerPrefs.Save();
    }

    public void LoadStageShuffle()
    {
        if (stageNumberShuffleList != null)
            return;
        var stageNumberShullfeString = PlayerPrefs.GetString(GameDefine.StageNumberShuffleKey, "");
        if (stageNumberShullfeString != null)
        {
            string[] stageNumberStringShullfeList = stageNumberShullfeString.Split(',');
            var stageNumberIntShuffleList = new int[stageNumberStringShullfeList.Length];
            for (int i = 0; i < stageNumberStringShullfeList.Length; i++)
            {
                string stageNumberString = stageNumberStringShullfeList[i];
                if (int.TryParse(stageNumberString, out int stageNumberInt))
                {
                    stageNumberIntShuffleList[i] = stageNumberInt;
                }
                else
                {   // ロード失敗の場合
                    ShuffleStage();
                    return;
                }
            }
            stageNumberShuffleList = stageNumberIntShuffleList;
        }
        else
        {   // ロード失敗の場合
            ShuffleStage();
        }
    }

    public void ShuffleStage()
    {
        if (stageNumberShuffleList == null || stageNumberShuffleList.Length != stageManager.StageNumberLength)
        {
            stageNumberShuffleList = new int[stageManager.StageNumberLength];
        }
        for (int i = 0; i < stageManager.StageNumberLength; i++)
        {
            stageNumberShuffleList[i] = stageManager.StageNumberMin + i;
        }
        for (int i = 0; i < stageNumberShuffleList.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, stageNumberShuffleList.Length);
            int tmp = stageNumberShuffleList[i];
            stageNumberShuffleList[i] = stageNumberShuffleList[randomIndex];
            stageNumberShuffleList[randomIndex] = tmp;
        }
        var stageNumberShuffleString = "";
        foreach (var stageNumberInt in stageNumberShuffleList)
        {
            stageNumberShuffleString += stageNumberInt.ToString() + ",";
        }
        stageNumberShuffleString = stageNumberShuffleString.TrimEnd(',');
        PlayerPrefs.SetString(GameDefine.StageNumberShuffleKey, stageNumberShuffleString);
    }

    public void UpdateCoin()
    {
        if (coinText.text != coin.ToString())
        {
            coinText.text = coin.ToString();
            coinAnimator.SetTrigger("Strong");
        }
    }

    public void EarnCoin()
    {
        var getCoin = GetPerCoinValue();
        if (coinFromStage + getCoin <= GetStageMaxCoinValue())
        {
            coinFromStage += getCoin;
            coin += getCoin;
            UpdateCoin();
        }
    }

    public void ShowResultEarnCoin()
    {
        resultEarnCoinUI.SetNumImmidiate(0);
        resultEarnCoinUI.SetNum(coinFromStage);
        SaveGame();
    }

    private int GetPerCoinValue()       //コイン一回貰える数
    {
        return GetStageMaxCoinValue() / stageManager.StageNow.coinPoints.Count;
    }

    private int GetStageMaxCoinValue()  //ステージで貰える最大のコイン数
    {
        return 70 + stageNumber * 30 + stageManager.StageNow.enabledSwitchs.Count * 20 + (GetTrainSpeedLevel() + 1) * 30;
    }

    public void SetMasterVolumn(float volume)
    {
        SoundManager.SetMasterVolume(volume);
    }
    public void SetBGMVolume(float volume)
    {
        SoundManager.SetBGMVolume(volume);
    }
    public void SetSEVolume(float volume)
    {
        SoundManager.SetSEVolume(volume);
    }

    public void ToggleMasterVolumn()
    {
        if (SoundManager.VolumeMaster > 0)
        {
            SoundManager.SetMasterVolume(0);
        }
        else
        {
            SoundManager.SetMasterVolume(1);
        }
    }
    public void ToggleBGMVolume()
    {
        if (SoundManager.VolumeBGM > 0)
        {
            SoundManager.SetBGMVolume(0);
        }
        else
        {
            SoundManager.SetBGMVolume(1);
        }
        SetBGMVolumeButtonLable();
    }
    public void ToggleSEVolume()
    {
        if (SoundManager.VolumeSE > 0)
        {
            SoundManager.SetSEVolume(0);
        }
        else
        {
            SoundManager.SetSEVolume(1);
        }
        SetSEVolumeButtonLable();
    }

    public void SetBGMVolumeButtonLable()
    {
        if (SoundManager.VolumeBGM > 0)
        {
            volumeBGMOnText.gameObject.SetActive(true);
            volumeBGMOffText.gameObject.SetActive(false);
        }
        else
        {
            volumeBGMOnText.gameObject.SetActive(false);
            volumeBGMOffText.gameObject.SetActive(true);
        }
    }
    public void SetSEVolumeButtonLable()
    {
        if (SoundManager.VolumeSE > 0)
        {
            volumeSEOnText.gameObject.SetActive(true);
            volumeSEOffText.gameObject.SetActive(false);
        }
        else
        {
            volumeSEOnText.gameObject.SetActive(false);
            volumeSEOffText.gameObject.SetActive(true);
        }
    }

    public void ShowTownScene()
    {
        SceneTransition.Instance.ChangeScene(GameDefine.SceneNameTown);
    }

    private int GetTrainSpeedLevel()
    {
        return Mathf.FloorToInt((float)(stageNumber - 1) / stageManager.StageNumberLength);
    }

    /// <summary>
    /// 進捗ゲージの初期化
    /// </summary>
    private void InitProgressBar()
    {
        // 前のステージの分岐アイコンを消す
        foreach (var switchIcon in progressBarSwitchIconList)
        {
            Destroy(switchIcon);
        }
        progressBarSwitchIconList.Clear();
        controlPointDistanceList.Clear();
        // 分岐の距離を確認
        PathDirection pathDirection = new PathDirection(train.Locomotive);
        float distance = 0;
        float position = train.Locomotive.AbsolutePosition;
        for (int loopCount = 0; pathDirection.controlPoint != null && loopCount <= connectionSearchTimesLimit * 2; loopCount++)
        {
            var connection = pathDirection.GetNextConnection(true, false, 30);      //次のパスの繋がりまで進む
            distance += (pathDirection.movementDirection == MovementDirection.Forward ? 1 : -1) * (pathDirection.controlPoint.Distance - position);
            // 分岐ありの場合
            var connectionSwitch = connection?.GetComponent<ConnectionSwitch>();
            if (connectionSwitch?.enabled == true)
            {
                // 分岐先にMetaTrapがない方を探す
                foreach (var directionPair in connectionSwitch.AvailableDirection)
                {
                    if (directionPair.fromDirection == pathDirection)
                    {
                        foreach (var toDirection in directionPair.toDirections)
                        {
                            if (toDirection.GetNextMetadata<MetaTrap>(out _) == null)
                            {
                                controlPointDistanceList.Add((pathDirection.controlPoint, distance));
                                distance = 0;
                                pathDirection = toDirection;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            // パス乗り換え
            else if (pathDirection.controlPoint.FollowUp != null)
                pathDirection = pathDirection.GetNextPathDirection();
            // 今の位置を記録
            position = pathDirection.controlPoint.Distance;
            // 終点を確認
            var metaEnd = pathDirection.GetNextMetadata<MetaEnd>(out float distanceToEnd, segmentSearchTimesLimit / 4);
            if (metaEnd != null)
            {
                distance += distanceToEnd;
                controlPointDistanceList.Add((metaEnd.ControlPoint, distance));
                break;
            }
        }
        // パスの合計長さ
        //string distanceStr = "";
        totalDistance = 0;
        foreach (var controlPointDistanceTuple in controlPointDistanceList)
        {
            totalDistance += controlPointDistanceTuple.distance;
            //distanceStr += controlPointDistanceTuple.distance.ToString() + ",";
        }
        //Debug.Log(distanceStr);
        // 分岐アイコン生成
        float distanceNow = 0;
        var fillRectSiblingIndex = progressBarSlider.fillRect.transform.GetSiblingIndex();
        for (int i = 0; i < controlPointDistanceList.Count - 1; i++)
        {
            (CurvySplineSegment controlPoint, float distance) controlPointDistanceTuple = controlPointDistanceList[i];
            distanceNow += controlPointDistanceTuple.distance;
            progressBarSlider.value = distanceNow / totalDistance;
            var switchIcon = Instantiate(progressBarSwitchIconPrefab, progressBarSlider.handleRect);
            switchIcon.transform.SetParent(progressBarSlider.transform);
            switchIcon.transform.SetSiblingIndex(fillRectSiblingIndex + 1);
            switchIcon.SetActive(true);
            progressBarSwitchIconList.Add(switchIcon);
        }
        progressBarSlider.value = 0;
    }

    private void initThirdParty()
    {

#if UNITY_EDITOR
#elif UNITY_ANDROID || UNITY_IOS

        // ゲームアナリティクス
        GameAnalyticsSDK.GameAnalytics.Initialize();

        // フェイスブック
        FB.Init(() => {
            if (FB.IsInitialized) {
                FB.ActivateApp();
            } else {
                Debug.Log("Failed to Initialize the Facebook SDK");
            }
        }, isGameShown => {
            if (!isGameShown) {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            } else {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        });
#endif

    }

    public enum GamePhase
    {
        Loading,
        WaitStart,
        Gaming,
        Retry,
        Result,
    }
}

