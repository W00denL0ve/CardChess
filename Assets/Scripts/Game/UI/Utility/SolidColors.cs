using UnityEngine;

public static class SolidColors
{
    public static string RED = "#FF4F4F";
    public static string GREEN = "#00FF1C";
    public static string MAGIC_PURPLE = "#845BFF";
    public static string MAGIC_PINK = "#F66CFF";
    public static string YELLOW = "#FFB800";
    public static string SKY_BLUE = "#8CFFF9";
    public static string Brown = "#9F7C4E";

    public static string TxtColor(string txt, string colorName)
    {
        return $"<color={colorName}>{txt}</color>";
    }

    public static Color GetColor(string colorName)
    {
        if (ColorUtility.TryParseHtmlString(colorName, out Color myColor))
        {
            return myColor;
        }

        return Color.white;
    }
}