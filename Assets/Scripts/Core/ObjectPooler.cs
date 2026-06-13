// DungeonSoul — ObjectPooler.cs — Simple prefab pools (projectile, enemy, VFX).

using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [SerializeField] private List<PoolEntry> pools = new List<PoolEntry>();

    private readonly Dictionary<string, Queue<GameObject>> poolLookup = new Dictionary<string, Queue<GameObject>>();

    [System.Serializable]
    private class PoolEntry
    {
        public string key;
        public GameObject prefab;
        public int prewarm = 8;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PrewarmAll();
    }

    private void PrewarmAll()
    {
        for (int i = 0; i < pools.Count; i++)
            PrewarmEntry(pools[i]);
    }

    /// <summary>Đăng ký pool lúc runtime (vd EnemySpawner gọi khi có prefab).</summary>
    public void RegisterRuntimePool(string key, GameObject prefab, int prewarm = 16)
    {
        if (string.IsNullOrEmpty(key) || prefab == null)
            return;

        if (poolLookup.ContainsKey(key))
            return;

        PoolEntry entry = new PoolEntry { key = key, prefab = prefab, prewarm = prewarm };
        pools.Add(entry);
        PrewarmEntry(entry);
    }

    private void PrewarmEntry(PoolEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.key) || entry.prefab == null)
            return;

        if (!poolLookup.ContainsKey(entry.key))
            poolLookup[entry.key] = new Queue<GameObject>();

        int count = Mathf.Max(0, entry.prewarm);
        for (int n = 0; n < count; n++)
        {
            GameObject obj = RuntimeSpawnGuard.Mark(Instantiate(entry.prefab, transform));
            obj.SetActive(false);
            poolLookup[entry.key].Enqueue(obj);
        }
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolLookup.TryGetValue(key, out Queue<GameObject> queue))
            return null;

        GameObject obj = queue.Count > 0 ? queue.Dequeue() : CreateFromKey(key);
        if (obj == null)
            return null;

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Return(string key, GameObject obj)
    {
        if (obj == null || string.IsNullOrEmpty(key))
            return;

        obj.SetActive(false);
        obj.transform.SetParent(transform, false);

        if (!poolLookup.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolLookup[key] = queue;
        }

        queue.Enqueue(obj);
    }

    private GameObject CreateFromKey(string key)
    {
        for (int i = 0; i < pools.Count; i++)
        {
            if (pools[i] != null && pools[i].key == key && pools[i].prefab != null)
                return RuntimeSpawnGuard.Mark(Instantiate(pools[i].prefab, transform));
        }

        return null;
    }
}
