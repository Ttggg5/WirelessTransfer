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
        public event EventHandler<Cmd.Cmd> ReceivedCmd;

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

                        //tcpClient.GetStream().BeginRead(tmpBuffer, 0, tmpBuffer.Length, new AsyncCallback(ReceiveCallBack), ConnectedClients.Last());
                        Task.Factory.StartNew(() =>
                        {
                            MyTcpClientInfo clientInfo = ConnectedClients.Last();
                            try
                            {
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
                                        /*
                                        for (int i = 0; i < actualLength; i++)
                                        {
                                            buffer[EndIndex++] = tmpBuffer[i];
                                            if (EndIndex == buffer.Length) EndIndex = 0;
                                            if (startIndex == EndIndex) startIndex++;
                                            if (startIndex == buffer.Length) startIndex = 0;
                                        }
                                        */
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
                                if (ConnectedClients.Remove(clientInfo))
                                {
                                    ClientDisconnected?.Invoke(this, clientInfo);

                                    // Start accepting the next client asynchronously when client is not full
                                    if (ConnectedClients.Count < MaxClient)
                                        Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
                                }
                            }
                        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                    else tcpClient.Close();
                }

                // Start accepting the next client asynchronously when client is not full
                if (ConnectedClients.Count < MaxClient)
                    Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;

            MyTcpClientInfo mtci = (MyTcpClientInfo)ar.AsyncState;
            try
            {
                TcpClient client = mtci.Client;
                int actualLength = client.GetStream().EndRead(ar);
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
                    /*
                    for (int i = 0; i < actualLength; i++)
                    {
                        buffer[EndIndex++] = tmpBuffer[i];
                        if (EndIndex == buffer.Length) EndIndex = 0;
                        if (startIndex == EndIndex) startIndex++;
                        if (startIndex == buffer.Length) startIndex = 0;
                    }
                    */
                    Cmd.Cmd? cmd = CmdDecoder.DecodeCmd(buffer, ref startIndex, ref EndIndex);
                    if (cmd != null) ReceivedCmd?.Invoke(this, cmd);
                }
                client.GetStream().BeginRead(tmpBuffer, 0, tmpBuffer.Length, new AsyncCallback(ReceiveCallBack), mtci);
            }
            catch (Exception ex)
            {
                if (ConnectedClients.Remove(mtci))
                {
                    ClientDisconnected?.Invoke(this, mtci);

                    // Start accepting the next client asynchronously when client is not full
                    if (ConnectedClients.Count < MaxClient)
                        Server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), null);
                }
            }
        }

        public void SendCmd(Cmd.Cmd cmd, MyTcpClientInfo clientInfo)
        {
            try
            {
                byte[] bytes = cmd.Encode();
                clientInfo.Client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch
            {
                if (ConnectedClients.Remove(clientInfo))
                    ClientDisconnected?.Invoke(this, clientInfo);
            }
        }

        public void Stop()
        {
            Server?.Stop();
            if (ConnectedClients.Count > 0)
            {
                foreach (var client in ConnectedClients)
                {
                    ClientDisconnected?.Invoke(this, client);
                }
            }
            ConnectedClients.Clear();
        }
    }
}
