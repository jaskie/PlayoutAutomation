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

namespace TAS.Server
{
    public abstract class PersistentMedia: Media
    {

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
        public string idAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, "idAux"); }
        } // auxiliary Id from external system

        internal TMediaEmphasis _mediaEmphasis;
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, "MediaEmphasis"); }
        }

        
        public Media OriginalMedia { get; set; }

        private ObservableSynchronizedCollection<MediaSegment> _mediaSegments;

        public ObservableSynchronizedCollection<MediaSegment> MediaSegments
        {
            get
            {
                if (_mediaSegments == null)
                    _mediaSegments = this.DbMediaSegmentsRead();
                return _mediaSegments;
            }
        }

        public override void CloneMediaProperties(Media fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is PersistentMedia)
            {
                idAux = (fromMedia as PersistentMedia).idAux;
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

        internal override void Verify()
        {
            base.Verify();
            Save();
        }
    }
}
