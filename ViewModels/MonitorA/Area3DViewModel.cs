using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using ViewModels.MonitorB;
using Views.MonitorA;

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
        [HideInInspector] public UnityEvent<int> OnObservatoryLoaded = new UnityEvent<int>();

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
            Debug.Log("========================================");
            Debug.Log($"[Area3DViewModel] ✅ Monitor B 알람 선택 감지 → ObsId={obsId}");

            var alarmData = AlarmLogViewModel.Instance?.AllLogs?.Find(a => a.obsId == obsId);

            if (alarmData == null)
            {
                Debug.LogError($"[Area3DViewModel] 알람 데이터를 찾을 수 없음: obsId={obsId}");
                return;
            }

            // ⭐⭐⭐ areaName으로 areaId 찾기!
            int areaId = GetAreaIdByName(alarmData.areaName);

            if (areaId < 1 || areaId > 10)
            {
                Debug.LogError($"[Area3DViewModel] ❌ 잘못된 지역 ID: {areaId}, areaName={alarmData.areaName}");
                return;
            }

            Debug.Log($"[Area3DViewModel] ObsId={obsId} → AreaName={alarmData.areaName} → AreaId={areaId}");

            // 화면 전환 코루틴 시작
            StartCoroutine(TransitionToObservatoryCoroutine(areaId, obsId, alarmData.areaName, alarmData.obsName));
        }

        #region Public Methods
        public void LoadObservatory(int obsId)
        {
            LoadObservatory(obsId, "", "");
        }

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

            OnObservatoryLoaded?.Invoke(obsId);
            OnObservatoryLoadedWithNames?.Invoke(obsId, areaName, obsName);
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

        #region Private Methods - 화면 전환

        /// <summary>
        /// 지역명으로 지역 ID 찾기
        /// </summary>
        private int GetAreaIdByName(string areaName)
        {
            // 띄어쓰기 제거 후 비교
            string normalizedName = areaName.Replace(" ", "").Trim();

            var areaMapping = new Dictionary<string, int>
    {
        // ⭐ TB_AREA의 실제 AREANM 값 (이미지 1, 7 참조)
        { "인천", 1 },
        { "평택/대산", 2 },
        { "여수/광양", 3 },
        { "부산", 4 },
        { "울산", 5 },
        { "보령화력", 6 },
        { "영광원자력", 7 },
        { "사천화력", 8 },
        { "고리원자력", 9 },
        { "동해화력", 10 },
        
        // ⭐ 띄어쓰기 없는 버전도 지원
        { "평택대산", 2 },
        { "여수광양", 3 },
        { "보령", 6 },
        { "영광", 7 },
        { "사천", 8 },
        { "고리", 9 },
        { "동해", 10 }
    };

            if (areaMapping.TryGetValue(normalizedName, out int areaId))
            {
                Debug.Log($"[Area3DViewModel] ✅ 지역 매칭 성공: '{areaName}' → AreaId={areaId}");
                return areaId;
            }

            Debug.LogError($"[Area3DViewModel] ❌ 알 수 없는 지역명: '{areaName}' (정규화: '{normalizedName}')");
            return 0;
        }

        /// <summary>
        /// Monitor B 알람 선택 시 Monitor A 화면 자동 전환
        /// </summary>
        private IEnumerator TransitionToObservatoryCoroutine(int areaId, int obsId, string areaName, string obsName)
        {
            Debug.Log($"[Area3DViewModel] 화면 전환 시작: AreaId={areaId}, ObsId={obsId}");

            // Step 0: 기존 3D 화면 닫기
            if (_isObservatoryActive)
            {
                Debug.Log("[Area3DViewModel] Step 0: 기존 3D 화면 닫기");
                CloseObservatory();
                yield return new WaitForSeconds(0.2f);
            }

            // Step 1: MapNationView 찾기
            var mapNationView = FindObjectOfType<MapNationView>();
            if (mapNationView == null)
            {
                Debug.LogError("[Area3DViewModel] ❌ MapNationView를 찾을 수 없습니다!");
                yield break;
            }

            // Step 2: 전국 지도 축소
            Debug.Log("[Area3DViewModel] Step 1: 전국 지도 축소");
            mapNationView.SwitchToMinimapMode();
            yield return new WaitForSeconds(0.1f);

            // Step 3: MapAreaViewModel 확인
            if (MapAreaViewModel.Instance == null)
            {
                Debug.LogError("[Area3DViewModel] ❌ MapAreaViewModel.Instance가 null!");
                yield break;
            }

            // Step 4: 지역 데이터 로드
            Debug.Log($"[Area3DViewModel] Step 2: 지역 데이터 로드 (AreaId={areaId})");
            MapAreaViewModel.Instance.LoadAreaData(areaId);
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Area3DViewModel] 지역 데이터 로드 대기 완료");

            // ⭐⭐⭐ Step 4.5: MapAreaView 숨김 처리 추가!
            var mapAreaView = FindFirstObjectByType<HNS.MonitorA.Views.MapAreaView>();
            if (mapAreaView != null)
            {
                Debug.Log("[Area3DViewModel] Step 2.5: 지역 지도 숨김");
                // MapAreaView의 CanvasGroup을 숨김
                var canvasGroup = mapAreaView.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                    Debug.Log("[Area3DViewModel] ✅ 지역 지도 숨김 완료");
                }
            }
            else
            {
                Debug.LogWarning("[Area3DViewModel] MapAreaView를 찾을 수 없습니다!");
            }

            // Step 5: 3D 관측소 화면 전환
            Debug.Log($"[Area3DViewModel] Step 3: 3D 관측소 화면 전환 (ObsId={obsId})");
            LoadObservatory(obsId, areaName, obsName);
            yield return new WaitForSeconds(0.1f);

            Debug.Log("[Area3DViewModel] ✅ Monitor A 화면 전환 완료!");
            Debug.Log("========================================");
        }

        #endregion
    }
}