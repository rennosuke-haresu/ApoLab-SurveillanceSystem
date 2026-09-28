using UnityEngine;
using UdonSharp;

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
        [Header("ボタン設定")]
        [Tooltip("操作するモニター")]
        public MonitorDisplay targetMonitor;

        [Tooltip("押したときの動作")]
        public MonitorButtonAction action = MonitorButtonAction.Next;

        [Header("押し込み")]
        [Tooltip("押し込むオブジェクト（空ならこのオブジェクト）")]
        public Transform pressTarget;

        [Tooltip("押し込む量（ローカル座標）")]
        public Vector3 pressOffset = new Vector3(0f, 0f, 0.004f);

        [Tooltip("押し込んでいる時間（秒）")]
        public float pressDuration = 0.15f;

        [Header("デバッグ設定")]
        [Tooltip("デバッグログを出力するかどうか")]
        public bool enableDebugLog = false;

        // 内部状態管理
        private Vector3 _restPosition;
        private int _pendingReleases = 0;

        void Start()
        {
            if (pressTarget == null) pressTarget = transform;
            _restPosition = pressTarget.localPosition;

            if (targetMonitor == null)
            {
                LogError($"{gameObject.name}: targetMonitorが設定されていません");
            }
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
            _pendingReleases--;
            if (_pendingReleases > 0) return;
            _pendingReleases = 0;
            pressTarget.localPosition = _restPosition;
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
