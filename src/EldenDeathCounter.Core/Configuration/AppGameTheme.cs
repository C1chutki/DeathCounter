namespace EldenDeathCounter.Core.Configuration;

public sealed record AppGameTheme(
    string Title,
    string Primary,
    string Secondary,
    string Tertiary,
    string Neutral,
    string Ink,
    string MutedInk,
    string Panel,
    string PanelAlt,
    string Border,
    string OverlayBackground,
    string OverlayBorder,
    string OverlayText)
{
    public static AppGameTheme EldenRing { get; } = new(
        "Elden Ring Death Counter",
        "#EAC36D",
        "#191919",
        "#F1EEE7",
        "#0B0B0B",
        "#F1EEE7",
        "#B8B1A6",
        "#141414",
        "#191815",
        "#4A3D27",
        "#7F000000",
        "#EAC36D",
        "#FFFFFF");

    public static AppGameTheme DarkSouls1 { get; } = new(
        "Dark Souls Death Counter",
        "#4A90E2",
        "#161616",
        "#1A1A1A",
        "#0A0A0A",
        "#F1F4F8",
        "#C2C2C2",
        "#1A1A1A",
        "#232323",
        "#2E4A66",
        "#CC101010",
        "#4A90E2",
        "#BBD4F2");

    public static AppGameTheme DarkSouls2 { get; } = new(
        "Dark Souls 2 Death Counter",
        "#9FA8DA",
        "#1A237E",
        "#B2DFDB",
        "#121212",
        "#ECEFF1",
        "#8C93B0",
        "#17182B",
        "#21243D",
        "#5C4F1E",
        "#CC12152E",
        "#9FA8DA",
        "#E8EAF6");

    public static AppGameTheme DarkSouls3 { get; } = new(
        "Dark Souls 3 Death Counter",
        "#E65100",
        "#424242",
        "#CFD8DC",
        "#0A0A0A",
        "#F2ECE8",
        "#998982",
        "#1E1E1E",
        "#242424",
        "#6F4536",
        "#CC1E1E1E",
        "#E65100",
        "#FFB49F");
}
