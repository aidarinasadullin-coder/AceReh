using System;
using System.IO;
using System.IO.Compression;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace SnowMeltingCalculator.Services
{
    /// <summary>
    /// Обход дефекта PDFsharp 6.x: <see cref="DeflateStream"/>-кодирование
    /// (<c>FlateDecode.Encode</c>) пишет заголовок zlib (78 DA) и raw-deflate,
    /// но не дописывает трейлер Adler-32 — каждый FlateDecode-поток
    /// оказывается невалидным zlib-потоком. Толерантные вьюеры (Chrome,
    /// Firefox, Sumatra) это прощают; Adobe Acrobat строго валидирует
    /// image-XObject и показывает «Недостаточно данных для изображения»
    /// (empira/PDFsharp#258; обход BestSpeed из #258 не лечит).
    /// Ремонт: дописать отсутствующий Adler-32 к данным каждого
    /// image-потока (сам raw-deflate полон — распаковка даёт ровно
    /// Width×Height×компоненты). Вызывается после RenderDocument, до Save.
    /// </summary>
    public static class PdfFlateStreamRepair
    {
        /// <summary>Починить Flate-потоки всех image-XObject документа
        /// (страницы + SMask-маски, на которые ссылаются картинки);
        /// поток-дефект распознаётся по отсутствию корректного Adler-32.
        /// Идемпотентно: повторный вызов ничего не меняет.</summary>
        public static void RepairImageStreams(PdfDocument document)
        {
            var repaired = new HashSet<PdfDictionary>();
            foreach (PdfPage page in document.Pages)
            {
                if (page.Elements.GetDictionary("/Resources") is not PdfDictionary resources)
                {
                    continue;
                }

                if (resources.Elements.GetDictionary("/XObject") is not PdfDictionary xobjects)
                {
                    continue;
                }

                foreach (var element in xobjects.Elements.Values.ToArray())
                {
                    if (element is PdfReference reference && reference.Value is PdfDictionary xobject)
                    {
                        RepairImageXObject(xobject, repaired);
                    }
                }
            }
        }

        private static void RepairImageXObject(PdfDictionary xobject, HashSet<PdfDictionary> repaired)
        {
            if (!repaired.Add(xobject))
            {
                return;
            }

            RepairFlateStream(xobject);

            // Маска прозрачности живёт вне /XObject страницы — чинится вместе с картинкой.
            if (xobject.Elements["/SMask"] is PdfReference smaskReference
                && smaskReference.Value is PdfDictionary smask)
            {
                RepairFlateStream(smask);
            }
        }

        /// <summary>Дописать Adler-32, если поток FlateDecode обрезан
        /// (zlib-заголовок есть, трейлера нет). Идемпотентно: поток с
        /// корректным трейлером не меняется.</summary>
        private static void RepairFlateStream(PdfDictionary streamDictionary)
        {
            if (streamDictionary.Stream is not PdfDictionary.PdfStream stream || stream.Value is not { Length: > 6 } data)
            {
                return;
            }

            byte[] raw;
            try
            {
                // Raw-deflate после 2-байтового zlib-заголовка декодируется
                // полностью (дефект — только в отсутствии трейлера).
                using var rawInput = new MemoryStream(data, 2, data.Length - 2);
                using var deflate = new DeflateStream(rawInput, CompressionMode.Decompress);
                using var output = new MemoryStream();
                deflate.CopyTo(output);
                raw = output.ToArray();
            }
            catch (InvalidDataException)
            {
                return; // не наш дефект — не трогаем
            }

            if (raw.Length == 0)
            {
                return;
            }

            var adler = ComputeAdler32(raw);
            var existing = (uint)((data[^4] << 24) | (data[^3] << 16) | (data[^2] << 8) | data[^1]);
            if (existing == adler)
            {
                return; // трейлер уже корректен
            }

            stream.Value = [.. data, (byte)(adler >> 24), (byte)(adler >> 16), (byte)(adler >> 8), (byte)adler];
        }

        /// <summary>Adler-32 (RFC 1950) — тот самый отсутствующий трейлер.</summary>
        private static uint ComputeAdler32(byte[] data)
        {
            const uint modAdler = 65521;
            uint a = 1, b = 0;
            foreach (var value in data)
            {
                a = (a + value) % modAdler;
                b = (b + a) % modAdler;
            }

            return (b << 16) | a;
        }
    }
}
