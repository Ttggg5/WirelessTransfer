﻿using WirelessTransfer.Tools.InternetSocket.Cmd;
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
    public enum MyTcpClientState
    {
        Waiting,
        Connected,
        Disconnected,
    }

    public class MyTcpClient
    {
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<Cmd.Cmd> ReceivedCmd;

        public MyTcpClientState State { get; private set; }
        public TcpClient client { get; private set; }
        public IPAddress serverIp { get; private set; }
        public int serverPort { get; private set; }
        public string clientName { get; private set; }

        int timeoutCounter = 0;
        int startIndex = 0, EndIndex = 0;
        byte[] buffer = new byte[12582912]; // 12MB

        public MyTcpClient(IPAddress serverIp, int serverPort, string clientName)
        {
            client = new TcpClient();
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            this.clientName = clientName;

            State = MyTcpClientState.Disconnected;
        }

        /// <summary>
        /// Connect to server asynchronously.
        /// </summary>
        public void Connect()
        {
            if (State == MyTcpClientState.Disconnected)
            {
                State = MyTcpClientState.Waiting;
                client.BeginConnect(serverIp, serverPort, new AsyncCallback(OnConnect), null);
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                ClientInfoCmd clientInfoCmd = new ClientInfoCmd(clientName, GetLocalIPAddress());
                byte[] bytes = clientInfoCmd.Encode();
                client.GetStream().Write(bytes);
                State = MyTcpClientState.Connected;
                Connected?.Invoke(this, new EventArgs());

                //client.GetStream().BeginRead(tmpBuffer, 0, tmpBuffer.Length, new AsyncCallback(ReceiveCallBack), null);
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            int actualLength = client.GetStream().Read(buffer, EndIndex, buffer.Length - EndIndex);
                            if (actualLength > 0)
                            {
                                EndIndex += actualLength;
                                if (EndIndex >= buffer.Length)
                                    EndIndex -= buffer.Length;

                                // prevent it doesn't only read one cmd
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
                            else throw new Exception("no data read");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (State != MyTcpClientState.Disconnected)
                        {
                            State = MyTcpClientState.Disconnected;
                            Disconnected?.Invoke(this, new EventArgs());
                        }
                    }
                });
            }
            catch (IOException)
            {
                if (State != MyTcpClientState.Disconnected)
                {
                    State = MyTcpClientState.Disconnected;
                    Disconnected?.Invoke(this, new EventArgs());
                }
            }
            catch (SocketException)
            {
                if (timeoutCounter == 10)
                {
                    if (State != MyTcpClientState.Disconnected)
                    {
                        State = MyTcpClientState.Disconnected;
                        Disconnected?.Invoke(this, new EventArgs());
                    }
                    timeoutCounter = 0;
                }
                else
                {
                    Task.Delay(500).ContinueWith((o) =>
                    {
                        timeoutCounter++;
                        client.BeginConnect(serverIp, serverPort, new AsyncCallback(OnConnect), null);
                    });
                }
            }
        }

        public void SendCmd(Cmd.Cmd cmd)
        {
            try
            {
                byte[] bytes = cmd.Encode();
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                if (State != MyTcpClientState.Disconnected)
                {
                    State = MyTcpClientState.Disconnected;
                    Disconnected?.Invoke(this, new EventArgs());
                }
            }
        }

        private IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.None;
        }

        public void Disconnect()
        {
            if (State != MyTcpClientState.Disconnected)
            {
                State = MyTcpClientState.Disconnected;
                client.Close();
                Disconnected?.Invoke(this, new EventArgs());
            }
        }
    }
}
