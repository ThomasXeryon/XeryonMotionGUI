using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace XeryonMotionGUI.Services;
public class UsbDeviceWatcherService
{
    private DeviceWatcher deviceWatcher;

    // Event to notify when a USB device is added
    public event EventHandler<string> DeviceAdded;
    // Event to notify when a USB device is removed
    public event EventHandler<string> DeviceRemoved;

    public UsbDeviceWatcherService()
    {
        //StartUSBDeviceWatcher();
    }

    // Start the USB device watcher
    private void StartUSBDeviceWatcher()
    {
        Debug.WriteLine("Starting USB device watcher");

        // Create a watcher for USB devices
        deviceWatcher = DeviceInformation.CreateWatcher("System.Devices.InterfaceClassGuid:=\"{A5DCBF10-6530-11D2-901F-00C04FB951ED}\"");

        // Trigger when a USB device is added
        deviceWatcher.Added += (DeviceWatcher sender, DeviceInformation deviceInfo) =>
        {
            Debug.WriteLine($"USB device added: {deviceInfo.Name}");
            DeviceAdded?.Invoke(this, deviceInfo.Name); // Raise event for added device
        };

        // Trigger when a USB device is removed
        deviceWatcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate) =>
        {
            Debug.WriteLine($"USB device removed: {deviceInfoUpdate.Id}");
            DeviceRemoved?.Invoke(this, deviceInfoUpdate.Id); // Raise event for removed device
        };

        // Start the watcher
        deviceWatcher.Start();
    }

    // Stop the USB device watcher
    public void StopUSBDeviceWatcher()
    {
        if (deviceWatcher != null)
        {
            Debug.WriteLine("Stopping USB device watcher");
            deviceWatcher.Stop();
            deviceWatcher = null;
        }
    }
}