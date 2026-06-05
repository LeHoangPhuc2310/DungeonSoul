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
        {
            PoolEntry entry = pools[i];
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.prefab == null)
                continue;

            if (!poolLookup.ContainsKey(entry.key))
                poolLookup[entry.key] = new Queue<GameObject>();

            for (int n = 0; n < entry.prewarm; n++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                obj.SetActive(false);
                poolLookup[entry.key].Enqueue(obj);
            }
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
                return Instantiate(pools[i].prefab, transform);
        }

        return null;
    }
}
