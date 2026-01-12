using System.Windows;
using System.Windows.Controls;
using SpaceInvaders.Wpf.Helpers;

namespace SpaceInvaders.Wpf.Views;

public partial class MainMenuPage : Page
{
    private readonly ShellWindow _shell;

    public MainMenuPage(ShellWindow shell)
    {
        InitializeComponent();
        _shell = shell;
        Refresh();
    }

    private void Refresh()
    {
        CoinsText.Text = $"Coins: {_shell.Session.Meta.Coins}";
    }

    private void OnStart(object sender, RoutedEventArgs e)
    {
        _shell.NavigateToGame(this);
    }

    private void OnShop(object sender, RoutedEventArgs e)
    {
        _shell.NavigateToShop(this);
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        _shell.NavigateToSettings(this);
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
        _shell.Close();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        Refresh();
    }
}
