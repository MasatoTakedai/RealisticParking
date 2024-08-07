using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Vehicles;

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
            //updateSystem.UpdateAfter<ParkingRerouteSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<NewPersonalCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<NewPersonalCarAISystem.Actions, NewPersonalCarAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<NewPersonalCarAISystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateAfter<NewPersonalCarAISystem.Actions, NewPersonalCarAISystem>(SystemUpdatePhase.LoadSimulation);
            updateSystem.UpdateBefore<NewParkingLaneDataSystem>(SystemUpdatePhase.ModificationEnd);
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
