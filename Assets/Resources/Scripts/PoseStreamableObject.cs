using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;

public class PoseStreamableObject : StreamableObject
{
    /**
     * This class is responsible for providing the pose data that will be streamed by the StreamManager.
     * It is needed because after the server computes the object pose, the frame of reference is still the one of the camera.
     * In order to anchor the object to a real world reference frame, what this class should do is provide an API
     * that streams the relative position of the device with respect to a point in the real world (realistically an aruco).
     * The marker would also be used in order to validate the data obtained with an external tracking device.
        *
        *   marker ----> device ----> object
        *      \                         ^
        *       \-----------------------/
        *
        * Connections used:
        * - marker -> device (PoseStreaming)
        * - device -> object (CameraStreaming + Computation)
        * - marker -> object (Used for validation)
        * 
     *
     * There could also be the need to find the offset camera -> device. That is because when generating
     * the point cloud the algorithm takes one of the two cameras (by convention the left one), and uses it as 
     * the origin to place the points. It could be different from the origin that is used by Unity for the 
     * device, so further calibration might be needed
     *
     * Aruco API: https://developer-docs.magicleap.cloud/docs/guides/features/marker-tracking/
     */
    public override void Initialize()
    {
        
    }

    public override byte[] GetData(uint timeoutMilliseconds)
    {
        return new byte[0];
    }
}