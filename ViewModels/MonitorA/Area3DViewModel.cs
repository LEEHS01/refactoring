using UnityEngine;
using UnityEngine.Events;
using System;
using ViewModels.MonitorB;

namespace HNS.MonitorA.ViewModels
{
    public class Area3DViewModel : MonoBehaviour
    {
        #region Singleton
        public static Area3DViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[Area3DViewModel] 인스턴스 생성");
            }
            else
            {
                Destroy(gameObject);
                Debug.LogWarning("[Area3DViewModel] 중복 인스턴스 제거");
            }
        }
        #endregion

        #region Events
        // ⭐⭐⭐ 기존 이벤트 유지 (Monitor A용 - 호환성)
        [HideInInspector] public UnityEvent<int> OnObservatoryLoaded = new UnityEvent<int>();

        // ⭐⭐⭐ 새 이벤트 추가 (Monitor B용 - 지역명/관측소명 포함)
        [Serializable]
        public class ObservatoryLoadedWithNamesEvent : UnityEvent<int, string, string> { }
        [HideInInspector] public ObservatoryLoadedWithNamesEvent OnObservatoryLoadedWithNames = new ObservatoryLoadedWithNamesEvent();

        [HideInInspector] public UnityEvent OnObservatoryClosed = new UnityEvent();
        [HideInInspector] public UnityEvent<string> OnError = new UnityEvent<string>();
        #endregion

        #region Private Fields
        private int _currentObsId = -1;
        private string _currentAreaName = "";
        private string _currentObsName = "";
        private bool _isObservatoryActive = false;
        #endregion

        #region Properties
        public int CurrentObsId => _currentObsId;
        public bool IsObservatoryActive => _isObservatoryActive;
        #endregion

        private void Start()
        {
            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.AddListener(OnAlarmSelected);
                Debug.Log("[Area3DViewModel] ✅ AlarmLogViewModel 구독 완료");
            }
            else
            {
                Debug.LogWarning("[Area3DViewModel] AlarmLogViewModel.Instance가 null!");
            }
        }

        private void OnDestroy()
        {
            if (AlarmLogViewModel.Instance != null)
            {
                AlarmLogViewModel.Instance.OnAlarmSelected.RemoveListener(OnAlarmSelected);
                Debug.Log("[Area3DViewModel] AlarmLogViewModel 구독 해제");
            }
        }

        private void OnAlarmSelected(int obsId)
        {
            Debug.Log($"[Area3DViewModel] ✅ Monitor B 알람 선택 감지 → ObsId={obsId}");

            var alarmData = AlarmLogViewModel.Instance?.AllLogs?.Find(a => a.obsId == obsId);

            if (alarmData != null)
            {
                LoadObservatory(obsId, alarmData.areaName, alarmData.obsName);
            }
            else
            {
                Debug.LogWarning($"[Area3DViewModel] 알람 데이터를 찾을 수 없음: obsId={obsId}");
                LoadObservatory(obsId, "", "");
            }
        }

        #region Public Methods
        // ⭐ 버전 1: 기존 호환 (obsId만)
        public void LoadObservatory(int obsId)
        {
            LoadObservatory(obsId, "", "");
        }

        // ⭐ 버전 2: 완전한 버전
        public void LoadObservatory(int obsId, string areaName, string obsName)
        {
            if (obsId <= 0)
            {
                OnError?.Invoke($"잘못된 관측소 ID: {obsId}");
                return;
            }

            _currentObsId = obsId;
            _currentAreaName = areaName;
            _currentObsName = obsName;
            _isObservatoryActive = true;

            Debug.Log($"[Area3DViewModel] 관측소 로드: ObsId={obsId}, Area={areaName}, Obs={obsName}");

            // ⭐⭐⭐ 두 이벤트 모두 발생!
            OnObservatoryLoaded?.Invoke(obsId);  // Monitor A용 (기존)
            OnObservatoryLoadedWithNames?.Invoke(obsId, areaName, obsName);  // Monitor B용 (새)
        }

        public void CloseObservatory()
        {
            Debug.Log($"[Area3DViewModel] 관측소 닫기: ObsId={_currentObsId}");

            _currentObsId = -1;
            _currentAreaName = "";
            _currentObsName = "";
            _isObservatoryActive = false;

            OnObservatoryClosed?.Invoke();
        }
        #endregion
    }
}