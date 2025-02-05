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

        public float[] Cost { get; private set; }

        public float[] LowerBounds { get; private set; }
        public float[] UpperBounds { get; private set; }

        public List<ConstraintRow> Constraints { get; } = new();

        public List<ConstraintRow> ExactlyOneChoiceConstraints { get; } = new();

        public List<MaxJobsConstraintRow> MaxJobsConstraints { get; } = new();

        public void AddConstraint(float[] coeff, float lowerBound, float upperBound)
        {
            if (coeff.Length != VariableCount)
                throw new ArgumentException("Coefficient array size mismatch.");

            Constraints.Add(new ConstraintRow { Coeff = coeff, LowerBound = lowerBound, UpperBound = upperBound });
        }

        public void AddMaxJobsConstraint(float[] coeff, float lowerBound, float upperBound, int priorityIndex)
        {
            if (coeff.Length != VariableCount)
                throw new ArgumentException("Coefficient array size mismatch.");

            MaxJobsConstraints.Add(
                new MaxJobsConstraintRow { Coeff = coeff, LowerBound = lowerBound, UpperBound = upperBound, PriorityIndex = priorityIndex }
            );
        }

        public void AddExactlyOneChoiceConstraint(float[] coeff, float lowerBound, float upperBound)
        {
            if (coeff.Length != VariableCount)
                throw new ArgumentException("Coefficient array size mismatch.");

            ExactlyOneChoiceConstraints.Add(new ConstraintRow { Coeff = coeff, LowerBound = lowerBound, UpperBound = upperBound });
        }

        public class ConstraintRow
        {
            public float[] Coeff { get; init; } = null!;
            public float LowerBound { get; init; }
            public float UpperBound { get; init; }
        }

        public class MaxJobsConstraintRow : ConstraintRow
        {
            public int PriorityIndex { get; init; }
        }
    }
}
