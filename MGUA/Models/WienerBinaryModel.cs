using System;
using System.Collections.Generic;
using System.Linq;
using MGUA.UtilModels;

namespace MGUA.Models
{
    public class WienerBinaryModel : BaseMGUAModel
    {
        public WienerBinaryModel()
        {
            
        }
        
        public WienerBinaryModel(int modelIndex, int index1, int index2, Matrix xValues, Matrix yValues) : base(modelIndex, index1, index2, xValues, yValues)
        {
            SetModelCoefficients();
        }

        public WienerBinaryModel(int modelIndex, int roundIndex, Matrix xValues, Matrix yValues, WienerBinaryModel firstSubModel, WienerBinaryModel secondSubModel) : base(modelIndex, roundIndex, xValues, yValues)
        {
            SubModels[0] = firstSubModel;
            SubModels[1] = secondSubModel;
            SetModelCoefficients();
        }

        public override WienerBinaryModel[] SubModels { get; }

        private void SetModelCoefficients()
        {
            XModelValues = new double[XValues.Rows, 6];

            for (int i = 0; i < XValues.Rows; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    switch (j)
                    {
                        case 0:
                            {
                                XModelValues[i, j] = 1;
                                break;
                            }
                        case 1:
                            {
                                if (SubModels != null)
                                {
                                    XModelValues[i, j] = XValues[i, SubModels[0].ModelIndex];
                                }
                                else
                                {
                                    XModelValues[i, j] = XValues[i, FirstVariableIndex];
                                }
                                break;
                            }
                        case 2:
                            {
                                if (SubModels != null)
                                {
                                    XModelValues[i, j] = XValues[i, SubModels[1].ModelIndex];
                                }
                                else
                                {
                                    XModelValues[i, j] = XValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                        case 3:
                            {
                                if (SubModels != null)
                                {
                                    XModelValues[i, j] = XValues[i, SubModels[0].ModelIndex] * XValues[i, SubModels[1].ModelIndex];
                                }
                                else
                                {
                                    XModelValues[i, j] = XValues[i, FirstVariableIndex] * XValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                        case 4:
                            {
                                if (SubModels != null)
                                {
                                    XModelValues[i, j] = XValues[i, SubModels[0].ModelIndex] * XValues[i, SubModels[0].ModelIndex];
                                }
                                else
                                {
                                    XModelValues[i, j] = XValues[i, FirstVariableIndex] * XValues[i, FirstVariableIndex];
                                }
                                break;
                            }
                        case 5:
                            {
                                if (SubModels != null)
                                {
                                    XModelValues[i, j] = XValues[i, SubModels[1].ModelIndex] * XValues[i, SubModels[1].ModelIndex];

                                }
                                else
                                {
                                    XModelValues[i, j] = XValues[i, SecondVariableIndex] * XValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                    }
                }
            }

            YModelValues = new double[YValues.Rows, 1];
            for (int i = 0; i < YValues.Rows; i++)
            {
                YModelValues[i, 0] = YValues[i, 0];
            }

            SolveBModelValues();
        }

        private void SolveBModelValues()
        {
            var xMatrix = new Matrix(XValues.Rows, 6, XModelValues);
            var yMatrix = new Matrix(YValues.Rows, 1, YModelValues);
            var transposeMatrix = xMatrix.CreateTransposeMatrix();
            var xPowMatrix = transposeMatrix * xMatrix;
            var inverseMatrix = xPowMatrix.InverseMatrix();
            var res = inverseMatrix * transposeMatrix;
            BModelValues = res * yMatrix;
        }

        private void SolveTestBModelValues(Matrix xTestValues, Matrix yTestValues)
        {
            var xMatrix = new Matrix(xTestValues.Rows, 6, XTestModelValues);
            var yMatrix = new Matrix(yTestValues.Rows, 1, YTestModelValues);
            var transposeMatrix = xMatrix.CreateTransposeMatrix();
            var xPowMatrix = transposeMatrix * xMatrix;
            var inverseMatrix = xPowMatrix.InverseMatrix();
            var res = inverseMatrix * transposeMatrix;
            BTestModelValues = res * yMatrix;
        }

        public double GetExternalCriterion(Matrix xTestValues, Matrix yTestValues)
        {
            RegularCriterion = GetRegularCriterion(xTestValues, yTestValues);
            UnbiasednessCriterion = GetUnbiasednessCriterion(xTestValues, yTestValues);
            ExternalCriterion = RegularCriterion + UnbiasednessCriterion;
            return ExternalCriterion;
        }

        private double GetRegularCriterion(Matrix xTestValues, Matrix yTestValues)
        {
            double criterionSum = 0;
            for (int i = 0; i < xTestValues.Rows; i++)
            {
                if (SubModels != null)
                {
                    criterionSum += Math.Pow(yTestValues[i, 0] - CalculateModelValue(xTestValues[i, SubModels[0].ModelIndex], xTestValues[i, SubModels[1].ModelIndex]), 2);
                }
                else
                {
                    criterionSum += Math.Pow(yTestValues[i, 0] - CalculateModelValue(xTestValues[i, FirstVariableIndex], xTestValues[i, SecondVariableIndex]), 2);
                }
            }

            double ySum = 0;

            for (int i = 0; i < yTestValues.Rows; i++)
            {
                ySum += yTestValues[i, 0] * yTestValues[i, 0];
            }

            //Console.WriteLine("Regular criterion: " + criterionSum / (ySum == 0 ? 1 : ySum));
            return criterionSum / (ySum == 0 ? 1 : ySum);
        }

        private double GetUnbiasednessCriterion(Matrix xTestValues, Matrix yTestValues)
        {
            SetTestCoefficients(xTestValues, yTestValues);
            double criterionSum = 0;
            for (int i = 0; i < XValues.Rows; i++)
            {
                if (SubModels != null)
                {
                    criterionSum += Math.Pow(CalculateModelValue(XValues[i, SubModels[0].ModelIndex], XValues[i, SubModels[1].ModelIndex]) -
                                            CalculateTestModelValue(XValues[i, SubModels[0].ModelIndex], XValues[i, SubModels[1].ModelIndex]), 2);
                }
                else
                {
                    criterionSum += Math.Pow(CalculateModelValue(XValues[i, FirstVariableIndex], XValues[i, SecondVariableIndex]) -
                                             CalculateTestModelValue(XValues[i, FirstVariableIndex], XValues[i, SecondVariableIndex]), 2);
                }
            }

            for (int i = 0; i < xTestValues.Rows; i++)
            {
                if (SubModels != null)
                {
                    criterionSum += Math.Pow(CalculateModelValue(xTestValues[i, SubModels[0].ModelIndex], xTestValues[i, SubModels[1].ModelIndex]) -
                                            CalculateTestModelValue(xTestValues[i, SubModels[0].ModelIndex], xTestValues[i, SubModels[1].ModelIndex]), 2);
                }
                else
                {
                    criterionSum += Math.Pow(CalculateModelValue(xTestValues[i, FirstVariableIndex], xTestValues[i, SecondVariableIndex]) -
                        CalculateTestModelValue(xTestValues[i, FirstVariableIndex], xTestValues[i, SecondVariableIndex]), 2);
                }
            }

            double ySum = 0;
            for (int i = 0; i < YValues.Rows; i++)
            {
                ySum += YValues[i, 0] * YValues[i, 0];
            }

            for (int i = 0; i < yTestValues.Rows; i++)
            {
                ySum += yTestValues[i, 0] * yTestValues[i, 0];
            }

            return criterionSum / (ySum == 0 ? 1 : ySum);
        }

        public double CalculateModelValue(double x1, double x2)
        {
            return BModelValues[0, 0] +
              BModelValues[1, 0] * x1 +
              BModelValues[2, 0] * x2 +
              BModelValues[3, 0] * x1 * x2 +
              BModelValues[4, 0] * x1 * x1 +
              BModelValues[5, 0] * x2 * x2;

        }

        private double CalculateTestModelValue(double x1, double x2)
        {
            return BTestModelValues[0, 0] +
              BTestModelValues[1, 0] * x1 +
              BTestModelValues[2, 0] * x2 +
              BTestModelValues[3, 0] * x1 * x2 +
              BTestModelValues[4, 0] * x1 * x1 +
              BTestModelValues[5, 0] * x2 * x2;

        }

        private void SetTestCoefficients(Matrix xTestValues, Matrix yTestValues)
        {
            XTestModelValues = new double[xTestValues.Rows, 6];

            for (int i = 0; i < xTestValues.Rows; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    switch (j)
                    {
                        case 0:
                            {
                                XTestModelValues[i, j] = 1;
                                break;
                            }
                        case 1:
                            {
                                if (SubModels != null)
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SubModels[0].ModelIndex];
                                }
                                else
                                {
                                    XTestModelValues[i, j] = xTestValues[i, FirstVariableIndex];
                                }
                                break;
                            }
                        case 2:
                            {
                                if (SubModels != null)
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SubModels[1].ModelIndex];
                                }
                                else
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                        case 3:
                            {
                                if (SubModels != null)
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SubModels[0].ModelIndex] * xTestValues[i, SubModels[1].ModelIndex];
                                }
                                else
                                {
                                    XTestModelValues[i, j] = xTestValues[i, FirstVariableIndex] * xTestValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                        case 4:
                            {
                                if (SubModels != null)
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SubModels[0].ModelIndex] * xTestValues[i, SubModels[0].ModelIndex];
                                }
                                else
                                {
                                    XTestModelValues[i, j] = xTestValues[i, FirstVariableIndex] * xTestValues[i, FirstVariableIndex];
                                }
                                break;
                            }
                        case 5:
                            {
                                if (SubModels != null)
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SubModels[1].ModelIndex] * xTestValues[i, SubModels[1].ModelIndex];

                                }
                                else
                                {
                                    XTestModelValues[i, j] = xTestValues[i, SecondVariableIndex] * xTestValues[i, SecondVariableIndex];
                                }
                                break;
                            }
                    }
                }
            }

            YTestModelValues = new double[yTestValues.Rows, 1];
            for (int i = 0; i < yTestValues.Rows; i++)
            {
                YTestModelValues[i, 0] = yTestValues[i, 0];
            }

            SolveTestBModelValues(xTestValues, yTestValues);
        }

        public int[] GetRootVariables()
        {
            var indexes = new List<int>();
            if (SubModels != null)
            {
                foreach (var model in SubModels)
                {
                    indexes.AddRange(model.GetRootVariables());
                }
            }
            else
            {
                indexes.AddRange(new[] { FirstVariableIndex, SecondVariableIndex });
            }

            return indexes.Distinct().ToArray();
        }

        public double GetModelPrediction(Matrix xValues, int index)
        {
            if (SubModels != null)
            {
                return BModelValues[0, 0] +
             BModelValues[1, 0] * SubModels[0].GetModelPrediction(xValues, index) +
             BModelValues[2, 0] * SubModels[1].GetModelPrediction(xValues, index) +
             BModelValues[3, 0] * SubModels[0].GetModelPrediction(xValues, index) * SubModels[1].GetModelPrediction(xValues, index) +
             BModelValues[4, 0] * SubModels[0].GetModelPrediction(xValues, index) * SubModels[0].GetModelPrediction(xValues, index) +
             BModelValues[5, 0] * SubModels[1].GetModelPrediction(xValues, index) * SubModels[1].GetModelPrediction(xValues, index);
            }
            else
            {
                return BModelValues[0, 0] +
                BModelValues[1, 0] * xValues[index, FirstVariableIndex] +
                BModelValues[2, 0] * xValues[index, SecondVariableIndex] +
                BModelValues[3, 0] * xValues[index, FirstVariableIndex] * xValues[index, SecondVariableIndex] +
                BModelValues[4, 0] * xValues[index, FirstVariableIndex] * xValues[index, FirstVariableIndex] +
                BModelValues[5, 0] * xValues[index, SecondVariableIndex] * xValues[index, SecondVariableIndex];
            }

        }

        public void GetModelView(SortedDictionary<int, Dictionary<string, WienerBinaryModel>> dictionary)
        {
            if (SubModels != null)
            {
                var modelView = $"{Math.Round(BModelValues[0, 0], 5)} + " +
                              $"{Math.Round(BModelValues[1, 0], 5)}m{SubModels[0].RoundIndex}_{SubModels[0].ModelIndex + 1} + " +
                              $"{Math.Round(BModelValues[2, 0], 5)}m{SubModels[1].RoundIndex}_{SubModels[1].ModelIndex + 1} + " +
                              $"{Math.Round(BModelValues[3, 0], 5)}m{SubModels[0].RoundIndex}_{SubModels[0].ModelIndex + 1}•m{SubModels[1].RoundIndex}_{SubModels[1].ModelIndex + 1} + " +
                              $"{Math.Round(BModelValues[4, 0], 5)}m{SubModels[0].RoundIndex}_{SubModels[0].ModelIndex + 1}•m{SubModels[0].RoundIndex}_{SubModels[0].ModelIndex + 1} + " +
                              $"{Math.Round(BModelValues[5, 0], 5)}m{SubModels[1].RoundIndex}_{SubModels[1].ModelIndex + 1}•m{SubModels[1].RoundIndex}_{SubModels[1].ModelIndex + 1}";
                if (dictionary.ContainsKey(RoundIndex))
                {
                    var d = dictionary[RoundIndex];
                    if (!d.ContainsKey(modelView))
                    {
                        d.Add(modelView, this);
                    }

                }
                else
                {
                    dictionary.Add(RoundIndex, new Dictionary<string, WienerBinaryModel>
                    {
                        { modelView, this }
                    });
                }

                SubModels[0].GetModelView(dictionary);
                SubModels[1].GetModelView(dictionary);
            }
            else
            {
                var modelView = $"{Math.Round(BModelValues[0, 0], 5)} + " +
                              $"{Math.Round(BModelValues[1, 0], 5)}x{FirstVariableIndex + 1} + " +
                              $"{Math.Round(BModelValues[2, 0], 5)}x{SecondVariableIndex + 1} + " +
                              $"{Math.Round(BModelValues[3, 0], 5)}x{FirstVariableIndex + 1}•x{SecondVariableIndex + 1} + " +
                              $"{Math.Round(BModelValues[4, 0], 5)}x{FirstVariableIndex + 1}•x{FirstVariableIndex + 1} + " +
                              $"{Math.Round(BModelValues[5, 0], 5)}x{SecondVariableIndex + 1}•x{SecondVariableIndex + 1}";
                if (dictionary.ContainsKey(RoundIndex))
                {
                    var d = dictionary[RoundIndex];
                    if (!d.ContainsKey(modelView))
                    {
                        d.Add(modelView, this);
                    }
                }
                else
                {
                    dictionary.Add(RoundIndex, new Dictionary<string, WienerBinaryModel>
                    {
                        { modelView, this }
                    });
                }
            }
        }
    }
}
