using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class ConnectionManager
{
    /**
     * Would be optimal to use an UDP socket, however adb doesnt
     * support it. Since cable is the only way to achieve 1 Gb/s 
     * reliably, the extra latency is tolerable.
     */
    private TcpClient Socket;

    /**
     * This lock is not technically needed as only one StreamManager has access to
     * the socket at once, but you never know.
     */
    private SemaphoreSlim SendLock = new SemaphoreSlim(1, 1);

    public bool IsConnected => Socket != null && Socket.Connected;

    public async Task Connect(string address, int port)
    {
        if (IsConnected)
        {
            Debug.LogWarning("Already connected.");
            return;
        }

        TcpClient client = new TcpClient();
        await client.ConnectAsync(address, port);
        Socket = client;

        Debug.Log($"Successfully connected to {address}:{port}");
    }

    public void Disconnect()
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Not connected.");
            return;
        }

        Socket.Close();
        Socket = null;

        Debug.Log("Disconnected.");
    }

    public async Task SendAsync(byte[] payload)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected. Cannot send data.");
        }
        
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        await SendLock.WaitAsync();
        try
        {
            NetworkStream stream = Socket.GetStream();
            if (!stream.CanWrite)
            {
                throw new InvalidOperationException("Socket stream is not writable.");
            }
            
            await stream.WriteAsync(payload, 0, payload.Length);
            await stream.FlushAsync();

            Debug.Log($"Sent {payload.Length} bytes.");
        }
        finally
        {
            SendLock.Release();
        }
    }
}
