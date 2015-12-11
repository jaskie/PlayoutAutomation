using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace TAS.Client.ViewModels
{
    public class IngestTrimmerViewmodel:ViewmodelBase
    {
        public IngestTrimmerViewmodel()
        {
            BitmapSource = new System.Windows.Media.Imaging.BitmapImage();
        }

        public ImageSource BitmapSource { get; set; }

        protected override void OnDispose()
        {
            
        }
    }
}
