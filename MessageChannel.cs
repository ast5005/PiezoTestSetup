using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiezoTestSetup
{
    public delegate void MessageEventHandler(object sender, EventArgs e);
    public class MessageChannel
    {
        internal event MessageEventHandler msgChanged;

        //Current Message that will show up at  main window
        public string currMessage;
        public string currTimeStamp;
        public DateTime startTime;
        internal bool newmsg=false;
        public MessageChannel()
        {
            currMessage = "System initilazing\n";
            startTime = DateTime.Now;
            currTimeStamp = startTime.ToString();
            OnChanged(EventArgs.Empty);
        }
        public bool setcurrMessage(string Mesg)
        {
            //currMessage = Mesg;
            DateTime now = DateTime.Now;
            currTimeStamp = now.Subtract(startTime).ToString();
            currMessage += currTimeStamp + ": " + Mesg + "\n";
            newmsg = true;
            OnChanged(EventArgs.Empty);
            return true;
        }
        public string getMessage()
        {
            newmsg = false;
            return currMessage;
        }
        internal virtual void OnChanged(EventArgs e)
        {
            if(msgChanged != null) msgChanged(this, e);
        }
        public static implicit operator MessageChannel(Messagechannel v)
        {
            throw new NotImplementedException();
        }
    }
}
