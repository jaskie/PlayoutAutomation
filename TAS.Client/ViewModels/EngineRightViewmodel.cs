using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;

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

        public EngineRightViewmodel(IAclRight right) : base(right)
        {
            Acl = right.Acl;
            _play = (right.Acl & (ulong)EngineRight.Play) != 0;
            _preview = (right.Acl & (ulong)EngineRight.Preview) != 0;
            _rundown = (right.Acl & (ulong)EngineRight.Rundown) != 0;
            _mediaIngest = (right.Acl & (ulong)EngineRight.MediaIngest) != 0;
            _mediaEdit = (right.Acl & (ulong)EngineRight.MediaEdit) != 0;
            _mediaDelete = (right.Acl & (ulong)EngineRight.MediaDelete) != 0;
        }

        public bool Play
        {
            get { return _play; }
            set
            {
                if (SetField(ref _play, value))
                    if (value)
                        Acl |= (ulong)EngineRight.Play;
                    else
                        Acl &= ~(ulong)EngineRight.Play;
            }
        }

        public bool Preview
        {
            get { return _preview; }
            set
            {
                if (SetField(ref _preview, value))
                    if (value)
                        Acl |= (ulong)EngineRight.Preview;
                    else
                        Acl &= ~(ulong)EngineRight.Preview;
            }
        }

        public bool Rundown
        {
            get { return _rundown; }
            set
            {
                if (SetField(ref _rundown, value))
                    if (value)
                        Acl |= (ulong)EngineRight.Rundown;
                    else
                        Acl &= ~(ulong)EngineRight.Rundown;
            }
        }

        public bool MediaIngest
        {
            get { return _mediaIngest; }
            set
            {
                if (SetField(ref _mediaIngest, value))
                    if (value)
                        Acl |= (ulong)EngineRight.MediaIngest;
                    else
                        Acl &= ~(ulong)EngineRight.MediaIngest;
            }
        }

        public bool MediaEdit
        {
            get { return _mediaEdit; }
            set
            {
                if (SetField(ref _mediaEdit, value))
                    if (value)
                        Acl |= (ulong)EngineRight.MediaEdit;
                    else
                        Acl &= ~(ulong)EngineRight.MediaEdit;
            }
        }

        public bool MediaDelete
        {
            get { return _mediaDelete; }
            set
            {
                if (SetField(ref _mediaDelete, value))
                    if (value)
                        Acl |= (ulong)EngineRight.MediaDelete;
                    else
                        Acl &= ~(ulong)EngineRight.MediaDelete;
            }
        }



        public ISecurityObject SecurityObject => Right.SecurityObject;
    }
}
