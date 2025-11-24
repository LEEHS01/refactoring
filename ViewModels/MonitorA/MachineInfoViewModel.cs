using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HNS.MonitorA.ViewModels
{
    /// <summary>
    /// 기계(센서/장비) 제원 정보 ViewModel (Singleton)
    /// STCD(상태 코드) 기반 장비 상태 관리
    /// </summary>
    public class MachineInfoViewModel : MonoBehaviour
    {
        #region Singleton

        public static MachineInfoViewModel Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MachineInfoViewModel] Singleton 등록 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 기계 정보 팝업 열기 이벤트 (stcd: 상태 코드)
        /// </summary>
        [Serializable]
        public class MachineInfoOpenedEvent : UnityEvent<string> { }
        [HideInInspector] public MachineInfoOpenedEvent OnMachineInfoOpened = new MachineInfoOpenedEvent();

        /// <summary>
        /// 팝업 닫기 이벤트
        /// </summary>
        [Serializable]
        public class MachineInfoClosedEvent : UnityEvent { }
        [HideInInspector] public MachineInfoClosedEvent OnMachineInfoClosed = new MachineInfoClosedEvent();

        #endregion

        #region STCD 상태 코드 정의

        // STCD(상태 코드) 설명 딕셔너리
        private Dictionary<string, string> stcdDescriptions = new Dictionary<string, string>()
        {
            { "00", "정상" },      // 정상 작동
            { "03", "점검중" },    // 점검/유지보수 중
            { "06", "불량" }       // 장비 불량/고장
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// 기계 정보 팝업 열기
        /// </summary>
        /// <param name="stcd">상태 코드 (예: "00", "03", "06")</param>
        public void OpenMachineInfo(string stcd)
        {
            if (string.IsNullOrEmpty(stcd))
            {
                stcd = "00";  // 기본값: 정상
            }

            OnMachineInfoOpened?.Invoke(stcd);
            Debug.Log($"[MachineInfoViewModel] 기계 정보 팝업 열기: STCD={stcd} ({GetStcdDescription(stcd)})");
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void CloseMachineInfo()
        {
            OnMachineInfoClosed?.Invoke();
            Debug.Log("[MachineInfoViewModel] 기계 정보 팝업 닫기");
        }

        /// <summary>
        /// STCD 코드를 한글 설명으로 변환
        /// </summary>
        public string GetStcdDescription(string stcd)
        {
            if (stcdDescriptions.TryGetValue(stcd, out string description))
            {
                return description;
            }
            return "알 수 없는 상태";
        }

        #endregion
    }
}