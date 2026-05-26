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
        "#99000000",
        "#00000000",
        "#FFFFFF");

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
