using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class EngineRightViewmodel: AclRightViewmodel
    {
        private bool _play;
        private bool _preview;
        private bool _rundown;
        private bool _mediaIngest;
        private bool _mediaEdit;
        private bool _mediaDelete;
        private bool _mediaArchive;
        private bool _mediaExport;
        private bool _rundownRightsAdmin;
        
        public EngineRightViewmodel(IAclRight right) : base(right)
        {
            Acl = right.Acl;
            Load();
        }

        public bool Play
        {
            get => _play;
            set
            {
                if (!SetField(ref _play, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.Play;
                else
                    Acl &= ~(ulong)EngineRight.Play;
            }
        }

        public bool Preview
        {
            get => _preview;
            set
            {
                if (!SetField(ref _preview, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.Preview;
                else
                    Acl &= ~(ulong)EngineRight.Preview;
            }
        }

        public bool Rundown
        {
            get => _rundown;
            set
            {
                if (!SetField(ref _rundown, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.Rundown;
                else
                    Acl &= ~(ulong)EngineRight.Rundown;
            }
        }
        public bool RundownRightsAdmin
        {
            get => _rundownRightsAdmin;
            set
            {
                if (!SetField(ref _rundownRightsAdmin, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.RundownRightsAdmin;
                else
                    Acl &= ~(ulong)EngineRight.RundownRightsAdmin;
            }
        }

        public bool MediaIngest
        {
            get => _mediaIngest;
            set
            {
                if (!SetField(ref _mediaIngest, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.MediaIngest;
                else
                    Acl &= ~(ulong)EngineRight.MediaIngest;
            }
        }

        public bool MediaEdit
        {
            get => _mediaEdit;
            set
            {
                if (!SetField(ref _mediaEdit, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.MediaEdit;
                else
                    Acl &= ~(ulong)EngineRight.MediaEdit;
            }
        }

        public bool MediaDelete
        {
            get => _mediaDelete;
            set
            {
                if (!SetField(ref _mediaDelete, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.MediaDelete;
                else
                    Acl &= ~(ulong)EngineRight.MediaDelete;
            }
        }

        public bool MediaArchive
        {
            get => _mediaArchive;
            set
            {
                if (!SetField(ref _mediaArchive, value))
                    return;
                if (value)
                    Acl |= (ulong)EngineRight.MediaArchive;
                else
                    Acl &= ~(ulong)EngineRight.MediaArchive;
            }
        }

        public bool MediaExport
        {
            get => _mediaExport;
            set
            {
                if (!SetField(ref _mediaExport, value))
                    return;
                if (value)
                    Acl |= (ulong) EngineRight.MediaExport;
                else
                    Acl &= ~(ulong) EngineRight.MediaExport;
            }
        }

        public void Load()
        {
            _play = (Acl & (ulong)EngineRight.Play) != 0;
            _preview = (Acl & (ulong)EngineRight.Preview) != 0;
            _rundownRightsAdmin = (Acl & (ulong)EngineRight.RundownRightsAdmin) != 0;
            _rundown = (Acl & (ulong)EngineRight.Rundown) != 0;
            _mediaIngest = (Acl & (ulong)EngineRight.MediaIngest) != 0;
            _mediaEdit = (Acl & (ulong)EngineRight.MediaEdit) != 0;
            _mediaDelete = (Acl & (ulong)EngineRight.MediaDelete) != 0;
            _mediaArchive = (Acl & (ulong)EngineRight.MediaArchive) != 0;
            _mediaExport = (Acl & (ulong)EngineRight.MediaExport) != 0;
        }

        public ISecurityObject SecurityObject => Right.SecurityObject;
    }
}
