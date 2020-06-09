using System.Windows.Input;
using TAS.Common.Interfaces;

namespace TAS.Client.Common.Plugin
{
    public interface IUiEngine : IUiPluginContext
    {
        IEvent SelectedEvent { get; }

        ICommand CommandClearAll { get; }
        ICommand CommandClearMixer { get; }
        ICommand CommandRestart { get; }
        ICommand CommandStartSelected { get; }
        ICommand CommandForceNextSelected { get; }
        ICommand CommandStartLoaded { get; }
        ICommand CommandLoadSelected { get; }
        ICommand CommandScheduleSelected { get; }
        ICommand CommandRescheduleSelected { get; }
        ICommand CommandTrackingToggle { get; }
        ICommand CommandRestartRundown { get; }
        ICommand CommandNewRootRundown { get; }
        ICommand CommandNewContainer { get; }
        ICommand CommandDeleteSelected { get; }
        ICommand CommandCopySelected { get; }
        ICommand CommandPasteSelected { get; }
        ICommand CommandCutSelected { get; }
        ICommand CommandExportMedia { get; }
        ICommand CommandUndelete { get; }
        ICommand CommandSaveRundown { get; }
        ICommand CommandLoadRundown { get; }
        ICommand CommandRestartLayer { get; }

        ICommand CommandEventHide { get; }
        ICommand CommandAddNextMovie { get; }
        ICommand CommandAddNextEmptyMovie { get; }
        ICommand CommandAddNextRundown { get; }
        ICommand CommandAddNextLive { get; }
        ICommand CommandAddSubMovie { get; }
        ICommand CommandAddSubRundown { get; }
        ICommand CommandAddSubLive { get; }
        ICommand CommandAddAnimation { get; }
        ICommand CommandToggleEnabled { get; }
        ICommand CommandToggleHold { get; }
        ICommand CommandToggleLayer { get; }
        ICommand CommandMoveUp { get; }
        ICommand CommandMoveDown { get; }
        ICommand CommandToggleCg { get; }
    }
}
