using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

namespace ApoLab.SurveillanceSystem
{
    /// <summary>
    /// 立体ボタンの動作
    /// </summary>
    public enum MonitorButtonAction
    {
        Previous,
        Next,
        Off
    }

    /// <summary>
    /// 立体ボタン - Interact でモニターのカメラを切り替える
    /// Collider を付けたオブジェクトに追加し、Target Monitor と Action を設定します
    /// ※ 表示状態はプレイヤーごとにローカルで、他プレイヤーには同期されません
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MonitorButton : UdonSharpBehaviour
    {
        [Header("Button")]
        [Tooltip("Monitor to control\n操作するモニター")]
        public MonitorDisplay targetMonitor;

        [Tooltip("Action when pressed\n押したときの動作")]
        public MonitorButtonAction action = MonitorButtonAction.Next;

        [Tooltip("Show the Interaction Text in the player's language (EN/JA/KO/ZH). While on, the Interaction Text field is not used\nプレイヤーの言語に合わせて Interaction Text を切り替える（英・日・韓・中）。ON の間は Interaction Text の欄は使われない")]
        public bool autoLocalize = false;

        [Header("Press Animation")]
        [Tooltip("Object to push in (empty = this object)\n押し込むオブジェクト（空ならこのオブジェクト）")]
        public Transform pressTarget;

        [Tooltip("Push offset (local space)\n押し込む量（ローカル座標）")]
        public Vector3 pressOffset = new Vector3(0f, 0f, 0.004f);

        [Tooltip("How long it stays pushed (seconds)\n押し込んでいる時間（秒）")]
        public float pressDuration = 0.15f;

        [Header("Debug")]
        [Tooltip("Output debug logs\nデバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private Vector3 _restPosition;
        private int _pendingReleases = 0;
        private bool _initialized = false;

        void Start()
        {
            if (pressTarget == null) pressTarget = transform;
            _restPosition = pressTarget.localPosition;
            _initialized = true;

            if (targetMonitor == null)
            {
                LogError($"{gameObject.name}: targetMonitor is not set");
                // 押しても何も起きないボタンに Use の表示を出さない
                DisableInteractive = true;
            }

            if (autoLocalize) ApplyLocalizedText(VRCPlayerApi.GetCurrentLanguage());
        }

        void OnDisable()
        {
            // 押し込み中に非アクティブになると戻す遅延イベントが届かないため、ここで戻す
            if (!_initialized) return;
            _pendingReleases = 0;
            pressTarget.localPosition = _restPosition;
        }

        public override void OnLanguageChanged(string language)
        {
            if (autoLocalize) ApplyLocalizedText(language);
        }

        public override void Interact()
        {
            if (targetMonitor == null) return;

            switch (action)
            {
                case MonitorButtonAction.Previous:
                    targetMonitor._PreviousCamera();
                    break;
                case MonitorButtonAction.Next:
                    targetMonitor._NextCamera();
                    break;
                case MonitorButtonAction.Off:
                    targetMonitor._SetDisplayOff();
                    break;
            }

            Press();

            if (enableDebugLog)
            {
                LogDebug($"{gameObject.name}: {action} - Monitor: {targetMonitor.GetMonitorId()}");
            }
        }

        /// <summary>
        /// ボタンを押し込み、一定時間後に戻す
        /// </summary>
        private void Press()
        {
            pressTarget.localPosition = _restPosition + pressOffset;
            _pendingReleases++;
            // 0 以下だと戻る前に次の押下と重なるため下限を設ける
            SendCustomEventDelayedSeconds(nameof(_Release), Mathf.Max(pressDuration, 0.05f));
        }

        /// <summary>
        /// 押し込みを戻す（遅延実行用）。連打中は最後の押下の分で戻す
        /// </summary>
        public void _Release()
        {
            // OnDisable で戻したあとに残りのイベントが届いた場合も、ここで 0 に丸めて戻す
            _pendingReleases--;
            if (_pendingReleases > 0) return;
            _pendingReleases = 0;
            pressTarget.localPosition = _restPosition;
        }

        /// <summary>
        /// 言語と動作に合った文言を Interaction Text に入れる。表にない言語は英語
        /// </summary>
        private void ApplyLocalizedText(string language)
        {
            switch (language)
            {
                case "ja": InteractionText = PickText("前へ", "次へ", "オフ"); break;
                case "ko": InteractionText = PickText("이전", "다음", "끄기"); break;
                case "zh-CN": InteractionText = PickText("上一个", "下一个", "关闭"); break;
                case "zh-HK": InteractionText = PickText("上一個", "下一個", "關閉"); break;
                default: InteractionText = PickText("Previous", "Next", "Off"); break;
            }
        }

        private string PickText(string previous, string next, string off)
        {
            switch (action)
            {
                case MonitorButtonAction.Previous: return previous;
                case MonitorButtonAction.Off: return off;
                default: return next;
            }
        }

        #region Debug Logging
        // 補間文字列を渡す場合は、呼び出し側で enableDebugLog をガードすること
        // （false でも文字列生成のコストが発生するため）
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[MonitorButton] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MonitorButton] {message}");
        }
        #endregion
    }
}
