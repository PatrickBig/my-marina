using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.Photos;

public static class AspectRatioValidator
{
    public static (bool Valid, string? ErrorMessage) Validate(MarinaPhotoKind kind, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return (false, "Image dimensions must be positive.");

        return kind switch
        {
            MarinaPhotoKind.Logo =>
                Math.Abs((double)width / height - 1.0) < 0.10
                    ? (true, null)
                    : (false, "Logo must be approximately square (1:1 aspect ratio ±10%)."),

            MarinaPhotoKind.Banner =>
                Math.Abs((double)width / height - 1.7778) < 0.267
                    ? (true, null)
                    : (false, "Banner must be approximately 16:9 aspect ratio (±15%)."),

            MarinaPhotoKind.Gallery or MarinaPhotoKind.Aerial or MarinaPhotoKind.Approach =>
                width >= 800
                    ? (true, null)
                    : (false, "Photo must be at least 800 pixels wide."),

            _ => (false, $"Unknown photo kind: {kind}")
        };
    }
}
