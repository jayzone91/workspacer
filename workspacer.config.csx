#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Gap;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;
using System.Diagnostics;

return new Action<IConfigContext>((IConfigContext context) =>
{
  var fontSize = 11;
  var barHeight = 19;
  var fontName = "Hack Nerd Font";
  var background = new Color(0x0, 0x0, 0x0);

  // Config
  context.CanMinimizeWindows = true;

  // Gap
  var gap = barHeight - 8;
  var gapPlugin = context.AddGap(new GapPluginConfig()
  {
    InnerGap = gap,
    OuterGap = gap / 2,
    Delta = gap / 2,
  });

  // Bar
  context.AddBar(new BarPluginConfig()
  {
    FontSize = fontSize,
    BarHeight = barHeight,
    FontName = fontName,
    DefaultWidgetBackground = background,
    LeftWidgets = () => new IBarWidget[] {
        new WorkspaceWidget(), new TextWidget(": "), new TitleWidget() {
          IsShortTitle = true
        }
      },
    RightWidgets = () => new IBarWidget[]{
        new TimeWidget(1000, "HH:mm:ss"),
        new ActiveLayoutWidget(),
      }
  });

  // Bar focus Indicator
  context.AddFocusIndicator();

  // Default Layouts.
  Func<ILayoutEngine[]> defaultLayout = () => new ILayoutEngine[]{
      new DwindleLayoutEngine(),
      new TallLayoutEngine(),
      new VertLayoutEngine(),
      new HorzLayoutEngine(),
      new FullLayoutEngine(),
    };
  context.DefaultLayouts = defaultLayout;

  // Workspaces
  // Array of workspace names and their layouts
  (string, ILayoutEngine[])[] workspaces = {
      ("main", defaultLayout()),
      ("🎶", defaultLayout()),
    };

  foreach ((string name, ILayoutEngine[] layouts) in workspaces)
  {
    context.WorkspaceContainer.CreateWorkspace(name, layouts);
  }

  // Filters
  context.WindowRouter.AddFilter((window) => !window.Title.Contains("iCloud-Passwörter"));
  context.WindowRouter.AddFilter((window) => !window.Title.Contains("PS Remote Play"));
  context.WindowRouter.AddFilter((window) => !window.Title.Contains("Hyper-V-Manager"));

  // Action Menu
  var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
  {
    RegisterKeybind = false,
    MenuHeight = barHeight * 2,
    FontSize = fontSize,
    FontName = fontName,
    Background = background
  });

  // Action menu builder
  Func<ActionMenuItemBuilder> createActionMenuBuilder = () =>
  {
    var menuBuilder = actionMenu.Create();

    // Switch to workspaces
    menuBuilder.AddMenu("switch", () =>
    {
      var workspaceMenu = actionMenu.Create();
      var monitor = context.MonitorContainer.FocusedMonitor;
      var workspaces = context.WorkspaceContainer.GetWorkspaces(monitor);

      Func<int, Action> createChildMenu = (workspaceindex) => () =>
      {
        context.Workspaces.SwitchMonitorToWorkspace(monitor.Index, workspaceindex);
      };

      int workspaceIndex = 0;
      foreach (var workspace in workspaces)
      {
        workspaceMenu.Add(workspace.Name, createChildMenu(workspaceIndex));
        workspaceIndex++;
      }

      return workspaceMenu;
    });

    // Move WIndow to Workspace
    menuBuilder.AddMenu("move", () =>
    {
      var moveMenu = actionMenu.Create();
      var focussedWorkspace = context.Workspaces.FocusedWorkspace;

      var workspaces = context.WorkspaceContainer.GetWorkspaces(focussedWorkspace).ToArray();
      Func<int, Action> createChildMenu = (index) => () =>
    {
      context.Workspaces.MoveFocusedWindowToWorkspace(index);
    };

      for (int i = 0; i < workspaces.Count(); i++)
      {
        moveMenu.Add(workspaces[i].Name, createChildMenu(i));
      }
      return moveMenu;
    });


    // Rename workspace
    menuBuilder.AddFreeForm("rename", (name) =>
    {
      context.Workspaces.FocusedWorkspace.Name = name;
    });

    // Create Workspace
    menuBuilder.AddFreeForm("create workspace", (name) =>
    {
      context.WorkspaceContainer.CreateWorkspace(name);
    });

    // delete focussed Workspace
    menuBuilder.Add("close", () =>
    {
      context.WorkspaceContainer.RemoveWorkspace(context.Workspaces.FocusedWorkspace);
    });

    // Workspacer
    menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
    menuBuilder.Add("toggle enabled", () => context.Enabled = !context.Enabled);
    menuBuilder.Add("restart", () => context.Restart());
    menuBuilder.Add("quit", () => context.Quit());

    string shutdownCmd;
    shutdownCmd = "/C shutdown /s /t 0";
    menuBuilder.Add("shutdown", () => System.Diagnostics.Process.Start("CMD.exe", shutdownCmd));

    return menuBuilder;
  };

  var actionMenuBuilder = createActionMenuBuilder();

  // Keybindings
  Action setKeybindings = () =>
  {
    KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
    KeyModifiers winCtrl = KeyModifiers.Win | KeyModifiers.Control;
    KeyModifiers win = KeyModifiers.Win;
    KeyModifiers alt = KeyModifiers.Alt;
    IKeybindManager manager = context.Keybinds;

    var workspaces = context.Workspaces;


    manager.UnsubscribeAll();
    manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());

    // Left, Right Keys
    manager.Subscribe(winCtrl, Keys.Left, () => workspaces.SwitchToPreviousWorkspace(), "switch to prev workspace");
    manager.Subscribe(winCtrl, Keys.Right, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");
    manager.Subscribe(winShift, Keys.Left, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move window to prev monitor");
    manager.Subscribe(winShift, Keys.Right, () => workspaces.MoveFocusedWindowToNextMonitor(), "move window to next monitor");

    // H, L keys
    manager.Subscribe(alt, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    manager.Subscribe(alt, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "Expand primary area");

    manager.Subscribe(winCtrl, Keys.H, () => workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
    manager.Subscribe(winCtrl, Keys.L, () => workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");

    // K, J keys
    manager.Subscribe(win, Keys.K, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and  next window");
    manager.Subscribe(win, Keys.J, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and prev window");

    manager.Subscribe(alt, Keys.K, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    manager.Subscribe(alt, Keys.J, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus prev window");

    manager.Subscribe(alt, Keys.P, () => actionMenu.ShowMenu(actionMenuBuilder), "show menu");

    manager.Subscribe(alt, Keys.B, () => System.Diagnostics.Process.Start("vivaldi"), "Open Browser");
    manager.Subscribe(alt, Keys.N, () => System.Diagnostics.Process.Start("vivaldi --incognito"), "Open Private Browser");
  };
  setKeybindings();
});

// OLD SHIT!
// using System;
// using workspacer;
// using workspacer.Bar;
// using workspacer.ActionMenu;
// using workspacer.FocusIndicator;
// using workspacer.Gap;
//
// Action<IConfigContext> doConfig = (context) =>
// {
//     var gap = 10;
//     context.AddGap(
//       new GapPluginConfig()
//       {
//         InnerGap = gap,
//         OuterGap = gap / 2,
//         Delta = gap / 2,
//       }
//     );
//     context.AddBar(new BarPluginConfig(){
//         FontSize = 11,
//         FontName = "Hack Nerd Font",
//         });
//     context.AddFocusIndicator();
//     var actionMenu = context.AddActionMenu();
//
//     context.WorkspaceContainer.CreateWorkspaces("1", "2", "3");
//     context.CanMinimizeWindows = true; // false by default
// };
// return doConfig;
