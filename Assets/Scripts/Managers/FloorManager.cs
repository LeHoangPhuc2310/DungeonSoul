using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public static int currentFloor = 1;
    public int CurrentFloor => currentFloor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdateHUD();
    }

    public void NextFloor()
    {
        currentFloor++;
        UpdateHUD();
        // Here you would typically trigger scene reload or dungeon regeneration
    }

    private void UpdateHUD()
    {
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateFloor(currentFloor);
        }
    }
}
