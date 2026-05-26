using EldenDeathCounter.Core.Hotkeys;

namespace EldenDeathCounter.Tests.Core;

public sealed class HotkeyDefinitionTests
{
    [Fact]
    public void ParsesSingleFunctionKey()
    {
        var parsed = HotkeyDefinition.Parse("F8");

        Assert.True(parsed.IsValid);
        Assert.Equal("F8", parsed.Key);
        Assert.False(parsed.Control);
        Assert.False(parsed.Alt);
        Assert.False(parsed.Shift);
        Assert.False(parsed.Windows);
    }

    [Fact]
    public void ParsesModifierCombination()
    {
        var parsed = HotkeyDefinition.Parse("Ctrl+Alt+F9");

        Assert.True(parsed.IsValid);
        Assert.Equal("F9", parsed.Key);
        Assert.True(parsed.Control);
        Assert.True(parsed.Alt);
        Assert.False(parsed.Shift);
        Assert.False(parsed.Windows);
    }

    [Fact]
    public void RejectsMissingKey()
    {
        var parsed = HotkeyDefinition.Parse("Ctrl+Alt");

        Assert.False(parsed.IsValid);
        Assert.NotEmpty(parsed.Error);
    }
}
