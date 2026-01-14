using NeeView.Drawing;
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
    /// Lazy-loading, memory-efficient EPUB archive.
    /// Handles comic EPUBs (image-based) and ZIP fallback.
    /// Streams individual images on demand to avoid memory spikes.
    /// </summary>
    public class EpubArchive : Archive, IDisposable
    {
        private EpubBookRef? _bookRef;
        private readonly SemaphoreSlim _bookLock = new(1, 1);
        private readonly SemaphoreSlim _zipLock = new(1, 1);
        private HashSet<string>? _zipEntries;

        public EpubArchive(string path, ArchiveEntry? source, ArchiveHint archiveHint)
            : base(path, source, archiveHint)
        {
        }

        public override string ToString() => "Epub";
        public override bool IsSupported() => true;

        // ============================================================
        // Entry enumeration
        // ============================================================
        protected override async ValueTask<List<ArchiveEntry>> GetEntriesInnerAsync(bool decrypt, CancellationToken token)
        {
            try
            {
                return await GetEntriesByEpubAsync(token);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpubArchive: VersOne lazy-load failed, fallback ZIP\n{ex}");
                return await GetEntriesByZipAsync(token);
            }
        }

        // ============================================================
        // EPUB (lazy-loaded)
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByEpubAsync(CancellationToken token)
        {
            var bookRef = await LoadBookRefAsync(token);

            // 取图片列表，不管是漫画型还是文本型
            var images = bookRef.Content?.Images?.Local;
            if (images == null || images.Count == 0)
                return new List<ArchiveEntry>(); // 没有图片，返回空列表

            var list = new List<ArchiveEntry>();
            int id = 0;

            foreach (var img in images.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();

                list.Add(new ArchiveEntry(this)
                {
                    IsValid = true,
                    Id = id++,
                    RawEntryName = img.Key,
                    Length = 0, // 流式加载
                    CreationTime = CreationTime,
                    LastWriteTime = LastWriteTime
                });
            }

            return list;
        }


        private async Task<EpubBookRef> LoadBookRefAsync(CancellationToken token)
        {
            if (_bookRef != null)
                return _bookRef;

            await _bookLock.WaitAsync(token);
            try
            {
                if (_bookRef == null)
                {
                    var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _bookRef = await EpubReader.OpenBookAsync(fs); // lazy load
                }
                return _bookRef;
            }
            finally
            {
                _bookLock.Release();
            }
        }

        private static bool IsComicEpub(EpubBookRef bookRef)
        {
            int imageCount = bookRef.Content?.Images?.Local?.Count ?? 0;
            int htmlCount = bookRef.Content?.Html?.Local?.Count ?? 0;
            return imageCount >= 5 && imageCount > htmlCount * 2;
        }

        // ============================================================
        // ZIP fallback
        // ============================================================
        private async ValueTask<List<ArchiveEntry>> GetEntriesByZipAsync(CancellationToken token)
        {
            await _zipLock.WaitAsync(token);
            try
            {
                if (_zipEntries != null && _zipEntries.Count > 0)
                    return _zipEntries.Select((name, index) => new ArchiveEntry(this)
                    {
                        Id = index,
                        RawEntryName = name,
                        IsValid = true,
                        Length = 0,
                        CreationTime = CreationTime,
                        LastWriteTime = LastWriteTime
                    }).ToList();

                using var zip = ZipFile.OpenRead(Path);
                _zipEntries = new HashSet<string>(zip.Entries
                    .Where(e => IsImageFile(e.FullName))
                    .Select(e => e.FullName));

                int id = 0;
                var list = new List<ArchiveEntry>();
                foreach (var name in _zipEntries.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    token.ThrowIfCancellationRequested();
                    list.Add(new ArchiveEntry(this)
                    {
                        Id = id++,
                        RawEntryName = name,
                        IsValid = true,
                        Length = 0,
                        CreationTime = CreationTime,
                        LastWriteTime = LastWriteTime
                    });
                }
                return list;
            }
            finally
            {
                _zipLock.Release();
            }
        }

        // ============================================================
        // Open image stream
        // ============================================================
        protected override async ValueTask<Stream> OpenStreamInnerAsync(ArchiveEntry entry, bool decrypt, CancellationToken token)
        {
            // ZIP entry
            if (_zipEntries != null && _zipEntries.Contains(entry.RawEntryName))
            {
                await _zipLock.WaitAsync(token);
                try
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
                finally
                {
                    _zipLock.Release();
                }
            }

            // EPUB lazy stream
            var bookRef = await LoadBookRefAsync(token);
            var fileRef = bookRef.Content?.Images?.Local?
                .FirstOrDefault(f => string.Equals(f.Key, entry.RawEntryName, StringComparison.OrdinalIgnoreCase));

            if (fileRef == null)
                throw new FileNotFoundException(entry.RawEntryName);

            // Stream content on demand without allocating full byte[]
            return await fileRef.GetContentStreamAsync();
        }

        private static bool IsImageFile(string name)
        {
            return name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
        }

        protected override ValueTask ExtractToFileInnerAsync(ArchiveEntry entry, string exportFileName, bool isOverwrite, CancellationToken token)
        {
            throw new NotSupportedException("Use stream extraction for EPUB images.");
        }

        // ============================================================
        // Proper disposal to close underlying file streams
        // ============================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bookRef?.Dispose();
                _bookRef = null;
                _bookLock.Dispose();
                _zipLock.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
