using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// 監視システム統合管理クラス - 全体的なシステム制御と初期化
    /// 全カメラとモニターの統合管理、システム状態監視を行います
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SurveillanceManager : UdonSharpBehaviour
    {
        [Header("システム基本設定")]
        [Tooltip("自動初期化を有効にする")]
        public bool autoInitialize = true;

        [Header("コンポーネント管理")]
        [Tooltip("システム内の全カメラコントローラー")]
        public CameraController[] allCameras = new CameraController[0];

        [Tooltip("システム内の全モニターディスプレイ")]
        public MonitorDisplay[] allMonitors = new MonitorDisplay[0];

        [Tooltip("システム内の全カメラセレクター")]
        public CameraSelector[] allSelectors = new CameraSelector[0];

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        [Tooltip("詳細なシステム情報を表示")]
        public bool showDetailedStatus = false;

        // 内部状態管理
        private bool _isInitialized = false;

        /// <summary>
        /// システムが初期化されているか
        /// </summary>
        public bool IsSystemInitialized => _isInitialized;

        void Start()
        {
            if (autoInitialize)
            {
                // 全コンポーネントのStart()完了後に初期化するため1フレーム遅延
                SendCustomEventDelayedFrames(nameof(_DelayedInitialize), 1);
            }
        }

        /// <summary>
        /// 1フレーム遅延後の初期化（全コンポーネントのStart()完了を保証）
        /// </summary>
        public void _DelayedInitialize()
        {
            InitializeSystem();
        }

        /// <summary>
        /// システム全体の初期化処理
        /// </summary>
        public void InitializeSystem()
        {
            LogDebug("Initializing...");

            // コンポーネントの検証と初期化
            bool initSuccess = true;
            initSuccess &= InitializeCameras();
            initSuccess &= InitializeMonitors();
            initSuccess &= InitializeSelectors();

            if (!initSuccess)
            {
                LogError("System initialization failed due to component errors");
                return;
            }

            // システム間の関連付け
            LinkSystemComponents();

            // 初期状態設定
            SetInitialState();

            // セレクターUIをカメラ設定反映後に更新
            RefreshAllSelectors();

            _isInitialized = true;
            LogDebug("Initialization completed successfully");

            // 初期ステータス表示
            if (showDetailedStatus)
            {
                DisplaySystemStatus();
            }
        }

        /// <summary>
        /// カメラコントローラーの初期化
        /// </summary>
        /// <returns>成功した場合true</returns>
        private bool InitializeCameras()
        {
            if (allCameras == null || allCameras.Length == 0)
            {
                LogWarning("No cameras configured in the system");
                return true; // カメラがなくてもシステムは動作可能
            }

            int validCameras = 0;
            for (int i = 0; i < allCameras.Length; i++)
            {
                if (allCameras[i] == null)
                {
                    LogWarning($"Camera controller [{i}] is null");
                    continue;
                }

                if (allCameras[i].IsValidConfiguration())
                {
                    validCameras++;
                }
                else
                {
                    LogError($"Camera controller [{i}] has invalid configuration");
                }
            }

            LogDebug($"Camera initialization: {validCameras}/{allCameras.Length} cameras valid");
            return validCameras > 0;
        }

        /// <summary>
        /// モニターディスプレイの初期化
        /// </summary>
        /// <returns>成功した場合true</returns>
        private bool InitializeMonitors()
        {
            if (allMonitors == null || allMonitors.Length == 0)
            {
                LogWarning("No monitors configured in the system");
                return false; // モニターがないとシステムとして意味がない
            }

            int validMonitors = 0;
            for (int i = 0; i < allMonitors.Length; i++)
            {
                if (allMonitors[i] == null)
                {
                    LogWarning($"Monitor display [{i}] is null");
                    continue;
                }

                // モニターにカメラコントローラー配列を設定
                if (allCameras != null && allCameras.Length > 0)
                {
                    allMonitors[i].cameraControllers = allCameras;
                }

                if (allMonitors[i].IsValidConfiguration())
                {
                    validMonitors++;
                }
                else
                {
                    LogError($"Monitor display [{i}] has invalid configuration");
                }
            }

            LogDebug($"Monitor initialization: {validMonitors}/{allMonitors.Length} monitors valid");
            return validMonitors > 0;
        }

        /// <summary>
        /// カメラセレクターの初期化
        /// </summary>
        /// <returns>成功した場合true</returns>
        private bool InitializeSelectors()
        {
            if (allSelectors == null || allSelectors.Length == 0)
            {
                LogWarning("No camera selectors configured in the system");
                return true; // セレクターがなくてもシステムは動作可能
            }

            int validSelectors = 0;
            for (int i = 0; i < allSelectors.Length; i++)
            {
                if (allSelectors[i] == null)
                {
                    LogWarning($"Camera selector [{i}] is null");
                    continue;
                }

                if (allSelectors[i].IsValidConfiguration())
                {
                    validSelectors++;
                }
                else
                {
                    LogError($"Camera selector [{i}] has invalid configuration");
                }
            }

            LogDebug($"Selector initialization: {validSelectors}/{allSelectors.Length} selectors valid");
            return true;
        }

        /// <summary>
        /// システムコンポーネント間の関連付け
        /// </summary>
        private void LinkSystemComponents()
        {
            // モニターにマネージャーの参照を設定
            if (allMonitors == null) return;

            for (int i = 0; i < allMonitors.Length; i++)
            {
                if (allMonitors[i] != null)
                {
                    allMonitors[i].surveillanceManager = this;
                }
            }
        }

        /// <summary>
        /// システムの初期状態を設定
        /// </summary>
        private void SetInitialState()
        {
            // 全カメラを初期状態（非アクティブ）に設定
            DeactivateAllCameras();

            // 全モニターをOFF状態に設定
            TurnOffAllMonitors();

            LogDebug("System set to initial state (all cameras and monitors off)");
        }

        /// <summary>
        /// 全セレクターのUIを更新（カメラ設定完了後に呼び出す）
        /// </summary>
        private void RefreshAllSelectors()
        {
            if (allSelectors == null) return;

            for (int i = 0; i < allSelectors.Length; i++)
            {
                if (allSelectors[i] != null)
                {
                    allSelectors[i].RefreshUI();
                }
            }
        }

        /// <summary>
        /// 全カメラを非アクティブに設定
        /// </summary>
        public void DeactivateAllCameras()
        {
            if (allCameras == null) return;

            for (int i = 0; i < allCameras.Length; i++)
            {
                if (allCameras[i] != null && allCameras[i].IsValidConfiguration())
                {
                    allCameras[i].DeactivateCamera();
                }
            }

            LogDebug("All cameras deactivated");
        }

        /// <summary>
        /// 全モニターをOFF状態に設定
        /// </summary>
        public void TurnOffAllMonitors()
        {
            if (allMonitors == null) return;

            for (int i = 0; i < allMonitors.Length; i++)
            {
                if (allMonitors[i] != null && allMonitors[i].IsValidConfiguration())
                {
                    allMonitors[i].SetDisplayOff();
                }
            }

            LogDebug("All monitors turned off");
        }

        /// <summary>
        /// システム状態の詳細表示
        /// </summary>
        public void DisplaySystemStatus()
        {
            LogDebug("=== System Status ===");
            LogDebug($"Initialized: {_isInitialized}");
            LogDebug($"Cameras: {(allCameras != null ? allCameras.Length : 0)} configured");
            LogDebug($"Monitors: {(allMonitors != null ? allMonitors.Length : 0)} configured");
            LogDebug($"Selectors: {(allSelectors != null ? allSelectors.Length : 0)} configured");
            LogDebug("====================");
        }

        /// <summary>
        /// システムの完全リセット
        /// </summary>
        public void ResetSystem()
        {
            LogDebug("Resetting surveillance system...");

            // 全コンポーネントを初期状態に戻す
            DeactivateAllCameras();
            TurnOffAllMonitors();

            // システム再初期化
            _isInitialized = false;
            InitializeSystem();
        }

        /// <summary>
        /// 指定したカメラの情報を取得
        /// </summary>
        /// <param name="cameraId">カメラID</param>
        /// <returns>カメラ情報文字列</returns>
        public string GetCameraInfo(int cameraId)
        {
            if (allCameras == null || cameraId < 0 || cameraId >= allCameras.Length || allCameras[cameraId] == null)
            {
                return $"Camera {cameraId}: Not found";
            }

            return allCameras[cameraId].GetStatusInfo();
        }

        /// <summary>
        /// 指定したモニターの情報を取得
        /// </summary>
        /// <param name="monitorId">モニターID</param>
        /// <returns>モニター情報文字列</returns>
        public string GetMonitorInfo(int monitorId)
        {
            if (allMonitors == null || monitorId < 0 || monitorId >= allMonitors.Length || allMonitors[monitorId] == null)
            {
                return $"Monitor {monitorId}: Not found";
            }

            return allMonitors[monitorId].GetStatusInfo();
        }

        #region Debug Logging
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SurveillanceManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[SurveillanceManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SurveillanceManager] {message}");
        }
        #endregion

        #region Editor Support
        private void OnValidate()
        {
            // Editorでの設定検証
        }
        #endregion
    }
}