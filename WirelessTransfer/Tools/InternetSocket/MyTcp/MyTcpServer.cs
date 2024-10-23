using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WirelessTransfer.Tools.InternetSocket.MyTcp
{
    public class MyTcpServer
    {
        public event EventHandler<MyTcpClientInfo> ClientConnected;
        public event EventHandler<MyTcpClientInfo> ClientDisconnected;

        public TcpListener? Server { get; }
        public List<MyTcpClientInfo> ConnectedClients { get; set; }
        public IPEndPoint LocalIPEP { get; }

        public MyTcpServer(int port)
        {
            LocalIPEP = new IPEndPoint(IPAddress.Any, port);
            ConnectedClients = new List<MyTcpClientInfo>();
            Server = new TcpListener(LocalIPEP);
        }

        /// <summary>
        /// Start the server and listening for client connections asynchronously.
        /// </summary>
        /// <param name="maxClient">0 means no limit</param>
        public void Start(int maxClient)
        {
            Server?.Start(maxClient);
            Server?.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        }

        private void OnClientConnect(IAsyncResult ar)
        {
            byte[] tmpBuf = new byte[1024]; // this is only for ClientInfoCmd

            // Accept the client
            TcpClient tcpClient = Server.EndAcceptTcpClient(ar);
            try
            {
                int actualLength = tcpClient.GetStream().Read(tmpBuf, 0, tmpBuf.Length);
                if (actualLength > 0)
                {
                    Cmd.Cmd? cmd = CmdDecoder.DecodeCmd(tmpBuf, 0, actualLength);
                    if (cmd != null && cmd.CmdType == CmdType.ClientInfo)
                    {
                        cmd.Decode();
                        ConnectedClients.Add(new MyTcpClientInfo(tcpClient, ((ClientInfoCmd)cmd).ClientName, ((ClientInfoCmd)cmd).IP));
                        ClientConnected?.Invoke(this, ConnectedClients.Last());
                    }
                    else tcpClient.Close();
                }
            }
            catch (IOException)
            {

            }

            // Start accepting the next client asynchronously
            Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
        }

        public void Stop()
        {
            Server?.Stop();
            foreach (var client in ConnectedClients)
            {
                ClientDisconnected?.Invoke(this, client);
            }
            ConnectedClients.Clear();
        }
    }
}
