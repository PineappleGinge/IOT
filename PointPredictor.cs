using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TigerBotV2
{
    public class PointPredictor
    {
        public static int[] PredictPoint(int[] p1, int[] p2, int[] p3, int predictionTimeInSeconds)
        {
            int t1 = 0;
            int t2 = p2[2];
            int t3 = p3[2];

            double[] xCoefficients = SolveQuadraticSystem(t1, t2, t3, p1[0], p2[0], p3[0]);
            double[] yCoefficients = SolveQuadraticSystem(t1, t2, t3, p1[1], p2[1], p3[1]);

            double xPredict = xCoefficients[0] + xCoefficients[1] * predictionTimeInSeconds + xCoefficients[2] * Math.Pow(predictionTimeInSeconds, 2);
            double yPredict = yCoefficients[0] + yCoefficients[1] * predictionTimeInSeconds + yCoefficients[2] * Math.Pow(predictionTimeInSeconds, 2);

            int distance = (int)Math.Round(Math.Sqrt(Math.Pow(xPredict, 2) + Math.Pow(yPredict, 2)));

            return new int[] { (int)Math.Round(xPredict), (int)Math.Round(yPredict), distance };
        }

        private static double[] SolveQuadraticSystem(double t1, double t2, double t3, double y1, double y2, double y3)
        {
            double[,] matrix = {
            {1, t1, Math.Pow(t1, 2)},
            {1, t2, Math.Pow(t2, 2)},
            {1, t3, Math.Pow(t3, 2)}
        };

            double[] constants = { y1, y2, y3 };
            double[] result = GaussianElimination(matrix, constants);
            return result;
        }

        private static double[] GaussianElimination(double[,] matrix, double[] constants)
        {
            int n = constants.Length;
            for (int i = 0; i < n; i++)
            {
                // Search for maximum in this column
                double maxEl = Math.Abs(matrix[i, i]);
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(matrix[k, i]) > maxEl)
                    {
                        maxEl = Math.Abs(matrix[k, i]);
                        maxRow = k;
                    }
                }

                // Swap maximum row with current row (column by column)
                for (int k = i; k < n; k++)
                {
                    double tmp = matrix[maxRow, k];
                    matrix[maxRow, k] = matrix[i, k];
                    matrix[i, k] = tmp;
                }

                // Swap constant vector
                double tmpConst = constants[maxRow];
                constants[maxRow] = constants[i];
                constants[i] = tmpConst;

                // Make all rows below this one 0 in current column
                for (int k = i + 1; k < n; k++)
                {
                    double c = -matrix[k, i] / matrix[i, i];
                    for (int j = i; j < n; j++)
                    {
                        if (i == j)
                        {
                            matrix[k, j] = 0;
                        }
                        else
                        {
                            matrix[k, j] += c * matrix[i, j];
                        }
                    }
                    constants[k] += c * constants[i];
                }
            }

            // Solve equation Ax=b for an upper triangular matrix A
            double[] solution = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                solution[i] = constants[i] / matrix[i, i];
                for (int k = i - 1; k >= 0; k--)
                {
                    constants[k] -= matrix[k, i] * solution[i];
                }
            }

            return solution;
        }
    }
}
