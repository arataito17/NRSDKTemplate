/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using NRKernal;
using NRKernal.Record;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking; // 追加
using Newtonsoft.Json;

namespace NRKernal.NRExamples
{
#if UNITY_ANDROID && !UNITY_EDITOR
    using GalleryDataProvider = NativeGalleryDataProvider;
#else
    using GalleryDataProvider = MockGalleryDataProvider;
#endif

    /// <summary> A photo capture example. </summary>
    [HelpURL("https://developer.xreal.com/develop/unity/video-capture")]
    public class PhotoCaptureExample : MonoBehaviour
    {
        /// <summary> The photo capture object. </summary>
        private NRPhotoCapture m_PhotoCaptureObject;
        /// <summary> The camera resolution. </summary>
        private Resolution m_CameraResolution;
        private bool isOnPhotoProcess = false;
        GalleryDataProvider galleryDataTool;

        // --- 追加: ピンチ継続時間管理用 ---
        private float pinchDuration = 0f;
        public float pinchThresholdSeconds = 2.0f;

        void Update()
        {
            var handState = NRInput.Hands.GetHandState(HandEnum.RightHand);
            if (handState.isPinching)
            {
                pinchDuration += Time.deltaTime;
                if (pinchDuration >= pinchThresholdSeconds)
                {
                    TakeAPhoto();
                    pinchDuration = 0f; // 連続撮影防止
                }
            }
            else
            {
                pinchDuration = 0f;
            }
        }

        /// <summary> Use this for initialization. </summary>
        void Create(Action<NRPhotoCapture> onCreated)
        {
            if (m_PhotoCaptureObject != null)
            {
                NRDebugger.Info("The NRPhotoCapture has already been created.");
                return;
            }

            // Create a PhotoCapture object
            NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
            {
                m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

                if (captureObject == null)
                {
                    NRDebugger.Error("Can not get a captureObject.");
                    return;
                }

                m_PhotoCaptureObject = captureObject;

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
                cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.PNG;
                cameraParameters.frameRate = NativeConstants.RECORD_FPS_DEFAULT;
                cameraParameters.blendMode = BlendMode.RGBOnly;

                // Activate the camera
                m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
                {
                    NRDebugger.Info("Start PhotoMode Async");
                    if (result.success)
                    {
                        onCreated?.Invoke(m_PhotoCaptureObject);
                    }
                    else
                    {
                        isOnPhotoProcess = false;
                        this.Close();
                        NRDebugger.Error("Start PhotoMode faild." + result.resultType);
                    }
                }, true);
            });
        }

        /// <summary> Take a photo. </summary>
        void TakeAPhoto()
        {
            if (isOnPhotoProcess)
            {
                NRDebugger.Warning("Currently in the process of taking pictures, Can not take photo .");
                return;
            }

            isOnPhotoProcess = true;
            if (m_PhotoCaptureObject == null)
            {
                this.Create((capture) =>
                {
                    capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
            }
            else
            {
                m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
        }

        /// <summary> Executes the 'captured photo memory' action. </summary>
        /// <param name="result">            The result.</param>
        /// <param name="photoCaptureFrame"> The photo capture frame.</param>
         void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            NRDebugger.Info("PhotoCaptureExample: OnCapturedPhotoToMemory, Resolution: {0}x{1}", m_CameraResolution.width, m_CameraResolution.height);

            var targetTexture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height);

            // ここでTexture2Dのサイズをログ出力
            Debug.Log($"[Check] Texture2D size: {targetTexture.width} x {targetTexture.height}");

            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            // Create a gameobject that we can apply our texture to
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
            quadRenderer.material = new Material(Resources.Load<Shader>("Record/Shaders/CaptureScreen"));

            var headTran = NRSessionManager.Instance.NRHMDPoseTracker.centerAnchor;
            quad.name = "picture";
            quad.transform.localPosition = headTran.position + headTran.forward * 3f;
            quad.transform.forward = headTran.forward;
            quad.transform.localScale = new Vector3(1.6f, 0.9f, 0);
            quadRenderer.material.SetTexture("_MainTex", targetTexture);
            SaveTextureAsPNG(photoCaptureFrame);

            // 画像データをPCへ送信
            if (photoCaptureFrame.TextureData != null)
            {
                StartCoroutine(SendPhotoToPC(photoCaptureFrame.TextureData));
            }

            SaveTextureToGallery(photoCaptureFrame);
            // Release camera resource after capture the photo.
            this.Close();
        }


        // --- 追加: 画像データをPCへ送信するコルーチン ---
        [System.Serializable]
        public class ObjectInfo
        {
            public string description;
            public float[] world_position;
        }

        [System.Serializable]
        public class Intrinsics
        {
            public float fx;
            public float fy;
            public float cx;
            public float cy;
        }

        IEnumerator SendPhotoToPC(byte[] imageData)
        {
            Debug.Log("Sending photo to PC...");
            string url = "http://192.168.1.21:5001/upload";

            // カメラ内部行列を取得
            Debug.Log("NRFrame.GetDeviceIntrinsicMatrixを呼び出し");
            NativeMat3f mat = NRFrame.GetDeviceIntrinsicMatrix(NativeDevice.RGB_CAMERA);
            float fx = mat[0, 0];
            float fy = mat[1, 1];
            float cx = mat[0, 2];
            float cy = mat[1, 2];

            // JSON化
            var intrinsics = new Intrinsics { fx = fx, fy = fy, cx = cx, cy = cy };
            string intrinsicsJson = JsonUtility.ToJson(intrinsics);
            Debug.Log("送信intrinsics: " + intrinsicsJson); // デバッグ用

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageData, "photo.png", "image/png");
            form.AddField("intrinsics", intrinsicsJson);

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                Debug.Log("PCへの画像送信開始");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("PCへの画像送信成功");

                    // --- ここから追加 ---
                    string json = www.downloadHandler.text;
                    var objDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ObjectInfo>>(json);

                    foreach (var kv in objDict)
                    {
                        // カメラ座標系での位置
                        Vector3 camPos = new Vector3(
                            kv.Value.world_position[0],
                            kv.Value.world_position[1],
                            kv.Value.world_position[2]
                        );
                        // カメラ座標系→ワールド座標系
                        Vector3 worldPos = Camera.main.transform.TransformPoint(camPos);

                        // 写真Quadと同じようにCubeを生成
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.position = worldPos;
                        cube.transform.localScale = Vector3.one * 0.1f; // サイズ調整
                        cube.name = kv.Key + "_object";

                        // 必要なら説明をデバッグ表示
                        Debug.Log($"{kv.Key}: {kv.Value.description} @ {worldPos}");
                    }
                    // --- ここまで追加 ---
                }
                else
                {
                    Debug.LogError("PCへの画像送信失敗: " + www.error);
                }
            }
        }

        IEnumerator SendPhotoToPCAndClose(byte[] imageData)
        {
            yield return StartCoroutine(SendPhotoToPC(imageData));
            this.Close(); // 送信完了後にClose
        }

        void SaveTextureAsPNG(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
                return;
            try
            {
                string filename = string.Format("Xreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                string path = string.Format("{0}/XrealShots", Application.persistentDataPath);
                string filePath = string.Format("{0}/{1}", path, filename);

                byte[] _bytes = photoCaptureFrame.TextureData;
                NRDebugger.Info("Photo capture: {0}Kb was saved to [{1}]",  _bytes.Length / 1024, filePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.WriteAllBytes(string.Format("{0}/{1}", path, filename), _bytes);

            }
            catch (Exception e)
            {
                NRDebugger.Error("Save picture faild!");
                throw e;
            }
        }

        /// <summary> Closes this object. </summary>
        void Close()
        {
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Error("The NRPhotoCapture has not been created.");
                return;
            }
            // Deactivate our camera
            m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        /// <summary> Executes the 'stopped photo mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
            isOnPhotoProcess = false;
        }

        /// <summary> Executes the 'destroy' action. </summary>
        void OnDestroy()
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
        }

        public void SaveTextureToGallery(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
                return;
            try
            {
                string filename = string.Format("Xreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                byte[] _bytes = photoCaptureFrame.TextureData;
                NRDebugger.Info(_bytes.Length / 1024 + "Kb was saved as: " + filename);
                if (galleryDataTool == null)
                {
                    galleryDataTool = new GalleryDataProvider();
                }

                galleryDataTool.InsertImage(_bytes, filename, "Screenshots");
            }
            catch (Exception e)
            {
                NRDebugger.Error("[TakePicture] Save picture faild!");
                throw e;
            }
        }
    }
}