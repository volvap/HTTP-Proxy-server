using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
class Program
{
    static void Main(string[] args)
    {
        //IPAddress ad = IPAddress.Parse("127.0.0.1");
        TcpListener listener = new TcpListener(IPAddress.Any, 8001); //прослушивает входящие подключения по определенному порту. 
        listener.Start();

        while (true)
        {
            Socket Client = listener.AcceptSocket();
            ThreadPool.QueueUserWorkItem(ProxySocket, Client); //Помещает метод в очередь на выполнение и указывает объект, содержащий данные для использования методом.
        }
    }   

    
    private static Regex RegexHost = new Regex(@"(Host:\s)(\S+)");
    private static Regex RegexHTTPAnswer = new Regex(@"(HTTP/1.1\s)(\S+\s)(\S+)");
    private static Regex R = new Regex(@"(:)(\S+)");


    static void ProxySocket(object request)
    {
        try
        {
            string requestString = string.Empty;
            int bytesReceived;
            int bytesSended;
            string Host = "";
            byte[] buffer = new byte[60000];
            byte[] byteOriginalRequest;
            Int32 pport;

            Socket socketClient = (Socket)request;

            bytesReceived = socketClient.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            while (socketClient.Available > 0)
            {
                bytesReceived = socketClient.Receive(buffer, 0, buffer.Length, SocketFlags.None);
            }
            
            byteOriginalRequest = buffer;
            
            requestString = Encoding.ASCII.GetString(byteOriginalRequest);

            Match MatchHost = RegexHost.Match(requestString);
            if (MatchHost.Success)
            {
                Host = MatchHost.Groups[2].Value;
            }
 

            IPAddress[] ipAddress = Dns.GetHostAddresses(Host);
            IPEndPoint endPoint = new IPEndPoint(ipAddress[0],80);

            Socket SocketProxy = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            {
                SocketProxy.Connect(endPoint);

                bytesSended = SocketProxy.Send(byteOriginalRequest, byteOriginalRequest.Length, SocketFlags.None);
                try
                {
                    
                        bytesReceived = SocketProxy.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                byte[] FinalResponse = buffer;
                string stringFinalResponse = Encoding.ASCII.GetString(FinalResponse);
               // Console.WriteLine(stringFinalResponse);
                string Answer = "";
                Match MatchHTTPAnswer = RegexHTTPAnswer.Match(stringFinalResponse);
                Answer = MatchHTTPAnswer.Groups[2].Value + MatchHTTPAnswer.Groups[3].Value;
                if (MatchHTTPAnswer.Success)
                {
                    Console.WriteLine(" URL {0} Answer code {1}", Host, Answer);
                }
                bytesSended = socketClient.Send(FinalResponse, FinalResponse.Length, SocketFlags.None);
                SocketProxy.Shutdown(SocketShutdown.Send);
                SocketProxy.Close();
            }
            socketClient.Shutdown(SocketShutdown.Send);
            socketClient.Close();
        }
        catch (Exception ex)
        {
        }
    }
    
}