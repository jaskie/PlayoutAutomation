using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace TAS.Server.Remoting
{
    public class MediaManagerBehavior: WebSocketBehavior
    {
        readonly MediaManager _mediaManager;
        public MediaManagerBehavior(MediaManager mediaManager)
        {
            _mediaManager = mediaManager;
            Debug.WriteLine("Created MM behavior");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }   

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
        }
    }
}
