using UnityEngine;

public class ItemRemove : MonoBehaviour
{ 
    void Update()
    {
        if(transform.position.y < -10)
        {
            Destroy(gameObject);
        }
    }
}
