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
    public enum MyTcpServerState
    {
        Listening,
        Closed,
    }

    public class MyTcpServer
    {
        public event EventHandler<MyTcpClientInfo> ClientConnected;
        public event EventHandler<MyTcpClientInfo> ClientDisconnected;
        public event EventHandler<Cmd.Cmd> ReceivedCmd;

        public MyTcpServerState CurState { get; private set; }
        public TcpListener? Server { get; }
        public List<MyTcpClientInfo> ConnectedClients { get; set; }
        public IPEndPoint LocalIPEP { get; }
        public int MaxClient { get; private set; }

        int startIndex = 0, EndIndex = 0;
        byte[] buffer = new byte[12582912]; // 12MB
        byte[] tmpBuffer = new byte[6291456]; // 6MB

        public MyTcpServer(int port)
        {
            LocalIPEP = new IPEndPoint(IPAddress.Any, port);
            ConnectedClients = new List<MyTcpClientInfo>();
            Server = new TcpListener(LocalIPEP);
            CurState = MyTcpServerState.Closed;
        }

        /// <summary>
        /// Start the server and listening for client connections asynchronously.
        /// </summary>
        /// <param name="maxClient">0 means no limit</param>
        public void Start(int maxClient)
        {
            MaxClient = maxClient;
            Server?.Start();
            Server?.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
            CurState = MyTcpServerState.Listening;
        }

        private void OnClientConnect(IAsyncResult ar)
        {
            byte[] tmpBuf = new byte[1024]; // this is only for ClientInfoCmd

            // Accept the client
            try
            {
                TcpClient tcpClient = Server.EndAcceptTcpClient(ar);
                int actualLength = tcpClient.GetStream().Read(tmpBuf, 0, tmpBuf.Length);
                if (actualLength > 0)
                {
                    Cmd.Cmd? cmd = CmdDecoder.DecodeCmd(tmpBuf, 0, actualLength);
                    if (cmd != null && cmd.CmdType == CmdType.ClientInfo)
                    {
                        ConnectedClients.Add(new MyTcpClientInfo(tcpClient, ((ClientInfoCmd)cmd).ClientName, ((ClientInfoCmd)cmd).IP));
                        ClientConnected?.Invoke(this, ConnectedClients.Last());

                        Task.Factory.StartNew(() =>
                        {
                            MyTcpClientInfo clientInfo = null;
                            try
                            {
                                clientInfo = ConnectedClients.Last();
                                while (true)
                                {
                                    int actualLength = clientInfo.Client.GetStream().Read(tmpBuffer, 0, tmpBuffer.Length);
                                    if (actualLength > 0)
                                    {
                                        int tmpLength = buffer.Length - EndIndex;
                                        if (actualLength <= tmpLength)
                                        {
                                            Array.Copy(tmpBuffer, 0, buffer, EndIndex, actualLength);
                                            EndIndex += actualLength;
                                        }
                                        else
                                        {
                                            Array.Copy(tmpBuffer, 0, buffer, EndIndex, tmpLength);
                                            Array.Copy(tmpBuffer, tmpLength, buffer, 0, actualLength - tmpLength);
                                            EndIndex = actualLength - tmpLength;
                                        }

                                        while (true)
                                        {
                                            Cmd.Cmd? cmd = CmdDecoder.DecodeCmd(buffer, ref startIndex, ref EndIndex);
                                            if (cmd != null)
                                            {
                                                ReceivedCmd?.Invoke(this, cmd);
                                                continue;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                bool removeable = false;
                                lock (ConnectedClients)
                                {
                                    if (removeable = ConnectedClients.Remove(clientInfo))
                                    {
                                        // Start accepting the next client asynchronously when client is not full
                                        if (ConnectedClients.Count < MaxClient)
                                        {
                                            try
                                            {
                                                Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
                                            }
                                            catch
                                            {
                                                CurState = MyTcpServerState.Closed;
                                            }
                                        }
                                    }
                                }
                                if (removeable)
                                    ClientDisconnected?.Invoke(this, clientInfo);
                            }
                        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                    else tcpClient.Close();
                }

                // Start accepting the next client asynchronously when client is not full
                if (ConnectedClients.Count < MaxClient)
                {
                    try
                    {
                        Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
                    }
                    catch
                    {
                        CurState = MyTcpServerState.Closed;
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        public void SendCmd(Cmd.Cmd cmd, MyTcpClientInfo clientInfo)
        {
            if (CurState == MyTcpServerState.Closed) return;

            try
            {
                byte[] bytes = cmd.Encode();
                clientInfo.Client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch
            {
                bool removeable = false;
                lock (ConnectedClients)
                {
                    removeable = ConnectedClients.Remove(clientInfo);
                }
                if (removeable)
                    ClientDisconnected?.Invoke(this, clientInfo);
            }
        }

        public void Stop()
        {
            List<MyTcpClientInfo> tmp = new List<MyTcpClientInfo>();
            lock (ConnectedClients)
            {
                if (ConnectedClients.Count > 0)
                {
                    foreach (var client in ConnectedClients)
                    {
                        client.Client.Close();
                        tmp.Add(client);
                    }
                }
                ConnectedClients.Clear();
            }

            tmp.ForEach(client =>
            {
                ClientDisconnected?.Invoke(this, client);
            });
            tmp.Clear();

            Server?.Stop();
            CurState = MyTcpServerState.Closed;
        }
    }
}
