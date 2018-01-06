using System;

namespace OppaiSharp
{
    public class Accuracy
    {
        public int n300 = 0, n100 = 0, n50 = 0, nmisses = 0;

        public Accuracy() { }

        /**
        * @param n300 the number of 300s, if -1 it will be calculated
        *             from the object count in Accuracy#value(int).
        */
        public Accuracy(int n300, int n100, int n50, int nmisses)
        {
            this.n300 = n300;
            this.n100 = n100;
            this.n50 = n50;
            this.nmisses = nmisses;
        }

        /**
        * calls Accuracy(-1, n100, n50, nmisses) .
        * @see Koohii.Accuracy#Koohii.Accuracy(int, int, int, int)
        */
        public Accuracy(int n100, int n50, int nmisses) : this(-1, n100, n50, nmisses) { }

        /**
        * calls Accuracy(-1, n100, n50, 0) .
        * @see Koohii.Accuracy#Koohii.Accuracy(int, int, int, int)
        */
        public Accuracy(int n100, int n50) : this(-1, n100, n50, 0) { }

        /**
        * calls Accuracy(-1, n100, 0, 0) .
        * @see Koohii.Accuracy#Koohii.Accuracy(int, int, int, int)
        */
        public Accuracy(int n100) : this(-1, n100, 0, 0) { }

        /**
        * rounds to the closest amount of 300s, 100s, 50s for a given
        * accuracy percentage.
        * @param nobjects the total number of hits (n300 + n100 + n50 +
        *        nmisses)
        */
        public Accuracy(double acc_percent, int nobjects, int nmisses)
        {
            nmisses = Math.Min(nobjects, nmisses);
            int max300 = nobjects - nmisses;

            double maxacc =
                new Accuracy(max300, 0, 0, nmisses).value() * 100.0;

            acc_percent = Math.Max(0.0, Math.Min(maxacc, acc_percent));

            /* just some black magic maths from wolfram alpha */
            n100 = (int)
                Math.Round(
                    -3.0 *
                    ((acc_percent * 0.01 - 1.0) * nobjects + nmisses) *
                    0.5
                );

            if (n100 > max300)
            {
                /* acc lower than all 100s, use 50s */
                n100 = 0;

                n50 = (int)
                    Math.Round(
                        -6.0 *
                        ((acc_percent * 0.01 - 1.0) * nobjects +
                            nmisses) * 0.5
                    );

                n50 = Math.Min(max300, n50);
            }

            n300 = nobjects - n100 - n50 - nmisses;
        }

        /**
        * @param nobjects the total number of hits (n300 + n100 + n50 +
        *                 nmiss). if -1, n300 must have been set and
        *                 will be used to deduce this value.
        * @return the accuracy value (0.0-1.0)
        */
        public double value(int nobjects)
        {
            if (nobjects < 0 && n300 < 0)
            {
                throw new ArgumentException(
                    "either nobjects or n300 must be specified"
                );
            }

            int n300_ = n300 > 0 ? n300 :
                nobjects - n100 - n50 - nmisses;

            if (nobjects < 0)
            {
                nobjects = n300_ + n100 + n50 + nmisses;
            }

            double res = (n50 * 50.0 + n100 * 100.0 + n300_ * 300.0) /
                (nobjects * 300.0);

            return Math.Max(0, Math.Min(res, 1.0));
        }

        /**
        * calls value(-1) .
        * @see Accuracy#value(int)
        */
        public double value() => value(-1);
    }
}
