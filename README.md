# MagicLeapClient

Unity application for the Magic Leap 2 that captures the three world cameras and streams them
to a host machine for stereo reconstruction and model pose estimation.

It is the device side of a two-repository project; the host side is
[magicleapserver](https://github.com/fdeiasio/magicleapserver).

The application does no image processing. It reads frames from `MLWorldCamera`, packs them into
the `WCAM` container described below, and sends them over TCP. Everything else — rectification,
disparity, point cloud, ICP — happens on the host.

## Requirements

| | |
| --- | --- |
| Unity | **6000.0.72f1** (Unity 6.0 LTS) |
| Java | 17 |
| Platform | Android, **x86_64**, IL2CPP, min API 29 |
| SDK | MagicLeap Unity SDK 2.6.0 (resolved from `Packages/manifest.json`) |
| Device | Magic Leap 2, connected over USB with `adb` |

The Unity version is not a preference. Newer releases dropped both the x86_64 Android target and
Magic Leap 2 support, so the project will not build on them. Match `ProjectSettings/ProjectVersion.txt`
exactly if you can.

## Opening the project

1. Install Unity 6000.0.72f1 through Unity Hub, with the Android Build Support module.
2. In Unity Hub, **Add project from disk** and select this directory.
3. Open it and let the package manager resolve `com.magicleap.unitysdk` and the XR packages.
4. `Assets/MyScene.unity` is already the only enabled scene in the build settings. Open it.
5. Switch the platform to Android (File > Build Profiles) if it is not already active.

## Building and deploying

```bash
adb devices                     # confirm the headset is visible
adb install -r path/to/build.apk
adb reverse tcp:9000 tcp:9000   # device loopback -> host, required before connecting
```

Then start the host server and launch the app on the device.

## Using the app

The scene exposes four buttons, driven by `ButtonsManager`. They are enabled in sequence, so
only the actions that make sense in the current state are clickable.

| Button | Action |
| --- | --- |
| **Connect** | Opens the TCP connection to `127.0.0.1:9000`. Turns green on success, "Try Again" on failure. |
| **Stream Cameras** | Starts sending camera frames at 30 fps. Toggles to **Stop Stream**. |
| **Stream Pose** | Present but disabled. The pose path was designed and not built — see Limitations. |
| **Disconnect** | Stops streaming and closes the connection. |

## Architecture

```
ButtonsManager      MonoBehaviour. Owns the UI, sequences the buttons, calls ApplicationLogic.
ApplicationLogic    Orchestration and configuration. One StreamManager per data source.
StreamManager       Paces the stream, requests the Android permission, owns the send loop.
ConnectionManager   TCP socket plus an async send locked by a SemaphoreSlim.
StreamableObject    Abstract data source: Initialize() and GetData(timeout).
  CameraStreamableObject   MLWorldCamera -> WCAM packet bytes.
  PoseStreamableObject     Placeholder for the device pose / ArUco path.
```

Two details are worth knowing before editing the camera path:

- **Frames must be deep copied.** The buffer behind `frame.FrameBuffer.Data` is only valid while
  the `GetLatestWorldCameraData` scope is alive, so `CameraFrame` copies it with `Marshal.Copy`.
  Holding the pointer produces null references instead of an error.
- **A failed update is not fatal.** If a camera does not deliver in time the previously cached
  frame is resent, so the host always receives a complete three-frame packet.

## Wire protocol

The device sends over TCP to port 9000. One packet per batch, all integers little-endian:

| Field | Size | Value |
| --- | --- | --- |
| Magic header | 4 B | ASCII `WCAM` |
| Frame block 1..3 | — | One per camera, in dictionary order |
| ↳ metadata length | 4 B int32 | |
| ↳ metadata | n B | UTF-8 JSON: `cameraId`, `frameType`, `frameNumber`, `width`, `height`, `stride`, `bytesPerPixel`, `timestampNs` |
| ↳ image length | 4 B int32 | |
| ↳ image | n B | Uncompressed 8-bit grayscale, `width * height` bytes |

A camera with no cached frame yet is written as a zero-length metadata block followed by a
zero-length image. The host matches frames by `cameraId`, so block order is not significant.

## Troubleshooting

**"Try Again" on Connect, or the app looks connected but nothing happens.**
Run `adb reverse tcp:9000 tcp:9000`. `adb` resets the forwarding when the device reconnects, so
it may need to be re-run. Note that `adb` accepts the connection before checking whether anything
is listening, so a successful connect does not prove the server is running: if the app connects
but never streams, start the server, then disconnect and reconnect.

**Reading logs.**

```bash
adb logcat -s Unity
```

## Limitations

- **Pose streaming is not implemented.** `PoseStreamableObject` returns an empty array and the
  third button stays disabled. The intent was to stream the device pose relative to an ArUco
  marker so the object pose could be anchored to a world frame and validated against an external
  tracker, and to calibrate the camera-to-device offset. `adb reverse` does not forward UDP, so
  carrying it over the existing cable link would require a TCP handshake.
- **Frames are uncompressed.** Three 1016x1016 grayscale frames is about 3.1 MB per batch; at
  30 fps that is roughly 93 MB/s, which is why the link is a USB cable rather than WiFi.
- **30 fps is the ceiling**, set by the host pipeline, not the device.
- `Socket.Connected` is used as the liveness check in `ConnectionManager`. It reflects the last
  I/O result rather than the current state, so a half-open connection can read as connected.

## License

MIT — see [LICENSE](LICENSE). The MagicLeap SDK and the embedded `Packages/com.magicleap.setuptool`
package are third-party and carry their own licenses.
