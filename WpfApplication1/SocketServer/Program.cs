namespace SocketServer
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    // State object for reading client data asynchronously  
    public class StateObject
    {
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.  
        public StringBuilder sb = new StringBuilder();

        // Client  socket.  
        public Socket workSocket;
    }

    internal class Program
    {
        private static readonly ManualResetEvent AllDone = new ManualResetEvent(false);

        private static void AcceptCallback(IAsyncResult ar)
        {
            AllDone.Set();

            var clientSocket = (Socket)ar.AsyncState;
            var handler = clientSocket.EndAccept(ar);

            var state = new StateObject();
            state.workSocket = handler;

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        }

        private static void Main(string[] args)
        {
            StartListening();
        }

        private static void ReadCallback(IAsyncResult ar)
        {
            var content = string.Empty;

            var state = (StateObject)ar.AsyncState;
            var clientSocket = state.workSocket;
            var bytesRead = clientSocket.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the   
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);

                    // Echo the data back to the client.  
                    Send(clientSocket, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
                }
            }
        }

        private static void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            var byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                var bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void StartListening()
        {
            ////var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = IPAddress.Parse("127.0.0.1"); ////ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, 11000);

            var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    AllDone.Reset();

                    listener.BeginAccept(AcceptCallback, listener);

                    AllDone.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error.", ex);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
