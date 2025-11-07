using Assets.Scripts_refactoring.ViewModels.MonitorA;
using HNS.MonitorA.ViewModels;
using Services;
using System.Collections;
using UnityEngine;
using ViewModels.Common;
using ViewModels.MonitorB;

public class AppInitializer : MonoBehaviour
{
    public static AppInitializer Instance { get; private set; }
    public static bool IsInitialized { get; private set; }

    public static event System.Action OnInitializationComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitializeApp());
    }

    private IEnumerator InitializeApp()
    {
        Debug.Log("[AppInitializer] 초기화 시작");

        // 1. DatabaseService 대기
        while (DatabaseService.Instance == null)
            yield return null;

        // 2. 모든 ViewModel 초기화 (자동 생성)
        yield return InitializeViewModels();

        // 3. 기타 서비스 초기화
        yield return InitializeServices();

        IsInitialized = true;
        OnInitializationComplete?.Invoke();

        Debug.Log("[AppInitializer] 초기화 완료");
    }

    /// <summary>
    /// 모든 ViewModel을 자동으로 생성하고 초기화
    /// </summary>
    private IEnumerator InitializeViewModels()
    {
        Debug.Log("[AppInitializer] ViewModel 자동 생성 시작...");

        // Monitor B ViewModel들
        CreateViewModel<AlarmLogViewModel>("AlarmLogViewModel");
        CreateViewModel<TimeViewModel>("TimeViewModel");
        CreateViewModel<SensorMonitorViewModel>("SensorMonitorViewModel");
        CreateViewModel<SensorChartViewModel>("SensorChartViewModel");
        CreateViewModel<AIAnalysisViewModel>("AIAnalysisViewModel");
        CreateViewModel<AlarmDetailViewModel>("AlarmDetailViewModel");
        CreateViewModel<PopUpToxinDetail2ViewModel>("PopUpToxinDetail2ViewModel");
        CreateViewModel<CCTVViewModel>("CCTVViewModel");

        // Monitor A ViewModel들 ← 추가!
        CreateViewModel<MonthlyAlarmTop5ViewModel>("MonthlyAlarmTop5ViewModel");
        CreateViewModel<YearlyAlarmTop5ViewModel>("YearlyAlarmTop5ViewModel");
        CreateViewModel<AreaListTypeViewModel>("AreaListTypeViewModel");
        CreateViewModel<MapNationViewModel>("MapNationViewModel");

        // 모든 ViewModel이 준비될 때까지 대기
        while (AlarmLogViewModel.Instance == null ||
               TimeViewModel.Instance == null ||
               SensorMonitorViewModel.Instance == null ||
               SensorChartViewModel.Instance == null ||
               AIAnalysisViewModel.Instance == null ||
               AlarmDetailViewModel.Instance == null ||
               PopUpToxinDetail2ViewModel.Instance == null ||
               CCTVViewModel.Instance == null ||
               MonthlyAlarmTop5ViewModel.Instance == null ||
               YearlyAlarmTop5ViewModel.Instance == null ||
               AreaListTypeViewModel.Instance == null ||
               MapNationViewModel.Instance == null)  
        {
            yield return null;
        }

        Debug.Log("[AppInitializer] 모든 ViewModel 준비 완료");
        Debug.Log($"  - AlarmLogViewModel: {AlarmLogViewModel.Instance != null}");
        Debug.Log($"  - TimeViewModel: {TimeViewModel.Instance != null}");
        Debug.Log($"  - SensorMonitorViewModel: {SensorMonitorViewModel.Instance != null}");
        Debug.Log($"  - PopUpToxinDetail2ViewModel: {PopUpToxinDetail2ViewModel.Instance != null}");
        Debug.Log($"  - MonthlyAlarmTop5ViewModel: {MonthlyAlarmTop5ViewModel.Instance != null}");  // ← 추가!
    }

    /// <summary>
    /// ViewModel을 자동으로 생성하는 헬퍼 메서드
    /// </summary>
    private void CreateViewModel<T>(string name) where T : MonoBehaviour
    {
        // 이미 존재하는지 확인 (중복 방지)
        if (FindFirstObjectByType<T>() != null)
        {
            Debug.Log($"[AppInitializer] {name} 이미 존재함 - 건너뜀");
            return;
        }

        // 새로운 GameObject 생성
        GameObject viewModelObject = new GameObject(name);

        // ViewModel 컴포넌트 추가
        viewModelObject.AddComponent<T>();

        // Scene 전환 시에도 유지
        DontDestroyOnLoad(viewModelObject);

        Debug.Log($"[AppInitializer] {name} 자동 생성 완료");
    }

    private IEnumerator InitializeServices()
    {
        // 추가 서비스 초기화
        yield return null;
    }
}