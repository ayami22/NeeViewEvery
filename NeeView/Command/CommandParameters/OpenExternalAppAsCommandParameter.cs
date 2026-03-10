using NeeView.Windows.Property;
using System;

namespace NeeView
{
    public class OpenExternalAppAsCommandParameter : CommandParameter
    {
        private MultiPagePolicy _multiPagePolicy = MultiPagePolicy.Once;
        private int _index;


        /// <summary>
        /// 複数ページのときの動作
        /// </summary>
        [PropertyMember]
        public MultiPagePolicy MultiPagePolicy
        {
            get { return _multiPagePolicy; }
            set { _multiPagePolicy = value; }
        }

        /// <summary>
        /// 選択された外部アプリの番号。0 は未選択
        /// </summary>
        [PropertyMember(NoteConverter = typeof(IntToExternalAppString))]
        public int Index
        {
            get { return _index; }
            set { SetProperty(ref _index, Math.Max(0, value)); }
        }
    }
}
