using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// 個別カメラ制御クラス - 監視カメラシステムの基本構成要素
    /// 各カメラのON/OFF制御とRenderTexture管理を行います
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CameraController : UdonSharpBehaviour
    {
        [Header("カメラ基本設定")]
        [Tooltip("カメラ識別ID (0から連番で設定)")]
        public int cameraId = 0;

        [Tooltip("カメラ表示名 (UI表示用)")]
        public string cameraName = "Camera 1";

        [Tooltip("制御対象のカメラコンポーネント")]
        public Camera targetCamera;

        [Tooltip("カメラ映像出力先のRenderTexture")]
        public RenderTexture renderTexture;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private bool _isActive = false;
        private bool _isInitialized = false;

        /// <summary>
        /// カメラのアクティブ状態を取得
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// カメラ名を取得
        /// </summary>
        public string GetCameraName() => cameraName;

        /// <summary>
        /// カメラIDを取得
        /// </summary>
        public int GetCameraId() => cameraId;

        void Start()
        {
            InitializeCamera();
        }

        /// <summary>
        /// カメラの初期化処理
        /// </summary>
        private void InitializeCamera()
        {
            // 必須コンポーネントのチェック
            if (targetCamera == null)
            {
                LogError($"CameraController (ID: {cameraId}): targetCameraが設定されていません");
                return;
            }

            if (renderTexture == null)
            {
                LogError($"CameraController (ID: {cameraId}): renderTextureが設定されていません");
                return;
            }

            // カメラ設定
            targetCamera.targetTexture = renderTexture;

            // デフォルトは非アクティブ状態
            SetCameraActive(false);

            _isInitialized = true;

            LogDebug($"CameraController initialized - ID: {cameraId}, Name: {cameraName}");
        }

        /// <summary>
        /// カメラのアクティブ状態を設定
        /// </summary>
        /// <param name="active">アクティブ状態</param>
        public void SetCameraActive(bool active)
        {
            if (!_isInitialized)
            {
                LogWarning($"CameraController (ID: {cameraId}) is not initialized");
                return;
            }

            _isActive = active;
            targetCamera.enabled = active;

            LogDebug($"Camera {cameraId} ({cameraName}) set to {(active ? "Active" : "Inactive")}");
        }

        /// <summary>
        /// カメラのアクティブ状態を切り替え
        /// </summary>
        public void ToggleCameraActive()
        {
            SetCameraActive(!_isActive);
        }

        /// <summary>
        /// カメラをアクティブに設定
        /// </summary>
        public void ActivateCamera()
        {
            SetCameraActive(true);
        }

        /// <summary>
        /// カメラを非アクティブに設定
        /// </summary>
        public void DeactivateCamera()
        {
            SetCameraActive(false);
        }

        /// <summary>
        /// カメラの状態情報を取得
        /// </summary>
        /// <returns>状態情報文字列</returns>
        public string GetStatusInfo()
        {
            return $"Camera {cameraId}: {cameraName} - {(_isActive ? "Active" : "Inactive")}";
        }

        /// <summary>
        /// カメラの設定が有効かチェック
        /// </summary>
        /// <returns>設定が有効な場合true</returns>
        public bool IsValidConfiguration()
        {
            // 外部から呼ばれた時点で未初期化なら初期化を試行
            if (!_isInitialized && targetCamera != null && renderTexture != null)
            {
                InitializeCamera();
            }
            return targetCamera != null && renderTexture != null && _isInitialized;
        }

        /// <summary>
        /// 外部からの初期化要求（SurveillanceManagerから呼び出し用）
        /// </summary>
        public void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                InitializeCamera();
            }
        }

        #region Debug Logging
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[CameraController] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[CameraController] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CameraController] {message}");
        }
        #endregion

        #region Editor Support
        private void OnValidate()
        {
            // Editorでの設定検証
            if (cameraName == "" || cameraName == null)
            {
                cameraName = $"Camera {cameraId}";
            }
        }
        #endregion
    }
}