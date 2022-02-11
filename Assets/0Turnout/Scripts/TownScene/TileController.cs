using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UniRx;

public class TileController : MonoBehaviour
{
    [System.Serializable]
    private class TileLocation {
        public Vector2Int location;
        public VineteTile tile;
    }

    [SerializeField] private List<Transform> tileTopList;
    [SerializeField] private List<TileLocation> tileLocationList;

    private Dictionary<Vector2Int, TileLocation> tileLocationMap = new Dictionary<Vector2Int, TileLocation>();

    private ReactiveProperty<bool> initProperty = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> InitProperty => initProperty;

    public void Initialize() {
        StartCoroutine(init());
    }

    private IEnumerator init() {

        // 既に初期化済み
        if (InitProperty.Value) {
            initProperty.SetValueAndForceNotify(true);
            yield break;
        }

        // ロケーションを整理
        foreach (TileLocation tileLocation in tileLocationList) {
            tileLocationMap.Add(tileLocation.location, tileLocation);
        }

        // 現状の進行度取得
        int progress = PlayerPrefs.GetInt(GameDefine.VineteProgressKey, 0);

        // 成長の情報を集約
        Dictionary<Vector2Int, int> growthMap = new Dictionary<Vector2Int, int>();
        for (int i = 1; i <= progress; i++) {

            // ロード
            string mstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", i);
            ResourceRequest req = Resources.LoadAsync<VineteGrowth>(mstKey);

            // ロード完了を待つ
            yield return new WaitUntil(() => req.isDone);

            // 成長情報格納
            VineteGrowth vineteGrowth = (VineteGrowth)req.asset;
            if (vineteGrowth == null) {
                //TODO 次待ってねメッセージ？
                break;
            }
            
            if (!growthMap.ContainsKey(vineteGrowth.location)) {
                growthMap.Add(vineteGrowth.location, 0);
            }
            growthMap[vineteGrowth.location] += 1;
        }

        int maxTileNum = 1;

        // 作成
        foreach (KeyValuePair<Vector2Int, int> pair in growthMap) {

            Vector2Int location = pair.Key;
            int growth = pair.Value;

            // 最大のXを退避
            maxTileNum = Mathf.Max(maxTileNum, Mathf.Max(location.x, location.y));

            // 建物作成
            VineteTile tile = tileLocationMap[location].tile;
            tile.SetGrowth(growth);
        }

        // 不必要な階層を非表示
        for (int i = 0; i < maxTileNum; i++) {
            tileTopList[i].gameObject.SetActive(true);
        }

        // 次のタイル
        int needTileIndex = getNeedNextTileIndex(progress + 1);
        if (needTileIndex < tileTopList.Count) {
            for (int i = needTileIndex; maxTileNum <= i; i--) {
                tileTopList[i].gameObject.SetActive(true);
            }
        }
    }

    public void Grow(VineteGrowth vineteGrowth, int nextProgress, System.Action callback) {

        // 開拓
        tileLocationMap[vineteGrowth.location].tile.Grow(() => {

            // タイル拡張アニメーション
            int needTileIndex = getNeedNextTileIndex(nextProgress);
            if (needTileIndex < tileTopList.Count) {
                Transform tileTopTrans = tileTopList[needTileIndex];
                tileTopTrans.gameObject.SetActive(true);
                PlayableDirector director = tileTopTrans.GetComponent<PlayableDirector>();
                director.Play();

                // 終了時にコールバック
                Observable.Timer(System.TimeSpan.FromSeconds(director.duration)).Subscribe(_ => {
                    director.Stop();
                    callback();
                });
            } else {
                callback();
            }
        });
    }

    private int getNeedNextTileIndex(int nextProgress) {

        // 次のマスタをロード
        string nextMstKey = string.Format("MasterData/VineteGrowth/VineteGrowth_{0:000}", nextProgress);
        VineteGrowth nextVineteGrowth = Resources.Load<VineteGrowth>(nextMstKey);

        // 次の拡張はない
        if (nextVineteGrowth == null) {
            return tileTopList.Count;
        }

        // 次に必要なタイルトップ
        int needTileIndex = Mathf.Max(nextVineteGrowth.location.x, nextVineteGrowth.location.y) - 1;
        Transform tileTopTrans = tileTopList[needTileIndex];

        // タイル拡張アニメーションのインデックス
        return tileTopTrans.gameObject.activeSelf ? tileTopList.Count : needTileIndex;
    }
}
