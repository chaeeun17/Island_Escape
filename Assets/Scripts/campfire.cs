using UnityEngine;
using System.Collections;

public class Campfire : MonoBehaviour
{
    [Header("상태")]
    public bool isLit = false;

    [Header("시각 효과")]
    public ParticleSystem fireParticles;
    public Light fireLight;

    [Header("요리 설정")]
    public float cookTime = 3f;

    private void Start()
    {
        UpdateFireState();
    }

    public void Ignite()
    {
        isLit = true;
        UpdateFireState();
    }

    private void UpdateFireState()
    {
        if (fireParticles != null)
        {
            if (isLit) fireParticles.Play();
            else fireParticles.Stop();
        }
        if (fireLight != null) fireLight.enabled = isLit;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 불 켜기 로직 (횃불 등은 여전히 태그나 특정 로직 유지)
        if (other.CompareTag("Torch") && !isLit)
        {
            Ignite();
            return;
        }

        // 2. 요리 로직 (아이템 특성 기반)
        if (isLit)
        {
            // 충돌한 3D 오브젝트에서 WorldItem 스크립트 추출
            WorldItem worldItem = other.GetComponent<WorldItem>();

            if (worldItem != null && worldItem.itemData != null)
            {
                // 아이템 데이터의 특성이 'RawFood'인지 확인
                if (worldItem.itemData.GetItemType() == ItemType.RawFood)
                {
                    // 요리 코루틴 실행 (물리적 오브젝트와 원본 데이터 모두 전달)
                    StartCoroutine(CookFood(other.gameObject, worldItem.itemData));
                }
            }
        }
    }

    private IEnumerator CookFood(GameObject rawFoodObject, Item rawFoodData)
    {
        Debug.Log($"{rawFoodData.getItemName()} 요리 시작...");

        yield return new WaitForSeconds(cookTime);

        // 요리가 완료되는 시점까지 물체가 파괴되지 않고 남아있다면
        if (rawFoodObject != null)
        {
            Vector3 spawnPos = rawFoodObject.transform.position;

            // 데이터에 등록된 구워진 결과물 가져오기
            Item cookedItemData = rawFoodData.GetCookedVersion();

            if (cookedItemData != null && cookedItemData.GetPrefab3D() != null)
            {
                // 구워진 3D 프리팹 생성
                Instantiate(cookedItemData.GetPrefab3D(), spawnPos, Quaternion.identity);
                Debug.Log("요리 완성!");
            }
            else
            {
                Debug.LogWarning("결과물 데이터나 3D 프리팹이 등록되지 않았습니다.");
            }

            // 기존 날고기 3D 오브젝트 파괴
            Destroy(rawFoodObject);
        }
    }
}