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
                // CANDY pastel palet - dekor objeleriyle (gumball/seker) ayni yumusak tonlar, birbirinden ayrik
                case CargoColor.Red:
                    return new Color(0.99f, 0.48f, 0.54f); // candy cilek/pembe-kirmizi
                case CargoColor.Blue:
                    return new Color(0.56f, 0.81f, 1.00f); // candy gokyuzu mavi
                case CargoColor.Yellow:
                    return new Color(1.00f, 0.83f, 0.43f); // candy limon sari
                case CargoColor.Green:
                    return new Color(0.57f, 0.90f, 0.68f); // candy mint yesil
                case CargoColor.Purple:
                    return new Color(0.77f, 0.63f, 1.00f); // candy uzum mor
                case CargoColor.Orange:
                    return new Color(1.00f, 0.69f, 0.43f); // candy seftali turuncu
                default:
                    return Color.white;
            }
        }
    }
}
