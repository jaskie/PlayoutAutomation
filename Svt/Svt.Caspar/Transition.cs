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
		WIPE
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
		}
		public Transition(TransitionType type, int duration)
		{
			type_ = type;
			duration_ = duration;
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

		public override string ToString()
		{
            return Type.ToString() + " " + duration_.ToString() + " " + direction_.ToString();
		}
	}
}
