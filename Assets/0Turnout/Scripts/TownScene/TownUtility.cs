using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TownUtility
{
    public static bool CanGrowTown() {

        int progress = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);
        int checkProgress = progress + 1;

        // マスタをロード
        string mstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", checkProgress);
        VineteGrowth vineteGrowth = Resources.Load<VineteGrowth>(mstKey);

        if (vineteGrowth == null) {
            return false;
        }

        int coin = PlayerPrefs.GetInt(GameDefine.CoinKey, 0);
        return vineteGrowth.needCoin <= coin;
    }
}
