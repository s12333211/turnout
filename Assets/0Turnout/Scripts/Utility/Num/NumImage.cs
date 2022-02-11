using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BigInteger = System.Numerics.BigInteger;

public class NumImage : NumComponent
{
    public Sprite[] numSprites;
    public int maxLength;
    public TextAlignment alignment;

    private int num;
    private Dictionary<int, Sprite> spriteMap = new Dictionary<int, Sprite>();
    private List<Image> imageList = new List<Image>();

    void Awake() {
        for (int i = 0; i < numSprites.Length || i < 10; i++) {
            spriteMap.Add(i, numSprites[i]);
        }
        for (int i = 0; i < maxLength; i++) {
            imageList.Add(createImage());
        }
    }

    protected override void _setNum(BigInteger num)
    {
        string str = num.ToString();

        // 足りないオブジェクトを作成
        if (maxLength <= 0) {
            for (int i = imageList.Count; i < str.Length; i++) {
                imageList.Add(createImage());
            }
        }

        // 作成
        float widthAll = 0;
        int length = str.Length;
        if (0 < maxLength) {
            length = Mathf.Min(maxLength, str.Length);
        }
        for (int i = 0; i < length; i++) {

            int val = int.Parse(str.Substring(length - i - 1, 1));
            Sprite sprite = spriteMap[val];

            Image img = imageList[i];
            img.sprite = sprite;
            img.SetNativeSize();

            widthAll += sprite.rect.width;
        }

        // 位置を合わせる
        float nowTotalWidth = 0;
        for (int i = 0; i < length; i++) {

            Image img = imageList[length - i - 1];
            img.gameObject.SetActive(true);

            Vector3 pos = img.transform.localPosition;
            pos.x = nowTotalWidth + img.sprite.rect.width / 2;
            switch (alignment) {
                case TextAlignment.Center:
                    pos.x -= (widthAll / 2);
                    break;
                case TextAlignment.Left:
                    pos.x -= widthAll;
                    break;
                case TextAlignment.Right:
                default:
                    break;
            }
            img.transform.localPosition = pos;

            nowTotalWidth += img.sprite.rect.width;
        }

        // 使わないオブジェクト非表示に
        for (int i = length; i < imageList.Count; i++) {
            Image img = imageList[i];
            img.gameObject.SetActive(false);
        }
    }

    private Image createImage() {
        GameObject obj = new GameObject();
        obj.transform.parent = this.transform;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        obj.SetActive(false);
        Image img = obj.GetComponent<Image>();
        if (img == null) {
            img = obj.AddComponent<Image>();
        }
        return img;
    }
}
