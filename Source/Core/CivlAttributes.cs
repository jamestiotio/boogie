﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Boogie
{
  public class LayerRange
  {
    public static int Min = 0;
    public static int Max = int.MaxValue;
    public static LayerRange MinMax = new LayerRange(Min, Max);

    public int LowerLayer;
    public int UpperLayer;

    public LayerRange(int layer) : this(layer, layer)
    {
    }

    public LayerRange(int lower, int upper)
    {
      Debug.Assert(lower <= upper);
      this.LowerLayer = lower;
      this.UpperLayer = upper;
    }

    public bool Contains(int layerNum)
    {
      return LowerLayer <= layerNum && layerNum <= UpperLayer;
    }

    public bool Subset(LayerRange other)
    {
      return other.LowerLayer <= LowerLayer && UpperLayer <= other.UpperLayer;
    }

    public bool OverlapsWith(LayerRange other)
    {
      return LowerLayer <= other.UpperLayer && other.LowerLayer <= UpperLayer;
    }

    public static LayerRange Union(LayerRange first, LayerRange second)
    {
      return new LayerRange(Math.Min(first.LowerLayer, second.LowerLayer), Math.Max(first.UpperLayer, second.UpperLayer));
    }

    public static LayerRange Union(List<LayerRange> layerRanges)
    {
      Debug.Assert(layerRanges.Any());
      var unionLayerRange = layerRanges.First();
      foreach (var layerRange in layerRanges)
      {
        unionLayerRange = Union(unionLayerRange, layerRange);
      }
      return unionLayerRange;
    }

    public override string ToString()
    {
      return $"[{LowerLayer}, {UpperLayer}]";
    }

    public override bool Equals(object obj)
    {
      LayerRange other = obj as LayerRange;
      if (obj == null)
      {
        return false;
      }

      return LowerLayer == other.LowerLayer && UpperLayer == other.UpperLayer;
    }

    public override int GetHashCode()
    {
      return (23 * 31 + LowerLayer) * 31 + UpperLayer;
    }
  }

  public static class CivlAttributes
  {
    public const string LAYER = "layer";
    public const string YIELDS = "yields";
    public const string MARK = "mark";
    public const string HIDE = "hide";
    public const string PENDING_ASYNC = "pending_async";
    public const string SYNC = "sync";

    private static string[] CIVL_ATTRIBUTES = { LAYER, YIELDS, MARK, HIDE, PENDING_ASYNC, SYNC };

    public const string LINEAR = "linear";
    public const string LINEAR_IN = "linear_in";
    public const string LINEAR_OUT = "linear_out";

    private static string[] LINEAR_ATTRIBUTES = { LINEAR, LINEAR_IN, LINEAR_OUT };

    public static bool HasCivlAttribute(this ICarriesAttributes obj)
    {
      for (var kv = obj.Attributes; kv != null; kv = kv.Next)
      {
        if (CIVL_ATTRIBUTES.Contains(kv.Key) || LINEAR_ATTRIBUTES.Contains(kv.Key))
        {
          return true;
        }
      }

      return false;
    }

    public static List<QKeyValue> FindAllAttributes(this ICarriesAttributes obj, string name)
    {
      var attributes = new List<QKeyValue>();
      for (var kv = obj.Attributes; kv != null; kv = kv.Next)
      {
        if (kv.Key == name)
        {
          attributes.Add(kv);
        }
      }

      return attributes;
    }

    public static bool HasAttribute(this ICarriesAttributes obj, string name)
    {
      return QKeyValue.FindBoolAttribute(obj.Attributes, name);
    }

    public static bool RemoveAttributes(ICarriesAttributes obj, Func<QKeyValue, bool> cond)
    {
      QKeyValue curr = obj.Attributes;
      bool removed = false;

      while (curr != null && cond(curr))
      {
        curr = curr.Next;
        removed = true;
      }

      obj.Attributes = curr;
      while (curr != null)
      {
        QKeyValue next = curr.Next;
        while (next != null && cond(next))
        {
          next = next.Next;
          removed = true;
        }

        curr.Next = next;
        curr = next;
      }

      return removed;
    }

    public static void RemoveAttributes(ICarriesAttributes obj, ICollection<string> keys)
    {
      RemoveAttributes(obj, kv => keys.Contains(kv.Key));
    }

    public static void RemoveCivlAttributes(ICarriesAttributes obj)
    {
      RemoveAttributes(obj, CIVL_ATTRIBUTES);
    }

    public static void RemoveLinearAttributes(ICarriesAttributes obj)
    {
      RemoveAttributes(obj, LINEAR_ATTRIBUTES);
    }

    public static bool IsCallMarked(CallCmd callCmd)
    {
      return callCmd.HasAttribute(MARK);
    }
  }

  public static class CivlPrimitives
  {
    public static HashSet<string> LinearTypes = new() { "Lmap", "Lset", "Lval" };

    public static HashSet<string> LinearPrimitives = new()
    {
      "Loc_New",
      "Lmap_Empty", "Lmap_Alloc", "Lmap_Create", "Lmap_Free", "Lmap_Move", "Lmap_Assume",
      "Lset_Empty", "Lset_Split", "Lset_Get", "Lset_Put",
      "Lval_Split", "Lval_Get", "Lval_Put"
    };

    public static IdentifierExpr ExtractRootFromAccessPathExpr(Expr expr)
    {
      if (expr is IdentifierExpr identifierExpr)
      {
        return identifierExpr;
      }
      if (expr is NAryExpr nAryExpr)
      {
        if (nAryExpr.Fun is FieldAccess or MapSelect)
        {
          return ExtractRootFromAccessPathExpr(nAryExpr.Args[0]);
        }
      }
      return null;
    }

    public static IdentifierExpr ModifiedArgument(CallCmd callCmd)
    {
      switch (Monomorphizer.GetOriginalDecl(callCmd.Proc).Name)
      {
        case "Loc_New":
        case "Lmap_Empty":
        case "Lmap_Alloc":
        case "Lmap_Create":
        case "Lmap_Free":
        case "Lmap_Move":
        case "Lmap_Assume":
        case "Lset_Empty":
          return null;
        default:
          return ExtractRootFromAccessPathExpr(callCmd.Ins[0]);
      }
    }

    public static HashSet<string> Async = new()
    {
      "create_async", "create_asyncs", "create_multi_asyncs", "set_choice"
    };
  }
}
