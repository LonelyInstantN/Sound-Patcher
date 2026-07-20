using System.Runtime.InteropServices;

namespace SoundSwitcher.Audio;

public sealed class AudioDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; set; }
    public bool IsHidden { get; set; }
}

public sealed class AudioManager
{
    private const uint DeviceStateActive = 0x1;

    private static readonly PropertyKey FriendlyNameKey = new(
        new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 14);

    private readonly IMMDeviceEnumerator _enumerator;

    public AudioManager()
    {
        _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
    }

    public List<AudioDeviceInfo> GetRenderDevices()
    {
        var result = new List<AudioDeviceInfo>();
        string? defaultId = GetDefaultDeviceId();

        Marshal.ThrowExceptionForHR(_enumerator.EnumAudioEndpoints(EDataFlow.eRender, DeviceStateActive, out var collection));
        Marshal.ThrowExceptionForHR(collection.GetCount(out uint count));

        for (uint i = 0; i < count; i++)
        {
            Marshal.ThrowExceptionForHR(collection.Item(i, out var device));
            Marshal.ThrowExceptionForHR(device.GetId(out string id));
            string name = GetDeviceName(device);
            result.Add(new AudioDeviceInfo
            {
                Id = id,
                Name = name,
                IsActive = id == defaultId
            });
            Marshal.ReleaseComObject(device);
        }
        Marshal.ReleaseComObject(collection);
        return result;
    }

    public string? GetDefaultDeviceId()
    {
        try
        {
            int hr = _enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out var device);
            if (hr != 0) return null;
            device.GetId(out string id);
            Marshal.ReleaseComObject(device);
            return id;
        }
        catch
        {
            return null;
        }
    }

    public void SetDefault(string deviceId)
    {
        var policyConfig = (IPolicyConfig)new PolicyConfigComObject();
        Marshal.ThrowExceptionForHR(policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole));
        Marshal.ThrowExceptionForHR(policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia));
        Marshal.ThrowExceptionForHR(policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications));
        Marshal.ReleaseComObject(policyConfig);
    }

    private static string GetDeviceName(IMMDevice device)
    {
        Marshal.ThrowExceptionForHR(device.OpenPropertyStore(0, out var store));
        var key = FriendlyNameKey;
        Marshal.ThrowExceptionForHR(store.GetValue(ref key, out var pv));
        string name = "Unknown Device";
        if (pv.vt == 31 && pv.p != IntPtr.Zero)
        {
            name = Marshal.PtrToStringUni(pv.p) ?? name;
        }
        PropVariantClear(ref pv);
        Marshal.ReleaseComObject(store);
        return name;
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);
}
