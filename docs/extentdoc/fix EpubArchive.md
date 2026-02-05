Fix EpubArchive Freeze/Crash on Large Files
The current implementation of 
EpubArchive
 uses EpubReader.ReadBookAsync from VersOne.Epub, which eagerly loads all content (images) into memory as byte arrays. For large comic EPUBs (e.g., 500MB+), this causes massive memory spikes, GC pressure, and potential OutOfMemory crashes or UI usage freezes.

The fix involves switching to EpubReader.OpenBookAsync, which returns an 
EpubBookRef
 (lazy reference). We will then access the manifest/spine to list entries and use 
GetContentStreamAsync
 to stream individual images on demand without loading the entire book.

User Review Required
IMPORTANT

This change modifies how EPUBs are loaded. Instead of loading the entire book into memory, it keeps a reference to the open file/stream. This is far more efficient but relies on VersOne.Epub's lazy loading implementation (which we verified exists and is correct).

Proposed Changes
NeeView
[MODIFY] 
EpubArchive.cs
Change private field EpubBook? _book to EpubBookRef? _bookRef.
Update 
LoadBookAsync
 to call EpubReader.OpenBookAsync(fs) instead of 
ReadBookAsync
.
Update 
IsComicEpub
 logic to inspect 
EpubBookRef
 (checking Content.Images.Local.Count).
Update 
GetEntriesByEpubAsync
 to iterate over _bookRef.Content.Images.Local (which are now 
EpubLocalByteContentFileRef
).
Update 
OpenStreamInnerAsync
 to finding the matching fileRef and call fileRef.GetContentStreamAsync(). This returns a 
Stream
 directly, avoiding MemoryStream(byte[]) allocation.
Ensure _bookRef is disposed properly if needed (Archive disposal), though 
EpubArchive
 manages the FileStream lifecycle via _bookLock or standard archive disposal?
EpubBookRef
 implements IDisposable. 
EpubArchive
 inherits 
Archive
. 
Archive
 probably has 
Dispose
.
EpubArchive
 currently keeps FileStream open inside _book?
EpubReader.ReadBookAsync(fs) consumes the stream.
EpubReader.OpenBookAsync(fs) keeps the stream open in 
EpubBookRef
.
We need to ensure 
EpubArchive
 disposes _bookRef.
Verification Plan
Manual Verification
The user should open the problematic EPUB file.
Observe memory usage (should be low, <100MB instead of >500MB).
Verify navigation between pages is smooth.
Verify that standard (small) EPUBs still work correctly.