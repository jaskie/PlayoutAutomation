using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public abstract class PersistentMedia: MediaBase, Database.Common.Interfaces.Media.IPersistentMedia
    {

        private DateTime? _killDate;
        private string _idAux;
        private TMediaEmphasis _mediaEmphasis;
        private bool _protected;
        private bool _isModifiedDisabled;
        private readonly Lazy<MediaSegments> _mediaSegments;

        protected PersistentMedia() 
        {
            _mediaSegments = new Lazy<MediaSegments>(() => DatabaseProvider.Database.MediaSegmentsRead<MediaSegments>(this));
        }
        public ulong IdPersistentMedia { get; set; }

        [DtoMember]
        public DateTime? KillDate
        {
            get => _killDate;
            set => SetField(ref _killDate, value);
        }

        [DtoMember]
        public ulong IdProgramme { get; set; }

        [DtoMember]
        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
        } 

        [DtoMember]
        public TMediaEmphasis MediaEmphasis
        {
            get => _mediaEmphasis;
            set => SetField(ref _mediaEmphasis, value);
        }

        [DtoMember]
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
            IsProtected = properties.IsProtected;
        }

        public void DisableIsModified()
        {
            _isModifiedDisabled = true;
        }

        public void EnableIsModified()
        {
            _isModifiedDisabled = false;
        }

        public abstract void Save();

        public bool IsModified { get; protected set; }

        public void SetHaveAlphaChannel(bool haveAlphaChannel)
        {
            HaveAlphaChannel = haveAlphaChannel;
        }

        public override void Verify(bool updateFormatAndDurations)
        {
            base.Verify(updateFormatAndDurations);
            if (IsModified)
                Save();
        }

        protected override bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (_isModifiedDisabled)
                return base.SetField(ref field, value, propertyName);
            var modified = base.SetField(ref field, value, propertyName);
            if (modified && propertyName != nameof(IsVerified))
                IsModified = true;
            return modified;
        }

    }
}
