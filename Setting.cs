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
    [SettingsUIGroupOrder(kDescGroup, kSettingsGroup)]
    [SettingsUIShowGroupName(kDescGroup, kSettingsGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kDescGroup = "Description";
        public const string kSettingsGroup = "Settings";

        public Setting(IMod mod) : base(mod)
        {

        }

        [SettingsUISection(kSection, kSettingsGroup)]
        public bool Enable { get; set; }

        [SettingsUISlider(min = 0, max = 20, step = 1)]
        [SettingsUISection(kSection, kSettingsGroup)]
        public int RerouteDistance { get; set; }

        [SettingsUISlider(min = 1, max = 40, step = 1)]
        [SettingsUISection(kSection, kSettingsGroup)]
        public int GarageSpotsMultiplier { get; set; }


        public override void SetDefaults()
        {
            this.Enable = true;
            this.RerouteDistance = 6;
            this.GarageSpotsMultiplier = 20;
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
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kDescGroup), "Description" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSettingsGroup), "Settings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RerouteDistance)), "Reroute Distance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RerouteDistance)), "Number of nodes away for the car to reroute based on parking availability" },
            };
        }

        public void Unload()
        {

        }
    }
}
