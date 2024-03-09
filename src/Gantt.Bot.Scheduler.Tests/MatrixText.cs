using Gantt.Bot.Scheduler.Helpers;
using Gantt.Bot.Scheduler.Logger;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Random;

namespace Gantt.Bot.Scheduler.Tests;

public sealed class MatrixText
{
    readonly float[] _arrayA = new float[VectorLength];
    readonly float[] _arrayB = new float[VectorLength];
    readonly float[] _scalerArray = new float[VectorLength];
    readonly float[] _divisorArray = new float[VectorLength];

    private readonly Vector _vectorA = (Vector)Vector.Build.Dense(VectorLength, 0f);
    private readonly Vector _vectorB = (Vector)Vector.Build.Dense(VectorLength, 0f);
    private static readonly int VectorLength = 21;
    private readonly SparseMatrix _matrixA = new SparseMatrix(4, VectorLength);
    private readonly DenseMatrix _matrixB = new DenseMatrix(4, VectorLength);

    public MatrixText()
    {
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < VectorLength; j++)
            {
                _matrixA[i, j] = (i + 1) / 4f;
                _matrixB[i, j] = _arrayB[j];
            }
        }

        var random = new Random(96234);
        for (var i = 0; i < _arrayA.Length; i++)
        {
            _vectorA[i] = _arrayA[i] = 0.75f;
            _vectorB[i] = _arrayB[i] = i / 20f;
            _scalerArray[i] = 2;
            _divisorArray[i] = 4;
        }
    }

    [Test]
    public void SparseMatrixWeightedAverage()
    {
        var logger = StreamDebugWriter.Console;
        var scaler = _matrixB.Multiply(2);
        var testA = _matrixA.Add(scaler);
        var testD = testA.Divide(3);
        _matrixA.Dump(logger, true);
        logger.WriteLine("----------------");
        _matrixB.Dump(logger, true);
        logger.WriteLine("----------------");
        testD.Dump(logger, true);
    }

    [Test]
    public void SparseMatrixAverage()
    {
        var logger = StreamDebugWriter.Console;
        var testA = _matrixA.Add(_matrixB);
        var testD = testA.Divide(2);
        _matrixA.Dump(logger, true);
        logger.WriteLine("----------------");
        _matrixB.Dump(logger, true);
        logger.WriteLine("----------------");
        testD.Dump(logger, true);
    }

    [Test]
    public void SparseMatrixScaler()
    {
        var logger = StreamDebugWriter.Console;
        var testA = _matrixA.PointwiseMultiply(_matrixB);
        _matrixA.Dump(logger, true);
        logger.WriteLine("----------------");
        _matrixB.Dump(logger, true);
        logger.WriteLine("----------------");
        testA.Dump(logger, true);
    }

    [Test]
    public void MathNetVectorAverage()
    {
        var testA = _vectorA.Add(_vectorB);
        var testD = testA.Divide(2);

        for (var i = 0; i < _arrayA.Length; i++)
        {
            Console.WriteLine(
                $"{i:000}: ({_vectorA[i]:N3} + {_vectorB[i]:N3} = {testA[i]:N3}) / 2 = {testD[i]:N3} ");
        }
    }

    [Test]
    public void MathNetVectorScale()
    {
        var testA = _vectorA.PointwiseMultiply(_vectorB);
        var testD = testA.Divide(2);

        for (var i = 0; i < _arrayA.Length; i++)
        {
            Console.WriteLine(
                $"{i:000}: ({_vectorA[i]:N3} * {_vectorB[i]:N3} = {testA[i]:N3}) / 2 = {testD[i]:N3} ");
        }
    }
}