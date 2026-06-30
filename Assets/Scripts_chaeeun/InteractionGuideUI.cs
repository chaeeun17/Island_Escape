using UnityEngine;

public class InteractionGuideUI : MonoBehaviour
{
    public Vector3 uiOffset = new Vector3(0f, 0.3f, 0f);

    void Start() => ShowUI(false);

    public void ShowUI(bool isOn, Transform target = null)
    {
        gameObject.SetActive(isOn);

        if (isOn && target != null)
        {
            // 위치 이동 및 카메라 바라보기 연산 전담
            gameObject.transform.position = target.position + uiOffset;
            if (Camera.main != null)
            {
                gameObject.transform.LookAt(Camera.main.transform.position);
                gameObject.transform.Rotate(0, 180, 0);
            }
        }
    }
}