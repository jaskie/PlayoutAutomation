using System.Collections.Generic;

namespace TAS.Client.Config.Model
{
    public class CgElementsController
    {               
        public string EngineName { get; set; }        
        public List<string> Startup { get; set; }               
        public List<CgElement> Crawls { get; set; }        
        public List<CgElement> Logos { get; set; }        
        public List<CgElement> Parentals { get; set; }        
        public List<CgElement> Auxes { get; set; }
        public bool IsEnabled { get; set; }
        public CgElementsController()
        {
            Crawls = new List<CgElement>();
            Logos = new List<CgElement>();
            Parentals = new List<CgElement>();
            Auxes = new List<CgElement>();
        }
    }
}
