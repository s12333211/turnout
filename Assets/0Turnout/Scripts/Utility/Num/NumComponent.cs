using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public abstract class NumComponent : MonoBehaviour
{
    public bool isDrumRoll;

    private BigInteger numNow = -1;
    private BigInteger numNowDisp = -1;

    private double numNowDrum;
    private TweenerCore<float, float, FloatOptions> drumTweener;

    public double GetNum() {
        return (double)numNow;
    }

    public void addNum(double num) {
        if (num == 0) {
            return;
        }
        double next = (double)numNow + num;
        SetNum(next);
    }

    public void AddNumImmidiate(double num) {
        if (num == 0) {
            return;
        }
        double next = (double)numNow + num;
        SetNumImmidiate(next);
    }

    public void SetNum(double num) {
        BigInteger tmp = new BigInteger(num);
        if (numNow != null && numNow == tmp) {
            return;
        }
        numNow = tmp;

        if (isDrumRoll) {
            numNowDrum = (double)(new BigInteger(num) - numNowDisp);
            numNow = new BigInteger(num);

            // 前のTweenを消す（途中とか関係なく）
            if (drumTweener != null) {
                drumTweener.Kill();
            }

            // ドラムロールのTween
            drumTweener = DOTween.To(
                () => 0f,
                (x) => {
                    BigInteger xBig = new BigInteger(numNowDrum * (1f - x));
                    SetNum(numNow - xBig);
                },
                1f,
                0.8f
            ).SetEase(Ease.OutQuart);
        } else {
            SetNum(numNow);
        }
    }

    public void SetNumImmidiate(double num) {
        BigInteger tmp = new BigInteger(num);
        if (numNow != null && numNow == tmp) {
            return;
        }
        numNow = tmp;
        
        SetNum(numNow);
    }

    public double GetNow() {
        return (double)numNow;
    }

    public double GetDramNow() {
        return (double)numNowDisp;
    }

    private void SetNum(BigInteger num) {
        numNowDisp = num;
        _setNum(num);
    }

    protected abstract void _setNum(BigInteger num);
}
