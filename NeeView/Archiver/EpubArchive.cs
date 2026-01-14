using NeeView.Drawing;
using NeeView.Properties;
using VersOne.Epub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView
{
    /// <summary>
    /// EPUB Archive
    /// - Comic EPUB only (image-based)
    /// - Text EPUB is explicitly rejected
    /// - ZIP fallback is image-only
    /// </summary>
    public class EpubArchive : Archive
    {
        private bool _zipMode;

        public EpubArchive(string path, ArchiveEntry? source, ArchiveHint archiveHint)
            : base(path, source, archiveHint)
        {
        }

        public override string ToString() => "Epub";

        public override bool IsSupported() => true;

        // ============================================================
        // Entry enumeration
        // ============================================================
        protected override async ValueTask<List<ArchiveEntry>> GetEntriesInnerAsync(
            bool decrypt, CancellationToken token)
        {
            try
            {
                _zipMode = false;
                return await GetEntriesByVersOneAsync(token);
            }
            catch (NotSupportedException)
            {
                // 明确拒绝文字 EPUB（不 fallback）
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpubArchive: VersOne failed → ZIP fallback\n{ex.Message}");
                _zipMode = true;
                return await GetEntriesByZipAsync(token);
            }
        }

        // ============================================================
        // VersOne path (Comic EPUB only)
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByVersOneAsync(
            CancellationToken token)
        {
            using var stream = new FileStream(
                Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var book = await EpubReader.ReadBookAsync(stream);

            if (!IsComicEpub(book))
            {
                Debug.WriteLine("EpubArchive: Text-based EPUB detected. Reject.");
                throw new NotSupportedException("Text-based EPUB is not supported.");
            }

            var list = new List<ArchiveEntry>();
            var imageFiles = book.Content?.Images?.Local;

            if (imageFiles == null || imageFiles.Count == 0)
            {
                throw new NotSupportedException("No images found in EPUB.");
            }

            int id = 0;
            foreach (var img in imageFiles
                .OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();

                list.Add(new ArchiveEntry(this)
                {
                    IsValid = true,
                    Id = id++,
                    RawEntryName = img.Key,
                    Length = img.Content?.Length ?? 0,
                    CreationTime = CreationTime,
                    LastWriteTime = LastWriteTime,
                });
            }

            Debug.WriteLine($"EpubArchive(VersOne): {list.Count} images loaded.");
            return list;
        }

        // ============================================================
        // ZIP fallback (image-only)
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByZipAsync(
            CancellationToken token)
        {
            var list = new List<ArchiveEntry>();

            using var zip = ZipFile.OpenRead(Path);

            var images = zip.Entries
                .Where(e => IsImageFile(e.FullName))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (images.Count == 0)
            {
                throw new NotSupportedException("No images found in EPUB.");
            }

            int id = 0;
            foreach (var e in images)
            {
                token.ThrowIfCancellationRequested();

                list.Add(new ArchiveEntry(this)
                {
                    IsValid = true,
                    Id = id++,
                    RawEntryName = e.FullName,
                    Length = e.Length,
                    CreationTime = CreationTime,
                    LastWriteTime = LastWriteTime,
                });
            }

            Debug.WriteLine($"EpubArchive(ZIP): {list.Count} images loaded.");
            return list;
        }

        // ============================================================
        // Open image stream
        // ============================================================
        protected override async ValueTask<Stream> OpenStreamInnerAsync(
            ArchiveEntry entry, bool decrypt, CancellationToken token)
        {
            if (_zipMode)
            {
                using var zip = ZipFile.OpenRead(Path);
                var zipEntry = zip.GetEntry(entry.RawEntryName)
                    ?? throw new FileNotFoundException(entry.RawEntryName);

                var ms = new MemoryStream();
                using var s = zipEntry.Open();
                await s.CopyToAsync(ms, token);
                ms.Position = 0;
                return ms;
            }

            using var stream = new FileStream(
                Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var book = await EpubReader.OpenBookAsync(stream);

            var file = book.Content?.Images?.Local?
                .FirstOrDefault(f =>
                    string.Equals(f.Key, entry.RawEntryName,
                        StringComparison.OrdinalIgnoreCase));

            if (file == null)
                throw new FileNotFoundException(entry.RawEntryName);

            var bytes = await file.ReadContentAsBytesAsync();
            return new MemoryStream(bytes);
        }

        // ============================================================
        // Helpers
        // ============================================================
        private static bool IsComicEpub(EpubBook book)
        {
            int imageCount = book.Content?.Images?.Local?.Count ?? 0;
            int htmlCount = book.Content?.Html?.Local?.Count ?? 0;

            // 经验规则：
            // - 漫画 EPUB：image >> html
            // - 小说 EPUB：html >= image
            return imageCount >= 5 && imageCount > htmlCount * 2;
        }

        private static bool IsImageFile(string name)
        {
            return name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // Extract (not supported)
        // ============================================================
        protected override ValueTask ExtractToFileInnerAsync(
            ArchiveEntry entry, string exportFileName,
            bool isOverwrite, CancellationToken token)
        {
            throw new NotSupportedException(
                "Use stream extraction for EPUB images.");
        }
    }
}
