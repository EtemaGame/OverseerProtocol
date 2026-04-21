using UnityEngine;

namespace OverseerProtocol.Features.HostFlow.OverseerHostScreen
{
    [CreateAssetMenu(fileName = "OverseerTheme", menuName = "OverseerProtocol/Theme")]
    public sealed class OverseerTheme : ScriptableObject
    {
        [Header("Backgrounds")]
        public Color overlayColor     = FromHex(0x060000, 0.94f);
        public Color frameColor       = FromHex(0x0B0101, 0.985f);
        public Color headerColor      = FromHex(0x0E0202, 1f);
        public Color sidebarColor     = FromHex(0x0A0101, 1f);
        public Color contentColor     = FromHex(0x090101, 1f);
        public Color panelColor       = FromHex(0x100202, 1f);
        public Color panelHeaderColor = FromHex(0x140303, 1f);
        public Color panelDark        = FromHex(0x070101, 1f);
        public Color scrollColor      = new Color(0f, 0f, 0f, 0.035f);
        public Color maskColor        = new Color(0f, 0f, 0f, 0.01f);

        [Header("Borders & Accents")]
        public Color borderColor      = FromHex(0x2A0707, 1f);
        public Color accent           = FromHex(0xD84A42, 1f);
        public Color accentSoft       = FromHex(0x8E3B35, 1f);
        public Color shadowColor      = new Color(0f, 0f, 0f, 0.20f);

        [Header("Selection")]
        public Color tabActive        = FromHex(0x120303, 1f);
        public Color presetSelected   = FromHex(0x221010, 1f);
        public Color presetIdle       = FromHex(0x110303, 1f);

        [Header("Text")]
        public Color text             = FromHex(0xF0E6D2, 1f);
        public Color textMuted        = FromHex(0xD0B38B, 1f);
        public Color textLabel        = FromHex(0xA98363, 1f);
        public Color textDim          = FromHex(0x8A654C, 1f);

        [Header("Status")]
        public Color statusGreen      = FromHex(0x78B96F, 1f);
        public Color statusAmber      = FromHex(0xD9B06D, 1f);
        public Color statusRed        = FromHex(0xD86464, 1f);

        [Header("Scanlines")]
        public Color scanlineColor    = FromHex(0x000000, 1f);

        [Range(0f, 0.15f)]
        public float scanlineAlpha    = 0.022f;

        [Header("Open Animation")]
        [Range(0f, 1f)]
        public float fadeInDuration   = 0.12f;

        private static Color FromHex(int rgb, float alpha)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, alpha);
        }
    }
}