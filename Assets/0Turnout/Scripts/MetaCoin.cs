using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator.Modules;
using UnityEngine;

public class MetaCoin : CurvyMetadataBase
{
    public GameObject coinPrefab = null;
    [SerializeField] private Vector3 positionOffset = new Vector3(0, 3, 0);
    //[SerializeField] private int number = 4;
    //[SerializeField] private float distance = 10;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // 編集中、いらないコイン設定を勝手に外してくれる
            if (isActiveAndEnabled == false)
            {
                Debug.LogWarning("無効化のオブジェクトにコイン追加できません、消滅します。");
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(this);
                };
            }
            else if (ControlPoint.Connection != null)
            {
                foreach (var controlPoint in ControlPoint.Connection.ControlPointsList)
                {
                    if (controlPoint == ControlPoint)
                        continue;
                    if (controlPoint.GetMetadata<MetaCoin>() != null)
                    {
                        Debug.LogWarning("パスの繋がりの複数コントロールポイントの内一つ設定すればいいです、消滅します。");
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            DestroyImmediate(this);
                        };
                    }
                }
            }
        }
    }
#endif

    protected void Start()
    {
        if (Application.isPlaying)
        {
            if (coinPrefab != null)
            {
                Instantiate(coinPrefab, transform.position + positionOffset, transform.rotation, transform);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isActiveAndEnabled == false)
            return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, 3f);
        GUIStyle gUIStyle = new GUIStyle();
        // テキストサイズ
        float zoom = Camera.current.orthographic == true ? Camera.current.orthographicSize : Vector3.Distance(Camera.current.transform.position, transform.position) / 2;
        gUIStyle.fontSize = Mathf.FloorToInt(512 / zoom);
        // 中央に寄せるための設定
        gUIStyle.fixedWidth = 1;
        gUIStyle.fixedHeight = 1;
        gUIStyle.alignment = TextAnchor.MiddleCenter;
        // テキスト色
        gUIStyle.normal.textColor = new Color(1f, 1f, 0f, 0.8f);
        UnityEditor.Handles.Label(transform.position, "コイン", gUIStyle);
    }
#endif
}