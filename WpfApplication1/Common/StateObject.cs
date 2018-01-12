namespace Common
{
    using System.Net.Sockets;
    using System.Text;

    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket;

        // Size of receive buffer.  
        public const int BufferSize = 256;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
}
