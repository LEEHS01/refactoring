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

        // 2. 모든 ViewModel 초기화 대기
        yield return InitializeViewModels();

        // 3. 기타 서비스 초기화
        yield return InitializeServices();

        IsInitialized = true;
        OnInitializationComplete?.Invoke();

        Debug.Log("[AppInitializer] 초기화 완료");
    }

    private IEnumerator InitializeViewModels()
    {
        // AlarmLogViewModel 대기
        while (AlarmLogViewModel.Instance == null)
            yield return null;

        // TimeViewModel 대기
        while (TimeViewModel.Instance == null)
            yield return null;

        // 추가 ViewModel들...

        Debug.Log("[AppInitializer] 모든 ViewModel 준비 완료");
    }

    private IEnumerator InitializeServices()
    {
        // 추가 서비스 초기화
        yield return null;
    }
}