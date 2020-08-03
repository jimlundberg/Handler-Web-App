using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Read Write CSV Files handler
/// </summary>
namespace ReadWriteCsvFile
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<String>
    {
        public String LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream) : base(stream) { }

        public CsvFileWriter(String filename) : base(filename) { }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (String value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                {
                    builder.Append(',');
                }

                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                {
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                }
                else
                {
                    builder.Append(value);
                }
                firstColumn = false;

                // If the shutdown flag is set, exit method
                if (Status.Services.StaticData.ShutdownFlag == true)
                {
                    Console.WriteLine("Shutdown WriteRow row {0} time {1:HH:mm:ss.fff}", value, DateTime.Now);
                    return;
                }
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }

    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream) : base(stream) { }

        public CsvFileReader(String filename) : base(filename) { }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
            {
                return false;
            }

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                String value;

                // Special handling for quoted field
                if (row.LineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        // Test for quote character
                        if (row.LineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != ',')
                    {
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                {
                    row[rows] = value;
                }
                else
                {
                    row.Add(value);
                }
                rows++;

                // Eat up to and including next comma
                while (pos < row.LineText.Length && row.LineText[pos] != ',')
                {
                    pos++;
                }
                if (pos < row.LineText.Length)
                {
                    pos++;
                }

                // If the shutdown flag is set, exit method
                if (Status.Services.StaticData.ShutdownFlag == true)
                {
                    Console.WriteLine("Shutdown ReadRow row {0} time {1:HH:mm:ss.fff}", value, DateTime.Now);
                    return false;
                }
            }

            // Delete any unused items
            while (row.Count > rows)
            {
                row.RemoveAt(rows);
            }

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}
