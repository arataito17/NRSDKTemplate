using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;
using System.IO;
using System.Linq;
using NRKernal.Record;
using UnityEngine.Networking;
using System.Text;

namespace NRKernal.NRExamples{
    #if UNITY_ANDROID && !UNITY_EDITOR
        using GalleryDataProvider = NativeGalleryDataProvider;
    #else
        using GalleryDataProvider = MockGalleryDataProvider;
    #endif
    public class HandGesturePhotoCapture : MonoBehaviour
    {
        private float victory_count;
        private bool takeflag;
        private bool isAnalyzing; // GPT分析中フラグを追加
        private NRPhotoCapture m_PhotoCaptureObject;
        private Resolution m_CameraResolution;
        GalleryDataProvider galleryDataTool;
        
        // OpenAI API設定
        [Header("OpenAI API 設定")]
        [SerializeField] private string openAIApiKey = "your-api-key-here";
        [SerializeField] private string gptPrompt = "この画像に何が写っていますか？簡潔に日本語で説明してください。";
        
        // TTS設定（Android用）
        private AndroidJavaObject tts;
        private AudioSource audioSource; // エミュレーター用の音声出力
        private bool useDebugTTS = false; // デバッグTTSモードのフラグ
        // Start is called before the first frame update
        void Start()
        {
            victory_count = 0f;
            takeflag = false;
            
            // AudioSourceコンポーネントを取得または追加
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.volume = 0.7f;
                NRDebugger.Info("AudioSourceコンポーネントを追加しました");
            }
            
            InitializeTTS();
            
            // 0.1秒ごとに元のUpdate処理を実行
            StartCoroutine(UpdateEvery01Seconds());
        }

        // 元のUpdateメソッドを削除またはコメントアウト
        /*
        void Update()
        {
            NRDebugger.Info("Update呼び出し");
            if (NRInput.Hands.GetHandState(HandEnum.RightHand).isVictory)
            {
                CapturePhoto();
            }
            
        }
        */

        // 0.1秒ごとに元のUpdate処理を実行
        private IEnumerator UpdateEvery01Seconds()
        {
            while (true)
            {
                // 0.1秒待機
                yield return new WaitForSeconds(0.1f);
                
                // 元のUpdate()の中身をそのまま実行
                NRDebugger.Info("呼び出し");
                if (NRInput.Hands.GetHandState(HandEnum.RightHand).isPinching)
                {
                    CapturePhoto();
                }
            }
        }
        private void CapturePhoto()
        {
            // 既に撮影中または分析中の場合は処理をスキップ
            if (takeflag || isAnalyzing)
            {
                NRDebugger.Info("撮影または分析が進行中のため、新しい撮影をスキップします");
                return;
            }
            takeflag = true;
            NRDebugger.Info("写真撮影を開始します");
            
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Info("PhotoCaptureオブジェクトを作成中...");
                this.Create((capture) =>
                {
                    if (capture != null)
                    {
                        NRDebugger.Info("PhotoCaptureオブジェクト作成完了。撮影開始...");
                        capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                    }
                    else
                    {
                        NRDebugger.Error("PhotoCaptureオブジェクトの作成に失敗しました");
                        takeflag = false;
                    }
                });
            }
            else
            {
                NRDebugger.Info("既存のPhotoCaptureオブジェクトで撮影開始...");
                m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
        }
        void Create(Action<NRPhotoCapture> onCreated)
        {
            if (m_PhotoCaptureObject != null)
            {
                NRDebugger.Warning("PhotoCaptureオブジェクトは既に存在します");
                return;
            }

            NRDebugger.Info("NRPhotoCapture.CreateAsyncを呼び出し中...");
            
            // Create a PhotoCapture object
            NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
            {
                if (captureObject == null)
                {
                    NRDebugger.Error("NRPhotoCaptureの作成に失敗しました");
                    onCreated?.Invoke(null);
                    return;
                }

                NRDebugger.Info("NRPhotoCaptureオブジェクト作成成功");
                
                m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                NRDebugger.Info("カメラ解像度: {0}x{1}", m_CameraResolution.width, m_CameraResolution.height);

                m_PhotoCaptureObject = captureObject;

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
                cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.PNG;
                cameraParameters.frameRate = NativeConstants.RECORD_FPS_DEFAULT;
                cameraParameters.blendMode = BlendMode.RGBOnly;

                NRDebugger.Info("PhotoModeを開始中...");
                
                // Activate the camera
                m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
                {
                    if (result.success)
                    {
                        NRDebugger.Info("PhotoMode開始成功");
                        onCreated?.Invoke(m_PhotoCaptureObject);
                    }
                    else
                    {
                        NRDebugger.Error("PhotoMode開始失敗: {0}", result.resultType);
                        this.Close();
                        onCreated?.Invoke(null);
                    }
                }, true);
            });
        }
        void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            if (!result.success)
            {
                NRDebugger.Error("写真撮影に失敗しました。");
                // 撮影失敗時にフラグをリセット
                takeflag = false;
                isAnalyzing = false;
                this.Close();
                return;
            }

            var targetTexture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height);
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
            
            // 写真を保存
            SaveTextureAsPNG(photoCaptureFrame);
            SaveTextureToGallery(photoCaptureFrame);
            
            // GPT分析開始フラグを設定
            isAnalyzing = true;
            NRDebugger.Info("GPT分析を開始します...");
            
            // GPTに画像を送信して分析
            StartCoroutine(SendImageToGPT(photoCaptureFrame.TextureData));
            
            NRDebugger.Info("写真撮影完了！ファイルとギャラリーに保存されました。");
            
            // Release camera resource after capture the photo.
            this.Close();
        }
        void SaveTextureAsPNG(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
            {
                NRDebugger.Warning("写真データが空です。保存できません。");
                return;
            }
            
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = string.Format("Xreal_Shot_{0}.png", timestamp);
                string path = string.Format("{0}/XrealShots", Application.persistentDataPath);
                string filePath = Path.Combine(path, filename);

                byte[] _bytes = photoCaptureFrame.TextureData;
                
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    NRDebugger.Info("保存ディレクトリを作成しました: {0}", path);
                }
                
                // ファイルを保存
                File.WriteAllBytes(filePath, _bytes);
                
                NRDebugger.Info("写真を保存しました: {0}KB -> [{1}]", _bytes.Length / 1024, filePath);
            }
            catch (Exception e)
            {
                NRDebugger.Error("写真の保存に失敗しました: {0}", e.Message);
                throw e;
            }
        }

        /// <summary> Closes this object. </summary>
        void Close()
        {
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Warning("PhotoCaptureオブジェクトが既にnullです");
                takeflag = false;
                return;
            }
            
            NRDebugger.Info("PhotoModeを停止中...");
            // Deactivate our camera
            m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        /// <summary> Executes the 'stopped photo mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                NRDebugger.Info("PhotoMode停止成功");
            }
            else
            {
                NRDebugger.Warning("PhotoMode停止時にエラーが発生: {0}", result.resultType);
            }
            
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
            takeflag = false;
            
            NRDebugger.Info("PhotoCaptureリソースを解放しました");
        }

        public void SaveTextureToGallery(PhotoCaptureFrame photoCaptureFrame)
        {
            if (photoCaptureFrame.TextureData == null)
            {
                NRDebugger.Warning("写真データが空です。ギャラリーに保存できません。");
                return;
            }
            
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = string.Format("Xreal_Shot_{0}.png", timestamp);
                byte[] _bytes = photoCaptureFrame.TextureData;
                
                NRDebugger.Info("ギャラリーに保存中: {0}KB - {1}", _bytes.Length / 1024, filename);
                
                if (galleryDataTool == null)
                {
                    galleryDataTool = new GalleryDataProvider();
                }

                galleryDataTool.InsertImage(_bytes, filename, "ARカメラ");
                NRDebugger.Info("ギャラリーへの保存が完了しました: {0}", filename);
            }
            catch (Exception e)
            {
                NRDebugger.Error("ギャラリーへの保存に失敗しました: {0}", e.Message);
                throw e;
            }
        }
        
        // TTS初期化（Android用）
        private void InitializeTTS()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new TTSInitListener());
                useDebugTTS = false;
                NRDebugger.Info("TTS初期化成功（実機）");
            }
            catch (Exception e)
            {
                NRDebugger.Error("TTS初期化失敗（実機）: {0}", e.Message);
                // 実機でTTSが失敗した場合、デバッグ用TTSにフォールバック
                useDebugTTS = true;
                NRDebugger.Info("デバッグ用TTSモードに切り替えます");
            }
            #else
            useDebugTTS = true;
            NRDebugger.Info("TTS初期化完了（エミュレーター/エディタ - デバッグモード）");
            #endif
        }
        
        // GPTに画像を送信
        private IEnumerator SendImageToGPT(byte[] imageData)
        {
            if (string.IsNullOrEmpty(openAIApiKey) || openAIApiKey == "your-api-key-here")
            {
                NRDebugger.Error("OpenAI APIキーが設定されていません");
                SpeakText("APIキーが設定されていません");
                
                // APIキーがない場合もフラグをリセット
                isAnalyzing = false;
                takeflag = false;
                yield break;
            }
            
            NRDebugger.Info("GPTに画像を送信中... サイズ: {0}KB", imageData.Length / 1024);
            SpeakText("画像を分析中です");
            
            // 画像をBase64エンコード
            string base64Image = Convert.ToBase64String(imageData);
            NRDebugger.Info("Base64エンコード完了: {0}文字", base64Image.Length);
            
            // OpenAI API リクエストボディを構築
            string jsonData = CreateGPTRequestJson(base64Image);
            NRDebugger.Info("リクエストJSON作成完了: {0}文字", jsonData.Length);
            
            UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);
            
            NRDebugger.Info("API リクエスト送信中...");
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                bool hasError = false;
                string gptResult = "";
                
                try
                {
                    string response = request.downloadHandler.text;
                    NRDebugger.Info("API 応答受信: {0}文字", response.Length);
                    
                    // 応答が有効なJSONかどうかをチェック
                    if (string.IsNullOrEmpty(response) || (!response.TrimStart().StartsWith("{") && !response.TrimStart().StartsWith("[")))
                    {
                        NRDebugger.Error("無効なJSON応答を受信しました: {0}", response);
                        gptResult = "無効な応答を受信しました";
                        hasError = true;
                    }
                    else
                    {
                        gptResult = ParseGPTResponse(response);
                        NRDebugger.Info("GPT分析結果: {0}", gptResult);
                    }
                }
                catch (Exception e)
                {
                    NRDebugger.Error("GPT応答の解析に失敗: {0}", e.Message);
                    gptResult = "画像の分析に失敗しました";
                    hasError = true;
                }
                
                // 結果を音声で読み上げ
                SpeakText(gptResult);
                
                // TTS完了を待機してからフラグをリセット
                yield return StartCoroutine(WaitForTTSCompletion());
            }
            else
            {
                string errorMessage = request.downloadHandler != null ? request.downloadHandler.text : "不明なエラー";
                NRDebugger.Error("GPT APIエラー: {0}", request.error);
                NRDebugger.Error("HTTPステータス: {0}", request.responseCode);
                NRDebugger.Error("エラー詳細: {0}", errorMessage);
                
                // APIエラーの種類に応じたメッセージ
                if (request.responseCode == 401)
                {
                    SpeakText("APIキーが無効です");
                }
                else if (request.responseCode == 429)
                {
                    SpeakText("API使用量が上限に達しました");
                }
                else if (request.responseCode >= 500)
                {
                    SpeakText("サーバーエラーが発生しました");
                }
                else
                {
                    SpeakText("画像の分析に失敗しました");
                }
                
                yield return StartCoroutine(WaitForTTSCompletion());
            }
            
            // 分析完了後にフラグをリセット
            isAnalyzing = false;
            takeflag = false;
            NRDebugger.Info("分析とTTS出力が完了しました。新しい撮影が可能です。");
        }
        
        private IEnumerator WaitForTTSCompletion()
        {
            // TTS出力完了を待機
            if (useDebugTTS)
            {
                // デバッグモードでは、音声が再生されているかどうかをチェック
                while (audioSource != null && audioSource.isPlaying)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                // 追加の少しの待機時間
                yield return new WaitForSeconds(0.5f);
                NRDebugger.Info("デバッグTTS完了待機終了");
            }
            else if (tts != null)
            {
                // 実機でのAndroid TTS完了待機
                bool hasError = false;
                
                // TTSがまだ話している間は待機
                while (true)
                {
                    try
                    {
                        if (!tts.Call<bool>("isSpeaking"))
                            break;
                    }
                    catch (Exception e)
                    {
                        NRDebugger.Warning("TTS完了待機中にエラー: {0}", e.Message);
                        hasError = true;
                        break;
                    }
                    yield return new WaitForSeconds(0.1f);
                }
                
                if (hasError)
                {
                    // エラーの場合は短い待機時間を設ける
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    // 追加の少しの待機時間
                    yield return new WaitForSeconds(0.5f);
                }
                NRDebugger.Info("Android TTS完了待機終了");
            }
            else
            {
                // TTSが初期化されていない場合は短い待機
                yield return new WaitForSeconds(1.0f);
                NRDebugger.Info("TTS未初期化のため固定待機終了");
            }
        }
        
        private string CreateGPTRequestJson(string base64Image)
        {
            // 手動でJSONを構築（JsonUtilityの制限を回避）
            string json = @"{
                ""model"": ""gpt-4o"",
                ""messages"": [
                    {
                        ""role"": ""user"",
                        ""content"": [
                            {
                                ""type"": ""text"",
                                ""text"": """ + gptPrompt.Replace("\"", "\\\"") + @"""
                            },
                            {
                                ""type"": ""image_url"",
                                ""image_url"": {
                                    ""url"": ""data:image/png;base64," + base64Image + @"""
                                }
                            }
                        ]
                    }
                ],
                ""max_tokens"": 300
            }";
            
            return json;
        }
        
        private string ParseGPTResponse(string jsonResponse)
        {
            NRDebugger.Info("GPT応答の解析を開始します");
            NRDebugger.Info("応答サイズ: {0}文字", jsonResponse.Length);
            
            // デバッグのために応答の最初の500文字を表示
            string debugResponse = jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) + "..." : jsonResponse;
            NRDebugger.Info("GPT応答 (最初の500文字): {0}", debugResponse);
            
            try
            {
                // 複数のパターンでcontentを検索
                string[] contentPatterns = {
                    "\"content\":\"",
                    "\"content\": \"",
                    "'content':'",
                    "'content': '"
                };
                
                int contentStart = -1;
                string usedPattern = "";
                
                foreach (string pattern in contentPatterns)
                {
                    contentStart = jsonResponse.IndexOf(pattern);
                    if (contentStart != -1)
                    {
                        usedPattern = pattern;
                        NRDebugger.Info("contentフィールドが見つかりました。パターン: {0}", pattern);
                        break;
                    }
                }
                
                if (contentStart == -1)
                {
                    NRDebugger.Error("応答にcontentフィールドが見つかりません");
                    NRDebugger.Info("検索対象の応答: {0}", jsonResponse);
                    return "分析結果を取得できませんでした";
                }
                
                contentStart += usedPattern.Length;
                int contentEnd = contentStart;
                int escapeCount = 0;
                char quoteChar = usedPattern.Contains("'") ? '\'' : '"';
                
                // エスケープ文字を考慮して終了位置を見つける
                while (contentEnd < jsonResponse.Length)
                {
                    char c = jsonResponse[contentEnd];
                    if (c == '\\')
                    {
                        escapeCount++;
                    }
                    else if (c == quoteChar && escapeCount % 2 == 0)
                    {
                        break;
                    }
                    else if (c != '\\')
                    {
                        escapeCount = 0;
                    }
                    contentEnd++;
                }
                
                if (contentEnd >= jsonResponse.Length)
                {
                    NRDebugger.Error("contentフィールドの終了が見つかりません");
                    return "分析結果を取得できませんでした";
                }
                
                string content = jsonResponse.Substring(contentStart, contentEnd - contentStart);
                // エスケープ文字を解除
                content = content.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\").Replace("\\'", "'");
                
                NRDebugger.Info("抽出されたコンテンツ: {0}", content);
                return string.IsNullOrEmpty(content) ? "分析結果を取得できませんでした" : content;
            }
            catch (Exception e)
            {
                NRDebugger.Error("JSON解析エラー: {0}", e.Message);
                NRDebugger.Error("エラー発生時の応答: {0}", jsonResponse);
                return "分析結果を取得できませんでした";
            }
        }
        
        // 音声読み上げ
        private void SpeakText(string text)
        {
            NRDebugger.Info("音声読み上げ開始: {0}", text);
            
            if (useDebugTTS)
            {
                // エミュレーター/エディタ環境では、代替音声出力
                StartCoroutine(PlayDebugTTS(text));
            }
            else
            {
                // 実機環境でのAndroid TTS
                #if UNITY_ANDROID && !UNITY_EDITOR
                if (tts != null)
                {
                    try
                    {
                        tts.Call<int>("speak", text, 0, null, "speech");
                        NRDebugger.Info("Android TTS音声読み上げ: {0}", text);
                    }
                    catch (Exception e)
                    {
                        NRDebugger.Error("Android TTS音声読み上げエラー: {0}", e.Message);
                        // TTSエラーの場合、デバッグモードにフォールバック
                        useDebugTTS = true;
                        StartCoroutine(PlayDebugTTS(text));
                    }
                }
                else
                {
                    NRDebugger.Warning("TTSオブジェクトがnullです。デバッグモードに切り替えます");
                    useDebugTTS = true;
                    StartCoroutine(PlayDebugTTS(text));
                }
                #else
                // 実機以外の環境
                StartCoroutine(PlayDebugTTS(text));
                #endif
            }
        }
        
        // デバッグ用TTS（エミュレーター/エディタ用）
        private IEnumerator PlayDebugTTS(string text)
        {
            NRDebugger.Info("デバッグTTS出力: {0}", text);
            
            // ビープ音や効果音を再生（オプション）
            if (audioSource != null)
            {
                // 簡単なビープ音を生成
                AudioClip beepClip = GenerateBeepSound(0.3f, 800f);
                audioSource.PlayOneShot(beepClip, 0.5f);
                
                // 音が再生される時間だけ待機
                yield return new WaitForSeconds(0.3f);
            }
            
            // テキストの長さに応じた待機時間をシミュレート
            float speechDuration = Mathf.Max(1.0f, text.Length * 0.1f);
            NRDebugger.Info("TTS再生時間をシミュレート: {0}秒", speechDuration);
            yield return new WaitForSeconds(speechDuration);
            
            NRDebugger.Info("デバッグTTS完了");
        }
        
        // ビープ音生成
        private AudioClip GenerateBeepSound(float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time) * 0.5f;
            }
            
            AudioClip clip = AudioClip.Create("BeepSound", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        
        private void OnDestroy()
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
            
            // TTS リソースを解放
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (tts != null)
            {
                tts.Call("shutdown");
                tts = null;
            }
            #endif
        }
    }
    
    // TTS初期化リスナー
    #if UNITY_ANDROID && !UNITY_EDITOR
    public class TTSInitListener : AndroidJavaProxy
    {
        public TTSInitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
        
        public void onInit(int status)
        {
            if (status == 0) // TextToSpeech.SUCCESS
            {
                NRDebugger.Info("TTS初期化完了");
            }
            else
            {
                NRDebugger.Error("TTS初期化失敗");
            }
        }
    }
    #endif
}