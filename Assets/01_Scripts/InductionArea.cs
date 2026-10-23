using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class InductionArea : MonoBehaviour
{
    private BoxCollider2D area;
    public Bounds Bounds
    {
        get
        {
            if (area == null) area = GetComponent<BoxCollider2D>();
            return area.bounds;
        }
    }

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnDrawGizmosSelected()
    {
        var bounds = Bounds;
        Gizmos.color = new Color(1f, 0.9f, 0.35f, 0.25f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(1f, 0.9f, 0.35f, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
