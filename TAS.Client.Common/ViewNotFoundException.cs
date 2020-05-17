using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common
{
    [Serializable]
    public class ViewNotFoundException : Exception
    {
        private string _message = "Could not locate proper View for ViewModel";
        public ViewNotFoundException() { }        
        public ViewNotFoundException(ViewModelBase vm) 
        { 
            _message = $"Could not locate proper View for {vm.GetType().FullName}";
        }
        public ViewNotFoundException(string message) : base(message) 
        {
            _message = message;
        }                
        public ViewNotFoundException(string message, Exception innerException) : base(message, innerException) 
        {
            _message = message;
        }        
        public ViewNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public override string Message => _message;        
    }
}
