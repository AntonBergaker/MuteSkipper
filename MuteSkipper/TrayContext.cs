
using AudioSwitcher.AudioApi.CoreAudio;
using MuteSkipper.Properties;

namespace MuteSkipper;
public class TrayContext : ApplicationContext {
    private readonly NotifyIcon trayIcon;
    private readonly SkipDoubleMuteService handler;
    private readonly ToolStripMenuItem deviceStrip;

    private readonly string saveFilePath;

    public TrayContext() {
        handler = new SkipDoubleMuteService();

        deviceStrip = new ToolStripMenuItem();
        deviceStrip.Text = "Select device";
        deviceStrip.DropDownItems.Add("Loading devices...");
        _ = GetAvailableDevices();

        // Initialize Tray Icon
        var strip = new ContextMenuStrip();

        strip.MouseEnter += (_, _) => _ = GetAvailableDevices();

        var exit = new ToolStripMenuItem();
        exit.Text = "Exit";
        exit.Click += Exit;

        strip.Items.Add(deviceStrip);
        strip.Items.Add(exit);

        trayIcon = new NotifyIcon() {
            Icon = Resources.TaskBarIcon,
            ContextMenuStrip = strip,
            Visible = true
        };

        saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MuteSkipper", "LastDeviceId.txt");
        if (File.Exists(saveFilePath)) {
            var lastDevice = File.ReadAllText(saveFilePath);
            _ = SetDeviceFromId(lastDevice);
        }
    }

    private async Task SetDeviceFromId(string deviceId) {
        var devices = await handler.GetAvailableDevices();
        foreach (var device in devices) {
            if (device.RealId == deviceId) {
                handler.SetDevice(device);
                break;
            }
        }
    }

    bool waitingForDevices = false;

    private async Task GetAvailableDevices() {
        if (waitingForDevices) {
            return;
        }
        waitingForDevices = true;
        var devices = await handler.GetAvailableDevices();
        waitingForDevices = false;
        deviceStrip.DropDownItems.Clear();
        foreach (var device in devices) {
            var entry = new ToolStripMenuItem(device.FullName);
            entry.CheckOnClick = true;
            entry.CheckedChanged += (_, _) => Entry_CheckedChanged(entry, device);
            if (handler.CurrentDevice?.RealId == device.RealId) {
                entry.Checked = true;
            }
            deviceStrip.DropDownItems.Add(entry);
        }
    }

    private void Entry_CheckedChanged(ToolStripMenuItem entry, CoreAudioDevice device) {
        if (entry.Checked == false) {
            return;
        }

        foreach (ToolStripMenuItem item in deviceStrip.DropDownItems) {
            if (item == entry) {
                continue;
            }
            item.Checked = false;
        }

        var dir = Path.GetDirectoryName(saveFilePath);
        if (Directory.Exists(dir) == false) {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(saveFilePath, device.RealId);
        handler.SetDevice(device);
    }

    private void Exit(object? sender, EventArgs e) {
        trayIcon.Visible = false;

        Application.Exit();
    }
}