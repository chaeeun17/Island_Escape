using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ModelSaver : MonoBehaviour
{
    public RenderTexture DrawTexture;   //PNG저장할 타겟 렌더 텍스쳐
    public string name;


    void Start()
    {
        StartCoroutine(SaveScreenshot());
    }

    IEnumerator SaveScreenshot()
    {
        yield return new WaitForEndOfFrame();
        RenderTextureSave();
    }

    void RenderTextureSave()
    {
        RenderTexture.active = DrawTexture;
        var texture2D = new Texture2D(DrawTexture.width, DrawTexture.height);
        texture2D.ReadPixels(new Rect(0, 0, DrawTexture.width, DrawTexture.height), 0, 0);



        Color[] pixels = texture2D.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pixels[i].gamma;
        }

        texture2D.SetPixels(pixels);


        texture2D.Apply();
        var data = texture2D.EncodeToPNG();
        File.WriteAllBytes($"C:/Users/yesol/Desktop/Terrain/20260623/{name}.png", data);
    }
}