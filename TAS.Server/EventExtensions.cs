using System;
using TAS.Common;

namespace TAS.Server
{
    public static class EventExtensions
    {
        /// <summary>
        /// Finds Event or associated <see cref="Interfaces.Media.IMedia"> AudioVolume
        /// </summary>
        /// <param name="aEvent">Event to find the value for</param>
        /// <returns>audio volume as absolute value, or 1.0 if unknown</returns>
        public static double GetAudioVolumeLinearValue(this Event aEvent)
        {
            var volume = aEvent.GetAudioVolume();
            return Math.Pow(10, aEvent.GetAudioVolume() / 20);
        }

        public static bool OccupiesSameVideoLayerAs(this Event e, Event other)
        {
            return e != null
                && e.Layer == other?.Layer
                && (e.IsVisibleEvent())
                && (other.IsVisibleEvent());
        }
    }
}
