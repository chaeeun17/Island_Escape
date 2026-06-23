using UnityEngine;

public class MouthController : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트가 'Food' 태그를 가지고 있는지 확인
        if (other.CompareTag("Food"))
        {
           PlayerStats.Instance.RestoreHunger(25f);
           Destroy(other.gameObject);
           Debug.Log($"{other.gameObject.name}을 먹었습니다!");
        }
    }

    private void Eat()
    {
        
        
    }
}