using UnityEngine;
using UnityEngine.Rendering;
using TunicRandomizer;

namespace Klak.Spout
{

    //
    // Spout sender class (main implementation)
    //
    public sealed partial class SpoutSender : MonoBehaviour
    {
        #region Sender plugin object

        Sender _sender;

        void ReleaseSender()
        {
            _sender?.Dispose();
            _sender = null;
        }

        #endregion

        #region Buffer texture object

        RenderTexture _buffer;

        void PrepareBuffer(int width, int height)
        {
            // If the buffer exists but has wrong dimensions, destroy it first.
            if (_buffer != null &&
                (_buffer.width != width || _buffer.height != height))
            {
                ReleaseSender();
                DestroyBuffer();
            }

            // Create a buffer if it hasn't been allocated yet.
            if (_buffer == null && width > 0 && height > 0)
            {
                _buffer = new RenderTexture(width, height, 0);
                _buffer.hideFlags = HideFlags.DontSave;
                _buffer.Create();
            }
        }

        void DestroyBuffer()
        {
            if (_buffer == null) return;

            if (Application.isPlaying)
                Object.Destroy(_buffer);
            else
                Object.DestroyImmediate(_buffer);
            
            _buffer = null;
        }

        #endregion


        #region MonoBehaviour implementation

        void OnDisable()
        {
            ReleaseSender();
            PrepareBuffer(0, 0);
        }

        void Update()
        {

            if (_sourceTexture == null) return;
            PrepareBuffer(_sourceTexture.width, _sourceTexture.height);
            Graphics.Blit(_sourceTexture, _buffer);

            // Sender lazy initialization
            if (_sender == null)
            {
                TunicLogger.LogInfo("No sendr in place! creating...");
                _sender = new Sender(_spoutName, _buffer);
            }

            // Sender plugin-side update
            _sender.Update();
            MemoryPool.OnEndOfFrame();
        }

        #endregion
    }

} // namespace Klak.Spout