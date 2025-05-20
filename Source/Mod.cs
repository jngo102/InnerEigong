using BepInEx;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Preload;
using RCGFSM.Animation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerEigong;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Mod : BaseUnityPlugin {
    internal static Mod Instance { get; private set; }

    private const string SniperScene = "A5_S4_CastleMid_Remake_5wei";
    private const string SniperLaserPath = "A5_S4/Room/SniperTeleportGroup (1)/StealthGameMonster_Sniper (1)/MonsterCore/Animator(Proxy)/Animator";

    private static Harmony _harmony = null!;

    [Preload(scene: SniperScene, path: SniperLaserPath)]
    internal GameObject? sniperPrefab;

    internal GameObject SniperCore => sniperPrefab.transform.Find("Dragon_Sniper/DragonSniper/RotateRArm/RArm/Bow/SniperLaserCore").gameObject;

    internal GameObject SniperAudio => sniperPrefab.transform.Find("LogicRoot/Audio/EnemySFX_Sniper_Attack").gameObject;

    private void Awake() {
        Log.Init(Logger);
        
        Instance = this;
        
        RCGLifeCycle.DontDestroyForever(gameObject);

        NineSolsAPICore.Preloader.AddPreloadClass(this);
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(Patches));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }


    private async void Start() {
        await InitializeAssets();
#if DEBUG
        // await StartEigongFight();
        // Log.Info("Started Eigong fight");
#endif
    }

    private static async UniTask StartEigongFight() {
        // Load Eigong scene immediately on game start
        RuntimeInitHandler.LoadCore();
        GameConfig.Instance.InstantiateGameCore();
        await UniTask.WaitUntil(() => SaveManager.Instance);
        var saveManager = SaveManager.Instance;
        saveManager.SavePlayerPref();
        await saveManager.LoadSaveAtSlot(4);
        await SceneManager.LoadSceneAsync(Constants.BossSceneName);
        // Skip the boss intro cutscene
        var fsmObj = GameObject.Find("General Boss Fight FSM Object Variant");
        var fsmOwner = fsmObj.TryGetComp<StateMachineOwner>();
        var fsmContext = fsmOwner.FsmContext;
        await UniTask.WaitUntil(() => fsmContext.currentStateType);
        var playAction = fsmContext.currentStateType.GetComponentInChildren<CutScenePlayAction>(true);
        if (playAction.cutscene is SimpleCutsceneManager cutsceneManager) {
            cutsceneManager.TrySkip();
        }
    }

    private void OnDestroy() {
        AssetManager.Unload();
        _harmony.UnpatchSelf();
    }

    private static async UniTask InitializeAssets() {
        await AssetManager.Load();
        if (AssetManager.TryGet<GameObject>("Tracking Slashes", out var trackingSlashes)) {
            trackingSlashes?.AddComponent<TrackingSlashes>();
        }
    }
}