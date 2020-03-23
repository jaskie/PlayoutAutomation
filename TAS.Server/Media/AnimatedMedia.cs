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

        [DtoField]
        public Dictionary<string, string> Fields
        {
            get => _fields;
            set => SetField(ref _fields, value);
        }

        [DtoField]
        public TemplateMethod Method { get => _method; set => SetField(ref _method, value); }

        [DtoField]
        public int TemplateLayer { get => _templateLayer; set => SetField(ref _templateLayer, value); }

        [DtoField]
        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => SetField(ref _scheduledDelay, value); }

        [DtoField]
        public TStartType StartType { get => _startType; set => SetField(ref _startType, value); }

        [DtoField]
        public override IDictionary<string, int> FieldLengths { get; } = EngineController.Current.Database.ServerMediaFieldLengths;

        public override void Save()
        {
            var saved = false;
            try
            {
                if (Directory is AnimationDirectory directory)
                {
                    if (MediaStatus == TMediaStatus.Deleted)
                    {
                        if (IdPersistentMedia != 0)
                            saved = EngineController.Current.Database.DeleteMedia(this);
                    }
                    else if (IdPersistentMedia == 0)
                        saved = EngineController.Current.Database.InsertMedia(this, directory.Server.Id);
                    else if (IsModified)
                    {
                        EngineController.Current.Database.UpdateMedia(this, directory.Server.Id);
                        saved = true;
                    }
                    if (saved)
                        directory.OnMediaSaved(this);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving {0}", MediaName);
            }
            base.Save();
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
