using UnityEngine;

public class IconGenerator : MonoBehaviour
{
    [Header("3D 원본 프리팹")]
    public Transform prefab3D;

    [Header("이미지를 주입할 아이템 (SO)")]
    public Item targetItem;

    void Awake()
    {

        if (prefab3D != null && targetItem != null)
        {
            GenerateAndSetItemIcon();
        }
    }

    public void GenerateAndSetItemIcon()
    {

        RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
        RuntimePreviewGenerator.Padding = 0.1f;
        RuntimePreviewGenerator.OrthographicMode = true; 

  
        Texture2D previewTexture = RuntimePreviewGenerator.GenerateModelPreview(prefab3D, 256, 256);

        Sprite generatedSprite = Sprite.Create(
            previewTexture,
            new Rect(0, 0, previewTexture.width, previewTexture.height),
            new Vector2(0.5f, 0.5f)
        );


        targetItem.setSprite(generatedSprite);

        Debug.Log($"{targetItem.getItemName()} 아이템의 2D 아이콘 생성 및 적용 완료!");
    }
}