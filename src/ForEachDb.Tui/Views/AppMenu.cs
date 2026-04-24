using ForEachDb.Tui.Theme;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class AppMenu
{
    public static MenuBar Build(MainWindow main) =>
        new(
        [
            new MenuBarItem("_File",
            [
                new MenuItem("_Quit", "Ctrl+Q", main.Quit)
            ]),
            new MenuBarItem("_Run",
            [
                new MenuItem("_Run query", "F5", () => _ = main.RunAsync()),
                new MenuItem("_Cancel", "F6", main.RequestCancel),
                new MenuItem("_Threads…", "Ctrl+T", main.OpenThreadsDialog)
            ]),
            new MenuBarItem("_View",
            [
                new MenuItem("_Results…", "Ctrl+R", main.ShowResults),
                new MenuItem("Toggle log _filter", "Ctrl+L", main.CycleFilter),
                null!,
                new MenuBarItem("_Theme",
                [
                    new MenuItem("_Softer dark",   "", () => Themes.Apply(ThemeKind.SofterDark)),
                    new MenuItem("_Midnight blue", "", () => Themes.Apply(ThemeKind.MidnightBlue)),
                    new MenuItem("M_inimal mono",  "", () => Themes.Apply(ThemeKind.MinimalMono)),
                    new MenuItem("_Retro green",   "", () => Themes.Apply(ThemeKind.RetroGreen))
                ])
            ]),
            new MenuBarItem("Re_cipes",
            [
                new MenuItem("_Save current…", "Ctrl+S", main.SaveRecipe)
            ]),
            new MenuBarItem("_Help",
            [
                new MenuItem("Key _bindings…", "F1", HelpDialog.Show)
            ])
        ]);
}
