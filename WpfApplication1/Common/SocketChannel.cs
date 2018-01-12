namespace Common
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    
    public class SocketChannel
    {
        // ManualResetEvent instances signal completion.  
        private static readonly ManualResetEvent AllDone = new ManualResetEvent(false);
        private static readonly ManualResetEvent connectDone = new ManualResetEvent(false);
        private static readonly ManualResetEvent sendDone = new ManualResetEvent(false);
        private static readonly ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.  
        private static string response = string.Empty;
        private Socket socket;
        private IPEndPoint remoteEndPoint;

        public SocketChannel(string ipAddress, int port)
        {   
            var parsedIpAddress = IPAddress.Parse(ipAddress);
            remoteEndPoint = new IPEndPoint(parsedIpAddress, port);
            
            this.socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public void ConnectAsServer(int numberOfClients)
        {
            try
            {
                socket.Bind(remoteEndPoint);
                socket.Listen(numberOfClients);

                while (true)
                {
                    AllDone.Reset();

                    socket.BeginAccept(AcceptCallback, socket);

                    AllDone.WaitOne();
                }
            }
            catch (Exception)
            {
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            AllDone.Set();

            var clientSocket = (Socket)ar.AsyncState;
            var handler = clientSocket.EndAccept(ar);

            var state = new StateObject();
            state.workSocket = handler;

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
        }

        public bool Connect()
        {
            try
            {
                // Connect to the remote endpoint.  
                socket.BeginConnect(remoteEndPoint, ConnectCallback, socket);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        public bool Disconnect()
        {
            try
            {
                // Release the socket.  
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public Byte[] Send(Byte[] content)
        {
            // Send test data to the remote device.  
            Send(socket, content);
            
            // Receive the response from the remote device.  
            Receive(socket);
            
            return null;
        }
        
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint);

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                var state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the socket    
                // from the asynchronous state object.  
                var state = (StateObject)ar.AsyncState;
                var client = state.workSocket;

                // Read data from the remote device.  
                var bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }

                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, byte[] data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            // var byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(data, 0, data.Length, 0, SendCallback, client);
            sendDone.WaitOne();
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                var bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
