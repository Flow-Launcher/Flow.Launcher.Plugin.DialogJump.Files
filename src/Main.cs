using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.DialogJump.Files;

public class Main : IPlugin, IPluginI18n, IDialogJumpExplorer
{
    private static readonly string ClassName = nameof(Main);

    internal static PluginInitContext Context { get; private set; } = null!;

    public void Init(PluginInitContext context)
    {
        Context = context;
    }

    public List<Result> Query(Query query)
    {
        return [];
    }

    public string GetTranslatedPluginTitle()
    {
        return Context.API.GetTranslation("flowlauncher_plugin_dialog_jump_files_plugin_name");
    }

    public string GetTranslatedPluginDescription()
    {
        return Context.API.GetTranslation("flowlauncher_plugin_dialog_jump_files_plugin_description");
    }

    public IDialogJumpExplorerWindow? CheckExplorerWindow(nint hwnd)
    {
        IDialogJumpExplorerWindow? filesWindow = null;

        // Is it from Files?
        string processName;
        try
        {
            processName = Win32Helper.GetProcessNameFromHwnd(new(hwnd));
        }
        catch (Exception e)
        {
            Context.API.LogWarn(ClassName, $"Failed to get process name: {e}");
            return null;
        }
        if (processName.Equals("files.exe", StringComparison.OrdinalIgnoreCase))
        {
            // Is it Files's file window?
            try
            {
                UIA3Automation automation = new();
                AutomationElement Files = automation.FromHandle(hwnd);
                string lowerFilesName = Files.Name.ToLower();
                if (lowerFilesName == "files" || lowerFilesName.Contains("- files"))
                {
                    filesWindow = new FilesWindow(hwnd, automation, Files);
                }
            }
            catch (TimeoutException e)
            {
                Context.API.LogWarn(ClassName, $"UIA timeout: {e}");
            }
            catch (Exception e)
            {
                Context.API.LogWarn(ClassName, $"Failed to bind window: {e}");
            }
        }

        return filesWindow;
    }

    public void Dispose()
    {

    }

    private class FilesWindow(IntPtr hWnd, UIA3Automation automation, AutomationElement Files) : IDialogJumpExplorerWindow, IDisposable
    {
        private readonly UIA3Automation _automation = automation;
        private readonly AutomationElement _Files = Files;

        public IntPtr Handle { get; } = hWnd;

        public void Dispose()
        {
            _automation.Dispose();
        }

        public string? GetExplorerPath()
        {
            return GetCurrentTab().GetCurrentFolder();
        }

        private FilesTab GetCurrentTab()
        {
            return new FilesTab(_Files);
        }

        private class FilesTab
        {
            private static readonly string ClassName = nameof(FilesTab);

            private readonly TextBox? _currentPathGet;
            private readonly TextBox? _currentPathSet;

            public FilesTab(AutomationElement Files)
            {
                // Find window content to reduce the scope
                AutomationElement _windowContent = Files.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));

                _currentPathGet ??= _windowContent.FindFirstChild(cf => cf.ByAutomationId("CurrentPathGet"))?.AsTextBox();
                if (_currentPathGet == null)
                {
                    // TODO: Fix issue here
                    Context.API.LogError(ClassName, "Failed to find CurrentPathGet");
                }

                _currentPathSet ??= _windowContent.FindFirstChild(cf => cf.ByAutomationId("CurrentPathSet"))?.AsTextBox();
                if (_currentPathSet == null)
                {
                    // TODO: Fix issue here
                    Context.API.LogError(ClassName, "Failed to find CurrentPathSet");
                }
            }

            public string GetCurrentFolder()
            {
                try
                {
                    return _currentPathGet!.Text;
                }
                catch (Exception e)
                {
                    Context.API.LogError(ClassName, $"Failed to get current folder: {e}");
                    return string.Empty;
                }
            }

            public bool OpenFolder(string path)
            {
                try
                {
                    _currentPathSet!.Text = path;
                    return true;
                }
                catch (Exception e)
                {
                    Context.API.LogError(ClassName, $"Failed to get current folder: {e}");
                    return false;
                }
            }
        }
    }
}
