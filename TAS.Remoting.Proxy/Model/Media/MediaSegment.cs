using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model.Media
{
    public class MediaSegment : ProxyObjectBase, IMediaSegment
    {
#pragma warning disable CS0649

        [DtoMember(nameof(IMediaSegment.SegmentName))]
        private string _segmentName;

        [DtoMember(nameof(IMediaSegment.TcIn))]
        private TimeSpan _tcIn;

        [DtoMember(nameof(IMediaSegment.TcOut))]
        private TimeSpan _tcOut;

        [DtoMember(nameof(IMediaSegment.FieldLengths))]
        private IDictionary<string, int> _fieldLengths;

#pragma warning restore

        public string SegmentName { get => _segmentName; set => Set(value); }

        public TimeSpan TcIn { get => _tcIn; set => Set(value); }

        public TimeSpan TcOut { get => _tcOut; set => Set(value); }

        public void Delete()
        {
            Invoke();
        }
        public IDictionary<string, int> FieldLengths { get => _fieldLengths; set => Set(value); }

        //TODO: check if it can cause problems
        public IMediaSegments Owner { get; }

        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
