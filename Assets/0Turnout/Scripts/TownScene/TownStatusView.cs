using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TownStatusView : MonoBehaviour
{
    [SerializeField] private Text stageText;
    [SerializeField] private DispNumText coinText;

    public void UpdateView(bool isImmidiate) {

        // タウンレベル
        int townLevel = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);
        stageText.text = townLevel.ToString();

        // コイン
        int coin = PlayerPrefs.GetInt(GameDefine.CoinKey, 0);
        if (isImmidiate) {
            coinText.SetNumImmidiate(coin);
        } else {
            coinText.SetNum(coin);
        }
        
    }
}
