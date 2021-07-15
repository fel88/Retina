using System;


namespace Retina
{
    public class InternalArray
    {
        public override string ToString()
        {
            if (shp == null)
            {
                string shp1 = "(";
                for (int i = 0; i < Shape.Length; i++)
                {
                    if (i > 0) { shp1 += ", "; }
                    shp1 += Shape[i];
                }
                shp1 += ")";
                shp = $"{Name}: {shp1}";
            }
            return shp;
        }
        string shp = null;
        #region ctors
        public InternalArray(int[] dims)
        {
            Shape = (int[])dims.Clone();
            long l = 1;
            for (int i = 0; i < dims.Length; i++)
            {
                l *= dims[i];
            }
            Data = new double[l];


            offsets = new int[dims.Length - 1];
            for (int i = 0; i < dims.Length - 1; i++)
            {
                int val = 1;

                for (int j = 0; j < (i + 1); j++)
                {
                    val *= Shape[Shape.Length - 1 - j];
                }
                offsets[offsets.Length - 1 - i] = val;
            }            
        }

        #endregion



        #region fields

        public string Name { get; set; }
        public readonly int[] offsets = null;
        public double[] Data;
        public int[] Shape;

        #endregion

        public void Set4D(int v1, int v2, int v3, int v4, double val)
        {            
            int[] dat = new int[] { v1, v2, v3 };

            int pos = v4;
            for (int i = 0; i < 3; i++)
            {
                pos += dat[i] * offsets[i];
            }
            Data[pos] = val;
        }

        public void Set3D(int v1, int v2, int v3, double val)
        {
            Data[v3 + v1 * offsets[0] + v2 * offsets[1]] = val;
        }


        public double Get3D(int v1, int v2, int v3)
        {
            return Data[v1 * offsets[0] + v2 * offsets[1] + v3];
        }

        public void Sub(InternalArray ar)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] -= ar.Data[i];
            }
        }

        public void Sub(double bias)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] -= bias;
            }
        }

        public double GetItem(int[] index)
        {
            int pos = 0;
            for (int i = 0; i < index.Length; i++)
            {
                pos += index[i] * Shape[i];
            }
            return Data[pos];
        }

        public InternalArray Clone()
        {
            InternalArray ret = new InternalArray(Shape);
            ret.Shape = (int[])ret.Shape.Clone();
            ret.Data = new double[Data.Length];
            Array.Copy(Data, ret.Data, Data.Length);
            return ret;
        }

     
        internal void Set(int[] inds, double val)
        {
            switch (inds.Length)
            {
                case 2:
                    Set2D(inds[0], inds[1], val);
                    break;
                case 3:
                    Set3D(inds[0], inds[1], inds[2], val);
                    break;
                case 4:
                    Set4D(inds[0], inds[1], inds[2], inds[3], val);
                    break;
                default:
                    throw new Exception($"set value: unsupported dim len: {inds.Length}");
            }
        }
        internal void Set2D(int i, int j, double val)
        {
            int pos = i * Shape[1] + j;
            Data[pos] = val;
        }


        internal bool WithIn(int x, int y, int z, int k)
        {
            return x >= 0 && y >= 0 && z >= 0 && k >= 0 && x < Shape[0] && y < Shape[1] && z < Shape[2] && k < Shape[3];
        }

        public static double tolerance = 10e-6;    
    }
}