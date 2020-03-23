using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public abstract class PersistentMedia: MediaBase, Common.Database.Interfaces.Media.IPersistentMedia
    {

        private DateTime? _killDate;
        private string _idAux;
        private TMediaEmphasis _mediaEmphasis;
        private bool _protected;
        private bool _isDbReading;
        private readonly Lazy<MediaSegments> _mediaSegments;

        protected PersistentMedia() 
        {
            _mediaSegments = new Lazy<MediaSegments>(() => EngineController.Current.Database.MediaSegmentsRead<MediaSegments>(this));
        }
        public ulong IdPersistentMedia { get; set; }

        [DtoField]
        public DateTime? KillDate
        {
            get => _killDate;
            set => SetField(ref _killDate, value);
        }

        [DtoField]
        public ulong IdProgramme { get; set; }

        [DtoField]
        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
        } 

        [DtoField]
        public TMediaEmphasis MediaEmphasis
        {
            get => _mediaEmphasis;
            set => SetField(ref _mediaEmphasis, value);
        }

        [DtoField]
        public bool IsProtected
        {
            get => _protected;
            set => SetField(ref _protected, value);
        }

        public IMediaSegments GetMediaSegments() => _mediaSegments.Value;

        public abstract IDictionary<string, int> FieldLengths { get; } 


        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (!(fromMedia is IPersistentMediaProperties properties))
                return;
            IdAux = properties.IdAux;
            IdProgramme = properties.IdProgramme;
            MediaEmphasis = properties.MediaEmphasis;
            KillDate = properties.KillDate;
        }

        public void BeginDbRead()
        {
            _isDbReading = true;
        }

        public void EndDbRead()
        {
            _isDbReading = false;
        }

        public virtual void Save()
        {
            IsModified = false;
        }

        public bool IsModified { get; protected set; }

        public override void Verify(bool updateFormatAndDurations)
        {
            base.Verify(updateFormatAndDurations);
            Save();
        }

        protected override bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (_isDbReading)
            {
                field = value;
                return false;
            }
            var modified = base.SetField(ref field, value, propertyName);
            if (modified && propertyName != nameof(IsVerified))
                IsModified = true;
            return modified;
        }

    }
}
