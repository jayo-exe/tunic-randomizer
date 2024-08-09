using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using TunicRandomizer;

namespace Klak.Spout
{

    // Render event IDs
    // Should match with KlakSpout::EventID (Event.h)
    enum EventID
    {
        UpdateSender,
        UpdateReceiver,
        CloseSender,
        CloseReceiver
    }

    // Render event attachment data structure
    // Should match with KlakSpout::EventData (Event.h)
    [StructLayout(LayoutKind.Sequential)]
    struct EventData
    {
        public IntPtr instancePointer;
        public IntPtr texturePointer;

        public EventData(IntPtr instance, IntPtr texture)
        {
            instancePointer = instance;
            texturePointer = texture;
        }

        public EventData(IntPtr instance)
        {
            instancePointer = instance;
            texturePointer = IntPtr.Zero;
        }
    }

    class EventKicker : IDisposable
    {
        public EventKicker(EventData data)
          => _dataMem = GCHandle.Alloc(data, GCHandleType.Pinned);

        public void Dispose()
      => MemoryPool.FreeOnEndOfFrame(_dataMem);

        public void IssuePluginEvent(EventID eventID)
        {
            TunicLogger.LogInfo("Issuing Plugin Event");
            if (_cmdBuffer == null)
                _cmdBuffer = new CommandBuffer();
            else
                _cmdBuffer.Clear();
            TunicLogger.LogInfo($"Cleared Command Buffer, re-issuing for objuect pinned at {_dataMem.AddrOfPinnedObject().ToInt64()}");
            _cmdBuffer.IssuePluginEventAndData
              (Plugin.GetRenderEventCallback(),
               (int)eventID, _dataMem.AddrOfPinnedObject());
            TunicLogger.LogInfo("Executihng Command Buffer");
            Graphics.ExecuteCommandBuffer(_cmdBuffer);
            TunicLogger.LogInfo("Issuing complete!");
        }

        static CommandBuffer _cmdBuffer;
        GCHandle _dataMem;
        static Stack<GCHandle> _toBeFreed = new Stack<GCHandle>();
    }

} // namespace Klak.Spout