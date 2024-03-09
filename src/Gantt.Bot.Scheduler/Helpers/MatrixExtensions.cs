using System.Collections.Immutable;
using Gantt.Bot.Scheduler.Logger;
using MathNet.Numerics.LinearAlgebra;
using Dbl = MathNet.Numerics.LinearAlgebra.Double;

namespace Gantt.Bot.Scheduler.Helpers;

public static class MatrixExtensions
{
    public static readonly string Hr = new('-', 80);

    public static void DumpList<T>(this IDebugWriter sb, string title, IReadOnlyList<T> matrix)
    {
        sb.Write("List: ");
        sb.WriteLine(title);
        var i = 0;
        foreach (var item in matrix)
        {
            sb.Write(i++.ToString("N0"));
            sb.WriteLine(':');
            sb.WriteLine(item);
        }
        sb.WriteLine();
        sb.WriteLine(Hr);
    }
    
    public static void Dump(this IDebugWriter sb, string title, IReadOnlyList<Vector<double>> matrix, bool sparse = false)
    {
        sb.Write("Vectors: ");
        sb.Write(title);
        sb.WriteLine();
        Dump(sb, sparse, matrix);
        sb.WriteLine();
        sb.WriteLine(Hr);
    }
    
    public static void DumpTranspose(this IDebugWriter sb, string title, IReadOnlyList<Vector<double>> matrix, bool sparse = false)
    {
        sb.Write("Vectors: ");
        sb.Write(title);
        sb.WriteLine();
        DumpTranspose(sb, sparse, matrix);
        sb.WriteLine();
        sb.WriteLine(Hr);
    }
    
    public static void Dump(this IDebugWriter sb, string title, IReadOnlyDictionary<string, Dbl.Vector> matrix, bool sparse = false)
    {
        sb.Write("Vectors: ");
        sb.Write(title);
        sb.WriteLine();
        Dump(sb, sparse, matrix);
        sb.WriteLine();
        sb.WriteLine(Hr);
    }

    public static void Dump(this IDebugWriter sb, string title, Matrix<float> matrix, bool sparse = false)
    {
        sb.Write("Matrix: ");
        sb.Write(title);
        sb.WriteLine();
        Dump(matrix, sb, sparse);
        sb.WriteLine();
        sb.WriteLine(Hr);
    }

    public static void Dump(this IDebugWriter sb, string title, Matrix<double> matrix, bool sparse = false)
    {
        sb.Write("Matrix: ");
        sb.Write(title);
        sb.WriteLine();
        Dump(matrix, sb, sparse);
        sb.WriteLine();
        sb.WriteLine(Hr);
    }

    public static void Dump(this IDebugWriter sb, IReadOnlyList<Vector<double>> matrix)
    {
        Dump(sb, false, matrix);
    }

    public static void Dump(this IDebugWriter sb, bool sparse, IReadOnlyList<Vector<double>> matrix)
    {
        // Set to the precision of the number, so an "N5" format will have 5 digits and '0.' to we need 7 characters
        var blank = new string(' ', 7);
        
        var colCount = matrix.Count;
        if (colCount == 0)
        {
            sb.WriteLine("No data");
            return;
        }
        
        var rowCount = matrix.Max(v => v.Count);
        if (rowCount == 0)
        {
            sb.WriteLine("No data");
            return;
        }

        
        // Print the matrix with column and row headers, tab delimited
        // We will use N4 format for the numbers
        // var sb = new StringBuilder();
        // Print Header of X values
        // so we should have around 6 characters for each number
        sb.Write("_".PadRight(7));
        for (var i = 0; i < colCount; i++)
        {
            sb.Write('\t');
            sb.Write(i.ToString("N0").PadRight(7));
        }
        
        for (var row = 0; row < rowCount; row++)
        {
            sb.WriteLine();
            // print row header
            sb.Write(row.ToString("N0").PadLeft(7));
            for (var col = 0; col < colCount; col++)
            {
                sb.Write('\t');
                var m = matrix[col];
                if (m.Count < row)
                {
                    // This vector is shorter than the others
                    // so just leave a blank space
                    sb.Write(blank);
                    continue;
                }

                var v = m[row];
                if (sparse && Math.Abs(v) < double.Epsilon)
                {
                    // If the value is less then the precision we are show, just show a blank space
                    // Consider: if the value is not zero, but less than the precision, is there value in showing it?
                    sb.Write(blank);
                }
                else
                {
                    sb.Write(v >= 10 ? v.ToString("N4") : v.ToString("N5"));
                }
            }
        }
    }
    
    public static void DumpTranspose(this IDebugWriter sb, bool sparse, IReadOnlyList<Vector<double>> matrix)
    {
        // Set to the precision of the number, so an "N5" format will have 5 digits and '0.' to we need 7 characters
        var blank = new string(' ', 7);

        var rowCount = matrix.Count;
        if(rowCount == 0)
            return;
        
        var colCount = matrix.Max(v => v.Count);

        // Print the matrix with column and row headers, tab delimited
        // We will use N4 format for the numbers
        // var sb = new StringBuilder();
        // Print Header of X values
        // so we should have around 6 characters for each number
        sb.Write("_".PadRight(7));
        for (var i = 0; i < colCount; i++)
        {
            sb.Write('\t');
            sb.Write(i.ToString("N0").PadRight(7));
        }
        
        for (var row = 0; row < rowCount; row++)
        {
            sb.WriteLine();
            // print row header
            sb.Write(row.ToString("N0").PadLeft(7));
            var m = matrix[row];
            for (var col = 0; col < colCount; col++)
            {
                sb.Write('\t');
                if (m.Count < col)
                {
                    // This vector is shorter than the others
                    // so just leave a blank space
                    sb.Write(blank);
                    continue;
                }

                var v = m[col];
                if (sparse && Math.Abs(v) < double.Epsilon)
                {
                    // If the value is less then the precision we are show, just show a blank space
                    // Consider: if the value is not zero, but less than the precision, is there value in showing it?
                    sb.Write(blank);
                }
                else
                {
                    sb.Write(v >= 10 ? v.ToString("N4") : v.ToString("N5"));
                }
            }
        }
    }
    
    public static void Dump(this IDebugWriter sb, bool sparse, IReadOnlyDictionary<string, Dbl.Vector> matrix)
    {
        // Set to the precision of the number, so an "N5" format will have 5 digits and '0.' to we need 7 characters
        var blank = new string(' ', 7);

        var rowCount = matrix.Values.Max(v => v.Count);

        // Print the matrix with column and row headers, tab delimited
        // We will use N4 format for the numbers
        // var sb = new StringBuilder();
        sb.Write("_".PadRight(7));

        foreach (var key in matrix.Keys)
        {
            // Print Header of X values
            // so we should have around 6 characters for each number
                sb.Write('\t');
                sb.Write(key.Length > 7 ? key[..7] : key.PadRight(7));
        }

        var cols = matrix.Values.ToImmutableArray();
        for (var row = 0; row < rowCount; row++)
        {
            sb.WriteLine();
            // print row header
            sb.Write(row.ToString("N0").PadLeft(7));
            for (var col = 0; col < cols.Length; col++)
            {
                sb.Write('\t');
                var c = cols[col];
                if (c.Count < row)
                {
                    // This vector is shorter than the others
                    // so just leave a blank space
                    sb.Write(blank);
                    continue;
                }

                var v = c[row];
                if (sparse && Math.Abs(v) < 0.00001)
                {
                    // If the value is less then the precision we are show, just show a blank space
                    // Consider: if the value is not zero, but less than the precision, is there value in showing it?
                    sb.Write(blank);
                }
                else
                {
                    sb.Write(v >= 10 ? v.ToString("N4") : v.ToString("N5"));
                }
            }
        }
    }

    public static void Dump(this Matrix<float> matrix, IDebugWriter sb, bool sparse = false)
    {
        var blank = new string(' ', 7);

        // Print the matrix with column and row headers, tab delimited
        // We will use N4 format for the numbers
        // var sb = new StringBuilder();
        for (var x = 0; x < matrix.ColumnCount; x++)
        {
            if (x == 0)
            {
                // Print Header of X values
                // so we should have around 6 characters for each number
                sb.Write("_".PadRight(7));
                for (var i = 0; i < matrix.RowCount; i++)
                {
                    sb.Write('\t');
                    sb.Write(i.ToString("N0").PadRight(7));
                }
            }

            sb.WriteLine();
            // print row header
            sb.Write(x.ToString("N0").PadLeft(7));
            for (var y = 0; y < matrix.RowCount; y++)
            {
                sb.Write('\t');
                var v = matrix[y, x];
                if (sparse && Math.Abs(v) < 0.00001)
                {
                    // If the value is less then the precision we are show, just show a blank space
                    // Consider: if the value is not zero, but less than the precision, is there value in showing it?
                    sb.Write(blank);
                }
                else
                {
                    sb.Write(v >= 10 ? v.ToString("N4") : v.ToString("N5"));
                }
            }
        }
    }

    public static void Dump(this Matrix<double> matrix, IDebugWriter sb, bool sparse = false)
    {
        // Print the matrix with column and row headers, tab delimited
        // We will use N4 format for the numbers
        for (var x = 0; x < matrix.ColumnCount; x++)
        {
            if (x == 0)
            {
                // Print Header of X values
                // so we should have around 6 characters for each number
                sb.Write("_".PadRight(7));
                for (var i = 0; i < matrix.RowCount; i++)
                {
                    sb.Write('\t');
                    sb.Write(i.ToString("N0").PadLeft(7));
                }
            }

            sb.WriteLine();
            // print row header
            sb.Write(x.ToString("N0").PadLeft(7));
            for (var y = 0; y < matrix.RowCount; y++)
            {
                sb.Write('\t');
                var v = matrix[y, x];
                if (sparse && Math.Abs(v) < 0.00001)
                {
                    // If the value is less then the precision we are show, just show a blank space
                    // Consider: if the value is not zero, but less than the precision, is there value in showing it?
                    sb.Write(new string(' ', 7));
                }
                else
                {
                    sb.Write(v >= 10 ? v.ToString("N4") : v.ToString("N5"));
                }
            }
        }
    }
}