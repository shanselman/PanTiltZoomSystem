using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SignalR.Hubs;

namespace PTZSignalRRelay
{
    public class RelayHub : Hub
    {
        public void Move(int x, int y, string groupName)
        {
            Clients[groupName].Move(x, y); //test
        }

        public void Zoom(int value, string groupName)
        {
            Clients[groupName].Zoom(value);
        }

        public void JoinRelay(string groupName)
        {
            Groups.Add(Context.ConnectionId, groupName);
        }
    }
}