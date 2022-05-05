using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerCore;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Dns (Domain name system) cmd -> ping www.google.com
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);


            for (int i = 0; i < 5; i++)
            {
                Connector connector = new Connector();
                connector.Connect(endPoint, () => { return new ServerSession(); });
            }

            while (true)
            {
                
                try
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                Thread.Sleep(100);
                
            }
        }
    }
}
