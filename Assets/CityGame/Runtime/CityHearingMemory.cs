using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Remembers a sound location, never the emitter's subsequent hidden position.
    public sealed class CityHearingMemory
    {
        public const float Duration = 6f;
        public Vector3 Position { get; private set; }
        public float Remaining { get; private set; }
        public bool Active => Remaining > 0f;

        public bool Hear(Vector3 source, Vector3 listener, bool clearPath, float radius)
        {
            if (!Finite(source) || !Finite(listener) || !float.IsFinite(radius) || radius <= 0f)
                throw new ArgumentException("Hearing requires finite positions and a positive radius.");
            if (!clearPath || (source - listener).sqrMagnitude > radius * radius) return false;
            Position = source;
            Remaining = Duration;
            return true;
        }

        public void Tick(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            Remaining = Mathf.Max(0f, Remaining - seconds);
        }

        public void Clear() => Remaining = 0f;
        private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
}
