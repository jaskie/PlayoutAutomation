using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    [JsonObject]
    public class MediaExportDescription
    {
        public MediaExportDescription(IMedia media, List<IMedia> logos, TimeSpan startTC, TimeSpan duration, double audioVolume)
        {
            Media = media;
            Logos = logos;
            StartTC = startTC;
            Duration = duration;
            AudioVolume = audioVolume;
        }
        [JsonProperty]
        public IMedia Media { get; private set; }
        [JsonProperty(ItemIsReference = true, TypeNameHandling = TypeNameHandling.All, ItemTypeNameHandling = TypeNameHandling.All)]
        public List<IMedia> Logos { get; }
        [JsonProperty]
        public TimeSpan Duration;
        [JsonProperty]
        public TimeSpan StartTC;
        [JsonProperty]
        public double AudioVolume;

        public void AddLogo(IMedia logo)
        {
            Logos.Add(logo);
        }
        public void RemoveLogo(IMedia logo)
        {
            Logos.Remove(logo);
        }
        public override string ToString()
        {
            return Media.MediaName;
        }
    }
}
