using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(Common.Plugin.IVideoPreview))]
    public class VideoPreviewViewmodel : ViewModels.ViewmodelBase, Common.Plugin.IVideoPreview
    {
        private readonly IEnumerable<string> _videoSources;
        private string _selectedVideoSource;

        public VideoPreviewViewmodel()
        {
            View = new VideoPreviewView { DataContext = this };
            _videoSources = new ObservableCollection<string>();            
        }
        public UserControl View { get; private set; }

        public IEnumerable<string> VideoSources { get { return _videoSources; } }

        public string SelectedVideoSource
        {
            get { return _selectedVideoSource; }
            set
            {
                if (SetField(ref _selectedVideoSource, value))
                {

                }
            }
        }
        protected override void OnDispose()
        {
            
        }
    }
}
