using System;

namespace MGUA.Services
{
    public static class FourierModelHelper
    {
        public static double GetLambdaParameter(int n, int m)
        {
            if(m > 0 && n > 0)
            {
                return 1d;
            }

            if (n == m && n == 0)
            {
                return 1d / 4d;
            }

            if (m == 0 && n > 0 || m > 0 && n == 0)
            {
                return 1d / 2d;
            }
            
            throw new ArgumentException("Invalid value for parameters");
        }
    }
}