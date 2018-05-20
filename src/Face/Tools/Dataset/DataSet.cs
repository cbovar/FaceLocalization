using System;
using System.Collections.Generic;
using System.Linq;
using ConvNetSharp.Volume.GPU.Single;

namespace Face.Tools.Dataset
{
    public abstract class DataSet<T>
    {
        private readonly Random _random = new Random();
        protected readonly int Heigth;
        protected readonly int Width;

        protected DataSet(int width, int heigth)
        {
            this.Width = width;
            this.Heigth = heigth;
        }

        public List<T> TrainSet { get; set; } = new List<T>();

        protected abstract Tuple<Volume, Volume, T[]> CreateBatch(int n, T[] entries);

        public Tuple<Volume, Volume, T[]> GetBatch(int n)
        {
            // Select n entry randomly
            var entries = this.TrainSet.OrderBy(x => this._random.Next()).Take(n).ToArray();
            //var entries = this.TrainSet.Take(n).ToArray();

            return CreateBatch(n, entries);
        }
    }
}