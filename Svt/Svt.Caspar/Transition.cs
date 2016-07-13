using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
	public enum TransitionType
	{
		CUT,
		MIX,
		PUSH,
		SLIDE,
		WIPE,
        SQUEEZE
	}

    public enum TransitionDirection
    {
        LEFT,
        RIGHT
    }

	public class Transition
	{
		public Transition()
		{
			type_ = TransitionType.CUT;
            direction_ = TransitionDirection.RIGHT;
			duration_ = 0;
            pause_ = 0;
            easing_ = Easing.None;
		}
		public Transition(TransitionType type, int duration, int pause, Easing easing)
		{
			type_ = type;
			duration_ = duration;
            pause_ = pause;
            easing_ = easing;
		}

        private TransitionDirection direction_;
        public TransitionDirection Direction
        {
            get { return direction_; }
            set { direction_ = value; }
        }

		private TransitionType type_;
		public TransitionType Type
		{
			get { return type_; }
			set { type_ = value; }
		}
		private int duration_;
		public int Duration
		{
			get { return duration_; }
			set { duration_ = value; }
		}

        private int pause_;
        public int Pause
        {
            get { return pause_; }
            set { pause_ = value; }
        }

        private Easing easing_;
        public Easing Easing
        {
            get { return easing_; }
            set { easing_ = value; }
        }

        public override string ToString()
		{
            if (type_ == TransitionType.CUT)
                return string.Empty;
            StringBuilder sb = new StringBuilder(type_.ToString())
                .AppendFormat(" {0} {1} {2} {3}", duration_, easing_.ToString().ToUpperInvariant(), direction_, pause_);
            return sb.ToString();
		}
	}
}
