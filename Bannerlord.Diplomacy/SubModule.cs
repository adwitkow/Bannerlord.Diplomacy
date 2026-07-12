using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.UIExtenderEx;
using Bannerlord.UIExtenderEx.ResourceManager;

using Diplomacy.CampaignBehaviors;
using Diplomacy.Events;
using Diplomacy.Models;
using Diplomacy.PatchTools;
using Diplomacy.Widgets;

using Microsoft.Extensions.Logging;

using Serilog.Events;

using System;
using System.Linq;
using System.Xml;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Diplomacy
{
    public sealed class SubModule : MBSubModuleBase
    {
        public static readonly string Version = $"v{typeof(SubModule).Assembly.GetName().Version!.ToString(3)}";

        public static readonly string Name = typeof(SubModule).Namespace!;
        public static readonly string DisplayName = new TextObject($"{{=MYz8nKqq}}{Name}").ToString();
        public static readonly string MainHarmonyDomain = "bannerlord." + Name.ToLower();
        public static readonly string CampaignHarmonyDomain = MainHarmonyDomain + ".campaign";
        public static readonly string WidgetHarmonyDomain = MainHarmonyDomain + ".widgets";

        internal static readonly Color StdTextColor = Color.FromUint(0x00F16D26); // Orange

        internal static SubModule Instance { get; set; } = default!;

        private static ILogger Log { get; set; } = default!;

        private bool _hasLoaded;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Instance = this;

            this.AddSerilogLoggerProvider($"{Name}.log", new[] { $"{Name}.*" }, config => config.MinimumLevel.Is(LogEventLevel.Verbose));
            Log = LogFactory.Get<SubModule>();
            Log.LogInformation($"Loading {Name} {Version}...");

            PatchManager.ApplyMainPatches(MainHarmonyDomain);
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            Log.LogInformation($"Unloaded {Name} {Version}!");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!_hasLoaded)
            {
                _hasLoaded = true;

                var extender = UIExtender.Create(Name);
                extender.Register(typeof(SubModule).Assembly);
                extender.Enable();

                RegisterPrefabs();

                Log.LogInformation($"Loaded {Name} {Version}!");

                InformationManager.DisplayMessage(new InformationMessage(new TextObject($"{{=hPERH3u4}}Loaded {{NAME}}").SetTextVariable("NAME", DisplayName).ToString(), StdTextColor));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (game.GameType is Campaign)
            {
                PatchManager.ApplyCampaignPatches(CampaignHarmonyDomain);

                DiplomacyEvents.Instance = new DiplomacyEvents();
                var gameStarter = (CampaignGameStarter) gameStarterObject;

                gameStarter.AddBehavior(new DiplomaticAgreementBehavior());
                gameStarter.AddBehavior(new CooldownBehavior());
                gameStarter.AddBehavior(new MessengerBehavior());

                if (Settings.Instance!.EnableWarExhaustion)
                    gameStarter.AddBehavior(new WarExhaustionBehavior());

                if (Settings.Instance!.EnableFiefFirstRight)
                    gameStarter.AddBehavior(new KeepFiefAfterSiegeBehavior());

                gameStarter.AddBehavior(new MaintainInfluenceBehavior());
                gameStarter.AddBehavior(new ExpansionismBehavior());
                gameStarter.AddBehavior(new CivilWarBehavior());
                gameStarter.AddBehavior(new UIBehavior());

                var currentKingdomDecisionPermissionModel = GetGameModel<KingdomDecisionPermissionModel>(gameStarterObject);
                if (currentKingdomDecisionPermissionModel is null)
                    Log.LogWarning("No default KingdomDecisionPermissionModel found!");

                gameStarter.AddModel(new DiplomacyKingdomDecisionPermissionModel(currentKingdomDecisionPermissionModel));

                Log.LogDebug("Campaign session started.");
            }
        }

        private T? GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
        {
            var models = gameStarterObject.Models.ToArray();

            for (int index = models.Length - 1; index >= 0; --index)
            {
                if (models[index] is T gameModel1)
                    return gameModel1;
            }
            return default;
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);

            if (game.GameType is Campaign)
            {
                //PatchManager.RemoveCampaignPatches();// Not sure we should do this...
                Log.LogDebug("Campaign session ended.");
            }
        }

        private void RegisterPrefabs()
        {
            WidgetFactoryManager.Register(typeof(CriticalThresholdTextWidget));

            BrushFactoryManager.CreateAndRegister(LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Brushes.Diplomacy.xml"));

            WidgetFactoryManager.CreateAndRegister(
                "EncyclopediaFactionPageInject",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.Encyclopedia.EncyclopediaSubPages.EncyclopediaFactionPageInject.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "EncyclopediaHeroPageInject",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.Encyclopedia.EncyclopediaSubPages.EncyclopediaHeroPageInject.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "FactionButtonInject",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.Encyclopedia.EncyclopediaSubPages.FactionButtonInject.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "GrantFief",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.GrantFief.GrantFief.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "GrantFiefTuple",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.GrantFief.GrantFiefTuple.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "ClansPanel",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Clan.ClansPanel.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "DonateGold",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Clan.DonateGold.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "DiplomacyPanelButtons",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Diplomacy.DiplomacyPanelButtons.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "DiplomacyPanelCustom",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Diplomacy.DiplomacyPanelCustom.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "OverviewTab",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Diplomacy.OverviewTab.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "Relationship",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Diplomacy.Relationship.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "StatsTab",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.KingdomManagement.Diplomacy.StatsTab.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "RebelFactionDivider",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.RebelFactions.RebelFactionDivider.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "RebelFactionParticipant",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.RebelFactions.RebelFactionParticipant.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "RebelFactions",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.RebelFactions.RebelFactions.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "RebelFactionsItem",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.RebelFactions.RebelFactionsItem.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "BasicDiplomacyButton",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.Standard.BasicDiplomacyButton.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "StaticDiplomacyButton",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.Standard.StaticDiplomacyButton.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "DetailWarView",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.WarExhaustion.DetailWarView.xml"));
            WidgetFactoryManager.CreateAndRegister(
                "WarExhaustionMapIndicator",
                LoadEmbeddedXml("Bannerlord.Diplomacy.GUI.Prefabs.WarExhaustion.WarExhaustionMapIndicator.xml"));
        }

        private static XmlDocument LoadEmbeddedXml(string embedPath)
        {
            using var stream = typeof(SubModule).Assembly.GetManifestResourceStream(embedPath);
            using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreComments = true });
            var doc = new XmlDocument();
            doc.Load(xmlReader);

#if LOWER_THAN_1_4
            FlipVerticalStacklayouts(doc.DocumentElement);
#endif

            return doc;
        }

        private static void FlipVerticalStacklayouts(XmlNode? node)
        {
            if (node?.Attributes is not null)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (!attribute.Name.Equals("LayoutImp.LayoutMethod", StringComparison.Ordinal)
                        && !attribute.Name.Equals("StackLayout.LayoutMethod", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (attribute.Value.Equals("VerticalTopToBottom", StringComparison.Ordinal))
                    {
                        attribute.Value = "VerticalBottomToTop";
                    }
                    else if (attribute.Value.Equals("VerticalBottomToTop", StringComparison.Ordinal))
                    {
                        attribute.Value = "VerticalTopToBottom";
                    }
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                FlipVerticalStacklayouts(child);
            }
        }
    }
}