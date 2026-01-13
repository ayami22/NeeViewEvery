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
    /// EPUB Archive (VersOne + ZIP Fallback)
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
            catch (Exception ex)
            {
                Debug.WriteLine($"EpubArchive: VersOne failed → ZIP fallback\n{ex.Message}");
                _zipMode = true;
                return await GetEntriesByZipAsync(token);
            }
        }

        // ============================================================
        // VersOne path (strict EPUB)
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByVersOneAsync(
            CancellationToken token)
        {
            var list = new List<ArchiveEntry>();

            using var stream = new FileStream(
                Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var book = await EpubReader.ReadBookAsync(stream);

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (book.Content?.AllFiles?.Local != null)
            {
                foreach (var f in book.Content.AllFiles.Local)
                {
                    keys.Add(f.Key);
                }
            }

            int id = 0;
            foreach (var key in keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();

                var entry = new ArchiveEntry(this)
                {
                    IsValid = true,
                    Id = id,
                    RawEntryName = key,
                    CreationTime = CreationTime,
                    LastWriteTime = LastWriteTime,
                };

                if (entry.IsImage(true))
                {
                    list.Add(entry);
                    id++;
                }
            }

            Debug.WriteLine($"EpubArchive(VersOne): {list.Count} images loaded.");
            return list;
        }

        // ============================================================
        // ZIP fallback path (漫画 EPUB)
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByZipAsync(
            CancellationToken token)
        {
            var list = new List<ArchiveEntry>();

            using var zip = ZipFile.OpenRead(Path);

            var images = zip.Entries
                .Where(e =>
                    e.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

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
                var zipEntry = zip.GetEntry(entry.RawEntryName);
                if (zipEntry == null)
                    throw new FileNotFoundException(entry.RawEntryName);

                var ms = new MemoryStream();
                using var s = zipEntry.Open();
                await s.CopyToAsync(ms, token);
                ms.Position = 0;
                return ms;
            }

            using var stream = new FileStream(
                Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var book = await EpubReader.OpenBookAsync(stream);

            var file = book.Content?.AllFiles?.Local?
                .FirstOrDefault(f =>
                    string.Equals(f.Key, entry.RawEntryName,
                        StringComparison.OrdinalIgnoreCase));

            if (file == null)
                throw new FileNotFoundException(entry.RawEntryName);

            var bytes = await file.ReadContentAsBytesAsync();
            return new MemoryStream(bytes);
        }

        // ============================================================
        // Extract (not optimized)
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
