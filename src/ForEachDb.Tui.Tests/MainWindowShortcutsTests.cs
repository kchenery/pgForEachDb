using AwesomeAssertions;
using ForEachDb.Tui.Views;
using NUnit.Framework;
using Terminal.Gui;

namespace ForEachDb.Tui.Tests;

[NonParallelizable] // Application state is global.
public class MainWindowShortcutsTests
{
    private TuiHarness _harness = null!;
    private MainWindow _main = null!;

    [SetUp]
    public void SetUp()
    {
        _harness = TuiHarness.Start();
        _main = new MainWindow(TuiHarness.FakeConnection("alpha", "beta", "gamma"));
    }

    [TearDown]
    public void TearDown()
    {
        _main.Dispose();
        _harness.Dispose();
    }

    [Test]
    public void RootKeyEvent_IsInstalled_AfterConstruction()
    {
        Application.RootKeyEvent.Should().NotBeNull("MainWindow must hook shortcuts globally or TextView focus swallows them");
    }

    [Test]
    public void CtrlR_IsHandled_EvenWithoutResults()
    {
        var handled = Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.R | Terminal.Gui.Key.CtrlMask));

        handled.Should().BeTrue("the key should be swallowed and routed to ShowResults (logs a hint when empty)");
    }

    [Test]
    public void CtrlN_SelectsNone()
    {
        _main.SelectedDatabaseCount.Should().Be(3);

        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.N | Terminal.Gui.Key.CtrlMask));

        _main.SelectedDatabaseCount.Should().Be(0);
    }

    [Test]
    public void CtrlA_SelectsAll_AfterSelectingNone()
    {
        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.N | Terminal.Gui.Key.CtrlMask));
        _main.SelectedDatabaseCount.Should().Be(0);

        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.A | Terminal.Gui.Key.CtrlMask));

        _main.SelectedDatabaseCount.Should().Be(3);
    }

    [Test]
    public void CtrlL_CyclesLogFilter()
    {
        _main.CurrentLogFilter.Should().Be(LogFilter.All);

        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.L | Terminal.Gui.Key.CtrlMask));
        _main.CurrentLogFilter.Should().Be(LogFilter.SelectedDatabase);

        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.L | Terminal.Gui.Key.CtrlMask));
        _main.CurrentLogFilter.Should().Be(LogFilter.FailedOnly);

        Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.L | Terminal.Gui.Key.CtrlMask));
        _main.CurrentLogFilter.Should().Be(LogFilter.All);
    }

    [Test]
    public void Unmapped_Key_IsNotHandled()
    {
        var handled = Application.RootKeyEvent!(TuiHarness.Key(Terminal.Gui.Key.Z | Terminal.Gui.Key.CtrlMask));

        handled.Should().BeFalse();
    }
}
