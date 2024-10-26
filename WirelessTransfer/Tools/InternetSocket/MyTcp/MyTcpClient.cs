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
        public event EventHandler<Cmd.Cmd> ReceivedCmd;

        TcpClient client;
        IPAddress serverIp;
        int serverPort;
        string clientName;

        int timeoutCounter = 0;
        byte[] buffer = new byte[5242880]; // 5MB

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

                client.GetStream().BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), null);
            }
            catch (IOException)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
            catch (SocketException)
            {
                if (timeoutCounter == 10)
                {
                    Disconnected?.Invoke(this, new EventArgs());
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

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                int actualLength = client.GetStream().EndRead(ar);
                if (actualLength > 0)
                {
                    Cmd.Cmd? cmd = CmdDecoder.DecodeCmd(buffer, 0, actualLength);
                    if (cmd != null) ReceivedCmd?.Invoke(this, cmd);
                }

                client.GetStream().BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), null);
            }
            catch (IOException)
            {
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        public void SendCmd(Cmd.Cmd cmd)
        {
            try
            {
                byte[] bytes = cmd.Encode();
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (IOException)
            {
                Disconnected?.Invoke(this, new EventArgs());
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
