using System;

namespace MGUA.UtilModels
{
    public static class MatrixExtensions
    {
        public static Matrix InverseMatrix(this Matrix matrix)
        {
            if (matrix.Rows != matrix.Columns)
                return null;
            var determinant = matrix.CalculateDeterminant();
            if (Math.Abs(determinant) < 0.000001)
                return null;

            var result = new Matrix(matrix.Rows, matrix.Rows);

            matrix.ProcessCellsWithFunction((i, j) =>
            {
                result[i, j] = ((i + j + 2) % 2 == 1 ? -1 : 1) * matrix.CalculateMinor(i, j) / determinant;
            });

            result = result.CreateTransposeMatrix();
            return result;
        }

        private static double CalculateMinor(this Matrix matrix, int i, int j)
        {
            return matrix.CreateMatrixWithoutColumn(j).CreateMatrixWithoutRow(i).CalculateDeterminant();
        }

        public static double CalculateDeterminant(this Matrix matrix)
        {
            if (matrix.Columns == 2)
            {
                return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
            }

            double result = 0;
            for (var j = 0; j < matrix.Columns; j++)
            {
                result += ((j + 1) % 2 == 1 ? 1 : -1) * matrix[0, j] *
                     matrix.CreateMatrixWithoutColumn(j).
                     CreateMatrixWithoutRow(0).CalculateDeterminant();
            }

            matrix.Determinant = result;

            return result;
        }

        private static Matrix CreateMatrixWithoutRow(this Matrix matrix, int row)
        {
            if (row < 0 || row >= matrix.Rows)
            {
                throw new ArgumentException("invalid row index");
            }

            var result = new Matrix(matrix.Rows - 1, matrix.Columns);
            result.ProcessCellsWithFunction((i, j) => result[i, j] = i < row ? matrix[i, j] : matrix[i + 1, j]);
            return result;
        }

        private static Matrix CreateMatrixWithoutColumn(this Matrix matrix, int column)
        {
            if (column < 0 || column >= matrix.Columns)
            {
                throw new ArgumentException("invalid column index");
            }

            var result = new Matrix(matrix.Rows, matrix.Columns - 1);
            result.ProcessCellsWithFunction((i, j) => result[i, j] = j < column ? matrix[i, j] : matrix[i, j + 1]);
            return result;
        }

        public static Matrix CreateTransposeMatrix(this Matrix matrix)
        {
            var transposeMatrix = new Matrix(matrix.Columns, matrix.Rows);
            transposeMatrix.ProcessCellsWithFunction((i, j) => transposeMatrix[i, j] = matrix[j, i]);
            return transposeMatrix;
        }

        public static void ProcessCellsWithFunction(this Matrix matrix, Action<int, int> function)
        {
            for (var i = 0; i < matrix.Rows; i++)
            {
                for (var j = 0; j < matrix.Columns; j++)
                {
                    function(i, j);
                }
            }
        }

        public static void Print(this Matrix matrix)
        {
            Console.WriteLine();
            for (var i = 0; i < matrix.Rows; i++)
            {
                for (var j = 0; j < matrix.Columns; j++)
                {
                    Console.Write(matrix[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
