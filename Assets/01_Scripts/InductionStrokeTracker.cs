using UnityEngine;

/// <summary>Settles only complete, uninterrupted traversals between the area's two ends.</summary>
public sealed class InductionStrokeTracker
{
    private int startingSide;
    private float energy;
    private float elapsed;

    public void Reset() { startingSide = 0; energy = 0f; elapsed = 0f; }

    public bool Advance(float fromY, float toY, float deltaTime, float bottomY, float topY,
        float segmentEnergy, out float completedEnergy, out float completedDuration)
    {
        completedEnergy = 0f;
        completedDuration = 0f;
        if (topY <= bottomY || deltaTime <= 0f) return false;
        int Side(float y) => y <= bottomY ? -1 : y >= topY ? 1 : 0;
        var fromSide = Side(fromY);
        var toSide = Side(toY);

        // Being at either end arms the next crossing. Starting inside grants no partial reward.
        if (fromSide != 0)
        {
            startingSide = fromSide;
            energy = 0f;
            elapsed = 0f;
        }
        if (startingSide == 0)
        {
            if (toSide != 0) startingSide = toSide;
            return false;
        }

        var movement = toY - fromY;
        if (movement * startingSide > 0.00001f)
        {
            // Reversing inside invalidates the attempt, preventing edge-wiggle farming.
            Reset();
            if (toSide != 0) startingSide = toSide;
            return false;
        }

        var distance = Mathf.Abs(movement);
        var insideDistance = Mathf.Abs(Mathf.Clamp(toY, bottomY, topY) - Mathf.Clamp(fromY, bottomY, topY));
        if (distance > 0.00001f)
        {
            energy += Mathf.Max(0f, segmentEnergy);
            elapsed += deltaTime * insideDistance / distance;
        }
        else if (fromSide == 0) elapsed += deltaTime;

        if (toSide != -startingSide) return false;
        completedEnergy = energy;
        completedDuration = elapsed;
        startingSide = toSide;
        energy = 0f;
        elapsed = 0f;
        return completedDuration > 0f;
    }
}
