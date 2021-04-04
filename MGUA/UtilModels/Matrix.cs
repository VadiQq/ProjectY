namespace MGUA.UtilModels
{
    public class Matrix
    {
        public int Rows { get; }
        public int Columns { get; }
        public double Determinant { get; set; }
        public double[,] MatrixValues { get; }

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            MatrixValues = new double[rows, columns];
        }

        public Matrix(int rows, int columns, double[,] initialValues)
        {
            Rows = rows;
            Columns = columns;
            MatrixValues = initialValues;
        }

        #region override operators
        public double this[int x, int y]
        {
            get => MatrixValues[x, y];
            set => MatrixValues[x, y] = value;
        }

        public static Matrix operator *(Matrix leftMatrix, Matrix rightMatrix)
        {
            var resultMatrix = new Matrix(leftMatrix.Rows, rightMatrix.Columns);
            resultMatrix.ProcessCellsWithFunction((i, j) =>
            {
                for (var k = 0; k < leftMatrix.Columns; k++)
                {
                    resultMatrix[i, j] += leftMatrix[i, k] * rightMatrix[k, j];
                }
            });

            return resultMatrix;
        }

        public Matrix Copy()
        {
            var v = new double[Rows, Columns];
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    v[i, j] = MatrixValues[i, j];
                }
            }

            return new Matrix(Rows, Columns, v);
        }
        #endregion
    }
}
