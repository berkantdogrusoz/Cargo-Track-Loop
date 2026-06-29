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
        Orange,
        Pink,
        Cyan,
        Teal,
        Lime,
        Brown,
        Indigo,
        Obstacle   // ENGEL: siyah, portrede ASLA yok; sadece blocker sepetler kullanir
    }

    public static class CargoColorPalette
    {
        // ADAPTIVE PALET: doluysa slot 0..11 renkleri BUNDAN gelir (gorselin KENDI renkleri). Obstacle haric.
        public static Color[] Override;

        public static Color ToColor(CargoColor color)
        {
            if (color == CargoColor.Obstacle) return new Color(0.14f, 0.14f, 0.16f); // ENGEL her zaman siyah
            if (Override != null) { int oi = (int)color; if (oi >= 0 && oi < Override.Length) return Override[oi]; }
            switch (color)
            {
                // CANDY pastel palet - birbirinden ayrik, dekor objeleriyle uyumlu
                case CargoColor.Red:    return new Color(0.99f, 0.48f, 0.54f); // candy cilek/pembe-kirmizi
                case CargoColor.Blue:   return new Color(0.56f, 0.81f, 1.00f); // candy gokyuzu mavi
                case CargoColor.Yellow: return new Color(1.00f, 0.83f, 0.43f); // candy limon sari
                case CargoColor.Green:  return new Color(0.57f, 0.90f, 0.68f); // candy mint yesil
                case CargoColor.Purple: return new Color(0.77f, 0.63f, 1.00f); // candy uzum mor
                case CargoColor.Orange: return new Color(1.00f, 0.69f, 0.43f); // candy seftali turuncu
                case CargoColor.Pink:   return new Color(0.97f, 0.55f, 0.80f); // candy magenta-pembe
                case CargoColor.Cyan:   return new Color(0.45f, 0.87f, 0.92f); // candy turkuaz
                case CargoColor.Teal:   return new Color(0.22f, 0.68f, 0.62f); // koyu deniz yesili
                case CargoColor.Lime:   return new Color(0.74f, 0.92f, 0.38f); // fistik yesili
                case CargoColor.Brown:  return new Color(0.68f, 0.50f, 0.37f); // cikolata kahve
                case CargoColor.Indigo: return new Color(0.46f, 0.45f, 0.85f); // koyu mor-mavi
                case CargoColor.Obstacle: return new Color(0.14f, 0.14f, 0.16f); // SIYAH engel (renk degil)
                default:                return Color.white;
            }
        }
    }
}
