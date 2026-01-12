using System.Windows;
using System.Windows.Controls;
using SpaceInvaders.Core.Upgrades;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf.Views;

public partial class ShopPage : Page
{
    private readonly ShellWindow _shell;

    public ShopPage(ShellWindow shell)
    {
        InitializeComponent();
        _shell = shell;
        Rebuild();
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        _shell.NavigateToMainMenu(this);
    }

    private void Rebuild()
    {
        CoinsText.Text = $"Coins: {_shell.Session.Meta.Coins}";
        ItemsPanel.Children.Clear();

        foreach (var up in MetaUpgradeCatalog.All)
        {
            var level = up.GetLevel(_shell.Session.Meta);
            int? nextCost = level >= up.MaxLevel ? null : up.CostForNextLevel(level + 1);

            var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

            var text = new TextBlock
            {
                Text = $"{up.Name}  (Lv {level}/{up.MaxLevel})\n{up.Description}" + (nextCost is null ? "\nMAX" : $"\nNext: {nextCost} coins"),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 18
            };

            var buy = new Button
            {
                Content = nextCost is null ? "MAX" : $"Buy ({nextCost})",
                Height = 56,
                IsEnabled = nextCost is not null && _shell.Session.Meta.Coins >= nextCost.Value
            };

            buy.Click += (_, _) =>
            {
                if (up.TryPurchase(_shell.Session.Meta))
                {
                    _shell.SaveProfile();
                    Rebuild();
                }
            };

            Grid.SetColumn(text, 0);
            Grid.SetColumn(buy, 1);
            row.Children.Add(text);
            row.Children.Add(buy);

            ItemsPanel.Children.Add(row);
        }
    }
}
