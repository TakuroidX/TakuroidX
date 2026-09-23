using System;

namespace Game
{
    /// <summary>
    /// Pure reel-motion math (no Unity dependency, EditMode-testable).
    ///
    /// Coordinate system: Position is a continuous "top row strip index". During a spin it decreases (symbols move down).
    /// Cell with virtual index i: y = -(i - Position) * cellSize (the top row is at y=0 when i = Position).
    /// Symbol shown by virtual index i: strip[Mod(i + Shift(i), length)]
    ///
    /// At stop time, only indices off screen (above the top) switch to a new shift. That puts the target
    /// stop a few symbols ahead without any visible jump ("splicing").
    /// </summary>
    public sealed class ReelMotion
    {
        private readonly int _length;
        private int _oldShift;
        private int _newShift;
        /// <summary>Indices at or below this use _newShift, above it _oldShift</summary>
        private long _switchIndex = long.MinValue;

        public ReelMotion(int stripLength, int initialStop)
        {
            if (stripLength <= 0) throw new ArgumentOutOfRangeException(nameof(stripLength));
            _length = stripLength;
            Position = initialStop;
        }

        public double Position { get; set; }

        public static int Mod(long a, int n)
        {
            var r = (int)(a % n);
            return r < 0 ? r + n : r;
        }

        public int StripIndexOf(long virtualIndex)
        {
            var shift = virtualIndex <= _switchIndex ? _newShift : _oldShift;
            return Mod(virtualIndex + shift, _length);
        }

        /// <summary>
        /// Decides the stop destination. From the current Position, place the target so that at least
        /// minTravel symbols are left to travel, and switch the shift so the target window shows the given stop.
        /// Returns the final Position (a whole number).
        /// </summary>
        public long PlanStop(int targetStop, int minTravel, int rows)
        {
            if (minTravel < 0) throw new ArgumentOutOfRangeException(nameof(minTravel));
            var top = (long)Math.Floor(Position);
            // Indices at or below top-1 are off screen right now (the top row is index top)
            var switchIndex = top - 1;
            // Every index in the target window (target .. target+rows-1) must be at or below switchIndex
            var target = switchIndex - (rows - 1) - minTravel;

            // Carry the current mapping forward before switching, so already-visible symbols never change
            _oldShift = CurrentShiftAbove(switchIndex);
            _newShift = Mod(targetStop - target, _length);
            _switchIndex = switchIndex;
            return target;
        }

        /// <summary>Called after the stop completes: fold the mapping into one shift</summary>
        public void Settle(long finalPosition)
        {
            Position = finalPosition;
            _oldShift = _newShift = StripIndexOf(finalPosition) - (int)Mod(finalPosition, _length);
            _switchIndex = long.MinValue;
        }

        private int CurrentShiftAbove(long switchIndex) => switchIndex + 1 <= _switchIndex ? _newShift : _oldShift;
    }
}
