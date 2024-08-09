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
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kInducedDemandGroup = "Induced Demand";
        public const string kRerouteDistanceGroup = "Reroute Distance";
        public const string kGarageSpotsGroup = "Garage Spots";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISection(kSection, kInducedDemandGroup)]
        public bool InducedDemandEnable { get; set; }

        [SettingsUISlider(min = 2000, max = 10000, step = 1)]
        [SettingsUISection(kSection, kInducedDemandGroup)]
        public int InducedDemandCooldown { get; set; }

        [SettingsUISlider(min = 0, max = 20, step = 1)]
        [SettingsUISection(kSection, kInducedDemandGroup)]
        public int InducedDemandInitialTolerance { get; set; }

        [SettingsUISlider(min = 1, max = 10, step = 0.5f)]
        [SettingsUISection(kSection, kInducedDemandGroup)]
        public float InducedDemandQueueSizePerSpot { get; set; }


        [SettingsUISection(kSection, kRerouteDistanceGroup)]
        public bool RerouteDistanceEnable { get; set; }

        [SettingsUISlider(min = 1, max = 1000, step = 1)]
        [SettingsUISection(kSection, kRerouteDistanceGroup)]
        public int RerouteDistance { get; set; }


        [SettingsUISlider(min = 1, max = 40, step = 1)]
        [SettingsUISection(kSection, kGarageSpotsGroup)]
        public int GarageSpotsMultiplier { get; set; }


        public override void SetDefaults()
        {
            InducedDemandEnable = true;
            InducedDemandCooldown = 5000;
            InducedDemandInitialTolerance = 6;
            InducedDemandQueueSizePerSpot = 3;
            RerouteDistanceEnable = true;
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
                { m_Setting.GetSettingsLocaleID(), "RealisticParking" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Settings" },


                { m_Setting.GetOptionGroupLocaleID(Setting.kInducedDemandGroup), "Induced Demand" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandEnable)), "Enable Reroute Distance Change Change" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Induced Demand Initial Tolerance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Induced Demand Queue Size per Parking Spot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Number of nodes away for the car to reroute based on parking availability" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandCooldown)), "Induced Demand Cooldown Length" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandCooldown)), "Number of nodes away for the car to reroute based on parking availability" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kRerouteDistanceGroup), "Reroute Distance" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RerouteDistanceEnable)), "Enable Reroute Distance Change Change" },
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
