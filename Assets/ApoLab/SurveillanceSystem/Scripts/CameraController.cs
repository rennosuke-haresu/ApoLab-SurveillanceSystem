using UnityEngine;
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
        [Header("Camera")]
        [Tooltip("Camera ID (sequential from 0)\nカメラ識別ID (0から連番で設定)")]
        public int cameraId = 0;

        [Tooltip("Camera name shown on monitors\nカメラ表示名 (UI表示用)")]
        public string cameraName = "Camera 1";

        [Tooltip("Camera component to control\n制御対象のカメラコンポーネント")]
        public Camera targetCamera;

        [Tooltip("RenderTexture the camera renders to\nカメラ映像出力先のRenderTexture")]
        public RenderTexture renderTexture;

        [Header("Debug")]
        [Tooltip("Output debug logs\nデバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private bool _isActive = false;
        private bool _isInitialized = false;

        // このカメラを表示しているモニターの数
        // 複数モニターが同じカメラを映しているとき、1枚がOFFになっても
        // 残りのモニターの映像が止まらないようにするためのカウンタ
        private int _viewerCount = 0;

        /// <summary>
        /// カメラのアクティブ状態を取得
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// このカメラを表示しているモニターの数
        /// </summary>
        public int ViewerCount => _viewerCount;

        /// <summary>
        /// カメラ名を取得（未設定なら "Camera {ID}"）
        /// </summary>
        public string GetCameraName()
        {
            if (cameraName == null || cameraName == "") return $"Camera {cameraId}";
            return cameraName;
        }

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
                LogError($"CameraController (ID: {cameraId}): targetCamera is not set");
                return;
            }

            if (renderTexture == null)
            {
                LogError($"CameraController (ID: {cameraId}): renderTexture is not set");
                // 出力先がないままカメラが有効だとプレイヤーの画面に直接描画されるため止める
                targetCamera.enabled = false;
                return;
            }

            // カメラ設定
            targetCamera.targetTexture = renderTexture;

            _isInitialized = true;

            // デフォルトは非アクティブ状態
            _viewerCount = 0;
            SetCameraActive(false);

            if (enableDebugLog)
            {
                LogDebug($"CameraController initialized - ID: {cameraId}, Name: {GetCameraName()}");
            }
        }

        /// <summary>
        /// 表示中のモニターを1台追加する（1台以上でカメラが有効になる）
        /// </summary>
        public void _AddViewer()
        {
            _viewerCount++;
            SetCameraActive(true);
        }

        /// <summary>
        /// 表示中のモニターを1台減らす（0台になったらカメラを無効化）
        /// </summary>
        public void _RemoveViewer()
        {
            _viewerCount--;

            if (_viewerCount <= 0)
            {
                _viewerCount = 0;
                SetCameraActive(false);
            }
        }

        /// <summary>
        /// 表示カウントを0に戻してカメラを無効化する（システム初期化用）
        /// </summary>
        public void _ResetViewers()
        {
            _viewerCount = 0;
            SetCameraActive(false);
        }

        /// <summary>
        /// カメラのアクティブ状態を設定（外部からは _AddViewer / _RemoveViewer を経由すること）
        /// </summary>
        /// <param name="active">アクティブ状態</param>
        private void SetCameraActive(bool active)
        {
            if (!_isInitialized)
            {
                if (enableDebugLog)
                {
                    LogWarning($"CameraController (ID: {cameraId}) is not initialized");
                }
                return;
            }

            _isActive = active;
            targetCamera.enabled = active;

            if (enableDebugLog)
            {
                LogDebug($"Camera {cameraId} ({GetCameraName()}) set to {(active ? "Active" : "Inactive")}");
            }
        }

        /// <summary>
        /// カメラの状態情報を取得
        /// </summary>
        /// <returns>状態情報文字列</returns>
        public string GetStatusInfo()
        {
            return $"Camera {cameraId}: {GetCameraName()} - {(_isActive ? "Active" : "Inactive")}";
        }

        /// <summary>
        /// カメラの設定が有効かチェック（副作用なし）
        /// </summary>
        /// <returns>設定が有効な場合true</returns>
        public bool IsValidConfiguration()
        {
            return _isInitialized && targetCamera != null && renderTexture != null;
        }

        /// <summary>
        /// 未初期化なら初期化を試行する（SurveillanceManagerから検証前に呼び出し）
        /// </summary>
        public void _EnsureInitialized()
        {
            if (!_isInitialized)
            {
                InitializeCamera();
            }
        }

        #region Debug Logging
        // 補間文字列を渡す場合は、呼び出し側で enableDebugLog をガードすること
        // （false でも文字列生成のコストが発生するため）
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
    }
}
