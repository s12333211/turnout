using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DispNumTextMeshPro : DispNumComponent
{
    public string prifix;
    public string postfix;
    private TextMeshProUGUI text;
    
    protected override void _setText(string dispNum) {

        if (text == null) {
            text = GetComponent<TextMeshProUGUI>();
        }

        text.text = prifix + dispNum + postfix;
    }
}