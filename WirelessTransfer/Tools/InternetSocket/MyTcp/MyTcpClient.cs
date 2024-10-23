using WirelessTransfer.Tools.InternetSocket.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WirelessTransfer.Tools.InternetSocket.MyTcp
{
    public class MyTcpClient
    {
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        TcpClient client;
        IPAddress serverIp;
        int serverPort;
        string clientName;

        int counter = 0;

        public MyTcpClient(IPAddress serverIp, int serverPort, string clientName)
        {
            client = new TcpClient();
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            this.clientName = clientName;
        }

        /// <summary>
        /// Connect to server asynchronously.
        /// </summary>
        public void Connect()
        {
            if (!IsConnected())
                client.BeginConnect(serverIp, serverPort, new AsyncCallback(OnConnect), null);
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                ClientInfoCmd clientInfoCmd = new ClientInfoCmd(clientName, IPAddress.Any);
                byte[] bytes = clientInfoCmd.Encode();
                client.GetStream().Write(bytes);
                Connected?.Invoke(this, new EventArgs());
            }
            catch (IOException)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
            catch (SocketException)
            {
                if (counter == 10)
                {
                    Disconnected?.Invoke(this, new EventArgs());
                    counter = 0;
                }
                else
                {
                    Task.Delay(500).ContinueWith((o) =>
                    {
                        counter++;
                        client.BeginConnect(serverIp, serverPort, new AsyncCallback(OnConnect), null);
                    });
                }
            }
        }

        public bool IsConnected()
        {
            try
            {
                byte[] buffer = new byte[1];
                client.Client.Receive(buffer, SocketFlags.Peek);
                return true; // If no exception, client is still connected
            }
            catch (SocketException)
            {
                return false; // If an exception occurs, client is disconnected
            }
        }

        public void Disconnect()
        {
            client.Close();
            Disconnected?.Invoke(this, new EventArgs());
        }
    }
}
