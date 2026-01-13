using NeeLaboratory.ComponentModel;
using System.ComponentModel;

namespace NeeView
{
    public class ArchiveConfig : BindableBase
    {
        public ZipArchiveConfig Zip { get; set; } = new ZipArchiveConfig();

        public SevenZipArchiveConfig SevenZip { get; set; } = new SevenZipArchiveConfig();

        public PdfArchiveConfig Pdf { get; set; } = new PdfArchiveConfig();

        public EpubArchiveConfig Epub { get; set; } = new EpubArchiveConfig();
      
        public MediaArchiveConfig Media { get; set; } = new MediaArchiveConfig();
    }
}
