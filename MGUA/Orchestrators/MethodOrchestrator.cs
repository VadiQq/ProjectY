using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MGUA.Models;
using MGUA.Services;
using MGUA.UtilModels;

namespace MGUA.Orchestrators
{
    public class MethodOrchestrator
    {
        public void RunMGUAMethod(string trainingDataPath, string testingDataPath, int processesNumber)
        {
            var taskModels = new List<FourierBinaryModel>[processesNumber];
            Matrix previousXValues = null;
            Matrix previousXTestValues = null;

            //reading data for analysis
            var contents = DataManager.ReadFromFile(trainingDataPath);
            var xMatrix = DataManager.GetXValues(contents, "\t");
            var yMatrix = DataManager.GetYValues(contents, "\t");
            var length = DataManager.GetDataLength(contents, "\t");

            double minExternalCriterion = 1;

            //creating zero round models
            var models = new List<FourierBinaryModel>();
            int modelIndex = 0;
            for (int i = 0; i < length; i++)
            {
                for (int j = i + 1; j < length; j++)
                {
                    models.Add(new FourierBinaryModel(modelIndex++, i, j, xMatrix, yMatrix));
                }
            }

            //calculating zero round models' external criterion
            var testSet = DataManager.ReadFromFile(testingDataPath);
            var testXMatrix = DataManager.GetXValues(testSet, "\t");
            var testYMatrix = DataManager.GetYValues(testSet, "\t");
            foreach (var model in models)
            {
                model.GetExternalCriterion(testXMatrix, testYMatrix);
            }

            models = models.OrderBy(m => m.ExternalCriterion).ToList();
            var minModelsCriterion = models[0].ExternalCriterion;
            minExternalCriterion = minModelsCriterion < minExternalCriterion ? minModelsCriterion : minExternalCriterion;

            var finalModel = new FourierBinaryModel();
            var previousRoundModels = new List<FourierBinaryModel>();
            
            int round = 0;

            //calculating new data set for next round models
            var newXValues = new double[xMatrix.Rows, models.Count];
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                for (int j = 0; j < xMatrix.Rows; j++)
                {
                    newXValues[j, i] = model.CalculateModelValue(xMatrix[j, model.FirstVariableIndex], xMatrix[j, model.SecondVariableIndex]);
                }
            }

            var newXTestValues = new double[testXMatrix.Rows, models.Count];
            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                for (int j = 0; j < testXMatrix.Rows; j++)
                {
                    newXTestValues[j, i] = model.CalculateModelValue(testXMatrix[j, model.FirstVariableIndex], testXMatrix[j, model.SecondVariableIndex]);
                }
            }

            Console.WriteLine($"Round {round} - calculating");
            Console.WriteLine($"Round minimum external criterion - {minExternalCriterion}");
            Stopwatch a = new Stopwatch();
            a.Start();
            var newXMatrix = new Matrix(xMatrix.Rows, models.Count, newXValues);
            var newXTestMatrix = new Matrix(testXMatrix.Rows, models.Count, newXTestValues);
            modelIndex = 0;

            while (true)
            {
                Console.WriteLine($"Round {round+1} - calculating");
                var roundModels = new List<FourierBinaryModel>();
                var roundsyncModels = new List<FourierBinaryModel>();
                if (round == 0)
                {
                    previousXValues = newXMatrix;
                    previousXTestValues = newXTestMatrix;
                    previousRoundModels = models;
                }

                if (round != 0)
                {
                    var amountToTake = previousRoundModels.Count * 2 / 3 > 100 ? 100 : previousRoundModels.Count * 2 / 3;
                    if (amountToTake < 3)
                    {
                        finalModel = previousRoundModels[0];
                        break;
                    }

                    previousRoundModels = previousRoundModels.Take(amountToTake).ToList();
                }
                //synchronous algorithm
                //roundModels = ProcessModelGeneration(modelIndex, round, previousRoundModels, previousXTestValues, testYMatrix, previousXValues, yMatrix);

                //parallelization
                var processWorkAmount = previousRoundModels.Count / processesNumber;
                var offset = previousRoundModels.Count % processesNumber;
                var tasks = new ConcurrentBag<Task>();
                for (int i = 0; i < processesNumber; i++)
                {
                    var index = i;
                    tasks.Add(new Task(() => ProcessModelGenerationAsync(index, processWorkAmount, offset, 
                        processesNumber, round, taskModels, previousRoundModels, 
                        previousXTestValues, testYMatrix, previousXValues, yMatrix)));
                }

                foreach (var task in tasks)
                {
                    task.Start();
                }

                Task.WaitAll(tasks.ToArray());


                foreach (var modelsList in taskModels)
                {
                    roundModels.AddRange(modelsList);
                }

                roundModels = roundModels.OrderBy(m => m.ExternalCriterion).ToList();
                Console.WriteLine($"Round minimum external criterion - {roundModels[0].ExternalCriterion}");
                var tt = roundModels[0].ExternalCriterion - minModelsCriterion;
                if (roundModels[0].ExternalCriterion > minModelsCriterion)
                {
                    Console.WriteLine($"Models are degrade - abort");
                    finalModel = previousRoundModels[0];
                    break;
                }

                
                minModelsCriterion = roundModels[0].ExternalCriterion;
                previousRoundModels = roundModels.ToList();
                
                var modelXValues = new double[previousXValues.Rows, roundModels.Count];
                for (int i = 0; i < roundModels.Count; i++)
                {
                    var model = roundModels[i];
                    for (int j = 0; j < previousXValues.Rows; j++)
                    {
                        modelXValues[j, i] = model.CalculateModelValue(previousXValues[j, model.SubModels[0].ModelIndex], previousXValues[j, model.SubModels[1].ModelIndex]);
                    }
                }

                var modelXTestValues = new double[previousXTestValues.Rows, roundModels.Count];
                for (int i = 0; i < roundModels.Count; i++)
                {
                    var model = roundModels[i];
                    for (int j = 0; j < previousXTestValues.Rows; j++)
                    {
                        modelXTestValues[j, i] = model.CalculateModelValue(previousXTestValues[j, model.SubModels[0].ModelIndex], previousXTestValues[j, model.SubModels[1].ModelIndex]);
                    }
                }

                previousXValues = new Matrix(previousXValues.Rows, roundModels.Count, modelXValues);
                previousXTestValues = new Matrix(previousXTestValues.Rows, roundModels.Count, modelXTestValues);
                modelIndex = 0;
                round++;
            }

            //printing result to console
            var bestModel = finalModel;
            Console.WriteLine($"Time elapsed: " + (float)a.ElapsedMilliseconds / 1000);
            Console.Write($"Best model: m{bestModel.RoundIndex}{bestModel.ModelIndex}; External criterion: {bestModel.ExternalCriterion}; ");
            if (bestModel.ExternalCriterion < 0.05)
            {
                Console.WriteLine($"Model is valuable; ");
            }
            else
            {
                Console.WriteLine($"Model external criterion is larger then expected. Model might be not accurate;");
            }

            //valuable variables for final model
            var ind = bestModel.GetRootVariables();
            ind = ind.OrderBy(x => x).ToArray();
            Console.Write($"Root variables: ");
            foreach(int i in ind)
            {
                Console.Write($"x{i + 1} ");
            }
            Console.WriteLine();

            List<string> modelPredictions = new List<string>();
            for(int i=0; i < xMatrix.Rows; i++)
            {
                modelPredictions.Add(finalModel.GetModelPrediction(xMatrix, i).ToString());
            }

            for (int i = 0; i < testXMatrix.Rows; i++)
            {
                modelPredictions.Add(finalModel.GetModelPrediction(testXMatrix, i).ToString());
            }

            File.WriteAllLines("result.txt", modelPredictions.ToArray());

            //printing final model
            SortedDictionary<int, Dictionary<string, FourierBinaryModel>> modelView = new SortedDictionary<int, Dictionary<string, FourierBinaryModel>>();
            finalModel.GetModelView(modelView);

            var keys = modelView.Keys.ToArray();

            for(int i = keys.Length - 1;i >= 0; i--)
            {
                var index = 1;
                var key = keys[i];
                Console.WriteLine($"Round - {key}");


                var modelViews = modelView[key];
                var views = modelViews.Keys.ToArray();
                foreach (var view in views)
                {
                    var model = modelViews[view];
                    Console.Write($"Model #{model.RoundIndex}_{model.ModelIndex +1}: ");
                    Console.WriteLine(view);
                    index++;
                }
            }

            Console.ReadLine();
        }

        private void ProcessModelGenerationAsync(int index, int processWorkAmount, int offset, int processesNumber, int round,
            List<FourierBinaryModel>[] taskModels,  
            List<FourierBinaryModel> previousRoundModels, 
            Matrix previousXTestValues,
            Matrix testYMatrix,
            Matrix previousXValues, 
            Matrix yMatrix)
        {
            var modelIndex = 0;
            var startPosition = index * processWorkAmount;
            var amount = index == processesNumber - 1 ? index * processWorkAmount + processWorkAmount + offset : index * processWorkAmount + processWorkAmount;
            for (int i = 0; i < startPosition; i++)
            {
                modelIndex += previousRoundModels.Count - i - 1;
            }
            var currentTaskModels = new List<FourierBinaryModel>();

            for (int k = startPosition; k < amount; k++)
            {
                
                for (int j = k + 1; j < previousRoundModels.Count; j++)
                {
                    var ind = modelIndex++;
                    currentTaskModels.Add(new FourierBinaryModel(ind, round + 1, previousXValues, yMatrix, previousRoundModels[k], 
                        previousRoundModels[j]));
                }
            }

            foreach (var model in currentTaskModels)
            {
                model.GetExternalCriterion(previousXTestValues, testYMatrix);
            }

            taskModels[index] = currentTaskModels;
        }

        private List<FourierBinaryModel> ProcessModelGeneration(int modelIndex, int round,
            List<FourierBinaryModel> previousRoundModels,
            Matrix previousXTestValues,
            Matrix testYMatrix,
            Matrix previousXValues,
            Matrix yMatrix)
        {
            var roundModels = new List<FourierBinaryModel>();
            for (int i = 0; i < previousRoundModels.Count; i++)
            {
                for (int j = i + 1; j < previousRoundModels.Count; j++)
                {
                    roundModels.Add(new FourierBinaryModel(modelIndex++, round + 1, previousXValues, yMatrix, previousRoundModels[i], previousRoundModels[j]));
                }
            }

            foreach (var model in roundModels)
            {
                model.GetExternalCriterion(previousXTestValues, testYMatrix);
            }
            return roundModels.OrderBy(m => m.ExternalCriterion).ToList();
        }
    }
}
