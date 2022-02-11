using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public abstract class DispNumComponent : NumComponent
{
    public NumUtility.NumType numType = NumUtility.NumType.none;
    public string customUnit;

    public bool isFirstValue;
    public string firstValue;

    void Start() {
        if (isFirstValue) {
            _setText(firstValue);
        }
    }

    protected override void _setNum(BigInteger num) {
        string text = string.IsNullOrEmpty(customUnit)
                    ? NumUtility.MakeDispNum(num, numType)
                    : NumUtility.MakeDispNum(num, customUnit);
        _setText(text);
    }

    protected abstract void _setText(string dispNum);
}
