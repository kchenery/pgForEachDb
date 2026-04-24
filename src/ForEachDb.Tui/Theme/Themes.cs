using Terminal.Gui;

namespace ForEachDb.Tui.Theme;

public enum ThemeKind
{
    SofterDark,
    MidnightBlue,
    MinimalMono,
    RetroGreen
}

/// <summary>
/// Four hand-tuned palettes for Terminal.Gui's five global <see cref="ColorScheme" />s.
/// Must be called after <see cref="Application.Init()" />.
/// </summary>
/// <remarks>
/// Mutates the <see cref="Colors.TopLevel" />/<see cref="Colors.Base" />/etc. <i>instances</i> in place
/// rather than replacing them, so views that captured the reference at construction time
/// pick up the new attributes on the next draw.
/// </remarks>
public static class Themes
{
    public static ThemeKind Current { get; private set; } = ThemeKind.SofterDark;

    public static void Apply(ThemeKind kind)
    {
        var d = Application.Driver;
        if (d is null) return;

        switch (kind)
        {
            case ThemeKind.SofterDark:   ApplySofterDark(d);   break;
            case ThemeKind.MidnightBlue: ApplyMidnightBlue(d); break;
            case ThemeKind.MinimalMono:  ApplyMinimalMono(d);  break;
            case ThemeKind.RetroGreen:   ApplyRetroGreen(d);   break;
        }

        Current = kind;

        // Force every view on screen to repaint using the now-mutated attributes.
        if (Application.Top is { } top)
        {
            SetNeedsDisplayRecursive(top);
            Application.Refresh();
        }
    }

    private static void SetNeedsDisplayRecursive(View view)
    {
        view.SetNeedsDisplay();
        foreach (var child in view.Subviews)
            SetNeedsDisplayRecursive(child);
    }

    private static void Assign(ColorScheme target, ConsoleDriver d,
        (Color fg, Color bg) normal, (Color fg, Color bg) focus,
        (Color fg, Color bg) hotNormal, (Color fg, Color bg) hotFocus,
        (Color fg, Color bg) disabled)
    {
        target.Normal    = d.MakeAttribute(normal.fg,    normal.bg);
        target.Focus     = d.MakeAttribute(focus.fg,     focus.bg);
        target.HotNormal = d.MakeAttribute(hotNormal.fg, hotNormal.bg);
        target.HotFocus  = d.MakeAttribute(hotFocus.fg,  hotFocus.bg);
        target.Disabled  = d.MakeAttribute(disabled.fg,  disabled.bg);
    }

    private static void ApplySofterDark(ConsoleDriver d)
    {
        Assign(Colors.TopLevel, d,
            normal:    (Color.Gray,         Color.Black),
            focus:     (Color.White,        Color.DarkGray),
            hotNormal: (Color.Brown,        Color.Black),
            hotFocus:  (Color.BrightYellow, Color.DarkGray),
            disabled:  (Color.DarkGray,     Color.Black));
        Assign(Colors.Base, d,
            normal:    (Color.Gray,         Color.Black),
            focus:     (Color.Black,        Color.Gray),
            hotNormal: (Color.Brown,        Color.Black),
            hotFocus:  (Color.Black,        Color.Gray),
            disabled:  (Color.DarkGray,     Color.Black));
        Assign(Colors.Dialog, d,
            normal:    (Color.White,        Color.DarkGray),
            focus:     (Color.Black,        Color.Gray),
            hotNormal: (Color.Brown,        Color.DarkGray),
            hotFocus:  (Color.Black,        Color.Gray),
            disabled:  (Color.Gray,         Color.DarkGray));
        Assign(Colors.Menu, d,
            normal:    (Color.White,        Color.DarkGray),
            focus:     (Color.Black,        Color.Cyan),
            hotNormal: (Color.Brown,        Color.DarkGray),
            hotFocus:  (Color.Black,        Color.Cyan),
            disabled:  (Color.Gray,         Color.DarkGray));
        Assign(Colors.Error, d,
            normal:    (Color.BrightRed,    Color.Black),
            focus:     (Color.White,        Color.Red),
            hotNormal: (Color.BrightYellow, Color.Black),
            hotFocus:  (Color.BrightYellow, Color.Red),
            disabled:  (Color.Red,          Color.Black));
    }

    private static void ApplyMidnightBlue(ConsoleDriver d)
    {
        Assign(Colors.TopLevel, d,
            normal:    (Color.White,        Color.Blue),
            focus:     (Color.White,        Color.BrightBlue),
            hotNormal: (Color.BrightCyan,   Color.Blue),
            hotFocus:  (Color.BrightCyan,   Color.BrightBlue),
            disabled:  (Color.Gray,         Color.Blue));
        Assign(Colors.Base, d,
            normal:    (Color.White,        Color.Blue),
            focus:     (Color.White,        Color.BrightBlue),
            hotNormal: (Color.BrightCyan,   Color.Blue),
            hotFocus:  (Color.BrightCyan,   Color.BrightBlue),
            disabled:  (Color.Gray,         Color.Blue));
        Assign(Colors.Dialog, d,
            normal:    (Color.White,        Color.DarkGray),
            focus:     (Color.Black,        Color.Gray),
            hotNormal: (Color.BrightCyan,   Color.DarkGray),
            hotFocus:  (Color.BrightCyan,   Color.Gray),
            disabled:  (Color.Gray,         Color.DarkGray));
        Assign(Colors.Menu, d,
            normal:    (Color.White,        Color.Blue),
            focus:     (Color.White,        Color.BrightBlue),
            hotNormal: (Color.BrightCyan,   Color.Blue),
            hotFocus:  (Color.White,        Color.BrightBlue),
            disabled:  (Color.Gray,         Color.Blue));
        Assign(Colors.Error, d,
            normal:    (Color.BrightRed,    Color.Blue),
            focus:     (Color.White,        Color.Red),
            hotNormal: (Color.BrightYellow, Color.Blue),
            hotFocus:  (Color.BrightYellow, Color.Red),
            disabled:  (Color.Red,          Color.Blue));
    }

    private static void ApplyMinimalMono(ConsoleDriver d)
    {
        var normal   = (Color.White,        Color.Black);
        var focus    = (Color.Black,        Color.White);
        var hotN     = (Color.BrightYellow, Color.Black);
        var hotF     = (Color.Black,        Color.White);
        var disabled = (Color.DarkGray,     Color.Black);

        Assign(Colors.TopLevel, d, normal, focus, hotN, hotF, disabled);
        Assign(Colors.Base,     d, normal, focus, hotN, hotF, disabled);
        Assign(Colors.Dialog,   d, normal, focus, hotN, hotF, disabled);
        Assign(Colors.Menu,     d, normal, focus, hotN, hotF, disabled);
        Assign(Colors.Error, d,
            normal:    (Color.BrightRed,    Color.Black),
            focus:     (Color.White,        Color.Red),
            hotNormal: (Color.BrightYellow, Color.Black),
            hotFocus:  (Color.BrightYellow, Color.Red),
            disabled:  (Color.Red,          Color.Black));
    }

    private static void ApplyRetroGreen(ConsoleDriver d)
    {
        var panes = (normal: (Color.BrightGreen,  Color.Black),
                     focus:  (Color.Black,        Color.BrightGreen),
                     hotN:   (Color.BrightYellow, Color.Black),
                     hotF:   (Color.Black,        Color.BrightGreen),
                     dis:    (Color.Green,        Color.Black));

        Assign(Colors.TopLevel, d, panes.normal, panes.focus, panes.hotN, panes.hotF, panes.dis);
        Assign(Colors.Base,     d, panes.normal, panes.focus, panes.hotN, panes.hotF, panes.dis);
        Assign(Colors.Dialog,   d, panes.normal, panes.focus, panes.hotN, panes.hotF, panes.dis);
        Assign(Colors.Menu, d,
            normal:    (Color.Black,        Color.BrightGreen),
            focus:     (Color.BrightGreen,  Color.Black),
            hotNormal: (Color.BrightYellow, Color.BrightGreen),
            hotFocus:  (Color.BrightYellow, Color.Black),
            disabled:  (Color.Green,        Color.BrightGreen));
        Assign(Colors.Error, d,
            normal:    (Color.BrightRed,    Color.Black),
            focus:     (Color.White,        Color.Red),
            hotNormal: (Color.BrightYellow, Color.Black),
            hotFocus:  (Color.BrightYellow, Color.Red),
            disabled:  (Color.Red,          Color.Black));
    }
}
