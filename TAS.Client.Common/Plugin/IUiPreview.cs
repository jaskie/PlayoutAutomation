using System;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPreview: INotifyPropertyChanged
    {
        ICommand CommandBackward { get; }
        ICommand CommandBackwardOneFrame { get; }
        ICommand CommandCopyToTcIn { get; }
        ICommand CommandCopyToTcOut { get; }
        ICommand CommandCue { get; }
        ICommand CommandDeleteSegment { get; }
        ICommand CommandFastForward { get; }
        ICommand CommandFastForwardOneFrame { get; }
        ICommand CommandLoadLiveDevice { get; }
        ICommand CommandNewSegment { get; }
        ICommand CommandPlayTheEnd { get; }
        ICommand CommandSaveSegment { get; }
        ICommand CommandSeek { get; }
        ICommand CommandSetSegmentNameFocus { get; }
        ICommand CommandToggleLayer { get; }
        ICommand CommandTogglePlay { get; }
        ICommand CommandTrimSource { get; }
        ICommand CommandUnload { get; }
        TimeSpan Duration { get; set; }
        VideoFormatDescription FormatDescription { get; }
        long FramesPerSecond { get; }
        bool HaveLiveDevice { get; }
        bool IsEnabled { get; }
        bool IsLoaded { get; }
        bool IsSegmentNameFocused { get; set; }
        bool IsSegmentsEnabled { get; }
        bool IsSegmentsVisible { get; set; }
        bool IsStill1Loaded { get; }
        bool IsStill2Loaded { get; }
        bool IsStill3Loaded { get; }
        bool IsStillButton1Visible { get; }
        bool IsStillButton2Visible { get; }
        bool IsStillButton3Visible { get; }
        long LoadedDuration { get; }
        IMedia LoadedMedia { get; }
        bool PlayWholeClip { get; set; }
        TimeSpan Position { get; set; }
        string SegmentName { get; set; }
        IEvent SelectedEvent { get; set; }
        IIngestOperation SelectedIngestOperation { get; set; }
        IMedia SelectedMedia { get; set; }
        long SliderPosition { get; set; }
        long SliderTickFrequency { get; }
        TimeSpan StartTc { get; set; }
        TimeSpan TcIn { get; set; }
        TimeSpan TcOut { get; set; }
        TVideoFormat VideoFormat { get; }
        void OnUiThread(Action action);

    }
}
