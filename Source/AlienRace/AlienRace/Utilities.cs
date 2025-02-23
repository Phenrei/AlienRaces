﻿namespace AlienRace
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HarmonyLib;
    using JetBrains.Annotations;
    using RimWorld;
    using Verse;
    using Verse.AI;

    [DefOf]
    public static class AlienDefOf
    {
#pragma warning disable IDE1006 // Benennungsstile
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Global
        public static TraitDef Xenophobia;
        public static ThoughtDef XenophobiaVsAlien;
        public static ThingCategoryDef alienCorpseCategory;
        // ReSharper restore UnusedMember.Global
        // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006 // Benennungsstile
    }

    [UsedImplicitly]
    public class ThinkNode_ConditionalIsMemberOfRace : ThinkNode_Conditional
    {
        public List<ThingDef> races;

        protected override bool Satisfied(Pawn pawn) => 
            this.races.Contains(pawn.def);
    }

    [UsedImplicitly]
    public class Thought_XenophobeVsXenophile : Thought_SituationalSocial
    {
        public override float OpinionOffset() =>
             this.pawn.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) == 1 ? -25f : -15f;
    }

    [UsedImplicitly]
    public class ThoughtWorker_XenophobeVsXenophile : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn) => 
            p.RaceProps.Humanlike && otherPawn.RaceProps.Humanlike && p.story.traits.HasTrait(AlienDefOf.Xenophobia) && otherPawn.story.traits.HasTrait(AlienDefOf.Xenophobia) && 
            p.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) != otherPawn.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) && RelationsUtility.PawnsKnowEachOther(p, otherPawn);
    }

    [UsedImplicitly]
    public class Thought_XenophobiaVsAlien : Thought_SituationalSocial
    {
        public override float OpinionOffset() => 
            this.pawn.def != this.OtherPawn().def ? this.pawn.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) == 1 ? -30 : 
            this.OtherPawn().story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) == 1 ? -15 : 0 : 0;
    }

    [UsedImplicitly]
    public class ThoughtWorker_XenophobiaVsAlien : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn) =>
            p.def != otherPawn.def && p.RaceProps.Humanlike && otherPawn.RaceProps.Humanlike && RelationsUtility.PawnsKnowEachOther(p, otherPawn) &&
            !(p.def is ThingDef_AlienRace par && par.alienRace.generalSettings.notXenophobistTowards.Contains(otherPawn.def)) &&
            !(otherPawn.def is ThingDef_AlienRace oar && oar.alienRace.generalSettings.immuneToXenophobia) ?
                p.story.traits.HasTrait(AlienDefOf.Xenophobia) ?
                    p.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) == -1 ?
                        ThoughtState.ActiveAtStage(stageIndex: 0) :
                        ThoughtState.ActiveAtStage(stageIndex: 1) :
                    false :
                false;
    }

    [UsedImplicitly]
    public class ThoughtWorker_AlienVsXenophobia : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn) =>
            p.def != otherPawn.def && p.RaceProps.Humanlike && otherPawn.RaceProps.Humanlike && RelationsUtility.PawnsKnowEachOther(p, otherPawn) &&
            !(otherPawn.def is ThingDef_AlienRace par && par.alienRace.generalSettings.notXenophobistTowards.Contains(p.def)) &&
            !(p.def is ThingDef_AlienRace oar && oar.alienRace.generalSettings.immuneToXenophobia) ?
                otherPawn.story.traits.HasTrait(AlienDefOf.Xenophobia) ?
                    otherPawn.story.traits.DegreeOfTrait(AlienDefOf.Xenophobia) == -1 ?
                        ThoughtState.ActiveAtStage(stageIndex: 0) :
                        ThoughtState.ActiveAtStage(stageIndex: 1) :
                    false :
                false;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class LoadDefFromField : Attribute
    {
        private string defName;

        public LoadDefFromField(string defName)
        {
            this.defName = defName;
        }

        public Def GetDef(Type defType) => 
            GenDefDatabase.GetDef(defType, this.defName);
    }

    public class Graphic_Multi_RotationFromData : Graphic_Multi
    {
        public override bool ShouldDrawRotated => 
            this.data?.drawRotated ?? false;
    }

    public static class CachedData
    {
        private static Dictionary<RaceProperties, ThingDef> racePropsToRaceDict = new Dictionary<RaceProperties, ThingDef>();

        public static ThingDef GetRaceFromRaceProps(RaceProperties props)
        {
            if (!racePropsToRaceDict.ContainsKey(props))
                racePropsToRaceDict.Add(props,
                                        new List<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading).Concat(new List<ThingDef_AlienRace>(DefDatabase<ThingDef_AlienRace>.AllDefsListForReading))
                                                                                                    .First(predicate: td => td.race == props));

            return racePropsToRaceDict[props];
        }

        public static readonly AccessTools.FieldRef<Pawn_StoryTracker, string> headGraphicPath = AccessTools.FieldRefAccess<Pawn_StoryTracker, string>("headGraphicPath");

        public static readonly AccessTools.FieldRef<List<ThingStuffPair>> allApparelPairs =
            AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(AccessTools.Field(typeof(PawnApparelGenerator), "allApparelPairs"));

        public static readonly AccessTools.FieldRef<List<ThingStuffPair>> allWeaponPairs =
            AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(AccessTools.Field(typeof(PawnWeaponGenerator), "allWeaponPairs"));

        public delegate void PawnGeneratorPawnRelations(Pawn pawn, ref PawnGenerationRequest request);

        public static readonly PawnGeneratorPawnRelations generatePawnsRelations =
            AccessTools.MethodDelegate<PawnGeneratorPawnRelations>(AccessTools.Method(typeof(PawnGenerator), "GeneratePawnRelations"));

        public static readonly AccessTools.FieldRef<PawnTextureAtlas, Dictionary<Pawn, PawnTextureAtlasFrameSet>> PawnTextureAtlasFrameAssignments =
            AccessTools.FieldRefAccess<PawnTextureAtlas, Dictionary<Pawn, PawnTextureAtlasFrameSet>>("frameAssignments");
    }
}