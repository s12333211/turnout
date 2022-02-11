using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class GrowBtn : MonoBehaviour
{
    [SerializeField] private RectTransform growOk;
    [SerializeField] private RectTransform growNg;
    [SerializeField] private Button btn;
    [SerializeField] private Text needCoinOk;
    [SerializeField] private Text needCoinNg;
    
    void Start()
    {
        // 開拓
        btn.OnClickAsObservable().Subscribe(_ => {
            TownManager.Instance.Grow();
        }).AddTo(this.gameObject);
    }

    public void UpdateView() {

        int coin = PlayerPrefs.GetInt(GameDefine.CoinKey, 0);
        int progress = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);
        int checkProgress = progress + 1;

        // マスタをロード
        string mstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", checkProgress);
        VineteGrowth vineteGrowth = Resources.Load<VineteGrowth>(mstKey);

        // 必要コイン
        string needCoinStr = vineteGrowth != null ? NumUtility.MakeCommaNum(vineteGrowth.needCoin) : "COMING\nSOON";
        needCoinOk.text = needCoinStr;
        needCoinNg.text = needCoinStr;

        // ボタン表示切り替え
        bool isEnable = vineteGrowth != null && vineteGrowth.needCoin <= coin;
        growOk.gameObject.SetActive(isEnable);
        growNg.gameObject.SetActive(!isEnable);
    }
}
