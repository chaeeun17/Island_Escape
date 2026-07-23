using UnityEngine;

public class InteractionGuideUI : MonoBehaviour
{
    public Vector3 uiOffset = new Vector3(0f, 0.3f, 0f);

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    void Start()
    {
        // 처음 시작할 때 숨기기
        ShowUI(false);
    }

    public void ShowUI(bool isOn, Transform target = null)
    {
        if (canvas != null)
        {
            canvas.enabled = isOn;
        }

        if (isOn && target != null)
        {
            gameObject.transform.position = target.position + uiOffset;
            
            if (Camera.main != null)
            {
                gameObject.transform.LookAt(Camera.main.transform.position);
                gameObject.transform.Rotate(0, 180, 0);
            }
        }
    }
}