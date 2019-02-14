using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using NLog;
using Svt.Caspar;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class NowPlayingNotifier: IEnginePlugin
    {
        private bool _disposed;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [XmlIgnore]
        public IEngine Engine { get; private set; }

        public void Initialize(IEngine engine)
        {
            Engine = engine;
            Engine.EngineOperation += _engine_EngineOperation;
        }

        public string CommandOnPlay { get; set; }

        [XmlAttribute]
        public string EngineName { get; set; }

        public DataItem[] Data { get; set; }

        public TMediaCategory? MediaCategory { get; set; }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Engine.EngineOperation -= _engine_EngineOperation;
        }

        private void _engine_EngineOperation(object sender, EngineOperationEventArgs e)
        {
            Debug.WriteLine($"Plugin notification received for {e.Event}");
            if (e.Event == null)
                return;
            if (MediaCategory != null && e.Event.Media?.MediaCategory != MediaCategory)
                return;
            try
            {
                if (e.Operation == TEngineOperation.Play)
                {
                    var data = new CasparCGDataCollection();
                    foreach (var dataItem in Data)
                    {
                        switch (dataItem.Value)
                        {
                            case DataValueKind.CurrentItemName:
                                data.SetData(dataItem.Name, e.Event.EventName);
                                break;
                            case DataValueKind.CurrentItemDurationInSeconds:
                                data.SetData(dataItem.Name, e.Event.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
                                break;
                            case DataValueKind.NextItemName:
                                data.SetData(dataItem.Name, e.Event.GetSuccessor()?.EventName ?? string.Empty);
                                break;
                            case DataValueKind.NextShowName:
                                data.SetData(dataItem.Name, FindNextShowName(e.Event)?.EventName ?? string.Empty);
                                break;
                            case DataValueKind.NextNextItemName:
                                data.SetData(dataItem.Name, e.Event.GetSuccessor()?.GetSuccessor()?.EventName ?? string.Empty);
                                break;
                            case DataValueKind.NextNextShowName:
                                data.SetData(dataItem.Name, FindNextNextShowName(e.Event)?.EventName ?? string.Empty);
                                break;
                        }
                    }
                    Engine.Execute($"{CommandOnPlay} {data.ToAMCPEscapedXml()}");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

        private IEvent FindNextNextShowName(IEvent aEvent)
        {
            var e = FindNextShowName(aEvent);
            return e == null ? null : FindNextShowName(e);
        }

        private IEvent FindNextShowName(IEvent aEvent)
        {
            var successor = aEvent.GetSuccessor();
            while (successor != null)
            {
                if (successor.Media?.MediaCategory == TMediaCategory.Show)
                    return successor;
                successor = successor.GetSuccessor();
            }
            return null;
        }

        public enum DataValueKind
        {
            CurrentItemName,
            CurrentItemDurationInSeconds,
            NextItemName,
            NextShowName,
            NextNextItemName,
            NextNextShowName
        }

        public class DataItem
        {
            [XmlAttribute]
            public string Name { get; set; }

            [XmlAttribute]
            public DataValueKind Value { get; set; }
        }
    }
}
