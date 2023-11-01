using System;
using System.Collections.Generic;

namespace CustomURP
{
    public class Common
    {
        public enum Pass
        {
            Default = 0,
            Before,
            Normal,
            After,
            Async,
            Max
        };

        public enum RenderType
        {
            Default = 0,
            Depth,
            Normal,
            Light,
            Shadow,
            Post,
            Compute,
            Max
        };
    };

    public class Sort
    {
        public static void QuickSort(List<int> list, int left, int right)
        {
            if (left >= right) return;

            int i, j, temp;
            i = left; j = right;
            temp = list[left];
            while (i != j)
            {
                while (list[j] >= temp && i < j)
                {
                    j--;
                }

                while (list[i] <= temp && i < j)
                {
                    i++;
                }

                if(i < j)
                {
                    int t = list[i];
                    list[i] = list[j];
                    list[j] = t;
                }
            }

            list[left] = list[i];
            list[i] = temp;
            QuickSort(list, i + 1, right);
            QuickSort(list, left, i - 1);
        }
    }
}