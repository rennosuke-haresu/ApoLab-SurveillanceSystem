using UnityEngine;
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
            _InitializeSystem();
        }

        /// <summary>
        /// システム全体の初期化処理
        /// </summary>
        public void _InitializeSystem()
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
                _DisplaySystemStatus();
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
                    if (enableDebugLog)
                    {
                        LogWarning($"Camera controller [{i}] is null");
                    }
                    continue;
                }

                // Start()が未実行のケースに備えて初期化を保証してから検証する
                allCameras[i]._EnsureInitialized();

                if (allCameras[i].IsValidConfiguration())
                {
                    validCameras++;
                }
                else
                {
                    LogError($"Camera controller [{i}] has invalid configuration");
                }
            }

            if (enableDebugLog)
            {
                LogDebug($"Camera initialization: {validCameras}/{allCameras.Length} cameras valid");
            }
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
            int pendingMonitors = 0;
            for (int i = 0; i < allMonitors.Length; i++)
            {
                if (allMonitors[i] == null)
                {
                    if (enableDebugLog)
                    {
                        LogWarning($"Monitor display [{i}] is null");
                    }
                    continue;
                }

                // 個別にカメラを指定していないモニターだけ全カメラを割り当てる
                MonitorDisplay monitor = allMonitors[i];
                if (!monitor.HasAssignedCameras())
                {
                    if (allCameras != null && allCameras.Length > 0)
                    {
                        monitor.cameraControllers = allCameras;
                    }
                }
                else if (enableDebugLog)
                {
                    WarnUnregisteredCameras(i, monitor.cameraControllers);
                }

                // 非アクティブなモニターは、初めてアクティブになったときに自身の Start で初期化される
                if (!monitor.gameObject.activeInHierarchy)
                {
                    if (monitor.HasAssignedCameras())
                    {
                        pendingMonitors++;
                    }
                    else
                    {
                        LogError($"Monitor display [{i}] has no cameras");
                    }
                    continue;
                }

                if (monitor.IsValidConfiguration())
                {
                    validMonitors++;
                }
                else
                {
                    LogError($"Monitor display [{i}] has invalid configuration");
                }
            }

            if (enableDebugLog)
            {
                LogDebug($"Monitor initialization: {validMonitors}/{allMonitors.Length} monitors valid, {pendingMonitors} inactive");
            }
            return validMonitors + pendingMonitors > 0;
        }

        /// <summary>
        /// モニターに個別指定されたカメラのうち All Cameras に含まれないものを警告する
        /// （映像は映るが、マネージャーの検証対象から外れるため）
        /// </summary>
        private void WarnUnregisteredCameras(int monitorIndex, CameraController[] monitorCameras)
        {
            for (int c = 0; c < monitorCameras.Length; c++)
            {
                CameraController cam = monitorCameras[c];
                if (cam == null) continue;

                bool registered = false;
                if (allCameras != null)
                {
                    for (int a = 0; a < allCameras.Length; a++)
                    {
                        if (allCameras[a] == cam)
                        {
                            registered = true;
                            break;
                        }
                    }
                }

                if (!registered)
                {
                    LogWarning($"Monitor display [{monitorIndex}]: camera [{c}] is not registered in All Cameras");
                }
            }
        }

        /// <summary>
        /// カメラセレクターの初期化
        /// セレクターは任意のコンポーネントのため、検証結果にかかわらず常に true を返します
        /// （設定不備は個別にログへ出力されます）
        /// </summary>
        /// <returns>常にtrue</returns>
        private bool InitializeSelectors()
        {
            if (allSelectors == null || allSelectors.Length == 0)
            {
                LogWarning("No camera selectors configured in the system");
                return true;
            }

            int validSelectors = 0;
            for (int i = 0; i < allSelectors.Length; i++)
            {
                if (allSelectors[i] == null)
                {
                    if (enableDebugLog)
                    {
                        LogWarning($"Camera selector [{i}] is null");
                    }
                    continue;
                }

                // 非アクティブなセレクターは、初めてアクティブになったときに自身の Start で初期化される
                if (!allSelectors[i].gameObject.activeInHierarchy) continue;

                // 対象モニターが非アクティブな間は、そのモニターと同じく保留として扱う
                MonitorDisplay selectorTarget = allSelectors[i].targetMonitor;
                if (selectorTarget != null && !selectorTarget.gameObject.activeInHierarchy) continue;

                if (allSelectors[i].IsValidConfiguration())
                {
                    validSelectors++;
                }
                else
                {
                    LogError($"Camera selector [{i}] has invalid configuration");
                }
            }

            if (enableDebugLog)
            {
                LogDebug($"Selector initialization: {validSelectors}/{allSelectors.Length} selectors valid");
            }
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
            // 全モニターをOFF状態に設定（各モニターが表示登録を解除するため、カメラの表示カウントも正しく減る）
            // ※ _DeactivateAllCameras は使わない。マネージャー未登録のモニターが見ているカメラまで止めてしまうため
            _TurnOffAllMonitors();

            LogDebug("System set to initial state (all monitors off)");
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
                    allSelectors[i]._RefreshUI();
                }
            }
        }

        /// <summary>
        /// 全カメラの表示カウントをリセットし、非アクティブに設定
        /// ※ マネージャーに登録されていないモニターが表示中のカメラも止まる（そのモニターの映像は最後のフレームで固まる）
        /// </summary>
        public void _DeactivateAllCameras()
        {
            if (allCameras == null) return;

            for (int i = 0; i < allCameras.Length; i++)
            {
                if (allCameras[i] != null && allCameras[i].IsValidConfiguration())
                {
                    allCameras[i]._ResetViewers();
                }
            }

            LogDebug("All cameras deactivated");
        }

        /// <summary>
        /// 全モニターをOFF状態に設定
        /// </summary>
        public void _TurnOffAllMonitors()
        {
            if (allMonitors == null) return;

            for (int i = 0; i < allMonitors.Length; i++)
            {
                if (allMonitors[i] != null && allMonitors[i].IsValidConfiguration())
                {
                    allMonitors[i]._SetDisplayOff();
                }
            }

            LogDebug("All monitors turned off");
        }

        /// <summary>
        /// システム状態の詳細表示
        /// </summary>
        public void _DisplaySystemStatus()
        {
            if (!enableDebugLog) return;

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
        public void _ResetSystem()
        {
            LogDebug("Resetting surveillance system...");

            // 再初期化が途中で失敗しても表示が残らないよう、先に登録モニターをOFFにする
            _TurnOffAllMonitors();

            // システム再初期化
            _isInitialized = false;
            _InitializeSystem();
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
        // 補間文字列を渡す場合は、呼び出し側で enableDebugLog をガードすること
        // （false でも文字列生成のコストが発生するため）
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
    }
}
