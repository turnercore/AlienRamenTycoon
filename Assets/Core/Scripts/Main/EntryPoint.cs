using System.Collections;
using System.Collections.Generic;
using Project;
using Server;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    /// <summary>
    /// The main boot class which is initializing the project, loads the platforms settings, initializes the platforms and creates the game states
    /// </summary>
    public sealed class EntryPoint : MonoBehaviour
    {
        // Authoring
        public AssetReferenceT<InitializerSettingsFile> initializerSettingsFile;

        // Globals
        private IPlatform platform;
        private ApplicationData applicationData;
        private Dictionary<ApplicationState, IApplicationState> applicationStates;
        private ProfilerMarker createApplicationStateMarker = new("createApplicationState");

        // Settings
        private InitializerSettingsFile initializerSettings;
        private SplashBootSettings splashBootSettings;
        private MainMenuBootSettings mainMenuBootSettings;
        private GameModeBootSettings gameModeBootSettings;
        private MenuApplicationStateData menuApplicationStateData;

        private readonly OptionsData optionsData = new();

        // Network
        private INetworkService networkService;

        /// <summary>
        /// The is the first method called when the game starts. It will load the Initializer prefab which will initialize the project
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Load options data
#if UNITY_EDITOR
            if (BootMode.BootType == BootType.UnityDefault)
            {
                return;
            }
#endif
            var entryPoint = FindFirstObjectByType<EntryPoint>(FindObjectsInactive.Include);
            if (entryPoint == null)
            {
                Addressables.InstantiateAsync("Initializer");
            }
        }

        /// <summary>
        /// Main application initialization coroutine
        /// </summary>
        public IEnumerator Start()
        {
            DontDestroyOnLoad(this);
            optionsData.Load();
            EnsureEventSystem();

            // scene authoring is helping with making each scene stand-alone playable
            SceneReference activeSceneReference = FindFirstObjectByType<SceneReference>();
            if (activeSceneReference == null)
            {
                Debug.LogWarning(
                    "The scene has no SceneReference script. Will start by default in Splash Application State"
                );
                GameObject sceneRef = new GameObject { name = "Scene Reference" };
                activeSceneReference = sceneRef.AddComponent<SceneReference>();
                activeSceneReference.applicationState = ApplicationState.Splash;
                yield return null;
            }

            // Initialize platform selector
            applicationData = new ApplicationData
            {
                platformSelector = new PlatformSelector(
                    PlatformSelector.GetDevicePlatform(),
                    PlatformSelector.GetPlatformDefaultInputMode()
                ),
            };
            applicationData.ChangeApplicationState(activeSceneReference.applicationState);

            // Load Initialization Settings
            AsyncOperationHandle<InitializerSettingsFile> initSettingsHandle =
                Addressables.LoadAssetAsync<InitializerSettingsFile>(initializerSettingsFile);
            yield return new WaitUntil(() => initSettingsHandle.IsDone);
            initializerSettings = initSettingsHandle.Result;

            Debug.Log("InitializerSettingsFile loaded");

            if (initializerSettings == null)
            {
                Debug.LogError(
                    "InitializerSettingsFile is null! Check if initializerSettingsFile reference is set in EntryPoint inspector."
                );
                yield break;
            }

            StartCoroutine(SetUpNetworking());

            yield return CreatePlatformFactory();

            // Load all boot settings
            var splashBootSettingsHandle = Addressables.LoadAssetAsync<SplashBootSettings>(
                initializerSettings.splashBootSettings
            );
            var mainMenuBootSettingsHandle = Addressables.LoadAssetAsync<MainMenuBootSettings>(
                initializerSettings.mainMenuBootSettings
            );
            var gameModeBootSettingsHandle = Addressables.LoadAssetAsync<GameModeBootSettings>(
                initializerSettings.gameModeBootSettings
            );

            yield return new WaitUntil(() =>
                splashBootSettingsHandle.IsDone
                && mainMenuBootSettingsHandle.IsDone
                && gameModeBootSettingsHandle.IsDone
            );
            splashBootSettings = splashBootSettingsHandle.Result;
            mainMenuBootSettings = mainMenuBootSettingsHandle.Result;
            gameModeBootSettings = gameModeBootSettingsHandle.Result;

            // We initialize the Application State Runner which will run the game states
            menuApplicationStateData = new MenuApplicationStateData();
            CreateApplicationStates();
            ApplicationStateRunner applicationStateRunner =
                gameObject.AddComponent<ApplicationStateRunner>();
            applicationStateRunner.Initialize(applicationStates, applicationData, platform);
        }

        private IEnumerator SetUpNetworking()
        {
            // --- NETWORK: load settings and create connection -----------------
            // Load network settings and create service
            Debug.Log("Loading network settings...");
            var networkSettingsHandle = Addressables.LoadAssetAsync<NetworkSettings>(
                initializerSettings.networkSettings
            );
            yield return new WaitUntil(() => networkSettingsHandle.IsDone);
            Debug.Log("Network settings loaded");

            if (networkSettingsHandle.Result == null)
            {
                Debug.LogError("NetworkSettings asset is null!");
            }

            networkService = NetworkServiceFactory.Create(networkSettingsHandle.Result);
            Debug.Log($"NetworkService created: {(networkService != null ? "not null" : "null")}");

            if (networkService != null)
            {
                Debug.Log("Initializing network service...");
                // Initialize the service (which initializes its connection)
                yield return networkService.Initialize();
                Debug.Log("Network service initialized");

                Debug.Log($"Network status: {networkService.Status}");
                int connectionTimeout = 0;
                while (
                    networkService.Status == NetworkConnectionStatus.Connecting
                    && connectionTimeout < 300
                )
                {
                    yield return null;
                    connectionTimeout++;
                }

                Debug.Log(
                    $"Exited connection loop. Status: {networkService.Status}, Timeout: {connectionTimeout >= 300}"
                );

                if (networkService.Status == NetworkConnectionStatus.Connected)
                {
                    Debug.Log("Network: connected successfully during boot.");
                }
                else
                {
                    Debug.LogError(
                        $"Network: failed to connect during boot. Status: {networkService.Status}"
                    );
                }

                // Expose to the rest of the app
                applicationData.Network = networkService;
            }
            else
            {
                Debug.Log("No network service created (null)");
            }
            // ------------------------------------------------------------------
        }

        /// <summary>
        /// Gets the current platform factory based on the set define symbols in DeviceHandler
        /// </summary>
        private IEnumerator CreatePlatformFactory()
        {
            platform = null;
            DevicePlatform devicePlatform = applicationData.platformSelector.devicePlatform;
            platform = devicePlatform switch
            {
                DevicePlatform.Desktop => new DesktopPlatform(
                    initializerSettings.desktopPlatformSettings
                ),
                DevicePlatform.Web => new WebPlatform(initializerSettings.webPlatformSettings),
                _ => platform,
            };

            if (
                !Equals(applicationData.platformSelector.devicePlatform, DevicePlatform.Desktop)
                && PlatformSelector.GetPlatformDefaultInputMode() == InputMode.Desktop
            )
            {
                platform = new DesktopPlatform(initializerSettings.desktopPlatformSettings);
            }
            yield return platform?.Initialize(applicationData);
        }

        /// <summary>
        /// Add, create and initialize new game states using data/settings constructor dependency injection
        /// </summary>
        private void CreateApplicationStates()
        {
            createApplicationStateMarker.Begin();
            applicationStates = new Dictionary<ApplicationState, IApplicationState>
            {
                [ApplicationState.Splash] = new SplashApplicationState(
                    splashBootSettings,
                    applicationData
                ),
                [ApplicationState.MainMenu] = new MainMenuApplicationState(
                    applicationData,
                    menuApplicationStateData,
                    mainMenuBootSettings,
                    optionsData
                ),
                [ApplicationState.GameMode] = new GameModeApplicationState(
                    applicationData,
                    gameModeBootSettings.gameModeSettings
                ),
                [ApplicationState.Exit] = new QuitApplicationState(),
            };
            createApplicationStateMarker.End();
        }

        private void OnDestroy()
        {
            networkService?.Dispose();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystemGo);
        }
    }
}
