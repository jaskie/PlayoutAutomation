using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class AnimatedMedia : PersistentMedia, Common.Database.Interfaces.Media.IAnimatedMedia
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TemplateMethod _method;
        private int _templateLayer;
        private Dictionary<string, string> _fields;
        private TimeSpan _scheduledDelay;
        private TStartType _startType = TStartType.WithParent;

        [DtoMember]
        public Dictionary<string, string> Fields
        {
            get => _fields;
            set => SetField(ref _fields, value);
        }

        [DtoMember]
        public TemplateMethod Method { get => _method; set => SetField(ref _method, value); }

        [DtoMember]
        public int TemplateLayer { get => _templateLayer; set => SetField(ref _templateLayer, value); }

        [DtoMember]
        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => SetField(ref _scheduledDelay, value); }

        [DtoMember]
        public TStartType StartType { get => _startType; set => SetField(ref _startType, value); }

        [DtoMember]
        public override IDictionary<string, int> FieldLengths { get; } = DatabaseProvider.Database.ServerMediaFieldLengths;

        public override void Save()
        {
            try
            {
                if (!(MediaStatus != TMediaStatus.Unknown && MediaStatus != TMediaStatus.Deleted && Directory is AnimationDirectory directory))
                    return;
                if (IdPersistentMedia == 0)
                {
                    DatabaseProvider.Database.InsertMedia(this, directory.Server.Id);
                    directory.OnMediaSaved(this);
                }
                else
                {
                    DatabaseProvider.Database.UpdateMedia(this, directory.Server.Id);
                    directory.OnMediaSaved(this);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            IsModified = false;
        }

        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is AnimatedMedia a)
                Fields = a.Fields;
        }

        public override void Verify(bool updateFormatAndDurations)
        {
            if (!FileExists())
            {
                MediaStatus = TMediaStatus.Deleted;
                return;
            }
            IsVerified = true;
            MediaStatus = TMediaStatus.Available;
        }

    }
}
