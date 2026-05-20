using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PoolBootstrapEntry
{
    public string key = "EnemyProjectile";
    public GameObject prefab;
    public int size = 30;
}

public interface IPoolable
{
    void OnSpawn(Vector2 position, Vector2 direction, float damage);
    void OnDespawn();
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private List<PoolBootstrapEntry> bootstrapPools = new List<PoolBootstrapEntry>();

    private readonly Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, GameObject> registeredPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeBootstrapPools();
    }

    public GameObject GetFromPool(string key, GameObject prefab)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            key = prefab != null ? prefab.name : "PoolObject";
        }

        EnsurePoolExists(key);
        if (prefab != null)
        {
            registeredPrefabs[key] = prefab;
        }

        GameObject obj = null;
        while (pools[key].Count > 0 && obj == null)
        {
            obj = pools[key].Dequeue();
        }

        if (obj == null)
        {
            if (prefab == null && !registeredPrefabs.TryGetValue(key, out prefab))
            {
                Debug.LogError(string.Format("PoolManager could not spawn object for key '{0}' because no prefab is registered.", key));
                return null;
            }

            obj = Instantiate(prefab, transform);
        }

        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(string key, GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            key = obj.name.Replace("(Clone)", string.Empty).Trim();
        }

        EnsurePoolExists(key);

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        obj.transform.SetParent(transform);
        obj.SetActive(false);
        pools[key].Enqueue(obj);
    }

    private void InitializeBootstrapPools()
    {
        for (int i = 0; i < bootstrapPools.Count; i++)
        {
            var definition = bootstrapPools[i];
            if (definition == null || definition.prefab == null)
            {
                continue;
            }

            EnsurePoolExists(definition.key);
            registeredPrefabs[definition.key] = definition.prefab;
            for (int j = pools[definition.key].Count; j < definition.size; j++)
            {
                var instance = Instantiate(definition.prefab, transform);
                instance.SetActive(false);
                pools[definition.key].Enqueue(instance);
            }
        }
    }

    private void EnsurePoolExists(string key)
    {
        if (pools.ContainsKey(key))
        {
            return;
        }

        pools[key] = new Queue<GameObject>();
    }
}
