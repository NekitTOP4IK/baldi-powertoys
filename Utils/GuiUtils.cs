using UnityEngine;

namespace BaldiPowerToys.Utils {
    public static class GuiUtils {
        public static void DrawBoxWithBorder(Rect rect, Texture2D background, Texture2D border) {
            GUI.DrawTexture(rect, background, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), border, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), border, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), border, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), border, ScaleMode.StretchToFill);
        }

        public static void DrawTextWithShadow(Rect rect, string text, GUIStyle style) {
            // The shadow was causing a "double text" effect. Removing it for now.
            GUI.Label(rect, text, style);
        }

        public static Texture2D CreateGradientTexture(int height, Color startColor, Color endColor) {
            int width = 1;
            Texture2D texture = new Texture2D(width, height);
            for (int y = 0; y < height; y++) {
                float normalY = (float)y / (height - 1);
                texture.SetPixel(0, y, Color.Lerp(startColor, endColor, normalY));
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateSolidTexture(Color color) {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
