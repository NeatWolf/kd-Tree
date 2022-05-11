using System.Collections.Generic;
using System;
using UnityEngine;

public abstract class KDTree<P, S>
    where P : IEquatable<P> where S : IComparable<S>
{
    protected class Node
    {
        public P point;
        public Node left, right;

        public Node(P point)
        {
            this.point = point;
        }
    }

    /// <summary>
    /// number of dimensions
    /// </summary>
    protected abstract int k { get; }
    /// <summary>
    /// a point the considered invalid, used for serializing
    /// </summary>
    public abstract P InvalidPoint { get; }

    protected Node root;

    protected KDTree() { }
    public KDTree(List<P> points)
    {
        BalanceInsertion(ref root, 0, points, 0, points.Count);
    }

    //insert the points by median sort
    private void BalanceInsertion(ref Node node, int d, List<P> points, int start, int end)
    {
        var size = end - start;
        if (size < 1)
            return;
        if (size == 1)
        {
            node = new Node(points[start]);
            return;
        }

        int mid = start + size / 2;
        int nd = (d + 1) % k;

        IndexSort(points, start, end, mid, (a, b) => Get(a, d).CompareTo(Get(b, d)));

        node = new Node(points[mid]);
        BalanceInsertion(ref node.left, nd, points, start, mid);
        BalanceInsertion(ref node.right, nd, points, mid + 1, end);
    }

    public void Insert(P point)
    {
        Insert(ref root, 0, point);
    }
    private void Insert(ref Node node, int d, P point)
    {
        if (node == null)
        {
            node = new Node(point);
            return;
        }
        int nd = (d + 1) % k;
        var comp = Get(point, d).CompareTo(Get(node.point, d));
        if (comp < 0)
            Insert(ref node.left, nd, point);
        else
            Insert(ref node.right, nd, point);
    }

    /// <summary>
    /// get all the points between min and max
    /// </summary>
    public List<P> Query(P min, P max)
    {
        var res = new List<P>();
        Query(root, 0, min, max, res);
        return res;
    }
    public void Query(P min, P max, List<P> output)
    {
        Query(root, 0, min, max, output);
    }
    void Query(Node node, int d, P min, P max, List<P> points)
    {
        if (node == null)
            return;

        var dmin = Get(min, d);
        var dmax = Get(max, d);
        var t = Get(node.point, d);

        int nd = (d + 1) % k;

        if (t.CompareTo(dmin) < 0)
            Query(node.right, nd, min, max, points);
        else if (t.CompareTo(dmax) <= 0)
        {
            bool add = true;
            var od = nd;
            for (int i = 0; i < k - 1; i++)
            {
                dmin = Get(min, od);
                dmax = Get(max, od);
                t = Get(node.point, od);
                if (t.CompareTo(dmin) < 0 || t.CompareTo(dmax) > 0)
                {
                    add = false;
                    break;
                }
                od = (od + 1) % k;
            }
            if (add)
                points.Add(node.point);

            Query(node.right, nd, min, max, points);
            Query(node.left, nd, min, max, points);
        }
        else
            Query(node.left, nd, min, max, points);
    }

    /// <summary>
    /// convert the tree to list, for serialize proposes 
    /// </summary>
    public List<P> ToList()
    {
        var list = new List<P>();
        ToList(root, list);
        return list;
    }
    private void ToList(Node node, List<P> list)
    {
        if (node == null)
        {
            list.Add(InvalidPoint);
            return;
        }
        list.Add(node.point);
        ToList(node.left, list);
        ToList(node.right, list);
    }

    /// <summary>
    /// retrieve the tree from the list
    /// </summary>
    public void FromList(List<P> list)
    {
        int index = 0;
        FromList(ref root, list, ref index);
    }
    private void FromList(ref Node node, List<P> list, ref int index)
    {
        if (index > list.Count - 1)
            return;

        var val = list[index++];
        if (val.Equals(InvalidPoint))
            return;

        node = new Node(val);
        FromList(ref node.left, list, ref index);
        FromList(ref node.right, list, ref index);
    }

    /// <summary>
    /// get the point dimension
    /// </summary>
    protected abstract S Get(P p, int d);
    /// <summary>
    /// set the point dimension
    /// </summary>
    protected abstract void Set(ref P p, int d, S value);

    /// <summary>
    /// split the space between min and max at the point p
    /// </summary>
    protected void Split(P p, int d, P min, P max, out P smin, out P smax)
    {
        smin = default;
        smax = default;

        Set(ref smin, d, Get(p, d));
        Set(ref smax, d, Get(p, d));

        var od = d;
        for (int i = 0; i < k - 1; i++)
        {
            od = (od + 1) % k;
            Set(ref smin, od, Get(min, od));
            Set(ref smax, od, Get(max, od));
        }
    }

    #region Median Sort

    //using quick sort until we find the sorted value at the given index then terminate
    private static void IndexSort<T>(List<T> array, int start, int end, int index, Comparison<T> comparison)
    {
        int pivot = Partition(array, start, end, comparison);

        if (pivot > index)
            IndexSort(array, start, pivot, index, comparison);
        else if (pivot < index)
            IndexSort(array, pivot + 1, end, index, comparison);
    }

    private static int Partition<T>(List<T> array, int start, int end, Comparison<T> comparison)
    {
        int i = start;
        T pivotVal = array[start];
        for (int j = start + 1; j < end; j++)
            if (comparison(array[j], pivotVal) <= 0)
                Swap(array, ++i, j);
        Swap(array, i, start);
        return i;
    }

    private static void Swap<T>(List<T> array, int i, int j)
    {
        var t = array[i];
        array[i] = array[j];
        array[j] = t;
    }

    #endregion
}

public sealed class Tree3D : KDTree<Vector3, float>
{
    public Tree3D() { }
    public Tree3D(List<Vector3> points) : base(points) { }

    protected override int k => 3;
    public override Vector3 InvalidPoint => new Vector3(float.MinValue, float.MinValue, float.MinValue);

    protected override float Get(Vector3 p, int d) => p[d];
    protected override void Set(ref Vector3 p, int d, float value) => p[d] = value;

    public void DrawGizmos(Vector3 min, Vector3 max)
    {
        DrawGizmos(root, 0, min, max);
    }
    private void DrawGizmos(Node node, int d, Vector3 min, Vector3 max)
    {
        if (node == null)
            return;

        Split(node.point, d, min, max, out var smin, out var smax);
        Gizmos.DrawWireCube((smin + smax) * 0.5f, smax - smin);
        var nd = (d + 1) % k;
        DrawGizmos(node.left, nd, smin, max);
        DrawGizmos(node.right, nd, min, smax);
    }
}

public sealed class Tree2D : KDTree<Vector2, float>
{
    public Tree2D() { }
    public Tree2D(List<Vector2> points) : base(points) { }

    protected override int k => 2;
    public override Vector2 InvalidPoint => new Vector2(float.MinValue, float.MinValue);

    protected override float Get(Vector2 p, int d) => p[d];
    protected override void Set(ref Vector2 p, int d, float value) => p[d] = value;

    public void DrawGizmos(Vector2 min, Vector2 max)
    {
        DrawGizmos(root, 0, min, max);
    }
    private void DrawGizmos(Node node, int d, Vector2 min, Vector2 max)
    {
        if (node == null)
            return;

        Split(node.point, d, min, max, out var smin, out var smax);
        Gizmos.DrawLine(smin, smax);
        var nd = (d + 1) % k;
        DrawGizmos(node.left, nd, smin, max);
        DrawGizmos(node.right, nd, min, smax);
    }
}