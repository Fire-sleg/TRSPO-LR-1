using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using Microsoft.Win32;
using System.IO;
using OxyPlot.Axes;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts;
using Axis = LiveCharts.Wpf.Axis;
using LiveCharts.Definitions.Charts;
using System.Collections.Concurrent;
using System.Threading;

namespace LR_1_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static TextBox? txtEditorBox { get; set; }
        public static TextBox? minThreadBox { get; set; }
        public static TextBox? maxThreadBox { get; set; }
        public static string[]? lines { get; set; }
        public static List<int>? parralelListResult { get; set; } = new List<int>();
        public static int? notParralelResult { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesHistogramCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
                lines = File.ReadAllLines(openFileDialog.FileName);
            }
        }
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            lines = txtEditor.Text.Split("\r\n");
        }
        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            txtEditor.Text = "";
        }
        private void CalculateClick(object sender, RoutedEventArgs e)
        {
            //Reset
            //if (cartesianChart.Series != null)
            //{
            //    cartesianChart.Series.Clear();
            //}
            //if (histogram.Series != null)
            //{
            //    histogram.Series.Clear();
            //}
            table.ItemsSource = null;
            table.Items.Clear();

            parralelListResult = new List<int>();

            txtEditorBox = (TextBox)FindName("txtEditor");
            minThreadBox = (TextBox)FindName("minThread");
            maxThreadBox = (TextBox)FindName("maxThread");

            int size = numberOfSegments.Text == "" ? 0 : int.Parse(numberOfSegments.Text);

            var resultList = MainCalculation(size);

            size = resultList.Count;



            var colors = GenerateRandomColor(size);

            SeriesCollection = new SeriesCollection();
            SeriesHistogramCollection = new SeriesCollection();

            //Draw Lines
            int i = 0;
            foreach (var group in resultList)
            {
                Color color = (Color)ColorConverter.ConvertFromString(colors[i]);

                foreach (var vector in group)
                {
                    var lineSeries = new LineSeries
                    {
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(color),
                        LineSmoothness = 0,
                        PointGeometry = null
                    };
                    lineSeries.Values = new ChartValues<ObservablePoint>
                    {
                        new ObservablePoint(vector.Point1.X, vector.Point1.Y),
                        new ObservablePoint(vector.Point2.X, vector.Point2.Y)
                    };
                    //SeriesCollection.Add(lineSeries);

                }

                i++;
            }

            YFormatter = value => Math.Round(value, 2).ToString();
            XFormatter = value => Math.Round(value, 2).ToString();

            //cartesianChart.Series = SeriesCollection;

            //Draw Histogram
            var valuesHistorgram = new ChartValues<int>();
            if (notParralelResult != null)
            {
                valuesHistorgram.Add((int)notParralelResult);
            }
            else
            {
                valuesHistorgram.Add(1000);
            }
            foreach (var paralel in parralelListResult)
            {
                valuesHistorgram.Add(paralel);
            }
            SeriesHistogramCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "ms",
                    Values = valuesHistorgram
                }
            };


            int min = int.Parse(minThreadBox.Text);
            int max = int.Parse(maxThreadBox.Text) + 1;

            minTime.Text = $"Min Time = {valuesHistorgram.Min()}";
            minThreadText.Text = $"Min Thread = {valuesHistorgram.IndexOf(valuesHistorgram.Min()) + min - 1}";

            int[] numbers = new int[max - min + 1];
            numbers[0] = 1;
            for (int j = min; j < max; j++)
            {
                numbers[j - min + 1] = j;
            }
            //int[] numbers = Enumerable.Range(min - 1, max).ToArray();
            for (int j = 0; j < numbers.Length; j++)
            {
                table.Items.Add(new double[] { numbers[j], valuesHistorgram[j] });
            }
            Labels = numbers.Select(x => x.ToString()).ToArray();

            histogram.Series = SeriesHistogramCollection;
            //cartesianChart.Series = SeriesCollection;
            DataContext = this;
        }

        //static void WriteInFile(List<List<Vector>> resultList)
        //{
        //    using (StreamWriter writetext = new StreamWriter("write.txt"))
        //    {
        //        foreach (var vectorList in resultList)
        //        {
        //            foreach(var vector in vectorList)
        //            {
        //                writetext.WriteLine(vector.ToString());
        //            }
        //        }
        //    }
        //}
        static void WriteInFile(List<Vector> resultList)
        {
            using (StreamWriter writetext = new StreamWriter("write.txt"))
            {
                foreach (var vector in resultList)
                {
                    writetext.WriteLine(vector.ToString());
                }
            }
        }
        public static List<string> GenerateRandomColor(int size)
        {
            List<string> colors = new List<string>();
            Random random = new Random();
            for (int i = 0; i < size; i++)
            {
                byte[] colorBytes = new byte[3];
                random.NextBytes(colorBytes);

                Color randomColor = Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
                colors.Add($"#{randomColor.R:X2}{randomColor.G:X2}{randomColor.B:X2}");
            }
            return colors;
        }
        private static List<List<Vector>> MainCalculation(int size)
        {
            List<Vector> segments = new List<Vector>();
            if (txtEditorBox.Text == "")
            {
                segments = GenerateSegments(size);
                WriteInFile(segments);
            }
            else
            {
                foreach (var line in lines)
                {
                    string[] parts = line.Split('|');

                    if (parts.Length == 2)
                    {
                        Point point1 = Point.FromString(parts[0]);
                        Point point2 = Point.FromString(parts[1]);

                        segments.Add(new Vector(point1, point2));
                    }

                }
            }



            HashSet<Vector> set = new HashSet<Vector>(segments);
            segments = new List<Vector>(set);

            //NoParallelFind(segments);
            List<List<Vector>> result = new List<List<Vector>>();
            int min = int.Parse(minThreadBox.Text);
            int max = int.Parse(maxThreadBox.Text);
            int numberOfIteration = 1; //10
            for (int i = min; i <= max; i++)
            {
                result = ParallelFind(segments, i, numberOfIteration);
            }
            return result;
        }

        static List<Vector> GenerateSegments(int count)
        {
            HashSet<Vector> segments = new HashSet<Vector>();
            for (int i = 0; i < count; i++)
            {
                Point point1 = new Point();
                Point point2 = new Point();

                segments.Add(new Vector(point1, point2));
            }
            return segments.ToList();
        }



        //const int paralelization = 8;
        static List<List<Vector>> ParallelFind(List<Vector> segments, int paralelization, int numberOfIteration)
        {
            long sum = 0;
            var newList = segments.Select(x => new List<Vector> { x }).ToList();
            for (int i = 0; i < numberOfIteration; i++)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                newList = ParallelAlgorithm(newList, paralelization).Where(x => x.Count > 1).ToList();

                stopwatch.Stop();

                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                sum += elapsedMilliseconds;
            }
            double temp = (double)sum / numberOfIteration;
            parralelListResult.Add((int)(temp));
            return newList;
        }

        static List<List<Vector>> NoParallelFind(List<Vector> segments)
        {
            var newList = segments.Select(x => new List<Vector> { x }).ToList();

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            newList = NotParallelAlgorithm(newList).Where(x => x.Count > 1).ToList();

            stopwatch.Stop();

            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            notParralelResult = (int?)elapsedMilliseconds;

            return newList;
        }

        static bool AreParallel(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return false;
            }

            if (vector1.X == vector2.X && vector1.Y == vector2.Y)
            {
                //return false;
                return true;
            }

            return vector1.X * vector2.Y - vector2.X * vector1.Y == 0;
        }


        static List<List<Vector>> ParallelAlgorithm(List<List<Vector>> group, int parallelization)
        {
            // Use all available processors
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)((1 << Environment.ProcessorCount) - 1);

            List<Thread> threads = new List<Thread>();
            int segment = group.Count / parallelization;
            Dictionary<double, List<Vector>> dictionary = new Dictionary<double, List<Vector>>();
            Dictionary<double, List<Vector>> zeroDictionary = new Dictionary<double, List<Vector>>();
            int rest = group.Count % parallelization;

            for (int i = 0; i < parallelization; i++)
            {
                var start = i * segment;
                var end = start + segment;
                if (i == parallelization - 1)
                {
                    end += rest;
                }

                Thread thread = new Thread(() =>
                {

                    double key = 0.0;

                    List<Vector> vectorList = new List<Vector>();
                    bool[] arrayBool = new bool[end - start];
                    for (int j = start; j < end; j++)
                    {
                        if (arrayBool[j - start])
                        {
                            continue;
                        }
                        HashSet<Vector> vector = new HashSet<Vector>(group[j]);
                        for (int k = j + 1; k < end; k++)
                        {
                            if (AreParallel(group[j][0], group[k][0]))
                            {
                                arrayBool[k - start] = true;
                                for (int h = 0; h < group[k].Count; h++)
                                {
                                    vector.Add(group[k][h]);
                                }

                            }
                        }
                        vectorList = new List<Vector>(vector);
                        for (int h = 0; h < vectorList.Count; h++)
                        {
                            if (vectorList[h].X == 0 || vectorList[h].Y == 0)
                            {
                                continue;
                            }
                            else
                            {
                                key = Math.Round((double)vectorList[h].X / vectorList[h].Y, 6);
                                break;
                            }
                        }
                        if (vectorList[0].X == 0 || vectorList[0].Y == 0)
                        {
                            lock (zeroDictionary)
                            {
                                if (vectorList[0].X == 0)
                                {
                                    if (!zeroDictionary.ContainsKey(0))
                                    {
                                        zeroDictionary[0] = vectorList;
                                    }
                                    else
                                    {
                                        zeroDictionary[0].AddRange(vectorList);
                                    }
                                }
                                else
                                {
                                    if (!zeroDictionary.ContainsKey(1))
                                    {
                                        zeroDictionary[1] = vectorList;
                                    }
                                    else
                                    {
                                        zeroDictionary[1].AddRange(vectorList);
                                    }
                                }
                            }
                        }
                        else
                        {
                            lock (dictionary)
                            {
                                key = Math.Round((double)vectorList[0].X / vectorList[0].Y, 6);
                                if (!dictionary.ContainsKey(key))
                                {
                                    dictionary[key] = vectorList;
                                }
                                else
                                {
                                    dictionary[key].AddRange(vectorList);
                                }
                            }
                        }
                    }



                    /*
                    for (int j = start; j < end; j++)
                    {

                        HashSet<Vector> vectorSet = new HashSet<Vector>(group[j]);
                        for (int k = j + 1; k < end; k++)
                        {
                            if (AreParallel(group[j][0], group[k][0]))
                            {
                                vectorSet.UnionWith(group[k]);
                            }
                        }

                        List<Vector> vectorList = new List<Vector>(vectorSet);
                        double key = GetKey(vectorList);

                        if (key == 0 || key == 1)
                        {
                            lock (zeroDictionary)
                            {
                                if (!zeroDictionary.ContainsKey(key))
                                {
                                    zeroDictionary[key] = new List<Vector>();
                                }
                                zeroDictionary[key].AddRange(vectorList);
                            }
                        }
                        else
                        {
                            lock (dictionary)
                            {
                                if (!dictionary.ContainsKey(key))
                                {
                                    dictionary[key] = new List<Vector>();
                                }
                                dictionary[key].AddRange(vectorList);
                            }
                        }

                    }
                    */
                });

                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            List<List<Vector>> vectors = new List<List<Vector>>();
            vectors.AddRange(dictionary.Values);
            vectors.AddRange(zeroDictionary.Values);
            return vectors;
        }





        private static double GetKey(List<Vector> vectors)
        {
            foreach (var vector in vectors)
            {
                if (vector.X != 0 && vector.Y != 0)
                {
                    return Math.Round((double)vector.X / vector.Y, 6);
                }
            }
            return vectors[0].X == 0 ? 0 : 1;
        }


        //static List<List<Vector>> ParallelAlgorithm(List<List<Vector>> group, int paralelization)
        //{
        //    Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)((1 << Environment.ProcessorCount) - 1);
        //    List<Thread> threads = new List<Thread>();
        //    int segment = group.Count / paralelization;
        //    Dictionary<double, List<Vector>> dictionary = new Dictionary<double, List<Vector>>();
        //    Dictionary<double, List<Vector>> zeroDictionary = new Dictionary<double, List<Vector>>();
        //    int rest = group.Count - segment * paralelization;

        //    for (int i = 0; i < paralelization; i++)
        //    {
        //        var start = i * segment;
        //        var end = (i + 1) * segment;
        //        if (rest > 0 && i == paralelization - 1)
        //        {
        //            end += rest;
        //        }

        //        Thread thread = new Thread(() =>
        //        {
        //            double key = 0.0;
        //            List<Vector> vectorList = new List<Vector>();
        //            bool[] arrayBool = new bool[end - start];

        //            for (int j = start; j < end; j++)
        //            {
        //                if (arrayBool[j - start])
        //                {
        //                    continue;
        //                }

        //                HashSet<Vector> vector = new HashSet<Vector>(group[j]);
        //                for (int k = j + 1; k < end; k++)
        //                {
        //                    if (AreParallel(group[j][0], group[k][0]))
        //                    {
        //                        arrayBool[k - start] = true;
        //                        vector.UnionWith(group[k]);
        //                    }
        //                }

        //                vectorList = new List<Vector>(vector);
        //                for (int h = 0; h < vectorList.Count; h++)
        //                {
        //                    if (vectorList[h].X == 0 || vectorList[h].Y == 0)
        //                    {
        //                        continue;
        //                    }
        //                    else
        //                    {
        //                        key = Math.Round((double)vectorList[h].X / vectorList[h].Y, 6);
        //                        break;
        //                    }
        //                }

        //                if (vectorList[0].X == 0 || vectorList[0].Y == 0)
        //                {
        //                    lock (zeroDictionary)
        //                    {
        //                        if (vectorList[0].X == 0)
        //                        {
        //                            if (!zeroDictionary.ContainsKey(0))
        //                            {
        //                                zeroDictionary[0] = vectorList;
        //                            }
        //                            else
        //                            {
        //                                zeroDictionary[0].AddRange(vectorList);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (!zeroDictionary.ContainsKey(1))
        //                            {
        //                                zeroDictionary[1] = vectorList;
        //                            }
        //                            else
        //                            {
        //                                zeroDictionary[1].AddRange(vectorList);
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    lock (dictionary)
        //                    {
        //                        key = Math.Round((double)vectorList[0].X / vectorList[0].Y, 6);
        //                        if (!dictionary.ContainsKey(key))
        //                        {
        //                            dictionary[key] = vectorList;
        //                        }
        //                        else
        //                        {
        //                            dictionary[key].AddRange(vectorList);
        //                        }
        //                    }
        //                }
        //            }
        //        });

        //        thread.Start();
        //        threads.Add(thread);
        //    }

        //    foreach (var thread in threads)
        //    {
        //        thread.Join();
        //    }

        //    List<List<Vector>> vectors = new List<List<Vector>>();
        //    vectors.AddRange(dictionary.Values);
        //    vectors.AddRange(zeroDictionary.Values);
        //    return vectors;
        //}
        //static List<List<Vector>> ParallelAlgorithm(List<List<Vector>> group, int paralelization)
        //{

        //    List<Task> tasks = new List<Task>();
        //    int segment = group.Count / paralelization;
        //    Dictionary<double, List<Vector>> dictionary = new Dictionary<double, List<Vector>>();
        //    Dictionary<double, List<Vector>> zeroDictionary = new Dictionary<double, List<Vector>>();
        //    int rest = group.Count - segment * paralelization;
        //    for (int i = 0; i < paralelization; i++)
        //    {
        //        var start = i * segment;
        //        var end = (i + 1) * segment;
        //        if (rest > 0 && i == paralelization - 1)
        //        {
        //            end += rest;
        //        }

        //        var task = Task.Run(() =>
        //        {
        //            double key = 0.0;

        //            List<Vector> vectorList = new List<Vector>();
        //            bool[] arrayBool = new bool[end - start];
        //            for (int j = start; j < end; j++)
        //            {
        //                if (arrayBool[j - start])
        //                {
        //                    continue;
        //                }
        //                HashSet<Vector> vector = new HashSet<Vector>(group[j]);
        //                for (int k = j + 1; k < end; k++)
        //                {
        //                    if (AreParallel(group[j][0], group[k][0]))
        //                    {
        //                        arrayBool[k - start] = true;
        //                        for (int h = 0; h < group[k].Count; h++)
        //                        {
        //                            vector.Add(group[k][h]);
        //                        }

        //                    }
        //                }
        //                vectorList = new List<Vector>(vector);
        //                for (int h = 0; h < vectorList.Count; h++)
        //                {
        //                    if (vectorList[h].X == 0 || vectorList[h].Y == 0)
        //                    {
        //                        continue;
        //                    }
        //                    else
        //                    {
        //                        key = Math.Round((double)vectorList[h].X / vectorList[h].Y, 6);
        //                        break;
        //                    }
        //                }
        //                if (vectorList[0].X == 0 || vectorList[0].Y == 0)
        //                {
        //                    lock (zeroDictionary)
        //                    {
        //                        if (vectorList[0].X == 0)
        //                        {
        //                            if (!zeroDictionary.ContainsKey(0))
        //                            {
        //                                zeroDictionary[0] = vectorList;
        //                            }
        //                            else
        //                            {
        //                                zeroDictionary[0].AddRange(vectorList);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (!zeroDictionary.ContainsKey(1))
        //                            {
        //                                zeroDictionary[1] = vectorList;
        //                            }
        //                            else
        //                            {
        //                                zeroDictionary[1].AddRange(vectorList);
        //                            }
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    lock (dictionary)
        //                    {
        //                        key = Math.Round((double)vectorList[0].X / vectorList[0].Y, 6);
        //                        if (!dictionary.ContainsKey(key))
        //                        {
        //                            dictionary[key] = vectorList;
        //                        }
        //                        else
        //                        {
        //                            dictionary[key].AddRange(vectorList);
        //                        }
        //                    }
        //                }
        //            }

        //        });
        //        tasks.Add(task);
        //    }

        //    Task.WaitAll(tasks.ToArray());

        //    List<List<Vector>> vectors = new List<List<Vector>>();
        //    vectors = dictionary.Values.ToList();
        //    vectors.AddRange(zeroDictionary.Values.ToList());
        //    return vectors;
        //}

        static List<List<Vector>> NotParallelAlgorithm(List<List<Vector>> group)
        {
            Dictionary<double, List<Vector>> dictionary = new Dictionary<double, List<Vector>>();
            Dictionary<double, List<Vector>> zeroDictionary = new Dictionary<double, List<Vector>>();
            double key = 0.0;

            List<Vector> vectorList = new List<Vector>();
            bool[] arrayBool = new bool[group.Count];
            for (int i = 0; i < group.Count; i++)
            {
                if (arrayBool[i])
                {
                    continue;
                }
                HashSet<Vector> vector = new HashSet<Vector>(group[i]);
                for (int j = i + 1; j < group.Count; j++)
                {
                    if (AreParallel(group[i][0], group[j][0]))
                    {
                        arrayBool[j] = true;
                        for (int h = 0; h < group[j].Count; h++)
                        {
                            vector.Add(group[j][h]);
                        }

                    }
                }
                vectorList = new List<Vector>(vector);

                if (vectorList[0].X == 0 || vectorList[0].Y == 0)
                {
                    if (vectorList[0].X == 0)
                    {
                        if (!zeroDictionary.ContainsKey(0))
                        {
                            zeroDictionary[0] = vectorList;
                        }
                        else
                        {
                            zeroDictionary[0].AddRange(vectorList);
                        }
                    }
                    else
                    {
                        if (!zeroDictionary.ContainsKey(1))
                        {
                            zeroDictionary[1] = vectorList;
                        }
                        else
                        {
                            zeroDictionary[1].AddRange(vectorList);
                        }
                    }

                }
                else
                {
                    key = Math.Round((double)vectorList[0].X / vectorList[0].Y, 6);
                    if (!dictionary.ContainsKey(key))
                    {
                        dictionary[key] = vectorList;
                    }
                    else
                    {
                        dictionary[key].AddRange(vectorList);
                    }
                }

            }
            List<List<Vector>> vectors = new List<List<Vector>>();
            vectors = dictionary.Values.ToList();
            vectors.AddRange(zeroDictionary.Values.ToList());
            return vectors;
        }
        class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
            private const int Minimum = 1;
            private const int Maximum = 1000;
            private static Random random = new Random(0);
            public static int RandomCoordinate()
            {
                return random.Next(Minimum, Maximum);
            }

            public Point()
            {
                X = RandomCoordinate();
                Y = RandomCoordinate();
            }
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool IsZero()
            {
                return X == 0 && Y == 0;
            }

            public override string ToString()
            {
                return $"({X}, {Y})";
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;

                Point other = (Point)obj;
                return X == other.X && Y == other.Y;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(X, Y);
            }
            public static Point FromString(string str)
            {
                string[] parts = str.Trim(new char[] { ')', ' ', '(' }).Split(',');

                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    return new Point(x, y);
                }
                else
                {
                    throw new ArgumentException("Invalid input format.");
                }
            }
        }
        class Vector : Point
        {
            public Point? Point1 { get; set; }
            public Point? Point2 { get; set; }

            public Vector(Point point1, Point point2)
            {
                X = point1.X - point2.X;
                Y = point1.Y - point2.Y;
                Point1 = point1;
                Point2 = point2;
            }
            public Vector(int x, int y)
            {
                X = x;
                Y = y;
            }
            public override string ToString()
            {
                return $"{Point1} | {Point2}";
            }
        }
    }
}

