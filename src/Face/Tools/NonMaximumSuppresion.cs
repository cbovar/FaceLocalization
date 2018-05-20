using System;
using System.Collections.Generic;
using System.Linq;

namespace Face.Tools
{
    public class NonMaximumSuppresion
    {
        private List<float> CopyByIndexes(float[] x, IEnumerable<int> ids)
        {
            return ids.Select(t => x[t]).ToList();
        }

        private List<float> Divide(List<float> x1, List<float> x2)
        {
            var result = new List<float>();

            for (var i = 0; i < x1.Count; i++)
            {
                result.Add(x1[i] / x2[i]);
            }

            return result;
        }

        private List<float> Maximum(float max, List<float> x)
        {
            var maxVec = new List<float>(x);

            for (var index = 0; index < maxVec.Count; index++)
            {
                var f = maxVec[index];

                if (f < max)
                {
                    maxVec[index] = max;
                }
            }

            return maxVec;
        }

        private List<float> Minimum(float min, List<float> x)
        {
            var minVec = new List<float>(x);

            for (var index = 0; index < minVec.Count; index++)
            {
                var f = minVec[index];

                if (f > min)
                {
                    minVec[index] = min;
                }
            }

            return minVec;
        }

        private List<float> Multiply(List<float> x1, List<float> x2)
        {
            var result = new List<float>();

            for (var i = 0; i < x1.Count; i++)
            {
                result.Add(x1[i] * x2[i]);
            }

            return result;
        }

        public List<BoundingBox> Nms(List<BoundingBox> boxes, float threshold)
        {
            if (boxes.Count == 0)
            {
                return new List<BoundingBox>();
            }

            // grab the coordinates of the bounding boxes
            var x1 = boxes.Select(o => o.x1).ToArray();
            var y1 = boxes.Select(o => o.y1).ToArray();
            var x2 = boxes.Select(o => o.x2).ToArray();
            var y2 = boxes.Select(o => o.y2).ToArray();

            // compute the area of the bounding boxes and sort the bounding
            // boxes by the bottom-right y-coordinate of the bounding box
            var area = boxes.Select(o => (o.x2 - o.x1 + 1) * (o.y2 - o.y1 + 1)).ToArray();
            var idxs = y2.Select((x, i) => new Tuple<int, float>(i, x)).OrderBy(o => o.Item2).Select(o => o.Item1).ToList();

            var pick = new List<int>();

            // keep looping while some indexes still remain in the indexes list
            while (idxs.Count > 0)
            {
                // grab the last index in the indexes list and add the
                // index value to the list of picked indexes
                var i = idxs.Last();
                pick.Add(i);

                // find the largest (x, y) coordinates for the start of
                // the bounding box and the smallest (x, y) coordinates
                // for the end of the bounding box
                var idxsWoLast = RemoveLast(idxs);

                var xx1 = Maximum(x1[i], CopyByIndexes(x1, idxsWoLast));
                var yy1 = Maximum(y1[i], CopyByIndexes(y1, idxsWoLast));
                var xx2 = Minimum(x2[i], CopyByIndexes(x2, idxsWoLast));
                var yy2 = Minimum(y2[i], CopyByIndexes(y2, idxsWoLast));

                // compute the width and height of the bounding box
                var w = Maximum(0, Subtract(xx2, xx1));
                var h = Maximum(0, Subtract(yy2, yy1));

                // compute the ratio of overlap
                var overlap = Divide(Multiply(w, h), CopyByIndexes(area, idxsWoLast));

                // delete all indexes from the index list that have
                var deleteIdxs = overlap.Select((v, idx) => new Tuple<int, float>(idx, v)).Where(o => o.Item2 > threshold).Select(o => o.Item1).ToList();
                deleteIdxs.Add(idxs.Count - 1);

                idxs = RemoveByIndexes(idxs, deleteIdxs);
            }

            var result = new List<BoundingBox>();

            foreach (var i in pick)
            {
                result.Add(boxes[i]);
            }

            return result;
        }

        private List<int> RemoveByIndexes(List<int> idxs, List<int> deleteIdxs)
        {
            var resultVec = new List<int>(idxs);
            var offset = 0;

            foreach (var i in deleteIdxs)
            {
                resultVec.RemoveAt(i + offset);
                offset -= 1;
            }

            return resultVec;
        }

        private List<int> RemoveLast(List<int> idxs)
        {
            var result = new List<int>(idxs);
            result.RemoveAt(result.Count - 1);
            return result;
        }


        private List<float> Subtract(List<float> x1, List<float> x2)
        {
            var result = new List<float>();

            for (var i = 0; i < x1.Count; i++)
            {
                result.Add(x1[i] - x2[i] + 1);
            }

            return result;
        }
    }
}