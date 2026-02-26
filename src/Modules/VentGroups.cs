using UnityEngine;

namespace BetterAmongUs.Modules;

internal static class VentGroups
{
    private static readonly Dictionary<int, int> ventToLowestId = [];
    private static readonly Dictionary<int, Color> groupColors = [];

    internal static void CalculateAllVentGroups(Vent[] vents)
    {
        ventToLowestId.Clear();
        groupColors.Clear();

        HashSet<Vent> unprocessedVents = [.. vents];

        HashSet<int> groupLowestIds = [];
        Dictionary<int, List<Vent>> groups = [];

        while (unprocessedVents.Count > 0)
        {
            Vent startVent = unprocessedVents.First();

            HashSet<Vent> group = [];
            Queue<Vent> toVisit = new();
            int lowestId = startVent.Id;

            toVisit.Enqueue(startVent);

            while (toVisit.Count > 0)
            {
                Vent current = toVisit.Dequeue();
                if (group.Contains(current)) continue;

                group.Add(current);
                unprocessedVents.Remove(current);

                if (current.Id < lowestId)
                {
                    lowestId = current.Id;
                }

                Vent[] connections = { current.Left, current.Right, current.Center };
                foreach (var connection in connections)
                {
                    if (connection != null && !group.Contains(connection) && !toVisit.Contains(connection))
                    {
                        toVisit.Enqueue(connection);
                    }
                }
            }

            groupLowestIds.Add(lowestId);
            groups[lowestId] = [.. group];
        }

        Color[] colorGroups =
        [
            Color.red,
            Color.green,
            Color.blue,
            Color.gray,
            Color.magenta,
            Color.cyan,
            Color.white
        ];

        for (int i = 0; i < groupLowestIds.Count; i++)
        {
            int lowestId = groupLowestIds.ElementAt(i);
            int colorIndex = i % colorGroups.Length;
            groupColors[lowestId] = colorGroups[colorIndex] + (new Color(1f, 1f, 1f, 0f) * 0.2f);
        }

        foreach (var kvp in groups)
        {
            int lowestId = kvp.Key;
            foreach (var vent in kvp.Value)
            {
                ventToLowestId[vent.Id] = lowestId;
            }
        }
    }

    internal static Color GetVentGroupColor(Vent vent)
    {
        if (ventToLowestId.TryGetValue(vent.Id, out int lowestId))
        {
            return groupColors[lowestId];
        }

        return Color.black;
    }
}
