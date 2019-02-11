using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TAS.Client.NDIVideoPreview
{
    public class AudioLevelBarViewmodel : INotifyPropertyChanged
    {
        private const long SilenceDurationToHideTicks = 3000 * TimeSpan.TicksPerMillisecond;
        private const double SilenceLevel = -60;

        private double _audioLevel;
        private bool _isVisible;
        private DateTime _lastNoSilenceTime;
        
        public double AudioLevel
        {
            get => _audioLevel;
            set
            {
                if (Math.Abs(_audioLevel - value) > double.Epsilon)
                {
                    _audioLevel = value;
                    OnPropertyChanged();
                }
                if (value > SilenceLevel)
                    SetNoSilence();
                else
                {
                    if ((DateTime.Now - _lastNoSilenceTime).Ticks > SilenceDurationToHideTicks)
                        IsVisible = false;
                }
            }
        }

        private void SetNoSilence()
        {
            IsVisible = true;
            _lastNoSilenceTime = DateTime.Now;
        }

        public bool IsVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
