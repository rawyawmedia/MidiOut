using UnityEngine;

namespace RawYawMedia
{
    public class MidiOutComponent : MonoBehaviour
    {
        private MidiOut _midiOut;

        void Start()
        {
            _midiOut = new MidiOut();
        }

        void Update()
        {
            _midiOut.Update(Time.deltaTime);
        }

        void OnDestroy()
        {
            _midiOut = null;
        }

        public void SendNoteDownAndUp(byte channel, byte note)
        {
            _midiOut.SendNoteDownAndUp(channel, note);
        }

        public void SendNoteDown(byte channel, byte note)
        {
            _midiOut.SendNoteDown(channel, note);
        }

        public void SendNoteUp(byte channel, byte note)
        {
            _midiOut.SendNoteUp(channel, note);
        }

        public void SendControlValue(byte channel, byte controlNumber, float value)
        {
            _midiOut.SendControlValue(channel, controlNumber, value);
        }
    }
}