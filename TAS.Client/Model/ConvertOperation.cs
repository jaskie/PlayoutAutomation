using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    class ConvertOperation : IConvertOperation
    {
        public bool Aborted { get; set; }
        public TAspectConversion AspectConversion { get; set; }
        public TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        public decimal AudioVolume { get; set; }
        public IMedia DestMedia { get; set; }
        public Action FailureCallback { get; set; }
        public DateTime FinishedTime { get; set; }
        public bool IsIndeterminate { get; }
        public TFileOperationKind Kind { get; set; }
        public List<string> OperationOutput { get; set; }
        public FileOperationStatus OperationStatus { get; set; }
        public List<string> OperationWarning { get; set; }
        public TVideoFormat OutputFormat { get; set; }
        public int Progress { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
        public IMedia SourceMedia { get; set; }
        public DateTime StartTime { get; set; }
        public Action SuccessCallback { get; set; }
        public int TryCount { get; set; }        
        public event PropertyChangedEventHandler PropertyChanged;
        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
        public bool Do()
        {
            throw new NotImplementedException();
        }
        public void Fail()
        {
            throw new NotImplementedException();
        }
    }
}
