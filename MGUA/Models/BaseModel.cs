using System;
using System.Collections.Generic;
using MGUA.UtilModels;

namespace MGUA.Models
{
    public abstract class BaseMGUAModel
    {
        public int ModelIndex { get; }
        public int RoundIndex { get; }
        public virtual BaseMGUAModel[] SubModels { get; }
        public int FirstVariableIndex { get; }
        public int SecondVariableIndex { get; }
        public double ExternalCriterion { get; protected set; }
        protected double RegularCriterion { get; set; }
        protected double UnbiasednessCriterion { get; set; }
        protected Matrix BModelValues;
        protected Matrix BTestModelValues;

        protected double[,] XModelValues;
        protected double[,] YModelValues;
        protected double[,] XTestModelValues;
        protected double[,] YTestModelValues;
        protected readonly Matrix XValues;
        protected readonly Matrix YValues;
        
        protected BaseMGUAModel()
        {
        }
        
        protected BaseMGUAModel(int modelIndex, int index1, int index2, Matrix xValues, Matrix yValues)
        {
            ModelIndex = modelIndex;
            FirstVariableIndex = index1;
            SecondVariableIndex = index2;
            XValues = xValues;
            YValues = yValues;
        }

        protected BaseMGUAModel(int modelIndex, int roundIndex, Matrix xValues, Matrix yValues)
        {
            ModelIndex = modelIndex;
            RoundIndex = roundIndex;
            XValues = xValues;
            YValues = yValues;
            SubModels = new WienerBinaryModel[2];
        }
    }
}