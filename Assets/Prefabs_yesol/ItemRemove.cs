using UnityEngine;
using Fusion;

public class ItemRemove : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if(transform.position.y < -100)
        {
            Runner.Despawn(Object);
        }
    }
}
