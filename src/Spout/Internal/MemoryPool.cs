using UnityEngine.LowLevel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TunicRandomizer;

namespace Klak.Spout
{

    //
    // "Memory pool" class without actual memory pool functionality
    // At the moment, it only provides the delayed destruction method.
    //
    static class MemoryPool
    {
        #region Public method

        public static void FreeOnEndOfFrame(GCHandle gch)
          => _toBeFreed.Push(gch);

        #endregion

        #region Delayed destruction

        static Stack<GCHandle> _toBeFreed = new Stack<GCHandle>();

        public static void OnEndOfFrame()
        {
            TunicLogger.LogInfo($"Memory Pool end-of-frame reached, {_toBeFreed.Count} items to be freed");
            while (_toBeFreed.Count > 0)
            {
                GCHandle temphandle = _toBeFreed.Pop();
                TunicLogger.LogInfo($"freeing item pinned at {temphandle.AddrOfPinnedObject()}");
                temphandle.Free();
            }
        }

        #endregion

        static MemoryPool()
        {

        }
    }

} // namespace Klak.Spout