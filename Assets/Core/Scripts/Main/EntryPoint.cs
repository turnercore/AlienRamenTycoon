using System.Collections;
using System.Collections.Generic;
using Project;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core
{
    /// <summary>
    /// The main boot class which is initializing the project, loads the platforms settings, initializes the platforms and creates the game states
    /// </summary>
    public class EntryPoint : MonoBehaviour
    {
        // Authoring
        // Instead of using hard references to resources, we load files using AssetReference or AssetReferenceT which point to addresses of actual assets
        // from Addressables groups bundles, in order to have a better control over memory load. Each request will return a handle
        // which is contained in the AssetRequest. Releasing this handle will unload the resource and free the associated references,
        // if they are not used anymore.
        public AssetReferenceT<InitializerSettingsFile> initializerSettingsFile;

        // Globals
        private IPlatform platform;
        private ApplicationData applicationData;
        private Dictionary<ApplicationState, IApplicationState> applicationStates;
        private ProfilerMarker createApplicationStateMarker = new("createApplicationState");

        //Settings
        private InitializerSettingsFile initializerSettings;
        private SplashBootSettings splashBootSettings;
        private MainMenuBootSettings mainMenuBootSettings;
        private GameModeBootSettings gameModeBootSettings;
        private MenuApplicationStateData menuApplicationStateData;

        /// <summary>
        /// The is the first method called when the game starts. It will load the Initializer prefab which will initialize the project
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
#if UNITY_EDITOR
            if (BootMode.BootType == BootType.UnityDefault)
            {
                return;
            }
#endif
            var entryPoint = FindFirstObjectByType<EntryPoint>(FindObjectsInactive.Include);
            if (entryPoint == null)
            {
                var handler = Addressables.InstantiateAsync("Initializer");
            }
        }

        /// <summary>
        /// Main application initialization coroutine
        /// </summary>
        public IEnumerator Start()
        {
            DontDestroyOnLoad(this);

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

            // Initialize platform
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

            // We make sure the platform is initialized before entering the game states
            yield return CreatePlatformFactory();

            // Booting into the default starting Application State
            var splashBootSettingsHandle = Addressables.LoadAssetAsync<SplashBootSettings>(
                initializerSettings.bootAssetReference
            );
            yield return new WaitUntil(() => splashBootSettingsHandle.IsDone);
            splashBootSettings = splashBootSettingsHandle.Result;

            // We initialize the Application State Runner which will run the game states
            menuApplicationStateData = new MenuApplicationStateData();
            CreateApplicationStates();
            ApplicationStateRunner applicationStateRunner =
                gameObject.AddComponent<ApplicationStateRunner>();
            applicationStateRunner.Initialize(applicationStates, applicationData, platform);
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
                    mainMenuBootSettings
                ),
                // The GameMode state is where we will be playing the game or the games. It is where we can have multiple game modes
                [ApplicationState.GameMode] = new GameModeApplicationState(
                    applicationData,
                    gameModeBootSettings.gameModeSettings
                ),
            };
            createApplicationStateMarker.End();
        }
    }
}
