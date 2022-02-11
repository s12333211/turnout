using System.Collections.Generic;
using UnityEngine;

public class CargoWagon : MonoBehaviour
{
    [field: SerializeField, Header("ローカルサイズ")] public Bounds WagonSizeBounds { get; private set; }
    [SerializeField] private List<GameObject> cargoPrefabList = new List<GameObject>();
    private GameObject cargo;

    const float cargoMass = 50f;

    public void Init()
    {
        // 貨物初期化
        if (cargo != null)
            Destroy(cargo);
        //貨物を乗せる
        if (cargoPrefabList.Count > 0)
        {
            cargo = Instantiate(cargoPrefabList[Random.Range(0, cargoPrefabList.Count)], transform);
        }
    }

    public void Depart(Vector3 velocity)
    {
        // 貨物にRigidbodyと速度を与える
        var colliders = cargo.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            var rigidbodyCargo = collider.GetComponent<Rigidbody>();
            if (rigidbodyCargo == null)
            {
                rigidbodyCargo = collider.gameObject.AddComponent<Rigidbody>();
            }
            rigidbodyCargo.mass = cargoMass;
            rigidbodyCargo.velocity = velocity;
        }
    }
}
