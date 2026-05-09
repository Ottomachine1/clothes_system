namespace ClothesSystem.Infrastructure.Services.Workbook;

internal static class SpreadsheetImageMetadataReader
{
    public static SpreadsheetImageMetadata? TryRead(byte[] binaryContent, string? declaredContentType)
    {
        if (TryReadPng(binaryContent, out var pngWidth, out var pngHeight))
        {
            return new SpreadsheetImageMetadata(pngWidth, pngHeight, "image/png");
        }

        if (TryReadJpeg(binaryContent, out var jpegWidth, out var jpegHeight))
        {
            return new SpreadsheetImageMetadata(jpegWidth, jpegHeight, "image/jpeg");
        }

        if (TryReadWebP(binaryContent, out var webpWidth, out var webpHeight))
        {
            return new SpreadsheetImageMetadata(webpWidth, webpHeight, "image/webp");
        }

        return string.IsNullOrWhiteSpace(declaredContentType)
            ? null
            : new SpreadsheetImageMetadata(0, 0, declaredContentType.Trim());
    }

    private static bool TryReadPng(byte[] binaryContent, out int widthPixels, out int heightPixels)
    {
        widthPixels = 0;
        heightPixels = 0;

        if (binaryContent.Length < 24)
        {
            return false;
        }

        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (!binaryContent.AsSpan(0, signature.Length).SequenceEqual(signature))
        {
            return false;
        }

        widthPixels = ReadBigEndianInt32(binaryContent, 16);
        heightPixels = ReadBigEndianInt32(binaryContent, 20);
        return widthPixels > 0 && heightPixels > 0;
    }

    private static bool TryReadJpeg(byte[] binaryContent, out int widthPixels, out int heightPixels)
    {
        widthPixels = 0;
        heightPixels = 0;

        if (binaryContent.Length < 4 || binaryContent[0] != 0xFF || binaryContent[1] != 0xD8)
        {
            return false;
        }

        var offset = 2;
        while (offset + 3 < binaryContent.Length)
        {
            if (binaryContent[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            while (offset < binaryContent.Length && binaryContent[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= binaryContent.Length)
            {
                return false;
            }

            var marker = binaryContent[offset++];
            if (marker is 0xD8 or 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
            {
                continue;
            }

            if (offset + 1 >= binaryContent.Length)
            {
                return false;
            }

            var segmentLength = ReadBigEndianUInt16(binaryContent, offset);
            if (segmentLength < 2 || offset + segmentLength > binaryContent.Length)
            {
                return false;
            }

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                if (offset + 6 >= binaryContent.Length)
                {
                    return false;
                }

                heightPixels = ReadBigEndianUInt16(binaryContent, offset + 3);
                widthPixels = ReadBigEndianUInt16(binaryContent, offset + 5);
                return widthPixels > 0 && heightPixels > 0;
            }

            offset += segmentLength;
        }

        return false;
    }

    private static bool TryReadWebP(byte[] binaryContent, out int widthPixels, out int heightPixels)
    {
        widthPixels = 0;
        heightPixels = 0;

        if (binaryContent.Length < 16 ||
            !binaryContent.AsSpan(0, 4).SequenceEqual("RIFF"u8) ||
            !binaryContent.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return false;
        }

        var offset = 12;
        while (offset + 8 <= binaryContent.Length)
        {
            var chunkType = System.Text.Encoding.ASCII.GetString(binaryContent, offset, 4);
            var chunkLength = ReadLittleEndianInt32(binaryContent, offset + 4);
            var chunkDataOffset = offset + 8;
            if (chunkLength < 0 || chunkDataOffset + chunkLength > binaryContent.Length)
            {
                return false;
            }

            if (chunkType == "VP8X" && chunkLength >= 10)
            {
                widthPixels = 1 + ReadLittleEndianUInt24(binaryContent, chunkDataOffset + 4);
                heightPixels = 1 + ReadLittleEndianUInt24(binaryContent, chunkDataOffset + 7);
                return widthPixels > 0 && heightPixels > 0;
            }

            if (chunkType == "VP8 " && chunkLength >= 10)
            {
                if (binaryContent[chunkDataOffset + 3] == 0x9D &&
                    binaryContent[chunkDataOffset + 4] == 0x01 &&
                    binaryContent[chunkDataOffset + 5] == 0x2A)
                {
                    widthPixels = ReadLittleEndianUInt16(binaryContent, chunkDataOffset + 6) & 0x3FFF;
                    heightPixels = ReadLittleEndianUInt16(binaryContent, chunkDataOffset + 8) & 0x3FFF;
                    return widthPixels > 0 && heightPixels > 0;
                }
            }

            if (chunkType == "VP8L" && chunkLength >= 5)
            {
                var b0 = binaryContent[chunkDataOffset + 1];
                var b1 = binaryContent[chunkDataOffset + 2];
                var b2 = binaryContent[chunkDataOffset + 3];
                var b3 = binaryContent[chunkDataOffset + 4];
                widthPixels = 1 + (b0 | ((b1 & 0x3F) << 8));
                heightPixels = 1 + ((b1 >> 6) | (b2 << 2) | ((b3 & 0x0F) << 10));
                return widthPixels > 0 && heightPixels > 0;
            }

            offset = chunkDataOffset + chunkLength + (chunkLength % 2);
        }

        return false;
    }

    private static int ReadBigEndianInt32(byte[] binaryContent, int offset) =>
        (binaryContent[offset] << 24) |
        (binaryContent[offset + 1] << 16) |
        (binaryContent[offset + 2] << 8) |
        binaryContent[offset + 3];

    private static int ReadLittleEndianInt32(byte[] binaryContent, int offset) =>
        binaryContent[offset] |
        (binaryContent[offset + 1] << 8) |
        (binaryContent[offset + 2] << 16) |
        (binaryContent[offset + 3] << 24);

    private static int ReadBigEndianUInt16(byte[] binaryContent, int offset) =>
        (binaryContent[offset] << 8) | binaryContent[offset + 1];

    private static int ReadLittleEndianUInt16(byte[] binaryContent, int offset) =>
        binaryContent[offset] | (binaryContent[offset + 1] << 8);

    private static int ReadLittleEndianUInt24(byte[] binaryContent, int offset) =>
        binaryContent[offset] |
        (binaryContent[offset + 1] << 8) |
        (binaryContent[offset + 2] << 16);
}

internal readonly record struct SpreadsheetImageMetadata(int WidthPixels, int HeightPixels, string ContentType);
