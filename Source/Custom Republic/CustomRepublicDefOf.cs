using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CustomRepublic;

[DefOf]
public class CustomRepublicDefOf
{
    [MayRequireRoyalty]
    public static RoyalTitlePermitDef? InviteToRepublic;

    static CustomRepublicDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(CustomRepublicDefOf));
}
