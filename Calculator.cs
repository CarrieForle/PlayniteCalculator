using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Calculator
{
    public class Calculator : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private CalculatorSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e5da871f-1b18-424b-8bea-ab829b044da9");

        public Calculator(IPlayniteAPI api) : base(api)
        {
            settings = new CalculatorSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new CalculatorSettingsView();
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Opened = () =>
                {
                    return new SidebarView();
                },
                Closed = () =>
                {
                    // TODO
                },
                Icon = "Assets/sidebar-icon.png",
                Type = SiderbarItemType.View,
                Visible = true,
            };
        }
    }
}