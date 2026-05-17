using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using UdonSharp;
using TMPro;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// カメラ切り替えUI制御クラス - シンプルな前後切り替えインターフェース
    /// [◀][▶][OFF]の3ボタンでカメラを簡単に切り替えます
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CameraSelector : UdonSharpBehaviour
    {
        [Header("UI制御設定")]
        [Tooltip("制御対象のモニターディスプレイ")]
        public MonitorDisplay targetMonitor;

        [Header("UIボタン要素")]
        [Tooltip("前のカメラボタン [◀]")]
        public Button previousButton;

        [Tooltip("前のカメラボタンのImageコンポーネント（色変更用）")]
        public Image previousButtonImage;

        [Tooltip("次のカメラボタン [▶]")]
        public Button nextButton;

        [Tooltip("次のカメラボタンのImageコンポーネント（色変更用）")]
        public Image nextButtonImage;

        [Tooltip("表示OFFボタン [OFF]")]
        public Button offButton;

        [Tooltip("OFFボタンのImageコンポーネント（色変更用）")]
        public Image offButtonImage;

        [Header("UI表示要素")]
        [Tooltip("現在のカメラ名を表示するTextMeshPro")]
        public TextMeshProUGUI currentCameraText;

        [Tooltip("モニター名を表示するTextMeshPro")]
        public TextMeshProUGUI monitorNameText;

        [Header("視覚的フィードバック")]
        [Tooltip("ボタン押下時の色変更")]
        public bool enableButtonFeedback = true;

        [Tooltip("フィードバック用の色")]
        public Color feedbackColor = Color.yellow;

        [Tooltip("フィードバック表示時間")]
        public float feedbackDuration = 0.2f;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private bool _isInitialized = false;
        private Color _originalPrevColor;
        private Color _originalNextColor;
        private Color _originalOffColor;

        void Start()
        {
            InitializeSelector();
        }

        /// <summary>
        /// セレクターの初期化処理
        /// </summary>
        private void InitializeSelector()
        {
            // 必須コンポーネントのチェック
            if (targetMonitor == null)
            {
                LogError("CameraSelector: targetMonitorが設定されていません");
                return;
            }

            if (!ValidateUIComponents())
            {
                return;
            }

            // ボタンの初期色を保存
            SaveOriginalButtonColors();

            // UIの初期状態を設定
            UpdateUI();

            _isInitialized = true;

            LogDebug($"CameraSelector initialized for Monitor: {targetMonitor.GetMonitorName()}");
        }

        /// <summary>
        /// UIコンポーネントの検証
        /// </summary>
        /// <returns>全て正常な場合true</returns>
        private bool ValidateUIComponents()
        {
            bool isValid = true;

            if (previousButton == null)
            {
                LogError("CameraSelector: previousButtonが設定されていません");
                isValid = false;
            }

            if (nextButton == null)
            {
                LogError("CameraSelector: nextButtonが設定されていません");
                isValid = false;
            }

            if (offButton == null)
            {
                LogError("CameraSelector: offButtonが設定されていません");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// ボタンの初期色を保存
        /// </summary>
        private void SaveOriginalButtonColors()
        {
            if (enableButtonFeedback)
            {
                if (previousButtonImage != null)
                    _originalPrevColor = previousButtonImage.color;
                if (nextButtonImage != null)
                    _originalNextColor = nextButtonImage.color;
                if (offButtonImage != null)
                    _originalOffColor = offButtonImage.color;
            }
        }

        /// <summary>
        /// 前のカメラボタン押下時の処理
        /// </summary>
        public void OnPreviousButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(previousButton, _originalPrevColor);

            // カメラ切り替え実行
            targetMonitor.PreviousCamera();

            // UI更新
            UpdateUI();

            LogDebug($"Previous camera button clicked - Monitor: {targetMonitor.GetMonitorId()}");
        }

        /// <summary>
        /// 次のカメラボタン押下時の処理
        /// </summary>
        public void OnNextButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(nextButton, _originalNextColor);

            // カメラ切り替え実行
            targetMonitor.NextCamera();

            // UI更新
            UpdateUI();

            LogDebug($"Next camera button clicked - Monitor: {targetMonitor.GetMonitorId()}");
        }

        /// <summary>
        /// OFFボタン押下時の処理
        /// </summary>
        public void OnOffButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(offButton, _originalOffColor);

            // 表示OFF実行
            targetMonitor.SetDisplayOff();

            // UI更新
            UpdateUI();

            LogDebug($"Off button clicked - Monitor: {targetMonitor.GetMonitorId()}");
        }

        /// <summary>
        /// UI表示の更新
        /// </summary>
        private void UpdateUI()
        {
            if (targetMonitor == null) return;

            // 現在のカメラ名表示更新
            UpdateCameraNameDisplay();

            // モニター名表示更新
            UpdateMonitorNameDisplay();

            // ボタンの有効/無効状態更新
            UpdateButtonStates();
        }

        /// <summary>
        /// 現在のカメラ名表示の更新
        /// </summary>
        private void UpdateCameraNameDisplay()
        {
            if (currentCameraText == null) return;

            string displayText;
            if (targetMonitor.IsDisplayActive)
            {
                // アクティブなカメラ名を表示
                int currentIndex = targetMonitor.CurrentCameraIndex;
                if (currentIndex >= 0 && currentIndex < targetMonitor.cameraControllers.Length && targetMonitor.cameraControllers[currentIndex] != null)
                {
                    displayText = targetMonitor.cameraControllers[currentIndex].GetCameraName();
                }
                else
                {
                    displayText = $"Camera {currentIndex + 1}";
                }
            }
            else
            {
                displayText = "OFF";
            }

            currentCameraText.text = displayText;
        }

        /// <summary>
        /// モニター名表示の更新
        /// </summary>
        private void UpdateMonitorNameDisplay()
        {
            if (monitorNameText == null) return;

            monitorNameText.text = targetMonitor.GetMonitorName();
        }

        /// <summary>
        /// ボタンの有効/無効状態を更新
        /// </summary>
        private void UpdateButtonStates()
        {
            // カメラが1台もない場合はボタンを無効化
            bool hasValidCameras = targetMonitor.IsValidConfiguration() && targetMonitor.cameraControllers.Length > 0;

            if (previousButton != null)
                previousButton.interactable = hasValidCameras;

            if (nextButton != null)
                nextButton.interactable = hasValidCameras;

            if (offButton != null)
                offButton.interactable = hasValidCameras;
        }

        /// <summary>
        /// ボタン押下時の視覚的フィードバックを表示
        /// </summary>
        /// <param name="button">対象ボタン</param>
        /// <param name="originalColor">元の色</param>
        private void ShowButtonFeedback(Button button, Color originalColor)
        {
            if (!enableButtonFeedback || button == null) return;

            // 色を変更（Imageコンポーネントを直接参照）
            if (button == previousButton && previousButtonImage != null)
            {
                previousButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestorePreviousButtonColor), feedbackDuration);
            }
            else if (button == nextButton && nextButtonImage != null)
            {
                nextButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestoreNextButtonColor), feedbackDuration);
            }
            else if (button == offButton && offButtonImage != null)
            {
                offButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestoreOffButtonColor), feedbackDuration);
            }
        }

        /// <summary>
        /// 前ボタンの色を元に戻す（遅延実行用）
        /// </summary>
        public void _RestorePreviousButtonColor()
        {
            if (previousButtonImage != null)
                previousButtonImage.color = _originalPrevColor;
        }

        /// <summary>
        /// 次ボタンの色を元に戻す（遅延実行用）
        /// </summary>
        public void _RestoreNextButtonColor()
        {
            if (nextButtonImage != null)
                nextButtonImage.color = _originalNextColor;
        }

        /// <summary>
        /// OFFボタンの色を元に戻す（遅延実行用）
        /// </summary>
        public void _RestoreOffButtonColor()
        {
            if (offButtonImage != null)
                offButtonImage.color = _originalOffColor;
        }

        /// <summary>
        /// セレクターの状態情報を取得
        /// </summary>
        /// <returns>状態情報文字列</returns>
        public string GetStatusInfo()
        {
            if (targetMonitor == null)
                return "CameraSelector: No target monitor";

            return $"CameraSelector for {targetMonitor.GetStatusInfo()}";
        }

        /// <summary>
        /// セレクターの設定が有効かチェック
        /// </summary>
        /// <returns>設定が有効な場合true</returns>
        public bool IsValidConfiguration()
        {
            return _isInitialized && targetMonitor != null && targetMonitor.IsValidConfiguration() &&
                   previousButton != null && nextButton != null && offButton != null;
        }

        /// <summary>
        /// 手動でUI更新を実行（外部から呼び出し可能）
        /// </summary>
        public void RefreshUI()
        {
            UpdateUI();
        }

        #region Debug Logging
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[CameraSelector] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[CameraSelector] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CameraSelector] {message}");
        }
        #endregion

        #region Editor Support
        private void OnValidate()
        {
            // Editorでの設定検証
            if (feedbackDuration <= 0)
            {
                feedbackDuration = 0.2f;
            }
        }
        #endregion
    }
}