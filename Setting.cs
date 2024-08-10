using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

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

        [SettingsUISlider(min = 0, max = 20, step = 1)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public int InducedDemandInitialTolerance { get; set; }

        [SettingsUISlider(min = 1.0f, max = 10.0f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public float InducedDemandQueueSizePerSpot { get; set; }

        [SettingsUISlider(min = 3000, max = 10000, step = 1)]
        [SettingsUISection(MainTab, InducedDemandGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideInducedDemand))]
        public int InducedDemandCooldown { get; set; }


        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        public bool EnableRerouteDistance { get; set; }
        private bool hideRerouteDistance() => !EnableRerouteDistance;

        [SettingsUISlider(min = 1, max = 200, step = 1)]
        [SettingsUISection(MainTab, RerouteDistanceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(hideRerouteDistance))]
        public int RerouteDistance { get; set; }


        [SettingsUISlider(min = 0f, max = 4f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, kGarageSpotsGroup)]
        public float GarageSpotsPerResProp { get; set; }

        [SettingsUISlider(min = 0f, max = 1f, step = 0.1f, unit = "floatSingleFraction")]
        [SettingsUISection(MainTab, kGarageSpotsGroup)]
        public float GarageSpotsPerWorker { get; set; }

        [SettingsUIButton]
        [SettingsUISection(MainTab, kGarageSpotsGroup)]
        public bool SetGarageCapacitiesButton { set { SetGarageCapacities(); } }

        private void SetGarageCapacities()
        {
            if (World.DefaultGameObjectInjectionWorld.IsCreated)
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<NewParkingLaneDataSystem>().UpdateGarageCapacities();
        }


        public override void SetDefaults()
        {
            EnableInducedDemand = true;
            InducedDemandCooldown = 6000;
            InducedDemandInitialTolerance = 6;
            InducedDemandQueueSizePerSpot = 1.8f;
            EnableRerouteDistance = true;
            RerouteDistance = 10;
            GarageSpotsPerResProp = 1.3f;
            GarageSpotsPerWorker = 0.5f;
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
                    "Enable induced demand system for parking spots. This system counts every time a car pathfinds to a parking lane, and once enough cars have routed to it, " +
                    "it will disable that spot for future pathfinding. The system resets once the spot is full or enough time passes." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandInitialTolerance)), "Demand Initial Tolerance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandInitialTolerance)), 
                    "The number of cars for parking lanes to initially let pathfind in without considering the queue size per spot. For example, if there are 2 spots open and " +
                    "this is set to 5 and the queue size per spot to 1, the parking spot will be disabled after 7 cars have pathfinded to that spot. If this setting is set " +
                    "to a low number with limited parking spots in the city, it may result in a low number of cars driving around." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), "Demand Queue Size per Parking Spot" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandQueueSizePerSpot)), 
                    "The number of cars to be allowed to pathfind to each available parking spot. Decimal values are allowed." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InducedDemandCooldown)), "Demand Reset Length" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InducedDemandCooldown)), 
                    "The number of simulation frames after the most recent pathfind for the demand to reset to 0. For reference, one in-game minute is approximately equal to " +
                    "182 frames." 
                },

                { m_Setting.GetOptionGroupLocaleID(Setting.RerouteDistanceGroup), "Reroute Distance" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableRerouteDistance)), "Enable Reroute Distance Change" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableRerouteDistance)), 
                    "Enable reroute distance change for car navigation. In the vanilla system, a car can sense that the parking spot they are navigating to is full up to 4000 " +
                    "nodes away. This causes random u-turns on main roads, as they will suddenly change parking destinations while driving. This system aims to get rid of that " +
                    "by only letting the car sense that the parking is unavailable from a closer distance." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RerouteDistance)), "Reroute Node Distance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RerouteDistance)), 
                    "Number of nodes away for cars to reroute based on parking availability. For reference, the vanilla value is 4000." 
                },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGarageSpotsGroup), "Garage Spots" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GarageSpotsPerResProp)), "Garage Spots per Household" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GarageSpotsPerResProp)), 
                    "The number of garage spots per household in the apartment. Apartments with no garage in the asset are not affected." 
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GarageSpotsPerWorker)), "Garage Spots per Worker" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GarageSpotsPerWorker)),
                    "The number of garage spots per worker in the property. Non-RICO buildings are not affected. Properties with no garage in the asset are not affected."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SetGarageCapacitiesButton)), "Set Garage Capacities" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SetGarageCapacitiesButton)),
                    "Sets custom garage capacities."
                },
            };
        }

        public void Unload()
        {

        }
    }
}
