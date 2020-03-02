using System;

namespace System.Diagnostics
{
    public class TaskActivityStatusReport
    {
        private readonly int _ActivityId;
        private string _ActivityDescription = "";
        private TaskActivityDurationSpecifier _ActivityDuration;
        private string _ActivityName = "";
        private decimal _RelativeProgress;

        internal TaskActivityStatusReport( int activityId, int processingLevel, string activityName, string activityDescription, TaskActivityDurationSpecifier activityDuration, DateTime? lastUpdated = null )
        {
            _ActivityId = activityId;
            _ActivityName = activityName;
            _ActivityDescription = activityDescription;
            _ActivityDuration = activityDuration;
            this.ProcessingLevel = processingLevel;

            this.LastUpdated = lastUpdated.GetValueOrDefault(DateTime.Now);
        }

        private int _ActivityCurrentProgressCount;
        private int _ActivityMaximumProgressCount;

        public int ProcessingLevel;

        public string ActivityDescription
        {
            get { return _ActivityDescription; }

            set { _ActivityDescription = value; }
        }

        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The number of steps completed in the current Activity.
        /// </summary>
        public int ActivityCurrentProgressCount
        {
            get { return _ActivityCurrentProgressCount; }

            set
            {
                _ActivityCurrentProgressCount = value;

                this.RecalculateRelativeProgress();
            }
        }

        private void RecalculateRelativeProgress()
        {
            if ( _ActivityMaximumProgressCount > 0 )
            {
                _RelativeProgress = (decimal)_ActivityCurrentProgressCount / (decimal)_ActivityMaximumProgressCount;
            }
            else
            {
                _RelativeProgress = 0;
            }
        }

        /// <summary>
        /// The number of steps required to complete the current Activity.
        /// </summary>
        public int ActivityMaximumProgressCount
        {
            get { return _ActivityMaximumProgressCount; }

            set
            {
                _ActivityMaximumProgressCount = value;

                this.RecalculateRelativeProgress();
            }
        }

        public TaskActivityDurationSpecifier ActivityDurationType
        {
            get { return _ActivityDuration; }

            set { _ActivityDuration = value; }
        }

        public int ActivityId
        {
            get { return _ActivityId; }
        }

        public string ActivityName
        {
            get { return _ActivityName; }

            set { _ActivityName = value; }
        }

        /// <summary>
        /// The percentage of steps completed in the current activity.
        /// </summary>
        public decimal RelativeProgress
        {
            get { return _RelativeProgress; }

            set
            {
                _RelativeProgress = value;

                // If we have a maximum progress count, recalculate the number of steps processed.
                if ( _ActivityMaximumProgressCount > 0 )
                {
                    _ActivityCurrentProgressCount = (int)Math.Round( _ActivityMaximumProgressCount * ( _RelativeProgress / 100 ) );
                }
            }
        }

    }
}