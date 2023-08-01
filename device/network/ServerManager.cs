using GodOfBeer.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodOfBeer.network
{
    class ServerManager : GenericSingleton<ServerManager>
    {
        public MainServer mainServer = new MainServer();
        public StatelessServer statelessServer = new StatelessServer();
        public UdpSessionManager udpSessionManager = new UdpSessionManager();

        public event EventHandler OnOrderUpdate;
        public event EventHandler OnDeviceUpdate;

        public void UpdateOrder()
        {
            OnOrderUpdate?.Invoke(this, null);
        }

        public void UpdateDeivce()
        {
            OnDeviceUpdate.Invoke(this, null);
        }
    }
}
