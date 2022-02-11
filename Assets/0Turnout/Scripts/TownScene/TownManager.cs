using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;

public class TownManager : SingletonMonoBehaviour<TownManager>
{
    [SerializeField] private TileController tileController;
    [SerializeField] private TownStatusView statusView;
    [SerializeField] private GrowBtn growBtn;
    [SerializeField] private Button stageBtn;
    [SerializeField] private EventSystem eventSystem;
    
    void Start()
    {
        // ステージに戻る
        stageBtn.OnClickAsObservable().Subscribe(_ => {
            goStage();
        }).AddTo(this.gameObject);

        // 建物初期化
        tileController.Initialize();
        // 建物初期化完了時
        tileController.InitProperty.Where(isInited => isInited).Subscribe(_ => {
            //TODO 本当はここでトランジションを開ける
        });

        // UI更新
        updateUi(true);
    }

    public void Grow() {

        // 進行度
        int progress = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);
        progress += 1;

        // マスタをロード
        string mstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", progress);
        VineteGrowth vineteGrowth = Resources.Load<VineteGrowth>(mstKey);

        // コイン
        int coin = PlayerPrefs.GetInt(GameDefine.CoinKey, 0);
        coin -= vineteGrowth.needCoin;

        // なぜかコインがない
        if (coin < 0) {
            return;
        }

        // 保存
        PlayerPrefs.SetInt(GameDefine.VineteProgressKey, progress);
        PlayerPrefs.SetInt(GameDefine.CoinKey, coin);
        PlayerPrefs.Save();

        // 開拓
        setEventEnable(false);
        tileController.Grow(vineteGrowth, progress + 1, () => {
            setEventEnable(true);
        });
        // UI更新
        updateUi(false);
    }

    private void setEventEnable(bool isEnable) {
        eventSystem.enabled = isEnable;
    }

    private void updateUi(bool isImmidiate) {

        // ステータス表示
        statusView.UpdateView(isImmidiate);

        // 開拓ボタン
        growBtn.UpdateView();
    }

    private void goStage() {
        SceneTransition.Instance.ChangeScene(GameDefine.SceneNameStage);
    }
}
