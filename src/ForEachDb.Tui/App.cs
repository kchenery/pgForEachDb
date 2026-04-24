using ForEachDb.Tui.Theme;
using ForEachDb.Tui.Views;
using Terminal.Gui;

namespace ForEachDb.Tui;

public static class App
{
    public static void Run()
    {
        Application.Init();
        Themes.Apply(ThemeKind.SofterDark);

        try
        {
            var dialog = new ConnectionDialog();
            Application.Run(dialog);

            if (dialog.Result is not { } result)
                return;

            var main = new MainWindow(result)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            var menu = AppMenu.Build(main);

            var top = new Toplevel
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(menu, main);

            try
            {
                Application.Run(top);
            }
            finally
            {
                Application.RootKeyEvent = null;
            }
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
