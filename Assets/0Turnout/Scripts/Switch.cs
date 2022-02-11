using Cinemachine;
using DG.Tweening;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;

public class Switch : MonoBehaviour
{
    [SerializeField] private Transform diretionObject = null;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private ParticleSystem changeDirectionEffect = null;
    [SerializeField] private Animator arrowAnimator = null;
    [SerializeField] private CurvyGenerator arrowGenerator = null;
    [SerializeField] private CurvySplineSegment[] arrowControlPoints = null;
    [SerializeField] private Renderer[] arrowRenderers = null;
    [SerializeField] private Material arrowDefaultMaterial = null;
    [SerializeField] private Material arrowFocusMaterial = null;
    private bool state = false;
    private PathDirection toDirectionNow;
    [Header("矢印の高さ")]
    [SerializeField] private float arrowHeight = 10;
    [Header("矢印の線のパスセグメントの長さ")]
    [SerializeField] private float arrowLengthPerSegment = 7;

    private void Awake()
    {
        SetFocus(false);
    }

    private void LateUpdate()
    {
        diretionObject.rotation = Quaternion.LookRotation(-Camera.main.transform.up, -Camera.main.transform.forward);
        diretionObject.rotation *= Quaternion.AngleAxis(-diretionObject.rotation.eulerAngles.y, Vector3.up);
    }

    public void SetDirection(PathDirection toDirection)
    {
        if (state && toDirection != toDirectionNow)
            changeDirectionEffect.Play();
        toDirectionNow = toDirection;
        // 矢印の座標をリセット
        diretionObject.transform.localPosition = Vector3.zero;
        diretionObject.transform.rotation = Quaternion.identity;
        // 矢印の形をサンプリング
        float direction = arrowLengthPerSegment * (toDirection.movementDirection == MovementDirection.Forward ? 1 : -1);
        for (int i = 0; i < arrowControlPoints.Length; i++)
        {
            var position = toDirection.controlPoint.Spline.InterpolateByDistance(toDirection.controlPoint.Distance + direction * 2 * i, Space.World);
            arrowControlPoints[i].SetLocalPosition((position - transform.position) / 2);
        }
        // 矢印の高さを設定
        diretionObject.transform.localPosition = new Vector3(0, arrowHeight, 0);
        // 矢印の見た目を更新
        arrowControlPoints[arrowControlPoints.Length - 1].Spline.Refresh();
        arrowControlPoints[arrowControlPoints.Length - 1].BakeOrientationToTransform();
        arrowGenerator.Refresh(true);
    }

    public void SetFocus(bool state)
    {
        this.state = state;
        if (state == true)
        {
            arrowAnimator.speed = 1;
            foreach (var renderer in arrowRenderers)
            {
                renderer.sharedMaterial = arrowFocusMaterial;
            }
        }
        else
        {
            arrowAnimator.speed = 0;
            foreach (var renderer in arrowRenderers)
            {
                renderer.sharedMaterial = arrowDefaultMaterial;
            }
        }
    }
}