using UnityEngine;
using UdonSharp;
using TMPro;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// モニター表示制御クラス - 監視システムのモニター画面制御
    /// カメラ映像の表示とカメラ切り替え機能を提供します
    /// ※ 表示状態はプレイヤーごとにローカルで、他プレイヤーには同期されません
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MonitorDisplay : UdonSharpBehaviour
    {
        [Header("Monitor")]
        [Tooltip("Monitor ID (sequential from 0)\nモニター識別ID (0から連番で設定)")]
        public int monitorId = 0;

        [Tooltip("Monitor name\nモニター表示名")]
        public string monitorName = "Monitor 1";

        [Tooltip("Renderer that shows the video (Quad)\n映像を表示するRenderer（Quad使用）")]
        public Renderer displayRenderer;

        [Header("Cameras")]
        [Tooltip("Cameras this monitor switches between. Empty = the manager's All Cameras\nこのモニターで切り替えるカメラ。空ならマネージャーの All Cameras を使用。指定するとこのモニターではそのカメラだけを切り替える")]
        public CameraController[] cameraControllers = new CameraController[0];

        [Header("UI")]
        [Tooltip("TextMeshPro that shows the camera name\nカメラ名を表示するTextMeshPro")]
        public TextMeshProUGUI cameraNameText;

        [Header("Display")]
        [Tooltip("Texture shown when off (e.g. black)\nOFF時に表示するテクスチャ（黒画像を設定）")]
        public Texture offTexture;

        [Header("Status Light (Optional)")]
        [Tooltip("Renderer whose material changes with the video state (does nothing if empty)\n映像の有無で色を変えるランプ（未設定なら何もしない）")]
        public Renderer statusLight;

        [Tooltip("Material while showing video\n映しているときのマテリアル")]
        public Material statusOnMaterial;

        [Tooltip("Material while off\nOFF のときのマテリアル")]
        public Material statusOffMaterial;

        [Header("Debug")]
        [Tooltip("Output debug logs\nデバッグログを出力するかどうか")]
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
        /// モニター名を取得（未設定なら "Monitor {ID}"）
        /// </summary>
        public string GetMonitorName()
        {
            if (monitorName == null || monitorName == "") return $"Monitor {monitorId}";
            return monitorName;
        }

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
                LogError($"MonitorDisplay (ID: {monitorId}): displayRenderer is not set");
                return;
            }

            // カメラコントローラー配列の検証
            ValidateCameraControllers();

            // デフォルトはOFF状態
            _SetDisplayOff();

            // UI初期化
            UpdateCameraNameDisplay();

            _isInitialized = true;

            if (enableDebugLog)
            {
                LogDebug($"MonitorDisplay initialized - ID: {monitorId}, Name: {GetMonitorName()}, Cameras: {cameraControllers.Length}");
            }
        }


        /// <summary>
        /// カメラコントローラー配列の検証
        /// </summary>
        private void ValidateCameraControllers()
        {
            if (cameraControllers == null || cameraControllers.Length == 0)
            {
                if (enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): no camera controllers are set");
                }
                return;
            }

            // null要素のチェック
            for (int i = 0; i < cameraControllers.Length; i++)
            {
                if (cameraControllers[i] == null && enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): camera controller [{i}] is null");
                }
            }
        }

        /// <summary>
        /// 指定したインデックスのカメラを表示
        /// </summary>
        /// <param name="cameraIndex">カメラインデックス</param>
        public void _DisplayCamera(int cameraIndex)
        {
            if (!_isInitialized)
            {
                if (enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}) is not initialized");
                }
                return;
            }

            // 非アクティブなモニターのためにカメラを描画させない（OnDisable で解除した表示を再登録しない）
            if (!gameObject.activeInHierarchy) return;

            // インデックスの範囲チェック
            if (cameraControllers == null || cameraIndex < 0 || cameraIndex >= cameraControllers.Length || cameraControllers[cameraIndex] == null)
            {
                if (enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): invalid camera index {cameraIndex}");
                }
                return;
            }

            // 既に同じカメラを表示中なら何もしない（表示カウントの往復を避ける）
            if (_currentCameraIndex == cameraIndex) return;

            CameraController targetCamera = cameraControllers[cameraIndex];

            // カメラの設定確認
            if (!targetCamera.IsValidConfiguration())
            {
                LogError($"MonitorDisplay (ID: {monitorId}): camera {cameraIndex} is not configured correctly");
                return;
            }

            // 前のカメラの表示を解除
            RemoveCurrentViewer();

            // 新しいカメラの表示を登録
            targetCamera._AddViewer();
            displayRenderer.material.mainTexture = targetCamera.renderTexture;
            _currentCameraIndex = cameraIndex;
            UpdateStatusLight();

            // UI更新
            UpdateCameraNameDisplay();

            if (enableDebugLog)
            {
                LogDebug($"Monitor {monitorId} switched to Camera {cameraIndex} ({targetCamera.GetCameraName()})");
            }
        }

        /// <summary>
        /// 表示をOFFに設定
        /// </summary>
        public void _SetDisplayOff()
        {
            // 初期化中の呼び出しも許可（displayRendererのみチェック）
            if (displayRenderer == null) return;

            // 現在のカメラの表示を解除
            RemoveCurrentViewer();

            displayRenderer.material.mainTexture = offTexture;
            _currentCameraIndex = -1;
            UpdateStatusLight();

            // UI更新（初期化完了後のみ）
            if (_isInitialized)
            {
                UpdateCameraNameDisplay();

                if (enableDebugLog)
                {
                    LogDebug($"Monitor {monitorId} display turned OFF");
                }
            }
        }

        /// <summary>
        /// モニターが非アクティブになったら表示を解除（見られていないカメラの描画を止めるため）
        /// </summary>
        private void OnDisable()
        {
            if (_isInitialized)
            {
                _SetDisplayOff();
            }
        }

        /// <summary>
        /// 状態表示ランプを現在の状態に合わせる（Renderer ごとの差し替えなので他のモニターに波及しない）
        /// </summary>
        private void UpdateStatusLight()
        {
            if (statusLight == null) return;
            Material m = _currentCameraIndex >= 0 ? statusOnMaterial : statusOffMaterial;
            if (m != null) statusLight.sharedMaterial = m;
        }

        /// <summary>
        /// 現在表示中のカメラの表示登録を解除
        /// </summary>
        private void RemoveCurrentViewer()
        {
            if (cameraControllers != null && _currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
            {
                cameraControllers[_currentCameraIndex]._RemoveViewer();
            }
        }

        /// <summary>
        /// カメラ名表示の更新
        /// </summary>
        private void UpdateCameraNameDisplay()
        {
            if (cameraNameText == null) return;

            if (cameraControllers != null && _currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
            {
                cameraNameText.text = cameraControllers[_currentCameraIndex].GetCameraName();
            }
            else
            {
                cameraNameText.text = "OFF";
            }
        }

        /// <summary>
        /// 指定方向で次に表示できる有効なカメラのインデックスを探す
        /// </summary>
        /// <param name="startIndex">探索の起点（-1 = OFF状態）</param>
        /// <param name="step">+1で順送り、-1で逆送り</param>
        /// <returns>見つかったインデックス。見つからない場合は -1</returns>
        private int FindValidIndex(int startIndex, int step)
        {
            if (cameraControllers == null || cameraControllers.Length == 0) return -1;

            int count = cameraControllers.Length;
            int index = startIndex;

            for (int i = 0; i < count; i++)
            {
                index += step;

                if (index >= count) index = 0;
                else if (index < 0) index = count - 1;

                if (cameraControllers[index] != null && cameraControllers[index].IsValidConfiguration())
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 次のカメラに切り替え（無効なカメラは読み飛ばす）
        /// </summary>
        public void _NextCamera()
        {
            if (!_isInitialized) return;

            int nextIndex = FindValidIndex(_currentCameraIndex, 1);

            if (nextIndex < 0)
            {
                if (enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): no camera available to display");
                }
                return;
            }

            _DisplayCamera(nextIndex);
        }

        /// <summary>
        /// 前のカメラに切り替え（無効なカメラは読み飛ばす）
        /// </summary>
        public void _PreviousCamera()
        {
            if (!_isInitialized) return;

            int prevIndex = FindValidIndex(_currentCameraIndex, -1);

            if (prevIndex < 0)
            {
                if (enableDebugLog)
                {
                    LogWarning($"MonitorDisplay (ID: {monitorId}): no camera available to display");
                }
                return;
            }

            _DisplayCamera(prevIndex);
        }

        /// <summary>
        /// モニターの状態情報を取得
        /// </summary>
        /// <returns>状態情報文字列</returns>
        public string GetStatusInfo()
        {
            string cameraName = (cameraControllers != null && _currentCameraIndex >= 0 && _currentCameraIndex < cameraControllers.Length && cameraControllers[_currentCameraIndex] != null)
                ? cameraControllers[_currentCameraIndex].GetCameraName()
                : "OFF";

            return $"Monitor {monitorId}: {GetMonitorName()} - {cameraName}";
        }

        /// <summary>
        /// 切り替え対象のカメラが設定されているか（初期化状態に依存しないため、Start の実行順に左右されない）
        /// </summary>
        /// <returns>表示先とカメラが1台以上設定されている場合true</returns>
        public bool HasCameras()
        {
            return displayRenderer != null && HasAssignedCameras();
        }

        /// <summary>
        /// カメラ一覧に null でない要素が1つ以上あるか（サイズだけ設定して中身が空の一覧は false）
        /// </summary>
        /// <returns>カメラが1台以上登録されている場合true</returns>
        public bool HasAssignedCameras()
        {
            if (cameraControllers == null) return false;

            for (int i = 0; i < cameraControllers.Length; i++)
            {
                if (cameraControllers[i] != null) return true;
            }
            return false;
        }

        /// <summary>
        /// モニターの設定が有効かチェック
        /// </summary>
        /// <returns>設定が有効な場合true</returns>
        public bool IsValidConfiguration()
        {
            return _isInitialized && displayRenderer != null && HasAssignedCameras();
        }

        #region Debug Logging
        // 補間文字列を渡す場合は、呼び出し側で enableDebugLog をガードすること
        // （false でも文字列生成のコストが発生するため）
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
    }
}
