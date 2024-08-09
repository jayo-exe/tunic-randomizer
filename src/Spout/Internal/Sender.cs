﻿using UnityEngine;
using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;
using TunicRandomizer;
using System;

namespace Klak.Spout
{

    //
    // Wrapper class for sender instances on the native plugin side
    //
    sealed class Sender : IDisposable
    {
        #region Private objects

        IntPtr _plugin;
        EventKicker _event;

        #endregion

        #region Object lifecycle

        public Sender(string target, Texture texture)
        {
            // Plugin object allocation
            _plugin = Plugin.CreateSender(target, texture.width, texture.height);
            if (_plugin == IntPtr.Zero)
            {
                TunicLogger.LogError("Couldn't create sender plugin");
                return;
            }

            TunicLogger.LogInfo($"Created Spout Sender at {_plugin.ToInt64()}");

            // Event kicker (heap block for interop communication)
            _event = new EventKicker
              (new EventData(_plugin, texture.GetNativeTexturePtr()));

            // Initial update event
            _event.IssuePluginEvent(EventID.UpdateSender);
        }

        public void Dispose()
        {
            if (_plugin != IntPtr.Zero)
            {
                // Isssue the closer event to destroy the plugin object from the
                // render thread.
                _event.IssuePluginEvent(EventID.CloseSender);

                // Event kicker (interop memory) deallocation:
                // The close event above will refer to the block from the render
                // thread, so we actually can't free the memory here. To avoid this
                // problem, EventKicker uses MemoryPool to delay the memory
                // deallocation by the end of the frame.
                _event.Dispose();

                _plugin = IntPtr.Zero;
            }
        }

        #endregion

        #region Frame update method

        public void Update()
        {
            TunicLogger.LogInfo("Updating Sender Frame");
            try
            {
                _event?.IssuePluginEvent(EventID.UpdateSender);
            } catch(Exception e)
            {
                TunicLogger.LogError($"Error sending spout: {e.Message}");
            }
            
        }


        #endregion
    }

} // namespace Klak.Spout