using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;
using TunicRandomizer;

namespace Klak.Spout
{

    static class Plugin
    {
        // Receiver interop data structure
        // Should match with KlakSpout::Receiver::InteropData (Receiver.h)
        [StructLayout(LayoutKind.Sequential)]
        public struct ReceiverData
        {
            public uint width, height;
            public IntPtr texturePointer;
        }

    [DllImport("KlakSpout")]
    public static extern IntPtr GetRenderEventCallback();

    [DllImport("KlakSpout")]
    public static extern IntPtr CreateSender(string name, int width, int height);

    [DllImport("KlakSpout")]
    public static extern void GetSenderNames
    (out IntPtr[] names, out int count);

    }

} // namespace Klak.Spout