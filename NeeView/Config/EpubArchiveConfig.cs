using NeeLaboratory.ComponentModel;
using System;
using System.Collections.Generic;

namespace NeeView
{
    public class EpubArchiveConfig : BindableBase
    {
        private bool _isEnabled = true;
        private FileTypeCollection _supportFileTypes = new FileTypeCollection(".epub");

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public FileTypeCollection SupportFileTypes
        {
            get => _supportFileTypes;
            set => SetProperty(ref _supportFileTypes, value);
        }
    }
}
