using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PTZRemoteCameraListener
{
    class PTZRemoteTraceWriter : TextWriter
    {
        private Action<string> log;
        public PTZRemoteTraceWriter(Action<string> Log)
        {
            this.log = Log;
        }

        public override void WriteLine(string value)
        {
            log(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        
    }
}
