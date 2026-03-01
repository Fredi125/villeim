using UnityEngine;

namespace VillageLife.Quest
{
    /// <summary>
    /// Base class for quest objectives.
    /// </summary>
    public abstract class QuestObjective
    {
        public abstract bool IsComplete();
        public abstract float GetProgress();
        public abstract string GetDescription();
        public abstract string SerializeProgress();
    }

    /// <summary>
    /// Kill X of a specific creature type.
    /// </summary>
    public class KillObjective : QuestObjective
    {
        public string TargetCreature { get; }
        public int RequiredCount { get; }
        public int CurrentCount { get; private set; }

        public KillObjective(string target, int count)
        {
            TargetCreature = target;
            RequiredCount = count;
        }

        public void IncrementProgress(int amount)
        {
            CurrentCount = Mathf.Min(CurrentCount + amount, RequiredCount);
        }

        public override bool IsComplete() => CurrentCount >= RequiredCount;
        public override float GetProgress() => (float)CurrentCount / RequiredCount;
        public override string GetDescription() => $"Kill {TargetCreature}: {CurrentCount}/{RequiredCount}";
        public override string SerializeProgress() => $"{TargetCreature},{RequiredCount},{CurrentCount}";
    }

    /// <summary>
    /// Gather X of a specific item (checks player inventory).
    /// </summary>
    public class GatherObjective : QuestObjective
    {
        public string TargetItem { get; }
        public int RequiredCount { get; }
        public int CurrentCount { get; private set; }

        public GatherObjective(string target, int count)
        {
            TargetItem = target;
            RequiredCount = count;
        }

        public void IncrementProgress(int amount)
        {
            CurrentCount = Mathf.Min(CurrentCount + amount, RequiredCount);
        }

        public void SyncWithInventory(int inventoryCount)
        {
            CurrentCount = Mathf.Min(inventoryCount, RequiredCount);
        }

        public override bool IsComplete() => CurrentCount >= RequiredCount;
        public override float GetProgress() => (float)CurrentCount / RequiredCount;
        public override string GetDescription() => $"Gather {TargetItem}: {CurrentCount}/{RequiredCount}";
        public override string SerializeProgress() => $"{TargetItem},{RequiredCount},{CurrentCount}";
    }

    /// <summary>
    /// Deliver X of a specific item to the quest giver NPC.
    /// </summary>
    public class DeliverObjective : QuestObjective
    {
        public string TargetItem { get; }
        public int RequiredCount { get; }
        public bool Delivered { get; private set; }

        public DeliverObjective(string target, int count)
        {
            TargetItem = target;
            RequiredCount = count;
        }

        public bool TryDeliver(Player player)
        {
            var inventory = player.GetInventory();
            if (inventory.CountItems(TargetItem) >= RequiredCount)
            {
                inventory.RemoveItem(TargetItem, RequiredCount);
                Delivered = true;
                return true;
            }
            return false;
        }

        public override bool IsComplete() => Delivered;
        public override float GetProgress() => Delivered ? 1f : 0f;
        public override string GetDescription() => $"Deliver {RequiredCount}x {TargetItem}: {(Delivered ? "Done" : "Pending")}";
        public override string SerializeProgress() => $"{TargetItem},{RequiredCount},{(Delivered ? 1 : 0)}";
    }

    /// <summary>
    /// Discover a specific location or point of interest.
    /// </summary>
    public class ExploreObjective : QuestObjective
    {
        public string TargetLocation { get; }
        public bool Discovered { get; private set; }

        public ExploreObjective(string target)
        {
            TargetLocation = target;
        }

        public void MarkComplete()
        {
            Discovered = true;
        }

        public override bool IsComplete() => Discovered;
        public override float GetProgress() => Discovered ? 1f : 0f;
        public override string GetDescription() => $"Explore {TargetLocation}: {(Discovered ? "Found" : "Undiscovered")}";
        public override string SerializeProgress() => $"{TargetLocation},{(Discovered ? 1 : 0)}";
    }

    /// <summary>
    /// Build a specific structure/piece.
    /// </summary>
    public class BuildObjective : QuestObjective
    {
        public string TargetPiece { get; }
        public int RequiredCount { get; }
        public int CurrentCount { get; private set; }

        public BuildObjective(string target, int count)
        {
            TargetPiece = target;
            RequiredCount = count;
        }

        public void IncrementProgress(int amount)
        {
            CurrentCount = Mathf.Min(CurrentCount + amount, RequiredCount);
        }

        public override bool IsComplete() => CurrentCount >= RequiredCount;
        public override float GetProgress() => (float)CurrentCount / RequiredCount;
        public override string GetDescription() => $"Build {TargetPiece}: {CurrentCount}/{RequiredCount}";
        public override string SerializeProgress() => $"{TargetPiece},{RequiredCount},{CurrentCount}";
    }
}
