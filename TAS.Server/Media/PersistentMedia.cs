using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;
using Newtonsoft.Json;

namespace TAS.Server
{
    public abstract class PersistentMedia: Media, IPersistentMedia
    {
        public PersistentMedia(IMediaDirectory directory) : base(directory) { }
        public PersistentMedia(IMediaDirectory directory, Guid guid) : base(directory, guid) { }
        public PersistentMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid) { IdPersistentMedia = idPersistentMedia; }
        public UInt64 IdPersistentMedia { get; set; }

        // media properties

        internal DateTime _killDate;
        [JsonProperty]
        public DateTime KillDate
        {
            get { return _killDate; }
            set { SetField(ref _killDate, value, "KillDate"); }
        }

        // content properties
        [JsonProperty]
        public UInt64 IdProgramme { get; set; }
        internal string _idAux;
        [JsonProperty]
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, "IdAux"); }
        } // auxiliary Id from external system

        internal TMediaEmphasis _mediaEmphasis;
        [JsonProperty]
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, "MediaEmphasis"); }
        }

        protected bool _protected;
        [JsonProperty]
        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value, "Protected"); }
        }

        public IMedia OriginalMedia { get; set; }

        private ObservableSynchronizedCollection<IMediaSegment> _mediaSegments;

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments
        {
            get
            {
                if (_mediaSegments == null)
                    _mediaSegments = this.DbMediaSegmentsRead<MediaSegment>();
                return _mediaSegments;
            }
        }

        public IMediaSegment CreateSegment()
        {
            return new MediaSegment(this.MediaGuid);
        }

        public override void CloneMediaProperties(IMedia fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is PersistentMedia)
            {
                IdAux = (fromMedia as PersistentMedia).IdAux;
                IdProgramme = (fromMedia as PersistentMedia).IdProgramme;
                OriginalMedia = (fromMedia as PersistentMedia).OriginalMedia;
                MediaEmphasis = (fromMedia as PersistentMedia).MediaEmphasis;
            }
        }

        public abstract bool Save();

        public bool Modified { get; set; }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            bool modified = base.SetField<T>(ref field, value, propertyName);
            if (modified && propertyName != "Verified") 
                Modified = true; 
            return modified;
        }

        internal override void Verify()
        {
            base.Verify();
            if (Modified)
                Save();
        }
    }
}
