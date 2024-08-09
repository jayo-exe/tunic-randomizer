using UnityEngine;

namespace Klak.Spout
{

    //
    // Spout sender class (properties)
    //
    partial class SpoutSender
    {
        #region Spout source

        string _spoutName = "Spout Sender";

        public string spoutName
        {
            get => _spoutName;
            set => ChangeSpoutName(value);
        }

        void ChangeSpoutName(string name)
        {
            // Sender refresh on renaming
            if (_spoutName == name) return;
            _spoutName = name;
            ReleaseSender();
        }

        Texture _sourceTexture = null;

        public Texture sourceTexture
        {
            get => _sourceTexture;
            set => _sourceTexture = value;
        }

        #endregion
    }

} // namespace Klak.Spout