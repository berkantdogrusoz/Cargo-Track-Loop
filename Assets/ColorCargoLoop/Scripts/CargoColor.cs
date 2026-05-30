using UnityEngine;

namespace ColorCargoLoop
{
    public enum CargoColor
    {
        Red,
        Blue,
        Yellow,
        Green,
        Purple,
        Orange
    }

    public static class CargoColorPalette
    {
        public static Color ToColor(CargoColor color)
        {
            switch (color)
            {
                // Canli/parlak hypercasual palet (referans: mor sort oyunu)
                case CargoColor.Red:
                    return new Color(1f, 0.30f, 0.33f);
                case CargoColor.Blue:
                    return new Color(0.26f, 0.64f, 1f);
                case CargoColor.Yellow:
                    return new Color(1f, 0.82f, 0.24f);
                case CargoColor.Green:
                    return new Color(0.40f, 0.86f, 0.42f);
                case CargoColor.Purple:
                    return new Color(0.72f, 0.45f, 1f);
                case CargoColor.Orange:
                    return new Color(1f, 0.60f, 0.22f);
                default:
                    return Color.white;
            }
        }
    }
}
