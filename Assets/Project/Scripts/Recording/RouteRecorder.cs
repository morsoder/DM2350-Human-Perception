using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace DM2350.Recording
{
    [Serializable] public sealed class RoutePoint
    {
        public double seconds;
        public int segment;
        public Vector3 position;
    }

    [Serializable] public sealed class RouteEvent
    {
        public double seconds;
        public string type;
        public string value;
    }

    [Serializable] public sealed class RouteSession
    {
        public int formatVersion = 1;
        public string sessionId, participantId, groupId, scene, unityVersion, platform, startedUtc, endedUtc;
        public string status, stopReason, imageStatus, imageError, initialLighting, initialControlMode;
        public float sampleInterval;
        public double durationSeconds;
        public int sampleCount, imageWidth, imageHeight;
        public Bounds mapBounds;
        public string coordinateSystem = "Unity metres; image right = +X; image up = +Z; CSV positions are world coordinates";
        public Color ambientSky, ambientEquator, ambientGround, fogColor;
        public bool fog;
        public float fogDensity, skyExposure, moonIntensity;
        public List<RouteEvent> events = new List<RouteEvent>();
    }

    /// <summary>Device-independent recording, incremental disk writes and map export.</summary>
    [DisallowMultipleComponent]
    public sealed class RouteRecorder : MonoBehaviour
    {
        [Header("Anonymous session labels")]
        public string participantId = "P001";
        public string groupId = "G1";
        [Header("Sources")]
        public Transform player;
        [HideInInspector] public Shader mapShader;
        [Header("Recording")]
        [Min(0.02f)] public float sampleInterval = 0.1f;
        [Min(0.1f)] public float flushInterval = 1f;
        [Min(0.1f)] public float discontinuityDistance = 5f;
        [Tooltip("Blank uses Documents/DM2350_Results on Windows; the application data directory on other platforms.")]
        public string outputDirectory;
        [Header("Map")]
        public Bounds mapBounds = new Bounds(new Vector3(100, 10, 0), new Vector3(240, 80, 240));
        [Range(512, 4096)] public int mapResolution = 1536;
        public bool showDesktopStatus = true;

        private readonly List<RoutePoint> points = new List<RoutePoint>();
        private RouteSession session;
        private FileStream stream;
        private StreamWriter writer;
        private double started, nextSample, nextFlush;
        private int segment;
        private string previousLighting, previousMode;
        private bool previousPaused;
        private bool saving;
        private bool previousRunInBackground;
        public bool IsRecording { get; private set; }
        public string Status { get; private set; } = "Ready - G / Xbox X / Quest left X";
        public string LastSessionDirectory { get; private set; }
        public RouteSession LastSession => session;
        public IReadOnlyList<RoutePoint> Points => points;

        private void OnEnable()
        {
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
        }

        public string ExportRoot
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(outputDirectory)) return Path.GetFullPath(outputDirectory);
                var documents = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : "";
                return Path.Combine(string.IsNullOrEmpty(documents) ? Application.persistentDataPath : documents, "DM2350_Results");
            }
        }

        public void ToggleRecording()
        {
            if (saving) return;
            if (IsRecording) StopRecording(); else StartRecording();
        }

        public bool StartRecording()
        {
            if (IsRecording || saving) return false;
            try
            {
                if (player == null) throw new InvalidOperationException("Player reference is missing.");
                var id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                LastSessionDirectory = Path.Combine(ExportRoot, SafeLabel(participantId), id);
                Directory.CreateDirectory(LastSessionDirectory);
                stream = new FileStream(Path.Combine(LastSessionDirectory, "trajectory.csv.partial"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, true);
                writer.WriteLine("time_seconds,utc_time,segment,x,y,z,head_x,head_y,head_z,head_forward_x,head_forward_y,head_forward_z,lighting_condition,control_mode,paused,head_source,head_tracking_valid");
                started = Time.realtimeSinceStartupAsDouble;
                nextSample = started;
                nextFlush = started + Mathf.Max(0.1f, flushInterval);
                segment = 0;
                points.Clear();
                session = new RouteSession
                {
                    sessionId = id, participantId = participantId, groupId = groupId,
                    scene = gameObject.scene.path, unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                    startedUtc = DateTime.UtcNow.ToString("O"), sampleInterval = Mathf.Max(0.02f, sampleInterval),
                    mapBounds = mapBounds, status = "recording", imageStatus = "pending",
                    ambientSky = RenderSettings.ambientSkyColor, ambientEquator = RenderSettings.ambientEquatorColor,
                    ambientGround = RenderSettings.ambientGroundColor, fog = RenderSettings.fog,
                    fogColor = RenderSettings.fogColor, fogDensity = RenderSettings.fogDensity,
                    skyExposure = RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure") ? RenderSettings.skybox.GetFloat("_Exposure") : 0f
                };
                var sun = RenderSettings.sun;
                session.moonIntensity = sun != null ? sun.intensity : 0f;
                previousLighting = Lighting();
                previousMode = Mode();
                previousPaused = Paused();
                session.initialLighting = previousLighting;
                session.initialControlMode = previousMode;
                IsRecording = true;
                AddEvent("start", previousLighting);
                Sample();
                Flush();
                Status = "Recording";
                Debug.Log("Route recording started: " + LastSessionDirectory, this);
                return true;
            }
            catch (Exception e)
            {
                IsRecording = false;
                CloseWriter();
                Status = "Cannot start recording: " + e.Message;
                Debug.LogWarning(Status, this);
                return false;
            }
        }

        private void LateUpdate()
        {
            if (!IsRecording) return;
            try
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (now >= nextSample) { Sample(); nextSample = now + session.sampleInterval; }
                if (now >= nextFlush) { Flush(); nextFlush = now + Mathf.Max(0.1f, flushInterval); }
            }
            catch (Exception e)
            {
                IsRecording = false;
                CloseWriter();
                Status = "Recording interrupted; partial data retained: " + e.Message;
                Debug.LogWarning(Status, this);
            }
        }

        private void Sample()
        {
            var elapsed = Time.realtimeSinceStartupAsDouble - started;
            var position = player.position;
            if (points.Count > 0 && Vector3.Distance(position, points[points.Count - 1].position) > discontinuityDistance)
            { segment++; AddEvent("new_segment", "Position discontinuity or reset"); }
            var lighting = Lighting();
            var mode = Mode();
            var paused = Paused();
            if (lighting != previousLighting) { AddEvent("lighting_change", lighting); previousLighting = lighting; }
            if (mode != previousMode) { segment++; AddEvent("control_mode_change", mode); previousMode = mode; }
            if (paused != previousPaused) { AddEvent(paused ? "pause" : "resume", ""); previousPaused = paused; }
            var head = Head();
            var hp = head != null ? head.position : position;
            var forward = head != null ? head.forward : player.forward;
            var vr = mode == ExperimentControlMode.VrController.ToString() || mode == ExperimentControlMode.VrPhysicalOffset.ToString();
            var tracking = "not_applicable";
            if (vr)
            {
                var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
                tracking = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked) && tracked ? "1" : "0";
            }
            var point = new RoutePoint { seconds = elapsed, segment = segment, position = position };
            points.Add(point);
            writer.WriteLine(string.Join(",", D(elapsed), DateTime.UtcNow.ToString("O"), segment.ToString(CultureInfo.InvariantCulture),
                F(position.x), F(position.y), F(position.z), F(hp.x), F(hp.y), F(hp.z), F(forward.x), F(forward.y), F(forward.z), lighting, mode, paused ? "1" : "0", vr ? "xr_head" : "desktop_camera", tracking));
            session.sampleCount = points.Count;
            session.durationSeconds = elapsed;
        }

        public bool StopRecording(string reason = "manual", bool exportImage = true)
        {
            if (!IsRecording || saving) return false;
            saving = true;
            try
            {
                Sample();
                AddEvent("stop", reason);
                IsRecording = false;
                session.endedUtc = DateTime.UtcNow.ToString("O");
                session.stopReason = reason;
                session.status = reason == "manual" ? "completed" : "interrupted";
                Flush();
                CloseWriter();
                var csv = Path.Combine(LastSessionDirectory, "trajectory.csv");
                File.Move(csv + ".partial", csv);
                session.imageStatus = exportImage ? "generating" : "not_generated_interrupted_session";
                WriteMetadata();
                if (exportImage)
                {
                    try
                    {
                        Status = "Saving map...";
                        RouteMapExporter.Export(mapShader, gameObject.scene, player, mapBounds, mapResolution, points, Path.Combine(LastSessionDirectory, "route.png"));
                        session.imageStatus = "saved";
                        session.imageWidth = session.imageHeight = Mathf.Clamp(mapResolution, 512, 4096);
                    }
                    catch (Exception imageError)
                    {
                        session.imageStatus = "failed";
                        session.imageError = imageError.Message;
                        Debug.LogWarning("Route data saved; map export failed: " + imageError.Message, this);
                    }
                }
                WriteMetadata();
                if (!File.Exists(csv) || new FileInfo(csv).Length == 0) throw new IOException("Exported CSV could not be verified.");
                Status = session.imageStatus == "saved" ? "Saved: " + LastSessionDirectory : "Data saved; map " + session.imageStatus + ": " + LastSessionDirectory;
                Debug.Log(Status, this);
                return true;
            }
            catch (Exception e)
            {
                IsRecording = false;
                CloseWriter();
                Status = "Save incomplete; retained files: " + LastSessionDirectory + " - " + e.Message;
                Debug.LogWarning(Status, this);
                return false;
            }
            finally { saving = false; }
        }

        private void AddEvent(string type, string value) => session.events.Add(new RouteEvent { seconds = Time.realtimeSinceStartupAsDouble - started, type = type, value = value });
        private void Flush() { writer.Flush(); stream.Flush(true); WriteMetadata(); }
        private void WriteMetadata() => File.WriteAllText(Path.Combine(LastSessionDirectory, "session.json"), JsonUtility.ToJson(session, true), new UTF8Encoding(false));
        private void CloseWriter()
        {
            var oldWriter = writer; var oldStream = stream;
            writer = null; stream = null;
            try { oldWriter?.Dispose(); } catch (Exception e) { Debug.LogWarning("Closing route CSV: " + e.Message, this); }
            try { oldStream?.Dispose(); } catch (Exception e) { Debug.LogWarning("Closing route file: " + e.Message, this); }
        }
        private string Lighting() { var lights = FindAnyObjectByType<StreetLightingConditions>(); return lights != null ? lights.Condition.ToString() : "unspecified"; }
        private string Mode() { var motor = player.GetComponent<ExperimentLocomotion>(); return motor != null ? motor.ControlMode.ToString() : "unspecified"; }
        private bool Paused() { var motor = player.GetComponent<ExperimentLocomotion>(); return motor != null && motor.IsPaused; }
        private Transform Head()
        {
            var motor = player.GetComponent<ExperimentLocomotion>();
            var vr = motor != null && (motor.ControlMode == ExperimentControlMode.VrController || motor.ControlMode == ExperimentControlMode.VrPhysicalOffset);
            if (vr) { var rig = player.GetComponent<OVRCameraRig>(); if (rig != null) return rig.centerEyeAnchor; }
            return player.Find("DesktopCameraRig");
        }
        private static string F(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        private static string D(double n) => n.ToString("F6", CultureInfo.InvariantCulture);
        private static string SafeLabel(string value)
        {
            var result = new StringBuilder();
            foreach (var c in value ?? "") if (char.IsLetterOrDigit(c) || c == '-' || c == '_') result.Append(c);
            return result.Length == 0 ? "anonymous" : result.ToString();
        }
        private void OnApplicationQuit() { if (IsRecording) StopRecording("application_quit", false); }
        private void OnDisable()
        {
            if (IsRecording) StopRecording("component_disabled", false);
            Application.runInBackground = previousRunInBackground;
        }

        private void OnGUI()
        {
            if (!showDesktopStatus) return;
            var elapsed = IsRecording ? Time.realtimeSinceStartupAsDouble - started : 0;
            GUI.Box(new Rect(16, 136, 600, 82), "Route recorder - G / Xbox X / Quest left X\n" + (IsRecording ? "RECORDING  " + elapsed.ToString("F1") + " s  |  " + points.Count + " samples" : Status));
            if (!IsRecording && !string.IsNullOrEmpty(LastSessionDirectory) && GUI.Button(new Rect(24, 221, 165, 28), "Open saved folder"))
                Application.OpenURL(new Uri(LastSessionDirectory).AbsoluteUri);
        }
    }
}
