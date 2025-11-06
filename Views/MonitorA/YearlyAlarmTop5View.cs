using HNS.MonitorA.Models;
using HNS.MonitorA.ViewModels;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HNS.MonitorA.Views
{
    public class YearlyAlarmTop5View : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private List<YearlyAlarmRankingItemView> rankingItems;

        private void Start()
        {
            // Singleton Instance로 접근
            if (YearlyAlarmTop5ViewModel.Instance == null)
            {
                Debug.LogError("[YearlyAlarmTop5View] YearlyAlarmTop5ViewModel.Instance가 null입니다!");
                return;
            }

            // 제목 초기화
            UpdateTitle();

            // ViewModel 이벤트 구독
            YearlyAlarmTop5ViewModel.Instance.OnTitleUpdated += UpdateTitle;
            YearlyAlarmTop5ViewModel.Instance.OnRankingUpdated += UpdateRanking;

            Debug.Log("[YearlyAlarmTop5View] 초기화 완료");
        }

        private void OnDestroy()
        {
            if (YearlyAlarmTop5ViewModel.Instance != null)
            {
                YearlyAlarmTop5ViewModel.Instance.OnTitleUpdated -= UpdateTitle;
                YearlyAlarmTop5ViewModel.Instance.OnRankingUpdated -= UpdateRanking;
            }
        }

        private void UpdateTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
                Debug.Log($"[YearlyAlarmTop5View] 제목 업데이트: {title}");
            }
        }

        private void UpdateTitle()
        {
            if (titleText != null)
            {
                int currentYear = DateTime.Now.Year % 100;
                titleText.text = $"연간 발생 건수 ({currentYear}년)";
            }
        }

        private void UpdateRanking(List<YearlyAlarmRankingData> rankings)
        {
            Debug.Log($"[YearlyAlarmTop5View] 랭킹 업데이트: {rankings?.Count ?? 0}개");

            if (rankings == null || rankingItems == null)
            {
                Debug.LogWarning("[YearlyAlarmTop5View] rankings 또는 rankingItems가 null입니다!");
                return;
            }

            for (int i = 0; i < rankingItems.Count; i++)
            {
                if (i < rankings.Count)
                {
                    rankingItems[i].UpdateData(rankings[i]);
                    rankingItems[i].gameObject.SetActive(true);
                }
                else
                {
                    rankingItems[i].gameObject.SetActive(false);
                }
            }
        }
    }
}