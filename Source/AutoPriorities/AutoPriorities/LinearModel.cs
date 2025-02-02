using System;
using System.Collections.Generic;

namespace AutoPriorities
{
    public class LinearModel
    {
        public LinearModel(float[] cost, float[] lowerBounds, float[] upperBounds)
        {
            VariableCount = cost.Length;
            Cost = cost;
            LowerBounds = lowerBounds;
            UpperBounds = upperBounds;
        }

        public int VariableCount { get; }

        // Cost array: cost[i] is the coefficient for variable x_i.
        public float[] Cost { get; private set; }

        // Bounds for each variable: x_i in [LowerBounds[i], UpperBounds[i]].
        public float[] LowerBounds { get; private set; }
        public float[] UpperBounds { get; private set; }

        // Each constraint: sum_i( Coeff[i] * x_i ) in [LowerBound, UpperBound].
        public List<ConstraintRow> Constraints { get; } = new();

        /// <summary>
        ///     Adds a linear constraint of the form:
        ///     sum_i( Coeff[i] * x_i ) in [lowerBound, upperBound]
        ///     Coeff array must have length == VariableCount.
        /// </summary>
        public void AddConstraint(float[] coeff, float lowerBound, float upperBound)
        {
            if (coeff.Length != VariableCount)
                throw new ArgumentException("Coefficient array size mismatch.");

            Constraints.Add(new ConstraintRow { Coeff = coeff, LowerBound = lowerBound, UpperBound = upperBound });
        }

        /// <summary>
        ///     A single constraint's data.
        /// </summary>
        public class ConstraintRow
        {
            public float[] Coeff { get; init; } = null!;
            public float LowerBound { get; init; }
            public float UpperBound { get; init; }
        }
    }
}
