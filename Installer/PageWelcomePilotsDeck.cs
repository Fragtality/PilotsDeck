using CFIT.AppTools;
using CFIT.Installer.UI.Behavior;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Installer
{
    public class PageWelcomePilotsDeck : PageWelcome
    {
        public PageWelcomePilotsDeck() : base()
        {
            
        }

        protected override void SetHeaderHints()
        {
            int maxWidth = 448;
            Thickness margin = new Thickness(8, 16, 8, 0);
            string text = "This Tool will install the Plugin to your StreamDeck Software.\r\nThe Software will be stopped/started during the Installation-Process.\r\nAdded/Changed Profiles, Images and Scripts will stay intact.";
            var header = CreateTextBlock(text, 14, FontWeights.DemiBold, HorizontalAlignment.Center);
            header.Width = maxWidth;
            header.TextWrapping = TextWrapping.WrapWithOverflow;
            header.Margin = margin;
            AddHeader(header);

            text = "DO NOT run the Installer, Plugin or StreamDeck Software as Admin!\r\nRunning the Simulator or FSUIPC as Admin can eventually cause Connections-Issues!";
            header = CreateTextBlock(text, 14, FontWeights.Regular, HorizontalAlignment.Center);
            header.Width = maxWidth;
            header.TextWrapping = TextWrapping.WrapWithOverflow;
            header.Margin = margin;
            AddHeader(header);

            text = "PilotsDeck is 100% free and Open-Source. The Software and the Developer do not have any Affiliation to Flight Panels.\r\nIt is the actual Plugin allowing the StreamDeck to interface with the Simulator and allowing the Creation of StreamDeck Profiles for Airplanes.";
            header = CreateTextBlock(text, 12, FontWeights.Regular, HorizontalAlignment.Center);
            header.TextWrapping = TextWrapping.WrapWithOverflow;
            header.Width = maxWidth;
            margin.Top = 24;
            header.Margin = margin;
            Hyperlink link = new Hyperlink(new Run("\r\nPilotsDeck on GitHub"))
            {
                NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck")
            };
            link.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
            header.Inlines.Add(link);
            AddHeader(header);
        }
    }
}
