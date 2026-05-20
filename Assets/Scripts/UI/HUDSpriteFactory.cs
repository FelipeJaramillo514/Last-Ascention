using UnityEngine;

public enum PixelHeartState
{
    Full,
    Half,
    Empty
}

public static class HUDSpriteFactory
{
    private static Sprite whiteSprite;
    private static Sprite heartFullSprite;
    private static Sprite heartHalfSprite;
    private static Sprite heartEmptySprite;
    private static Sprite crystalSprite;
    private static Sprite shadowSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            }

            return whiteSprite;
        }
    }

    public static Sprite GetHeartSprite(PixelHeartState state)
    {
        if (heartFullSprite == null)
        {
            heartFullSprite = CreateHeartSprite(PixelHeartState.Full);
            heartHalfSprite = CreateHeartSprite(PixelHeartState.Half);
            heartEmptySprite = CreateHeartSprite(PixelHeartState.Empty);
        }

        if (state == PixelHeartState.Full)
        {
            return heartFullSprite;
        }

        if (state == PixelHeartState.Half)
        {
            return heartHalfSprite;
        }

        return heartEmptySprite;
    }

    public static Sprite GetCrystalSprite()
    {
        if (crystalSprite == null)
        {
            crystalSprite = CreateDiamondSprite();
        }

        return crystalSprite;
    }

    public static Sprite GetShadowSprite()
    {
        if (shadowSprite == null)
        {
            shadowSprite = CreateShadowSprite();
        }

        return shadowSprite;
    }

    private static Sprite CreateHeartSprite(PixelHeartState state)
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        string[] pattern =
        {
            "0000000000000000",
            "0000011001100000",
            "0001111111111000",
            "0011111111111100",
            "0111111111111110",
            "0111111111111110",
            "0011111111111100",
            "0001111111111000",
            "0000111111110000",
            "0000011111100000",
            "0000001111000000",
            "0000000110000000",
            "0000000010000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000"
        };

        Color32 outline = new Color32(30, 0, 0, 255);
        Color32 fill = new Color32(220, 45, 55, 255);
        Color32 dim = new Color32(75, 16, 20, 255);
        Color32 empty = new Color32(68, 68, 68, 255);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool active = pattern[15 - y][x] == '1';
                if (!active)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isEdge = false;
                for (int yy = -1; yy <= 1 && !isEdge; yy++)
                {
                    for (int xx = -1; xx <= 1; xx++)
                    {
                        int nx = x + xx;
                        int ny = (15 - y) + yy;
                        if (nx < 0 || nx >= 16 || ny < 0 || ny >= 16 || pattern[ny][nx] != '1')
                        {
                            isEdge = true;
                            break;
                        }
                    }
                }

                if (state == PixelHeartState.Empty)
                {
                    texture.SetPixel(x, y, isEdge ? empty : new Color32(0, 0, 0, 70));
                    continue;
                }

                if (isEdge)
                {
                    texture.SetPixel(x, y, outline);
                    continue;
                }

                if (state == PixelHeartState.Half && x > 7)
                {
                    texture.SetPixel(x, y, dim);
                }
                else
                {
                    texture.SetPixel(x, y, fill);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite CreateDiamondSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        string[] pattern =
        {
            "0000000000000000",
            "0000000010000000",
            "0000000111000000",
            "0000001111100000",
            "0000011111110000",
            "0000111111111000",
            "0001111111111100",
            "0011111111111110",
            "0001111111111100",
            "0000111111111000",
            "0000011111110000",
            "0000001111100000",
            "0000000111000000",
            "0000000010000000",
            "0000000000000000",
            "0000000000000000"
        };

        Color32 outline = new Color32(0, 70, 115, 255);
        Color32 fill = new Color32(0, 191, 255, 255);
        Color32 highlight = new Color32(168, 240, 255, 255);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool active = pattern[15 - y][x] == '1';
                if (!active)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isEdge = x == 0 || x == 15 || pattern[15 - y][Mathf.Max(0, x - 1)] == '0' || pattern[15 - y][Mathf.Min(15, x + 1)] == '0';
                texture.SetPixel(x, y, isEdge ? outline : (x < 8 ? highlight : fill));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite CreateShadowSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        string[] pattern =
        {
            "0000000000000000",
            "0000001111000000",
            "0000011111100000",
            "0000011111100000",
            "0000001111000000",
            "0000011111100000",
            "0000111111110000",
            "0001111111111000",
            "0001111111111000",
            "0000111111110000",
            "0000110110110000",
            "0000010110100000",
            "0000010100100000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000"
        };

        Color32 fill = new Color32(10, 10, 10, 255);
        Color32 eye = new Color32(0, 229, 255, 255);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool active = pattern[15 - y][x] == '1';
                if (!active)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                Color32 color = fill;
                if ((x == 6 || x == 9) && y == 10)
                {
                    color = eye;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }
}
