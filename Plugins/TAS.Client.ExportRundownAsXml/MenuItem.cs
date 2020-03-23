using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TAS.Client.Common.Plugin;
using TAS.Common;

namespace TAS.Client.UiPluginExample
{
    public class MenuItem: UiMenuItemBase
    {
        public MenuItem(IUiPlugin owner) : base(owner)
        {
            Items = new List<IUiMenuItem>();
        }

        public override bool CanExecute(object parameter)
        {
            if (!(Owner.Context is IUiEngine engine))
                return false;
            var e = engine.SelectedEvent;
            return e?.EventType == TEventType.Rundown;
        }

        public override void Execute(object parameter)
        {
            if (!(Owner.Context is IUiEngine engine) || engine.SelectedEvent == null)
                return;
            var proxy = EventProxy.FromEvent(engine.SelectedEvent);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = proxy.EventName,
                DefaultExt = FileUtils.RundownFileExtension,
                Filter = "XML files|*.xml|All files|*.*"
            };
            if (dlg.ShowDialog() != true)
                return;
            using (var stream = new FileStream(dlg.FileName, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(EventProxy), new XmlRootAttribute("Rundown"));
                serializer.Serialize(stream, proxy);
            }
        }
    }
}
