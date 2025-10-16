# Prometheus.Devices Test Application

## Overview

This test application demonstrates the Prometheus.Devices library using:
- **Dependency Injection** via Microsoft.Extensions.DependencyInjection
- **Health Checks** for device status monitoring
- **DeviceManager** for centralized device management

## New Features

### 1. Dependency Injection

The application uses `ServiceCollection` to configure DI:

```csharp
var services = new ServiceCollection();
services.AddPrometheusDevicesCore(configuration); // Register services
services.AddHealthChecks()
    .AddDeviceHealthCheck("devices", failureStatus: HealthStatus.Degraded);
```

### 2. DeviceManager

All devices are registered in the central manager:

```csharp
// Create device
var printer = DeviceFactory.CreateDriverPrinter(connection, profile, driver);

// Register in DeviceManager
_deviceManager.RegisterDevice(printer);
```

**DeviceManager capabilities:**
- `RegisterDevice(device)` - register device
- `UnregisterDevice(deviceId)` - remove device
- `GetDevice(deviceId)` - get device by ID
- `GetDevicesByType<T>()` - get devices of specific type
- `ConnectAllAsync()` - connect all devices
- `DisconnectAllAsync()` - disconnect all devices

### 3. Health Checks

New menu option "5. Health Check all devices" shows the status of all registered devices:

```
Overall status: Healthy
Detailed information:
  [devices]: Healthy
    Description: All 2 devices healthy
    Data:
      TotalDevices: 2
      ReadyDevices: 2
      ErrorDevices: 0
```

**Health Check statuses:**
- `Healthy` - all devices working normally
- `Degraded` - no registered devices
- `Unhealthy` - devices with errors or disconnected

## Usage

### Manual Testing (Options 1-5)
1. Run the application
2. Select a device to test (1-4)
3. Device will be automatically registered in DeviceManager
4. Select option 5 to check status of all devices

### Configuration-Based Testing (Option 6)
1. Edit `appsettings.json` to configure your devices
2. Select option 6 "Load devices from appsettings.json"
3. All enabled devices will be loaded and registered automatically
4. You can then:
   - Connect to all devices at once
   - Test all printers
   - Capture from all cameras

## Configuration

The `appsettings.json` file contains settings and device configurations:

```json
{
  "PrometheusDevices": {
    "DefaultTimeout": 5000,
    "RetryAttempts": 3,
    "EnableHealthChecks": true,

    "Cameras": {
      "BuiltInCamera": {
        "Type": "Local",
        "Index": 0,
        "Resolution": "1920x1080",
        "FrameRate": 30,
        "Enabled": true
      }
    },

    "Printers": {
      "BixolonBK331": {
        "Type": "Driver",
        "IpAddress": "192.168.1.50",
        "Port": 9100,
        "ProfilePath": "printer.profile.json",
        "Enabled": false
      }
    }
  }
}
```

### Configuration Options

**Camera Types:**
- `Local` - Built-in or USB cameras (uses index)
- `IP` - Network IP cameras
- `USB` - USB cameras by VID/PID

**Printer Types:**
- `Driver` - ESC/POS printers with profile
- `Office` - System printers (Windows/Linux)
- `Network` - Network printers (TCP/IP)
- `Serial` - Serial port printers
- `USB` - USB printers by VID/PID

**Scanner Types:**
- `Office` - System scanners (SANE/WIA)

## Dependencies

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Diagnostics.HealthChecks
- Microsoft.Extensions.Hosting

## ASP.NET Core Integration

ServiceCollectionExtensions and DeviceHealthCheck can be easily integrated into ASP.NET Core applications:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddPrometheusDevicesCore(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDeviceHealthCheck("devices");

// Health Check endpoint
app.MapHealthChecks("/health");
```
