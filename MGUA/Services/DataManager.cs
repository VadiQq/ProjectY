using System.IO;
using System.Linq;
using MGUA.UtilModels;

namespace MGUA.Services
{
    public static class DataManager
    {
        public static string[] ReadFromFile(string path)
        {
            var content = File.ReadLines(path).ToArray();
            return content;
        }

        public static Matrix GetXValues(string[] readData, string dataSeparator)
        {
            var length = readData[0].Split(dataSeparator).Length - 1;
            double[,] valuesX = new double[readData.Length, length];

            for (int i = 0; i < readData.Length; i++)
            {
                string valueString = readData[i];
                var values = valueString.Split(dataSeparator).Where(s => s != "").ToArray();
                for (int j = 0; j < values.Length; j++)
                {
                    if (j == values.Length - 1)
                    {
                        break;
                    }
                    else
                    {
                        if (double.TryParse(values[j], out double x))
                        {
                            valuesX[i, j] = x;
                        }
                    }
                }
            }
 
            var xMatrix = new Matrix(readData.Length, length, valuesX);
            return xMatrix;
        }

        public static Matrix GetYValues(string[] readData, string dataSeparator)
        {
            var length = readData[0].Split(dataSeparator).Length - 1;
            double[,] valuesY = new double[readData.Length, 1];

            for (int i = 0; i < readData.Length; i++)
            {
                string valueString = readData[i];
                var values = valueString.Split(dataSeparator).Where(s => s != "").ToArray();
                for (int j = 0; j < values.Length; j++)
                {
                    if (j == values.Length - 1)
                    {
                        if (double.TryParse(values[j], out double y))
                        {
                            valuesY[i, 0] = y;
                        }
                    }
                }
            }

            var yMatrix = new Matrix(readData.Length, 1, valuesY);
            return yMatrix;
        }

        public static int GetDataLength(string[] readData, string dataSeparator)
        {
            return readData[0].Split(dataSeparator).Length - 1;
        }
    }
}
