namespace ImageConversion.Core;

public sealed record ConversionResult(
    string Text,
    int Width,
    int Height,
    int EstimatedCharacterCount,
    byte[] PreviewPng);
