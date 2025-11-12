using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Interval
{
    public float Start;
    public float End;

    public bool Intersects(Interval other)
    {
        return Start <= other.End && End >= other.Start;
    }

    public override string ToString()
    {
        return $"[{Start}, {End}]";
    }
}

public class SweepAndPrune : MonoBehaviour
{
    public List<Interval> intervals; // Liste publique d'intervalles

    void Start()
    {
        DetectIntersections();
    }

    void DetectIntersections()
    {
        List<(Interval interval, float point, bool isStart)> events = new List<(Interval, float, bool)>();

        // Créer une liste d'événements pour les points de début et de fin
        foreach (var interval in intervals)
        {
            events.Add((interval, interval.Start, true));  // Point de début
            events.Add((interval, interval.End, false));   // Point de fin
        }

        // Trier les événements
        events.Sort((a, b) =>
        {
            if (a.point == b.point)
                return a.isStart ? -1 : 1;  // Prioriser le début sur la fin
            return a.point.CompareTo(b.point);
        });

        List<Interval> activeIntervals = new List<Interval>();

        // Balayer les événements
        foreach (var (interval, point, isStart) in events)
        {
            if (isStart)
            {
                // Vérifier les intersections avec les intervalles actifs
                foreach (var active in activeIntervals)
                {
                    if (active.Intersects(interval))
                    {
                        Debug.Log($"Intersection détectée entre {active} et {interval}");
                    }
                }
                activeIntervals.Add(interval); // Ajouter l'intervalle actif
            }
            else
            {
                activeIntervals.Remove(interval); // Retirer l'intervalle actif
            }
        }
    }
}
