using System;

namespace OppaiSharp
{
    public struct Accuracy
    {
        public int Count300, Count100, Count50, CountMiss;

        /// <param name="count300">
        /// The number of 300s, if -1 it will be calculated from the object count in <see cref="Accuracy.Value(int)"/>
        /// </param>
        /// <param name="count100"></param>
        /// <param name="count50"></param>
        /// <param name="countMiss"></param>
        public Accuracy(int count300, int count100, int count50, int countMiss)
        {
            Count300 = count300;
            Count100 = count100;
            Count50 = count50;
            CountMiss = countMiss;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Calls <see cref="Accuracy(int,int,int,int)" /> with -1 300's
        /// </summary>
        public Accuracy(int count100, int count50, int countMiss) : this(-1, count100, count50, countMiss) { }

        /// <inheritdoc />
        /// <summary>
        /// Calls <see cref="Accuracy(int,int,int,int)" /> with -1 300's and 0 misses
        /// </summary>
        public Accuracy(int count100, int count50) : this(-1, count100, count50, 0) { }
        
        /// <inheritdoc />
        /// <summary>
        /// Calls <see cref="Accuracy(int,int,int,int)" /> with -1 300's, 0 50's and 0 misses
        /// </summary>
        public Accuracy(int count100) : this(-1, count100, 0, 0) { }

        /// <summary>
        /// Rounds to the closest amount of 300s, 100s, 50s for a given accuracy percentage
        /// </summary>
        /// <param name="accPercent"></param>
        /// <param name="countObjects">
        /// The total number of hits (<see cref="Count300"/> + <see cref="Count100"/> + <see cref="Count50"/> + 
        /// <see cref="CountMiss"/>)
        /// </param>
        /// <param name="countMiss"></param>
        public Accuracy(double accPercent, int countObjects, int countMiss)
        {
            Count50 = 0;
            CountMiss = Math.Min(countObjects, countMiss);
            int max300 = countObjects - countMiss;

            double maxAcc = new Accuracy(max300, 0, 0, countMiss).Value() * 100.0;

            accPercent = Math.Max(0.0, Math.Min(maxAcc, accPercent));

            //just some black magic maths from wolfram alpha
            Count100 = (int)Math.Round(-3.0 * ((accPercent * 0.01 - 1.0) * countObjects + countMiss) * 0.5);

            if (Count100 > max300) {
                //acc lower than all 100s, use 50s
                Count100 = 0;
                Count50 = (int)Math.Round(-6.0 * ((accPercent * 0.01 - 1.0) * countObjects + countMiss) * 0.5);
                Count50 = Math.Min(max300, Count50);
            }

            Count300 = countObjects - Count100 - Count50 - countMiss;
        }

        /// <param name="countObjects">
        /// The total number of hits (<see cref="Count300"/> + <see cref="Count100"/> + <see cref="Count50"/> + 
        /// <see cref="CountMiss"/>). If -1, Count300 must have been set and will be used to deduce this value
        /// </param>
        /// <returns>The accuracy value(0.0-1.0)</returns>
        public double Value(int countObjects = -1)
        {
            if (countObjects < 0 && Count300 < 0)
                throw new ArgumentException($"Either {nameof(countObjects)} or {nameof(Count300)} must be specified");

            int count300 = Math.Max(Count300 > 0 
                ? Count300 
                : countObjects - Count100 - Count50 - CountMiss, 0);

            if (countObjects < 0)
                countObjects = count300 + Count100 + Count50 + CountMiss;

            double res = (Count50 * 50.0 + Count100 * 100.0 + count300 * 300.0) / (countObjects * 300.0);

            return Math.Max(0, Math.Min(res, 1.0));
        }
    }
}
