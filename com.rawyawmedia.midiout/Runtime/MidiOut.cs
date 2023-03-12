using System;
using System.Collections.Generic;
using UnityEngine;
using RtMidiDll = RtMidi.Unmanaged;

namespace RawYawMedia
{
    unsafe sealed public class MidiOut
    {
        private class MidiPort
        {
            public int portNumber;
            public string portName;
            public RtMidiDll.Wrapper* rtmidi;
            public MidiPort(Minis.MidiProbe probe, int portNumber)
            {
                this.portNumber = portNumber;
                this.portName = probe.GetPortName(portNumber);
                this.rtmidi = RtMidiDll.OutCreateDefault();
                RtMidiDll.OpenPort(rtmidi, (uint)this.portNumber, "RtMidi Output");
            }

            ~MidiPort()
            {
                if (rtmidi == null || !rtmidi->ok)
                {
                    return;
                }

                RtMidiDll.OutFree(rtmidi);
                rtmidi = null;
            }
        }

        public RtMidiDll.Api api;
        private Queue<DelayedMidiNote> _delayedNoteUps = new Queue<DelayedMidiNote>();

        private const int MIDI_MESSAGE_SIZE = 3;
        private const byte NOTE_UP_STATUS = 8;
        private const byte NOTE_DOWN_STATUS = 9;
        private const byte CONTROL_CHANGE_STATUS = 11;

        Minis.MidiProbe _probe;
        List<MidiPort> _ports = new List<MidiPort>();

        float time = 0;

        private class DelayedMidiNote
        {
            public float time;
            public MidiNote note;
            public DelayedMidiNote(float time, MidiNote note)
            {
                this.time = time;
                this.note = note;
            }
        }

        private struct MidiNote
        {
            public byte channel;
            public byte note;
            public MidiNote(byte channel, byte note)
            {
                this.channel = channel;
                this.note = note;
            }
        }

        public MidiOut()
        {
            _probe = new Minis.MidiProbe();
            for (int n = 0; n < _probe.PortCount; n++)
            {
                _ports.Add(new MidiPort(_probe, n));
            }
        }

        ~MidiOut()
        {
            _ports.Clear();
        }

        public void Update(float dt)
        {
            this.time += dt;
            if (this.time > 1.0f)
            {
                this.time = 0;
            }

            for (byte n = 1; n < 13; n++)
            {
                SendControlValue(0, n, this.time < 0.5f ? 1.0f : 0);
            }

            foreach (var delayedNote in _delayedNoteUps)
            {
                delayedNote.time -= dt;
            }

            bool complete = false;
            while (!complete && _delayedNoteUps.Count > 0)
            {
                var delayedNote = _delayedNoteUps.Peek();
                if (delayedNote.time < 0)
                {
                    SendNoteUp(delayedNote.note.channel, delayedNote.note.note);
                    _delayedNoteUps.Dequeue();
                }
                else
                {
                    complete = true;
                }
            }
        }

        public void SendNoteDownAndUp(byte channel, byte note)
        {
            SendNoteDown(channel, note);
            _delayedNoteUps.Enqueue(new DelayedMidiNote(0.15f, new MidiNote(channel, note)));
        }

        public void SendNoteDown(byte channel, byte note)
        {
            byte* message = stackalloc byte[MIDI_MESSAGE_SIZE];

            message[0] = 0;
            message[0] |= (NOTE_DOWN_STATUS << 4);
            message[0] |= channel;
            message[1] = note;
            message[2] = 127; // velocity

            foreach(var port in _ports)
            {
                UnityEngine.Assertions.Assert.AreEqual(0, RtMidiDll.OutSendMessage(port.rtmidi, message, MIDI_MESSAGE_SIZE), "Failed to send note down message");
            }
        }

        public void SendNoteUp(byte channel, byte note)
        {
            byte* message = stackalloc byte[MIDI_MESSAGE_SIZE];

            message[0] = 0;
            message[0] |= (NOTE_UP_STATUS << 4);
            message[0] |= channel;
            message[1] = note;
            message[2] = 0; // velocity

            foreach (var port in _ports)
            {
                UnityEngine.Assertions.Assert.AreEqual(0, RtMidiDll.OutSendMessage(port.rtmidi, message, MIDI_MESSAGE_SIZE), "Failed to send note up message");
            }
        }

        public void SendControlValue(byte channel, byte controlNumber, float value)
        {
            byte* message = stackalloc byte[MIDI_MESSAGE_SIZE];

            message[0] = 0;
            message[0] |= (CONTROL_CHANGE_STATUS << 4);
            message[0] |= channel;
            message[1] = controlNumber;
            message[2] = (byte)(Mathf.Clamp(value, 0, 1) * 127);

            foreach (var port in _ports)
            {
                UnityEngine.Assertions.Assert.AreEqual(0, RtMidiDll.OutSendMessage(port.rtmidi, message, MIDI_MESSAGE_SIZE), "Failed to send control message");
            }
        }
    }
}
