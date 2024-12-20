﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WirelessTransfer.Tools.InternetSocket
{
    public static class InternetInfo
    {
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }

        public static string GetSSID()
        {
            string notFoundMessage = "No wifi connected!";
            try
            {
                // Run the netsh command to get the Wi-Fi info
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Extract the SSID from the output
                int startIndex = output.IndexOf("SSID");
                if (startIndex == -1) return notFoundMessage;
                startIndex = output.IndexOf(":", startIndex + 4);
                if (startIndex == -1) return notFoundMessage;
                int endIndex = output.IndexOf("\n", startIndex + 1);
                if (endIndex == -1) return notFoundMessage;
                return output.Substring(startIndex + 1, endIndex - startIndex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error: " + ex.Message);
                return notFoundMessage;
            }
        }
    }
}
