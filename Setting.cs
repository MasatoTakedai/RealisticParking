using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI.Widgets;
using System.Collections.Generic;
using System.Linq;

namespace RealisticParking
{
    [FileLocation("ModsSettings/RealisticParking/RealisticParking")]
    [SettingsUIShowGroupName(InducedDemandGroup, RerouteDistanceGroup, ParkingMinimumsGroup)]
    public class Setting : ModSetting
    {
        public const string MainTab = "Main";

        public const string InducedDemandGroup = "Induced Demand";
        public const string RerouteDistanceGroup = "Reroute Distance";
        public const string ParkingMinimumsGroup = "Garage Spots";
        public const string ResetGroup = "Reset";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();

            // precompute dropdown items from presets
            presetDropdowns = presets.Keys
                .Append("Custom")
                .Select(key => new DropdownItem<string>
                {
                    value = key,
                    displayName = GetOptionLabelLocaleID(key),
                })
                .ToArray();
        }

        [SettingsUISection(MainTab, InducedDemandGroup)]
        public bool EnableInducedDemand { get; set; }

        private string _inducedDemandPreset;
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDropdown(typeof(Setting), nameof(GetPresets))]
        public string InducedDemandPreset 
        { 
            get { return _inducedDemandPreset; }
            set 
            { 
                _inducedDemandPreset = value;
                ApplyPreset(value); 
            } 
        }
        private struct InducedDemandSettings
        {
            public int initialTolerance;
            public float queueSizePerSpot;
        }
        private readonly Dictionary<string, InducedDemandSettings> presets = new Dictionary<string, InducedDemandSettings>
        {
            {"Low", new InducedDemandSettings { initialTolerance = 0, queueSizePerSpot = 1.0f } },
            {"Medium", new InducedDemandSettings { initialTolerance = 2, queueSizePerSpot = 1.5f } },
            {"High", new InducedDemandSettings { initialTolerance = 2, queueSizePerSpot = 3f } }
        };
        private readonly DropdownItem<string>[] presetDropdowns;
        public DropdownItem<string>[] GetPresets()
        {
            return presetDropdowns;
        }
        private void ApplyPreset(string preset)
        {
            if (presets.TryGetValue(preset, out var settings))
            {
                InducedDemandInitialTolerance = settings.initialTolerance;
                InducedDemandQueueSizePerSpot = settings.queueSizePerSpot;
            }
        }

        private bool enableInducedDemandValues() => EnableInducedDemand && InducedDemandPreset == "Custom";
        private bool disableInducedDemandPreset() => !enableInducedDemandValues();
        [SettingsUISlider(min = 0, max = 20, step = 1)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disableInducedDemandPreset))]
        public int InducedDemandInitialTolerance { get; set; }

        [SettingsUISlider(min = 1.0f, max = 10.0f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(disableInducedDemandPreset))]
        public float InducedDemandQueueSizePerSpot { get; set; }


        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        public bool EnableRerouteDistance { get; set; }
        private bool hideRerouteDistance() => !EnableRerouteDistance;

        [SettingsUISlider(min = 1, max = 200, step = 1)]
        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideRerouteDistance))]
        public int RerouteDistance { get; set; }


        [SettingsUISection(MainTab, ParkingMinimumsGroup)]
        public bool EnableParkingMins { get; set; }
        private bool hideParkingMins() => !EnableParkingMins;

        [SettingsUISlider(min = 0f, max = 4f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, ParkingMinimumsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideParkingMins))]
        public float GarageSpotsPerResProp { get; set; }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, ParkingMinimumsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideParkingMins))]
        public float GarageSpotsPerWorker { get; set; }


        [SettingsUIButton]
        [SettingsUISection(MainTab, ResetGroup)]
        public bool ResetButton { set { SetDefaults(); } }


        public override void SetDefaults()
        {
            EnableInducedDemand = true;
            InducedDemandPreset = "Medium";

            EnableRerouteDistance = true;
            RerouteDistance = 12;

            EnableParkingMins = true;
            GarageSpotsPerResProp = 1.2f;
            GarageSpotsPerWorker = 0.5f;
        }

        // for config files before the presets were added
        public void VerifyPreset()
        {
            if (presets.TryGetValue(InducedDemandPreset, out var settings) && 
                (InducedDemandInitialTolerance != settings.initialTolerance || InducedDemandQueueSizePerSpot != settings.queueSizePerSpot))
            {
                InducedDemandPreset = "Custom";
            }
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Realistic Parking" },
                { m_Setting.GetOptionTabLocaleID(Setting.MainTab), "Settings" },


                { m_Setting.GetOptionGroupLocaleID(Setting.InducedDemandGroup), "Induced Demand" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableInducedDemand)), "Enable Parking Induced Demand" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableInducedDemand)),
                    "Enable induced demand system for parking spots. This system counts every time a car or bike pathfinds to a parking lane, and once enough have routed to it, " +
                    "it will disable that spot for future pathfinding. If the spot remains vacant for some time, it will reopen for more requests." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandPreset)), "Induced Demand Preset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandPreset)), 
                    "Set a preset for the induced demand system. The presets adjust the initial tolerance and queue size per spot values. Use Custom to set your own values."
                },
                { m_Setting.GetOptionLabelLocaleID("Low"), "One vehicle per parking spot" },
                { m_Setting.GetOptionLabelLocaleID("Medium"), "A couple vehicles per parking spot" },
                { m_Setting.GetOptionLabelLocaleID("High"), "Lots of vehicles per parking spot" },
                { m_Setting.GetOptionLabelLocaleID("Custom"), "Custom" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Demand Initial Tolerance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandInitialTolerance)),
                    "The number of cars or bikes can pathfind to a parking lane before it starts considering the queue size per spot value. For example, if a parking lot " +
                    "has 2 spots, and the initial tolerance is 2 and the queue size per spot is 1.5, the parking lot will be disabled after 5 cars or bikes have pathfinded " +
                    "to it while it's available. (initial tolerance + queue size per spot * number of spots = 2 + 1.5 * 2 = 5)"
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Demand Queue Size per Parking Spot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), 
                    "The number of cars or bikes that can pathfind to each available parking spot before it gets disabled. See description for the setting above for more details."
                },

                { m_Setting.GetOptionGroupLocaleID(Setting.RerouteDistanceGroup), "Reroute Distance" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableRerouteDistance)), "Enable Reroute Distance Change" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableRerouteDistance)), 
                    "Enable reroute distance change for car and bike navigation. In the vanilla system, a car or bike can sense that the parking spot they are navigating to is unavailable " +
                    "up to 40000 nodes away. This causes random u-turns on main roads, as they will suddenly change parking destinations while driving. This system aims to get rid of that " +
                    "by only letting the car or bike sense that the parking is unavailable from a closer distance." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RerouteDistance)), "Reroute Node Distance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RerouteDistance)), 
                    "Number of nodes away for cars or bikes to reroute based on parking availability. For reference, the vanilla value is 4000." 
                },
                { m_Setting.GetOptionGroupLocaleID(Setting.ParkingMinimumsGroup), "Parking Minimums" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableParkingMins)), "Enable Parking Minimums for Garages" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableParkingMins)),
                    "Enable parking minimums for garages. If enabled, custom garage capacities will replace vanilla values. Vanilla apartments have up to 3 spots " +
                    "and offices have around 30. Properties with no garage in the asset are not affected."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GarageSpotsPerResProp)), "Garage Spots per Household" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GarageSpotsPerResProp)), 
                    "The number of garage spots per household in residential properties. Properties with no garage in the asset are not affected." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GarageSpotsPerWorker)), "Garage Spots per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GarageSpotsPerWorker)),
                    "The number of garage spots per worker in commericial/office properties. Non-RICO buildings are not affected. Properties with no garage in the asset are not affected."
                },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetButton)), "Reset Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetButton)),
                    "Reset settings to default values."
                },
            };
        }

        public void Unload()
        {

        }
    }
}
