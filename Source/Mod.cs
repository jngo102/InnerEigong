using BepInEx;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using RCGFSM.Animation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerEigong;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Mod : BaseUnityPlugin {
    private static Harmony _harmony = null!;

    private void Awake() {
        Log.Init(Logger);
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(Patches));

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }


    private async void Start() {
        await InitializeAssets();
        await PreloadManager.Initialize();
#if DEBUG
        await StartEigongFight();
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