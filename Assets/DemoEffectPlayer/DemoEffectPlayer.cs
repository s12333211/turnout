using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoEffectPlayer : MonoBehaviour
{
    public List<ObjInfo> collideObjInfoList;
    public Text coinText;
    public Animator coinTextAnimator;

    public ObjectEventHandlerBase handler;

    private Collider selfCollider = null;
    private Animation animationComponent;
    private const float animationCrossfadeTimeLength = 0.2f;

    public enum EffectTarget
    {
        Obj,
        Me,
        ObjColliderCenter,
        MeColliderCenter,
    }

    [System.Serializable]
    public class ObjInfo
    {
        public string tag;

        [Header("何秒毎に発生できる(マイナスは一回のみ)")]
        public float performInterval = 0;
        [HideInInspector]
        public float enterCooldown;

        [Header("何かが貯まる量(coinText必要)")]
        public int coin = 0;

        [Header("相手が消えるまでの秒数(マイナスは消えない)")]
        public float destroySec = 0;

        [Header("目標設定")]
        public EffectTarget effectTarget;

        [Header("エフェクト設定"), Space(15)]
        public bool isEffect = true;
        public ParticleSystem effectParticle;
        [Range(0.01f, 100f)] public float effectScale = 1;

        [Header("接触時の連続エフェクト設定"), Space(15)]
        public bool isContinuousEffect;
        public float continuousInterval;
        [HideInInspector]
        public float stayCooldown;

        [Header("吹っ飛び設定"), Space(15)]
        public bool isImpulse = true;
        public Vector3 directionMin = new Vector3(-80, 20, 80);
        public Vector3 directionMax = new Vector3(80, 50, 150);
        public ForceMode forceMode = ForceMode.VelocityChange;

        [Header("アニメーション設定"), Space(15)]
        public bool isAnimation = false;
        [Tooltip("このアニメーションクリップは強制的にLegacyに設定。戻したいならDebugに切り替えてLegacyを外す")]
        public AnimationClip animationClip;
        public bool isLoopAnimation = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (handler == null)
        {
            handler = GetComponent<ObjectEventHandlerBase>();
        }
        // Set Default Value
        foreach (ObjInfo info in collideObjInfoList)
        {
            if (info.tag == "")
            {
                if (info.effectScale == 0)
                    info.effectScale = 1;
                if (info.forceMode == ForceMode.Force)
                    info.forceMode = ForceMode.VelocityChange;
            }
        }
    }
#endif

    void Start()
    {
        handler.onEnter += objectEvent =>
        {
            int coin = 0;

            foreach (ObjInfo info in collideObjInfoList)
            {
                if (!objectEvent.GetGameObject().CompareTag(info.tag) || info.enterCooldown > 0)
                {
                    info.stayCooldown = Mathf.Infinity;     //Continuous Effect Would not spawn
                    continue;
                }
                if (info.performInterval < 0)
                    info.enterCooldown = Mathf.Infinity;    //Would not be trigger again
                else
                    info.enterCooldown = info.performInterval;
                info.stayCooldown = info.continuousInterval;

                coin += info.coin;

                if (0 <= info.destroySec)
                {
                    Destroy(objectEvent.GetGameObject(), info.destroySec);
                }

                if (info.isEffect && info.effectParticle != null)
                {
                    Emit(info, objectEvent);
                }

                if (info.isImpulse)
                {
                    Vector3 direction;
                    direction.x = Random.Range(info.directionMin.x, info.directionMax.x);
                    direction.y = Random.Range(info.directionMin.y, info.directionMax.y);
                    direction.z = Random.Range(info.directionMin.z, info.directionMax.z);

                    objectEvent.AddForce(direction, info.forceMode);
                }

                if (info.isAnimation)
                {
                    PlayAnimation(info, objectEvent);
                }
            }

            if (0 < coin && coinText != null)
            {
                int val = 0;
                int.TryParse(coinText.text, out val);
                val += coin;

                coinTextAnimator?.SetTrigger("Strong");
                coinText.text = val.ToString();
            }
        };

        handler.onStay += objectEvent =>
        {
            foreach (ObjInfo info in collideObjInfoList)
            {
                if (!objectEvent.GetGameObject().CompareTag(info.tag))
                {
                    continue;
                }

                if (info.isContinuousEffect)
                {
                    info.stayCooldown -= Time.deltaTime;
                    if (info.stayCooldown <= 0)
                    {
                        Emit(info, objectEvent);
                        info.stayCooldown = info.continuousInterval;
                    }
                }
            }
        };

        handler.onExit += objectEvent =>
        {
            foreach (ObjInfo info in collideObjInfoList)
            {
                if (!objectEvent.GetGameObject().CompareTag(info.tag))
                {
                    continue;
                }

                if (info.isLoopAnimation)
                {
                    if (animationComponent != null)
                        animationComponent.Stop();
                }
            }
        };
    }

    private void Update()
    {
        foreach (ObjInfo info in collideObjInfoList)
        {
            info.enterCooldown -= Time.deltaTime;
        }
    }

    private void Emit(ObjInfo info, ObjectEvent objectEvent)
    {
        ParticleSystem particle = Instantiate(info.effectParticle);
        switch (info.effectTarget)
        {
            case EffectTarget.Obj:
                particle.transform.position = objectEvent.GetTransform().position;
                break;
            case EffectTarget.Me:
                particle.transform.position = transform.position;
                break;
            case EffectTarget.ObjColliderCenter:
                particle.transform.position = objectEvent.GetBounds().center;
                break;
            case EffectTarget.MeColliderCenter:
                if (selfCollider == null)
                    selfCollider = GetComponent<Collider>();
                if (selfCollider != null)
                    particle.transform.position = selfCollider.bounds.center;
                else
                    particle.transform.position = transform.position;
                break;
        }
        particle.transform.localScale = Vector3.one * info.effectScale;
        Destroy(particle.gameObject, particle.main.duration);
    }

    private void PlayAnimation(ObjInfo info, ObjectEvent objectEvent)
    {
        GameObject target = null;
        switch (info.effectTarget)
        {
            case EffectTarget.Obj:
            case EffectTarget.ObjColliderCenter:
                target = objectEvent.GetGameObject();
                break;
            case EffectTarget.Me:
            case EffectTarget.MeColliderCenter:
                target = gameObject;
                break;
        }
        if (animationComponent == null || animationComponent.gameObject != target)
        {
            animationComponent = target.GetComponent<Animation>();
            if (animationComponent == null)
                animationComponent = target.AddComponent<Animation>();
            animationComponent.playAutomatically = false;
        }
        if (animationComponent.GetClip(info.animationClip.name) == null)
        {
            info.animationClip.legacy = true;
            animationComponent.AddClip(info.animationClip, info.animationClip.name);
        }

        animationComponent.CrossFade(info.animationClip.name, animationCrossfadeTimeLength);
        if (info.isLoopAnimation)
            animationComponent.wrapMode = WrapMode.Loop;
        else
            animationComponent.wrapMode = WrapMode.Once;
    }
}