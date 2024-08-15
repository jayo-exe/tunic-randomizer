using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TunicRandomizer
{
    public static class FoxCamHandler
    {
        private static GameObject foxCamObject;
        private static Camera foxCam;
        private static AmplifyColorEffect foxCamAmp;
        private static Kino.Bloom foxCamBloom;
        private static RenderTexture foxCamRT = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGBHalf);
        private static int foxCamCullingMask = 0;

        private static GameObject RenderTextureDisplay;
        private static GameObject rtdBack;

        public static void SetupCameraView()
        {
            if (GameObject.Find("_GameGUI(Clone)/AreaLabels/FoxCam Viewport") != null)
            {
                ShowCameraView();
                return;
            }

            TunicLogger.LogInfo("Attaching RTD");
            rtdBack = new GameObject("FoxCam Viewport");
            rtdBack.transform.SetParent(GameObject.Find("_GameGUI(Clone)/AreaLabels").transform);
            RectTransform bgRect = rtdBack.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(1, 0);
            bgRect.anchorMax = new Vector2(1, 0);
            bgRect.pivot = new Vector2(1, 0);
            bgRect.sizeDelta = new Vector2(400, 225);
            RawImage bg = rtdBack.AddComponent<RawImage>();
            bg.color = UnityEngine.Color.white;

            RenderTextureDisplay = new GameObject("RTD");
            RenderTextureDisplay.transform.SetParent(rtdBack.transform);
            RectTransform rectTransform = RenderTextureDisplay.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            RawImage rawImage = RenderTextureDisplay.AddComponent<RawImage>();
            rawImage.texture = foxCamRT;
        }

        public static void ShowCameraView()
        {
            rtdBack.SetActive(true);
        }

        public static void HideCameraView()
        {
            rtdBack.SetActive(false);
        }

        public static void ToggleCameraView()
        {
            rtdBack.SetActive(!rtdBack.active);
        }

        public static GameObject FindFox()
        {
            return GameObject.Find("_Fox(Clone)/Fox/root");
        }

        public static GameObject FindFoxCamera()
        {
            return GameObject.Find("_Fox(Clone)/Fox/root/pelvis/chest/head/foxCam");
        }

        public static GameObject FindOriginalCamera()
        {
            return GameObject.Find("Camera 2  - Post Processing");
        }

        public static GameObject FindBackgroundCamera()
        {
            return GameObject.Find("Camera 0 - Background");
        }

        public static GameObject FindBlitCamera()
        {
            return GameObject.Find("Camera 4 - Blit");
        }

        public static void AttachFoxCam()
        {
            if (GameObject.Find("_Fox(Clone)/Fox/root/pelvis/chest/head") == null)
            {
                TunicLogger.LogInfo("wer dat fox go?");
                return;
            }
            if (Camera.main == null) return;

            if (foxCamCullingMask == 0)
            {
                foxCamCullingMask = Camera.main.cullingMask;
            }

            TunicLogger.LogInfo("Attaching Camera");
            foxCamObject = new GameObject("foxCam");
            foxCam = foxCamObject.AddComponent<Camera>();
            foxCam.transform.parent = GameObject.Find("_Fox(Clone)/Fox/root/pelvis/chest/head").transform;
            foxCam.transform.localPosition = new Vector3(0f, 0.6f, 0.29f);
            foxCam.nearClipPlane = 0.01f;
            foxCam.fieldOfView = 70f;
            foxCam.transform.localRotation = Quaternion.identity;

            foxCam.allowHDR = true;
            foxCam.targetTexture = foxCamRT;
            foxCam.cullingMask = foxCamCullingMask;
            
            foxCamBloom = foxCamObject.AddComponent<Kino.Bloom>();
            foxCamBloom.antiFlicker = false;
            foxCamBloom._shader = Shader.Find("Hidden/Kino/Bloom");
            
            foxCamAmp = foxCamObject.AddComponent<AmplifyColorEffect>();
        }

        public static void UpdatePostProcess()
        {
            if (foxCamObject == null) return;

            GameObject origCamObject = FindOriginalCamera();
            if (origCamObject == null) return;

            AmplifyColorEffect origAmp = origCamObject.GetComponent<AmplifyColorEffect>();

            if(origAmp.LutTexture == null)
            {
                foxCamAmp.LutTexture = null;
            } else if(foxCamAmp.LutTexture == null || foxCamAmp.LutTexture.name != origAmp.LutTexture.name) {
                foxCamAmp.LutTexture = origAmp.LutTexture;
            }

            if (origAmp.LutBlendTexture == null)
            {
                foxCamAmp.LutBlendTexture = null;
            }
            else if (foxCamAmp.LutBlendTexture == null || foxCamAmp.LutBlendTexture.name != origAmp.LutBlendTexture.name)
            {
                foxCamAmp.LutBlendTexture = origAmp.LutBlendTexture;
            }

            foxCamAmp.BlendAmount = origAmp.BlendAmount;

            Kino.Bloom origBloom = origCamObject.GetComponent<Kino.Bloom>();
            foxCamBloom._intensity = origBloom._intensity;
            foxCamBloom._exposure = origBloom._exposure;
            foxCamBloom._radius = origBloom._radius;

        }

        //helper functions to manually tweak camera placement during dev
        public static void MoveFoxCamForward()
        {
            Vector3 pos = foxCam.transform.localPosition;
            foxCam.transform.localPosition = new Vector3(pos.x, pos.y, pos.z + 0.01f);
            LogCamPosition();
        }

        public static void MoveFoxCamBackward()
        {
            Vector3 pos = foxCam.transform.localPosition;
            foxCam.transform.localPosition = new Vector3(pos.x, pos.y, pos.z - 0.01f);
            LogCamPosition();
        }

        public static void MoveFoxCamUp()
        {
            Vector3 pos = foxCam.transform.localPosition;
            foxCam.transform.localPosition = new Vector3(pos.x, pos.y + 0.01f, pos.z);
            LogCamPosition();
        }

        public static void MoveFoxCamDown()
        {
            Vector3 pos = foxCam.transform.localPosition;
            foxCam.transform.localPosition = new Vector3(pos.x, pos.y - 0.01f, pos.z);
            LogCamPosition();
        }

        public static void LogCamPosition()
        {
            TunicLogger.LogInfo($"FoxCam is at {foxCam.transform.localPosition.ToString("F4")}");
        }
    }
}
