using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CameraController : MonoBehaviour{
    public Diffraction diffraction;
    public List<GameObject> UIObjects = new List<GameObject>();
    public int currentPlace = 0; // 0 overview, 1 edit aperature, 2 observe screen.
    public List<Transform> placements;
    public Transform aperature;
    bool GoingTo = false;
    Camera cam;

    public void GoTo(int place) {
        if (place == currentPlace || GoingTo) {
            return;
        }
                GoingTo = true;
        if (currentPlace == 1) {
            diffraction.EditingAperature = false;
            diffraction.RenderDiffraction();
            float x = 0;
            DOTween.To(() => UIObjects[1].GetComponent<RectTransform>().anchoredPosition.x, k => x = k, -227, 0.5f).SetEase(Ease.InBack).OnUpdate(() => { UIObjects[1].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, UIObjects[1].GetComponent<RectTransform>().anchoredPosition.y); });
            FakeOrthoReverseTransition(() => {
                transform.DOMove(placements[place].position, 0.5f).SetEase(Ease.OutBack);
                transform.DORotateQuaternion(placements[place].rotation, 0.5f).SetEase(Ease.OutBack);
            });
        } else {
            transform.DOMove(placements[place].position, 0.5f).SetEase(Ease.OutBack);
            transform.DORotateQuaternion(placements[place].rotation, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                if (place == 1) {
                    FakeOrthoTransition();
                } else {
                    GoingTo = false;
                }
            });
            if (place == 1) {
                float x = 0;
                DOTween.To(() => UIObjects[1].GetComponent<RectTransform>().anchoredPosition.x, k => x = k, 20, 0.5f).SetEase(Ease.OutBack).OnUpdate(() => { UIObjects[1].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, UIObjects[1].GetComponent<RectTransform>().anchoredPosition.y); });
            }
        }
        currentPlace = place;
    }

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();   
    }

    private float initHeightAtDist;

    // Calculate the frustum height at a given distance from the camera.
    float FrustumHeightAtDistance(float distance) {
        return 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // Calculate the FOV needed to get a given frustum height at a given distance.
    float FOVForHeightAndDistance(float height, float distance) {
        return 2.0f * Mathf.Atan(height * 0.5f / distance) * Mathf.Rad2Deg;
    }

    void FakeOrthoTransition() {
        UIObjects[0].SetActive(false);
        Vector3 originalPos = transform.position;
        var distance = Vector3.Distance(transform.position, aperature.position);
        initHeightAtDist = FrustumHeightAtDistance(distance);

        transform.DOMove(transform.position-transform.forward*500,1f).SetEase(Ease.InCirc).OnUpdate(()=>{
            cam.fieldOfView = FOVForHeightAndDistance(initHeightAtDist, Vector3.Distance(transform.position, aperature.position));
        }).OnComplete(()=> { 
            cam.orthographic = true;
            transform.position = originalPos;
            UIObjects[0].SetActive(true);
            diffraction.EditingAperature = true;
            GoingTo = false;
        });
        //float x = 0;
        //DOTween.To(() => cam.fieldOfView, k => x = k, 10, 0.5f).OnUpdate(()=> { cam.fieldOfView = x; });
    }

    void FakeOrthoReverseTransition(System.Action act) {
        UIObjects[0].SetActive(false);
        var distance = Vector3.Distance(transform.position, aperature.position);
        initHeightAtDist = 2.0f * distance * Mathf.Tan(60 * 0.5f * Mathf.Deg2Rad);

        Vector3 originalPos = transform.position;
        transform.position = transform.position - transform.forward * 500;
        cam.orthographic = false;
        cam.fieldOfView = FOVForHeightAndDistance(initHeightAtDist, Vector3.Distance(transform.position, aperature.position));

        transform.DOMove(originalPos, 1f).SetEase(Ease.OutCirc).OnUpdate(() => {
            cam.fieldOfView = FOVForHeightAndDistance(initHeightAtDist, Vector3.Distance(transform.position, aperature.position));
        }).OnComplete(() => {
            transform.position = originalPos;
            GoingTo = false;
            UIObjects[0].SetActive(true);
            act();
        });
        //float x = 0;
        //DOTween.To(() => cam.fieldOfView, k => x = k, 10, 0.5f).OnUpdate(()=> { cam.fieldOfView = x; });
    }

    public void SelectedErase(bool erase) {
        diffraction.Erasing = erase;
        if (erase) {
            UIObjects[2].GetComponent<Image>().color = new Color32(192, 244, 255,255);
            UIObjects[3].GetComponent<Image>().color = Color.white;
        } else {
            UIObjects[3].GetComponent<Image>().color = new Color32(192, 244, 255, 255);
            UIObjects[2].GetComponent<Image>().color = Color.white;
        }
    }
}
