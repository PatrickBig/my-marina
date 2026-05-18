using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Photos;

namespace MyMarina.UnitTests;

public class AspectRatioValidatorTests
{
    // ── Logo (1:1 ±10%) ──────────────────────────────────────────────────────

    [Fact]
    public void Logo_PerfectSquare_IsValid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Logo, 512, 512);
        Assert.True(valid);
    }

    [Fact]
    public void Logo_WithinTolerance_IsValid()
    {
        // 1.09 ratio — just inside ±10%
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Logo, 109, 100);
        Assert.True(valid);
    }

    [Fact]
    public void Logo_OutsideTolerance_IsInvalid()
    {
        // 1.20 ratio — outside ±10%
        var (valid, msg) = AspectRatioValidator.Validate(MarinaPhotoKind.Logo, 120, 100);
        Assert.False(valid);
        Assert.Contains("square", msg, StringComparison.OrdinalIgnoreCase);
    }

    // ── Banner (16:9 ±15%) ───────────────────────────────────────────────────

    [Fact]
    public void Banner_Perfect16x9_IsValid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Banner, 1600, 900);
        Assert.True(valid);
    }

    [Fact]
    public void Banner_WithinTolerance_IsValid()
    {
        // 2:1 ratio ≈ 2.0 → |2.0 - 1.7778| = 0.222 < 0.267
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Banner, 2000, 1000);
        Assert.True(valid);
    }

    [Fact]
    public void Banner_OutsideTolerance_IsInvalid()
    {
        // 1:1 → |1.0 - 1.7778| = 0.778 > 0.267
        var (valid, msg) = AspectRatioValidator.Validate(MarinaPhotoKind.Banner, 500, 500);
        Assert.False(valid);
        Assert.Contains("16:9", msg);
    }

    // ── Gallery (min 800px wide) ─────────────────────────────────────────────

    [Fact]
    public void Gallery_AtMinWidth_IsValid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Gallery, 800, 600);
        Assert.True(valid);
    }

    [Fact]
    public void Gallery_BelowMinWidth_IsInvalid()
    {
        var (valid, msg) = AspectRatioValidator.Validate(MarinaPhotoKind.Gallery, 799, 600);
        Assert.False(valid);
        Assert.Contains("800", msg);
    }

    // ── Aerial ───────────────────────────────────────────────────────────────

    [Fact]
    public void Aerial_WideEnough_IsValid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Aerial, 1200, 900);
        Assert.True(valid);
    }

    [Fact]
    public void Aerial_TooNarrow_IsInvalid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Aerial, 400, 300);
        Assert.False(valid);
    }

    // ── Approach ─────────────────────────────────────────────────────────────

    [Fact]
    public void Approach_WideEnough_IsValid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Approach, 1024, 768);
        Assert.True(valid);
    }

    [Fact]
    public void Approach_TooNarrow_IsInvalid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Approach, 640, 480);
        Assert.False(valid);
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public void AnyKind_ZeroDimensions_IsInvalid()
    {
        var (valid, msg) = AspectRatioValidator.Validate(MarinaPhotoKind.Logo, 0, 0);
        Assert.False(valid);
        Assert.Contains("positive", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnyKind_NegativeDimensions_IsInvalid()
    {
        var (valid, _) = AspectRatioValidator.Validate(MarinaPhotoKind.Gallery, -1, 600);
        Assert.False(valid);
    }
}
