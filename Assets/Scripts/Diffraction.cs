using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using UnityEngine.UI;
using DG.Tweening;

public class Diffraction : MonoBehaviour{
    public Material ApertureMat;
    public Material ScreenMat;
    public Transform AperatureObject;
    public Text text;
    private Texture2D aperture;
    private Texture2D screenImage;
    public List<Transform> cameraPoints = new List<Transform>();
    public Camera mainCam;

    public bool EditingAperature = false;
    public bool Erasing = true;

    public float BrushSizeMultiplier = 0.2f;

    public System.Numerics.Complex[,] aperatureInput;

    int aperatureSize = 512;

    public bool screenAccurate = true;

    // Start is called before the first frame update
    void Start(){
        aperatureInput = new System.Numerics.Complex[aperatureSize, aperatureSize];

        aperture = new Texture2D(aperatureSize, aperatureSize, TextureFormat.ARGB32, false);
        screenImage = new Texture2D(aperatureSize, aperatureSize, TextureFormat.ARGB32, false);
        aperture.filterMode = FilterMode.Point;
        screenImage.filterMode = FilterMode.Point;
        Color32 resetColor = new Color32(255, 255, 255, 255);
        Color32[] resetColorArray = aperture.GetPixels32();
        for (int i = 0; i < resetColorArray.Length; i++) {
            resetColorArray[i] = resetColor;
        }
        aperture.SetPixels32(resetColorArray);
        aperture.Apply();
        ApertureMat.mainTexture = aperture;

        Color32[] blackenArray = aperture.GetPixels32();
        for (int i = 0; i < blackenArray.Length; i++) {
            blackenArray[i] = Color.black;
        }
        screenImage.SetPixels32(blackenArray);
        screenImage.Apply();
        ScreenMat.mainTexture = screenImage;
    }

    // Update is called once per frame
    void Update(){
        if (EditingAperature) {
            if (Input.GetMouseButton(0)) {
                Vector2 mousePos = (new Vector2(Input.mousePosition.x, Input.mousePosition.y) - new Vector2(Screen.width / 2, Screen.height / 2) + (new Vector2(1, 1)) * (0.25f) * (AperatureObject.localScale.x / mainCam.orthographicSize) * (float)Screen.height) / (0.5f * (AperatureObject.localScale.x / mainCam.orthographicSize) * (float)Screen.height);

                if (mousePos.x > 0 && mousePos.x < 1 && mousePos.y > 0 && mousePos.y < 1) {
                    screenAccurate = false;
                    int pixelRadius = 1 + Mathf.RoundToInt(BrushSizeMultiplier*24);
                    if (pixelRadius == 1) {
                        if (Erasing) {
                            aperture.SetPixel(Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.x)), Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.y)), Color.clear);
                            aperatureInput[Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.x)), Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.y))] = 1;
                        } else {
                            aperture.SetPixel(Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.x)), Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.y)), Color.white);
                            aperatureInput[Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.x)), Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.y))] = 0;
                        }
                    } else {
                        int PixelMouseX = Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.x)) - pixelRadius;
                        int PixelMouseY = Mathf.RoundToInt(((float)aperatureSize - 1) * (1 - mousePos.y)) - pixelRadius;
                        for (int x = 0; x < 2 * pixelRadius; x++) {
                            if ((PixelMouseX + x) < 0 || (PixelMouseX + x) > aperatureSize - 1) {
                                continue;
                            }
                            for (int y = 0; y < 2 * pixelRadius; y++) {
                                if ((PixelMouseY + y) < 0 || (PixelMouseY + y) > aperatureSize - 1) {
                                    continue;
                                }
                                if (Mathf.Pow((x - pixelRadius), 2) + Mathf.Pow((y - pixelRadius), 2) < pixelRadius * pixelRadius) {
                                    if (Erasing) {
                                        aperture.SetPixel(PixelMouseX + x, PixelMouseY + y, Color.clear);
                                        aperatureInput[PixelMouseX + x, PixelMouseY + y] = 1;
                                    } else {
                                        aperture.SetPixel(PixelMouseX + x, PixelMouseY + y, Color.white);
                                        aperatureInput[PixelMouseX + x, PixelMouseY + y] = 0;
                                    }
                                }
                            }
                        }
                    }
                    aperture.Apply();
                }
            }
        }
    }

    public void ChangeBrushSize(float _multiplier) {
        BrushSizeMultiplier = _multiplier;
    }

    public void RenderDiffraction() {
        if (screenAccurate) {
            return;
        }
        System.Numerics.Complex[,] result = aperatureInput.Copy();
        for (int i = 0; i < aperatureSize / 2; i++) {
            for (int j = 0; j < aperatureSize / 2; j++) {
                System.Numerics.Complex hold = result[i + aperatureSize / 2, +j + aperatureSize / 2];
                result[i + aperatureSize / 2, +j + aperatureSize / 2] = result[i, j];
                result[i, j] = hold;
            }
        }
        for (int i = aperatureSize / 2; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize / 2; j++) {
                System.Numerics.Complex hold = result[i - aperatureSize / 2, +j + aperatureSize / 2];
                result[i - aperatureSize / 2, +j + aperatureSize / 2] = result[i, j];
                result[i, j] = hold;
            }
        }
        FourierTransform.FFT2(result, FourierTransform.Direction.Forward);
        float largest = 0;
        for (int i = 0; i < aperatureSize - 1; i++) {
            for (int j = 0; j < aperatureSize - 1; j++) {
                float mod = (float)result[i, j].Magnitude;
                if (mod > largest) {
                    largest = mod;
                }
            }
        }
        for (int i = 0; i < aperatureSize / 2; i++) {
            for (int j = 0; j < aperatureSize / 2; j++) {
                System.Numerics.Complex hold = result[i + aperatureSize / 2, +j + aperatureSize / 2];
                result[i + aperatureSize / 2, +j + aperatureSize / 2] = result[i, j];
                result[i, j] = hold;
            }
        }
        for (int i = aperatureSize / 2; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize / 2; j++) {
                System.Numerics.Complex hold = result[i - aperatureSize / 2, +j + aperatureSize / 2];
                result[i - aperatureSize / 2, +j + aperatureSize / 2] = result[i, j];
                result[i, j] = hold;
            }
        }
        for (int i = 0; i < aperatureSize - 1; i++) {
            for (int j = 0; j < aperatureSize - 1; j++) {
                float mod = (float)result[i, j].Magnitude;
                byte whiteVal = (byte)Mathf.RoundToInt(255 * Mathf.Clamp(mod, 0, largest) / largest);
                screenImage.SetPixel(i, j, new Color32(whiteVal, whiteVal, whiteVal, 255));
            }
        }
        screenImage.Apply();
        screenAccurate = true;
    }

    public void ResetAperature() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                aperatureInput[i, j] = 0;
                aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }

    public void DoubleSlit() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                if (i == (aperatureSize / 2) - 8 || i == (aperatureSize / 2) - 7 || i == (aperatureSize / 2) + 8 || i == (aperatureSize / 2) + 7) {
                    aperatureInput[i, j] = 1;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 0));
                } else {
                    aperatureInput[i, j] = 0;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
                }
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }

    public void SingleSlit() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                if (i == (aperatureSize / 2) || i==(aperatureSize/2)+1) {
                    aperatureInput[i, j] = 1;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 0));
                } else {
                    aperatureInput[i, j] = 0;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
                }
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }

    public void Pinhole() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                if (Mathf.Pow((i-aperatureSize/2),2)+ Mathf.Pow((j - aperatureSize / 2), 2)<25) {
                    aperatureInput[i, j] = 1;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 0));
                } else {
                    aperatureInput[i, j] = 0;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
                }
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }

    public void Rectangle() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                if (Mathf.Abs(i - (aperatureSize / 2))<14 && Mathf.Abs(j - (aperatureSize / 2)) < 6) {
                    aperatureInput[i, j] = 1;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 0));
                } else {
                    aperatureInput[i, j] = 0;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
                }
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }

    public void Gratting() {
        for (int i = 0; i < aperatureSize; i++) {
            for (int j = 0; j < aperatureSize; j++) {
                if (i%15==0 || (i-1)%15==0) {
                    aperatureInput[i, j] = 1;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 0));
                } else {
                    aperatureInput[i, j] = 0;
                    aperture.SetPixel(i, j, new Color32(255, 255, 255, 255));
                }
            }
        }
        aperture.Apply();
        screenAccurate = false;
    }
}
