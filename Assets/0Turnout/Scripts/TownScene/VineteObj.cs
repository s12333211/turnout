using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UniRx;

public class VineteObj : MonoBehaviour
{
    [SerializeField] private List<Transform> vineteList;
    [SerializeField] private List<Animator> animatorList;
    [SerializeField] private PlayableDirector playableDirector;

    void Start() {
        // TimelineAsset timeline = (TimelineAsset)playableDirector.playableAsset;
        // ActivationTrack track = (ActivationTrack)timeline.GetRootTrack(0);
    }

    public int GetGrowth() {
        for (int i = 0; i < vineteList.Count; i++) {
            if (vineteList[i].gameObject.activeSelf) {
                return i + 1;
            }
        }
        return 0;
    }

    public void SetGrowth(int growth) {
        int index = growth - 1;
        for (int i = 0; i < vineteList.Count; i++) {
            vineteList[i].gameObject.SetActive(i == index);
        }
    }

    public void Grow(int growth, System.Action callback) {
        int index = growth - 1;
        vineteList[index].gameObject.SetActive(true);

        PlayableBinding bindingObj = playableDirector.playableAsset.outputs.First(item => item.streamName == "AppearObj");
        PlayableBinding bindingAnimator = playableDirector.playableAsset.outputs.First(item => item.streamName == "AppearAnimator");

        playableDirector.SetGenericBinding(bindingObj.sourceObject, vineteList[index]);
        playableDirector.SetGenericBinding(bindingAnimator.sourceObject, animatorList[index]);

        playableDirector.Play();

        // 表示していたものを非表示
        Observable.Timer(System.TimeSpan.FromSeconds(0.2f)).Subscribe(_ => {
            for (int i = 0; i < vineteList.Count; i++) {
                if (i != index) {
                    vineteList[i].gameObject.SetActive(false);
                }
            }
        }).AddTo(this.gameObject);

        // 終了時にコールバック
        Observable.Timer(System.TimeSpan.FromSeconds(playableDirector.duration)).Subscribe(_ => {
            playableDirector.Stop();
            callback();
        });
    }

#if UNITY_EDITOR
    public void SetAnimators() {

        List<Animator> list = new List<Animator>();

        for (int i = 0; i < vineteList.Count; i++) {
            Transform trans = vineteList[i];
            if (trans.childCount <= 0) {
                Debug.LogErrorFormat("{0}個目のオブジェクトにVineteがありません", (i + 1));
                return;
            }

            Transform child = trans.GetChild(0);
            Animator animator = child.GetComponent<Animator>();
            if (animator == null) {
                animator = child.gameObject.AddComponent<Animator>();
                Debug.LogFormat("{0}個目のオブジェクトにアニメーターを作成しました", (i + 1));
            }

            list.Add(animator);
        }

        animatorList.Clear();
        animatorList = list;

        UnityEditor.EditorUtility.SetDirty(this.gameObject);
    }
#endif

}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(VineteObj))]
public class VineteObjEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {

        base.OnInspectorGUI();

        VineteObj obj = target as VineteObj;
        //ボタンを表示
        if (GUILayout.Button("アニメーター設定")) {
            obj.SetAnimators();
        }
    }
}
#endif
