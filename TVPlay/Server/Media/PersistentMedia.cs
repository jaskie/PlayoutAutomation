using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using TAS.Common;
using TAS.Data;
using TAS.Server.Interfaces;
using TAS.Server.Common;

namespace TAS.Server
{
    public abstract class PersistentMedia: Media, IPersistentMedia
    {
        public PersistentMedia(MediaDirectory directory) : base(directory) { }
        public PersistentMedia(MediaDirectory directory, Guid guid) : base(directory, guid) { }
        public UInt64 idPersistentMedia;

        // media properties

        internal DateTime _killDate;
        public DateTime KillDate
        {
            get { return _killDate; }
            set { SetField(ref _killDate, value, "KillDate"); }
        }

        // content properties
        public UInt64 idProgramme { get; set; }
        internal string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, "IdAux"); }
        } // auxiliary Id from external system

        internal TMediaEmphasis _mediaEmphasis;
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, "MediaEmphasis"); }
        }

        
        public IMedia OriginalMedia { get; set; }

        private ObservableSynchronizedCollection<IMediaSegment> _mediaSegments;

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments
        {
            get
            {
                if (_mediaSegments == null)
                    _mediaSegments = this.DbMediaSegmentsRead();
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
                idProgramme = (fromMedia as PersistentMedia).idProgramme;
                OriginalMedia = (fromMedia as PersistentMedia).OriginalMedia;
                MediaEmphasis = (fromMedia as PersistentMedia).MediaEmphasis;
            }
        }

        public abstract bool Save();

        internal bool Modified = false;


        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            bool modified = base.SetField<T>(ref field, value, propertyName);
            if (modified) 
                Modified = true; 
            return modified;
        }

        public override void Verify()
        {
            base.Verify();
            Save();
        }
    }
}
