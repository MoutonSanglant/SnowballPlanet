using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SnowballPlanet
{
    [RequireComponent(typeof(AudioSource))]
    public class SnowballSFX : MonoBehaviour
    {
        [SerializeField] private float TimeWindowDuration = 1f;
        [SerializeField] private float MuteWindow = 0f;
        [SerializeField] private float TimeBetweenNotes = 0.6f;
        [SerializeField] private AudioSource SFXAudioSource;
        [SerializeField] private AudioSource SoundTrackAudioSource;
        [SerializeField] private AudioClip BlankSFX;
        [SerializeField] private AudioClip NoteSFX;

        // Notes
        private float _nextNoteTimestamp;
        private Partition _partition;
        private IEnumerator _partitionReader;

        private void Awake()
        {
            _partition = new Partition(SoundTrackAudioSource, NoteSFX, BlankSFX);

            var snowballController = GetComponent<SnowballController>();

            snowballController.OnItemPickup += PlaySound;
            SnowballController.OnVictory += PlayFullPartition;
            snowballController.OnControllerMove += moveAmount =>
            {
                SFXAudioSource.volume = Mathf.Clamp01(moveAmount.magnitude);
            };
        }

        private void OnDestroy()
        {
            SnowballController.OnVictory -= PlayFullPartition;
        }

        private void PlayFullPartition()
        {
            StartCoroutine(PlayAllNotes());
        }

        private void PlaySound(PickableItem item)
        {
            if (Time.time < _nextNoteTimestamp)
            {
                if (Time.time < _nextNoteTimestamp - TimeWindowDuration + MuteWindow)
                {
                    // Don't play any sound when last sound has been played recently
                }
                else
                {
                    _partitionReader.MoveNext();

                    if (_partitionReader.Current == null)
                    {
                        _partitionReader = _partition.PlaySingleNote(true);
                        _partitionReader.MoveNext();
                    }

                    _nextNoteTimestamp = Time.time + TimeWindowDuration;
                }
            }
            else
            {
                _partitionReader = _partition.PlaySingleNote(true, _nextNoteTimestamp > 0.01f);
                _partitionReader.MoveNext();

                _nextNoteTimestamp = Time.time + TimeWindowDuration;
            }
        }

        private IEnumerator PlayAllNotes()
        {
            _partitionReader = _partition.PlaySingleNote();
            _partitionReader.MoveNext();

            while (_partitionReader.Current != null)
            {
                yield return new WaitForSeconds(TimeBetweenNotes);

                _partitionReader.MoveNext();
            }
        }

        private class Partition
        {
            //A = 0, // La
            //B = 2, // Si
            //C = 4, // Do
            //D = 6, // Ré
            //E = 8, // Mi
            //F = 10, // Fa
            //G = 12, // Sol
            private Note[] _notes = new[]
            {
                // Phrase 1
                Note.E, Note.E, Note.E, Note.Z,
                Note.E, Note.E, Note.E, Note.Z,
                Note.E, Note.G, Note.C, Note.D,
                Note.E, Note.Z, Note.Z, Note.Z,
                // Phrase 2
                Note.F, Note.F, Note.F, Note.Z,
                Note.E, Note.E, Note.E, Note.Z,
                Note.E, Note.D, Note.D, Note.E,
                Note.D, Note.Z, Note.G, Note.Z,
                // Phrase 3
                Note.E, Note.E, Note.E, Note.Z,
                Note.E, Note.E, Note.E, Note.Z,
                Note.E, Note.G, Note.C, Note.D,
                Note.E, Note.Z, Note.Z, Note.Z,
                // Phrase 4
                Note.F, Note.F, Note.F, Note.Z,
                Note.E, Note.E, Note.E, Note.Z,
                Note.G, Note.G, Note.F, Note.D,
                Note.C, Note.Z, Note.Z, Note.Z,
            };

            private AudioSource _source;
            private AudioClip _noteSfx;
            private AudioClip _blankSfx;

            public Partition(AudioSource source, AudioClip noteSfx, AudioClip blankSfx) => (_source, _noteSfx, _blankSfx) = (source, noteSfx, blankSfx);

            public IEnumerator PlaySingleNote(bool playMutedNotes = false, bool failed = false)
            {
                var reader = new Note[_notes.Length];

                Array.Copy(_notes, reader, _notes.Length);

                if (failed)
                {
                    _source.volume = 0.42f;
                    _source.PlayOneShot(_blankSfx);

                    yield return reader;
                }

                foreach (var note in reader)
                {
                    if (note == Note.Z)
                    {
                        if (playMutedNotes)
                        {
                            _source.volume = 0.14f;
                            _source.PlayOneShot(_blankSfx);
                        }
                    }
                    else
                    {
                        _source.volume = 1f;
                        _source.pitch = NoteToPitch(note);
                        _source.PlayOneShot(_noteSfx);
                    }

                    yield return reader;
                }

                yield return null;
            }

            private float NoteToPitch(Note note)
            {
                return 1f * Mathf.Pow(1.05946f, (float)note);
            }
        }

        private enum Note
        {
            A = 0, // La
            B = 2, // Si
            C = 4, // Do
            D = 6, // Ré
            E = 8, // Mi
            F = 10, // Fa
            G = 12, // Sol
            Z = 0, // Empty
        }
    }
}
