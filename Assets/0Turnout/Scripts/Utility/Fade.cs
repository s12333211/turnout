using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UniRx;

public class Fade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float duration = 0.8f;

#if UNITY_EDITOR
    private void OnValidate() {

        if (canvasGroup == null) {
            canvasGroup = this.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
#endif

    public void Show(System.Action callback = null) {
        if (this.gameObject.activeSelf) {
            return;
        }

        this.gameObject.SetActive(true);

        canvasGroup.alpha = 0f;

        canvasGroup.DOFade(1f, duration).OnComplete(() => {
            callback?.Invoke();
        });
    }

    public void Hide(System.Action callback = null) {
        if (!this.gameObject.activeSelf) {
            return;
        }

        // canvasGroup.alpha = 1f;
        canvasGroup.DOFade(0.1f, duration).OnComplete(() => {
            this.gameObject.SetActive(false);
            callback?.Invoke();
        });
    }
}
