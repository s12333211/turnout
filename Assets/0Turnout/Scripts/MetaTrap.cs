using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator.Modules;
using System.Collections.Generic;
using UnityEngine;

public class MetaTrap : CurvyMetadataBase
{
    private BuildShapeExtrusion shapeExtrusion; //この機能今は使わない、Curvyで道の見た目生成する場合のみ使える
    [Header("途切れ線路の消える部分をここに設定")]
    [SerializeField] private List<GameObject> hideGameObjects = new List<GameObject>();
    [SerializeField] private Trap trapPrefab = null;
    [SerializeField] private Vector3 trapRotation = Vector3.zero;
    [SerializeField] private bool trapRotationAlignSpline = false;
    public Trap TrapObject { get; private set; } = null;

    private void OnValidate()
    {
        if (Spline != null && (shapeExtrusion == null || shapeExtrusion.transform.parent != Spline.transform.parent.parent))
        {
            shapeExtrusion = Spline.transform.parent.parent?.GetComponentInChildren<BuildShapeExtrusion>();
        }
    }

    private void Start() { }    //コンポーネントの有効状態を確認できるために空のStartを入れる

    public void InitTrap()
    {
        if (Application.isPlaying)
        {
            // 既存のギミックを削除
            if (TrapObject != null)
                Destroy(TrapObject.gameObject);
            // 有効の場合のみギミック生成
            if (enabled == true)
            {
                Quaternion rotation;
                if (trapRotationAlignSpline)
                    rotation = ControlPoint.GetOrientationFast(0);
                else
                    rotation = transform.rotation;
                rotation *= Quaternion.Euler(trapRotation);
                if (trapPrefab != null)
                    TrapObject = Instantiate(trapPrefab, transform.position, rotation, transform);
                if (hideGameObjects.Count > 0)
                {
                    foreach (var go in hideGameObjects)
                    {
                        if (go != null)
                            go.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetSplineHole()
    {
        // Curvyの変形設定のスケール設定で線路を途切れに
        if (shapeExtrusion == null)
            return;
        float radius = 0;    //TrapObject.Collider.radius;
        float tFRddius = radius / Spline.Length;
        shapeExtrusion.ScaleMode = BuildShapeExtrusion.ScaleModeEnum.Advanced;
        shapeExtrusion.ScaleUniform = true;
        float from = Spline.TFToDistance(ControlPoint.TF) / Spline.Length - tFRddius;
        float to = Spline.TFToDistance(ControlPoint.TF) / Spline.Length + tFRddius;
        if (to > from)
        {
            shapeExtrusion.ScaleMultiplierX = new AnimationCurve
            (
                new Keyframe(0f, 1f),
                new Keyframe(from, 1f),
                new Keyframe(from, 0f),
                new Keyframe(to, 0f),
                new Keyframe(to, 1f),
                new Keyframe(1f, 1f)
            );
        }
        else
        {
            shapeExtrusion.ScaleMultiplierX = new AnimationCurve
            (
                new Keyframe(0f, 0f),
                new Keyframe(to, 0f),
                new Keyframe(to, 1f),
                new Keyframe(from, 1f),
                new Keyframe(from, 0f),
                new Keyframe(1f, 0f)
            );
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isActiveAndEnabled == false)
            return;
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.5f);
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
        gUIStyle.normal.textColor = new Color(1f, 0f, 0f, 0.8f);
        UnityEditor.Handles.Label(transform.position, "障害", gUIStyle);
    }
#endif
}