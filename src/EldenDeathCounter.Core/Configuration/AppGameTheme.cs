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
        "#D9B45A",
        "#0C0A06",
        "#ECE3CF",
        "#0B0907",
        "#ECE3CF",
        "#8A7C5E",
        "#13100B",
        "#1A150D",
        "#2E2719",
        "#7F000000",
        "#D9B45A",
        "#FFFFFF");

    public static AppGameTheme DarkSouls1 { get; } = EldenRing with
    {
        Title = "Dark Souls Death Counter",
        Primary = "#4A90E2",
        OverlayBorder = "#4A90E2",
    };

    public static AppGameTheme DarkSouls2 { get; } = EldenRing with
    {
        Title = "Dark Souls 2 Death Counter",
        Primary = "#5F7355",
        Secondary = "#2D3436",
        Tertiary = "#A68A4B",
        Neutral = "#10150F",
        OverlayBorder = "#5F7355",
    };

    public static AppGameTheme DarkSouls3 { get; } = EldenRing with
    {
        Title = "Dark Souls 3 Death Counter",
        Primary = "#E65100",
        OverlayBorder = "#E65100",
    };
}
