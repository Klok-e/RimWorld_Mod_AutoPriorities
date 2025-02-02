using System;
using System.Collections.Generic;
using System.Linq;
using AutoPriorities.Extensions;

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

        public LinearModelSparse CreateLinearModelSparse()
        {
            return new LinearModelSparse(this);
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

    public class LinearModelSparse
    {
        public LinearModelSparse(LinearModel model)
        {
            VariableCount = model.VariableCount;
            Cost = model.Cost.ToDouble();
            LowerBounds = model.LowerBounds.ToDouble();
            UpperBounds = model.UpperBounds.ToDouble();

            alglib.sparsecreate(model.Constraints.Count, VariableCount, out constraintCoeff);
            for (var index = 0; index < model.Constraints.Count; index++)
            {
                var modelConstraint = model.Constraints[index];
                for (int i = 0; i < modelConstraint.Coeff.Length; i++) alglib.sparseset(constraintCoeff, index, i, modelConstraint.Coeff[i]);
            }

            alglib.sparseconverttocrs(constraintCoeff);

            constraintLowerBound = model.Constraints.Select(x => (double)x.LowerBound).ToArray();
            constraintUpperBound = model.Constraints.Select(x => (double)x.UpperBound).ToArray();
        }

        public int VariableCount { get; }

        public double[] Cost { get; private set; }

        public double[] LowerBounds { get; private set; }
        public double[] UpperBounds { get; private set; }

        public alglib.sparsematrix constraintCoeff;
        public double[] constraintLowerBound;
        public double[] constraintUpperBound;
    }
}
