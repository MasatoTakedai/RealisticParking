using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace RealisticParking
{
    [FileLocation(nameof(RealisticParking))]
    [SettingsUIShowGroupName(InducedDemandGroup, RerouteDistanceGroup, kGarageSpotsGroup)]
    public class Setting : ModSetting
    {
        public const string MainTab = "Main";

        public const string InducedDemandGroup = "Induced Demand";
        public const string RerouteDistanceGroup = "Reroute Distance";
        public const string kGarageSpotsGroup = "Garage Spots";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISection(MainTab, InducedDemandGroup)]
        public bool EnableInducedDemand { get; set; }
        private bool hideInducedDemand() => !EnableInducedDemand;

        [SettingsUISlider(min = 3000, max = 10000, step = 1)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public int InducedDemandCooldown { get; set; }

        [SettingsUISlider(min = 0, max = 20, step = 1)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public int InducedDemandInitialTolerance { get; set; }

        [SettingsUISlider(min = 1, max = 10, step = 0.5f)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public float InducedDemandQueueSizePerSpot { get; set; }


        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        public bool EnableRerouteDistance { get; set; }
        private bool hideRerouteDistance() => !EnableRerouteDistance;

        [SettingsUISlider(min = 1, max = 500, step = 1)]
        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideRerouteDistance))]
        public int RerouteDistance { get; set; }


        [SettingsUISlider(min = 1, max = 40, step = 1)]
        [SettingsUISection(MainTab, kGarageSpotsGroup)]
        public int GarageSpotsMultiplier { get; set; }


        public override void SetDefaults()
        {
            EnableInducedDemand = true;
            InducedDemandCooldown = 6000;
            InducedDemandInitialTolerance = 6;
            InducedDemandQueueSizePerSpot = 1.5f;
            EnableRerouteDistance = true;
            RerouteDistance = 12;
            GarageSpotsMultiplier = 20;
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
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableInducedDemand)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Induced Demand Initial Tolerance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Induced Demand Queue Size per Parking Spot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandCooldown)), "Induced Demand Cooldown Length" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandCooldown)), "Number of nodes away for the car to reroute based on parking availability" },

                { m_Setting.GetOptionGroupLocaleID(Setting.RerouteDistanceGroup), "Reroute Distance" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableRerouteDistance)), "Enable Reroute Distance Change Change" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableRerouteDistance)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RerouteDistance)), "Reroute Node Distance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RerouteDistance)), "Number of nodes away for the car to reroute based on parking availability" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kGarageSpotsGroup), "Garage Spots" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GarageSpotsMultiplier)), "Garage Spots Multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GarageSpotsMultiplier)), "Number of nodes away for the car to reroute based on parking availability" },
            };
        }

        public void Unload()
        {

        }
    }
}
