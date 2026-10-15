using UnityEngine;
using System;
using System.Threading.Tasks;

public class ApplicationLogic
{
    /**
     * This class should only be a wrapper for the functions that 
     * the ButtonsManager is going to call and nothing more.
     */

    /**
     * The python server right now handles just below 30 FPS. With further optimizations, it might be able to handle more.
     * If needed, change this value and test the performance of the server with the new value.
     * Even then, I'm not sure if the Magic Leap Device Cameras support more than 30 FPS,
     * but I haven't been able to find any official documentation about it.
     */
    private const int FramesPerSecond = 30;

    /**
     * Right now the connection between the Device and the Workstation where the server runs is handled by adb. 
     * Meaning that it itercepts any packet sent to localhost:[PORT] and it forwards it. (using adb reverse)
     * It is theoretically possible to stream it wirelessly but the speed it requires is so high that I felt more 
     * confident doing it this way. For reference: a single frame is 1016 * 1016 bytes, so around 1 MB; if
     * we stream 3 frames 30 times a second it would need 90MB/s which is around 720 Mb/s. So in order 
     * for it to be stable it would need a connection of 1 Gb/s. 
     */
    private const string DeviceIP = "127.0.0.1";

    /**
     * Choose whatever as long as it is consistent with the server.
     */
    private const int CameraStreamPort = 9000;
    // private const int PoseStreamPort = 9001;

    private StreamManager cameraStreamManager = new StreamManager(new CameraStreamableObject());
    private StreamManager poseStreamManager = new StreamManager(new PoseStreamableObject());

    // Button One
    public async Task BuildConnections()
    {
        /**
        * Warning: if you are using adb with port forwarding,
        * the connection will succeed even if the server is not running.
        * That is because adb will accept the connection first and check
        * if the server is running only afterwards. 
        */

        var cameraTask = cameraStreamManager.ConnectAsync(
            DeviceIP, 
            CameraStreamPort
        );
        await cameraTask;
        
        /**
         * not needed right now as the pose stream is not implemented
         *
        var poseTask = poseStreamManager.ConnectAsync(
            DeviceIP, 
            PoseStreamPort
        );
        await Task.WhenAll(cameraTask, poseTask);
        */
    }

    // Button Two
    public bool IsCameraStreaming => cameraStreamManager.IsStreaming;

    public async Task BeginCameraStream()
    {
        if (cameraStreamManager.IsStreaming)
        {
            Debug.LogWarning("Camera stream is already running.");
            return;
        }

        await cameraStreamManager.StartStreamAsync(FramesPerSecond);
    }

    public async Task EndCameraStream()
    {
        if (!cameraStreamManager.IsStreaming) 
        {
            Debug.LogWarning("Camera stream is not running yet.");
            return;
        }

        await cameraStreamManager.StopStreamAsync();
    }

    // Button Three (Not implemented yet)
    public bool IsPoseStreaming => poseStreamManager.IsStreaming;

    public async Task BeginPoseStream()
    {
        if (poseStreamManager.IsStreaming)
        {
            Debug.LogWarning("Pose stream is already running.");
            return;
        }

        await poseStreamManager.StartStreamAsync(FramesPerSecond);
    }
   
    public async Task EndPoseStream()
    {
        if (!poseStreamManager.IsStreaming)
        {
            Debug.LogWarning("Pose stream is not running yet.");
            return;
        }

        await poseStreamManager.StopStreamAsync();
    }

    // Button Four
    public async Task CleanUp()
    {   
        await Task.WhenAll(
            cameraStreamManager.DisconnectAsync(), 
            poseStreamManager.DisconnectAsync()
        );
    }
}