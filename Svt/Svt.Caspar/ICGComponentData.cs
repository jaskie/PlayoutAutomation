using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
    public interface ICGComponentData
    {
        void ToAMCPEscapedXml(StringBuilder sb);
        void ToXml(StringBuilder sb);
    }
}
