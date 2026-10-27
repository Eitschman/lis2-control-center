using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LIS2.WmpLegacy.Audio;

/// <summary>Erfasst ausschließlich den angegebenen Prozessbaum; kein Systemmix und keine Ersatzquelle.</summary>
[SupportedOSPlatform("windows10.0.20348")]
public sealed class ProcessLoopbackCapture : IDisposable
{
    public const int SampleRate = 44100;
    private readonly AutoResetEvent _samplesReady = new(false);
    private IAudioClient? _audio;
    private IAudioCaptureClient? _capture;
    private bool _started;

    public void Start(int processId)
    {
        if (_audio is not null) throw new InvalidOperationException("Capture already initialized.");
        var parameters = Marshal.AllocHGlobal(12);
        IActivationOperation? operation = null;
        var completion = new ActivationCompletion();
        try
        {
            // AUDIOCLIENT_ACTIVATION_PARAMS: Prozess-Loopback, Ziel-PID, INCLUDE_TARGET_PROCESS_TREE.
            Marshal.WriteInt32(parameters, 0, 1);
            Marshal.WriteInt32(parameters, 4, processId);
            Marshal.WriteInt32(parameters, 8, 0);
            var variant = new ActivationVariant { Type = 65, Size = 12, Data = parameters };
            var identifier = typeof(IAudioClient).GUID;
            Check(ActivateAudioInterfaceAsync("VAD\\Process_Loopback", ref identifier,
                ref variant, completion, out operation));
            // Windows hält den Callback bis zum Abschluss. Die Aktivierungsdaten bleiben bis dahin gültig.
            _audio = completion.Result.Task.GetAwaiter().GetResult();
            var format = new WaveFormat
            {
                FormatTag = 1, Channels = 2, SamplesPerSecond = SampleRate,
                BitsPerSample = 16, BlockAlign = 4, AverageBytesPerSecond = SampleRate * 4
            };
            Check(_audio.Initialize(0, 0x80000000u | 0x20000u | 0x40000u,
                0, 0, ref format, IntPtr.Zero));
            Check(_audio.SetEventHandle(_samplesReady.SafeWaitHandle.DangerousGetHandle()));
            var captureId = typeof(IAudioCaptureClient).GUID;
            Check(_audio.GetService(ref captureId, out var service));
            _capture = (IAudioCaptureClient)service;
            Check(_audio.Start());
            _started = true;
        }
        finally
        {
            if (operation is not null) Marshal.ReleaseComObject(operation);
            Marshal.FreeHGlobal(parameters);
            GC.KeepAlive(completion);
        }
    }

    public void ReadAvailable(Action<short[], bool> consume)
    {
        if (_capture is null) throw new InvalidOperationException("Capture is not running.");
        _samplesReady.WaitOne(100);
        Check(_capture.GetNextPacketSize(out var available));
        while (available > 0)
        {
            Check(_capture.GetBuffer(out var data, out var frames, out var flags, out _, out _));
            try
            {
                var samples = new short[checked((int)frames * 2)];
                // AUDCLNT_BUFFERFLAGS_SILENT erlaubt einen ungültigen Datenzeiger.
                if ((flags & 2) == 0) Marshal.Copy(data, samples, 0, samples.Length);
                consume(samples, (flags & 1) != 0);
            }
            finally { Check(_capture.ReleaseBuffer(frames)); }
            Check(_capture.GetNextPacketSize(out available));
        }
    }

    public void Dispose()
    {
        try { if (_started && _audio is not null) Check(_audio.Stop()); }
        finally
        {
            _started = false;
            if (_capture is not null) Marshal.ReleaseComObject(_capture);
            if (_audio is not null) Marshal.ReleaseComObject(_audio);
            _capture = null;
            _audio = null;
            _samplesReady.Dispose();
        }
    }

    private static void Check(int result) => Marshal.ThrowExceptionForHR(result);

    [DllImport("Mmdevapi.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int ActivateAudioInterfaceAsync(string path, ref Guid identifier,
        ref ActivationVariant parameters, IActivationCompletion completion, out IActivationOperation operation);

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct ActivationVariant
    {
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(8)] public int Size;
        [FieldOffset(16)] public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WaveFormat
    {
        public ushort FormatTag, Channels;
        public uint SamplesPerSecond, AverageBytesPerSecond;
        public ushort BlockAlign, BitsPerSample, ExtraSize;
    }

    [ComVisible(true), Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActivationCompletion
    {
        [PreserveSig] int ActivateCompleted(IActivationOperation operation);
    }

    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActivationOperation
    {
        [PreserveSig] int GetActivateResult(out int result, [MarshalAs(UnmanagedType.IUnknown)] out object? audio);
    }

    [ComVisible(true), Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAgileObject { }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class ActivationCompletion : IActivationCompletion, IAgileObject
    {
        internal TaskCompletionSource<IAudioClient> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ActivateCompleted(IActivationOperation operation)
        {
            try
            {
                Check(operation.GetActivateResult(out var result, out var audio));
                Check(result);
                Result.TrySetResult((IAudioClient)(audio ?? throw new InvalidOperationException("No audio interface returned.")));
            }
            catch (Exception error) { Result.TrySetException(error); }
            return 0;
        }
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint flags, long duration, long periodicity, ref WaveFormat format, IntPtr session);
        [PreserveSig] int GetBufferSize(out uint frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint frames);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closest);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long normal, out long minimum);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr handle);
        [PreserveSig] int GetService(ref Guid identifier, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong position, out ulong timestamp);
        [PreserveSig] int ReleaseBuffer(uint frames);
        [PreserveSig] int GetNextPacketSize(out uint frames);
    }
}
