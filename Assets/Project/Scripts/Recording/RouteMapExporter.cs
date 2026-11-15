using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DM2350.Recording
{
    public static class RouteMapExporter
    {
        // The legacy shader argument is retained so existing scene references remain compatible.
        public static void Export(Shader shader, Scene scene, Transform player, Bounds bounds, int resolution, IReadOnlyList<RoutePoint> points, string path)
        {
            if (bounds.size.x < 1 || bounds.size.z < 1) throw new InvalidOperationException("Map bounds must cover at least one metre.");
            int size = Mathf.Clamp(resolution, 512, 4096);
            var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var image = new Texture2D(size, size, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Route export camera") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var hidden = new List<Renderer>();
            try
            {
                if (player != null)
                    foreach (var renderer in player.GetComponentsInChildren<Renderer>())
                        if (renderer.enabled) { hidden.Add(renderer); renderer.enabled = false; }
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.z;
                camera.aspect = bounds.size.x / bounds.size.z;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                camera.useOcclusionCulling = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.005f, 0.007f, 0.012f);
                float top = bounds.max.y + 5f;
                camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, top, bounds.center.z),
                    Quaternion.LookRotation(Vector3.down, Vector3.forward));
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = Mathf.Max(100f, top - bounds.min.y + 10f);
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderType = CameraRenderType.Base;
                data.allowXRRendering = false;
                data.renderShadows = true;
                data.renderPostProcessing = true;
                data.volumeLayerMask = ~0;
                data.volumeTrigger = player != null ? player : camera.transform;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("The active pipeline does not support the route camera render request.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                int width = Mathf.Max(2, size / 450);
                for (int i = 1; i < points.Count; i++)
                    if (points[i].segment == points[i - 1].segment)
                        Line(pixels, size, Pixel(points[i - 1].position, bounds, size), Pixel(points[i].position, bounds, size), new Color32(255, 207, 40, 255), width);
                if (points.Count > 0)
                {
                    Disk(pixels, size, Pixel(points[0].position, bounds, size), new Color32(40, 225, 120, 255), width * 3);
                    Disk(pixels, size, Pixel(points[points.Count - 1].position, bounds, size), new Color32(255, 75, 75, 255), width * 3);
                }
                Line(pixels, size, new Vector2Int(28, size - 65), new Vector2Int(28, size - 20), new Color32(240, 240, 240, 255), width);
                Line(pixels, size, new Vector2Int(28, size - 20), new Vector2Int(17, size - 36), new Color32(240, 240, 240, 255), width);
                Line(pixels, size, new Vector2Int(28, size - 20), new Vector2Int(39, size - 36), new Color32(240, 240, 240, 255), width);
                image.SetPixels32(pixels);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                if (!File.Exists(path) || new FileInfo(path).Length < 100) throw new IOException("PNG export could not be verified.");
            }
            finally
            {
                foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        public static Vector2Int Pixel(Vector3 p, Bounds b, int size) => new Vector2Int(Mathf.RoundToInt((p.x-b.min.x)/b.size.x*(size-1)),Mathf.RoundToInt((p.z-b.min.z)/b.size.z*(size-1)));
        private static void Disk(Color32[] pixels,int size,Vector2Int center,Color32 color,int radius)
        {
            for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++)
            {
                int px=center.x+x,py=center.y+y;
                if(x*x+y*y<=radius*radius && px>=0 && px<size && py>=0 && py<size)pixels[py*size+px]=color;
            }
        }
        private static void Line(Color32[] pixels,int size,Vector2Int a,Vector2Int b,Color32 color,int radius)
        {
            // Clip lines to image bounds before stepping; discontinuities are handled by segment IDs.
            float x0=a.x,y0=a.y,dx=b.x-a.x,dy=b.y-a.y,t0=0,t1=1;
            if(!Clip(-dx,x0,ref t0,ref t1)||!Clip(dx,size-1-x0,ref t0,ref t1)||!Clip(-dy,y0,ref t0,ref t1)||!Clip(dy,size-1-y0,ref t0,ref t1))return;
            int steps=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx*(t1-t0)),Mathf.Abs(dy*(t1-t0))));
            for(int i=0;i<=steps;i++) {float t=Mathf.Lerp(t0,t1,steps==0?0:(float)i/steps);Disk(pixels,size,new Vector2Int(Mathf.RoundToInt(x0+dx*t),Mathf.RoundToInt(y0+dy*t)),color,radius);}
        }
        private static bool Clip(float p,float q,ref float lo,ref float hi)
        {
            if(Mathf.Abs(p)<0.000001f)return q>=0;
            float t=q/p;
            if(p<0){if(t>hi)return false;lo=Mathf.Max(lo,t);}else{if(t<lo)return false;hi=Mathf.Min(hi,t);}return true;
        }
    }
}
