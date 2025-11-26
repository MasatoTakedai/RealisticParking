using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Pathfind;
using Game.SceneFlow;
using Game.Simulation;
using HarmonyLib;

namespace RealisticParking
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(RealisticParking)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public Setting settings;

        public static Mod INSTANCE;

        public void OnLoad(UpdateSystem updateSystem)
        {
            INSTANCE = this;
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            settings = new Setting(this);
            settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(settings));

            AssetDatabase.global.LoadSettings(nameof(RealisticParking), settings, new Setting(this));
            updateSystem.UpdateAfter<NewPersonalCarAISystem, PersonalCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<NewPersonalCarAISystem.Actions, NewPersonalCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<NewPersonalCarAISystem, PersonalCarAISystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateAfter<NewPersonalCarAISystem.Actions, NewPersonalCarAISystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateAfter<NewParkingLaneDataSystem, ParkingLaneDataSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<ParkingDemandSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<GarageLanesModifiedSystem>(SystemUpdatePhase.ModificationEnd);

            var harmony = new Harmony("daancingbanana.realisticparking");
            harmony.PatchAll();
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (settings != null)
            {
                settings.UnregisterInOptionsUI();
                settings = null;
            }
        }
    }
}
