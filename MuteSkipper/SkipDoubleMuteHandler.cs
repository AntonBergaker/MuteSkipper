using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuteSkipper;
public class SkipDoubleMuteService {

    private class DeviceListener {
        public string DeviceId { get; }

        private readonly MMDevice device;

        private bool lastValue;
        private DateTime lastTimeChanged;
        private int pressCount;
        private Task? pollTask;
        private CancellationTokenSource cancellation;

        public DeviceListener(MMDevice device) {
            this.device = device;
            DeviceId = device.ID;
            
            lastValue = GetMuted();
            lastTimeChanged = DateTime.Now;
            cancellation = new CancellationTokenSource();
        }

        private bool GetMuted() => device.AudioEndpointVolume.Mute;

        public void Start() {
            Task.Run(() => PollState(), cancellation.Token);
        }

        public async Task PollState() {
            while (cancellation.IsCancellationRequested == false) {
                var isMuted = GetMuted();
                if (isMuted != lastValue) {
                    if (DateTime.Now - lastTimeChanged < TimeSpan.FromMilliseconds(500)) {
                        pressCount++;
                        if (pressCount == 1) {
                            MediaKeyManager.SendPlayPauseCommand();
                        }
                        if (pressCount == 2) {
                            MediaKeyManager.SendSkipCommand();
                            device.AudioEndpointVolume.Mute = lastValue;
                        }

                    } else {
                        pressCount = 0;
                    }

                    lastValue = isMuted;
                    lastTimeChanged = DateTime.Now;
                }

                try {
                    await Task.Delay(75, cancellation.Token);
                } catch (TaskCanceledException) {
                    break;
                }
            }
        }

        private bool cancelled = false;
        public void Cancel() {
            if (cancelled) {
                return;
            }
            cancelled = true;
            cancellation.Cancel();

            device.Dispose();
        }
    }

    private DeviceListener? currentListener;

    public string? CurrentDeviceId => currentListener?.DeviceId;

    public SkipDoubleMuteService() {
    }


    public async Task SetDevice(string deviceId) {
        
        var devices = await GetDevicesInternal();

        MMDevice? targetDevice = null;

        foreach (var device in devices) {
            if (targetDevice == null && device.ID == deviceId) {
                targetDevice = device;
                continue;
            }

            device.Dispose();
        }

        if (targetDevice == null) {
            return;
        }

        currentListener?.Cancel();
        currentListener = new(targetDevice);
        currentListener.Start();
    }

    public record DeviceData(string DeviceName, string DeviceId);

    public async Task<DeviceData[]> GetAvailableDevices() {
        var devices = await GetDevicesInternal();
        var array = devices.Select(x => new DeviceData(x.FriendlyName, x.ID)).ToArray();
        foreach (var device in devices) {
            device.Dispose();
        }
        return array;
    }

    private Task<MMDevice[]> GetDevicesInternal() {
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        return Task.FromResult(
            enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All).Where(x => x.State == DeviceState.Active).ToArray()
        );
    }
}
