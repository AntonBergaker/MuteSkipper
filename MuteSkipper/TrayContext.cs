
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

        // Initialize Tray Icon
        var strip = new ContextMenuStrip();

        strip.MouseEnter += async (_, _) => await GetAvailableDevices();

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
            if (device.DeviceId == deviceId) {
                await handler.SetDevice(device.DeviceId);
                break;
            }
        }
        PopulateDeviceList(devices);
    }

    private bool waitingForDevices = false;

    private async Task GetAvailableDevices() {
        if (waitingForDevices) {
            return;
        }
        waitingForDevices = true;
        var devices = await handler.GetAvailableDevices();

        waitingForDevices = false;
        PopulateDeviceList(devices);
    }

    private HashSet<string> previousAvailableDevices = new();
    private void PopulateDeviceList(SkipDoubleMuteService.DeviceData[] devices) {
        // Same devices, don't have to do anything
        if (IsTheSameDevices(previousAvailableDevices, devices)) {
            return;
        }

        previousAvailableDevices = devices.Select(x => x.DeviceId).ToHashSet();
        deviceStrip.DropDownItems.Clear();
        foreach (var device in devices) {
            var entry = new ToolStripMenuItem(device.DeviceName);
            entry.CheckOnClick = true;
            entry.CheckedChanged += (_, _) => Entry_CheckedChanged(entry, device.DeviceId);

            if (handler.CurrentDeviceId == device.DeviceId) {
                entry.Checked = true;
            }
            
            deviceStrip.DropDownItems.Add(entry);
        }
    }

    /// <summary>
    /// Checks if the devices are the same in the two sets
    /// </summary>
    private bool IsTheSameDevices(HashSet<string> previous, SkipDoubleMuteService.DeviceData[] newDevices) {
        if (previous.Count != newDevices.Length) {
            return false;
        }

        foreach (var device in newDevices) {
            if (previous.Contains(device.DeviceId) == false) {
                return false;
            }
        }

        return true;
    }

    private void Entry_CheckedChanged(ToolStripMenuItem entry, string deviceId) {
        if (entry.Checked == false) {
            return;
        }

        foreach (ToolStripMenuItem item in deviceStrip.DropDownItems) {
            if (item == entry) {
                continue;
            }
            item.Checked = false;
        }

        var dir = Path.GetDirectoryName(saveFilePath)!;
        if (Directory.Exists(dir) == false) {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(saveFilePath, deviceId);
        _ = handler.SetDevice(deviceId);
    }

    private void Exit(object? sender, EventArgs e) {
        trayIcon.Visible = false;

        Application.Exit();
    }
}