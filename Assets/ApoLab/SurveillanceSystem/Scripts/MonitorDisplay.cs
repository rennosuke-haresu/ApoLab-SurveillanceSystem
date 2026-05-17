using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;
using TMPro;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// モニター表示制御クラス - 監視システムのモニター画面制御
    /// カメラ映像の表示とカメラ切り替え機能を提供します
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MonitorDisplay : UdonSharpBehaviour
    {
        [Header("モニター基本設定")]
        [Tooltip("モニター識別ID (0から連番で設定)")]
        public int monitorId = 0;

        [Tooltip("モニター表示名")]
        public string monitorName = "Monitor 1";

        [Tooltip("映像を表示するRenderer（Quad使用）")]
        public Renderer displayRenderer;

        [Header("カメラシステム連携")]
        [Tooltip("制御対象のカメラコントローラー配列")]
        public CameraController[] cameraControllers = new CameraController[0];

        [Tooltip("監視システムマネージャー（自動設定）")]
        public SurveillanceManager surveillanceManager;

        [Header("UI要素")]
        [Tooltip("カメラ名を表示するTextMeshPro")]
        public TextMeshProUGUI cameraNameText;

        [Header("表示設定")]
        [Tooltip("OFF時に表示するテクスチャ（黒画像を設定）")]
        public Texture offTexture;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private int _currentCameraIndex = -1; // -1 = OFF状態
        private bool _isInitialized = false;

        /// <summary>
        /// 現在表示中のカメラインデックスを取得
        /// </summary>
        public int CurrentCameraIndex => _currentCameraIndex;

        /// <summary>
        /// モニターがアクティブかどうか
        /// </summary>
        public bool IsDisplayActive => _currentCameraIndex >= 0;

        /// <summary>
        /// モニター名を取得
        /// </summary>
        public string GetMonitorName() => monitorName;

        /// <summary>
        /// モニターIDを取得
        /// </summary>
        public int GetMonitorId() => monitorId;

        void Start()
        {
            InitializeMonitor();
        }

        /// <summary>
        /// モニターの初期化処理
        /// </summary>
        private void InitializeMonitor()
        {
            // 必須コンポーネントのチェック
            if (displayRenderer == null)
            {
                LogError($"MonitorDisplay (ID: {monitorId}): displayRendererが設定されていません");
                return;
            }

            // カメラコントローラー配列の検証
            ValidateCameraControllers();

            // デフォルトはOFF状態
            SetDisplayOff();

            // UI初期化
            UpdateCameraNameDisplay();

            _isInitialized = true;

            LogDebug($"MonitorDisplay initialized - ID: {monitorId}, Name: {monitorName}, Cameras: {cameraControllers.Length}");
        }


        /// <summary>
        /// カメラコントローラー配列の検証
        /// </summary>
        private void ValidateCameraControllers()
        {
            if (cameraControllers == null || cameraControllers.Length == 0)
            {
                LogWarning($"MonitorDisplay (ID: {monitorId}): カメラコントローラーが設定されていません");
                return;
            }

            // null要素のチェック
            for (int i = 0; i < cameraControllers.Length; i++)
            {
                if (cameraControllers[i] == null)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): カメラコントローラー[{i}]がnullです");
                }
            }
        }

        /// <summary>
        /// 指定したインデックスのカメラを表示
        /// </summary>
        /// <param name="cameraIndex">カメラインデックス</param>
        public void DisplayCamera(int cameraIndex)
        {
            if (!_isInitialized)
            {
                LogWarning($"MonitorDisplay (ID: {monitorId}) is not initialized");
                return;
            }

            // インデックスの範囲チェック
            if (cameraIndex < 0 || cameraIndex >= cameraControllers.Length || cameraControllers[cameraIndex] == null)
            {
                LogWarning($"MonitorDisplay (ID: {monitorId}): 無効なカメラインデックス {cameraIndex}");
                return;
            }

            CameraController targetCamera = cameraControllers[cameraIndex];

            // カメラの設定確認
            if (!targetCamera.IsValidConfiguration())
            {
                LogError($"MonitorDisplay (ID: {monitorId}): カメラ {cameraIndex} の設定が無効です");
                return;
            }

            // 前のカメラを無効化
            DeactivateCurrentCamera();

            // 新しいカメラをアクティブ化
            targetCamera.SetCameraActive(true);
            displayRenderer.material.mainTexture = targetCamera.renderTexture;
            _currentCameraIndex = cameraIndex;

            // UI更新
            UpdateCameraNameDisplay();

            LogDebug($"Monitor {monitorId} switched to Camera {cameraIndex} ({targetCamera.GetCameraName()})");
        }

        /// <summary>
        /// 表示をOFFに設定
        /// </summary>
        public void SetDisplayOff()
        {
            // 初期化中の呼び出しも許可（displayRendererのみチェック）
            if (displayRenderer == null) return;

            // 現在のカメラを無効化
            DeactivateCurrentCamera();

            displayRenderer.material.mainTexture = offTexture;
            _currentCameraIndex = -1;

            // UI更新（初期化完了後のみ）
            if (_isInitialized)
            {
                UpdateCameraNameDisplay();
                LogDebug($"Monitor {monitorId} display turned OFF");
            }
        }

        /// <summary>
        /// 現在アクティブなカメラを無効化
        /// </summary>
        private void DeactivateCurrentCamera()
        {
            if (_currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
            {
                cameraControllers[_currentCameraIndex].SetCameraActive(false);
            }
        }

        /// <summary>
        /// カメラ名表示の更新
        /// </summary>
        private void UpdateCameraNameDisplay()
        {
            if (cameraNameText == null) return;

            if (_currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
            {
                cameraNameText.text = cameraControllers[_currentCameraIndex].GetCameraName();
            }
            else
            {
                cameraNameText.text = "OFF";
            }
        }

        /// <summary>
        /// 次のカメラに切り替え
        /// </summary>
        public void NextCamera()
        {
            if (!_isInitialized || cameraControllers.Length == 0) return;

            int nextIndex = _currentCameraIndex + 1;
            if (nextIndex >= cameraControllers.Length)
            {
                nextIndex = 0; // 最初のカメラに戻る
            }

            DisplayCamera(nextIndex);
        }

        /// <summary>
        /// 前のカメラに切り替え
        /// </summary>
        public void PreviousCamera()
        {
            if (!_isInitialized || cameraControllers.Length == 0) return;

            int prevIndex = _currentCameraIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = cameraControllers.Length - 1; // 最後のカメラに移動
            }

            DisplayCamera(prevIndex);
        }

        /// <summary>
        /// モニターの状態情報を取得
        /// </summary>
        /// <returns>状態情報文字列</returns>
        public string GetStatusInfo()
        {
            string cameraName = (_currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
                ? cameraControllers[_currentCameraIndex].GetCameraName()
                : "OFF";

            return $"Monitor {monitorId}: {monitorName} - {cameraName}";
        }

        /// <summary>
        /// モニターの設定が有効かチェック
        /// </summary>
        /// <returns>設定が有効な場合true</returns>
        public bool IsValidConfiguration()
        {
            return _isInitialized && displayRenderer != null && cameraControllers != null && cameraControllers.Length > 0;
        }

        #region Debug Logging
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[MonitorDisplay] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[MonitorDisplay] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MonitorDisplay] {message}");
        }
        #endregion

        #region Editor Support
        private void OnValidate()
        {
            // Editorでの設定検証
            if (monitorName == "" || monitorName == null)
            {
                monitorName = $"Monitor {monitorId}";
            }
        }
        #endregion
    }
}