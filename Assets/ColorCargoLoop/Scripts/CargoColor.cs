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
                case CargoColor.Red:
                    return new Color(0.96f, 0.08f, 0.08f);
                case CargoColor.Blue:
                    return new Color(0.12f, 0.53f, 1f);
                case CargoColor.Yellow:
                    return new Color(1f, 0.79f, 0.12f);
                case CargoColor.Green:
                    return new Color(0.14f, 0.82f, 0.36f);
                case CargoColor.Purple:
                    return new Color(0.58f, 0.28f, 1f);
                case CargoColor.Orange:
                    return new Color(1f, 0.46f, 0.08f);
                default:
                    return Color.white;
            }
        }
    }
}
