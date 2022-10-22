using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuteSkipper;
public class SkipDoubleMuteService {

    private record CurrentDeviceData(CoreAudioDevice Device, CancellationTokenSource Source, Task PollTask);

    private CurrentDeviceData? currentDeviceData;

    public CoreAudioDevice? CurrentDevice => currentDeviceData?.Device;

    public SkipDoubleMuteService() {
    }


    public void SetDevice(CoreAudioDevice device) {
        if (currentDeviceData != null) {
            currentDeviceData.Source.Cancel();
        }

        var source = new CancellationTokenSource();
        currentDeviceData = new(device, source, PollDeviceStatus(device, source.Token));
    }

    public async Task<CoreAudioDevice[]> GetAvailableDevices() {
        var controller = new CoreAudioController();
        var devices = await controller.GetCaptureDevicesAsync();
        return devices.Where(x => x.State == AudioSwitcher.AudioApi.DeviceState.Active).ToArray();
    }

    public async Task PollDeviceStatus(CoreAudioDevice device, CancellationToken cancellationToken) {
        var lastSwitchTime = DateTime.Now;

        var lastValue = device.IsMuted;

        while (cancellationToken.IsCancellationRequested == false) {
            var newValue = device.IsMuted;

            if (newValue != lastValue) {
                if (DateTime.Now - lastSwitchTime < TimeSpan.FromMilliseconds(500)) {
                    Console.WriteLine("Skipped!");
                    MediaKeyManager.SendSkipCommand();
                }

                lastValue = newValue;
                lastSwitchTime = DateTime.Now;
            }

            try {
                await Task.Delay(100, cancellationToken);
            } catch (TaskCanceledException) {
                break;
            }
        }

    }
}
