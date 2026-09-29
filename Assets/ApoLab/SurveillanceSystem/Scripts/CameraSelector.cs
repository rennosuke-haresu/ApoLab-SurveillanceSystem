using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using TMPro;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// カメラ切り替えUI制御クラス - シンプルな前後切り替えインターフェース
    /// [◀][▶][OFF]の3ボタンでカメラを簡単に切り替えます
    /// ※ カメラ名の表示は MonitorDisplay 側（Camera Name Text）が担当します
    /// </summary>
    // SyncMode None の UdonBehaviour にはネットワークイベントが届かないため、ボタン用メソッド（On*ButtonClick）を
    // `_` 接頭辞なしで公開しても他プレイヤーからは呼べない。同期モードを変える場合は接頭辞と NetworkCallable の扱いを見直すこと
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CameraSelector : UdonSharpBehaviour
    {
        [Header("Target")]
        [Tooltip("Monitor display to control\n制御対象のモニターディスプレイ")]
        public MonitorDisplay targetMonitor;

        [Header("Buttons")]
        [Tooltip("Previous camera button [◀]\n前のカメラボタン [◀]")]
        public Button previousButton;

        [Tooltip("Image of the previous button (for color feedback)\n前のカメラボタンのImageコンポーネント（色変更用）")]
        public Image previousButtonImage;

        [Tooltip("Next camera button [▶]\n次のカメラボタン [▶]")]
        public Button nextButton;

        [Tooltip("Image of the next button (for color feedback)\n次のカメラボタンのImageコンポーネント（色変更用）")]
        public Image nextButtonImage;

        [Tooltip("Display off button [OFF]\n表示OFFボタン [OFF]")]
        public Button offButton;

        [Tooltip("Image of the off button (for color feedback)\nOFFボタンのImageコンポーネント（色変更用）")]
        public Image offButtonImage;

        [Header("Labels")]
        [Tooltip("TextMeshPro that shows the monitor name\nモニター名を表示するTextMeshPro")]
        public TextMeshProUGUI monitorNameText;

        [Header("Button Feedback")]
        [Tooltip("Change the button color when pressed\nボタン押下時の色変更")]
        public bool enableButtonFeedback = true;

        [Tooltip("Feedback color\nフィードバック用の色")]
        public Color feedbackColor = Color.yellow;

        [Tooltip("Feedback duration (seconds)\nフィードバック表示時間")]
        public float feedbackDuration = 0.2f;

        [Header("Debug")]
        [Tooltip("Output debug logs\nデバッグログを出力するかどうか")]
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
                LogError("CameraSelector: targetMonitor is not set");
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

            if (enableDebugLog)
            {
                LogDebug($"CameraSelector initialized for Monitor: {targetMonitor.GetMonitorName()}");
            }
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
                LogError("CameraSelector: previousButton is not set");
                isValid = false;
            }

            if (nextButton == null)
            {
                LogError("CameraSelector: nextButton is not set");
                isValid = false;
            }

            if (offButton == null)
            {
                LogError("CameraSelector: offButton is not set");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// ボタンの初期色を保存
        /// </summary>
        private void SaveOriginalButtonColors()
        {
            // 実行中に enableButtonFeedback をオンにしても色を戻せるよう、設定にかかわらず保存する
            if (previousButtonImage != null)
                _originalPrevColor = previousButtonImage.color;
            if (nextButtonImage != null)
                _originalNextColor = nextButtonImage.color;
            if (offButtonImage != null)
                _originalOffColor = offButtonImage.color;
        }

        /// <summary>
        /// 前のカメラボタン押下時の処理（ボタンの OnClick から呼び出し）
        /// </summary>
        public void OnPreviousButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(previousButton);

            // カメラ切り替え実行
            targetMonitor._PreviousCamera();

            // UI更新
            UpdateUI();

            if (enableDebugLog)
            {
                LogDebug($"Previous camera button clicked - Monitor: {targetMonitor.GetMonitorId()}");
            }
        }

        /// <summary>
        /// 次のカメラボタン押下時の処理（ボタンの OnClick から呼び出し）
        /// </summary>
        public void OnNextButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(nextButton);

            // カメラ切り替え実行
            targetMonitor._NextCamera();

            // UI更新
            UpdateUI();

            if (enableDebugLog)
            {
                LogDebug($"Next camera button clicked - Monitor: {targetMonitor.GetMonitorId()}");
            }
        }

        /// <summary>
        /// OFFボタン押下時の処理（ボタンの OnClick から呼び出し）
        /// </summary>
        public void OnOffButtonClick()
        {
            if (!_isInitialized || targetMonitor == null) return;

            // 視覚的フィードバック
            ShowButtonFeedback(offButton);

            // 表示OFF実行
            targetMonitor._SetDisplayOff();

            // UI更新
            UpdateUI();

            if (enableDebugLog)
            {
                LogDebug($"Off button clicked - Monitor: {targetMonitor.GetMonitorId()}");
            }
        }

        /// <summary>
        /// UI表示の更新
        /// </summary>
        private void UpdateUI()
        {
            if (targetMonitor == null) return;

            // モニター名表示更新
            UpdateMonitorNameDisplay();

            // ボタンの有効/無効状態更新
            UpdateButtonStates();
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
            bool hasValidCameras = targetMonitor.HasCameras();

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
        private void ShowButtonFeedback(Button button)
        {
            if (!enableButtonFeedback || button == null) return;

            // 0 以下だと色が戻る前に次の押下と重なるため下限を設ける
            float duration = Mathf.Max(feedbackDuration, 0.05f);

            // 色を変更（Imageコンポーネントを直接参照）
            if (button == previousButton && previousButtonImage != null)
            {
                previousButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestorePreviousButtonColor), duration);
            }
            else if (button == nextButton && nextButtonImage != null)
            {
                nextButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestoreNextButtonColor), duration);
            }
            else if (button == offButton && offButtonImage != null)
            {
                offButtonImage.color = feedbackColor;
                SendCustomEventDelayedSeconds(nameof(_RestoreOffButtonColor), duration);
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
        /// 手動でUI更新を実行（SurveillanceManagerから呼び出し）
        /// </summary>
        public void _RefreshUI()
        {
            UpdateUI();
        }

        #region Debug Logging
        // 補間文字列を渡す場合は、呼び出し側で enableDebugLog をガードすること
        // （false でも文字列生成のコストが発生するため）
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[CameraSelector] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CameraSelector] {message}");
        }
        #endregion
    }
}
