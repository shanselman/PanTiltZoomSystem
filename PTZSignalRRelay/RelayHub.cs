using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace PTZSignalRRelay
{
    public class RelayHub : Hub
    {
        public void Move(int x, int y, string groupName)
        {
            Clients.Group(groupName).Move(x, y);
        }

        public void Zoom(int value, string groupName)
        {
            Clients.Group(groupName).Zoom(value);
        }

        public void JoinRelay(string groupName)
        {
            Groups.Add(Context.ConnectionId, groupName);
        }
    }
}