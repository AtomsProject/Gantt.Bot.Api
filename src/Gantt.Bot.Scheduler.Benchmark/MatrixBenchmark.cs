using BenchmarkDotNet.Attributes;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Gantt.Bot.Scheduler.Benchmark;

[MemoryDiagnoser(false)]
public class MatrixBenchmark
{
    readonly float[] _arrayA = new float[VectorLength];
    readonly float[] _arrayB = new float[VectorLength];
    readonly float[] _scalerArray = new float[VectorLength];
    readonly float[] _divisorArray = new float[VectorLength];

    private readonly Vector _vectorA = (Vector)Vector.Build.Dense(VectorLength, 2f);
    private readonly Vector _vectorB = (Vector)Vector.Build.Dense(VectorLength, 2f);
    private static readonly int VectorLength = 21;

    DenseMatrix matrixA = new DenseMatrix(4, VectorLength);
    DenseMatrix matrixB = new DenseMatrix(4, VectorLength);

    public MatrixBenchmark()
    {
        var random = new Random(96234);
        for (var i = 0; i < _arrayA.Length; i++)
        {
            _vectorA[i] = _arrayA[i] = 0.75f;
            _vectorB[i] = _arrayB[i] = i / 20f;
            _scalerArray[i] = 2;
            _divisorArray[i] = 4;
        }
        
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < VectorLength; j++)
            {
                matrixA[i, j] = (i + 1) / 4f;
                matrixB[i, j] = _arrayB[j];
            }
        }
    }
    [Benchmark]
    public void MatrixAverage()
    {
        var testA = matrixA.Add(matrixB);
        var testD = testA.Divide(3);
    }
}