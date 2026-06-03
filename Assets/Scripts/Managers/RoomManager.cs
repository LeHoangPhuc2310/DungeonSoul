using UnityEngine;

public enum RoomType
{
    Normal,
    Elite,
    Treasure,
    Healing,
    Shop,
    Forge,
    Curse,
    Mystery,
    Challenge,
    Boss
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [SerializeField] private int roomsPerFloor = 5;
    [SerializeField] private int currentRoomIndex = 1;

    public RoomType CurrentRoomType { get; private set; } = RoomType.Normal;
    public int CurrentRoomIndex => currentRoomIndex;
    public int RoomsPerFloor => roomsPerFloor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        EnterNextRoom();
    }

    public void EnterNextRoom()
    {
        if (currentRoomIndex > roomsPerFloor)
        {
            FloorManager.Instance?.NextFloor();
            currentRoomIndex = 1;
        }

        CurrentRoomType = RollRoomType(currentRoomIndex);
        ApplyRoomType(CurrentRoomType);
        currentRoomIndex++;

        if (HUDManager.Instance != null && FloorManager.Instance != null)
            HUDManager.Instance.UpdateFloor(FloorManager.Instance.CurrentFloor);
    }

    public void OnRoomCleared()
    {
        if (CurrentRoomType == RoomType.Boss)
            FloorManager.Instance?.NextFloor();

        EnterNextRoom();
    }

    private static RoomType RollRoomType(int roomIndex)
    {
        if (roomIndex >= 5)
            return RoomType.Boss;

        float roll = Random.value;
        if (roll < 0.35f) return RoomType.Normal;
        if (roll < 0.47f) return RoomType.Elite;
        if (roll < 0.55f) return RoomType.Treasure;
        if (roll < 0.63f) return RoomType.Healing;
        if (roll < 0.71f) return RoomType.Shop;
        if (roll < 0.76f) return RoomType.Forge;
        if (roll < 0.81f) return RoomType.Curse;
        if (roll < 0.86f) return RoomType.Mystery;
        if (roll < 0.91f) return RoomType.Challenge;
        return RoomType.Normal;
    }

    private void ApplyRoomType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Healing:
                HealPlayer(0.3f);
                break;
            case RoomType.Treasure:
                SkillSelectionUI.GetOrFind()?.ShowChest(RoomType.Treasure);
                break;
            case RoomType.Shop:
                Debug.Log("[Room] Shop room — hook MetaShop UI here.");
                break;
        }

        Debug.Log("[Room] Enter " + type + " (" + currentRoomIndex + "/" + roomsPerFloor + ")");
    }

    private static void HealPlayer(float percent)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;
        HealthSystem hs = player.GetComponent<HealthSystem>();
        if (hs != null)
            hs.Heal(hs.MaxHP * percent);
    }
}
