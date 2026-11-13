using System;
using UnityEngine;

/// <summary>
/// A spot in the room with a radius. It raises <see cref="Triggered"/> while the player is inside it
/// (measured on the floor plane). It holds no on/off state: whoever owns it enables and disables the GameObject.
/// </summary>
public class TriggerPoint : MonoBehaviour
{
    [SerializeField] private float radius = 0.5f;

    public event Action Triggered;

    private Transform _player;

    /// <summary>Gives the trigger point the transform it should measure distance to (the player's head).</summary>
    public void Initialize(Transform player)
    {
        _player = player;
    }

    private void Update()
    {
        var offset = _player.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= radius * radius)
        {
            Triggered?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
