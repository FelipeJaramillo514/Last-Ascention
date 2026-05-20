using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private Vector2Int textureSize = new Vector2Int(50, 50);
    [SerializeField] private Vector2Int displaySize = new Vector2Int(100, 100);

    private Texture2D minimapTexture;
    private DungeonBuilder builder;
    private DungeonLayout layout;
    private DungeonRoom currentRoom;
    private bool initialized;

    private void Awake()
    {
        EnsureUiExists();
        CreateTexture();
    }

    public void Initialize(DungeonBuilder dungeonBuilder, DungeonLayout dungeonLayout)
    {
        builder = dungeonBuilder;
        layout = dungeonLayout;
        initialized = builder != null && layout != null;
        EnsureUiExists();
        CreateTexture();
        Redraw();
    }

    public void SetCurrentRoom(DungeonRoom room)
    {
        currentRoom = room;
        if (currentRoom != null)
        {
            currentRoom.MarkVisited();
        }

        if (initialized)
        {
            Redraw();
        }
    }

    private void Update()
    {
        if (!initialized || currentRoom == null)
        {
            return;
        }

        Redraw();
    }

    private void EnsureUiExists()
    {
        if (minimapImage != null)
        {
            return;
        }

        Canvas canvas = FindOverlayCanvas();
        Transform frame = canvas.transform.Find("DungeonMinimapFrame");
        if (frame == null)
        {
            GameObject frameObject = new GameObject("DungeonMinimapFrame", typeof(RectTransform), typeof(Image));
            frame = frameObject.transform;
            frame.SetParent(canvas.transform, false);
        }

        Image frameImage = frame.GetComponent<Image>();
        frameImage.sprite = HUDSpriteFactory.WhiteSprite;
        frameImage.color = Color.black;

        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 0f);
        frameRect.anchorMax = new Vector2(0f, 0f);
        frameRect.pivot = new Vector2(0f, 0f);
        frameRect.anchoredPosition = new Vector2(16f, 56f);
        frameRect.sizeDelta = displaySize + new Vector2Int(2, 2);

        Transform rawImageTransform = frame.Find("DungeonMinimap");
        if (rawImageTransform == null)
        {
            GameObject rawImageObject = new GameObject("DungeonMinimap", typeof(RectTransform), typeof(RawImage));
            rawImageTransform = rawImageObject.transform;
            rawImageTransform.SetParent(frame, false);
        }

        minimapImage = rawImageTransform.GetComponent<RawImage>();
        RectTransform rectTransform = minimapImage.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = new Vector2(1f, 1f);
        rectTransform.sizeDelta = displaySize;
    }

    private void CreateTexture()
    {
        if (minimapImage == null)
        {
            return;
        }

        if (minimapTexture != null && minimapTexture.width == textureSize.x && minimapTexture.height == textureSize.y)
        {
            minimapImage.texture = minimapTexture;
            return;
        }

        minimapTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
        minimapTexture.filterMode = FilterMode.Point;
        minimapTexture.wrapMode = TextureWrapMode.Clamp;
        minimapImage.texture = minimapTexture;
    }

    private void Redraw()
    {
        if (minimapTexture == null || layout == null || builder == null)
        {
            return;
        }

        Color32 clear = new Color32(10, 10, 10, 220);
        Color32[] pixels = minimapTexture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }
        minimapTexture.SetPixels32(pixels);

        int cellWidth = textureSize.x / DungeonLayout.GridSize;
        int cellHeight = textureSize.y / DungeonLayout.GridSize;
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            DungeonLayout.RoomNode node = layout.Rooms[i];
            DungeonRoom room = builder.GetRoomByIndex(node.roomIndex);
            bool isVisited = room != null && room.IsVisited;
            Color color = GetRoomColor(node.type, isVisited);
            DrawCell(node.gridPos, cellWidth, cellHeight, color);
            DrawSpecialMarker(node, cellWidth, cellHeight);
        }

        if (currentRoom != null)
        {
            DrawPlayerMarker(currentRoom.GridPosition, cellWidth, cellHeight);
        }

        minimapTexture.Apply();
    }

    private Color GetRoomColor(RoomType roomType, bool visited)
    {
        if (roomType == RoomType.Entry)
        {
            return Color.white;
        }

        if (roomType == RoomType.Normal)
        {
            return visited ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.black;
        }

        if (roomType == RoomType.Boss)
        {
            return new Color(0.3f, 0.06f, 0.06f, 1f);
        }

        if (roomType == RoomType.Shop)
        {
            return new Color(0.76f, 0.62f, 0.12f, 1f);
        }

        return new Color(0.12f, 0.32f, 0.75f, 1f);
    }

    private void DrawCell(Vector2Int gridPosition, int cellWidth, int cellHeight, Color color)
    {
        int startX = gridPosition.x * cellWidth;
        int startY = gridPosition.y * cellHeight;
        for (int y = startY + 1; y < startY + cellHeight - 1; y++)
        {
            for (int x = startX + 1; x < startX + cellWidth - 1; x++)
            {
                minimapTexture.SetPixel(x, y, color);
            }
        }
    }

    private void DrawSpecialMarker(DungeonLayout.RoomNode node, int cellWidth, int cellHeight)
    {
        if (node.type == RoomType.Entry || node.type == RoomType.Normal)
        {
            return;
        }

        int startX = node.gridPos.x * cellWidth;
        int startY = node.gridPos.y * cellHeight;
        int centerX = startX + cellWidth / 2;
        int centerY = startY + cellHeight / 2;

        if (node.type == RoomType.Boss)
        {
            Color color = new Color(0.85f, 0.12f, 0.12f, 1f);
            DrawPattern(centerX, centerY, new[]
            {
                "010",
                "111",
                "101"
            }, color);
            return;
        }

        if (node.type == RoomType.Shop)
        {
            Color color = new Color(1f, 0.9f, 0.22f, 1f);
            DrawPattern(centerX, centerY, new[]
            {
                "010",
                "111",
                "010"
            }, color);
            return;
        }

        DrawPattern(centerX, centerY, new[]
        {
            "111",
            "010",
            "010"
        }, new Color(0.3f, 0.7f, 1f, 1f));
    }

    private void DrawPlayerMarker(Vector2Int gridPosition, int cellWidth, int cellHeight)
    {
        int startX = gridPosition.x * cellWidth;
        int startY = gridPosition.y * cellHeight;
        int centerX = startX + cellWidth / 2;
        int centerY = startY + cellHeight / 2;
        float blink = Mathf.Lerp(0.45f, 1f, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
        DrawPattern(centerX, centerY, new[]
        {
            "010",
            "111",
            "010"
        }, new Color(blink, blink, blink, 1f));
    }

    private void DrawPattern(int centerX, int centerY, string[] pattern, Color color)
    {
        int halfWidth = pattern[0].Length / 2;
        int halfHeight = pattern.Length / 2;
        for (int y = 0; y < pattern.Length; y++)
        {
            for (int x = 0; x < pattern[y].Length; x++)
            {
                if (pattern[y][x] != '1')
                {
                    continue;
                }

                int pixelX = centerX + x - halfWidth;
                int pixelY = centerY + (pattern.Length - 1 - y) - halfHeight;
                if (pixelX < 0 || pixelX >= minimapTexture.width || pixelY < 0 || pixelY >= minimapTexture.height)
                {
                    continue;
                }

                minimapTexture.SetPixel(pixelX, pixelY, color);
            }
        }
    }

    private Canvas FindOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        GameObject canvasObject = new GameObject("HUDCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }
}
