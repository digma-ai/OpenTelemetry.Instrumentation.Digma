using System.Net;
using System.Net.Sockets;

namespace AutoInstrumentation.ZeroCodeTests.Utils;

public static class PortUtils
{
    public static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}