using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SnowMeltingCalculator.Tests.Fixtures
{
    /// <summary>
    /// Строгая валидация image-стримов PDF: каждый /Subtype/Image —
    /// 2-байтовый zlib-заголовок декодируется полностью, а последние
    /// 4 байта потока равны Adler-32 распакованных данных. Пин ремонта
    /// Acrobat-дефекта PDFsharp 6.x (zlib без трейлера). Общая для
    /// PdfExportServiceTests и CalculationReportPdfRendererTests
    /// (волна 3 дедупликации 2026-09-18).
    /// </summary>
    internal static class PdfImageStreamValidator
    {
        internal static (int Total, List<string> Failures) ValidateImageStreamsStrictly(byte[] pdf)
        {
            // Latin1: байт↔символ 1:1, индексы строк == индексы байтов.
            var text = System.Text.Encoding.Latin1.GetString(pdf);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                text,
                "/Subtype/Image.*?>>\\s*stream\\r?\\n",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            var total = 0;
            var failures = new List<string>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var lengthMatch = System.Text.RegularExpressions.Regex.Match(
                    match.Value, "/Length (\\d+)");
                if (!lengthMatch.Success)
                {
                    continue;
                }

                total++;
                var dataStart = match.Index + match.Length;
                var length = int.Parse(lengthMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                var data = pdf[dataStart..(dataStart + length)];

                byte[] raw;
                try
                {
                    using var input = new MemoryStream(data, 2, data.Length - 2);
                    using var deflate = new DeflateStream(
                        input, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    deflate.CopyTo(output);
                    raw = output.ToArray();
                }
                catch (InvalidDataException)
                {
                    failures.Add($"obj@{match.Index}: deflate не декодируется");
                    continue;
                }

                var adler = ComputeAdler32(raw);
                var trailerOk = data.Length >= 4
                    && data[^4] == (byte)(adler >> 24)
                    && data[^3] == (byte)(adler >> 16)
                    && data[^2] == (byte)(adler >> 8)
                    && data[^1] == (byte)adler;
                if (!trailerOk)
                {
                    failures.Add($"obj@{match.Index}: трейлер Adler-32 отсутствует или не совпадает");
                }
            }

            return (total, failures);
        }

        /// <summary>Adler-32 (RFC 1950) — эталон для проверки трейлера.</summary>
        private static uint ComputeAdler32(byte[] data)
        {
            const uint modulus = 65521;
            uint a = 1, b = 0;
            foreach (var value in data)
            {
                a = (a + value) % modulus;
                b = (b + a) % modulus;
            }

            return (b << 16) | a;
        }
    }
}
