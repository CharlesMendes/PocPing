using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace starkdev.poc.ping
{
    class Program
    {
        [DllImport("wininet", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref ConnectionStatusEnum flags, int dw);

        static void Main(string[] args)
        {
            ListDHCPServers();
            //IsLocalIpAddress(endereco);

            Console.WriteLine("Informe o endereço a ser pesquisado:");
            string endereco = Console.ReadLine();
            Console.WriteLine();

            if (!string.IsNullOrEmpty(endereco))
            {
                Console.WriteLine(PingHost(endereco));
            }

            else
                Console.WriteLine("[StarkDev] Nenhum endereço informado");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para fechar...");
            Console.Read();
        }

        public static string PingHost(string host)
        {
            //string to hold our return messge
            string returnMessage = string.Empty;
            string printMessage = string.Empty;

            //IPAddress instance for holding the returned host
            IPAddress address = GetIpFromHost(ref host);

            if (address == null)
                returnMessage = string.Format("[StarkDev] A solicitação ping não pôde encontrar o host {0}. Verifique o nome e tente novamente.", host);

            else
            {
                //set the ping options, TTL 128
                PingOptions pingOptions = new PingOptions(128, true);

                //create a new ping instance
                Ping ping = new Ping();

                //32 byte buffer (create empty)
                byte[] buffer = new byte[32];

                //first make sure we actually have an internet connection
                if (HasConnection())
                {
                    Console.WriteLine("============================================");

                    //here we will ping the host 4 times (standard)
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            //send the ping 4 times to the host and record the returned data.
                            //The Send() method expects 4 items:
                            //1) The IPAddress we are pinging
                            //2) The timeout value
                            //3) A buffer (our byte array)
                            //4) PingOptions
                            PingReply pingReply = ping.Send(address, 1000, buffer, pingOptions);

                            //make sure we dont have a null reply
                            if (!(pingReply == null))
                            {
                                switch (pingReply.Status)
                                {
                                    case IPStatus.Success:
                                        printMessage = string.Format("[StarkDev] Resposta de {0}: bytes={1} time={2}ms TTL={3}", pingReply.Address, pingReply.Buffer.Length, pingReply.RoundtripTime, pingReply.Options.Ttl);
                                        break;

                                    case IPStatus.TimedOut:
                                        printMessage = "[StarkDev] Ops, timed out...";
                                        break;

                                    default:
                                        printMessage = string.Format("[StarkDev] Erro Ping: {0}", pingReply.Status.ToString());
                                        break;
                                }

                                Console.WriteLine(printMessage);
                            }
                            else
                                returnMessage = "[StarkDev] Conexão falhou, erro desconhecido...";
                        }
                        catch (PingException ex)
                        {
                            returnMessage = string.Format("[StarkDev] Connection Error (Ping): {0}", ex.Message);
                        }
                        catch (SocketException ex)
                        {
                            returnMessage = string.Format("[StarkDev] Connection Error (Socket): {0}", ex.Message);
                        }
                        catch (Exception ex)
                        {
                            returnMessage = string.Format("[StarkDev] Erro: {0}", ex.Message);
                        }

                        //Sleep for 2 seconds
                        Thread.Sleep(2000);
                    }
                    Console.WriteLine("============================================");
                }
                else
                    returnMessage = "[StarkDev] Sem conexão a Internet...";
            }

            //return the message
            return returnMessage;
        }

        /// <summary>
        /// method for retrieving the IP address from the host provided
        /// </summary>
        /// <param name="host">the host we need the address for</param>
        /// <returns></returns>
        private static IPAddress GetIpFromHost(ref string host)
        {
            //variable to hold our error message (if something fails)
            string errMessage = string.Empty;

            //IPAddress instance for holding the returned host
            IPAddress address = null;

            //wrap the attempt in a try..catch to capture
            //any exceptions that may occur
            try
            {
                //get the host IP from the name provided
                address = Dns.GetHostEntry(host).AddressList[0];
            }
            catch (SocketException ex)
            {
                //some DNS error happened, return the message
                errMessage = string.Format("[StarkDev] Erro de DNS: {0}", ex.Message);
                Console.WriteLine(string.Format("{0}", errMessage));
            }
            return address;
        }

        /// <summary>
        /// method to check the status of the pinging machines internet connection
        /// </summary>
        /// <returns></returns>
        private static bool HasConnection()
        {
            //instance of our ConnectionStatusEnum
            ConnectionStatusEnum state = 0;

            //call the API
            InternetGetConnectedState(ref state, 0);

            //check the status, if not offline and the returned state
            //isnt 0 then we have a connection
            if (((int)ConnectionStatusEnum.INTERNET_CONNECTION_OFFLINE & (int)state) != 0)
            {
                //return true, we have a connection
                return false;
            }
            //return false, no connection available
            return true;
        }

        /// <summary>
        /// enum to hold the possible connection states
        /// </summary>
        [Flags]
        enum ConnectionStatusEnum : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        /// <summary>
        /// Verifica se o endereco host informado pertence a algum ip local
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static bool IsLocalIpAddress(string host)
        {
            try
            { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static void ListDHCPServers()
        {
            Console.WriteLine("DHCP Servers");
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {

                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                IPAddressCollection addresses = adapterProperties.DhcpServerAddresses;
                if (addresses.Count > 0)
                {
                    Console.WriteLine(adapter.Description);
                    foreach (IPAddress address in addresses)
                    {
                        Console.WriteLine("  Dhcp Address ............................ : {0}",
                            address.ToString());
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
