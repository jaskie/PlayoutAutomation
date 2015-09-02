using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;

namespace TAS.Client.ViewModels
{
    public class MediaExportViewmodel: ViewmodelBase
    {
        public readonly MediaExport MediaExport;
        public MediaExportViewmodel(MediaExport mediaExport)
        {
            this.MediaExport = mediaExport;
        }
        public Media Media { get { return this.MediaExport.Media; } }
        public TimeSpan StartTC { get { return this.MediaExport.StartTC; } set { SetField(ref this.MediaExport.StartTC, value, "StartTC"); } }
        public TimeSpan Duration { get { return this.MediaExport.Duration; } set { SetField(ref this.MediaExport.Duration, value, "Duration"); } }
        protected override void OnDispose()
        {
            
        }
    }
}
