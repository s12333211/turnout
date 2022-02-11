using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DispNumText : DispNumComponent
{
    private Text text;
    
    protected override void _setText(string dispNum) {

        if (text == null) {
            text = GetComponent<Text>();
        }

        text.text = dispNum;
    }
}
