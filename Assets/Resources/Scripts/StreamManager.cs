using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using MagicLeap.Android;
using UnityEngine.Android;

public class StreamManager
{
    /**
     * This class is responsible for managing the streaming of data to the server.
     * The connection itself is handled by the ConnectionManager.
     * A StreamableObject is an abstract representation of the source of the data
     * to be streamed. 
     */
    private readonly ConnectionManager connection;
    private readonly StreamableObject streamableObject;
    
    private Task streamTask;
    private CancellationTokenSource streamCancellation;
    public bool IsStreaming { get; private set; } = false;

    public StreamManager(StreamableObject streamableObject)
    {
        this.connection = new ConnectionManager();
        this.streamableObject = streamableObject;
    }

    public async Task ConnectAsync(string address, int port)
    {
        await connection.Connect(address, port);
    }

    /**
     * Android grants camera access at runtime, so the permission has to be checked before
     * the first frame is requested. Permission.RequestPermission is callback based, so it
     * is wrapped in a task with a timeout: StartStreamAsync fails fast instead of hanging
     * forever if the user never answers the dialog.
     */
    private async Task<bool> EnsurePermissionAsync()
    {
        if (Permissions.CheckPermission(Permissions.MarkerTracking))
        {
            return true;
        }

        TaskCompletionSource<bool> permissionTcs = new TaskCompletionSource<bool>();
        Permissions.RequestPermission(
            Permissions.MarkerTracking,
            _ => permissionTcs.TrySetResult(true),
            _ => permissionTcs.TrySetResult(false),
            _ => permissionTcs.TrySetResult(false)
        );

        Task completedTask = await Task.WhenAny(permissionTcs.Task, Task.Delay(5000));
        if (completedTask != permissionTcs.Task)
        {
            return false;
        }

        return permissionTcs.Task.Result;
    }

    public async Task StartStreamAsync(uint framesPerSecond)
    {        
        if (framesPerSecond == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), "Frames per second must be greater than 0.");
        }

        if (!connection.IsConnected)
        {
            throw new InvalidOperationException("Not connected. Cannot start streaming.");
        }

        if (IsStreaming)
        {
            throw new InvalidOperationException("Already streaming.");
        }

        bool permissionGranted = await EnsurePermissionAsync();
        if (!permissionGranted)
        {
            throw new InvalidOperationException("Permission denied. Cannot start streaming.");
        }

        streamableObject.Initialize();

        IsStreaming = true;
        streamCancellation = new CancellationTokenSource();
        streamTask = StreamFramesAsync(streamCancellation.Token, framesPerSecond);
    }

    private async Task StreamFramesAsync(CancellationToken cancellationToken, uint framesPerSecond)
    {
        TimeSpan frameInterval = TimeSpan.FromSeconds(1d / framesPerSecond);
        uint timeout = Math.Max(1u, (uint)(frameInterval.TotalMilliseconds * 0.95d));  // Keep timeout slightly below frame period.

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                /**
                 * The timer is needed to that the time needed to read the frame is taken
                 * into account when calculating the delay, to achieve a more stable frame rate.
                 */
                var frameTimer = System.Diagnostics.Stopwatch.StartNew();

                await connection.SendAsync(
                    streamableObject.GetData(timeout)
                );

                frameTimer.Stop();

                TimeSpan remainingDelay = frameInterval - frameTimer.Elapsed;
                while (remainingDelay < TimeSpan.Zero)
                {
                    remainingDelay += frameInterval;
                }
                await Task.Delay(remainingDelay, cancellationToken);
            }
        }
        finally
        {
            IsStreaming = false;
        }
    }

    public async Task StopStreamAsync()
    {
        if (!IsStreaming)
        {
            throw new InvalidOperationException("Not currently streaming.");
        }

        try
        {
            streamCancellation.Cancel();
            await streamTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when stop is requested.
        }
        finally
        {
            streamCancellation.Dispose();
        }
    }

    public async Task DisconnectAsync()
    {
        if (IsStreaming)
        {   
            await StopStreamAsync();
        }

        connection.Disconnect();
    }
}