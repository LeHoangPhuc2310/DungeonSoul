// DungeonSoul — MinimapManager.cs — Fog-of-war minimap (90×90 dp, bottom-right).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [SerializeField] private RawImage minimapImage;
    [SerializeField] private int textureSize = 90;
    [SerializeField] private Color unrevealedColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Color roomColor = new Color(0.35f, 0.4f, 0.55f, 1f);
    [SerializeField] private Color playerColor = Color.white;
    [SerializeField] private Color bossColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    private Texture2D mapTexture;
    private readonly HashSet<int> revealedRooms = new HashSet<int>();
    private List<DungeonRoomNode> rooms = new List<DungeonRoomNode>();
    private Transform player;
    private float blinkTimer;
    private bool blinkOn;

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
        BuildMinimapUI();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    private void Update()
    {
        if (mapTexture == null || player == null)
            return;

        blinkTimer += Time.unscaledDeltaTime;
        if (blinkTimer > 0.5f)
        {
            blinkTimer = 0f;
            blinkOn = !blinkOn;
        }

        DrawMinimap();
    }

    public void Rebuild(IReadOnlyList<DungeonRoomNode> roomList, Vector2Int spawn)
    {
        rooms = roomList != null ? new List<DungeonRoomNode>(roomList) : new List<DungeonRoomNode>();
        revealedRooms.Clear();
        if (rooms.Count > 0)
            revealedRooms.Add(0);
        DrawMinimap();
    }

    public void RevealRoom(Vector3 worldPos)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].bounds.Contains(WorldToCell(worldPos)))
                revealedRooms.Add(i);
        }
    }

    private void DrawMinimap()
    {
        if (mapTexture == null)
            return;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = unrevealedColor;

        for (int r = 0; r < rooms.Count; r++)
        {
            if (!revealedRooms.Contains(r))
                continue;

            Color c = rooms[r].roomType == RoomType.Boss
                ? (blinkOn ? bossColor : unrevealedColor)
                : roomColor;
            FillRoomPixels(rooms[r], pixels, c);
        }

        if (player != null)
            DrawDot(WorldToCell(player.position), pixels, playerColor, 2);

        mapTexture.SetPixels(pixels);
        mapTexture.Apply();
    }

    private void FillRoomPixels(DungeonRoomNode room, Color[] pixels, Color color)
    {
        Vector2Int min = room.bounds.min;
        Vector2Int max = room.bounds.max;
        for (int x = min.x; x < max.x; x++)
        {
            for (int y = min.y; y < max.y; y++)
                SetPixel(x, y, pixels, color);
        }
    }

    private void DrawDot(Vector2Int cell, Color[] pixels, Color color, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
                SetPixel(cell.x + dx, cell.y + dy, pixels, color);
        }
    }

    private void SetPixel(int x, int y, Color[] pixels, Color color)
    {
        int mx = Mathf.RoundToInt((float)x / 64f * textureSize);
        int my = Mathf.RoundToInt((float)y / 48f * textureSize);
        if (mx < 0 || my < 0 || mx >= textureSize || my >= textureSize)
            return;
        pixels[my * textureSize + mx] = color;
    }

    private Vector2Int WorldToCell(Vector3 world)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
    }

    private void BuildMinimapUI()
    {
        if (minimapImage != null)
            return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject go = new GameObject("Minimap", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-16f, 100f);
        rt.sizeDelta = new Vector2(textureSize, textureSize);

        minimapImage = go.AddComponent<RawImage>();
        mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        mapTexture.filterMode = FilterMode.Point;
        minimapImage.texture = mapTexture;
        minimapImage.color = Color.white;
    }
}
