using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public class CameraStreamableObject : StreamableObject
{
    /**
     * This class is responsible for interfacing with the MagicLeap API,
     * to store the data received, and to provide a function that builds
     * the byte array that will be sent through the connection.
     * You can find the API documentation here:
     * https://developer-docs.magicleap.cloud/docs/api-ref/api/Modules/group___pixel_sensors/group___w_cam/
     */
    private MLWorldCamera cameraHandle = new MLWorldCamera();

    private Dictionary<MLWorldCamera.CameraId, CameraFrame> latestFrames = new()
    {
        { MLWorldCamera.CameraId.Center, null },
        { MLWorldCamera.CameraId.Left,   null },
        { MLWorldCamera.CameraId.Right,  null },
    };

    public override void Initialize()
    {
        /**
         * For some reason if I put this code inside the constructor, Unity refuses to compile it.
         * I suspect this has to do with the fact that it tries to optimize it but the API is not
         * fully loaded which in turn causes an error. 
         */

        MLWorldCamera.Settings cameraSettings = new MLWorldCamera.Settings(
            MLWorldCamera.Mode.NormalExposure, 
            MLWorldCamera.CameraId.Center | 
            MLWorldCamera.CameraId.Left   | 
            MLWorldCamera.CameraId.Right
        );

        MLResult connectResult = cameraHandle.Connect(in cameraSettings);
        if (!connectResult.IsOk)
        {
            throw new InvalidOperationException($"MLWorldCamera connect failed: {connectResult.Result}");
        }
    }

    private bool UpdateLatestFrames(uint timeout)
    {
        MLResult frameResult = cameraHandle.GetLatestWorldCameraData(out var frames, timeout);
        if (!frameResult.IsOk || frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"Failed to get latest camera frames: {frameResult.Result}");
            return false;
        }

        bool res = true;
        foreach (var frame in frames)
        {
            if (frame.FrameBuffer.Data == IntPtr.Zero || frame.FrameBuffer.DataSize <= 0)
            {
                Debug.LogWarning($"Skipping update due to invalid frame data from {frame.CameraId} camera.");
                res = false;
            }
            else
            {
                latestFrames[frame.CameraId] = new CameraFrame(frame);
            }
        }
        return res;
    }

    private byte[] BuildPacket()
    {
        /**
         * The packet is organized in the following way:
         * - Packer header (4 bytes): "WCAM"
         * - Metadata lenght (4 bytes)
         * - Metadata in json format
         * - Frame lenght (4 bytes) (usually 1016*1016)
         * - Raw frame bytes
         */

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Encoding.ASCII.GetBytes("WCAM"));

            foreach (var frame in latestFrames.Values)
            {
                if (frame == null)
                {
                    writer.Write(0); // metadata length
                    writer.Write(0); // buffer length
                    continue;
                }

                byte[] metadataBytes = frame.GetMetadata();
                writer.Write(metadataBytes.Length);
                writer.Write(metadataBytes);
                
                byte[] buffer = frame.GetBuffer();
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    public override byte[] GetData(uint timeoutMilliseconds)
    {
        /**
         * This is the method we espose to the application to get the data
         */

        bool framesUpdated = UpdateLatestFrames(timeoutMilliseconds);
        if (!framesUpdated)
        {
            Debug.LogWarning("Using previously cached frames due to update failure.");
        }

        return BuildPacket();
    }

    private class CameraFrame 
    {
        /**
         * This private class helps with the hanlding of the frame data
         */

        /**
         * This class needs [Serializable] otherwise when converting to a json the packet will include 
         * json syntax elements such as {} and :. I suppose it would be fine if the server handled it 
         * properly but as it is implemented now it does not.
         * Also most of the metadata is unnecessary but i kept it because the overhead is really low.
         */
        [Serializable]
        private class CameraFrameMetadata
        {
            public string cameraId;
            public string frameType;
            public long frameNumber;
            public uint width;
            public uint height;
            public uint stride;
            public uint bytesPerPixel;
            public long timestampNs;
        }

        private byte[] buffer;
        private CameraFrameMetadata metadata;

        public CameraFrame(MLWorldCamera.Frame frame)
        {
            /**
             * It is *really* important to do a deep copy and not a shallow one because the application doesn't
             * "own" the data passed by the pointer passed by GetLatestWorldCameraData(). Meaning
             * that as soon as it goes out of scope, the memory is cleaned and we have a null reference.
             */
            buffer = new byte[frame.FrameBuffer.DataSize];
            Marshal.Copy(frame.FrameBuffer.Data, buffer, 0, buffer.Length);

            metadata = new CameraFrameMetadata
            {
                cameraId = frame.CameraId.ToString(),
                frameType = frame.FrameType.ToString(),
                frameNumber = frame.FrameNumber,
                width = frame.FrameBuffer.Width,
                height = frame.FrameBuffer.Height,
                stride = frame.FrameBuffer.Stride,
                bytesPerPixel = frame.FrameBuffer.BytesPerPixel,
                timestampNs = frame.TimeStamp.Value
            };
        }

        /**
         * Both methods return a byte array for consistency
         */
        public byte[] GetBuffer()
        {
            return buffer;
        }

        public byte[] GetMetadata()
        {
            string metadataJson = JsonUtility.ToJson(metadata);

            return Encoding.UTF8.GetBytes(metadataJson);
        }
    }
}